using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Gecko;

namespace geckofxHtmlToPdf
{
	public partial class ConversionProgress : Form
	{
		private readonly ConversionOrder _conversionOrder;
		private GeckoWebBrowser _browser;
		private string _pathToTempPdf;
		Listener _progressListener;

		public ConversionProgress(ConversionOrder conversionOrder)
		{
			_conversionOrder = conversionOrder;
			InitializeComponent();
		}

		private void ConversionProgress_Load(object sender, EventArgs e)
		{
			_browser = new GeckoWebBrowser();
			_browser.Navigating += _browser_Navigating;
			_browser.ConsoleMessage += _browser_ConsoleMessage;
			_pathToTempPdf = Path.GetTempFileName() + ".pdf";//<-- TODO this makes an extra file
			File.Delete(_conversionOrder.OutputPath);
			//File.Delete(_pathToTempPdf);
			_checkForBrowserNavigatedTimer.Enabled = true;
			_statusLabel.Text = "Loading Html...";
			_browser.Navigate(_conversionOrder.InputPath);
		}

		void _browser_ConsoleMessage(object sender, ConsoleMessageEventArgs e)
		{
		Debug.WriteLine(e.Message );
		}

		void _browser_Navigating(object sender, Gecko.Events.GeckoNavigatingEventArgs e)
		{
			
		}


		private void StartMakingPdf()
		{
			nsIWebBrowserPrint print = Xpcom.QueryInterface<nsIWebBrowserPrint>(_browser.Window.DomWindow);

			var service = Xpcom.GetService<nsIPrintSettingsService>("@mozilla.org/gfx/printsettings-service;1");
			var printSettings = service.GetNewPrintSettingsAttribute();

			printSettings.SetToFileNameAttribute(_pathToTempPdf);
			printSettings.SetPrintSilentAttribute(true);//don't show a printer settings dialog
			printSettings.SetShowPrintProgressAttribute(!_conversionOrder.ShowProgressDialog);
			
			if (_conversionOrder.PageHeightInMillimeters > 0)
			{
				printSettings.SetPaperHeightAttribute(_conversionOrder.PageHeightInMillimeters);
				printSettings.SetPaperWidthAttribute(_conversionOrder.PageWidthInMillimeters);
				printSettings.SetPaperSizeUnitAttribute(1);//0=in, >0 = mm
			}
			else
			{
				printSettings.SetPaperSizeUnitAttribute(1);
				//doesn't actually work (problem in geckofx?) printSettings.SetPaperNameAttribute(_conversionOrder.PageSizeName);
				if (_conversionOrder.PageSizeName.ToLower() == "a4")
				{
					printSettings.SetPaperWidthAttribute(210);
					printSettings.SetPaperHeightAttribute(297);
				}
				else if (_conversionOrder.PageSizeName.ToLower() == "a5")
				{
					printSettings.SetPaperWidthAttribute(148); 
					printSettings.SetPaperHeightAttribute(210);
				}
				else if (_conversionOrder.PageSizeName.ToLower() == "a3")
				{
					printSettings.SetPaperWidthAttribute(297);
					printSettings.SetPaperHeightAttribute(420);
				}
				else if (_conversionOrder.PageSizeName.ToLower() == "b5")
				{
					printSettings.SetPaperWidthAttribute(176);
					printSettings.SetPaperHeightAttribute(250);
				}
				else if (_conversionOrder.PageSizeName.ToLower() == "letter")
				{
					printSettings.SetPaperWidthAttribute(215.9 );
					printSettings.SetPaperHeightAttribute(279.4);
				}
				else if (_conversionOrder.PageSizeName.ToLower() == "legal")
				{
					printSettings.SetPaperWidthAttribute(215.9);
					printSettings.SetPaperHeightAttribute(355.6);
				}
				else throw new ApplicationException("Sorry, currently GeckofxHtmlToPDF has a very limited set of paper sizes it knows about. Consider using the page-height and page-width arguments instead");
			}
			//this seems to be in inches, and doesn't have a unit-setter (unlike the paper size ones)
			const double kMillimetersPerInch = 25; //TODO what is it, exactly?
			printSettings.SetMarginTopAttribute(_conversionOrder.TopMarginInMillimeters/kMillimetersPerInch);
			printSettings.SetMarginBottomAttribute(_conversionOrder.BottomMarginInMillimeters / kMillimetersPerInch);
			printSettings.SetMarginLeftAttribute(_conversionOrder.LeftMarginInMillimeters / kMillimetersPerInch);
			printSettings.SetMarginRightAttribute(_conversionOrder.RightMarginInMillimeters / kMillimetersPerInch);
			

			printSettings.SetDownloadFontsAttribute(true);//review: what's this for?
			printSettings.SetOrientationAttribute(_conversionOrder.Landscape ? 1 : 0);
			printSettings.SetHeaderStrCenterAttribute("");
			printSettings.SetHeaderStrLeftAttribute("");
			printSettings.SetHeaderStrRightAttribute("");
			printSettings.SetFooterStrRightAttribute("");
			printSettings.SetFooterStrLeftAttribute("");
			printSettings.SetFooterStrCenterAttribute("");

			//TODO: doesn't see to do anything
			printSettings.SetScalingAttribute(_conversionOrder.Zoom);
			;
			printSettings.SetOutputFormatAttribute(2); // 2 == kOutputFormatPDF

			_statusLabel.Text = "Making PDF..";

			//TODO: How do you use the progress parameter here to know when it is done?
			_progressListener = new Listener();
			print.Print(printSettings, _progressListener);
			_checkForPdfFinishedTimer.Enabled = true;
		}

