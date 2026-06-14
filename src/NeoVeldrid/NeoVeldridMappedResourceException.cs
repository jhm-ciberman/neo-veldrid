namespace NeoVeldrid
{
    /// <summary>
    /// Represents errors that occur while mapping a <see cref="DeviceBuffer"/> or <see cref="Texture"/> into
    /// CPU-accessible memory, such as a failed map, a conflicting map mode, or using a resource that is currently mapped.
    /// </summary>
    public class NeoVeldridMappedResourceException : NeoVeldridException
    {
        /// <summary>
        /// Gets the resource whose mapping state caused the error.
        /// </summary>
        public MappableResource Resource { get; }

        /// <summary>
        /// Gets the subresource involved in the error. This is 0 for buffers, which have no subresources.
        /// </summary>
        public uint Subresource { get; }

        private NeoVeldridMappedResourceException(string message, MappableResource resource, uint subresource) : base(message)
        {
            Resource = resource;
            Subresource = subresource;
        }

        internal static NeoVeldridMappedResourceException Mapped(MappableResource resource, uint subresource)
        {
            return new($"The {Describe(resource, subresource)} is mapped.", resource, subresource);
        }

        internal static NeoVeldridMappedResourceException NotMapped(MappableResource resource, uint subresource)
        {
            return new($"The {Describe(resource, subresource)} is not mapped.", resource, subresource);
        }

        internal static NeoVeldridMappedResourceException MapFailed(MappableResource resource, uint subresource)
        {
            return new($"Failed to map the {Describe(resource, subresource)}.", resource, subresource);
        }

        internal static NeoVeldridMappedResourceException ConflictingMode(MappableResource resource, uint subresource)
        {
            return new($"The {Describe(resource, subresource)} was already mapped with a different MapMode.", resource, subresource);
        }

        private static string Describe(MappableResource resource, uint subresource)
        {
            string kind = resource is Texture ? "texture" : "buffer";
            string name = (resource as DeviceResource)?.Name;
            string described = string.IsNullOrEmpty(name) ? kind : $"{kind} '{name}'";
            return subresource == 0 ? described : $"{described} (subresource {subresource})";
        }
    }
}
