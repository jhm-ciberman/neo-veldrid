using System;
using System.Collections.Generic;
using System.Threading;
using NeoVeldrid.Tests;

class Program
{
    static int Main(string[] args)
    {
        List<string> newArgs = new List<string>(args);
        newArgs.Insert(0, typeof(Program).Assembly.Location);

        // xUnit runs tests on pool threads, but we want to start our window in the main thread.
        int returnCode = 0;
        using CancellationTokenSource cts = new CancellationTokenSource();
        Thread runner = new Thread(() =>
        {
            try
            {
                returnCode = Xunit.ConsoleClient.Program.Main(newArgs.ToArray());
            }
            finally
            {
                cts.Cancel();
            }
        });

        runner.Start();
        MainThread.Pump(cts.Token);
        runner.Join();

        Console.WriteLine("Tests finished. Press any key to exit.");
        if (!Console.IsInputRedirected)
        {
            Console.ReadKey(true);
        }
        return returnCode;
    }
}
