using System;
using System.IO;
using System.Numerics;
using NeoVeldrid;
using NeoVeldrid.Sdl2;
using NeoVeldrid.StartupUtilities;
using FontStashSharp;

namespace FontStashText;

internal class Program
{
    private static Sdl2Window _window;
    private static GraphicsDevice _graphicsDevice;
    private static CommandList _commandList;

    static void Main(string[] args)
    {
        GraphicsDeviceOptions options = new GraphicsDeviceOptions
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
        };

        string backendEnv = Environment.GetEnvironmentVariable("NEOVELDRID_BACKEND");
        GraphicsBackend backend = string.IsNullOrEmpty(backendEnv)
            ? NeoVeldridStartup.GetPlatformDefaultBackend()
            : backendEnv.ToLowerInvariant() switch
            {
                "d3d11" or "direct3d11" => GraphicsBackend.Direct3D11,
                "vulkan" or "vk" => GraphicsBackend.Vulkan,
                "opengl" or "gl" => GraphicsBackend.OpenGL,
                "opengles" or "gles" => GraphicsBackend.OpenGLES,
                _ => throw new InvalidOperationException($"Unknown NEOVELDRID_BACKEND: '{backendEnv}'")
            };

        WindowCreateInfo windowCI = new WindowCreateInfo
        {
            X = 100,
            Y = 100,
            WindowWidth = 800,
            WindowHeight = 600,
            WindowTitle = $"FontStashSharp + NeoVeldrid ({backend})",
        };

        NeoVeldridStartup.CreateWindowAndGraphicsDevice(windowCI, options, backend, out _window, out _graphicsDevice);
        _window.Resized += Window_Resized;

        _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();

        FontSystem fontSystem = new FontSystem();
        string fontPath = Path.Combine(AppContext.BaseDirectory, "JupiteroidRegular.ttf");
        fontSystem.AddFont(File.ReadAllBytes(fontPath));
        FontRenderer fontRenderer = new FontRenderer(_graphicsDevice);

        while (_window.Exists)
        {
            _window.PumpEvents();

            _commandList.Begin();
            _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
            _commandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);

            fontRenderer.Begin(_commandList, _window.Width, _window.Height);
            var font = fontSystem.GetFont(48);
            font.DrawText(fontRenderer, "Hello FontStashSharp in NeoVeldrid!", new Vector2(50, 250), FSColor.White);
            fontRenderer.Flush();

            _commandList.End();
            _graphicsDevice.SubmitCommands(_commandList);
            _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
        }

        fontRenderer.Dispose();
        _commandList.Dispose();
        _graphicsDevice.Dispose();
    }

    private static void Window_Resized() =>
        _graphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
}
