using NeoVeldrid.Sdl2;
using NeoVeldrid.StartupUtilities;

namespace NeoVeldrid.Samples;

internal class Program
{
    private static Sdl2Window _window;
    private static GraphicsDevice _graphics_device;

    static void Main(string[] args)
    {
        WindowCreateInfo window_ci = new WindowCreateInfo()
        {
            X = 100,
            Y = 100,
            WindowWidth = 960,
            WindowHeight = 540,
            WindowTitle = "Part 1: Getting Started",
        };
        _window = NeoVeldridStartup.CreateWindow(ref window_ci);

        GraphicsDeviceOptions gd_options = new GraphicsDeviceOptions
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
        };
        _graphics_device = NeoVeldridStartup.CreateGraphicsDevice(_window, gd_options);

        while (_window.Exists)
        {
            _window.PumpEvents();
        }
    }
}
