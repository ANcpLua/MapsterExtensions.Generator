<div align="center">

# MapsterExtensions.Generator

**Mapster configs → fluent, type-safe `To{Destination}()` extension methods.**

<a href="https://dotnet.microsoft.com/">
  <img src="https://img.shields.io/badge/.NET_Standard-2.0-512BD4?logo=dotnet&logoColor=white" alt=".NET Standard 2.0">
</a>
<a href="https://learn.microsoft.com/dotnet/csharp/">
  <img src="https://img.shields.io/badge/C%23-Latest-239120?logo=csharp&logoColor=white" alt="C# Latest">
</a>
<a href="https://www.nuget.org/packages/MapsterExtensions.Generator/">
  <img src="https://img.shields.io/nuget/v/MapsterExtensions.Generator.svg?color=0AA06E&logo=nuget&logoColor=white" alt="NuGet">
</a>
<a href="LICENSE">
  <img src="https://img.shields.io/badge/License-MIT-0AA06E" alt="MIT License">
</a>

<br><br>

[At a Glance](#at-a-glance) · [Quick Start](#quick-start) · [Features](#features) · [Generated API](#generated-api) · [How It Works](#how-it-works) · [Usage](#usage) · [Diagnostics](#diagnostics) · [Project Layout](#project-layout) · [Troubleshooting](#troubleshooting)

</div>

---

## At a Glance

| | |
|---|---|
| Type | Roslyn incremental generator (build-time only) |
| Target | .NET Standard 2.0 (works across modern .NET/Framework toolchains) |
| Input | `[Generate]` on `IRegister.Register(TypeAdapterConfig config)` with `config.NewConfig<TSource, TDest>()` |
| Output | `To{Dest}` extension methods grouped per source type in the source namespace |
| Runtime cost | None — generated partial classes, no reflection |

---

## Quick Start

```bash
dotnet add package Mapster
dotnet add package MapsterExtensions.Generator
```

```csharp
using Mapster;
using MapsterExtensions.Generator;
using System.Reflection;

public class MappingConfig : IRegister
{
    [Generate]
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>();
        config.NewConfig<Order, OrderDto>();
    }
}

// Program.cs / Startup
TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
```

```csharp
var dto = user.ToUserDto();
var orderDto = order.ToOrderDto();
```

---

## Features

- **Type-safe, discoverable API**: IDE completion shows `ToCustomerDto()`, not `Adapt<T>()` guesswork.
- **Zero runtime overhead**: Build-time generation only; no reflection or runtime scanning.
- **Mapster-native**: Uses your existing `IRegister` + `config.NewConfig<TSource, TDest>()` registrations.
- **Namespace-friendly**: Extension classes live next to your source types for clean imports.
- **Incremental**: Re-generates only when inputs change to keep builds fast.

---

## Generated API

```csharp
// Input config
config.NewConfig<User, UserDto>();

// Emitted at build time
namespace Your.Namespace;

public static partial class UserExtensions
{
    public static Your.Namespace.UserDto ToUserDto(this Your.Namespace.User source)
        => source.Adapt<Your.Namespace.UserDto>();
}
```

---

## How It Works

```mermaid
%%{init: {'theme': 'dark'}}%%
flowchart LR
    A[Incremental Generator] --> B{Find Generate methods}
    B --> C[Validate IRegister + Register]
    C --> D["Extract NewConfig‹TSource, TDest› pairs"]
    D --> E[Group by source type]
    E --> F["Emit To‹Dest› extension methods"]
```

## Usage

1. Add `[Generate]` to the `Register` method of an `IRegister` implementation.
2. Declare mappings with `config.NewConfig<TSource, TDest>()` inside that method.
3. Scan your assemblies once at startup with `TypeAdapterConfig.GlobalSettings.Scan(...)`.
4. Call the generated extensions anywhere you have the source type (`order.ToOrderDto()`).

Tips:
- Method signature must be `public void Register(TypeAdapterConfig config)` on an `IRegister` type.
- Mappings are grouped per source type; conflicting destination names raise diagnostics.
- Extension classes are `public static partial {Source}Extensions` in the source type’s namespace.

---

## Diagnostics

| Code   | Meaning / Fix |
|--------|---------------|
| ME0001 | `[Generate]` method is not in an `IRegister` class — implement `IRegister`. |
| ME0002 | Invalid signature — use `public void Register(TypeAdapterConfig config)`. |
| ME0003 | No `NewConfig<TSource, TDest>()` calls found — add at least one mapping. |
| ME0004 | Conflicting generated method name for a source type — rename destination or adjust mappings. |

---

## Project Layout

| File/Folder                  | Purpose |
|------------------------------|---------|
| MapsterExtensionGenerator.cs | Incremental generator: scans `[Generate]` methods, extracts mappings, emits extensions |
| Helpers/MapsterHelpers.cs    | Checks `IRegister`, validates signatures, collects `NewConfig` pairs and usings |
| MapsterTypes.cs              | `TypeIdentity` and `TypePair` models used during generation |
| MapsterRules.cs              | Diagnostic definitions (ME0001–ME0004) |
| Helpers/EquatableArray.cs    | Immutable array with value equality for generator caching |
| Helpers/DiagnosticInfo.cs    | Roslyn location/diagnostic helpers |

---

## Troubleshooting

- **No extensions appear**: Ensure `[Generate]` is on the `Register` method and the type implements `IRegister`.
- **ME0002/ME0003**: Check the method signature and that at least one `NewConfig<,>()` is declared.
- **Name conflicts**: If multiple destinations produce the same `To{Dest}` for a source, adjust destination names or mappings.

---

## License

This project is licensed under the [MIT License](LICENSE).
