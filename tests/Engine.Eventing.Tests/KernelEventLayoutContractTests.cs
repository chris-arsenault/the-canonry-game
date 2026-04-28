namespace Engine.Eventing.Tests;

using System.Globalization;
using System.Reflection;

using Engine.Eventing;
using Engine.Eventing.Schema;

public sealed class KernelEventLayoutContractTests
{
    [Fact]
    public void EveryKernelEventTypeHasOnePayloadLayout()
    {
        var layoutTypes = PayloadTypes()
            .Select(type => type.GetCustomAttribute<KernelEventLayoutAttribute>()!.EventType)
            .OrderBy(type => (int)type)
            .ToArray();
        var eventTypes = Enum.GetValues<KernelEventType>()
            .Where(type => type != KernelEventType.None)
            .OrderBy(type => (int)type)
            .ToArray();

        Assert.Equal(eventTypes, layoutTypes);
    }

    [Fact]
    public void PayloadRecordFieldsHaveExactlyOneSlot()
    {
        foreach (var payloadType in PayloadTypes())
        {
            var constructor = PayloadConstructor(payloadType);
            foreach (var parameter in constructor.GetParameters())
            {
                if (parameter.Name is "Tick" or "WorldVersion")
                {
                    Assert.Empty(parameter.GetCustomAttributes<KernelEventIntSlotAttribute>());
                    Assert.Empty(parameter.GetCustomAttributes<KernelEventFloatSlotAttribute>());
                    continue;
                }

                var intSlotCount = parameter.GetCustomAttributes<KernelEventIntSlotAttribute>().Count();
                var floatSlotCount = parameter.GetCustomAttributes<KernelEventFloatSlotAttribute>().Count();
                Assert.Equal(1, intSlotCount + floatSlotCount);
            }
        }
    }

    [Fact]
    public void GeneratedDescriptorsMatchPayloadAttributes()
    {
        var descriptors = KernelEventLayoutDescriptors.All.ToDictionary(descriptor => descriptor.EventType);
        foreach (var payloadType in PayloadTypes())
        {
            var layout = payloadType.GetCustomAttribute<KernelEventLayoutAttribute>()!;
            var descriptor = descriptors[layout.EventType];
            var parameters = PayloadConstructor(payloadType).GetParameters()
                .Where(parameter => parameter.Name is not "Tick" and not "WorldVersion")
                .ToArray();

            Assert.Equal(payloadType.Name, descriptor.PayloadTypeName);
            Assert.Equal(layout.SchemaMinor, descriptor.SchemaMinor);
            Assert.Equal(parameters.Length, descriptor.Slots.Count);

            foreach (var parameter in parameters)
            {
                var descriptorSlot = Assert.Single(descriptor.Slots, slot => slot.PropertyName == parameter.Name);
                var intSlot = parameter.GetCustomAttribute<KernelEventIntSlotAttribute>();
                var floatSlot = parameter.GetCustomAttribute<KernelEventFloatSlotAttribute>();
                if (intSlot is not null)
                {
                    Assert.Equal(KernelEventPrimitiveKind.Int32, descriptorSlot.PrimitiveKind);
                    Assert.Equal(intSlot.Slot, descriptorSlot.Slot);
                    Assert.Equal(intSlot.Name, descriptorSlot.Name);
                    Assert.Equal(intSlot.SemanticKind, descriptorSlot.SemanticKind);
                }
                else
                {
                    Assert.NotNull(floatSlot);
                    Assert.Equal(KernelEventPrimitiveKind.Float32, descriptorSlot.PrimitiveKind);
                    Assert.Equal(floatSlot.Slot, descriptorSlot.Slot);
                    Assert.Equal(floatSlot.Name, descriptorSlot.Name);
                    Assert.Equal(floatSlot.SemanticKind, descriptorSlot.SemanticKind);
                }
            }
        }
    }

