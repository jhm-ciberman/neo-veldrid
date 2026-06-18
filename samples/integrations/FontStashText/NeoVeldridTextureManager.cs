using System;
using NeoVeldrid;
using FontStashSharp.Interfaces;

namespace FontStashText;

public sealed class NeoVeldridTextureManager : ITexture2DManager
{
    private readonly GraphicsDevice _gd;

    public NeoVeldridTextureManager(GraphicsDevice gd) => _gd = gd;

    public object CreateTexture(int width, int height)
    {
        var desc = TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled);
        return _gd.ResourceFactory.CreateTexture(desc);
    }
    public System.Drawing.Point GetTextureSize(object texture)
    {
        var tex = (Texture)texture;
        return new System.Drawing.Point((int)tex.Width, (int)tex.Height);
    }
    public void SetTextureData(object texture, System.Drawing.Rectangle bounds, byte[] data)
    {
        var tex = (Texture)texture;
        unsafe
        {
            fixed (byte* pData = data)
            {
                _gd.UpdateTexture(
                    tex,
                    (IntPtr)pData,
                    (uint)data.Length,
                    (uint)bounds.X,
                    (uint)bounds.Y,
                    0,
                    (uint)bounds.Width,
                    (uint)bounds.Height,
                    1,
                    0,
                    0
                );
            }
        }
    }
}