		private class Listener : nsIWebProgressListener
		{
			public bool Done;
			public void OnStateChange(nsIWebProgress aWebProgress, nsIRequest aRequest, uint aStateFlags, int aStatus)
			{
				//was always 0 
				//Done = aStatus == (decimal) nsIWebProgressListenerConstants.STATE_STOP;
			}

			public void OnProgressChange(nsIWebProgress aWebProgress, nsIRequest aRequest, int aCurSelfProgress, int aMaxSelfProgress,
			                             int aCurTotalProgress, int aMaxTotalProgress)
			{
				Done = aCurTotalProgress == aMaxTotalProgress;
			}

			public void OnLocationChange(nsIWebProgress aWebProgress, nsIRequest aRequest, nsIURI aLocation, uint aFlags)
			{
			}

			public void OnStatusChange(nsIWebProgress aWebProgress, nsIRequest aRequest, int aStatus, string aMessage)
			{
				//was never called Done = aStatus == (decimal)nsIWebProgressListenerConstants.STATE_STOP;
			}

			public void OnSecurityChange(nsIWebProgress aWebProgress, nsIRequest aRequest, uint aState)
			{
			}
		}


		private void FinishMakingPdf()
		{
			if (!File.Exists(_pathToTempPdf))
				throw new ApplicationException("GeckoFxHtmlToPdf was not able to create the PDF.\r\n\r\nDetails: Gecko did not produce the expected document.");

			try
			{
				File.Move(_pathToTempPdf, _conversionOrder.OutputPath);
				Close();
			}
			catch (IOException e)
			{
				//TODO: we can get here for a different reason: the source file is still in use
				throw new ApplicationException(
						string.Format("Tried to save the file to {0}, but the Operating System said that it was locked. Please try again.\r\n\r\nDetails: {1}",
									  _conversionOrder.OutputPath, e.Message));

			}
			
		}

		private void OnCheckForBrowserNavigatedTimerTick(object sender, EventArgs e)
		{
			if (_browser.Document != null && _browser.Document.ActiveElement != null)
			{
				_checkForBrowserNavigatedTimer.Enabled = false;
				StartMakingPdf();
			}
		}

		private void OnCheckForPdfFinishedTimer_Tick(object sender, EventArgs e)
		{
			//TODO: use progress thingy to know when this is done, rather than just giving it 4 seconds.
			//if (File.GetLastWriteTime(_pathToTempPdf).AddSeconds(4) < DateTime.Now)
			if(_progressListener.Done)
			{
				_checkForPdfFinishedTimer.Enabled = false;
				FinishMakingPdf();
			}
		}
	}
}
