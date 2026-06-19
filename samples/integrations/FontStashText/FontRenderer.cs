using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using NeoVeldrid;
using NeoVeldrid.SPIRV;
using FontStashSharp;
using FontStashSharp.Interfaces;

namespace FontStashText;

public sealed class FontRenderer : IFontStashRenderer, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly FontTextureManager _textureManager;

    private Pipeline _pipeline;
    private CommandList _commandList;

    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;

    private DeviceBuffer _projectionMatrixBuffer;
    private ResourceLayout _projectionLayout;
    private ResourceSet _projectionSet;
    private Matrix4x4 _projectionMatrix;

    private ResourceLayout _textureLayout;
    private readonly Dictionary<Texture, ResourceSet> _textureSets = new();
    private Texture _currentTexture;
    private ResourceSet _currentTextureSet;

    private VertexPositionTextureColor[] _vertices = new VertexPositionTextureColor[2048];
    private ushort[] _indices = new ushort[3072];
    private int _vertexCount = 0;
    private int _indexCount = 0;

    public ITexture2DManager TextureManager => _textureManager;

    public FontRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _textureManager = new FontTextureManager(graphicsDevice);

        CreateResources();
    }

    public void Begin(CommandList commandList, int width, int height)
    {
        _commandList = commandList;
        _vertexCount = _indexCount = 0;
        _currentTexture = null;

        _projectionMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, 0, 1);
        _commandList.UpdateBuffer(_projectionMatrixBuffer, 0, ref _projectionMatrix);
    }

    public void Flush()
    {
        if (_vertexCount == 0 || _currentTexture == null) return;

        _commandList.UpdateBuffer(_vertexBuffer, 0, _vertices.AsSpan(0, _vertexCount));
        _commandList.UpdateBuffer(_indexBuffer, 0, _indices.AsSpan(0, _indexCount));

        _commandList.SetPipeline(_pipeline);
        _commandList.SetGraphicsResourceSet(0, _projectionSet);
        _commandList.SetGraphicsResourceSet(1, _currentTextureSet);
        _commandList.SetVertexBuffer(0, _vertexBuffer);
        _commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        _commandList.DrawIndexed((uint)_indexCount, 1, 0, 0, 0);

        _vertexCount = 0;
        _indexCount = 0;
    }

    public void Draw(object texture, Vector2 pos, System.Drawing.Rectangle? src, FSColor color, float rotation, Vector2 scale, float depth)
    {
        RgbaFloat vColor = new RgbaFloat(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        DrawQuad((Texture)texture, pos, src, vColor, rotation, scale, depth);
    }

    public void Dispose()
    {
        foreach (ResourceSet textureSet in _textureSets.Values)
        {
            textureSet.Dispose();
        }
        _textureSets.Clear();

        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _projectionMatrixBuffer.Dispose();
        _pipeline.Dispose();
        _projectionSet.Dispose();
        _projectionLayout.Dispose();
        _textureLayout.Dispose();
        _textureManager.Dispose();
    }

    private void CreateResources()
    {
        _vertexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)_vertices.Length * VertexPositionTextureColor.SizeInBytes,
            BufferUsage.VertexBuffer | BufferUsage.Dynamic));

        _indexBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(
            (uint)_indices.Length * sizeof(ushort),
            BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        _projectionMatrixBuffer = _graphicsDevice.ResourceFactory.CreateBuffer(
            new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        _projectionLayout = _graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "ProjectionBuffer",
                    ResourceKind.UniformBuffer,
                    ShaderStages.Vertex)));

        _textureLayout = _graphicsDevice.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(
                new ResourceLayoutElementDescription(
                    "SurfaceTexture",
                    ResourceKind.TextureReadOnly,
                    ShaderStages.Fragment),
                new ResourceLayoutElementDescription(
                    "SurfaceSampler",
                    ResourceKind.Sampler,
                    ShaderStages.Fragment)));

        _projectionSet = _graphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _projectionLayout,
            _projectionMatrixBuffer));

        Shader[] shaders = _graphicsDevice.ResourceFactory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(Shaders.VertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(Shaders.FragmentCode), "main"));

        GraphicsPipelineDescription pipelineDesc = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid,
                FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _projectionLayout, _textureLayout },
            ShaderSet = new ShaderSetDescription(new[]
            {
                new VertexLayoutDescription(
                    new VertexElementDescription(
                        "Position",
                        VertexElementSemantic.TextureCoordinate,
                        VertexElementFormat.Float3),

                    new VertexElementDescription(
                        "TexCoords",
                        VertexElementSemantic.TextureCoordinate,
                        VertexElementFormat.Float2),

                    new VertexElementDescription(
                        "Color",
                        VertexElementSemantic.TextureCoordinate,
                        VertexElementFormat.Float4))
            }, shaders),
            Outputs = _graphicsDevice.SwapchainFramebuffer.OutputDescription,
        };

        _pipeline = _graphicsDevice.ResourceFactory.CreateGraphicsPipeline(pipelineDesc);
    }

    private void DrawQuad(Texture texture, Vector2 pos, System.Drawing.Rectangle? src, RgbaFloat color, float rotation, Vector2 scale, float depth)
    {
        if (_currentTexture != texture)
        {
            Flush();
            _currentTexture = texture;
            _currentTextureSet = GetTextureResourceSet(texture);
        }

        if (_vertexCount + 4 > _vertices.Length || _indexCount + 6 > _indices.Length)
        {
            Flush();
        }

        float w = (src?.Width ?? (int)texture.Width) * scale.X;
        float h = (src?.Height ?? (int)texture.Height) * scale.Y;

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);

        Vector3 Transform(float x, float y) => new Vector3(
            pos.X + (x * cos - y * sin),
            pos.Y + (x * sin + y * cos), depth);

        Vector3 topLeft = Transform(0, 0);
        Vector3 topRight = Transform(w, 0);
        Vector3 bottomLeft = Transform(0, h);
        Vector3 bottomRight = Transform(w, h);

        Vector2 uv1 = Vector2.Zero;
        Vector2 uv2 = Vector2.One;
        if (src.HasValue)
        {
            uv1 = new Vector2(
                src.Value.X / (float)texture.Width,
                src.Value.Y / (float)texture.Height);
            uv2 = new Vector2(
                (src.Value.X + src.Value.Width) / (float)texture.Width,
                (src.Value.Y + src.Value.Height) / (float)texture.Height);
        }

        _vertices[_vertexCount + 0] = new VertexPositionTextureColor(topLeft, new Vector2(uv1.X, uv1.Y), color);
        _vertices[_vertexCount + 1] = new VertexPositionTextureColor(topRight, new Vector2(uv2.X, uv1.Y), color);
        _vertices[_vertexCount + 2] = new VertexPositionTextureColor(bottomLeft, new Vector2(uv1.X, uv2.Y), color);
        _vertices[_vertexCount + 3] = new VertexPositionTextureColor(bottomRight, new Vector2(uv2.X, uv2.Y), color);

        _indices[_indexCount + 0] = (ushort)(_vertexCount + 0);
        _indices[_indexCount + 1] = (ushort)(_vertexCount + 1);
        _indices[_indexCount + 2] = (ushort)(_vertexCount + 2);
        _indices[_indexCount + 3] = (ushort)(_vertexCount + 1);
        _indices[_indexCount + 4] = (ushort)(_vertexCount + 3);
        _indices[_indexCount + 5] = (ushort)(_vertexCount + 2);

        _vertexCount += 4;
        _indexCount += 6;
    }

    private ResourceSet GetTextureResourceSet(Texture texture)
    {
        if (!_textureSets.TryGetValue(texture, out ResourceSet textureSet))
        {
            textureSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
                new ResourceSetDescription(
                    _textureLayout,
                    texture,
                    _graphicsDevice.PointSampler));
            _textureSets[texture] = textureSet;
        }

        return textureSet;
    }

    private static class Shaders
    {
        public const string VertexCode = @"
        #version 450
        layout(set = 0, binding = 0) uniform ProjectionBuffer { mat4 Projection; };
        layout(location = 0) in vec3 Position;
        layout(location = 1) in vec2 TexCoords;
        layout(location = 2) in vec4 Color;
        layout(location = 0) out vec2 fsin_texCoords;
        layout(location = 1) out vec4 fsin_color;
        void main() {
            gl_Position = Projection * vec4(Position, 1);
            fsin_texCoords = TexCoords;
            fsin_color = Color;
        }";

        public const string FragmentCode = @"
        #version 450
        layout(set = 1, binding = 0) uniform texture2D SurfaceTexture;
        layout(set = 1, binding = 1) uniform sampler SurfaceSampler;
        layout(location = 0) in vec2 fsin_texCoords;
        layout(location = 1) in vec4 fsin_color;
        layout(location = 0) out vec4 fsout_color;
        void main() {
            fsout_color = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords) * fsin_color;
        }";
    }
}
