# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

In addition to SemVer defaults, an "Internal" section is used to denote changes that are not user facing but improve the maintenance state of the package.

## [Unreleased]

### Added

- [Core] `GraphicsDevice.IsDebugRequested` and `GraphicsDevice.IsDebugActive` properties to query whether debug mode was requested and whether the API's debug or validation facilities are actually active.
- [Core] `NeoVeldridMappedResourceException` (a `NeoVeldridException` subtype) thrown for mapped-resource errors, carrying the offending `Resource`/`Subresource`, so callers can catch and inspect them specifically.

### Changed

- [OpenGL] Binding a currently-mapped buffer as a vertex or index buffer now throws instead of producing undefined behavior.

### Removed

- [Core] Mobile-only `SwapchainSource.CreateAndroidSurface`, `SwapchainSource.CreateUIView`, and the `GraphicsDevice.CreateOpenGLES(GraphicsDeviceOptions, SwapchainDescription)` overload. NeoVeldrid targets desktop platforms only.

### Fixed

- [SDL2] `Sdl2Window.Title` now reflects the title the window was created with, instead of returning an empty string until it is next set.

### Internal

- Removed dead Android and iOS code paths left over from the Silk.NET port.
- Simplified and improved the `samples` directory structure in the repository to make it easier to read and navigate. 
- Fixed spelling issues across XML-docs.

## [1.1.0] - 2026-05-25

First minor release for NeoVeldrid. This release comes with a few tiny new features and a lot of bug fixes that improve the stability. Our goal with NeoVeldrid is to make it reliable and rock-solid to ship real high performance applications and games in dotnet.

