using NeoVeldrid.Sdl2;
using NeoVeldrid.StartupUtilities;
using Silk.NET.SDL;
using Xunit;

namespace NeoVeldrid.Tests;

public class WindowCreationTests
{
    [Fact]
    public unsafe void CreateWindow_RequestsOpenGLForCompatibility()
    {
        WindowCreateInfo windowCI = new WindowCreateInfo
        {
            WindowWidth = 100,
            WindowHeight = 100,
            WindowInitialState = WindowState.Hidden,
            WindowTitle = nameof(CreateWindow_RequestsOpenGLForCompatibility),
        };
        Sdl2Window window = MainThread.Invoke(() => NeoVeldridStartup.CreateWindow(windowCI));

        try
        {
            uint flags = Sdl2Window.SdlInstance.GetWindowFlags((Window*)window.SdlWindowHandle);

            Assert.Equal((uint)WindowFlags.Opengl, flags & (uint)WindowFlags.Opengl);
        }
        finally
        {
            MainThread.Invoke(window.Close);
        }
    }

    [Theory]
    [InlineData(GraphicsBackend.Direct3D11, false)]
    [InlineData(GraphicsBackend.Vulkan, false)]
    [InlineData(GraphicsBackend.OpenGL, true)]
    [InlineData(GraphicsBackend.OpenGLES, true)]
    public unsafe void CreateWindow_RequestsOpenGLForBackend(GraphicsBackend backend, bool expected)
    {
        WindowCreateInfo windowCI = new WindowCreateInfo
        {
            WindowWidth = 100,
            WindowHeight = 100,
            WindowInitialState = WindowState.Hidden,
            WindowTitle = nameof(CreateWindow_RequestsOpenGLForBackend),
        };
        Sdl2Window window = MainThread.Invoke(() => NeoVeldridStartup.CreateWindow(windowCI, backend));

        try
        {
            uint flags = Sdl2Window.SdlInstance.GetWindowFlags((Window*)window.SdlWindowHandle);

            Assert.Equal(expected, (flags & (uint)WindowFlags.Opengl) != 0);
        }
        finally
        {
            MainThread.Invoke(window.Close);
        }
    }

    [Fact]
    public void CreateWindow_RejectsInvalidBackend()
    {
        WindowCreateInfo windowCI = new WindowCreateInfo();

        Assert.Throws<NeoVeldridException>(
            () => NeoVeldridStartup.CreateWindow(windowCI, (GraphicsBackend)byte.MaxValue)
        );
    }

#if TEST_VULKAN
    [Fact]
    [Trait("Backend", "Vulkan")]
    public unsafe void CreateWindowAndGraphicsDevice_Vulkan_DoesNotRequestOpenGL()
    {
        WindowCreateInfo windowCI = new WindowCreateInfo
        {
            WindowWidth = 100,
            WindowHeight = 100,
            WindowInitialState = WindowState.Hidden,
            WindowTitle = nameof(CreateWindowAndGraphicsDevice_Vulkan_DoesNotRequestOpenGL),
        };

        (Sdl2Window Window, GraphicsDevice Device) Create()
        {
            NeoVeldridStartup.CreateWindowAndGraphicsDevice(
                windowCI,
                new GraphicsDeviceOptions(),
                GraphicsBackend.Vulkan,
                out Sdl2Window createdWindow,
                out GraphicsDevice createdDevice
            );
            return (createdWindow, createdDevice);
        }

        (Sdl2Window window, GraphicsDevice gd) = MainThread.Invoke(Create);

        try
        {
            uint flags = Sdl2Window.SdlInstance.GetWindowFlags((Window*)window.SdlWindowHandle);

            Assert.Equal(0u, flags & (uint)WindowFlags.Opengl);
        }
        finally
        {
            gd.Dispose();
            MainThread.Invoke(window.Close);
        }
    }
#endif
}
