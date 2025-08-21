# MapsterExtensions.Generator
[![NuGet](https://img.shields.io/nuget/v/MapsterExtensions.Generator.svg)](https://www.nuget.org/packages/MapsterExtensions.Generator)

Generate extension methods from your Mapster mappings.

## Usage

```csharp
using Mapster;

public class MappingConfig : IRegister
{
    [Generate] <--
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>();
    }
}
```

Build. Use the generated extensions:
```csharp
user.Adapt<UserDto>();    // meh

user.ToUserDto();         // nice, press T and Tab done
```

## License
This project is licensed under the [MIT License](LICENSE).
