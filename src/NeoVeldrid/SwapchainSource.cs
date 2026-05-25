using System;

namespace NeoVeldrid
{
    /// <summary>
    /// A platform-specific object representing a renderable surface.
    /// A SwapchainSource can be created with one of several static factory methods.
    /// A SwapchainSource is used to describe a Swapchain (see <see cref="SwapchainDescription"/>).
    /// </summary>
    public abstract class SwapchainSource
    {
        internal SwapchainSource() { }

        /// <summary>
        /// Creates a new SwapchainSource for a Win32 window.
        /// </summary>
        /// <param name="hwnd">The Win32 window handle.</param>
        /// <param name="hinstance">The Win32 instance handle.</param>
        /// <returns>A new SwapchainSource which can be used to create a <see cref="Swapchain"/> for the given Win32 window.
        /// </returns>
        public static SwapchainSource CreateWin32(IntPtr hwnd, IntPtr hinstance) => new Win32SwapchainSource(hwnd, hinstance);

        /// <summary>
        /// Creates a new SwapchainSource from the given Xlib information.
        /// </summary>
        /// <param name="display">An Xlib Display.</param>
        /// <param name="window">An Xlib Window.</param>
        /// <returns>A new SwapchainSource which can be used to create a <see cref="Swapchain"/> for the given Xlib window.
        /// </returns>
        public static SwapchainSource CreateXlib(IntPtr display, IntPtr window) => new XlibSwapchainSource(display, window);

        /// <summary>
        /// Creates a new SwapchainSource from the given Wayland information.
        /// </summary>
        /// <param name="display">The Wayland display proxy.</param>
        /// <param name="surface">The Wayland surface proxy to map.</param>
        /// <returns>A new SwapchainSource which can be used to create a <see cref="Swapchain"/> for the given Wayland surface.
        /// </returns>
        public static SwapchainSource CreateWayland(IntPtr display, IntPtr surface) => new WaylandSwapchainSource(display, surface);


        /// <summary>
        /// Creates a new SwapchainSource for the given NSWindow.
        /// </summary>
        /// <param name="nsWindow">A pointer to an NSWindow.</param>
        /// <returns>A new SwapchainSource which can be used to create a Vulkan <see cref="Swapchain"/> for the given NSWindow.
        /// </returns>
        public static SwapchainSource CreateNSWindow(IntPtr nsWindow) => new NSWindowSwapchainSource(nsWindow);

        /// <summary>
        /// Creates a new SwapchainSource for the given NSView.
        /// </summary>
        /// <param name="nsView">A pointer to an NSView.</param>
        /// <returns>A new SwapchainSource which can be used to create a Vulkan <see cref="Swapchain"/> for the given NSView.
        /// </returns>
        public static SwapchainSource CreateNSView(IntPtr nsView)
            => new NSViewSwapchainSource(nsView);
    }

    internal class Win32SwapchainSource : SwapchainSource
    {
        public IntPtr Hwnd { get; }
        public IntPtr Hinstance { get; }

        public Win32SwapchainSource(IntPtr hwnd, IntPtr hinstance)
        {
            Hwnd = hwnd;
            Hinstance = hinstance;
        }
    }

    internal class XlibSwapchainSource : SwapchainSource
    {
        public IntPtr Display { get; }
        public IntPtr Window { get; }

        public XlibSwapchainSource(IntPtr display, IntPtr window)
        {
            Display = display;
            Window = window;
        }
    }

    internal class WaylandSwapchainSource : SwapchainSource
    {
        public IntPtr Display { get; }
        public IntPtr Surface { get; }

        public WaylandSwapchainSource(IntPtr display, IntPtr surface)
        {
            Display = display;
            Surface = surface;
        }
    }

    internal class NSWindowSwapchainSource : SwapchainSource
    {
        public IntPtr NSWindow { get; }

        public NSWindowSwapchainSource(IntPtr nsWindow)
        {
            NSWindow = nsWindow;
        }
    }

    internal class NSViewSwapchainSource : SwapchainSource
    {
        public IntPtr NSView { get; }

        public NSViewSwapchainSource(IntPtr nsView)
        {
            NSView = nsView;
        }
    }
}
