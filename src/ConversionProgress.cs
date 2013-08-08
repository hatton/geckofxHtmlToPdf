using System;
using System.Windows.Forms;

namespace geckofxHtmlToPdf
{
	/// <summary>
	/// This class is used when the exe is called from the command line. It is invisibe
	/// if the --quiet parameter is used, otherwise it gives a little progress dialog.
	/// </summary>
	public partial class ConversionProgress : Form
	{
		private readonly ConversionOrder _conversionOrder;

		public ConversionProgress(ConversionOrder conversionOrder)
		{
			_conversionOrder = conversionOrder;
			InitializeComponent();
			_progressBar.Maximum = 100;
			if (conversionOrder.NoUIMode)
			{
				this.WindowState = FormWindowState.Minimized;
				this.ShowInTaskbar = false;
			}
		}

		private void ConversionProgress_Load(object sender, EventArgs e)
		{
			_pdfMaker.Finished += (o, args) => Close();
			_pdfMaker.Start(_conversionOrder);
		}

		private void OnPdfMakerStatusChanged(object sender, PdfMakingStatus pdfMakingStatus)
		{
			_statusLabel.Text = pdfMakingStatus.statusLabel;
			_progressBar.Value = pdfMakingStatus.percentage;
		}
	}
}
