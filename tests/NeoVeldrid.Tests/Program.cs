using System;
using System.Linq;
using System.Threading;
using NeoVeldrid.Tests;
using Xunit.MicrosoftTestingPlatform;
using Xunit.Runner.InProc.SystemConsole;

class Program
{
    static int Main(string[] args)
    {
        // xUnit runs tests on pool threads, but we want to start our window in the main thread.
        int returnCode = 0;
        using CancellationTokenSource cts = new();
        Thread runner = new(() =>
        {
            try
            {
                returnCode = RunTests(args);
            }
            finally
            {
                cts.Cancel();
            }
        });

        runner.Start();
        MainThread.Pump(cts.Token);
        runner.Join();

        if (args.Length == 0 && !Console.IsInputRedirected)
        {
            Console.WriteLine("Tests finished. Press any key to exit.");
            Console.ReadKey(true);
        }
        return returnCode;
    }

    private static int RunTests(string[] args)
    {
        if (args.Any(arg => arg == "-automated" || arg == "@@"))
        {
            return ConsoleRunner.Run(args).GetAwaiter().GetResult();
        }

        return TestPlatformTestFramework
            .RunAsync(args, SelfRegisteredExtensions.AddSelfRegisteredExtensions)
            .GetAwaiter()
            .GetResult();
    }
}
