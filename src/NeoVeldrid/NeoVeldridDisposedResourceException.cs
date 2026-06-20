namespace NeoVeldrid
{
    /// <summary>
    /// Thrown when a <see cref="Framebuffer"/> or <see cref="ResourceSet"/> is used after a resource it references has
    /// been disposed.
    /// </summary>
    public class NeoVeldridDisposedResourceException : NeoVeldridException
    {
        /// <summary>
        /// Gets the bound resource that caused the error: one that has been disposed, or that references a disposed resource.
        /// </summary>
        public BindableResource Resource { get; }

        private NeoVeldridDisposedResourceException(string message, BindableResource resource) : base(message)
        {
            Resource = resource;
        }

        internal static NeoVeldridDisposedResourceException FramebufferColorTarget(BindableResource resource, int index)
        {
            return new($"The {Describe(resource)} bound as color target {index} of the Framebuffer has been disposed.", resource);
        }

        internal static NeoVeldridDisposedResourceException FramebufferDepthTarget(BindableResource resource)
        {
            return new($"The {Describe(resource)} bound as the depth target of the Framebuffer has been disposed.", resource);
        }

        internal static NeoVeldridDisposedResourceException ResourceSetElement(BindableResource resource, uint slot, int element)
        {
            return new($"The {Describe(resource)} bound at element {element} of the ResourceSet in slot {slot} has been disposed or references a disposed resource.", resource);
        }
    }
}
