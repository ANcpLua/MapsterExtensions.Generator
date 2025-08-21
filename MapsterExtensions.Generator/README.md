# MapsterExtensions.Generator

Generate extension methods from your Mapster mappings.

## Installation

```bash
dotnet add package Mapster
dotnet add package MapsterExtensions.Generator
```

## Usage

```csharp
using Mapster;
using MapsterExtensions.Generator;

public class MappingConfig : IRegister
{
    [Generate]
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>();
    }
}
```

Register your configuration (if not already done):
```csharp
// Program.cs
TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
```

Build. Use the generated extensions:
```csharp
user.ToUserDto()
```

## License
This project is licensed under the [MIT License](LICENSE).

