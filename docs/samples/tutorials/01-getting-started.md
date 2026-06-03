---
uid: 01-getting-started
---

# Part 1: Getting Started

In this tutorial, we will walk through the basics of initializing a NeoVeldrid application: creating a new project, opening an SDL2 window, 
initializing a [GraphicsDevice](https://jhm-ciberman.github.io/neo-veldrid/api/NeoVeldrid.GraphicsDevice.html), and starting the main loop.

## Creating a Project
Before a project can be created, make sure that .NET 10 or later is installed on your computer. If not, stop here and 
[install the latest stable .NET SDK version](https://dotnet.microsoft.com/en-us/download/dotnet). To begin, create a new
console application and add the required NuGet packages.

```
dotnet new console -n 01_GettingStarted
cd 01_GettingStarted
dotnet add package NeoVeldrid
dotnet add package NeoVeldrid.SDL2
dotnet add package NeoVeldrid.StartupUtilities
```

If the packages fail to install, you can download them manually from the package registry: [NeoVeldrid](https://www.nuget.org/packages/NeoVeldrid), 
[NeoVeldrid.SDL2](https://www.nuget.org/packages/NeoVeldrid.SDL2), [NeoVeldrid.StartupUtilities](https://www.nuget.org/packages/NeoVeldrid.StartupUtilities).
Before continuing, build your project to make sure the required packages are installed correctly.

## Opening an SDL2 Window
Open your `Program.cs` file. At the beginning of the file, we need to import the namespaces that 
contain our window creation and startup logic. These are the portions of NeoVeldrid that we will 
be using in this tutorial.

```csharp
using NeoVeldrid.Sdl2;
using NeoVeldrid.StartupUtilities;
```

Inside of the `Program` class but outside of any functions, add the following private field:

```csharp
private static Sdl2Window _window;
```

This is the SDL2 window that we will be opening and should, in its scope, be usable by any function
within the `Program` class. Next, we need to define the required properties of our SDL2 window:

```csharp
WindowCreateInfo window_ci = new WindowCreateInfo()
{
    X = 100,
    Y = 100,
    WindowWidth = 960,
    WindowHeight = 540,
    WindowTitle = "Part 1: Getting Started",
};
```

This structure defines the position, size, and title of the window as it will exist at the moment of its
creation. These properties, especially position and size, will be changeable by the end user unless optional
properties are set to limit this behaviour. Next, we need to create the window using the required properties:

```csharp
_window = NeoVeldridStartup.CreateWindow(ref window_ci);
```

When the SDL2 window is created, the `_window` private field is populated. The `ref` keyword is used here to pass a
reference to `window_ci` instead of passing the entire structure itself, which can be more memory
efficient when large structures are used.

## Initializing a GraphicsDevice
Inside of the `Program` class but outside of any functions, add the following private field:

```csharp
private static GraphicsDevice _graphics_device;
```

This should be a single line below the `_window` private field. This is the object we will use in later 
tutorials to execute rendering commands. Below the window creation code, the following options should be defined:

```csharp
GraphicsDeviceOptions gd_options = new GraphicsDeviceOptions
{
    PreferStandardClipSpaceYDirection = true,
    PreferDepthRangeZeroToOne = true,
};
```
This should be right below the line of code where `_window` is populated. Both of these options deal with [clip space consistency](https://jhm-ciberman.github.io/neo-veldrid/articles/advanced/backend-differences.html#forcing-clip-space-consistency), 
which is beyond the scope of this tutorial. However, both options are important when we start rendering things to the SDL2 window.
Next, we need to create the graphics device using the provided options:

```csharp
_graphics_device = NeoVeldridStartup.CreateGraphicsDevice(_window, gd_options);
```

When the graphics device is created, the `_graphics_device` private field is populated. Each graphics device is paired to one window, which 
is why we needed to pass `_window` in the graphics device creation process.

## The Main Loop
The last step in this tutorial deals with the main loop, which executes rendering commands and other tasks that need to be performed every frame
of the application's lifespan. The loop will appear as follows:

```csharp
while (_window.Exists)
{
    _window.PumpEvents();
}
```

The loop's continuation is dependent on the existence of the window, so if you close the window the application ends. In each frame, the window needs
to poll and execute events, which is what lets the end user interact with it.

## [Next: Part 2](xref:02-render-square)

Here is what the application should look like at the end of this tutorial:

```csharp
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
```

The completed source code for this tutorial can be found [here](https://github.com/jhm-ciberman/neo-veldrid/tree/main/samples/tutorials/01_GettingStarted/).