    [Fact]
    public void SchemaRegistryMatchesGeneratedDescriptors()
    {
        foreach (var descriptor in KernelEventLayoutDescriptors.All)
        {
            var schema = KernelEventSchemaRegistry.Get(descriptor.EventType);
            Assert.Equal(descriptor.Name, schema.Name);
            Assert.Equal(descriptor.SchemaMinor, schema.SchemaMinor);
            Assert.Equal(descriptor.Slots.Count, schema.Slots.Count);

            foreach (var descriptorSlot in descriptor.Slots)
            {
                var schemaSlot = Assert.Single(
                    schema.Slots,
                    slot => slot.PrimitiveKind == descriptorSlot.PrimitiveKind && slot.Slot == descriptorSlot.Slot);
                Assert.Equal(descriptorSlot.Name, schemaSlot.Name);
                Assert.Equal(descriptorSlot.SemanticKind, schemaSlot.SemanticKind);
            }
        }
    }

    [Fact]
    public void GeneratedPackAndUnpackFollowPayloadAttributes()
    {
        foreach (var payloadType in PayloadTypes())
        {
            var constructor = PayloadConstructor(payloadType);
            var parameters = constructor.GetParameters();
            var arguments = SentinelArguments(parameters);
            var payload = constructor.Invoke(arguments);
            var kernelEvent = InvokeFrom(payloadType, payload);

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var intSlot = parameter.GetCustomAttribute<KernelEventIntSlotAttribute>();
                if (intSlot is not null)
                {
                    Assert.Equal(Convert.ToInt32(arguments[i], CultureInfo.InvariantCulture), kernelEvent.GetIntSlot(intSlot.Slot));
                    continue;
                }

                var floatSlot = parameter.GetCustomAttribute<KernelEventFloatSlotAttribute>();
                if (floatSlot is not null)
                {
                    Assert.Equal((float)arguments[i], kernelEvent.GetFloatSlot(floatSlot.Slot));
                }
            }

            var unpacked = InvokeAs(payloadType, kernelEvent);
            foreach (var parameter in parameters)
            {
                var property = payloadType.GetProperty(parameter.Name!)!;
                Assert.Equal(property.GetValue(payload), property.GetValue(unpacked));
            }
        }
    }

    private static IEnumerable<Type> PayloadTypes() =>
        typeof(KernelEvent).Assembly.GetTypes()
            .Where(type => Attribute.IsDefined(type, typeof(KernelEventLayoutAttribute)))
            .OrderBy(type => (int)type.GetCustomAttribute<KernelEventLayoutAttribute>()!.EventType);

    private static ConstructorInfo PayloadConstructor(Type payloadType) =>
        payloadType.GetConstructors()
            .Single(constructor =>
            {
                var parameters = constructor.GetParameters();
                return parameters.Length >= 2 &&
                       parameters[0].Name == "Tick" &&
                       parameters[1].Name == "WorldVersion";
            });

    private static object[] SentinelArguments(ParameterInfo[] parameters)
    {
        var arguments = new object[parameters.Length];
        var intValue = 101;
        var floatValue = 1001.25f;
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.Name == "Tick")
            {
                arguments[i] = 17;
            }
            else if (parameter.Name == "WorldVersion")
            {
                arguments[i] = 23L;
            }
            else if (parameter.ParameterType == typeof(int))
            {
                arguments[i] = intValue++;
            }
            else if (parameter.ParameterType == typeof(float))
            {
                arguments[i] = floatValue;
                floatValue += 1.25f;
            }
            else if (parameter.ParameterType.IsEnum)
            {
                arguments[i] = Enum.ToObject(parameter.ParameterType, intValue++);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported kernel event payload field type {parameter.ParameterType}.");
            }
        }

        return arguments;
    }

    private static KernelEvent InvokeFrom(Type payloadType, object payload)
    {
        var method = typeof(KernelEvent).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
            {
                if (candidate.Name != "From")
                {
                    return false;
                }

                var parameters = candidate.GetParameters();
                return parameters.Length == 1 &&
                       parameters[0].ParameterType.IsByRef &&
                       parameters[0].ParameterType.GetElementType() == payloadType;
            });
        return (KernelEvent)method.Invoke(null, [payload])!;
    }

    private static object InvokeAs(Type payloadType, KernelEvent kernelEvent)
    {
        var layout = payloadType.GetCustomAttribute<KernelEventLayoutAttribute>()!;
        var method = typeof(KernelEvent).GetMethod("As" + layout.EventType)!;
        return method.Invoke(kernelEvent, [])!;
    }
}
