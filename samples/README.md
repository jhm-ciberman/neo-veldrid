# Samples

Sample applications demonstrating Veldrid's graphics API. On Windows, samples default to D3D11. On other platforms, they default to Vulkan.

## Running

## Technique Samples (simplest to most complex)

Simple, focused samples demonstrating specific techniques or API usage patterns. These are a good starting point for learning how to use Veldrid.

| Sample | Description | Run command |
|--------|-------------|-------------|
| **GettingStarted** | Colored quad. Minimal Veldrid usage. | `dotnet run --project samples/techniques/GettingStarted/GettingStarted.csproj` |
| **ImageTint** | Compute shader that tints an image (headless). | `dotnet run --project samples/techniques/ImageTint/ImageTint.csproj -- <input.png> <output.png>` |
| **TexturedCube** | Textured 3D cube with depth buffer. | `dotnet run --project samples/techniques/TexturedCube/TexturedCube.csproj` |
| **Offscreen** | Render-to-texture using framebuffers. | `dotnet run --project samples/techniques/Offscreen/Offscreen.csproj` |
| **Instancing** | Instanced drawing with texture arrays. | `dotnet run --project samples/techniques/Instancing/Instancing.csproj` |
| **ComputeTexture** | Compute shader writing to a texture. | `dotnet run --project samples/techniques/ComputeTexture/ComputeTexture.csproj` |
| **ComputeParticles** | Compute shader particle simulation. | `dotnet run --project samples/techniques/ComputeParticles/ComputeParticles.csproj` |
| **AnimatedMesh** | Skeletal animation loaded via Assimp. | `dotnet run --project samples/techniques/AnimatedMesh/AnimatedMesh.csproj` |

## Full Demos

More complex, full-featured applications demonstrating multiple techniques and a complete rendering pipeline. These are useful for seeing how different techniques can be combined in a real application.

| Demo | Description | Run command |
|-----|-------------|-------------|
| **NeoDemo** | Full scene: Sponza atrium, shadow maps, reflections, ImGui overlay. | `dotnet run --project samples/demos/NeoDemo/NeoDemo.csproj` |

## Support Libraries

| Project | Description |
|---------|-------------|
| **SampleBase** | Shared window setup and render loop used by most samples. |
| **AssetPrimitives** | Serialization types for processed mesh/texture assets. |
| **AssetProcessor** | Converts raw assets (models, textures) into binary format for samples. |
| **Common** | Shared shader cross-compilation utilities. |
