using Microsoft.CodeAnalysis;

namespace MapsterExtensions.Generator;

public static class MapsterRules
{
    private const string Category = "Generate";

    public static readonly DiagnosticDescriptor InterfaceNotImplemented = new(
        id: "ME0001",
        title: "Missing IRegister interface",
        messageFormat: "Type '{0}' must implement Mapster.IRegister to use [Generate] attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Extension method generation requires the containing class to implement Mapster.IRegister.");

    public static readonly DiagnosticDescriptor IncorrectSignature = new(
        id: "ME0002",
        title: "Incorrect Register signature",
        messageFormat: "Method signature must be: public void Register(TypeAdapterConfig config)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Register method must accept a single TypeAdapterConfig parameter and return void.");

    public static readonly DiagnosticDescriptor EmptyConfiguration = new(
        id: "ME0003",
        title: "Empty mapping configuration",
        messageFormat: "No mappings found in '{0}' - add config.NewConfig<TSource, TDest>() calls",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "At least one type mapping is required to generate extension methods.");

    public static readonly DiagnosticDescriptor ConflictingMethodNames = new(
        id: "ME0004",
        title: "Method name conflict detected",
        messageFormat: "Cannot generate distinct methods for type '{0}' mapping to multiple '{1}' types",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Types with identical names from different namespaces create method name conflicts.");
}
