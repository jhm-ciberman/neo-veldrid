namespace NeoVeldrid
{
    /// <summary>
    /// The specific graphics API used by the <see cref="GraphicsDevice"/>.
    /// </summary>
    public enum GraphicsBackend : byte
    {
        /// <summary>
        /// The preferred backend for the executing platform. If the environment variable NEOVELDRID_BACKEND is
        /// set, the backend specified in that variable will be used.
        /// </summary>
        Preferred,

        /// <summary>
        /// Direct3D 11.
        /// </summary>
        Direct3D11,

        /// <summary>
        /// Vulkan.
        /// </summary>
        Vulkan,

        /// <summary>
        /// OpenGL.
        /// </summary>
        OpenGL,

        /// <summary>
        /// OpenGL ES.
        /// </summary>
        OpenGLES,
    }
}
