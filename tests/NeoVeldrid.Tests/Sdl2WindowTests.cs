// Creating a window needs a desktop session, which only the GPU test environments have.
#if TEST_VULKAN || TEST_D3D11 || TEST_OPENGL
using System;
using System.Threading;
using System.Threading.Tasks;
using NeoVeldrid.Sdl2;
using Silk.NET.SDL;
using Xunit;

namespace NeoVeldrid.Tests;

public class Sdl2WindowTests
{
    // Regression test for a bug where a threaded window destroyed itself on its event thread as
    // soon as SDL asked it to close (e.g. its close button), while the main thread could still be
    // rendering to it. On Vulkan, the next present then failed with "The Swapchain's underlying
    // surface has been lost".
    [Fact]
    public void ThreadedWindow_CloseRequest_DestroysWindowOnlyDuringPumpEvents()
    {
        Sdl2Window window = CreateThreadedWindow();
        try
        {
            PushCloseEvent(window);

            bool destroyedWithoutPump = SpinWait.SpinUntil(() => !window.Exists, TimeSpan.FromMilliseconds(500));
            Assert.False(destroyedWithoutPump);

            bool destroyedByPump = SpinWait.SpinUntil(() =>
            {
                window.PumpEvents();
                return !window.Exists;
            }, TimeSpan.FromSeconds(5));
            Assert.True(destroyedByPump);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void ThreadedWindow_Close_DestroysWindowBeforeReturning()
    {
        Sdl2Window window = CreateThreadedWindow();

        window.Close();

        Assert.False(window.Exists);
    }

    [Fact]
    public async Task ThreadedWindow_Close_ReturnsWhenAHandlerThrows()
    {
        Sdl2Window window = CreateThreadedWindow();
        window.Closing += () => throw new InvalidOperationException("Closing handler failed.");

        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Task close = Task.Run(window.Close, cancellationToken);

        await close.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
    }

    private static Sdl2Window CreateThreadedWindow()
    {
        // macOS only allows windows on the process main thread.
        Assert.SkipWhen(OperatingSystem.IsMacOS(), "Threaded windows are not supported on macOS.");

        return new Sdl2Window("Sdl2WindowTests", 0, 0, 200, 200, SDL_WindowFlags.Hidden, threadedProcessing: true);
    }

    private static unsafe void PushCloseEvent(Sdl2Window window)
    {
        Sdl sdl = Sdl2Window.SdlInstance;
        Event ev = default;
        ev.Window = new WindowEvent
        {
            Type = (uint)EventType.Windowevent,
            WindowID = sdl.GetWindowID((Silk.NET.SDL.Window*)window.SdlWindowHandle),
            Event = (byte)WindowEventID.Close,
        };
        Assert.Equal(1, sdl.PushEvent(&ev));
    }
}
#endif
