using System;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess.Papyrus
{
    public enum PapyrusVarType : byte
    {
        Null = 0,
        Ref = 1,
        String = 2,
        Integer = 3,
        Float = 4,
        Boolean = 5,
        Variant = 6,
        Struct = 7,
        RefArray = 11,
        StringArray = 12,
        IntegerArray = 13,
        FloatArray = 14,
        BooleanArray = 15,
        VariantArray = 16,
        StructArray = 17
    }

    public static class PapyrusVarTypeExtensions
    {
        public static bool IsArray(this PapyrusVarType t) => (byte)t >= 11;

        public static bool IsRefType(this PapyrusVarType t)
        {
            return t == PapyrusVarType.Ref
                || t == PapyrusVarType.RefArray
                || t == PapyrusVarType.Struct
                || t == PapyrusVarType.StructArray;
        }
    }

    public enum ParamType : byte
    {
        Null = 0,
        Identifier = 1,
        String = 2,
        Integer = 3,
        Float = 4,
        Boolean = 5,
        Variant = 6,
        Struct = 7,
        Unknown8 = 8
    }

    public enum PapyrusOpcode : byte
    {
        NOP = 0,
        IADD = 1, FADD = 2, ISUB = 3, FSUB = 4, IMUL = 5, FMUL = 6, IDIV = 7, FDIV = 8, IMOD = 9,
        NOT = 10, INEG = 11, FNEG = 12, ASSIGN = 13, CAST = 14,
        CMP_EQ = 15, CMP_LT = 16, CMP_LE = 17, CMP_GT = 18, CMP_GE = 19,
        JMP = 20, JMPT = 21, JMPF = 22,
        CALLMETHOD = 23, CALLPARENT = 24, CALLSTATIC = 25,
        RETURN = 26, STRCAT = 27, PROPGET = 28, PROPSET = 29,
        ARR_CREATE = 30, ARR_LENGTH = 31, ARR_GET = 32, ARR_SET = 33, ARR_FIND = 34, ARR_RFIND = 35,
        IS = 36, STRUCT_CREATE = 37, STRUCT_GET = 38, STRUCT_SET = 39,
        ARR_STRUCT_FIND = 40, ARR_STRUCT_RFIND = 41, ARR_ADD = 42, ARR_INSERT = 43,
        ARR_REMOVE_LAST = 44, ARR_REMOVE = 45, ARR_CLEAR = 46, ARR_GET_MATCHING = 47
    }

    public static class PapyrusOpcodeExtensions
    {
        private static readonly int[] ArgsTable =
        {
            0, 3, 3, 3, 3, 3, 3, 3, 3, 3,
            2, 2, 2, 2, 2,
            3, 3, 3, 3, 3,
            1, 2, 2,
            -3, -2, -3,
            1, 3, 3, 3,
            2, 2, 3, 3, 4, 4,
            3, 1, 3, 3,
            5, 3, 3, 3,
            1, 3, 1, 6
        };

        public static bool HasExtraTerms(this PapyrusOpcode op)
        {
            int idx = (int)op;
            return idx >= 0 && idx < ArgsTable.Length && ArgsTable[idx] < 0;
        }

        public static int GetFixedCount(this PapyrusOpcode op)
        {
            int idx = (int)op;
            if (idx < 0 || idx >= ArgsTable.Length) return 0;
            int a = ArgsTable[idx];
            return a < 0 ? (1 - a) : a;
        }
    }

    public sealed class EID : IEquatable<EID>
    {
        public ulong Value { get; }
        public bool Is64Bit { get; }
        public bool IsZero => Value == 0;

        public EID(ulong value, bool is64Bit)
        {
            Value = value;
            Is64Bit = is64Bit;
        }

        public static EID Read(BinaryReaderEx reader, bool is64Bit)
        {
            if (is64Bit)
            {
                return new EID(reader.ReadUInt64(), true);
            }
            return new EID(reader.ReadUInt32(), false);
        }

        public void Write(BinaryWriterEx writer)
        {
            if (Is64Bit) writer.WriteInt64((long)Value);
            else writer.WriteUInt32((uint)Value);
        }

        public int CalculateSize() => Is64Bit ? 8 : 4;

        public string ToHex() => Is64Bit ? Value.ToString("X16") : ((uint)Value).ToString("X8");

        public override string ToString() => ToHex();

        public bool Equals(EID other) => other != null && Value == other.Value && Is64Bit == other.Is64Bit;

        public override bool Equals(object obj) => Equals(obj as EID);

        public override int GetHashCode() => Value.GetHashCode();
    }

    public sealed class TString : IEquatable<TString>
    {
        public WString Value { get; }
        public int Index { get; }
        public bool Is32Bit { get; }

        public string Text => Value != null ? Value.Text : string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(Text);

        public TString(WString value, int index, bool is32Bit)
        {
            Value = value;
            Index = index;
            Is32Bit = is32Bit;
        }

        public void WriteIndex(BinaryWriterEx writer)
        {
            if (Is32Bit)
            {
                writer.WriteInt32(Index);
            }
            else
            {
                if (Index >= 0xFFFF)
                {
                    writer.WriteUInt16(0xFFFF);
                    writer.WriteInt32(Index);
                }
                else
                {
                    writer.WriteUInt16((ushort)Index);
                }
            }
        }

        public int CalculateIndexSize()
        {
            if (Is32Bit) return 4;
            return Index >= 0xFFFF ? 6 : 2;
        }

        public void WriteFull(BinaryWriterEx writer) => Value.Write(writer);
        public int CalculateFullSize() => Value.CalculateSize();

        public override string ToString() => Text;

        public bool Equals(TString other)
        {
            return other != null && string.Equals(Text, other.Text, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj) => Equals(obj as TString);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Text);
    }
}

