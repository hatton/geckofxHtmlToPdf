using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using Gecko;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace GeckofxHtmlToPdf
{
	/* NB: I am currently facing a problem in having a component using a geckofx in a client which is
	 * also using it (which was my original use case)... setting "gfx.direct2d.disabled" to true, as we
	 * have to do, was giving me a COM crash in the client app, which, by the time it used this, had
	 * already been using the browser for other purposes. So for now, I'm making this class private again.
	 * If someone really needed it, they could make it public again, particularly if they aren't already
	 * using geckofx in their main project. But for me, it's not worth maintaining/documenting at this
	 * point, as my client app is going to have to use this as a command-line. Note, Hindle couldn't
	 * reproduce this in the geckofx sample app, so there's a mystery to be solved some day.
	 *
	 * Why is this a component? Only becuase the geckobrowser that we use, even though it is invisible,
	* expects to be operating on the UI thread, getting events from the Application.DoEvents() loop, etc.
	* So we can't be making pdfs on a background thread. Having it as a component with a timer and
	* an event to signal when it is done makes it easy for programmer incorporating this see how to use it properly.
	 *
	 * This component is used by the ConversionProgress form in this assembly for when the exe is used
	 * on the command line. But it can also be used by any other winforms app that references our assembly
	 * (even though it is an exe, not the usual dll). Just drag the component onto some other form, then
	 * call Start();
	*/

	public partial class GeckofxHtmlToPdfComponent : Component, nsIWebProgressListener
	{
		private ConversionOrder _conversionOrder;
		private GeckoWebBrowser _browser;
		private nsIWebBrowserPrint _print;
		private nsIPrintSettings _printSettings;
		private string _pathToTempPdf;
		private bool _finished;
		private DateTime _startMakingPdf;
		private DateTime _beginPages;
		private int _currentPage;
		private string _currentFile;
		long _prevLength;
		int _prevCount;

		public event EventHandler Finished;
		public event EventHandler<PdfMakingStatus> StatusChanged;
		public string Status { get; private set; }

		public GeckofxHtmlToPdfComponent()
		{
			InitializeComponent();
		}

		public GeckofxHtmlToPdfComponent(IContainer container)
		{
			container.Add(this);

			InitializeComponent();
		}

		/// <summary>
		/// On the application event thread, work on creating the pdf. Will raise the StatusChanged and Finished events
		/// </summary>
		/// <param name="conversionOrder"></param>
		public void Start(ConversionOrder conversionOrder)
		{
			if (!Gecko.Xpcom.IsInitialized)
			{
				throw new ApplicationException("Developer: you must call Initialize(pathToXulRunnerFolder), or do your own Gecko.Xpcom.Initialize(), before calling Start()");
			}

			//without this, we get invisible (white?) text on some machines
			Gecko.GeckoPreferences.User["gfx.direct2d.disabled"] = true;

			if (conversionOrder.EnableGraphite)
				GeckoPreferences.User["gfx.font_rendering.graphite.enabled"] = true;

			// geckofx can sometimes cache stale data, at least on Windows.  These settings should prevent
			// any caching (which we don't need anyway) from happening.
			// See https://silbloom.myjetbrains.com/youtrack/issue/BL-5875 for details.
			GeckoPreferences.User["browser.cache.disk.enable"] = false;
			GeckoPreferences.User["browser.cache.memory.enable"] = false;
			GeckoPreferences.User["browser.cache.offline.enable"] = false;
			GeckoPreferences.User["dom.caches.enable"] = false;

			_conversionOrder = conversionOrder;
			_browser = new GeckoWebBrowser();
			this.components.Add(_browser);//so it gets disposed when we are

			if (conversionOrder.Debug)
			{
				_browser.ConsoleMessage += OnBrowserConsoleMessage;
			}

			var tempFileName = Path.GetTempFileName();
			File.Delete(tempFileName);
			_pathToTempPdf = tempFileName + ".pdf";
			File.Delete(_conversionOrder.OutputPdfPath);
			_checkForBrowserNavigatedTimer.Enabled = true;
			Status = "Loading Html...";

			// Why set a size here? If we don't, images sometimes don't show up in the PDF. See BL-408.
			// A size of 500x500 was enough to fix the problem for the most reproducible case,
			// JohnH's version of Pame's Family Battles Maleria. The size used here is based
			// on an unproved hypothesis that it's important for at least one picture to be
			// visible in the imaginary browser window; thus, we've made it big enough for a
			// 16x11 big-book page at fairly high screen resolution of 120dpi.
			_browser.Size = new Size(1920, 1320);

			_browser.Navigate(_conversionOrder.InputHtmlPath);
		}

		protected virtual void RaiseStatusChanged(PdfMakingStatus e)
		{
			var handler = StatusChanged;
			if (handler != null) handler(this, e);
		}

		protected virtual void RaiseFinished()
		{
			var handler = Finished;
			if (handler != null) handler(this, EventArgs.Empty);
		}

		private void OnBrowserConsoleMessage(object sender, ConsoleMessageEventArgs e)
		{
			//review: this won't do anything if we're not in command-line mode...
			//maybe a better design would be to rais an event that the consumer can
			//do something with, e.g. the command-line ConversionProgress form could
			//just turn around and write to the console
			Console.WriteLine(e.Message);
		}

		private class PaperSize
		{
			public readonly string Name;
			public readonly double WidthInMillimeters;
			public readonly double HeightInMillimeters;

			public PaperSize(string name, double widthInMillimeters, double heightInMillimeters)
			{
				Name = name;
				WidthInMillimeters = widthInMillimeters;
				HeightInMillimeters = heightInMillimeters;
			}
		}

		private PaperSize GetPaperSize(string name)
		{
			name = name.ToLower();
			var sizes = new List<PaperSize>
				{
					new PaperSize("a3", 297, 420),
					new PaperSize("a4", 210, 297),
					new PaperSize("a5", 148, 210),
					new PaperSize("a6", 105, 148),
					new PaperSize("b3", 353, 500),
					new PaperSize("b4", 250, 353),
					new PaperSize("b5", 176, 250),
					new PaperSize("b6", 125, 176),
					new PaperSize("letter", 215.9, 279.4),
					new PaperSize("halfletter", 139.7, 215.9),
					new PaperSize("quarterletter", 107.95, 139.7),
					new PaperSize("legal", 215.9, 355.6),
					new PaperSize("halflegal", 177.8, 215.9),
					new PaperSize("device16x9", 100, 1600/9d)
				};

			var match =sizes.Find(s => s.Name == name);
			if (match != null)
				return match;

			throw new ApplicationException(
					"Sorry, currently GeckofxHtmlToPDF has a very limited set of paper sizes it knows about. Consider using the page-height and page-width arguments instead");
		}

		private void StartMakingPdf()
		{
			_print = Xpcom.QueryInterface<nsIWebBrowserPrint>(_browser.Window.DomWindow);

			var service = Xpcom.GetService<nsIPrintSettingsService>("@mozilla.org/gfx/printsettings-service;1");
			_printSettings = service.GetNewPrintSettingsAttribute();

			_printSettings.SetToFileNameAttribute(new Gecko.nsAString(_pathToTempPdf));
			_printSettings.SetPrintToFileAttribute(true);
			_printSettings.SetPrintSilentAttribute(true); //don't show a printer settings dialog
			_printSettings.SetShowPrintProgressAttribute(false);

			if (_conversionOrder.PageHeightInMillimeters > 0)
			{
				_printSettings.SetPaperHeightAttribute(_conversionOrder.PageHeightInMillimeters);
				_printSettings.SetPaperWidthAttribute(_conversionOrder.PageWidthInMillimeters);
				_printSettings.SetPaperSizeUnitAttribute(1); //0=in, >0 = mm
			}
			else
			{
				//doesn't actually work.  Probably a problem in the geckofx wrapper. Meanwhile we just look it up from our small list
				//printSettings.SetPaperNameAttribute(_conversionOrder.PageSizeName);

				var size = GetPaperSize(_conversionOrder.PageSizeName);
				const double inchesPerMillimeter = 0.0393701;	// (or more precisely, 0.0393700787402)
				_printSettings.SetPaperHeightAttribute(size.HeightInMillimeters*inchesPerMillimeter);
				_printSettings.SetPaperWidthAttribute(size.WidthInMillimeters*inchesPerMillimeter);

			}

			// BL-2346: On Linux the margins were not being set correctly due to the "unwritable margins"
			//          which were defaulting to 0.25 inches.
			_printSettings.SetUnwriteableMarginTopAttribute(0d);
			_printSettings.SetUnwriteableMarginBottomAttribute(0d);
			_printSettings.SetUnwriteableMarginLeftAttribute(0d);
			_printSettings.SetUnwriteableMarginRightAttribute(0d);

			//this seems to be in inches, and doesn't have a unit-setter (unlike the paper size ones)
			const double kMillimetersPerInch = 25.4; // (or more precisely, 25.3999999999726)
			_printSettings.SetMarginTopAttribute(_conversionOrder.TopMarginInMillimeters/kMillimetersPerInch);
			_printSettings.SetMarginBottomAttribute(_conversionOrder.BottomMarginInMillimeters/kMillimetersPerInch);
			_printSettings.SetMarginLeftAttribute(_conversionOrder.LeftMarginInMillimeters/kMillimetersPerInch);
			_printSettings.SetMarginRightAttribute(_conversionOrder.RightMarginInMillimeters/kMillimetersPerInch);

			_printSettings.SetOrientationAttribute(_conversionOrder.Landscape ? 1 : 0);
//			printSettings.SetHeaderStrCenterAttribute("");
//			printSettings.SetHeaderStrLeftAttribute("");
//			printSettings.SetHeaderStrRightAttribute("");
//			printSettings.SetFooterStrRightAttribute("");
//			printSettings.SetFooterStrLeftAttribute("");
//			printSettings.SetFooterStrCenterAttribute("");

			_printSettings.SetPrintBGColorsAttribute(true);
			_printSettings.SetPrintBGImagesAttribute(true);

			_printSettings.SetShrinkToFitAttribute(false);


			//TODO: doesn't seem to do anything. Probably a problem in the geckofx wrapper
			//printSettings.SetScalingAttribute(_conversionOrder.Zoom);

			_printSettings.SetOutputFormatAttribute(2); // 2 == kOutputFormatPDF

			Status = "Making PDF..";
			if (_conversionOrder.FirstPageToPrint > 0)
			{
				_printSettings.SetPrintRangeAttribute(1);	// print only a range of pages
				if (_conversionOrder.LastPageToPrint < _conversionOrder.FirstPageToPrint)
					_conversionOrder.LastPageToPrint = _conversionOrder.FirstPageToPrint;
				_printSettings.SetStartPageRangeAttribute(_conversionOrder.FirstPageToPrint);
				_printSettings.SetEndPageRangeAttribute(_conversionOrder.LastPageToPrint);
			}
			else if (_conversionOrder.ReduceMemoryUse)
			{
				_printSettings.SetPrintRangeAttribute(1);	// print a range of pages
				_printSettings.SetStartPageRangeAttribute(1);
				_printSettings.SetEndPageRangeAttribute(1);
				_currentPage = 1;
				_currentFile = String.Format("{0}-page{1:000}",_pathToTempPdf, _currentPage);
				_printSettings.SetToFileNameAttribute(new Gecko.nsAString(_currentFile));
				_beginPages = DateTime.Now;
				Status = "Making Page 1 of PDF...";
			}

			_startMakingPdf = DateTime.Now;
			_print.Print(_printSettings, this);
			_checkForPdfFinishedTimer.Enabled = true;
		}

		private void FinishMakingPdf()
		{
			if (_conversionOrder.ReportMemoryUsage)
			{
				Console.WriteLine("Making the PDF took {0}", DateTime.Now - _startMakingPdf);
				MemoryManagement.CheckMemory(false, "Memory use after printing", false);
			}
			if (_conversionOrder.ReduceMemoryUse)
			{
				if (!File.Exists(_currentFile))
				{
					throw new ApplicationException(string.Format(
						"GeckoFxHtmlToPdf was not able to create the PDF file ({0}).{1}{1}Details: Gecko did not produce the expected document.",
						_currentFile, Environment.NewLine));
				}
				// collect all the memory we can between pages
				GC.Collect();
				GC.WaitForPendingFinalizers();
				MemoryService.MinimizeHeap(true);
				//not in Geckofx60: _browser.Window.WindowUtils.GarbageCollect(null /*hopefully nulls ok*/, 0);

				var length = new FileInfo(_currentFile).Length;
				if (IsPrintingFinished(length))
				{
					CombinePageFilesTogether();
					if (_conversionOrder.ReportMemoryUsage)
					{
						MemoryManagement.CheckMemory(false, "Memory use after combining all the pages", false);
						Console.WriteLine("Making all the PDF pages took {0}", DateTime.Now - _beginPages);
					}
					RaiseFinished();
					return;
				}
				++_currentPage;
				_printSettings.SetStartPageRangeAttribute(_currentPage);
				_printSettings.SetEndPageRangeAttribute(_currentPage);
				_currentFile = String.Format("{0}-page{1:000}",_pathToTempPdf, _currentPage);
				_printSettings.SetToFileNameAttribute(new Gecko.nsAString(_currentFile));
				_finished = false;
				_startMakingPdf = DateTime.Now;
				Status = String.Format("Making Page {0} of PDF...", _currentPage);
				RaiseStatusChanged(new PdfMakingStatus() { percentage = 0, statusLabel = Status });
				_print.Print(_printSettings, this);
				_checkForPdfFinishedTimer.Enabled = true;
				return;
			}

			if (!File.Exists(_pathToTempPdf))
				throw new ApplicationException(string.Format(
					"GeckoFxHtmlToPdf was not able to create the PDF file ({0}).{1}{1}Details: Gecko did not produce the expected document.",
					_pathToTempPdf, Environment.NewLine));

			try
			{
				File.Move(_pathToTempPdf, _conversionOrder.OutputPdfPath);
				RaiseFinished();
			}
			catch (IOException e)
			{
				// We can get here for a different reason: the source file is still in use
				throw new ApplicationException(
					string.Format(
						"Tried to move the file {0} to {1}, but the Operating System said that one of these files was locked. Please try again.{2}{2}Details: {3}",
						_pathToTempPdf, _conversionOrder.OutputPdfPath, Environment.NewLine, e.Message));
			}
		}

		private bool IsPrintingFinished(long length)
		{
			// (On Linux?) the end of the book is sometimes marked by a zero sized file being output for the nonexistent page.
			if (length == 0)
			{
				File.Delete(_currentFile);
				return true;
			}
			// Otherwise, nonexistent pages off the end of the book produce very small identical empty page files.
			// (For Geckofx45, these empty page files are 833 bytes on Linux and 843 bytes on Windows.)
			// heuristic: 5 consecutive very small files the same size must mark the end of the book.
			if (length < 850)
			{
				if (length == _prevLength)
				{
					++_prevCount;
					if (_prevCount < 4)
						return false;
					// Delete the empty files at the end.
					for (int i = _currentPage - _prevCount; i <= _currentPage; ++i)
					{
						var file = String.Format("{0}-page{1:000}",_pathToTempPdf, i);
						File.Delete(file);
					}
					return true;
				}
			}
			_prevLength = length;
			_prevCount = 0;
			return false;
		}

		private void CombinePageFilesTogether()
		{
			var filenames = GetPageFilenames();
			PdfDocument outputDocument = new PdfDocument();
			Status = "Combining pages into final PDF file...";
			for (int i = 0; i < filenames.Count; ++i)
			{
				RaiseStatusChanged(new PdfMakingStatus() { percentage = (int)((float)i / (float)filenames.Count), statusLabel = Status });
				var file = filenames[i];
				using (PdfDocument inputDocument = PdfReader.Open(file, PdfDocumentOpenMode.Import))
				{
					System.Diagnostics.Debug.Assert(inputDocument.PageCount == 1);
					PdfPage page = inputDocument.Pages[0];
					outputDocument.AddPage(page);
				}
			}
			outputDocument.Save(_conversionOrder.OutputPdfPath);
			// remove the page files that are no longer needed
			foreach (string file in filenames)
				File.Delete(file);
		}

		private List<string> GetPageFilenames()
		{
			var filenames = new List<string>();
			var nameToMatch =  Path.GetFileName(_pathToTempPdf) + "-page";
			var info = new DirectoryInfo(Path.GetDirectoryName(_pathToTempPdf));
			foreach (var file in info.GetFiles())
			{
				if (file.Name.StartsWith(nameToMatch))
					filenames.Add(file.FullName);
			}
			filenames.Sort();
			return filenames;
		}

		private void OnCheckForBrowserNavigatedTimerTick(object sender, EventArgs e)
		{
			if (_browser.Document != null && _browser.Document.ReadyState == "complete")
			{
				_checkForBrowserNavigatedTimer.Enabled = false;
				StartMakingPdf();
			}
		}

		private void OnCheckForPdfFinishedTimer_Tick(object sender, EventArgs e)
		{
			if (_finished)
			{
				_checkForPdfFinishedTimer.Enabled = false;
				FinishMakingPdf();
			}
		}


		public void OnStateChange(nsIWebProgress aWebProgress, nsIRequest aRequest, uint aStateFlags, int aStatus)
		{
			_finished = (aStateFlags & nsIWebProgressListenerConstants.STATE_STOP) != 0;
		}

		#region nsIWebProgressListener

		public void OnProgressChange(nsIWebProgress webProgress, nsIRequest request, int currentSelfProgress,
		                             int maxSelfProgress,
		                             int currentTotalProgress, int maxTotalProgress)
		{
			if (maxTotalProgress == 0)
				return;

			// if we use the maxTotalProgress, the problem is that it starts off below 100, the jumps to 100 at the end
			// so it looks a lot better to just always scale, to 100, the current progress by the max at that point
			if (maxTotalProgress < currentTotalProgress)
				maxTotalProgress = currentTotalProgress;	// sanity check
			RaiseStatusChanged(new PdfMakingStatus()
				{
					percentage = (int) (100.0*(currentTotalProgress)/maxTotalProgress),
					statusLabel = Status
				});
		}

		public void OnLocationChange(nsIWebProgress aWebProgress, nsIRequest aRequest, nsIURI aLocation, uint aFlags)
		{
		}

		public void OnStatusChange(nsIWebProgress aWebProgress, nsIRequest aRequest, int aStatus, string aMessage)
		{
		}

		public void OnSecurityChange(nsIWebProgress aWebProgress, nsIRequest aRequest, uint aState)
		{
		}

		#endregion
	}

	public class PdfMakingStatus : EventArgs
	{
		public int percentage;
		public string statusLabel;
	}
}

