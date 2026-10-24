using System;
using System.Runtime.InteropServices;

namespace NeoVeldrid.OpenGL;

internal static class OpenGLVersionInfo
{
    private static readonly object _lock = new();
    private static GraphicsApiVersion _cachedOpenGLVersion = GraphicsApiVersion.Unknown;
    private static GraphicsApiVersion _cachedOpenGLESVersion = GraphicsApiVersion.Unknown;

    public static GraphicsApiVersion GetApiVersion(GraphicsBackend backend)
    {
        var result = GraphicsApiVersion.Unknown;

        if (backend != GraphicsBackend.OpenGL && backend != GraphicsBackend.OpenGLES)
            return result;

        result = GetCachedApiVersion(backend);

        if (result == GraphicsApiVersion.Unknown)
        {
            string versionString = string.Empty;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (backend == GraphicsBackend.OpenGL)
                    versionString = OpenGLWglVersionProber.GetOpenGLVersionString();
                if (backend == GraphicsBackend.OpenGLES)
                {
                    try
                    {
                        versionString = OpenGLAngleVersionProber.GetOpenGLESVersionString();
                    }
                    catch (DllNotFoundException)
                    {
                        versionString = string.Empty;
                    }

                    if (string.IsNullOrEmpty(versionString))
                        versionString = OpenGLWglVersionProber.GetOpenGLESVersionString();
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    if (backend == GraphicsBackend.OpenGL)
                        versionString = OpenGLEglVersionProber.GetOpenGLVersionString();
                    if (backend == GraphicsBackend.OpenGLES)
                        versionString = OpenGLEglVersionProber.GetOpenGLESVersionString();
                }
                catch (DllNotFoundException)
                {
                    versionString = string.Empty;
                }

                if (string.IsNullOrEmpty(versionString))
                {
                    if (backend == GraphicsBackend.OpenGL)
                        versionString = OpenGLGlxVersionProber.GetOpenGLVersionString();
                    if (backend == GraphicsBackend.OpenGLES)
                        versionString = OpenGLGlxVersionProber.GetOpenGLESVersionString();
                }
            }

            GraphicsApiVersion.TryParseGLVersion(versionString, out result);

            // If the OpenGL version < 3.2, we have failed to return a valid OpenGL version.
            if (backend == GraphicsBackend.OpenGL &&
                (result.Major < 3 ||
                (result.Major == 3 && result.Minor < 2)))
                result = GraphicsApiVersion.Unknown;

            if (result != GraphicsApiVersion.Unknown)
                CacheApiVersion(backend, result);
        }

        return result;
    }

    private static GraphicsApiVersion GetCachedApiVersion(GraphicsBackend backend)
    {
        lock (_lock)
        {
            if (backend == GraphicsBackend.OpenGL)
                return _cachedOpenGLVersion;

            if (backend == GraphicsBackend.OpenGLES)
                return _cachedOpenGLESVersion;
        }
        return GraphicsApiVersion.Unknown;
    }

    private static void CacheApiVersion(GraphicsBackend backend, GraphicsApiVersion version)
    {
        lock (_lock)
        {
            if (backend == GraphicsBackend.OpenGL)
                _cachedOpenGLVersion = version;

            if (backend == GraphicsBackend.OpenGLES)
                _cachedOpenGLESVersion = version;
        }
    }
}

