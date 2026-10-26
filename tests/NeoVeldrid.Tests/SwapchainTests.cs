using NeoVeldrid.Sdl2;
using NeoVeldrid.StartupUtilities;
using Xunit;

namespace NeoVeldrid.Tests;

public abstract class SwapchainTests<T> : GraphicsDeviceTestBase<T> where T : GraphicsDeviceCreator
{
    [Theory]
    [InlineData(PixelFormat.R16_UNorm, false)]
    [InlineData(PixelFormat.R16_UNorm, true)]
    [InlineData(PixelFormat.R32_Float, false)]
    [InlineData(PixelFormat.R32_Float, true)]
    [InlineData(null, false)]
    [InlineData(null, true)]
    public void Ctor_SetsProperties(PixelFormat? depthFormat, bool syncToVerticalBlank)
    {
        WindowCreateInfo wci = new WindowCreateInfo
        {
            WindowWidth = 100,
            WindowHeight = 100,
            WindowInitialState = WindowState.Hidden,
            WindowTitle = "SwapchainTestWindow",
        };
        Sdl2Window window = MainThread.Invoke(() => NeoVeldridStartup.CreateWindow(wci));
        SwapchainSource source = NeoVeldridStartup.GetSwapchainSource(window);
        SwapchainDescription swapchainDesc = new SwapchainDescription(source, 100, 100, depthFormat, syncToVerticalBlank);
        Swapchain swapchain = RF.CreateSwapchain(ref swapchainDesc);

        if (depthFormat == null)
        {
            Assert.Null(swapchain.Framebuffer.DepthTarget);
        }
        else
        {
            Assert.NotNull(swapchain.Framebuffer.DepthTarget);
            Assert.Equal(depthFormat, swapchain.Framebuffer.DepthTarget.Value.Target.Format);
        }

        Assert.Equal(syncToVerticalBlank, swapchain.SyncToVerticalBlank);

        MainThread.Invoke(window.Close);
    }
}

public abstract class MainSwapchainTests<T> : GraphicsDeviceTestBase<T> where T : GraphicsDeviceCreator
{
    [Fact]
    public void Textures_Properties_Correct()
    {
        Texture colorTarget = GD.MainSwapchain.Framebuffer.ColorTargets[0].Target;
        Assert.Equal(TextureType.Texture2D, colorTarget.Type);
        Assert.InRange(colorTarget.Width, 1u, uint.MaxValue);
        Assert.InRange(colorTarget.Height, 1u, uint.MaxValue);
        Assert.Equal(1u, colorTarget.Depth);
        Assert.Equal(1u, colorTarget.ArrayLayers);
        Assert.Equal(1u, colorTarget.MipLevels);
        Assert.Equal(TextureUsage.RenderTarget, colorTarget.Usage);
        Assert.Equal(TextureSampleCount.Count1, colorTarget.SampleCount);

        Texture depthTarget = GD.MainSwapchain.Framebuffer.DepthTarget.Value.Target;
        Assert.Equal(TextureType.Texture2D, depthTarget.Type);
        Assert.Equal(colorTarget.Width, depthTarget.Width);
        Assert.Equal(colorTarget.Height, depthTarget.Height);
        Assert.Equal(1u, depthTarget.Depth);
        Assert.Equal(1u, depthTarget.ArrayLayers);
        Assert.Equal(1u, depthTarget.MipLevels);
        Assert.Equal(TextureUsage.DepthStencil, depthTarget.Usage);
        Assert.Equal(TextureSampleCount.Count1, depthTarget.SampleCount);
    }

    [Fact]
    public void SwapBuffers_AfterWindowResizeWithoutSwapchainResize_KeepsPresenting()
    {
        CommandList cl = RF.CreateCommandList();
        RenderFrames(cl, 3);

        MainThread.Invoke(() =>
        {
            Window.Width = 300;
            Window.Height = 260;
            Window.PumpEvents();
        });

        // The first present after the resize finds the swapchain out of date. Frames rendered after that
        // must present an image acquired from the recreated swapchain.
        RenderFrames(cl, 6);
        GD.WaitForIdle();
    }

    [Fact]
    public void Framebuffer_AfterWindowResizeWithoutSwapchainResize_MatchesColorTargetSize()
    {
        CommandList cl = RF.CreateCommandList();
        RenderFrames(cl, 1);

        MainThread.Invoke(() =>
        {
            Window.Width = 300;
            Window.Height = 260;
            Window.PumpEvents();
        });

        RenderFrames(cl, 3);
        GD.WaitForIdle();

        Framebuffer framebuffer = GD.MainSwapchain.Framebuffer;
        Texture colorTarget = framebuffer.ColorTargets[0].Target;
        Assert.Equal(colorTarget.Width, framebuffer.Width);
        Assert.Equal(colorTarget.Height, framebuffer.Height);
    }

    private void RenderFrames(CommandList cl, int count)
    {
        for (int i = 0; i < count; i++)
        {
            cl.Begin();
            cl.SetFramebuffer(GD.MainSwapchain.Framebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Red);
            cl.End();
            GD.SubmitCommands(cl);
            GD.SwapBuffers();
        }
    }
}

#if TEST_VULKAN
[Trait("Backend", "Vulkan")]
public class VulkanSwapchainTests : SwapchainTests<VulkanDeviceCreator> { }
[Trait("Backend", "Vulkan")]
public class VulkanMainSwapchainTests : MainSwapchainTests<VulkanDeviceCreatorWithMainSwapchain> { }
#endif
#if TEST_D3D11
[Trait("Backend", "D3D11")]
public class D3D11SwapchainTests : SwapchainTests<D3D11DeviceCreator> { }
[Trait("Backend", "D3D11")]
public class D3D11MainSwapchainTests : MainSwapchainTests<D3D11DeviceCreatorWithMainSwapchain> { }
#endif
#if TEST_OPENGL
[Trait("Backend", "OpenGL")]
public class OpenGLMainSwapchainTests : MainSwapchainTests<OpenGLDeviceCreator> { }
#endif
