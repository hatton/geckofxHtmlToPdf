using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Gecko;

namespace GeckofxHtmlToPdf
{
	/// <summary>
	/// The "Program" is used when this is invoked via command line, rather than embedded in a winforms app.
	/// </summary>
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

			if(!conversionOrder.IsHTTP && !File.Exists(conversionOrder.InputPath))
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
	}
}
