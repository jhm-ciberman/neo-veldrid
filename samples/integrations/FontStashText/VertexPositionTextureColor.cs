using System.Numerics;
using System.Runtime.InteropServices;
using NeoVeldrid;

namespace FontStashText;

[StructLayout(LayoutKind.Sequential)]
public struct VertexPositionTextureColor
{
    public const uint SizeInBytes = 36;

    public Vector3 Position;
    public Vector2 TexCoords;
    public RgbaFloat Color;

    public VertexPositionTextureColor(Vector3 position, Vector2 texCoords, RgbaFloat color)
    {
        Position = position;
        TexCoords = texCoords;
        Color = color;
    }
}
