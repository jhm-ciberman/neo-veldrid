using System;
using System.Diagnostics;

namespace NeoVeldrid
{
    [DebuggerDisplay("{DebuggerDisplayString,nq}")]
    public struct Point : IEquatable<Point>
    {
        public int X;
        public int Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public readonly bool Equals(Point other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override readonly bool Equals(object obj) => obj is Point p && Equals(p);
        public override readonly int GetHashCode() => HashHelper.Combine(X.GetHashCode(), Y.GetHashCode());
        public override readonly string ToString() => $"({X}, {Y})";

        public static bool operator ==(Point left, Point right) => left.Equals(right);
        public static bool operator !=(Point left, Point right) => !left.Equals(right);

        private readonly string DebuggerDisplayString => ToString();
    }
}
