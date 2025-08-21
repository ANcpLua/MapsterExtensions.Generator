using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using MapsterExtensions.Generator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MapsterExtensions.Generator;

[Generator]
public sealed class MapsterExtensionGenerator : IIncrementalGenerator
{
    private static class TrackingNames
    {
        public const string Transform = nameof(Transform);
        public const string Groups = nameof(Groups);
        public const string Diagnostics = nameof(Diagnostics);
    }

    private const string AttributeNamespace = "MapsterExtensions.Generator";
    private const string AttributeName = "Generate";
    private const string AttributeFullName = $"{AttributeNamespace}.{AttributeName}Attribute";

    private const string AttributeSourceCode = $$"""

                                                 #nullable enable
                                                 using System;

                                                 namespace {{AttributeNamespace}}
                                                 {
                                                     [Microsoft.CodeAnalysis.Embedded]
                                                     [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
                                                     internal sealed class {{AttributeName}}Attribute : Attribute
                                                     {
                                                     }
                                                 }
                                                 """;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(pi =>
        {
            pi.AddEmbeddedAttributeDefinition();
            pi.AddSource($"{AttributeName}Attribute.g.cs", SourceText.From(AttributeSourceCode, Encoding.UTF8));
        });

        var results = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeFullName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: Extract)
            .WithTrackingName(TrackingNames.Transform);

        var validBundles = results
            .Where(static r => r.Bundle.HasValue)
            .Select(static (r, _) => r.Bundle.GetValueOrDefault());

        var diagnostics = results
            .Where(static r => r.Diagnostic.HasValue)
            .Select(static (r, _) => r.Diagnostic.GetValueOrDefault())
            .WithTrackingName(TrackingNames.Diagnostics);

        var groups = validBundles
            .Collect()
            .Select(static (all, _) => GroupBySource(all))
            .SelectMany(static (arr, _) => arr.Items)
            .WithTrackingName(TrackingNames.Groups);

        context.RegisterSourceOutput(groups, static (spc, group) =>
        {
            if (HasDestinationNameCollision(in group, out var dupName))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    MapsterRules.ConflictingMethodNames,
                    group.Origin.ToLocation(),
                    group.Source.Fqn,
                    dupName));
                return;
            }

            spc.AddSource(group.HintName, SourceText.From(GenerateExtensions(in group), Encoding.UTF8));
        });

        context.RegisterSourceOutput(diagnostics, static (spc, d) =>
            spc.ReportDiagnostic(d.CreateDiagnostic()));
    }

    private static ExtractResult Extract(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        var node = (MethodDeclarationSyntax)ctx.TargetNode;
        var method = (IMethodSymbol)ctx.TargetSymbol;
        var containing = method.ContainingType;

        var compilation = ctx.SemanticModel.Compilation;
        var iregisterType = compilation.GetTypeByMetadataName("Mapster.IRegister");

        if (iregisterType is null)
        {
            return new ExtractResult(Diagnostic: DiagnosticInfo.Create(
                MapsterRules.InterfaceNotImplemented,
                node.Identifier,
                "Mapster"));
        }

        if (!MapsterHelpers.ImplementsIRegister(containing))
        {
            return new ExtractResult(Diagnostic: DiagnosticInfo.Create(
                MapsterRules.InterfaceNotImplemented,
                node.Identifier,
                containing.Name));
        }

        if (!MapsterHelpers.IsValidRegisterSignature(method))
        {
            return new ExtractResult(Diagnostic: DiagnosticInfo.Create(
                MapsterRules.IncorrectSignature,
                node.Identifier,
                null));
        }

        var pairs = MapsterHelpers.ExtractNewConfigPairs(node, ctx.SemanticModel, ct);
        if (pairs.IsDefaultOrEmpty)
        {
            return new ExtractResult(
                Diagnostic: DiagnosticInfo.Create(
                    MapsterRules.EmptyConfiguration,
                    node.Identifier,
                    method.Name));
        }

        return new ExtractResult(Bundle: new MethodBundle(
            Pairs: pairs,
            UsingNamespaces: MapsterHelpers.CollectUsingNamespaces(pairs),
            Origin: LocationInfo.From(node)));
    }

    private static EquatableArray<PerSourceGroup> GroupBySource(ImmutableArray<MethodBundle> bundles)
    {
        if (bundles.IsDefaultOrEmpty)
            return EquatableArray<PerSourceGroup>.Empty;

        var map = new Dictionary<TypeIdentity, GroupBuilder>(TypeIdentity.EqualityComparer);

        foreach (var bundle in bundles)
        {
            foreach (var pair in bundle.Pairs.Items)
            {
                if (!map.TryGetValue(pair.Source, out var builder))
                {
                    builder = new GroupBuilder(bundle.Origin);
                    map.Add(pair.Source, builder);
                }

                builder.AddDestination(pair.Destination);
                builder.AddUsings(bundle.UsingNamespaces.Items);
            }
        }

        var groups = ImmutableArray.CreateBuilder<PerSourceGroup>(map.Count);
        foreach (var kvp in map)
        {
            var source = kvp.Key;
            var builder = kvp.Value;
            var ns = source.Namespace ?? "Generated";

            groups.Add(new PerSourceGroup(
                Source: source,
                Namespace: ns,
                Destinations: builder.GetSortedDestinations(),
                UsingNamespaces: builder.GetSortedUsings(),
                HintName: MapsterHelpers.BuildHintName(ns, source),
                Origin: builder.Origin));
        }

        groups.Sort(PerSourceGroupComparer.Instance);
        return new EquatableArray<PerSourceGroup>(groups.ToImmutable());
    }

    private static string GenerateExtensions(in PerSourceGroup g)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using Mapster;");

        foreach (var ns in g.UsingNamespaces.Items)
        {
            if (ns is not "Mapster")
                sb.Append("using ").Append(ns).AppendLine(";");
        }

        sb.AppendLine();

        sb.Append("namespace ").AppendLine(g.Namespace);
        sb.AppendLine("{");
        sb.Append("    public static partial class ").Append(g.Source.Name).AppendLine("Extensions");
        sb.AppendLine("    {");

        foreach (var dest in g.Destinations.Items)
        {
            var methodName = "To" + dest.Name;
            sb.AppendLine("        /// <summary>");
            sb.Append("        /// Converts <see cref=\"").Append(g.Source.Fqn)
                .Append("\"/> to <see cref=\"").Append(dest.Fqn).AppendLine("\"/>.");
            sb.AppendLine("        /// </summary>");
            sb.Append("        public static ").Append(dest.Fqn).Append(' ').Append(methodName)
                .Append("(this ").Append(g.Source.Fqn).AppendLine(" source)");
            sb.Append("            => source.Adapt<").Append(dest.Fqn).AppendLine(">();");
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine("#nullable restore");

        return sb.ToString();
    }

    private static bool HasDestinationNameCollision(in PerSourceGroup g, out string duplicate)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dest in g.Destinations.Items)
        {
            if (!seen.Add(dest.Name))
            {
                duplicate = dest.Name;
                return true;
            }
        }

        duplicate = string.Empty;
        return false;
    }

    private readonly record struct ExtractResult(MethodBundle? Bundle = null, DiagnosticInfo? Diagnostic = null);

    private readonly record struct MethodBundle(
        EquatableArray<TypePair> Pairs,
        EquatableArray<string> UsingNamespaces,
        LocationInfo Origin);

    private readonly record struct PerSourceGroup(
        TypeIdentity Source,
        string Namespace,
        EquatableArray<TypeIdentity> Destinations,
        EquatableArray<string> UsingNamespaces,
        string HintName,
        LocationInfo Origin);

    private sealed class GroupBuilder
    {
        private readonly HashSet<TypeIdentity> _destinations = new(TypeIdentity.EqualityComparer);
        private readonly HashSet<string> _usings = new(StringComparer.Ordinal);

        public GroupBuilder(LocationInfo origin)
        {
            Origin = origin;
        }

        public LocationInfo Origin { get; }

        public void AddDestination(TypeIdentity dest) => _destinations.Add(dest);

        public void AddUsings(IEnumerable<string> usings)
        {
            foreach (var u in usings)
                _usings.Add(u);
        }

        public EquatableArray<TypeIdentity> GetSortedDestinations()
        {
            if (_destinations.Count is 0)
                return EquatableArray<TypeIdentity>.Empty;

            var array = new TypeIdentity[_destinations.Count];
            _destinations.CopyTo(array);
            Array.Sort(array, (x, y) =>
            {
                var nameComp = StringComparer.Ordinal.Compare(x.Name, y.Name);
                return nameComp is not 0 ? nameComp : StringComparer.Ordinal.Compare(x.Fqn, y.Fqn);
            });
            return new EquatableArray<TypeIdentity>([..array]);
        }

        public EquatableArray<string> GetSortedUsings()
        {
            if (_usings.Count is 0)
                return EquatableArray<string>.Empty;

            var array = new string[_usings.Count];
            _usings.CopyTo(array);
            Array.Sort(array, StringComparer.Ordinal);
            return new EquatableArray<string>([..array]);
        }
    }

    private sealed class PerSourceGroupComparer : IComparer<PerSourceGroup>
    {
        public static readonly PerSourceGroupComparer Instance = new();

        public int Compare(PerSourceGroup x, PerSourceGroup y)
        {
            var nsComp = StringComparer.Ordinal.Compare(x.Namespace, y.Namespace);
            return nsComp is not 0 ? nsComp : StringComparer.Ordinal.Compare(x.Source.Name, y.Source.Name);
        }
    }
}