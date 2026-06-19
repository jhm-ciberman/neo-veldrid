using System;
using System.Collections.Generic;
using NeoVeldrid;
using FontStashSharp.Interfaces;

namespace FontStashText;

public sealed class FontTextureManager : ITexture2DManager, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<Texture> _textures = new();

    public FontTextureManager(GraphicsDevice graphicsDevice) => _graphicsDevice = graphicsDevice;

    public object CreateTexture(int width, int height)
    {
        var desc = TextureDescription.Texture2D(
            (uint)width,
            (uint)height,
            1,
            1,
            PixelFormat.R8_G8_B8_A8_UNorm,
            TextureUsage.Sampled);
        var texture = _graphicsDevice.ResourceFactory.CreateTexture(desc);
        _textures.Add(texture);
        return texture;
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
                _graphicsDevice.UpdateTexture(
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

    public void Dispose()
    {
        foreach (Texture texture in _textures)
        {
            texture.Dispose();
        }
        _textures.Clear();
    }
}
