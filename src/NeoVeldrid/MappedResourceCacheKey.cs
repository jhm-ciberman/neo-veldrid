using System;

namespace NeoVeldrid
{
    internal struct MappedResourceCacheKey : IEquatable<MappedResourceCacheKey>
    {
        public readonly MappableResource Resource;
        public readonly uint Subresource;

        public MappedResourceCacheKey(MappableResource resource, uint subresource)
        {
            Resource = resource;
            Subresource = subresource;
        }

        public readonly bool Equals(MappedResourceCacheKey other)
        {
            return Resource.Equals(other.Resource)
                && Subresource.Equals(other.Subresource);
        }

        public override readonly int GetHashCode()
        {
            return HashHelper.Combine(Resource.GetHashCode(), Subresource.GetHashCode());
        }
    }
}
