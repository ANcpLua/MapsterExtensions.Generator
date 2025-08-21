# MapsterExtensions.Generator

Generate extension methods from your Mapster mappings.

## Usage

```csharp
using Mapster;
using MapsterExtensions.Generator;

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
user.ToUserDto()
```

## License
This project is licensed under the [MIT License](LICENSE).
