using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MapsterExtensions.Generator.Helpers;

internal readonly record struct DiagnosticInfo(
    DiagnosticDescriptor Descriptor,
    LocationInfo Location,
    EquatableMessageArgs MessageArgs)
{
    internal static DiagnosticInfo Create(DiagnosticDescriptor descriptor, SyntaxToken token, object? arg0)
        => new(descriptor, LocationInfo.From(token), new EquatableMessageArgs([arg0]));

    internal Diagnostic CreateDiagnostic()
        => MessageArgs.Args.IsDefaultOrEmpty
            ? Diagnostic.Create(Descriptor, Location.ToLocation())
            : Diagnostic.Create(Descriptor, Location.ToLocation(), MessageArgs.Args.ToArray());
}

internal readonly record struct LocationInfo(string Path, TextSpan Span, LinePositionSpan LineSpan)
{
    private static LocationInfo From(Location location)
    {
        var mapped = location.GetMappedLineSpan();
        var path = string.IsNullOrEmpty(mapped.Path)
            ? location.SourceTree?.FilePath ?? string.Empty
            : mapped.Path;
        return new LocationInfo(path, location.SourceSpan, mapped.Span);
    }

    internal static LocationInfo From(SyntaxNode node) => From(node.GetLocation());
    internal static LocationInfo From(SyntaxToken token) => From(token.GetLocation());

    internal Location ToLocation() => Location.Create(Path, Span, LineSpan);
}

internal readonly record struct EquatableMessageArgs(ImmutableArray<object?> Args)
{
    public bool Equals(EquatableMessageArgs other)
    {
        if (Args.IsDefault && other.Args.IsDefault) return true;
        if (Args.IsDefault || other.Args.IsDefault) return false;
        if (Args.Length != other.Args.Length) return false;

        for (var i = 0; i < Args.Length; i++)
        {
            if (!Equals(Args[i], other.Args[i])) return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        if (Args.IsDefault) return 0;

        unchecked
        {
            var hash = 17;
            foreach (var arg in Args)
            {
                hash = hash * 31 + (arg?.GetHashCode() ?? 0);
            }

            return hash;
        }
    }
}