using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace NeoVeldrid.Tests
{
    public abstract class DisposalTestBase<T> : GraphicsDeviceTestBase<T> where T : GraphicsDeviceCreator
    {
        [Fact]
        public void Dispose_Buffer()
        {
            DeviceBuffer b = RF.CreateBuffer(new BufferDescription(256, BufferUsage.VertexBuffer));
            b.Dispose();
            Assert.True(b.IsDisposed);
        }

        [Fact]
        public void Dispose_Texture()
        {
            Texture t = RF.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.Sampled));
            TextureView tv = RF.CreateTextureView(t);
            GD.WaitForIdle(); // Required currently by Vulkan backend.
            tv.Dispose();
            Assert.True(tv.IsDisposed);
            Assert.False(t.IsDisposed);
            t.Dispose();
            Assert.True(t.IsDisposed);
        }

        [Fact]
        public void Dispose_Framebuffer()
        {
            Texture t = RF.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget));
            Framebuffer fb = RF.CreateFramebuffer(new FramebufferDescription(null, t));
            GD.WaitForIdle(); // Required currently by Vulkan backend.
            fb.Dispose();
            Assert.True(fb.IsDisposed);
            Assert.False(t.IsDisposed);
            t.Dispose();
            Assert.True(t.IsDisposed);
        }

        [Fact]
        public void Dispose_CommandList()
        {
            CommandList cl = RF.CreateCommandList();
            cl.Dispose();
            Assert.True(cl.IsDisposed);
        }

        [Fact]
        public void Dispose_Sampler()
        {
            Sampler s = RF.CreateSampler(SamplerDescription.Point);
            s.Dispose();
            Assert.True(s.IsDisposed);
        }

        [Fact]
        public void Dispose_Pipeline()
        {
            Shader[] shaders = TestShaders.LoadVertexFragment(RF, "UIntVertexAttribs");
            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new VertexLayoutDescription[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("Color_UInt", VertexElementSemantic.TextureCoordinate, VertexElementFormat.UInt4))
                },
                shaders);

            ResourceLayout layout = RF.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("InfoBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Ortho", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            GraphicsPipelineDescription gpd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.Default,
                PrimitiveTopology.PointList,
                shaderSet,
                layout,
                new OutputDescription(null, new OutputAttachmentDescription(PixelFormat.R32_G32_B32_A32_Float)));
            Pipeline pipeline = RF.CreateGraphicsPipeline(ref gpd);
            pipeline.Dispose();
            Assert.True(pipeline.IsDisposed);
            Assert.False(shaders[0].IsDisposed);
            Assert.False(shaders[1].IsDisposed);
            Assert.False(layout.IsDisposed);
            layout.Dispose();
            Assert.True(layout.IsDisposed);
            Assert.False(shaders[0].IsDisposed);
            Assert.False(shaders[1].IsDisposed);
            shaders[0].Dispose();
            Assert.True(shaders[0].IsDisposed);
            shaders[1].Dispose();
            Assert.True(shaders[1].IsDisposed);
        }

        // Contract for every backend: a Pipeline keeps its shaders alive for its own lifetime,
        // so disposing the Shader objects after the Pipeline is built leaves the Pipeline fully
        // usable. The backends satisfy this differently (Vulkan consumes the shader module at
        // pipeline creation, D3D11 must hold a COM reference, OpenGL defers shader deletion), but
        // the observable behavior must be identical: bind the pipeline and render after the
        // shaders are gone, and the output is still correct.
        [Fact]
        public void DisposeShaders_AfterPipelineCreation_PipelineStillRenders()
        {
            const uint width = 4;
            const uint height = 4;
            using Texture output = RF.CreateTexture(TextureDescription.Texture2D(
                width, height, 1, 1, PixelFormat.R32_G32_B32_A32_Float, TextureUsage.RenderTarget));
            using Framebuffer framebuffer = RF.CreateFramebuffer(new FramebufferDescription(null, output));

            float yMod = GD.IsClipSpaceYInverted ? -1.0f : 1.0f;
            ColoredVertex[] vertices =
            {
                new ColoredVertex { Position = new Vector2(-1, 1 * yMod), Color = Vector4.One },
                new ColoredVertex { Position = new Vector2(1, 1 * yMod), Color = Vector4.One },
                new ColoredVertex { Position = new Vector2(-1, -1 * yMod), Color = Vector4.One },
                new ColoredVertex { Position = new Vector2(1, -1 * yMod), Color = Vector4.One },
            };
            uint vertexSize = (uint)Unsafe.SizeOf<ColoredVertex>();
            using DeviceBuffer buffer = RF.CreateBuffer(new BufferDescription(
                vertexSize * (uint)vertices.Length, BufferUsage.StructuredBufferReadOnly, vertexSize));
            GD.UpdateBuffer(buffer, 0, vertices);

            using ResourceLayout graphicsLayout = RF.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("InputVertices", ResourceKind.StructuredBufferReadOnly, ShaderStages.Vertex)));
            using ResourceSet graphicsSet = RF.CreateResourceSet(new ResourceSetDescription(graphicsLayout, buffer));

            Shader[] shaders = TestShaders.LoadVertexFragment(RF, "ColoredQuadRenderer");
            using Pipeline pipeline = RF.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleStrip,
                new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), shaders),
                graphicsLayout,
                framebuffer.OutputDescription));

            // Dispose the shaders while the pipeline that was built from them is still alive.
            foreach (Shader shader in shaders)
            {
                shader.Dispose();
                Assert.True(shader.IsDisposed);
            }

            using CommandList cl = RF.CreateCommandList();
            cl.Begin();
            cl.SetFramebuffer(framebuffer);
            cl.ClearColorTarget(0, RgbaFloat.Clear);
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, graphicsSet);
            cl.Draw((uint)vertices.Length);
            cl.End();
            GD.SubmitCommands(cl);
            GD.WaitForIdle();

            using Texture readback = GetReadback(output);
            MappedResourceView<RgbaFloat> readView = GD.Map<RgbaFloat>(readback, MapMode.Read);
            for (uint y = 0; y < height; y++)
                for (uint x = 0; x < width; x++)
                {
                    Assert.Equal(new RgbaFloat(1, 1, 1, 1), readView[x, y]);
                }
            GD.Unmap(readback);
        }

        // Contract for every backend: a Texture must not be disposed while a Framebuffer that
        // targets it is still in use. Using such a Framebuffer is caught and reported the same way
        // on every backend (D3D11 would otherwise silently tolerate it because the runtime keeps
        // the resource alive through the render-target view, while Vulkan and OpenGL destroy it).
        [Fact]
        public void UseFramebuffer_AfterColorTargetDisposed_Throws()
        {
            Texture target = RF.CreateTexture(TextureDescription.Texture2D(
                4, 4, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget));
            Framebuffer framebuffer = RF.CreateFramebuffer(new FramebufferDescription(null, target));

            GD.WaitForIdle(); // Required currently by Vulkan backend.
            target.Dispose();
            Assert.True(target.IsDisposed);

            CommandList cl = RF.CreateCommandList();
            cl.Begin();
            var ex = Assert.Throws<NeoVeldridDisposedResourceException>(() => cl.SetFramebuffer(framebuffer));
            Assert.Same(target, ex.Resource);
        }

        // The same contract through a TextureView: a Texture must not be disposed while a ResourceSet
        // binds a view of it. D3D11 keeps the resource alive through the view's SRV and would
        // otherwise sample a logically-dead texture; Vulkan and OpenGL destroy it outright.
        [Fact]
        public void BindResourceSet_AfterSampledTextureDisposed_Throws()
        {
            Texture sampled = RF.CreateTexture(TextureDescription.Texture2D(
                4, 4, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            TextureView view = RF.CreateTextureView(sampled);

            ResourceLayout layout = RF.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Input", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("InputSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            ResourceSet set = RF.CreateResourceSet(new ResourceSetDescription(layout, view, GD.PointSampler));

            Pipeline pipeline = RF.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleStrip,
                new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), TestShaders.LoadVertexFragment(RF, "FullScreenBlit")),
                layout,
                new OutputDescription(null, new OutputAttachmentDescription(PixelFormat.R8_G8_B8_A8_UNorm))));

            GD.WaitForIdle(); // Required currently by Vulkan backend.
            sampled.Dispose();
            Assert.True(sampled.IsDisposed);

            CommandList cl = RF.CreateCommandList();
            cl.Begin();
            cl.SetPipeline(pipeline);
            var ex = Assert.Throws<NeoVeldridDisposedResourceException>(() => cl.SetGraphicsResourceSet(0, set));
            Assert.Same(view, ex.Resource);
        }

        [Fact]
        public void BindResourceSet_AfterBufferDisposed_Throws()
        {
            DeviceBuffer buffer = RF.CreateBuffer(new BufferDescription(64, BufferUsage.StructuredBufferReadOnly, 16));

            ResourceLayout layout = RF.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("InputVertices", ResourceKind.StructuredBufferReadOnly, ShaderStages.Vertex)));
            ResourceSet set = RF.CreateResourceSet(new ResourceSetDescription(layout, buffer));

            Pipeline pipeline = RF.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleStrip,
                new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), TestShaders.LoadVertexFragment(RF, "ColoredQuadRenderer")),
                layout,
                new OutputDescription(null, new OutputAttachmentDescription(PixelFormat.R8_G8_B8_A8_UNorm))));

            GD.WaitForIdle(); // Required currently by Vulkan backend.
            buffer.Dispose();
            Assert.True(buffer.IsDisposed);

            CommandList cl = RF.CreateCommandList();
            cl.Begin();
            cl.SetPipeline(pipeline);
            var ex = Assert.Throws<NeoVeldridDisposedResourceException>(() => cl.SetGraphicsResourceSet(0, set));
            Assert.Same(buffer, ex.Resource);
        }

        [Fact]
        public void BindResourceSet_AfterSamplerDisposed_Throws()
        {
            Texture sampled = RF.CreateTexture(TextureDescription.Texture2D(
                4, 4, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            TextureView view = RF.CreateTextureView(sampled);
            Sampler sampler = RF.CreateSampler(SamplerDescription.Point);

            ResourceLayout layout = RF.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Input", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("InputSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
            ResourceSet set = RF.CreateResourceSet(new ResourceSetDescription(layout, view, sampler));

            Pipeline pipeline = RF.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.CullNone,
                PrimitiveTopology.TriangleStrip,
                new ShaderSetDescription(Array.Empty<VertexLayoutDescription>(), TestShaders.LoadVertexFragment(RF, "FullScreenBlit")),
                layout,
                new OutputDescription(null, new OutputAttachmentDescription(PixelFormat.R8_G8_B8_A8_UNorm))));

            GD.WaitForIdle(); // Required currently by Vulkan backend.
            sampler.Dispose();
            Assert.True(sampler.IsDisposed);

            CommandList cl = RF.CreateCommandList();
            cl.Begin();
            cl.SetPipeline(pipeline);
            var ex = Assert.Throws<NeoVeldridDisposedResourceException>(() => cl.SetGraphicsResourceSet(0, set));
            Assert.Same(sampler, ex.Resource);
        }

        [Fact]
        public void Dispose_ResourceSet()
        {
            ResourceLayout layout = RF.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("InfoBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("Ortho", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            DeviceBuffer ub0 = RF.CreateBuffer(new BufferDescription(256, BufferUsage.UniformBuffer));
            DeviceBuffer ub1 = RF.CreateBuffer(new BufferDescription(256, BufferUsage.UniformBuffer));

            ResourceSet rs = RF.CreateResourceSet(new ResourceSetDescription(layout, ub0, ub1));
            rs.Dispose();
            Assert.True(rs.IsDisposed);
            Assert.False(ub0.IsDisposed);
            Assert.False(ub1.IsDisposed);
            Assert.False(layout.IsDisposed);
            layout.Dispose();
            Assert.True(layout.IsDisposed);
            Assert.False(ub0.IsDisposed);
            Assert.False(ub1.IsDisposed);
            ub0.Dispose();
            Assert.True(ub0.IsDisposed);
            ub1.Dispose();
            Assert.True(ub1.IsDisposed);
        }
    }

#if TEST_VULKAN
    [Trait("Backend", "Vulkan")]
    public class VulkanDisposalTests : DisposalTestBase<VulkanDeviceCreator> { }
#endif
#if TEST_D3D11
    [Trait("Backend", "D3D11")]
    public class D3D11DisposalTests : DisposalTestBase<D3D11DeviceCreator> { }
#endif
#if TEST_OPENGL
    [Trait("Backend", "OpenGL")]
    public class OpenGLDisposalTests : DisposalTestBase<OpenGLDeviceCreator> { }
#endif
#if TEST_OPENGLES
    [Trait("Backend", "OpenGLES")]
    public class OpenGLESDisposalTests : DisposalTestBase<OpenGLESDeviceCreator> { }
#endif
}