internal static class OpenGLWglVersionProber
{
    private delegate IntPtr wglCreateContextAttribsARB(IntPtr hDC, IntPtr hShareContext, int[] attribList);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);
    [DllImport("gdi32.dll")]
    private static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);
    [DllImport("gdi32.dll")]
    private static extern bool SetPixelFormat(IntPtr hdc, int format, ref PIXELFORMATDESCRIPTOR ppfd);
    [DllImport("opengl32.dll")]
    private static extern IntPtr wglCreateContext(IntPtr hdc);
    [DllImport("opengl32.dll")]
    private static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);
    [DllImport("opengl32.dll")]
    private static extern bool wglDeleteContext(IntPtr hglrc);
    [DllImport("opengl32.dll")]
    private static extern IntPtr glGetString(uint name);
    [DllImport("opengl32.dll", CharSet = CharSet.Ansi)]
    private static extern IntPtr wglGetProcAddress(string lpszProc);

    public static string GetOpenGLVersionString()
    {
        IntPtr hwnd = IntPtr.Zero;
        IntPtr hdc = IntPtr.Zero;
        IntPtr context = IntPtr.Zero;

        string result = string.Empty;
        do
        {
            hwnd = CreateWindowEx(0, "STATIC", "", 0, 0, 0, 1, 1,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (hwnd == IntPtr.Zero)
                break;

            hdc = GetDC(hwnd);
            if (hdc == IntPtr.Zero)
                break;

            PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR
            {
                nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                nVersion = 1,
                dwFlags = 0x00000004 |              // PFD_DRAW_TO_WINDOW
                          0x00000020,               // PFD_SUPPORT_OPENGL
                iPixelType = 0,                     // PFD_TYPE_RGBA
                cColorBits = 32,
            };

            int pixelFormat = ChoosePixelFormat(hdc, ref pfd);
            if (pixelFormat == 0 || !SetPixelFormat(hdc, pixelFormat, ref pfd))
                break;

            context = wglCreateContext(hdc);
            if (context == IntPtr.Zero)
                break;
            wglMakeCurrent(hdc, context);

            // Checking for Microsoft software rasterizer fallback. If we are given that,
            // the method failed.
            IntPtr rendererPtr = glGetString(0x1F01); // GL_RENDERER
            if (rendererPtr != IntPtr.Zero)
            {
                string renderer = Marshal.PtrToStringAnsi(rendererPtr);
                if (!string.IsNullOrEmpty(renderer) && renderer.Contains("GDI Generic"))
                    break;
            }

            IntPtr versionPtr = glGetString(0x1F02); // GL_VERSION
            if (versionPtr == IntPtr.Zero)
                break;
            result = Marshal.PtrToStringAnsi(versionPtr);

            break;
        }
        while (true);

        if (context != IntPtr.Zero)
        {
            wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            wglDeleteContext(context);
        }
        if (hdc != IntPtr.Zero)
            ReleaseDC(hwnd, hdc);
        if (hwnd != IntPtr.Zero)
            DestroyWindow(hwnd);

        return result;
    }

    public static string GetOpenGLESVersionString()
    {
        IntPtr hwnd = IntPtr.Zero;
        IntPtr hdc = IntPtr.Zero;
        IntPtr context = IntPtr.Zero;

        string result = string.Empty;
        do
        {
            hwnd = CreateWindowEx(0, "STATIC", "", 0, 0, 0, 1, 1,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (hwnd == IntPtr.Zero)
                break;

            hdc = GetDC(hwnd);
            if (hdc == IntPtr.Zero)
                break;

            PIXELFORMATDESCRIPTOR pfd = new PIXELFORMATDESCRIPTOR
            {
                nSize = (ushort)Marshal.SizeOf<PIXELFORMATDESCRIPTOR>(),
                nVersion = 1,
                dwFlags = 0x00000004 |              // PFD_DRAW_TO_WINDOW
                          0x00000020,               // PFD_SUPPORT_OPENGL
                iPixelType = 0,                     // PFD_TYPE_RGBA
                cColorBits = 32,
            };

            int pixelFormat = ChoosePixelFormat(hdc, ref pfd);
            if (pixelFormat == 0 || !SetPixelFormat(hdc, pixelFormat, ref pfd))
                break;

            context = wglCreateContext(hdc);
            if (context == IntPtr.Zero)
                break;
            wglMakeCurrent(hdc, context);

            // We needed a regular OpenGL context for bootstrapping, now we get into
            // the actual ES stuff.
            IntPtr pCreateContextAttribs = wglGetProcAddress("wglCreateContextAttribsARB");
            if (pCreateContextAttribs == IntPtr.Zero)
                break;
            var createContextAttribs = Marshal.GetDelegateForFunctionPointer<wglCreateContextAttribsARB>(
                pCreateContextAttribs);

            int[] attribs = new int[]
            {
                0x2091, 3,      // WGL_CONTEXT_MAJOR_VERSION_ARB
                0x2092, 0,      // WGL_CONTEXT_MINOR_VERSION_ARB
                0x9126,         // WGL_CONTEXT_PROFILE_MASK_ARB
                0x00000004,     // WGL_CONTEXT_ES2_PROFILE_BIT_EXT
                0
            };

            IntPtr esContext = createContextAttribs(hdc, IntPtr.Zero, attribs);
            if (esContext == IntPtr.Zero)
            {
                // If 3.x isn't available, try 2.x.
                attribs[1] = 2; // Change WGL_CONTEXT_MAJOR_VERSION_ARB to 2
                esContext = createContextAttribs(hdc, IntPtr.Zero, attribs);

                if (esContext == IntPtr.Zero)
                    break;
            }

            wglMakeCurrent(hdc, esContext);

            IntPtr versionPtr = glGetString(0x1F02); // GL_VERSION
            if (versionPtr != IntPtr.Zero)
                result = Marshal.PtrToStringAnsi(versionPtr);

            // Safely delete the OpenGL ES context.
            wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            wglDeleteContext(esContext);

            break;
        }
        while (true);

        if (context != IntPtr.Zero)
        {
            wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            wglDeleteContext(context);
        }
        if (hdc != IntPtr.Zero)
            ReleaseDC(hwnd, hdc);
        if (hwnd != IntPtr.Zero)
            DestroyWindow(hwnd);

        return result;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PIXELFORMATDESCRIPTOR
    {
        public ushort nSize;
        public ushort nVersion;
        public uint dwFlags;
        public byte iPixelType;
        public byte cColorBits;
        public byte cRedBits;
        public byte cRedShift;
        public byte cGreenBits;
        public byte cGreenShift;
        public byte cBlueBits;
        public byte cBlueShift;
        public byte cAlphaBits;
        public byte cAlphaShift;
        public byte cAccumBits;
        public byte cAccumRedBits;
        public byte cAccumGreenBits;
        public byte cAccumBlueBits;
        public byte cAccumAlphaBits;
        public byte cDepthBits;
        public byte cStencilBits;
        public byte cAuxBuffers;
        public byte iLayerType;
        public byte bReserved;
        public uint dwLayerMask;
        public uint dwVisibleMask;
        public uint dwDamageMask;
    }
}

internal static class OpenGLAngleVersionProber
{
    [DllImport("libEGL.dll")]
    private static extern IntPtr eglGetDisplay(IntPtr display_id);
    [DllImport("libEGL.dll")]
    private static extern bool eglInitialize(IntPtr dpy, out int major, out int minor);
    [DllImport("libEGL.dll")]
    private static extern bool eglBindAPI(uint api);
    [DllImport("libEGL.dll")]
    private static extern bool eglChooseConfig(IntPtr dpy, int[] attrib_list, IntPtr[] configs, int config_size, out int num_config);
    [DllImport("libEGL.dll")]
    private static extern IntPtr eglCreateContext(IntPtr dpy, IntPtr config, IntPtr share_context, int[] attrib_list);
    [DllImport("libEGL.dll")]
    private static extern bool eglMakeCurrent(IntPtr dpy, IntPtr draw, IntPtr read, IntPtr ctx);
    [DllImport("libEGL.dll")]
    private static extern bool eglDestroyContext(IntPtr dpy, IntPtr ctx);
    [DllImport("libEGL.dll")]
    private static extern bool eglTerminate(IntPtr dpy);
    [DllImport("libEGL.dll")]
    public static extern IntPtr eglQueryString(IntPtr dpy, int name);
    [DllImport("libGLESv2.dll", EntryPoint = "glGetString")]
    private static extern IntPtr glGetString(uint name);

    public static string GetOpenGLESVersionString()
    {
        IntPtr eglDisplay = IntPtr.Zero;
        IntPtr eglContext = IntPtr.Zero;

        string result = string.Empty;
        bool isBorrowedDisplay = false;
        do
        {
            eglDisplay = eglGetDisplay(IntPtr.Zero);
            if (eglDisplay == IntPtr.Zero) break;

            isBorrowedDisplay = eglQueryString(eglDisplay, 0x3055) != IntPtr.Zero;

            if (!eglInitialize(eglDisplay, out int major, out int minor)) break;
            if (!eglBindAPI(0x30A0)) break; // EGL_OPENGL_ES_API

            int[] configAttribs = {
            0x3033, 0x0001,             // EGL_SURFACE_TYPE, EGL_PBUFFER_BIT
            0x3040, 0x0040 | 0x0004,    // EGL_RENDERABLE_TYPE, EGL_OPENGL_ES3_BIT | EGL_OPENGL_ES2_BIT
            0x3038,                     // EGL_NONE
        };

            IntPtr[] configs = new IntPtr[1];
            if (!eglChooseConfig(eglDisplay, configAttribs, configs, 1, out int numConfigs) || numConfigs == 0) break;

            int[] contextAttribs = new int[]
            {
            0x3098, 3,  // EGL_CONTEXT_CLIENT_VERSION, 3
            0x3038      // EGL_NONE
            };

            eglContext = eglCreateContext(eglDisplay, configs[0], IntPtr.Zero, contextAttribs);
            if (eglContext == IntPtr.Zero)
            {
                contextAttribs[1] = 2; // Retry on ES 2.x context constraints
                eglContext = eglCreateContext(eglDisplay, configs[0], IntPtr.Zero, contextAttribs);
                if (eglContext == IntPtr.Zero) break;
            }

            if (!eglMakeCurrent(eglDisplay, IntPtr.Zero, IntPtr.Zero, eglContext)) break;

            IntPtr versionPtr = glGetString(0x1F02); // GL_VERSION
            if (versionPtr != IntPtr.Zero)
                result = Marshal.PtrToStringAnsi(versionPtr);
        }
        while (true);

        if (eglContext != IntPtr.Zero)
        {
            eglMakeCurrent(eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            eglDestroyContext(eglDisplay, eglContext);
        }
        if (!isBorrowedDisplay)
            eglTerminate(eglDisplay);

        return result;
    }
}

internal static class OpenGLEglVersionProber
{
    [DllImport("libEGL.so.1")]
    private static extern IntPtr eglGetDisplay(IntPtr display_id);
    [DllImport("libEGL.so.1")]
    private static extern bool eglInitialize(IntPtr dpy, out int major, out int minor);
    [DllImport("libEGL.so.1")]
    private static extern bool eglBindAPI(uint api);
    [DllImport("libEGL.so.1")]
    private static extern bool eglChooseConfig(IntPtr dpy, int[] attrib_list, IntPtr[] configs, int config_size, out int num_config);
    [DllImport("libEGL.so.1")]
    private static extern IntPtr eglCreateContext(IntPtr dpy, IntPtr config, IntPtr share_context, int[] attrib_list);
    [DllImport("libEGL.so.1")]
    private static extern bool eglMakeCurrent(IntPtr dpy, IntPtr draw, IntPtr read, IntPtr ctx);
    [DllImport("libEGL.so.1")]
    private static extern bool eglDestroyContext(IntPtr dpy, IntPtr ctx);
    [DllImport("libEGL.so.1")]
    private static extern bool eglTerminate(IntPtr dpy);
    [DllImport("libEGL.so.1")]
    public static extern IntPtr eglQueryString(IntPtr dpy, int name);
    [DllImport("libGL.so.1", EntryPoint = "glGetString")]
    private static extern IntPtr glGetString(uint name);
    [DllImport("libGLESv2.so.2", EntryPoint = "glGetString")]
    private static extern IntPtr glGetStringES(uint name);

    public static string GetOpenGLVersionString()
    {
        IntPtr eglDisplay = IntPtr.Zero;
        IntPtr eglContext = IntPtr.Zero;

        string result = string.Empty;
        bool isBorrowedDisplay = false;
        do
        {
            eglDisplay = eglGetDisplay(IntPtr.Zero);
            if (eglDisplay == IntPtr.Zero)
                break;

            isBorrowedDisplay = eglQueryString(eglDisplay, 0x3055) != IntPtr.Zero;

            if (!eglInitialize(eglDisplay, out int major, out int minor))
                break;

            if (!eglBindAPI(0x30A2)) // EGL_OPENGL_API
                break;

            int[] majorVersions = { 4, 4, 4, 4, 4, 4, 4, 3 };
            int[] minorVersions = { 6, 5, 4, 3, 2, 1, 0, 3 };

            int[] configAttribs = {
                0x3033, // EGL_SURFACE_TYPE
                0x0001, // EGL_PBUFFER_BIT
                0x3040, // EGL_RENDERABLE_TYPE
                0x0008, // EGL_OPENGL_BIT
                0x3038, // EGL_NONE
            };

            IntPtr[] configs = new IntPtr[1];
            if (!eglChooseConfig(eglDisplay, configAttribs, configs, 1, out int numConfigs) || numConfigs == 0)
                break;

            for (int i = 0; i < majorVersions.Length; i++)
            {
                int[] contextAttribs = {
                0x3098, majorVersions[i],
                0x30FB, minorVersions[i],
                0x30FD, 0x00000001,
                0x3038 // EGL_NONE
            };

                eglContext = eglCreateContext(eglDisplay, configs[0], IntPtr.Zero, contextAttribs);
                if (eglContext != IntPtr.Zero)
                    break;
            }

            if (eglContext == IntPtr.Zero)
                break;

            if (!eglMakeCurrent(eglDisplay, IntPtr.Zero, IntPtr.Zero, eglContext))
                break;

            IntPtr versionPtr = glGetString(0x1F02); // GL_VERSION
            if (versionPtr != IntPtr.Zero)
                result = Marshal.PtrToStringAnsi(versionPtr);

            break;
        }
        while (true);

        if (eglContext != IntPtr.Zero)
        {
            eglMakeCurrent(eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            eglDestroyContext(eglDisplay, eglContext);
        }

        if (!isBorrowedDisplay)
            eglTerminate(eglDisplay);

        return result;
    }

    public static string GetOpenGLESVersionString()
    {
        IntPtr eglDisplay = IntPtr.Zero;
        IntPtr eglContext = IntPtr.Zero;

        string result = string.Empty;
        do
        {
            eglDisplay = eglGetDisplay(IntPtr.Zero);
            if (eglDisplay == IntPtr.Zero)
                break;

            if (!eglInitialize(eglDisplay, out int major, out int minor))
                break;

            if (!eglBindAPI(0x30A0)) // EGL_OPENGL_ES_API
                break;

            int[] configAttribs = {
                0x3033,             // EGL_SURFACE_TYPE
                0x0001,             // EGL_PBUFFER_BIT
                0x3040,             // EGL_RENDERABLE_TYPE
                0x0040 | 0x0004,    // EGL_OPENGL_ES3_BIT, EGL_OPENGL_ES2_BIT
                0x3038,             // EGL_NONE
            };

            IntPtr[] configs = new IntPtr[1];
            if (!eglChooseConfig(eglDisplay, configAttribs, configs, 1, out int numConfigs) || numConfigs == 0)
                break;

            // This makes sure that EGL tries to get an ES 3.x context first.
            int[] contextAttribs = new int[]
            {
                0x3098,     // EGL_CONTEXT_CLIENT_VERSION
                3, 0x3038,  // EGL_NONE
            };

            eglContext = eglCreateContext(eglDisplay, configs[0], IntPtr.Zero, contextAttribs);
            if (eglContext == IntPtr.Zero)
            {
                // If 3.x isn't available, try 2.x.
                contextAttribs[1] = 2;
                eglContext = eglCreateContext(eglDisplay, configs[0], IntPtr.Zero, contextAttribs);
                if (eglContext == IntPtr.Zero)
                    break;
            }

            if (!eglMakeCurrent(eglDisplay, IntPtr.Zero, IntPtr.Zero, eglContext))
                break;

            IntPtr versionPtr = glGetStringES(0x1F02); // GL_VERSION
            if (versionPtr != IntPtr.Zero)
                result = Marshal.PtrToStringAnsi(versionPtr);

            break;
        }
        while (true);

        if (eglContext != IntPtr.Zero)
        {
            eglMakeCurrent(eglDisplay, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            eglDestroyContext(eglDisplay, eglContext);
        }
        eglTerminate(eglDisplay);

        return result;
    }
}

internal static class OpenGLGlxVersionProber
{
    private delegate IntPtr glXCreateContextAttribsARBDelegate(IntPtr dpy, IntPtr config, IntPtr share_context, bool direct, int[] attrib_list);

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);
    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);
    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport("libX11.so.6")]
    private static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr parent, int x, int y, uint width, uint height, uint border_width, UIntPtr border, UIntPtr background);
    [DllImport("libX11.so.6")]
    private static extern int XDestroyWindow(IntPtr display, IntPtr window);
    [DllImport("libGL.so.1")]
    private static extern IntPtr glXChooseVisual(IntPtr dpy, int screen, int[] attribList);
    [DllImport("libGL.so.1")]
    private static extern IntPtr glXChooseFBConfig(IntPtr dpy, int screen, int[] attribList, out int nitems);
    [DllImport("libGL.so.1")]
    private static extern IntPtr glXCreateContext(IntPtr dpy, IntPtr vis, IntPtr shareList, bool direct);
    [DllImport("libGL.so.1")]
    private static extern bool glXMakeCurrent(IntPtr dpy, IntPtr drawable, IntPtr ctx);
    [DllImport("libGL.so.1")]
    private static extern void glXDestroyContext(IntPtr dpy, IntPtr ctx);
    [DllImport("libX11.so.6")]
    private static extern int XFree(IntPtr data);
    [DllImport("libGL.so.1", EntryPoint = "glGetString")]
    private static extern IntPtr glGetString(uint name);
    [DllImport("libGL.so.1", CharSet = CharSet.Ansi)]
    private static extern IntPtr glXGetProcAddress(string procName);

    public static string GetOpenGLVersionString()
    {
        IntPtr display = IntPtr.Zero;
        IntPtr window = IntPtr.Zero;
        IntPtr context = IntPtr.Zero;
        IntPtr visual = IntPtr.Zero;

        string result = string.Empty;
        do
        {
            display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero) break;

            int[] visualAttribs = {
            4, // GLX_RGBA
            8, // GLX_RED_SIZE
            8, // GLX_GREEN_SIZE
            8, // GLX_BLUE_SIZE
            8, // GLX_ALPHA_SIZE
            0  // None
        };

            visual = glXChooseVisual(display, 0, visualAttribs);
            if (visual == IntPtr.Zero) break;

            IntPtr rootWindow = XDefaultRootWindow(display);
            window = XCreateSimpleWindow(display, rootWindow, 0, 0, 1, 1, 0, UIntPtr.Zero, UIntPtr.Zero);
            if (window == IntPtr.Zero) break;

            context = glXCreateContext(display, visual, IntPtr.Zero, true);
            if (context == IntPtr.Zero) break;

            if (!glXMakeCurrent(display, window, context)) break;

            IntPtr versionPtr = glGetString(0x1F02); // GL_VERSION
            if (versionPtr != IntPtr.Zero)
                result = Marshal.PtrToStringAnsi(versionPtr);

            break;
        }
        while (true);

        if (context != IntPtr.Zero)
        {
            glXMakeCurrent(display, IntPtr.Zero, IntPtr.Zero);
            glXDestroyContext(display, context);
        }
        if (window != IntPtr.Zero) XDestroyWindow(display, window);
        if (visual != IntPtr.Zero) XFree(visual);
        if (display != IntPtr.Zero) XCloseDisplay(display);

        return result;
    }

    public static string GetOpenGLESVersionString()
    {
        IntPtr display = IntPtr.Zero;
        IntPtr window = IntPtr.Zero;
        IntPtr context = IntPtr.Zero;

        string result = string.Empty;
        do
        {
            display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero) break;

            int[] fbAttribs = {
            0x8010, 0x0001, // GLX_DRAWABLE_TYPE, GLX_WINDOW_BIT
            0x8012, 0x0001, // GLX_RENDER_TYPE, GLX_RGBA_BIT
            0x0008, 8,      // GLX_RED_SIZE, 8
            0x0009, 8,      // GLX_GREEN_SIZE, 8
            0x000A, 8,      // GLX_BLUE_SIZE, 8
            0               // None
        };

            IntPtr fbConfigs = glXChooseFBConfig(display, 0, fbAttribs, out int nitems);
            if (fbConfigs == IntPtr.Zero || nitems == 0) break;

            // Get the first FBConfig pointer from the array
            IntPtr fbConfig = Marshal.ReadIntPtr(fbConfigs);
            XFree(fbConfigs);

            IntPtr rootWindow = XDefaultRootWindow(display);
            window = XCreateSimpleWindow(display, rootWindow, 0, 0, 1, 1, 0, UIntPtr.Zero, UIntPtr.Zero);
            if (window == IntPtr.Zero) break;

            IntPtr pCreateContextAttribs = glXGetProcAddress("glXCreateContextAttribsARB");
            if (pCreateContextAttribs == IntPtr.Zero) break;

            var createContextAttribs = Marshal.GetDelegateForFunctionPointer<glXCreateContextAttribsARBDelegate>(pCreateContextAttribs);

            // Attempt OpenGLES 3.x first, fallback to 2.x
            int[] contextAttribs = {
            0x2091, 3, // GLX_CONTEXT_MAJOR_VERSION_ARB
            0x2092, 0, // GLX_CONTEXT_MINOR_VERSION_ARB
            0x9126, 0x00000004, // GLX_CONTEXT_PROFILE_MASK_ARB, GLX_CONTEXT_ES2_PROFILE_BIT_EXT
            0 // None
        };

            context = createContextAttribs(display, fbConfig, IntPtr.Zero, true, contextAttribs);
            if (context == IntPtr.Zero)
            {
                contextAttribs[1] = 2; // Try ES 2.0
                context = createContextAttribs(display, fbConfig, IntPtr.Zero, true, contextAttribs);
                if (context == IntPtr.Zero) break;
            }

            if (!glXMakeCurrent(display, window, context)) break;

            try
            {
                IntPtr versionPtr = glGetString(0x1F02); // GL_VERSION
                if (versionPtr != IntPtr.Zero)
                    result = Marshal.PtrToStringAnsi(versionPtr);
            }
            catch (Exception)
            {
                break;
            }

            break;
        }
        while (true);

        if (context != IntPtr.Zero)
        {
            glXMakeCurrent(display, IntPtr.Zero, IntPtr.Zero);
            glXDestroyContext(display, context);
        }
        if (window != IntPtr.Zero) XDestroyWindow(display, window);
        if (display != IntPtr.Zero) XCloseDisplay(display);

        return result;
    }
}
