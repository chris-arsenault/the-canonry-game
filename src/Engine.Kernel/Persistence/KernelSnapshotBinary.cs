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
            if (code == TypeInt)
            {
                writer.Write((int)field.GetValue(target)!);
            }
            else if (code == TypeLong)
            {
                writer.Write((long)field.GetValue(target)!);
            }
            else if (code == TypeFloat)
            {
                writer.Write((float)field.GetValue(target)!);
            }
            else if (code == TypeBool)
            {
                writer.Write((bool)field.GetValue(target)!);
            }
            else if (code == TypeUint)
            {
                writer.Write((uint)field.GetValue(target)!);
            }
            else if (code == TypeIntArray)
            {
                Write(writer, (int[])field.GetValue(target)!);
            }
            else if (code == TypeFloatArray)
            {
                Write(writer, (float[])field.GetValue(target)!);
            }
            else if (code == TypeBoolArray)
            {
                Write(writer, (bool[])field.GetValue(target)!);
            }
            else if (code == TypeIntTable)
            {
                ((PrimitiveIntTable)field.GetValue(target)!).WriteSnapshot(writer);
            }
            else if (code == TypeFloatTable)
            {
                ((PrimitiveFloatTable)field.GetValue(target)!).WriteSnapshot(writer);
            }
            else if (code == TypeSpatialLayerIndex)
            {
                ((SpatialLayerIndex)field.GetValue(target)!).WriteSnapshot(writer);
            }
            else
            {
                throw new global::System.InvalidOperationException();
            }
        }
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

            if (code == TypeInt)
            {
                field.SetValue(target, reader.ReadInt32());
            }
            else if (code == TypeLong)
            {
                field.SetValue(target, reader.ReadInt64());
            }
            else if (code == TypeFloat)
            {
                field.SetValue(target, reader.ReadSingle());
            }
            else if (code == TypeBool)
            {
                field.SetValue(target, reader.ReadBoolean());
            }
            else if (code == TypeUint)
            {
                field.SetValue(target, reader.ReadUInt32());
            }
            else if (code == TypeIntArray)
            {
                Read(reader, (int[])field.GetValue(target)!);
            }
            else if (code == TypeFloatArray)
            {
                Read(reader, (float[])field.GetValue(target)!);
            }
            else if (code == TypeBoolArray)
            {
                Read(reader, (bool[])field.GetValue(target)!);
            }
            else if (code == TypeIntTable)
            {
                ((PrimitiveIntTable)field.GetValue(target)!).ReadSnapshot(reader);
            }
            else if (code == TypeFloatTable)
            {
                ((PrimitiveFloatTable)field.GetValue(target)!).ReadSnapshot(reader);
            }
            else if (code == TypeSpatialLayerIndex)
            {
                ((SpatialLayerIndex)field.GetValue(target)!).ReadSnapshot(reader);
            }
            else
            {
                throw new global::System.InvalidOperationException();
            }
        }
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
