using Generator.Testing;

namespace MapsterExtensions.Generator.Tests;

public class MapsterExtensionDiagnosticTests
{
    private const string MapsterMocks = """
                                        namespace Mapster
                                        {
                                            public interface IRegister { }
                                            public class TypeAdapterConfig 
                                            {
                                                public object NewConfig<TSource, TDestination>() => new object();
                                            }
                                        }
                                        """;

    [Fact]
    public async Task NotImplementingIRegister_ReportsDiagnostic()
    {
        const string source = """
                              using Mapster;
                              using MapsterExtensions.Generator;

                              public class NotARegister
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

        await source.ShouldHaveDiagnostic<MapsterExtensionGenerator>("ME0001", additionalSources: MapsterMocks);
    }

    [Fact]
    public async Task InvalidMethodSignature_ReportsDiagnostic()
    {
        const string source = """
                              using Mapster;
                              using MapsterExtensions.Generator;

                              public class BadRegister : IRegister
                              {
                                  [Generate]
                                  public int Register()
                                  {
                                      return 42;
                                  }
                              }
                              """;

        await source.ShouldHaveDiagnostic<MapsterExtensionGenerator>("ME0002", additionalSources: MapsterMocks);
    }

    [Fact]
    public async Task NoNewConfigCalls_ReportsDiagnostic()
    {
        const string source = """
                              using Mapster;
                              using MapsterExtensions.Generator;

                              public class EmptyRegister : IRegister
                              {
                                  [Generate]
                                  public void Register(TypeAdapterConfig config)
                                  {
                                      // No NewConfig calls
                                  }
                              }
                              """;

        await source.ShouldHaveDiagnostic<MapsterExtensionGenerator>("ME0003", Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, additionalSources: MapsterMocks);
    }

    [Fact]
    public async Task DuplicateDestinationNames_ReportsDiagnostic()
    {
        const string source = """
                              using Mapster;
                              using MapsterExtensions.Generator;

                              public class DuplicateRegister : IRegister
                              {
                                  [Generate]
                                  public void Register(TypeAdapterConfig config)
                                  {
                                      config.NewConfig<Person, Customer>();
                                      config.NewConfig<Person, MyNamespace.Customer>();
                                  }
                              }

                              public class Person { }
                              public class Customer { }

                              namespace MyNamespace
                              {
                                  public class Customer { }
                              }
                              """;

        await source.ShouldHaveDiagnostic<MapsterExtensionGenerator>("ME0004", additionalSources: MapsterMocks);
    }
}