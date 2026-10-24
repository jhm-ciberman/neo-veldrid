using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;

namespace NeoVeldrid;

/// <summary>
/// Creates Silk.NET native contexts that are safe to use in Native AOT and single-file apps.
/// </summary>
internal static class SilkNativeContext
{
    // Silk.NET's default path resolver relies on assembly file paths and the .deps.json file, which produces
    // AOT warnings that can't be suppressed downstream. The runtime's own native library probing is used
    // instead: it searches the app directory and the .deps.json native assets before the system locations.

    /// <summary>
    /// Loads the first native library that can be found among the names for the current platform.
    /// </summary>
    /// <param name="windows">The library names to try on Windows, in order.</param>
    /// <param name="linux">The library names to try on Linux and other Unix platforms, in order.</param>
    /// <param name="macOS">The library names to try on macOS, in order.</param>
    /// <returns>A native context for the loaded library.</returns>
    /// <exception cref="System.IO.FileNotFoundException">None of the libraries could be loaded.</exception>
    public static DefaultNativeContext Create(string[] windows, string[] linux, string[] macOS)
    {
        string[] names = OperatingSystem.IsWindows() ? windows : OperatingSystem.IsMacOS() ? macOS : linux;
        return new DefaultNativeContext(names, RuntimeProbingLoader.Instance, PassthroughPathResolver.Instance);
    }

    /// <inheritdoc cref="Create(string[], string[], string[])"/>
    public static DefaultNativeContext Create(string windows, string linux, string macOS)
    {
        return Create([windows], [linux], [macOS]);
    }

    private sealed class RuntimeProbingLoader : LibraryLoader
    {
        public static readonly RuntimeProbingLoader Instance = new();

        protected override nint CoreLoadNativeLibrary(string name)
        {
            return NativeLibrary.TryLoad(name, typeof(SilkNativeContext).Assembly, null, out nint handle) ? handle : 0;
        }

        protected override void CoreFreeNativeLibrary(nint handle)
        {
            NativeLibrary.Free(handle);
        }

        protected override nint CoreLoadFunctionPointer(nint handle, string functionName)
        {
            return NativeLibrary.TryGetExport(handle, functionName, out nint address) ? address : 0;
        }
    }

    private sealed class PassthroughPathResolver : PathResolver
    {
        public static readonly PassthroughPathResolver Instance = new();

        public override IEnumerable<string> EnumeratePossibleLibraryLoadTargets(string name)
        {
            return [name];
        }
    }
}
