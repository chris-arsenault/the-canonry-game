namespace Engine.Eventing.Generators;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

[Generator]
public sealed class KernelEventSourceGenerator : ISourceGenerator
{
    private const string LayoutAttributeName = "Engine.Eventing.KernelEventLayoutAttribute";
    private const string IntSlotAttributeName = "Engine.Eventing.KernelEventIntSlotAttribute";
    private const string FloatSlotAttributeName = "Engine.Eventing.KernelEventFloatSlotAttribute";
    private const string TickFieldName = "Tick";
    private const string WorldVersionFieldName = "WorldVersion";

    private static readonly DiagnosticDescriptor InvalidLayout = new(
        "KEVT001",
        "Invalid kernel event layout",
        "{0}",
        "KernelEventLayout",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var compilation = context.Compilation;
        var symbols = RequiredSymbols.Resolve(compilation);
        if (symbols is null)
        {
            return;
        }

        var intSlotCount = ConstInt(symbols.KernelEvent, "IntSlotCount");
        var floatSlotCount = ConstInt(symbols.KernelEvent, "FloatSlotCount");
        var layouts = new List<EventLayout>();
        foreach (var type in AllTypes(compilation.Assembly.GlobalNamespace))
        {
            if (TryBuildLayout(
                    context,
                    type,
                    symbols.LayoutAttribute,
                    symbols.EventType,
                    symbols.IntSlotAttribute,
                    symbols.FloatSlotAttribute,
                    symbols.SemanticKind,
                    intSlotCount,
                    floatSlotCount,
                    out var layout))
            {
                layouts.Add(layout);
            }
        }

        if (layouts.Count == 0)
        {
            return;
        }

        foreach (var duplicate in layouts.GroupBy(static layout => layout.EventTypeValue).Where(static group => group.Skip(1).Any()))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                InvalidLayout,
                Location.None,
                $"Kernel event type {duplicate.First().EventTypeName} has more than one payload layout."));
        }

        layouts.Sort(static (left, right) => left.EventTypeValue.CompareTo(right.EventTypeValue));
        context.AddSource("KernelEvent.Generated.g.cs", GenerateKernelEvent(layouts));
        context.AddSource("KernelEventLayoutDescriptors.Generated.g.cs", GenerateDescriptors(layouts));
    }

    private static bool TryBuildLayout(
        GeneratorExecutionContext context,
        INamedTypeSymbol type,
        INamedTypeSymbol layoutAttribute,
        INamedTypeSymbol eventType,
        INamedTypeSymbol intSlotAttribute,
        INamedTypeSymbol floatSlotAttribute,
        INamedTypeSymbol semanticKind,
        int intSlotCount,
        int floatSlotCount,
        out EventLayout layout)
    {
        layout = default!;
        var layoutMetadata = GetLayoutMetadata(type, layoutAttribute, eventType);
        if (layoutMetadata is null)
        {
            return false;
        }

        var constructor = PayloadConstructor(type);
        if (constructor is null)
        {
            Report(context, type, $"{type.Name} must declare a primary payload constructor.");
            return false;
        }

        if (!TryCollectSlots(context, type, constructor, intSlotAttribute, floatSlotAttribute, semanticKind, out var slots))
        {
            return false;
        }

        ValidateSlots(context, type, slots, intSlotCount, floatSlotCount);
        layout = new EventLayout(
            layoutMetadata.EventTypeName,
            layoutMetadata.EventTypeValue,
            type.Name,
            layoutMetadata.SchemaMinor,
            slots);
        return true;
    }

    private static bool TryCollectSlots(
        GeneratorExecutionContext context,
        INamedTypeSymbol type,
        IMethodSymbol constructor,
        INamedTypeSymbol intSlotAttribute,
        INamedTypeSymbol floatSlotAttribute,
        INamedTypeSymbol semanticKind,
        out List<SlotLayout> slots)
    {
        slots = [];
        var valid = true;
        foreach (var parameter in constructor.Parameters)
        {
            if (parameter.Name is TickFieldName or WorldVersionFieldName)
            {
                continue;
            }

            if (TryCreateSlot(context, type, parameter, intSlotAttribute, floatSlotAttribute, semanticKind, out var slot) &&
                slot is not null)
            {
                slots.Add(slot);
            }
            else
            {
                valid = false;
            }
        }

        return valid;
    }

    private static bool TryCreateSlot(
        GeneratorExecutionContext context,
        INamedTypeSymbol type,
        IParameterSymbol parameter,
        INamedTypeSymbol intSlotAttribute,
        INamedTypeSymbol floatSlotAttribute,
        INamedTypeSymbol semanticKind,
        out SlotLayout? slot)
    {
        slot = null;
        var intSlot = SlotMetadata(parameter, intSlotAttribute, semanticKind, KernelEventSlotKind.Int);
        var floatSlot = SlotMetadata(parameter, floatSlotAttribute, semanticKind, KernelEventSlotKind.Float);
        if (intSlot is not null && floatSlot is not null)
        {
            Report(context, parameter, $"{type.Name}.{parameter.Name} declares both int and float slots.");
            return false;
        }

        var metadata = intSlot ?? floatSlot;
        if (metadata is null)
        {
            Report(context, parameter, $"{type.Name}.{parameter.Name} must declare exactly one kernel event slot.");
            return false;
        }

        if (!SlotTypeMatches(metadata, parameter))
        {
            Report(context, parameter, $"{type.Name}.{parameter.Name} uses a {metadata.Kind.ToString().ToLowerInvariant()} slot but has an incompatible type.");
            return false;
        }

        slot = metadata.WithPayload(
            parameter.Name,
            parameter.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        return true;
    }

    private static bool SlotTypeMatches(SlotLayout slot, IParameterSymbol parameter) =>
        slot.Kind == KernelEventSlotKind.Int
            ? IsIntSlotType(parameter.Type)
            : parameter.Type.SpecialType == SpecialType.System_Single;

    private sealed class RequiredSymbols
    {
        private RequiredSymbols(
            INamedTypeSymbol layoutAttribute,
            INamedTypeSymbol intSlotAttribute,
            INamedTypeSymbol floatSlotAttribute,
            INamedTypeSymbol kernelEvent,
            INamedTypeSymbol eventType,
            INamedTypeSymbol semanticKind)
        {
            LayoutAttribute = layoutAttribute;
            IntSlotAttribute = intSlotAttribute;
            FloatSlotAttribute = floatSlotAttribute;
            KernelEvent = kernelEvent;
            EventType = eventType;
            SemanticKind = semanticKind;
        }

        public INamedTypeSymbol LayoutAttribute { get; }
        public INamedTypeSymbol IntSlotAttribute { get; }
        public INamedTypeSymbol FloatSlotAttribute { get; }
        public INamedTypeSymbol KernelEvent { get; }
        public INamedTypeSymbol EventType { get; }
        public INamedTypeSymbol SemanticKind { get; }

        public static RequiredSymbols? Resolve(Compilation compilation)
        {
            var layoutAttribute = compilation.GetTypeByMetadataName(LayoutAttributeName);
            var intSlotAttribute = compilation.GetTypeByMetadataName(IntSlotAttributeName);
            var floatSlotAttribute = compilation.GetTypeByMetadataName(FloatSlotAttributeName);
            var kernelEvent = compilation.GetTypeByMetadataName("Engine.Eventing.KernelEvent");
            var eventType = compilation.GetTypeByMetadataName("Engine.Eventing.KernelEventType");
            var semanticKind = compilation.GetTypeByMetadataName("Engine.Eventing.KernelEventSemanticKind");

            if (layoutAttribute is null ||
                intSlotAttribute is null ||
                floatSlotAttribute is null ||
                kernelEvent is null ||
                eventType is null ||
                semanticKind is null)
            {
                return null;
            }

            return new RequiredSymbols(layoutAttribute, intSlotAttribute, floatSlotAttribute, kernelEvent, eventType, semanticKind);
        }
    }

    private static string GenerateKernelEvent(IReadOnlyList<EventLayout> layouts)
    {
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        source.AppendLine("#pragma warning disable");
        source.AppendLine("namespace Engine.Eventing;");
        source.AppendLine();
        source.AppendLine("public readonly partial record struct KernelEvent");
        source.AppendLine("{");
        foreach (var layout in layouts)
        {
            AppendKernelEventFactory(source, layout);
        }

        foreach (var layout in layouts)
        {
            AppendKernelEventAccessor(source, layout);
        }

        AppendEnsureType(source);
        source.AppendLine("}");
        return source.ToString();
    }

    private static void AppendKernelEventFactory(StringBuilder source, EventLayout layout)
    {
        source.Append("    public static KernelEvent From(in ")
            .Append(layout.PayloadTypeName)
            .AppendLine(" payload) =>");
        source.AppendLine("        new(");
        source.Append("            KernelEventType.")
            .Append(layout.EventTypeName)
            .AppendLine(",");
        source.AppendLine("            payload.Tick,");
        source.Append("            payload.WorldVersion");

        foreach (var slot in layout.Slots.OrderBy(static slot => slot.Kind).ThenBy(static slot => slot.Slot))
        {
            source.AppendLine(",");
            source.Append("            ")
                .Append(SlotArgumentName(slot))
                .Append(": ")
                .Append(PackExpression(slot));
        }

        source.AppendLine(");");
        source.AppendLine();
    }

    private static void AppendKernelEventAccessor(StringBuilder source, EventLayout layout)
    {
        source.Append("    public ")
            .Append(layout.PayloadTypeName)
            .Append(" As")
            .Append(layout.EventTypeName)
            .AppendLine("()");
        source.AppendLine("    {");
        source.Append("        EnsureType(KernelEventType.")
            .Append(layout.EventTypeName)
            .AppendLine(");");
        source.AppendLine("        return new(");
        source.AppendLine("            Tick,");
        source.Append("            WorldVersion");
        foreach (var slot in layout.SlotsInPayloadOrder)
        {
            source.AppendLine(",");
            source.Append("            ").Append(UnpackExpression(slot));
        }

        source.AppendLine(");");
        source.AppendLine("    }");
        source.AppendLine();
    }

    private static void AppendEnsureType(StringBuilder source)
    {
        source.AppendLine("    private void EnsureType(KernelEventType expected)");
        source.AppendLine("    {");
        source.AppendLine("        if (EventType != expected)");
        source.AppendLine("        {");
        source.AppendLine("            throw new global::System.InvalidOperationException($\"Kernel event is {EventType}, not {expected}.\");");
        source.AppendLine("        }");
        source.AppendLine("    }");
    }

    private static string GenerateDescriptors(IReadOnlyList<EventLayout> layouts)
    {
        var source = new StringBuilder();
        source.AppendLine("// <auto-generated />");
        source.AppendLine("#nullable enable");
        source.AppendLine("#pragma warning disable");
        source.AppendLine("namespace Engine.Eventing;");
        source.AppendLine();
        source.AppendLine("public static class KernelEventLayoutDescriptors");
        source.AppendLine("{");
        source.AppendLine("    public static readonly KernelEventLayoutDescriptor[] All =");
        source.AppendLine("    {");
        foreach (var layout in layouts)
        {
            source.Append("        new(KernelEventType.")
                .Append(layout.EventTypeName)
                .Append(", \"")
                .Append(layout.EventTypeName)
                .Append("\", \"")
                .Append(layout.PayloadTypeName)
                .Append("\", ")
                .Append(layout.SchemaMinor.ToString(CultureInfo.InvariantCulture))
                .AppendLine(",");
            source.AppendLine("            new KernelEventLayoutSlotDescriptor[]");
            source.AppendLine("            {");
            foreach (var slot in layout.Slots.OrderBy(static slot => slot.Kind).ThenBy(static slot => slot.Slot))
            {
                source.Append("                new(")
                    .Append(slot.Slot.ToString(CultureInfo.InvariantCulture))
                    .Append(", \"")
                    .Append(slot.SchemaName)
                    .Append("\", KernelEventPrimitiveKind.")
                    .Append(slot.Kind == KernelEventSlotKind.Int ? "Int32" : "Float32")
                    .Append(", KernelEventSemanticKind.")
                    .Append(slot.SemanticKindName)
                    .Append(", \"")
                    .Append(slot.PropertyName)
                    .AppendLine("\"),");
            }

            source.AppendLine("            }),");
        }

        source.AppendLine("    };");
        source.AppendLine("}");
        return source.ToString();
    }

    private static IEnumerable<INamedTypeSymbol> AllTypes(INamespaceSymbol namespaceSymbol)
    {
        foreach (var member in namespaceSymbol.GetTypeMembers())
        {
            yield return member;
            foreach (var nested in AllNestedTypes(member))
            {
                yield return nested;
            }
        }

        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var member in AllTypes(childNamespace))
            {
                yield return member;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> AllNestedTypes(INamedTypeSymbol type)
    {
        foreach (var nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (var child in AllNestedTypes(nested))
            {
                yield return child;
            }
        }
    }

    private static LayoutMetadata? GetLayoutMetadata(
        INamedTypeSymbol type,
        INamedTypeSymbol layoutAttribute,
        INamedTypeSymbol eventType)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, layoutAttribute))
            {
                continue;
            }

            var eventValue = Convert.ToInt32(attribute.ConstructorArguments[0].Value, CultureInfo.InvariantCulture);
            return new LayoutMetadata(
                EventMemberName(eventType, eventValue),
                eventValue,
                Convert.ToInt32(attribute.ConstructorArguments[1].Value, CultureInfo.InvariantCulture));
        }

        return null;
    }

    private static IMethodSymbol? PayloadConstructor(INamedTypeSymbol type) =>
        type.InstanceConstructors.FirstOrDefault(static constructor =>
            constructor.Parameters.Length >= 2 &&
            string.Equals(constructor.Parameters[0].Name, TickFieldName, StringComparison.Ordinal) &&
            string.Equals(constructor.Parameters[1].Name, WorldVersionFieldName, StringComparison.Ordinal));

    private static SlotLayout? SlotMetadata(
        IParameterSymbol parameter,
        INamedTypeSymbol slotAttribute,
        INamedTypeSymbol semanticKind,
        KernelEventSlotKind kind)
    {
        foreach (var attribute in parameter.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, slotAttribute))
            {
                continue;
            }

            var semanticValue = Convert.ToInt32(attribute.ConstructorArguments[2].Value, CultureInfo.InvariantCulture);
            return new SlotLayout(
                kind,
                Convert.ToInt32(attribute.ConstructorArguments[0].Value, CultureInfo.InvariantCulture),
                propertyName: string.Empty,
                schemaName: (string)attribute.ConstructorArguments[1].Value!,
                semanticKindName: EventMemberName(semanticKind, semanticValue),
                payloadTypeName: string.Empty);
        }

        return null;
    }

    private static void ValidateSlots(
        GeneratorExecutionContext context,
        INamedTypeSymbol type,
        IReadOnlyList<SlotLayout> slots,
        int intSlotCount,
        int floatSlotCount)
    {
        foreach (var slot in slots)
        {
            var maxSlot = slot.Kind == KernelEventSlotKind.Int ? intSlotCount - 1 : floatSlotCount - 1;
            if (slot.Slot < 0 || slot.Slot > maxSlot)
            {
                Report(context, type, $"{type.Name} declares {slot.Kind} slot {slot.Slot}, outside 0..{maxSlot}.");
            }
        }

        foreach (var (kind, slot) in slots
            .GroupBy(static slot => (slot.Kind, slot.Slot))
            .Where(static group => group.Skip(1).Any())
            .Select(static duplicate => duplicate.Key))
        {
            Report(context, type, $"{type.Name} declares duplicate {kind} slot {slot}.");
        }
    }

    private static int ConstInt(INamedTypeSymbol type, string name)
    {
        var field = type.GetMembers(name).OfType<IFieldSymbol>().FirstOrDefault();
        if (field?.ConstantValue is int value)
        {
            return value;
        }

        return 0;
    }

    private static bool IsIntSlotType(ITypeSymbol type) =>
        type.SpecialType == SpecialType.System_Int32 ||
        type.TypeKind == TypeKind.Enum;

    private static string SlotArgumentName(SlotLayout slot) =>
        slot.Kind == KernelEventSlotKind.Int ? "int" + slot.Slot.ToString(CultureInfo.InvariantCulture) :
            "float" + slot.Slot.ToString(CultureInfo.InvariantCulture);

    private static string PackExpression(SlotLayout slot)
    {
        if (slot.Kind == KernelEventSlotKind.Int && !string.Equals(slot.PayloadTypeName, "int", StringComparison.Ordinal))
        {
            return "(int)payload." + slot.PropertyName;
        }

        return "payload." + slot.PropertyName;
    }

    private static string UnpackExpression(SlotLayout slot)
    {
        var field = slot.Kind == KernelEventSlotKind.Int ? "_int" + slot.Slot.ToString(CultureInfo.InvariantCulture) :
            "_float" + slot.Slot.ToString(CultureInfo.InvariantCulture);
        if (slot.Kind == KernelEventSlotKind.Int && !string.Equals(slot.PayloadTypeName, "int", StringComparison.Ordinal))
        {
            return "(" + slot.PayloadTypeName + ")" + field;
        }

        return field;
    }

    private static string EventMemberName(INamedTypeSymbol enumType, int value)
    {
        foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
        {
            if (member.HasConstantValue &&
                Convert.ToInt32(member.ConstantValue, CultureInfo.InvariantCulture) == value)
            {
                return member.Name;
            }
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static void Report(GeneratorExecutionContext context, ISymbol symbol, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            InvalidLayout,
            symbol.Locations.FirstOrDefault(),
            message));
    }

    private sealed class LayoutMetadata
    {
        public LayoutMetadata(string eventTypeName, int eventTypeValue, int schemaMinor)
        {
            EventTypeName = eventTypeName;
            EventTypeValue = eventTypeValue;
            SchemaMinor = schemaMinor;
        }

        public string EventTypeName { get; }

        public int EventTypeValue { get; }

        public int SchemaMinor { get; }
    }

    private sealed class EventLayout
    {
        public EventLayout(
            string eventTypeName,
            int eventTypeValue,
            string payloadTypeName,
            int schemaMinor,
            IReadOnlyList<SlotLayout> slots)
        {
            EventTypeName = eventTypeName;
            EventTypeValue = eventTypeValue;
            PayloadTypeName = payloadTypeName;
            SchemaMinor = schemaMinor;
            Slots = slots;
        }

        public string EventTypeName { get; }

        public int EventTypeValue { get; }

        public string PayloadTypeName { get; }

        public int SchemaMinor { get; }

        public IReadOnlyList<SlotLayout> Slots { get; }

        public IReadOnlyList<SlotLayout> SlotsInPayloadOrder => Slots;
    }

    private sealed class SlotLayout
    {
        public SlotLayout(
            KernelEventSlotKind kind,
            int slot,
            string propertyName,
            string schemaName,
            string semanticKindName,
            string payloadTypeName)
        {
            Kind = kind;
            Slot = slot;
            PropertyName = propertyName;
            SchemaName = schemaName;
            SemanticKindName = semanticKindName;
            PayloadTypeName = payloadTypeName;
        }

        public KernelEventSlotKind Kind { get; }

        public int Slot { get; }

        public string PropertyName { get; }

        public string SchemaName { get; }

        public string SemanticKindName { get; }

        public string PayloadTypeName { get; }

        public SlotLayout WithPayload(string propertyName, string payloadTypeName) =>
            new(Kind, Slot, propertyName, SchemaName, SemanticKindName, payloadTypeName);
    }

    private enum KernelEventSlotKind
    {
        Int = 0,
        Float = 1,
    }
}
