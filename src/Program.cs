using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Args;
using Args.Help.Formatters;
using System.Linq;

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

			//using https://github.com/hatton/args. This fork includes the defaults in the message that gets printed.

			var argsDefinition = Args.Configuration.Configure<ConversionOrder>();
			argsDefinition.SwitchDelimiter = "-";
			argsDefinition.CommandModelDescription = "GeckofxHtmlToPdf";

			if (args.Length < 3 || (args.Length > 1 && (new string[] {"-h", "/h","?", "/?", "-?", "help"}).Contains(args[1].ToLower())))
			{
				ShowHelp(argsDefinition);
				return 0;
			}

			ConversionOrder conversionOrder;
			try
			{
				conversionOrder = argsDefinition.CreateAndBind(args);
			}
			catch (Exception)
			{
				ShowHelp(argsDefinition);
				return 1;
			}
			

			if(!conversionOrder.IsHTTP && !File.Exists(conversionOrder.InputHtmlPath))
			{
				Console.Error.WriteLine(
					"GeckofxHtmlToPDF could not locate the input file: " + conversionOrder.InputHtmlPath);
				Console.WriteLine(
					"GeckofxHtmlToPDF could not locate the input file: "+conversionOrder.InputHtmlPath);

				return 2;
			}
			
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
			AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;

			//Browser requires an Application event loop. This could eventually be a progress window or be invisible
			Application.Run(new ConversionProgress(conversionOrder));
			return _returnCode;
		}

		private static void ShowHelp(IModelBindingDefinition<ConversionOrder> argsDefinition)
		{
			var help = new Args.Help.HelpProvider().GenerateModelHelp(argsDefinition);
			var f = new ConsoleHelpFormatter(80, 1, 5);
			Console.WriteLine(f.GetHelp(help));
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
