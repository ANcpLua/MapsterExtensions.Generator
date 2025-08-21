using Generator.Testing;

namespace MapsterExtensions.Generator.Tests;

public class MapsterExtensionFunctionalTests
{
    [Fact]
    public async Task Generate_SimpleMapping_CreatesExtensionFile()
    {
        const string source = """
                              using Mapster;
                              using MapsterExtensions.Generator;

                              namespace Mapster
                              {
                                  public interface IRegister { }
                                  public class TypeAdapterConfig 
                                  {
                                      public object NewConfig<TSource, TDestination>() => new object();
                                  }
                              }

                              namespace TestNamespace;

                              public class PersonRegister : IRegister
                              {
                                  [Generate]
                                  public void Register(TypeAdapterConfig config)
                                  {
                                      config.NewConfig<Person, PersonDto>();
                                  }
                              }

                              public class Person { }
                              public class PersonDto { }
                              """;

        await source.ShouldGenerate<MapsterExtensionGenerator>(
            "TestNamespace.Person.g.cs");
    }

    [Fact]
    public async Task Generate_IdenticalInput_ProperlyUsesCaching()
    {
        const string source = """
                              using Mapster;
                              using MapsterExtensions.Generator;

                              namespace Mapster
                              {
                                  public interface IRegister { }
                                  public class TypeAdapterConfig 
                                  {
                                      public object NewConfig<TSource, TDestination>() => new object();
                                  }
                              }

                              namespace TestNamespace;

                              public class CacheRegister : IRegister
                              {
                                  [Generate]
                                  public void Register(TypeAdapterConfig config)
                                  {
                                      config.NewConfig<Person, Dto>();
                                  }
                              }

                              public class Person { }
                              public class Dto { }
                              """;

        await source.ShouldCache<MapsterExtensionGenerator>();
    }

    [Fact]
    public async Task Generate_MultipleSources_CreatesExtensionFilePerSource()
    {
        const string source = """
                              using Mapster;
                              using MapsterExtensions.Generator;

                              namespace Mapster
                              {
                                  public interface IRegister { }
                                  public class TypeAdapterConfig 
                                  {
                                      public object NewConfig<TSource, TDestination>() => new object();
                                  }
                              }

                              namespace TestNamespace;

                              public class CompleteRegister : IRegister
                              {
                                  [Generate]
                                  public void Register(TypeAdapterConfig config)
                                  {
                                      config.NewConfig<Person, PersonDto>();
                                      config.NewConfig<Person, PersonSummary>();
                                      config.NewConfig<Order, OrderDto>();
                                  }
                              }

                              public class Person { }
                              public class PersonDto { }
                              public class PersonSummary { }
                              public class Order { }
                              public class OrderDto { }
                              """;

        await source.ShouldGenerate<MapsterExtensionGenerator>(
            "TestNamespace.Person.g.cs", "TestNamespace.Order.g.cs");
    }
}