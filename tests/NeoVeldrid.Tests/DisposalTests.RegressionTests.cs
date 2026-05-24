using System;
using System.Threading.Tasks;
using NeoVeldrid.Sdl2;
using Xunit;

namespace NeoVeldrid.Tests
{
    // Regression tests for disposal bugs that have been fixed in NeoVeldrid. Each test guards a
    // specific past bug and is not part of the general disposal coverage in DisposalTests.cs.
    //
    // Unlike DisposalTestBase<T>, these tests do not use the shared fixture device, because they
    // need to dispose the GraphicsDevice itself: the fixture teardown calls WaitForIdle() before
    // its own Dispose(), which would hit the same hang the test below exercises. Each test creates
    // and owns a throwaway device instead.
    public abstract class DeviceDisposalRegressionTests<T> where T : GraphicsDeviceCreator
    {
        // GraphicsDevice.Dispose() used to re-run PlatformDispose() on every call. On the OpenGL
        // backend the second call posted a FlushAndFinish work item to the execution thread, but
        // that thread had already terminated during the first dispose, so the post blocked forever.
        // IDisposable requires Dispose to be safe to call more than once; this verifies the second
        // call returns promptly instead of deadlocking. The timeout turns a regression into a test
        // failure rather than a hung run. It also checks IsDisposed tracks the lifecycle: false
        // while the device is live, true once disposed.
        [Fact(Timeout = 10000)]
        public Task Dispose_Twice_DoesNotHang()
        {
            // The work runs on a separate thread so the timeout can fire even if the second
            // Dispose() blocks: a synchronous deadlock would freeze the very thread the test
            // runner waits on, leaving nothing to time out.
            return Task.Run(() =>
            {
                Activator.CreateInstance<T>().CreateGraphicsDevice(out Sdl2Window window, out GraphicsDevice gd);
                Assert.False(gd.IsDisposed);
                gd.Dispose();
                gd.Dispose();
                Assert.True(gd.IsDisposed);
                window?.Close();
            });
        }
    }

#if TEST_VULKAN
    [Trait("Backend", "Vulkan")]
    public class VulkanDeviceDisposalRegressionTests : DeviceDisposalRegressionTests<VulkanDeviceCreator> { }
#endif
#if TEST_D3D11
    [Trait("Backend", "D3D11")]
    public class D3D11DeviceDisposalRegressionTests : DeviceDisposalRegressionTests<D3D11DeviceCreator> { }
#endif
#if TEST_OPENGL
    [Trait("Backend", "OpenGL")]
    public class OpenGLDeviceDisposalRegressionTests : DeviceDisposalRegressionTests<OpenGLDeviceCreator> { }
#endif
#if TEST_OPENGLES
    [Trait("Backend", "OpenGLES")]
    public class OpenGLESDeviceDisposalRegressionTests : DeviceDisposalRegressionTests<OpenGLESDeviceCreator> { }
#endif
}
