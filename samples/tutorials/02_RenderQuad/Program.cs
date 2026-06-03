using System;
using System.Numerics;
using System.Text;
using NeoVeldrid.Sdl2;
using NeoVeldrid.SPIRV;
using NeoVeldrid.StartupUtilities;

namespace NeoVeldrid.Samples;

internal class Program
{
    private const string _vertex_shader_src = @"
#version 450

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;

layout(location = 0) out vec4 fsin_Color;

void main()
{
    gl_Position = vec4(Position, 0, 1);
    fsin_Color = Color;
}";
    private const string _fragment_shader_src = @"
#version 450

layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = fsin_Color;
}";

    private static Sdl2Window _window;
    private static GraphicsDevice _graphics_device;

    private static DeviceBuffer _vertex_buffer;
    private static DeviceBuffer _index_buffer;

    private static Shader[] _shaders;
    private static Pipeline _pipeline;
    private static CommandList _command_list;

    static void Main(string[] args)
    {
        WindowCreateInfo window_ci = new WindowCreateInfo()
        {
            X = 100,
            Y = 100,
            WindowWidth = 960,
            WindowHeight = 540,
            WindowTitle = "Part 2: Rendering A Square",
        };
        _window = NeoVeldridStartup.CreateWindow(ref window_ci);

        GraphicsDeviceOptions gd_options = new GraphicsDeviceOptions
        {
            PreferStandardClipSpaceYDirection = true,
            PreferDepthRangeZeroToOne = true,
        };
        _graphics_device = NeoVeldridStartup.CreateGraphicsDevice(_window, gd_options);

        CreateResources();

        while (_window.Exists)
        {
            _window.PumpEvents();

            if (_window.Exists)
                Draw();
        }

        DisposeResources();
    }

    private static void CreateResources()
    {
        ResourceFactory factory = _graphics_device.ResourceFactory;

        (Vector2, RgbaFloat)[] quad_vertices = {
            (new Vector2(-.75f, .75f), RgbaFloat.Red),
            (new Vector2(.75f, .75f), RgbaFloat.Green),
            (new Vector2(-.75f, -.75f), RgbaFloat.Blue),
            (new Vector2(.75f, -.75f), RgbaFloat.Yellow),
        };
        ushort[] quad_indices = { 0, 1, 2, 3 };

        int quad_vertices_size;
        unsafe
        {
            quad_vertices_size = sizeof((Vector2, RgbaFloat)) * quad_vertices.Length;
        }

        _vertex_buffer = factory.CreateBuffer(
            new BufferDescription((uint)quad_vertices_size, BufferUsage.VertexBuffer)
        );
        _index_buffer = factory.CreateBuffer(
            new BufferDescription((uint)quad_vertices_size, BufferUsage.IndexBuffer)
        );

        _graphics_device.UpdateBuffer(_vertex_buffer, 0, quad_vertices);
        _graphics_device.UpdateBuffer(_index_buffer, 0, quad_indices);

        VertexLayoutDescription vertex_layout_desc = new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4));
        ShaderDescription vertex_shader_desc = new ShaderDescription(
            ShaderStages.Vertex,
            Encoding.UTF8.GetBytes(_vertex_shader_src),
            "main");
        ShaderDescription fragment_shader_desc = new ShaderDescription(
            ShaderStages.Fragment,
            Encoding.UTF8.GetBytes(_fragment_shader_src),
            "main");
        _shaders = factory.CreateFromSpirv(vertex_shader_desc, fragment_shader_desc);

        GraphicsPipelineDescription pipe_desc = new GraphicsPipelineDescription()
        {
            BlendState = BlendStateDescription.SingleOverrideBlend,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(
                cullMode: FaceCullMode.Back,
                fillMode: PolygonFillMode.Solid,
                frontFace: FrontFace.Clockwise,
                depthClipEnabled: true,
                scissorTestEnabled: false),
            PrimitiveTopology = PrimitiveTopology.TriangleStrip,
            ResourceLayouts = Array.Empty<ResourceLayout>(),
            ShaderSet = new ShaderSetDescription(
                vertexLayouts: new VertexLayoutDescription[] { vertex_layout_desc },
                shaders: _shaders),
            Outputs = _graphics_device.SwapchainFramebuffer.OutputDescription,
        };

        _pipeline = factory.CreateGraphicsPipeline(pipe_desc);
        _command_list = factory.CreateCommandList();
    }

    private static void Draw()
    {
        _command_list.Begin();

        _command_list.SetFramebuffer(_graphics_device.SwapchainFramebuffer);
        _command_list.ClearColorTarget(0, RgbaFloat.Black);

        _command_list.SetVertexBuffer(0, _vertex_buffer);
        _command_list.SetIndexBuffer(_index_buffer, IndexFormat.UInt16);
        _command_list.SetPipeline(_pipeline);

        _command_list.DrawIndexed(
            indexCount: 4,
            instanceCount: 1,
            indexStart: 0,
            vertexOffset: 0,
            instanceStart: 0);

        _command_list.End();
        _graphics_device.SubmitCommands(_command_list);

        _graphics_device.SwapBuffers();
    }

    private static void DisposeResources()
    {
        _pipeline.Dispose();

        foreach (var shader in _shaders)
            shader.Dispose();

        _command_list.Dispose();
        _vertex_buffer.Dispose();
        _index_buffer.Dispose();
        _graphics_device.Dispose();
    }
}
