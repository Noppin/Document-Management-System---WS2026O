using System.Reflection;
using DocumentManagementSystem.Application.Dtos;
using DocumentManagementSystem.Application.Services;
using DocumentManagementSystem.Domain.Entities;
using DocumentManagementSystem.Domain.Repositories;

namespace DocumentManagementSystem.Application.Tests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Application_DoesNotReferencePersistenceOrEntityFramework()
    {
        var references = typeof(DocumentService).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references, reference =>
            reference.Name == "DocumentManagementSystem.Persistence" ||
            reference.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void RepositoryContracts_BelongToDomain()
    {
        var domainAssembly = typeof(Document).Assembly;

        Assert.Same(domainAssembly, typeof(IDocumentRepository).Assembly);
        Assert.Same(domainAssembly, typeof(ICollectionRepository).Assembly);
        Assert.DoesNotContain(domainAssembly.GetReferencedAssemblies(), reference =>
            reference.Name == "DocumentManagementSystem.Application" ||
            reference.Name == "DocumentManagementSystem.Persistence" ||
            reference.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }

    [Fact]
    public void DtoProperties_DoNotExposeDomainOrPersistenceTypes()
    {
        var dtoTypes = typeof(DocumentResponse).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace == typeof(DocumentResponse).Namespace);

        foreach (var dtoType in dtoTypes)
        {
            foreach (var property in dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var type in Flatten(property.PropertyType))
                {
                    Assert.False(
                        type.Namespace?.StartsWith("DocumentManagementSystem.Domain", StringComparison.Ordinal) == true ||
                        type.Namespace?.StartsWith("DocumentManagementSystem.Persistence", StringComparison.Ordinal) == true,
                        $"{dtoType.Name}.{property.Name} exposes {type.FullName}");
                }
            }
        }
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        var nestedTypes = type.IsArray
            ? new[] { type.GetElementType()! }
            : type.GetGenericArguments();
        foreach (var nestedType in nestedTypes)
        {
            foreach (var candidate in Flatten(nestedType))
                yield return candidate;
        }
    }
}
