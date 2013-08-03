namespace geckofxHtmlToPdf
{
	partial class ConversionProgress
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this._checkForPdfFinishedTimer = new System.Windows.Forms.Timer(this.components);
			this._checkForBrowserNavigatedTimer = new System.Windows.Forms.Timer(this.components);
			this._statusLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// _checkForPdfFinishedTimer
			// 
			this._checkForPdfFinishedTimer.Tick += new System.EventHandler(this.OnCheckForPdfFinishedTimer_Tick);
			// 
			// _checkForBrowserNavigatedTimer
			// 
			this._checkForBrowserNavigatedTimer.Interval = 3000;
			this._checkForBrowserNavigatedTimer.Tick += new System.EventHandler(this.OnCheckForBrowserNavigatedTimerTick);
			// 
			// _statusLabel
			// 
			this._statusLabel.AutoSize = true;
			this._statusLabel.Location = new System.Drawing.Point(36, 41);
			this._statusLabel.Name = "_statusLabel";
			this._statusLabel.Size = new System.Drawing.Size(54, 13);
			this._statusLabel.TabIndex = 0;
			this._statusLabel.Text = "Loading...";
			// 
			// ConversionProgress
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(343, 100);
			this.ControlBox = false;
			this.Controls.Add(this._statusLabel);
			this.Name = "ConversionProgress";
			this.Text = "GeckoFxHtmlToPdf";
			this.Load += new System.EventHandler(this.ConversionProgress_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Timer _checkForPdfFinishedTimer;
		private System.Windows.Forms.Timer _checkForBrowserNavigatedTimer;
		private System.Windows.Forms.Label _statusLabel;
	}
}