A big portion of the fixes were ported from the work made by @TechPizzaDev in their fork [veldrid2](https://github.com/veldrid2/veldrid2), so thank you.

### Added

- [ImGui] `ImGuiRenderer` now accepts an `autoInit: false` constructor option and exposes a public `Initialize()` method, so callers can configure `ImGui.GetIO()` (docking flags, custom fonts, etc.) before the first ImGui frame is opened.
- [Core] Added `GraphicsDevice.IsDisposed` property, matching the `IsDisposed` already exposed by every device resource.
- [Core] Structured buffers can now be created combined with `VertexBuffer`, `IndexBuffer`, or `IndirectBuffer` usage, so a compute shader can fill a vertex, index, or indirect buffer directly (GPU-driven rendering).

### Fixed

- [Core] Missing validation for the unsupported `BufferUsage` combinations `Dynamic | StructuredBufferReadWrite` and `Dynamic | IndirectBuffer`.
- [OpenGL] Direct-state-access being under-detected on GL 4.5+ drivers that no longer expose the `GL_ARB_direct_state_access` extension string.
- [OpenGL] Independent blend being under-detected on hardware that exposes it through `GL_ARB_draw_buffers_blend` below GL 4.0.
- [ImGui] `ImGuiRenderer`'s projection-matrix upload bypassing the command list.
- [D3D11] `GraphicsDeviceFeatures.StructuredBuffer` reporting `true` on feature levels below 11_0, where D3D11 does not support structured buffers.
- [Core] `TextureViewDescription`'s range-and-format constructor ignoring its format argument and using the target texture's format instead.
- [Core] `GraphicsDevice.Dispose()` deadlocking or crashing when called more than once.
- [Core] `Texture.Dispose()` racing concurrent creation of the texture's default view, which could free the device resource while it was still being used.
- [SDL2] Fix `Sdl2WindowRegistry` race condition in the event-pump thread.
- [Vulkan] `CommandList` staging-resource tracking racing between submit and fence-completion, which could corrupt the tracking dictionary or leak staging resources.

### Internal

- [Tests] Added negative tests for the `CreateBuffer` usage-validation rules that lacked coverage (structured stride rules, structured + uniform, staging exclusivity, uniform size).
- [Vulkan] Replaced the internal `StackList` stack-buffer helper with `stackalloc`.
- Marked all non-mutating struct members `readonly` across the codebase.

## [1.0.0] - 2026-04-25

First release of NeoVeldrid. A maintained, drop-in replacement for [Veldrid](https://github.com/mellinoe/veldrid) with every native binding replaced by [Silk.NET](https://github.com/dotnet/Silk.NET). If you have a Veldrid project today, migrating is roughly a 5 minute find-and-replace. See the [Migration Guide](docs/articles/prologue/migration.md) for the exact steps.

### Breaking

- `Sdl2Window.SetMousePosition` no longer supports per-frame warp-cursor-back mouselook. Code using that pattern must switch to `CursorRelativeMode` + `MouseDelta`. See the [Migration Guide](docs/articles/prologue/migration.md#mouselook-with-setmouseposition).
- `BufferDescription.RawBuffer` has been renamed to `BufferDescription.UseTypedHlslBinding` and its default behavior flipped. Structured buffers now bind as `(RW)ByteAddressBuffer` on D3D11 by default, matching the HLSL that `NeoVeldrid.SPIRV` generates from GLSL. Users binding hand-written HLSL that declares its storage buffers as `(RW)StructuredBuffer<T>` must set `UseTypedHlslBinding = true`.

### Changed

- Vulkan backend now binds through `Silk.NET.Vulkan` 2.23.0 (was `Vk` 1.0.25).
- D3D11 backend now binds through `Silk.NET.Direct3D11` 2.23.0 (was `Vortice.Direct3D11` 2.4.2).
- OpenGL and OpenGL ES backends now bind through `Silk.NET.OpenGL` 2.23.0 (was the custom `Veldrid.OpenGLBindings`).
- Windowing now goes through `Silk.NET.SDL` 2.23.0 instead of a hand-rolled SDL2 P/Invoke layer.
- SPIRV cross-compilation is now pure C# via `Silk.NET.SPIRV.Cross` + `Silk.NET.Shaderc`. There is no `libveldrid-spirv` native binary to build or ship anymore.
- `ImageSharp` bumped from 1.x to 3.x.
- Target framework is now `net10.0` (was `netstandard2.0`).
- Root namespace renamed from `Veldrid` to `NeoVeldrid`.

### Added

- macOS support through Vulkan + MoltenVK, Apple Silicon included. No extra setup, MoltenVK is bundled automatically.
- Linux native libraries are now bundled inside the NuGet package. No more chasing down system `libSDL2.so` or building native binaries yourself.

### Removed

- Metal backend. macOS is now covered by Vulkan via MoltenVK.
- `Sdl2Native` static class. Reach the underlying SDL API through `Sdl2Window.SdlInstance` instead.
- `Veldrid.VirtualReality` project. There was no VR hardware available for testing, so it would have rotted into dead code.
- Android and iOS sample projects. The core library still works on those platforms, but the samples are unmaintained. Contributions welcome.
- UWP swapchain support (`SwapchainSource.CreateUwp` and the `ISwapChainPanelNative`-taking `GraphicsDevice.CreateD3D11` overload). Microsoft deprecated UWP in 2023.

### Fixed

- [D3D11] Mipmap sampling being silently wrong at non-zero mip levels, caused by a struct layout mismatch in the old Vortice bindings.
- [Vulkan] Validation mode crashing the whole process on the first validation message, because the debug callback was throwing a managed exception back into unmanaged code.
- [Vulkan] `PixelFormat.R16_G16_Float` and `PixelFormat.R32_G32_Float` working incorrectly.
- [Vulkan] `GraphicsDeviceOptions.SwapchainSrgbFormat` being silently ignored by `CreateWindowAndGraphicsDevice`, so Vulkan swapchains always came back in the linear-UNorm format regardless of what the user requested.
- [Vulkan] Fixes memory leak with `VkDescriptorPoolManager` related to unfreed dynamic buffers.
- [Vulkan] Fixes `CreateLogicalDevice` ignoring the present queue family on GPUs where it differs from the graphics family.
- [SDL2] Scroll-to-zoom now respects sub-detent deltas from precision touchpads and high-end mice. Slow scrolls no longer round to zero.
- [SDL2] Cursor position no longer desyncs when the window regains focus or the pointer re-enters the window on Windows. Modern SDL2 emits zero-delta motion events in those cases, and the previous filter discarded them too aggressively.
- [Samples] The AnimatedMesh sample now renders correctly on OpenGL and OpenGL ES.
- [Samples] The ComputeParticles sample now renders correctly on D3D11.

[Unreleased]: https://github.com/jhm-ciberman/neo-veldrid/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/jhm-ciberman/neo-veldrid/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/jhm-ciberman/neo-veldrid/releases/tag/v1.0.0
