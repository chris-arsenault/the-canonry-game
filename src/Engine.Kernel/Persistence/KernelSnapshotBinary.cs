namespace Engine.Kernel;

internal static class KernelSnapshotBinary
{
    private const int TypeInt = 1;
    private const int TypeLong = 2;
    private const int TypeFloat = 3;
    private const int TypeBool = 4;
    private const int TypeUint = 5;
    private const int TypeIntArray = 6;
    private const int TypeFloatArray = 7;
    private const int TypeBoolArray = 8;
    private const int TypeIntTable = 9;
    private const int TypeFloatTable = 10;
    private const int TypeSpatialLayerIndex = 11;

    public static void WriteObject(global::System.IO.BinaryWriter writer, object target)
    {
        global::System.ArgumentNullException.ThrowIfNull(writer);
        global::System.ArgumentNullException.ThrowIfNull(target);
        var fields = Fields(target.GetType());
        writer.Write(fields.Length);
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var code = TypeCode(field.FieldType);
            writer.Write(code);
            WriteFieldValue(writer, code, field.GetValue(target)!);
        }
    }

    private static void WriteFieldValue(
        global::System.IO.BinaryWriter writer,
        int code,
        object value)
    {
        if (TryWriteScalar(writer, code, value) ||
            TryWriteArray(writer, code, value) ||
            TryWriteObject(writer, code, value))
        {
            return;
        }

        throw new global::System.InvalidOperationException();
    }

    private static bool TryWriteScalar(
        global::System.IO.BinaryWriter writer,
        int code,
        object value)
    {
        if (code == TypeInt)
        {
            writer.Write((int)value);
            return true;
        }

        if (code == TypeLong)
        {
            writer.Write((long)value);
            return true;
        }

        if (code == TypeFloat)
        {
            writer.Write((float)value);
            return true;
        }

        if (code == TypeBool)
        {
            writer.Write((bool)value);
            return true;
        }

        if (code != TypeUint)
        {
            return false;
        }

        writer.Write((uint)value);
        return true;
    }

    private static bool TryWriteArray(
        global::System.IO.BinaryWriter writer,
        int code,
        object value)
    {
        if (code == TypeIntArray)
        {
            Write(writer, (int[])value);
            return true;
        }

        if (code == TypeFloatArray)
        {
            Write(writer, (float[])value);
            return true;
        }

        if (code != TypeBoolArray)
        {
            return false;
        }

        Write(writer, (bool[])value);
        return true;
    }

    private static bool TryWriteObject(
        global::System.IO.BinaryWriter writer,
        int code,
        object value)
    {
        if (code == TypeIntTable)
        {
            ((PrimitiveIntTable)value).WriteSnapshot(writer);
            return true;
        }

        if (code == TypeFloatTable)
        {
            ((PrimitiveFloatTable)value).WriteSnapshot(writer);
            return true;
        }

        if (code != TypeSpatialLayerIndex)
        {
            return false;
        }

        ((SpatialLayerIndex)value).WriteSnapshot(writer);
        return true;
    }

    public static void ReadObject(global::System.IO.BinaryReader reader, object target)
    {
        global::System.ArgumentNullException.ThrowIfNull(reader);
        global::System.ArgumentNullException.ThrowIfNull(target);
        var fields = Fields(target.GetType());
        if (reader.ReadInt32() != fields.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var code = TypeCode(field.FieldType);
            if (reader.ReadInt32() != code)
            {
                throw new global::System.InvalidOperationException();
            }

            ReadFieldValue(reader, target, field, code);
        }
    }

    private static void ReadFieldValue(
        global::System.IO.BinaryReader reader,
        object target,
        global::System.Reflection.FieldInfo field,
        int code)
    {
        if (TryReadScalar(reader, target, field, code) ||
            TryReadArray(reader, target, field, code) ||
            TryReadObject(reader, target, field, code))
        {
            return;
        }

        throw new global::System.InvalidOperationException();
    }

    private static bool TryReadScalar(
        global::System.IO.BinaryReader reader,
        object target,
        global::System.Reflection.FieldInfo field,
        int code)
    {
        if (code == TypeInt)
        {
            field.SetValue(target, reader.ReadInt32());
            return true;
        }

        if (code == TypeLong)
        {
            field.SetValue(target, reader.ReadInt64());
            return true;
        }

        if (code == TypeFloat)
        {
            field.SetValue(target, reader.ReadSingle());
            return true;
        }

        if (code == TypeBool)
        {
            field.SetValue(target, reader.ReadBoolean());
            return true;
        }

        if (code != TypeUint)
        {
            return false;
        }

        field.SetValue(target, reader.ReadUInt32());
        return true;
    }

    private static bool TryReadArray(
        global::System.IO.BinaryReader reader,
        object target,
        global::System.Reflection.FieldInfo field,
        int code)
    {
        if (code == TypeIntArray)
        {
            Read(reader, (int[])field.GetValue(target)!);
            return true;
        }

        if (code == TypeFloatArray)
        {
            Read(reader, (float[])field.GetValue(target)!);
            return true;
        }

        if (code != TypeBoolArray)
        {
            return false;
        }

        Read(reader, (bool[])field.GetValue(target)!);
        return true;
    }

    private static bool TryReadObject(
        global::System.IO.BinaryReader reader,
        object target,
        global::System.Reflection.FieldInfo field,
        int code)
    {
        if (code == TypeIntTable)
        {
            ((PrimitiveIntTable)field.GetValue(target)!).ReadSnapshot(reader);
            return true;
        }

        if (code == TypeFloatTable)
        {
            ((PrimitiveFloatTable)field.GetValue(target)!).ReadSnapshot(reader);
            return true;
        }

        if (code != TypeSpatialLayerIndex)
        {
            return false;
        }

        ((SpatialLayerIndex)field.GetValue(target)!).ReadSnapshot(reader);
        return true;
    }

    public static void Write(global::System.IO.BinaryWriter writer, int[] values)
    {
        writer.Write(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            writer.Write(values[i]);
        }
    }

    public static void Read(global::System.IO.BinaryReader reader, int[] values)
    {
        if (reader.ReadInt32() != values.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = reader.ReadInt32();
        }
    }

    public static void Write(global::System.IO.BinaryWriter writer, float[] values)
    {
        writer.Write(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            writer.Write(values[i]);
        }
    }

    public static void Read(global::System.IO.BinaryReader reader, float[] values)
    {
        if (reader.ReadInt32() != values.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = reader.ReadSingle();
        }
    }

    public static void Write(global::System.IO.BinaryWriter writer, bool[] values)
    {
        writer.Write(values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            writer.Write(values[i]);
        }
    }

    public static void Read(global::System.IO.BinaryReader reader, bool[] values)
    {
        if (reader.ReadInt32() != values.Length)
        {
            throw new global::System.InvalidOperationException();
        }

        for (var i = 0; i < values.Length; i++)
        {
            values[i] = reader.ReadBoolean();
        }
    }

    private static global::System.Reflection.FieldInfo[] Fields(global::System.Type type)
    {
        var source = type.GetFields(
            global::System.Reflection.BindingFlags.Instance |
            global::System.Reflection.BindingFlags.Public |
            global::System.Reflection.BindingFlags.NonPublic);
        var count = 0;
        for (var i = 0; i < source.Length; i++)
        {
            if (FieldIsSupported(source[i]))
            {
                count++;
            }
        }

        var fields = new global::System.Reflection.FieldInfo[count];
        var cursor = 0;
        for (var i = 0; i < source.Length; i++)
        {
            if (FieldIsSupported(source[i]))
            {
                fields[cursor] = source[i];
                cursor++;
            }
        }

        global::System.Array.Sort(
            fields,
            static (left, right) => left.MetadataToken.CompareTo(right.MetadataToken));
        return fields;
    }

    private static bool FieldIsSupported(global::System.Reflection.FieldInfo field)
    {
        if (field.FieldType == typeof(Engine.Eventing.KernelEventLog) ||
            field.FieldType == typeof(KernelDataSet))
        {
            return false;
        }

        var code = TypeCode(field.FieldType);
        return code != 0 && (!field.IsInitOnly || code >= TypeIntArray);
    }

    private static int TypeCode(global::System.Type type)
    {
        var scalarCode = ScalarTypeCode(type);
        if (scalarCode != 0)
        {
            return scalarCode;
        }

        var arrayCode = ArrayTypeCode(type);
        return arrayCode != 0 ? arrayCode : ObjectTypeCode(type);
    }

    private static int ScalarTypeCode(global::System.Type type)
    {
        if (type == typeof(int))
        {
            return TypeInt;
        }

        if (type == typeof(long))
        {
            return TypeLong;
        }

        if (type == typeof(float))
        {
            return TypeFloat;
        }

        if (type == typeof(bool))
        {
            return TypeBool;
        }

        if (type == typeof(uint))
        {
            return TypeUint;
        }

        return 0;
    }

    private static int ArrayTypeCode(global::System.Type type)
    {
        if (type == typeof(int[]))
        {
            return TypeIntArray;
        }

        if (type == typeof(float[]))
        {
            return TypeFloatArray;
        }

        if (type == typeof(bool[]))
        {
            return TypeBoolArray;
        }

        return 0;
    }

    private static int ObjectTypeCode(global::System.Type type)
    {
        if (type == typeof(PrimitiveIntTable))
        {
            return TypeIntTable;
        }

        if (type == typeof(PrimitiveFloatTable))
        {
            return TypeFloatTable;
        }

        if (type == typeof(SpatialLayerIndex))
        {
            return TypeSpatialLayerIndex;
        }

        return 0;
    }
}
