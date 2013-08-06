using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Gecko;

namespace geckofxHtmlToPdf
{
	internal class Program
	{
		private static int _returnCode;



		[System.Runtime.InteropServices.DllImportAttribute("kernel32.dll", EntryPoint = "AttachConsole")]
		static extern bool AttachConsole(int dwProcessId);

		private static int Main(string[] args)
		{
			//allows us to output to the console even though we are a winforms app (which we are because geckofx needs the event pump)
			AttachConsole(-1);
			
			var conversionOrder = new ConversionOrder();
			
			if (!CommandLine.Parser.Default.ParseArguments(args, conversionOrder))
			{
				Console.Error.WriteLine(
					"GeckofxHtmlToPDF had a problem with the command line arguments (sorry, can't do better than that yet)");

				return 1;
			}
			if(!File.Exists(conversionOrder.InputPath))
			{
				Console.Error.WriteLine(
					"GeckofxHtmlToPDF could not locate the input file: " + conversionOrder.InputPath);
				Console.WriteLine(
					"GeckofxHtmlToPDF could not locate the input file: "+conversionOrder.InputPath);

				return 1;
			}
			
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

			//for developers, this will find xulrunner in a directory called "distfiles"
			//when installed, it will find it in the same directory as the exe.
			string xulrunnerPath = GetDirectoryDistributedWithApplication(false,"xulrunner");
			Gecko.Xpcom.Initialize(xulrunnerPath);

			if (conversionOrder.EnableGraphite)
				GeckoPreferences.User["gfx.font_rendering.graphite.enabled"] = true;

			//without this, we get invisible (white?) text on some machines
			Gecko.GeckoPreferences.User["gfx.direct2d.disabled"] = true;

			//Browser requires an Application event loop. This could eventually be a progress window or be invisible
			Application.Run(new ConversionProgress(conversionOrder));
			return _returnCode;
		}

		private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
		{
			Console.Error.WriteLine(e.Exception.Message);
			_returnCode = 1;
			Application.Exit();
		}

		private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (e.ExceptionObject is Exception)
                Console.Error.WriteLine(((Exception) e.ExceptionObject).Message);
            else
                Console.Error.WriteLine("geckohtmltopdf got unknown exception");
			_returnCode = 1;
			Application.Exit();
			
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
}
