namespace NeoVeldrid
{
    /// <summary>
    /// A resource which can be bound in a <see cref="ResourceSet"/> and used in a shader.
    /// See <see cref="DeviceBuffer"/>, <see cref="DeviceBufferRange"/>, <see cref="Texture"/>, <see cref="TextureView"/>
    /// and <see cref="Sampler"/>.
    /// </summary>
    public interface BindableResource
    {
        /// <summary>
        /// Whether this resource can currently be bound. Becomes <see langword="false"/> once it, or a resource it
        /// references, has been disposed.
        /// </summary>
        internal bool IsBindable { get; }
    }
}
