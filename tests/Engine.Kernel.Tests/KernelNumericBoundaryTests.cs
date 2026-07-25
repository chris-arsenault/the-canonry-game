namespace Engine.Kernel.Tests;

using System.Reflection;

using Engine.Domain.Compilation.KernelData;
using Engine.Domain.Compilation.KernelPrograms;
using Engine.Eventing;
using Engine.Kernel;

public sealed class KernelNumericBoundaryTests
{
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Instance |
        BindingFlags.Static |
        BindingFlags.DeclaredOnly;

    [Fact]
    public void KernelAssemblyHasNoFloatingPointRuntimeMembers()
    {
        AssertNoFloatingPointMembers(typeof(Simulation).Assembly.GetTypes());
    }

    [Fact]
    public void EventAssemblyHasNoFloatingPointPayloadOrCompatibilityMembers()
    {
        AssertNoFloatingPointMembers(typeof(KernelEvent).Assembly.GetTypes());
    }

    [Fact]
    public void CompiledKernelInputsHaveNoFloatingPointMembers()
    {
        var runtimeTypes = ReachableRuntimeTypes(
            typeof(KernelProgramSet),
            typeof(CompiledKernelDataSet));

        AssertNoFloatingPointMembers(runtimeTypes);
    }

    private static void AssertNoFloatingPointMembers(IEnumerable<Type> types)
    {
        var violations = types
            .SelectMany(MemberTypes)
            .Where(candidate => ContainsFloatingPoint(candidate.Type))
            .Select(candidate => candidate.Member)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    private static IEnumerable<(string Member, Type Type)> MemberTypes(Type type)
    {
        foreach (var field in type.GetFields(DeclaredMembers))
        {
            yield return ($"{type.FullName}.{field.Name}", field.FieldType);
        }

        foreach (var property in type.GetProperties(DeclaredMembers))
        {
            yield return ($"{type.FullName}.{property.Name}", property.PropertyType);
        }

        foreach (var method in type.GetMethods(DeclaredMembers))
        {
            yield return ($"{type.FullName}.{method.Name} return", method.ReturnType);
            foreach (var parameter in method.GetParameters())
            {
                yield return ($"{type.FullName}.{method.Name}({parameter.Name})", parameter.ParameterType);
            }
        }

        foreach (var constructor in type.GetConstructors(DeclaredMembers))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                yield return ($"{type.FullName}.ctor({parameter.Name})", parameter.ParameterType);
            }
        }
    }

    private static bool ContainsFloatingPoint(Type type)
    {
        if (type == typeof(float) || type == typeof(double))
        {
            return true;
        }

        if (type.HasElementType)
        {
            return ContainsFloatingPoint(type.GetElementType()!);
        }

        return type.IsGenericType &&
            type.GetGenericArguments().Any(ContainsFloatingPoint);
    }

    private static Type[] ReachableRuntimeTypes(params Type[] roots)
    {
        var allowedAssemblies = roots
            .Select(root => root.Assembly)
            .Append(typeof(KernelProgramSet).GetProperty(nameof(KernelProgramSet.LowAltitudeVerbs))!.PropertyType.Assembly)
            .ToHashSet();
        var pending = new Queue<Type>(roots);
        var found = new HashSet<Type>();
        while (pending.TryDequeue(out var type))
        {
            var normalized = Normalize(type);
            if (normalized is null || !allowedAssemblies.Contains(normalized.Assembly) || !found.Add(normalized))
            {
                continue;
            }

            EnqueueReferencedTypes(pending, normalized);
        }

        return [.. found];
    }

    private static void EnqueueReferencedTypes(Queue<Type> pending, Type type)
    {
        foreach (var candidate in MemberTypes(type))
        {
            pending.Enqueue(candidate.Type);
        }
    }

    private static Type? Normalize(Type type)
    {
        while (type.HasElementType)
        {
            type = type.GetElementType()!;
        }

        if (!type.IsGenericType)
        {
            return type;
        }

        foreach (var argument in type.GetGenericArguments())
        {
            if (argument.Assembly == typeof(KernelProgramSet).Assembly ||
                argument.Assembly == typeof(KernelProgramSet)
                    .GetProperty(nameof(KernelProgramSet.LowAltitudeVerbs))!
                    .PropertyType
                    .Assembly)
            {
                return argument;
            }
        }

        return null;
    }
}
