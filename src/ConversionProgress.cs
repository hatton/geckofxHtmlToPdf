using System;
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

		public ConversionProgress(ConversionOrder conversionOrder)
		{
			_conversionOrder = conversionOrder;
			InitializeComponent();
		}

		private void ConversionProgress_Load(object sender, EventArgs e)
		{
			_browser = new GeckoWebBrowser();

			_pathToTempPdf = Path.GetTempFileName() + ".pdf";//<-- TODO this makes an extra file
			File.Delete(_conversionOrder.OutputPath);
			//File.Delete(_pathToTempPdf);
			_checkForBrowserNavigatedTimer.Enabled = true;
			_statusLabel.Text = "Loading Html...";
			_browser.Navigate(_conversionOrder.InputPath);
		}


		private void StartMakingPdf()
		{
			nsIWebBrowserPrint print = Xpcom.QueryInterface<nsIWebBrowserPrint>(_browser.Window.DomWindow);

			var service = Xpcom.GetService<nsIPrintSettingsService>("@mozilla.org/gfx/printsettings-service;1");
			var printSettings = service.GetNewPrintSettingsAttribute();

			printSettings.SetToFileNameAttribute(_pathToTempPdf);
			printSettings.SetPrintSilentAttribute(true);
			printSettings.SetPaperNameAttribute("a4");
			printSettings.SetDownloadFontsAttribute(true);
			printSettings.SetOutputFormatAttribute(2); // 2 == kOutputFormatPDF

			_statusLabel.Text = "Making PDF..";

			//TODO: How do you use the progress parameter here to know when it is done?
			print.Print(printSettings, null);
			_checkForPdfFinishedTimer.Enabled = true;
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
			if (File.GetLastWriteTime(_pathToTempPdf).AddSeconds(4) < DateTime.Now)
			{
				_checkForPdfFinishedTimer.Enabled = false;
				FinishMakingPdf();
			}
		}
	}
}
