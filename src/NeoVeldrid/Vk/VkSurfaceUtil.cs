using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using static NeoVeldrid.Vk.VulkanUtil;
using System;
using System.Runtime.InteropServices;

namespace NeoVeldrid.Vk;

internal static unsafe partial class VkSurfaceUtil
{
    internal static SurfaceKHR CreateSurface(VkGraphicsDevice gd, Instance instance, SwapchainSource swapchainSource)
    {
        if (!gd.HasSurfaceExtension(CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME))
            throw new NeoVeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME}");

        switch (swapchainSource)
        {
            case XlibSwapchainSource xlibSource:
                if (!gd.HasSurfaceExtension(CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME))
                {
                    throw new NeoVeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME}");
                }
                return CreateXlib(gd, instance, xlibSource);
            case WaylandSwapchainSource waylandSource:
                if (!gd.HasSurfaceExtension(CommonStrings.VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME))
                {
                    throw new NeoVeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_WAYLAND_SURFACE_EXTENSION_NAME}");
                }
                return CreateWayland(gd, instance, waylandSource);
            case Win32SwapchainSource win32Source:
                if (!gd.HasSurfaceExtension(CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME))
                {
                    throw new NeoVeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME}");
                }
                return CreateWin32(gd, instance, win32Source);
            case NSWindowSwapchainSource nsWindowSource:
            {
                bool hasMetalExtension = gd.HasSurfaceExtension(CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME);
                if (hasMetalExtension || gd.HasSurfaceExtension(CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME))
                {
                    return CreateNSWindowSurface(gd, instance, nsWindowSource, hasMetalExtension);
                }
                throw new NeoVeldridException($"Neither macOS surface extension was available: " +
                    $"{CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME}, {CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME}");
            }
            case NSViewSwapchainSource nsViewSource:
            {
                bool hasMetalExtension = gd.HasSurfaceExtension(CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME);
                if (hasMetalExtension || gd.HasSurfaceExtension(CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME))
                {
                    return CreateNSViewSurface(gd, instance, nsViewSource, hasMetalExtension);
                }
                throw new NeoVeldridException($"Neither macOS surface extension was available: " +
                    $"{CommonStrings.VK_MVK_MACOS_SURFACE_EXTENSION_NAME}, {CommonStrings.VK_EXT_METAL_SURFACE_EXTENSION_NAME}");
            }
            default:
                throw new NeoVeldridException($"The provided SwapchainSource cannot be used to create a Vulkan surface.");
        }
    }

    private static SurfaceKHR CreateWin32(VkGraphicsDevice gd, Instance instance, Win32SwapchainSource win32Source)
    {
        Win32SurfaceCreateInfoKHR surfaceCI = new Win32SurfaceCreateInfoKHR
        {
            SType = StructureType.Win32SurfaceCreateInfoKhr
        };
        surfaceCI.Hwnd = win32Source.Hwnd;
        surfaceCI.Hinstance = win32Source.Hinstance;

        if (!gd.Vk.TryGetInstanceExtension(instance, out KhrWin32Surface khrWin32Surface))
        {
            throw new NeoVeldridException("VK_KHR_win32_surface extension not available.");
        }

        SurfaceKHR surface;
        Result result = khrWin32Surface.CreateWin32Surface(instance, in surfaceCI, null, out surface);
        CheckResult(result);
        return surface;
    }

    private static SurfaceKHR CreateXlib(VkGraphicsDevice gd, Instance instance, XlibSwapchainSource xlibSource)
    {
        XlibSurfaceCreateInfoKHR xsci = new XlibSurfaceCreateInfoKHR
        {
            SType = StructureType.XlibSurfaceCreateInfoKhr
        };
        xsci.Dpy = (nint*)xlibSource.Display;
        xsci.Window = (nint)xlibSource.Window;

        if (!gd.Vk.TryGetInstanceExtension(instance, out KhrXlibSurface khrXlibSurface))
        {
            throw new NeoVeldridException("VK_KHR_xlib_surface extension not available.");
        }

        SurfaceKHR surface;
        Result result = khrXlibSurface.CreateXlibSurface(instance, in xsci, null, out surface);
        CheckResult(result);
        return surface;
    }

    private static SurfaceKHR CreateWayland(VkGraphicsDevice gd, Instance instance, WaylandSwapchainSource waylandSource)
    {
        WaylandSurfaceCreateInfoKHR wsci = new WaylandSurfaceCreateInfoKHR
        {
            SType = StructureType.WaylandSurfaceCreateInfoKhr
        };
        wsci.Display = (nint*)waylandSource.Display;
        wsci.Surface = (nint*)waylandSource.Surface;

        if (!gd.Vk.TryGetInstanceExtension(instance, out KhrWaylandSurface khrWaylandSurface))
        {
            throw new NeoVeldridException("VK_KHR_wayland_surface extension not available.");
        }

        SurfaceKHR surface;
        Result result = khrWaylandSurface.CreateWaylandSurface(instance, in wsci, null, out surface);
        CheckResult(result);
        return surface;
    }

    private static unsafe SurfaceKHR CreateNSWindowSurface(VkGraphicsDevice gd, Instance instance, NSWindowSwapchainSource nsWindowSource, bool hasExtMetalSurface)
    {
        IntPtr contentView = ObjC.MsgSendIntPtr(nsWindowSource.NSWindow, ObjC.Sel("contentView"));
        return CreateNSViewSurface(gd, instance, new NSViewSwapchainSource(contentView), hasExtMetalSurface);
    }

    private static unsafe SurfaceKHR CreateNSViewSurface(VkGraphicsDevice gd, Instance instance, NSViewSwapchainSource nsViewSource, bool hasExtMetalSurface)
    {
        IntPtr metalLayer = GetOrCreateMetalLayer(nsViewSource.NSView);

        if (hasExtMetalSurface)
        {
            MetalSurfaceCreateInfoEXT surfaceCI = new MetalSurfaceCreateInfoEXT
            {
                SType = StructureType.MetalSurfaceCreateInfoExt,
                PLayer = (nint*)metalLayer
            };

            if (!gd.Vk.TryGetInstanceExtension(instance, out ExtMetalSurface extMetalSurface))
            {
                throw new NeoVeldridException("VK_EXT_metal_surface extension not available.");
            }

            SurfaceKHR surface;
            Result result = extMetalSurface.CreateMetalSurface(instance, in surfaceCI, null, out surface);
            CheckResult(result);
            return surface;
        }
        else
        {
            // Legacy path: VK_MVK_macos_surface
            MacOSSurfaceCreateInfoMVK surfaceCI = new MacOSSurfaceCreateInfoMVK
            {
                SType = StructureType.MacosSurfaceCreateInfoMvk,
                PView = nsViewSource.NSView.ToPointer()
            };

            var createMacOSSurface = gd.GetInstanceProcAddr<vkCreateMacOSSurfaceMVK_t>("vkCreateMacOSSurfaceMVK");
            if (createMacOSSurface == null)
            {
                throw new NeoVeldridException("vkCreateMacOSSurfaceMVK function not found.");
            }

            SurfaceKHR surface;
            Result result = createMacOSSurface(instance, &surfaceCI, null, &surface);
            CheckResult(result);
            return surface;
        }
    }

    private static IntPtr GetOrCreateMetalLayer(IntPtr nsView)
    {
        IntPtr layer = ObjC.MsgSendIntPtr(nsView, ObjC.Sel("layer"));
        IntPtr caMetalLayerClass = ObjC.GetClass("CAMetalLayer");

        if (layer == IntPtr.Zero || !ObjC.MsgSendBoolIntPtr(layer, ObjC.Sel("isKindOfClass:"), caMetalLayerClass))
        {
            layer = ObjC.MsgSendIntPtr(caMetalLayerClass, ObjC.Sel("alloc"));
            layer = ObjC.MsgSendIntPtr(layer, ObjC.Sel("init"));

            if (ObjC.MsgSendBool(nsView, ObjC.Sel("wantsBestResolutionOpenGLSurface")))
            {
                IntPtr window = ObjC.MsgSendIntPtr(nsView, ObjC.Sel("window"));
                if (window != IntPtr.Zero)
                {
                    double contentsScale = ObjC.MsgSendDouble(window, ObjC.Sel("backingScaleFactor"));
                    ObjC.MsgSendVoidDouble(layer, ObjC.Sel("setContentsScale:"), contentsScale);
                }
            }

            ObjC.MsgSendVoidIntPtr(nsView, ObjC.Sel("setLayer:"), layer);
        }

        ObjC.MsgSendVoidBool(nsView, ObjC.Sel("setWantsLayer:"), 1);
        return layer;
    }

    // Minimal ObjC runtime P/Invoke for macOS surface creation.
    private static partial class ObjC
    {
        private const string Lib = "/usr/lib/libobjc.A.dylib";

        [LibraryImport(Lib, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr Sel(string name);

        [LibraryImport(Lib, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr GetClass(string name);

        [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
        public static partial IntPtr MsgSendIntPtr(IntPtr receiver, IntPtr selector);

        [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
        public static partial void MsgSendVoidIntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

        [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
        public static partial void MsgSendVoidBool(IntPtr receiver, IntPtr selector, byte arg);

        [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
        public static partial void MsgSendVoidDouble(IntPtr receiver, IntPtr selector, double arg);

        [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
        public static partial double MsgSendDouble(IntPtr receiver, IntPtr selector);

        [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool MsgSendBool(IntPtr receiver, IntPtr selector);

        [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool MsgSendBoolIntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);
    }
}
