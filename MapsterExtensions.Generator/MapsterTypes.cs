using System;
using System.Collections.Generic;

namespace MapsterExtensions.Generator;

public readonly record struct TypeIdentity(string Fqn, string? Namespace, string Name)
{
    public static IEqualityComparer<TypeIdentity> EqualityComparer { get; } = new TypeIdentityEq();

    private sealed class TypeIdentityEq : IEqualityComparer<TypeIdentity>
    {
        public bool Equals(TypeIdentity x, TypeIdentity y) =>
            StringComparer.Ordinal.Equals(x.Fqn, y.Fqn);

        public int GetHashCode(TypeIdentity obj) =>
            StringComparer.Ordinal.GetHashCode(obj.Fqn);
    }
}

public readonly record struct TypePair(TypeIdentity Source, TypeIdentity Destination)
{
    public static IEqualityComparer<TypePair> EqualityComparer { get; } = new TypePairEq();

    private sealed class TypePairEq : IEqualityComparer<TypePair>
    {
        public bool Equals(TypePair x, TypePair y) =>
            StringComparer.Ordinal.Equals(x.Source.Fqn, y.Source.Fqn) &&
            StringComparer.Ordinal.Equals(x.Destination.Fqn, y.Destination.Fqn);

        public int GetHashCode(TypePair obj)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(obj.Source.Fqn);
                hash = hash * 31 + StringComparer.Ordinal.GetHashCode(obj.Destination.Fqn);
                return hash;
            }
        }
    }
}