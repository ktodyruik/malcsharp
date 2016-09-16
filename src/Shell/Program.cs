using System;
using System.IO;
using System.Threading;
using ColoredConsole;
using Microsoft.Scripting.Hosting.Shell;

namespace Shell
{
    class Program
    {
        static void Main(string[] args)
        {
            CommandLine commandLine = new CommandLine();
            IConsole console = new SuperConsole(commandLine, true);
            Interpreter interpreter = Interpreter.Create();
            bool keepRunning = true;

            GraceFullCtrlC();

            try
            {
                if (File.Exists("prelude.mal"))
                {
                    ColorConsole.WriteLine("Loading 'prelude.mal'...".Magenta());
                    interpreter.LoadFile("prelude.mal");
                }
            }
            catch (Exception e)
            {
                ColorConsole.WriteLine(("Error: " + e.Message).Red());
                ColorConsole.WriteLine(("Stacktrace: " + e.InnerException.StackTrace).DarkRed()); ;
            }

            while (keepRunning)
            {
                try
                {
                    console.WriteLine();
                    console.Write("> ", Style.Out);
                    string line = console.ReadLine(0);
                    string result = interpreter.Eval(line);
                    ColorConsole.WriteLine(result.Cyan());
                }
                catch (ExitException)
                {
                    break;
                }
                catch (Exception e)
                {
                    ColorConsole.WriteLine(("Error: " + e.Message).Red());
                    ColorConsole.WriteLine(("Error: " + e.GetBaseException().Message).Red());
                    ColorConsole.WriteLine(("Stacktrace: " + e.GetBaseException().StackTrace).DarkRed()); ;
                }
            }

            ColorConsole.WriteLine("Exiting".Yellow());
        }


        // http://www.codeproject.com/Articles/16164/Managed-Application-Shutdown
        static void GraceFullCtrlC()
        {
            Console.CancelKeyPress += delegate (object sender,
                                    ConsoleCancelEventArgs e)
            {
                if (e.SpecialKey == ConsoleSpecialKey.ControlBreak)
                {
                    ColorConsole.WriteLine("Exiting...".Yellow());
                    // Environment.Exit(1) would NOT do 
                    // a cooperative shutdown. No finalizers are called!
                    Thread t = new Thread(delegate ()
                    {
                        //ColorConsole.WriteLine("Asynchronous shutdown started".Yellow());
                        Environment.Exit(1);
                    });

                    t.Start();
                    t.Join();
                }
                if (e.SpecialKey == ConsoleSpecialKey.ControlC)
                {
                    e.Cancel = true; // tell the CLR to keep running
                    ColorConsole.WriteLine("Exiting...".Yellow());
                    // If we want to call exit triggered from
                    // out event handler we have to spin
                    // up another thread. If somebody of the
                    // CLR team reads this. Please fix!
                    new Thread(delegate ()
                    {
                        //ColorConsole.WriteLine("Asynchronous shutdown started".Yellow());
                        Environment.Exit(2);
                    }).Start();
                }
            };
        }
    }
}