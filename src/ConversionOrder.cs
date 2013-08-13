using System;
using CommandLine;

namespace GeckofxHtmlToPdf
{
	/// <summary>
	/// a goal here is to follow the wkhtmltopdf parameters where it makes sense, to ease people  switching
	/// to/from wkhtmltopdf:
	/// http://madalgo.au.dk/~jakobt/wkhtmltoxdoc/wkhtmltopdf-0.9.9-doc.html
	/// 
	/// Properties without the [Option] declarations aren't accessible directly, but are more convenient
	/// when the order is being constructed from code instead of commandline arguments.
	/// </summary>
	public class ConversionOrder
	{
		//		[Option(DefaultValue = true, Required = true, HelpText = "Path to input html")]
		[CommandLine.ValueOption(0)]
		public string InputPath { get; set; }

		//		[Option(DefaultValue = true, Required = true, HelpText = "Path to output pdf")]
		[CommandLine.ValueOption(1)]
		public string OutputPath { get; set; }

		[Option("graphite", DefaultValue = false, HelpText = "Enable SIL Graphite smart font rendering")]
		public bool EnableGraphite { get; set; }

		[Option('O', "orientation", HelpText = "Set orientation to Landscape or Portrait (default Portrait)")]
		public string Orientation { get; set; }

		public bool Landscape
		{
			get { return Orientation!=null && Orientation.ToLower() == "landscape"; }
			set { Orientation = value ? "landscape" : "portrait"; }
		}

		[Option('T', "margin-top", DefaultValue="10", HelpText="Set the page bottom margin")]
		public string TopMargin { get; set; }

		[Option('B', "margin-bottom", DefaultValue="10", HelpText="Set the page bottom margin")]
		public string BottomMargin { get; set; }

		[Option('L', "margin-left", DefaultValue = "10", HelpText = "Set the page left margin")]
		public string LeftMargin { get; set; }

		[Option('R', "margin-right", DefaultValue = "10", HelpText = "Set the page right margin")]
		public string RightMargin { get; set; }

		private double GetMillimeters(string distance)
		{
			//TODO: convert to mm. For now, just strips "mm"
			return double.Parse(distance.Replace("mm", ""));
		}
		public double TopMarginInMillimeters
		{
			get { return GetMillimeters(TopMargin); }
			set { TopMargin = value.ToString(); }
		}
		public double BottomMarginInMillimeters
		{
			get { return GetMillimeters(BottomMargin); }
			set { BottomMargin = value.ToString(); }
		}


		public double LeftMarginInMillimeters
		{
			get { return GetMillimeters(LeftMargin); }
			set { LeftMargin = value.ToString(); }
		}
		public double RightMarginInMillimeters
		{
			get { return GetMillimeters(RightMargin); }
			set { RightMargin = value.ToString(); }
		}

		[Option("zoom", DefaultValue=1.0, HelpText = "Zoom/scaling factor (default 1.0)")]
		public double Zoom { get; set; }

		[Option('s',"page-size", DefaultValue = "A4", HelpText = "Set paper size to: A4, Letter, etc.  (default A4)",
			//LongName = "page-size", 
			MutuallyExclusiveSet="PageSize")]
		public string PageSizeName { get; set; }

		[Option("page-height", HelpText ="Page Height (TODO units?)", MutuallyExclusiveSet="PageSize")]
		public string PageHeight { get; set; }

		[Option("page-width", HelpText = "Page Width (TODO units?)")]
		public string PageWidth { get; set; }

		public double PageHeightInMillimeters
		{
			get
			{
				if (string.IsNullOrWhiteSpace(PageHeight))
				{
					return 0;
				}
				else
				{
					return GetMillimeters(PageHeight);
				}	
			}//todo: units?
			set { PageHeight = value.ToString();}//todo: units?
		}
		public double PageWidthInMillimeters
		{
			get
			{
				if (string.IsNullOrWhiteSpace(PageWidth))
				{
					return 0;
				}
				else
				{
					return GetMillimeters(PageWidth);
				}
			} //todo: units?
			set { PageWidth = value.ToString(); }//todo: units?
		}

		[Option('q', "quiet", DefaultValue = false, HelpText = "Don't show the progress dialog")]
		public bool NoUIMode { get; set; }

		public bool IsHTTP
		{
			get { return InputPath.ToLower().StartsWith("http"); }
		}

		[Option("debug", DefaultValue = false, HelpText = "Send debugging information to the console.")]
		public bool Debug { get; set; }
	}
}