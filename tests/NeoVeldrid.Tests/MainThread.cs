using System;
using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace NeoVeldrid.Tests;

/// <summary>
/// Runs work on the thread that calls <see cref="Pump"/>.
/// </summary>
/// <remarks>
/// macOS requires SDL windows to be created and destroyed on the process main thread,
/// and xUnit runs tests on pool threads.
/// </remarks>
public static class MainThread
{
    private static readonly BlockingCollection<Action> _work = new BlockingCollection<Action>();

    private static Thread _pumpThread;

    /// <summary>
    /// Runs queued work on the calling thread until <paramref name="until"/> is cancelled.
    /// </summary>
    /// <param name="until">The token that stops the pump when cancelled.</param>
    public static void Pump(CancellationToken until)
    {
        _pumpThread = Thread.CurrentThread;
        try
        {
            foreach (Action work in _work.GetConsumingEnumerable(until))
            {
                work();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _pumpThread = null;
        }
    }

    /// <summary>
    /// Runs <paramref name="func"/> on the pumping thread and returns its result. Exceptions are
    /// rethrown on the calling thread with their original stack trace.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="func">The work to run.</param>
    /// <returns>The result of <paramref name="func"/>.</returns>
    public static T Invoke<T>(Func<T> func)
    {
        Thread pump = _pumpThread;
        if (pump == null || pump == Thread.CurrentThread)
        {
            return func();
        }

        T result = default;
        Exception error = null;

        using (ManualResetEventSlim done = new ManualResetEventSlim(false))
        {
            _work.Add(() =>
            {
                try
                {
                    result = func();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    done.Set();
                }
            });

            done.Wait();
        }

        if (error != null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }

        return result;
    }

    /// <summary>
    /// Runs <paramref name="action"/> on the pumping thread.
    /// </summary>
    /// <param name="action">The work to run.</param>
    public static void Invoke(Action action)
    {
        Invoke<object>(() =>
        {
            action();
            return null;
        });
    }
}
