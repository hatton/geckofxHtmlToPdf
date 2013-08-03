using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CommandLine;
using Gecko;

namespace geckofxHtmlToPdf
{
	class Program
	{
		private static void Main(string[] args)
		{
			var conversionOrder = new ConversionOrder();

			CommandLine.Parser.Default.ParseArguments(args, conversionOrder);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			string xulrunnerPath = GetDirectoryDistributedWithApplication(false,"xulrunner");
			Gecko.Xpcom.Initialize(xulrunnerPath);

			if (conversionOrder.EnableGraphite)
				GeckoPreferences.User["gfx.font_rendering.graphite.enabled"] = true;

			//without this, we get invisible (white?) text on some machines
			Gecko.GeckoPreferences.User["gfx.direct2d.disabled"] = true;

			//Browser requires an Application event loop. This could eventually be a progress window or be invisible
			Application.Run(new ConversionProgress(conversionOrder));


		}

		/// <summary>
		/// Find a file which, on a development machine, lives in [solution]/DistFiles/[subPath],
		/// and when installed, lives in 
		/// [applicationFolder]/[subPath1]/[subPathN]
		/// </summary>
		/// <example>GetFileDistributedWithApplication("info", "releaseNotes.htm");</example>
		public static string GetDirectoryDistributedWithApplication(bool optional, params string[] partsOfTheSubPath)
		{
			var path = DirectoryOfApplicationOrSolution;
			foreach (var part in partsOfTheSubPath)
			{
				path = System.IO.Path.Combine(path, part);
			}
			if (Directory.Exists(path))
				return path;

			//try distfiles
			path = DirectoryOfApplicationOrSolution;
			path = Path.Combine(path, "distFiles");
			foreach (var part in partsOfTheSubPath)
			{
				path = System.IO.Path.Combine(path, part);
			}
			if (Directory.Exists(path))
				return path;

			//try src (e.g. Bloom keeps its javascript under source directory (and in distfiles only when installed)
			path = DirectoryOfApplicationOrSolution;
			path = Path.Combine(path, "src");
			foreach (var part in partsOfTheSubPath)
			{
				path = System.IO.Path.Combine(path, part);
			}

			if (optional && !Directory.Exists(path))
				return null;

			if(!Directory.Exists(path))
				throw new ApplicationException("Could not locate "+path);
			return path;
		}

		/// <summary>
		/// Gives the directory of either the project folder (if running from visual studio), or
		/// the installation folder.  Helpful for finding templates and things; by using this,
		/// you don't have to copy those files into the build directory during development.
		/// It assumes your build directory has "output" as part of its path.
		/// </summary>
		/// <returns></returns>
		public static string DirectoryOfApplicationOrSolution
		{
			get
			{
				string path = DirectoryOfTheApplicationExecutable;
				char sep = Path.DirectorySeparatorChar;
				int i = path.ToLower().LastIndexOf(sep + "output" + sep);

				if (i > -1)
				{
					path = path.Substring(0, i + 1);
				}
				return path;
			}
		}

		public static string DirectoryOfTheApplicationExecutable
		{
			get
			{
				string path;
				bool unitTesting = Assembly.GetEntryAssembly() == null;
				if (unitTesting)
				{
					path = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
					path = Uri.UnescapeDataString(path);
				}
				else
				{
					var assembly = Assembly.GetEntryAssembly();
					path = assembly.Location;
				}
				return Directory.GetParent(path).FullName;
			}
		}
	}

	/// <summary>
	/// a goal here is to follow the wkhtmltopdf parameters where it makes sense, to ease people trying this out:
	/// http://madalgo.au.dk/~jakobt/wkhtmltoxdoc/wkhtmltopdf-0.9.9-doc.html
	/// </summary>
	public class ConversionOrder
	{
//		[Option(DefaultValue = true, Required = true, HelpText = "Path to input html")]
		[CommandLine.ValueOption(0)]
		public string InputPath { get; set; }
//		[Option(DefaultValue = true, Required = true, HelpText = "Path to output pdf")]
		[CommandLine.ValueOption(1)]
		public string OutputPath { get; set; }

		[Option("graphite",DefaultValue = false, HelpText = "Enable SIL Graphite smart font rendering")]
		public bool EnableGraphite { get; set; }
	}

}
