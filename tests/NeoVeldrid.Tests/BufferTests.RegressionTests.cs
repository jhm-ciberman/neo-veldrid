using System;
using System.Threading;
using Xunit;

namespace NeoVeldrid.Tests;

// Regression tests for specific bugs that have been fixed in NeoVeldrid. Each test in this
// file exists to prevent a particular past bug from coming back. The test name and a comment
// above each test describe the bug it guards against.
//
// This is a partial of BufferTestBase<T>, so every per-backend subclass at the bottom of
// BufferTests.cs runs these tests.
public abstract partial class BufferTestBase<T> where T : GraphicsDeviceCreator
{
    // Regression test for https://github.com/jhm-ciberman/neo-veldrid/issues/105. On Vulkan,
    // UpdateBuffer submitted the copy before it registered the staging buffer. When another
    // thread completed the fence in that gap, the entry was never removed, and a later upload
    // with the same command buffer threw "An item with the same key has already been added".
    [Fact]
    public void UpdateBuffer_FromManyThreads_Succeeds()
    {
        const int uploadsPerThread = 3000;
        int threadCount = Math.Max(4, Environment.ProcessorCount);
        Exception error = null;
        byte[] data = new byte[64];

        Thread[] threads = new Thread[threadCount];
        for (int i = 0; i < threadCount; i++)
        {
            DeviceBuffer buffer = RF.CreateBuffer(new BufferDescription(64, BufferUsage.VertexBuffer));
            threads[i] = new Thread(() =>
            {
                try
                {
                    for (int j = 0; j < uploadsPerThread && error == null; j++)
                    {
                        GD.UpdateBuffer(buffer, 0, data);
                    }
                }
                catch (Exception e)
                {
                    Interlocked.CompareExchange(ref error, e, null);
                }
            });
            threads[i].Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        GD.WaitForIdle();
        Assert.Null(error);
    }
}
