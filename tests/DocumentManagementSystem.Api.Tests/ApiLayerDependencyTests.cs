using System.Reflection;
using System.Runtime.CompilerServices;
using DocumentManagementSystem.Api.Controllers;

namespace DocumentManagementSystem.Api.Tests;

public sealed class ApiLayerDependencyTests
{
    private static readonly Assembly ApiAssembly = typeof(DocumentsController).Assembly;

    private static readonly string[] ForbiddenNamespacePrefixes =
    {
        "DocumentManagementSystem.Domain.Entities",
        "DocumentManagementSystem.Persistence",
    };

    [Fact]
    public void Controllers_do_not_reference_domain_entities_or_persistence_types()
    {
        var violations = new List<string>();
        var controllers = ApiAssembly.GetTypes().Where(IsController).ToList();

        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
        {
            foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (var parameter in method.GetParameters())
                {
                    CollectViolations(parameter.ParameterType, $"{controller.Name}.{method.Name} parameter '{parameter.Name}'", violations);
                }

                CollectViolations(method.ReturnType, $"{controller.Name}.{method.Name} return type", violations);
            }
        }

        Assert.True(
            violations.Count == 0,
            "API controllers must not depend on domain entities or persistence types:"
                + Environment.NewLine
                + string.Join(Environment.NewLine, violations));
    }

    private static bool IsController(Type type) =>
        type is { IsClass: true, IsAbstract: false }
        && type.Namespace == "DocumentManagementSystem.Api.Controllers"
        && !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);

    private static void CollectViolations(Type type, string usage, List<string> violations)
    {
        foreach (var candidate in Flatten(type))
        {
            var isForbidden = ForbiddenNamespacePrefixes.Any(prefix =>
                candidate.Namespace == prefix
                || (candidate.Namespace?.StartsWith(prefix + ".", StringComparison.Ordinal) ?? false));

            if (isForbidden)
            {
                violations.Add($"- {usage} uses {candidate.FullName}");
            }
        }
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;

        if (type.IsArray)
        {
            foreach (var element in Flatten(type.GetElementType()!))
            {
                yield return element;
            }
        }

        if (!type.IsGenericType)
        {
            yield break;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in Flatten(argument))
            {
                yield return nested;
            }
        }
    }
}
