using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MapsterExtensions.Generator.Helpers;

internal static class MapsterHelpers
{
    private static readonly SymbolDisplayFormat Fqn = SymbolDisplayFormat.FullyQualifiedFormat;

    internal static bool ImplementsIRegister(INamedTypeSymbol type)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name is "IRegister" &&
                iface.ContainingNamespace.ToDisplayString() is "Mapster")
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsValidRegisterSignature(IMethodSymbol method)
    {
        if (method.Name != "Register" ||
            method.DeclaredAccessibility != Accessibility.Public ||
            !method.ReturnsVoid ||
            method.Parameters.Length != 1)
        {
            return false;
        }

        var param = method.Parameters[0];
        return param.Type.Name == "TypeAdapterConfig" &&
               param.Type.ContainingNamespace.ToDisplayString() == "Mapster";
    }

    internal static EquatableArray<TypePair> ExtractNewConfigPairs(
        MethodDeclarationSyntax node,
        SemanticModel model,
        CancellationToken ct)
    {
        var set = new HashSet<TypePair>(TypePair.EqualityComparer);

        foreach (var descendant in node.DescendantNodes())
        {
            ct.ThrowIfCancellationRequested();

            if (descendant is not InvocationExpressionSyntax invocation)
                continue;

            var generic = invocation.Expression switch
            {
                GenericNameSyntax { Identifier.Text: "NewConfig", TypeArgumentList.Arguments.Count: 2 } gns
                    => gns,
                MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax
                        {
                            Identifier.Text: "NewConfig", TypeArgumentList.Arguments.Count: 2
                        } gns2
                    }
                    => gns2,
                _ => null
            };

            if (generic is null)
                continue;

            var srcType = model.GetTypeInfo(generic.TypeArgumentList.Arguments[0], ct).Type;
            var dstType = model.GetTypeInfo(generic.TypeArgumentList.Arguments[1], ct).Type;

            if (srcType is null || dstType is null)
                continue;

            set.Add(new TypePair(ToIdentity(srcType), ToIdentity(dstType)));
        }

        if (set.Count is 0)
            return EquatableArray<TypePair>.Empty;

        var array = new TypePair[set.Count];
        set.CopyTo(array);
        Array.Sort(array, TypePairComparer.Instance);

        return new EquatableArray<TypePair>([..array]);
    }

    private static TypeIdentity ToIdentity(ITypeSymbol type)
        => new(
            Fqn: type.ToDisplayString(Fqn),
            Namespace: type.ContainingNamespace?.IsGlobalNamespace is true
                ? null
                : type.ContainingNamespace?.ToDisplayString(),
            Name: type.Name);

    internal static EquatableArray<string> CollectUsingNamespaces(EquatableArray<TypePair> pairs)
    {
        var set = new HashSet<string>(StringComparer.Ordinal) { "Mapster" };

        foreach (var pair in pairs.Items)
        {
            if (pair.Source.Namespace is { } sourceNs)
                set.Add(sourceNs);
            if (pair.Destination.Namespace is { } destNs)
                set.Add(destNs);
        }

        if (set.Count is 0)
            return EquatableArray<string>.Empty;

        var array = new string[set.Count];
        set.CopyTo(array);
        Array.Sort(array, StringComparer.Ordinal);

        return new EquatableArray<string>([..array]);
    }

    internal static string BuildHintName(string ns, in TypeIdentity source)
    {
        var sb = new StringBuilder(ns.Length + source.Name.Length + 32);
        foreach (var ch in ns)
            sb.Append(char.IsLetterOrDigit(ch) || ch is '.' ? ch : '_');
        sb.Append('.');
        foreach (var ch in source.Name)
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        sb.Append(".g.cs");
        return sb.ToString();
    }

    private sealed class TypePairComparer : IComparer<TypePair>
    {
        internal static readonly TypePairComparer Instance = new();

        public int Compare(TypePair x, TypePair y)
        {
            var sourceComparison = StringComparer.Ordinal.Compare(x.Source.Fqn, y.Source.Fqn);
            return sourceComparison is not 0
                ? sourceComparison
                : StringComparer.Ordinal.Compare(x.Destination.Fqn, y.Destination.Fqn);
        }
    }
}