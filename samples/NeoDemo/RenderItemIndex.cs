using System;
using System.Diagnostics;

namespace NeoVeldrid.NeoDemo
{
    public struct RenderItemIndex : IComparable<RenderItemIndex>, IComparable
    {
        public RenderOrderKey Key { get; }
        public int ItemIndex { get; }

        public RenderItemIndex(RenderOrderKey key, int itemIndex)
        {
            Key = key;
            ItemIndex = itemIndex;
        }

        public readonly int CompareTo(object obj)
        {
            return ((IComparable)Key).CompareTo(obj);
        }

        public readonly int CompareTo(RenderItemIndex other)
        {
            return Key.CompareTo(other.Key);
        }

        public override readonly string ToString()
        {
            return string.Format("Index:{0}, Key:{1}", ItemIndex, Key);
        }
    }
}
