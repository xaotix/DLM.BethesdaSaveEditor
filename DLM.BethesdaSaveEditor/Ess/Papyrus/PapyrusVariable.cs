using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess.Papyrus
{
    public sealed class PapyrusVariable
    {
        public PapyrusVarType VarType { get; }
        public int IntValue { get; set; }
        public float FloatValue { get; set; }
        public TString StringValue { get; set; }
        public TString RefType { get; set; }
        public EID RefId { get; set; }
        public PapyrusVariable VariantInner { get; set; }

        public bool IsEditable => VarType == PapyrusVarType.Integer
            || VarType == PapyrusVarType.Float
            || VarType == PapyrusVarType.Boolean
            || VarType == PapyrusVarType.String
            || VarType == PapyrusVarType.Ref;

        private PapyrusVariable(PapyrusVarType type)
        {
            VarType = type;
        }

        public static PapyrusVariable CreateDefault(PapyrusVarType type, TString refType, PapyrusBlock ctx)
        {
            PapyrusVariable v = new PapyrusVariable(type);
            bool id64 = ctx.Ess.Header.Game.EhID64();

            switch (type)
            {
                case PapyrusVarType.Null:
                case PapyrusVarType.Integer:
                case PapyrusVarType.Boolean:
                    v.IntValue = 0;
                    break;
                case PapyrusVarType.Float:
                    v.FloatValue = 0f;
                    break;
                case PapyrusVarType.String:
                    v.StringValue = ctx.Strings.AddString(string.Empty);
                    break;
                case PapyrusVarType.Ref:
                case PapyrusVarType.Struct:
                case PapyrusVarType.RefArray:
                case PapyrusVarType.StructArray:
                    v.RefType = refType ?? ctx.Strings.AddString("Form");
                    v.RefId = new EID(0, id64);
                    break;
                case PapyrusVarType.Variant:
                    v.VariantInner = new PapyrusVariable(PapyrusVarType.Null);
                    break;
                default:
                    v.RefId = new EID(0, id64);
                    break;
            }
            return v;
        }

        public bool TrySetFromText(string text, PapyrusBlock ctx, out string error)
        {
            error = null;
            string s = (text ?? string.Empty).Trim();

            switch (VarType)
            {
                case PapyrusVarType.Integer:
                    int iv;
                    if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out iv))
                    {
                        error = "Invalid integer value.";
                        return false;
                    }
                    IntValue = iv;
                    return true;

                case PapyrusVarType.Float:
                    float fv;
                    if (!float.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out fv))
                    {
                        error = "Invalid float value.";
                        return false;
                    }
                    FloatValue = fv;
                    return true;

                case PapyrusVarType.Boolean:
                    if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || s == "1")
                    {
                        IntValue = 1;
                        return true;
                    }
                    if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) || s == "0")
                    {
                        IntValue = 0;
                        return true;
                    }
                    error = "Use True/False or 1/0.";
                    return false;

                case PapyrusVarType.String:
                    if (s.Length >= 2 && s.StartsWith("\"") && s.EndsWith("\""))
                    {
                        s = s.Substring(1, s.Length - 2);
                    }
                    StringValue = ctx.Strings.AddString(s);
                    return true;

                case PapyrusVarType.Ref:
                    string hex = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s.Substring(2) : s;
                    ulong idVal;
                    if (string.Equals(hex, "null", StringComparison.OrdinalIgnoreCase))
                    {
                        idVal = 0;
                    }
                    else if (!ulong.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out idVal))
                    {
                        error = "Invalid hex EID (use 0 or NULL to clear).";
                        return false;
                    }
                    RefId = new EID(idVal, RefId != null ? RefId.Is64Bit : ctx.Ess.Header.Game.EhID64());
                    return true;

                default:
                    error = "This variable type cannot be edited directly.";
                    return false;
            }
        }

        public static List<PapyrusVariable> ReadList(BinaryReaderEx reader, int count, PapyrusBlock ctx)
        {
            List<PapyrusVariable> list = new List<PapyrusVariable>(count);
            for (int i = 0; i < count; i++) list.Add(Read(reader, ctx));
            return list;
        }

        public static PapyrusVariable Read(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            PapyrusVarType type = (PapyrusVarType)reader.ReadByte();
            PapyrusVariable v = new PapyrusVariable(type);

            switch (type)
            {
                case PapyrusVarType.Null:
                case PapyrusVarType.Integer:
                case PapyrusVarType.Boolean:
                    v.IntValue = reader.ReadInt32();
                    break;
                case PapyrusVarType.Float:
                    v.FloatValue = reader.ReadSingle();
                    break;
                case PapyrusVarType.String:
                    v.StringValue = ctx.Strings.ReadReference(reader);
                    break;
                case PapyrusVarType.Ref:
                case PapyrusVarType.Struct:
                case PapyrusVarType.RefArray:
                case PapyrusVarType.StructArray:
                    v.RefType = ctx.Strings.ReadReference(reader);
                    v.RefId = ctx.ReadEID(reader);
                    break;
                case PapyrusVarType.Variant:
                    v.VariantInner = Read(reader, ctx);
                    break;
                case PapyrusVarType.StringArray:
                case PapyrusVarType.IntegerArray:
                case PapyrusVarType.FloatArray:
                case PapyrusVarType.BooleanArray:
                case PapyrusVarType.VariantArray:
                    v.RefId = ctx.ReadEID(reader);
                    break;
                default:
                    throw new InvalidDataException("Invalid Papyrus variable typecode: " + (byte)type);
            }

            return v;
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteByte((byte)VarType);
            switch (VarType)
            {
                case PapyrusVarType.Null:
                case PapyrusVarType.Integer:
                case PapyrusVarType.Boolean:
                    writer.WriteInt32(IntValue);
                    break;
                case PapyrusVarType.Float:
                    writer.WriteSingle(FloatValue);
                    break;
                case PapyrusVarType.String:
                    StringValue.WriteIndex(writer);
                    break;
                case PapyrusVarType.Ref:
                case PapyrusVarType.Struct:
                case PapyrusVarType.RefArray:
                case PapyrusVarType.StructArray:
                    RefType.WriteIndex(writer);
                    RefId.Write(writer);
                    break;
                case PapyrusVarType.Variant:
                    VariantInner.Write(writer);
                    break;
                default:
                    RefId.Write(writer);
                    break;
            }
        }

        public int CalculateSize()
        {
            switch (VarType)
            {
                case PapyrusVarType.Null:
                case PapyrusVarType.Integer:
                case PapyrusVarType.Float:
                case PapyrusVarType.Boolean:
                    return 5;
                case PapyrusVarType.String:
                    return 1 + StringValue.CalculateIndexSize();
                case PapyrusVarType.Ref:
                case PapyrusVarType.Struct:
                case PapyrusVarType.RefArray:
                case PapyrusVarType.StructArray:
                    return 1 + RefType.CalculateIndexSize() + RefId.CalculateSize();
                case PapyrusVarType.Variant:
                    return 1 + VariantInner.CalculateSize();
                default:
                    return 1 + RefId.CalculateSize();
            }
        }

        public string ToRawEditText()
        {
            switch (VarType)
            {
                case PapyrusVarType.Integer: return IntValue.ToString(CultureInfo.InvariantCulture);
                case PapyrusVarType.Float: return FloatValue.ToString("G6", CultureInfo.InvariantCulture);
                case PapyrusVarType.Boolean: return (IntValue != 0).ToString();
                case PapyrusVarType.String: return StringValue != null ? StringValue.Text : string.Empty;
                case PapyrusVarType.Ref: return RefId != null ? RefId.ToHex() : "0";
                default: return ToValueDisplay();
            }
        }

        public string ToValueDisplay()
        {
            switch (VarType)
            {
                case PapyrusVarType.Null: return "NULL";
                case PapyrusVarType.Integer: return IntValue.ToString();
                case PapyrusVarType.Float: return FloatValue.ToString("G6");
                case PapyrusVarType.Boolean: return (IntValue != 0).ToString();
                case PapyrusVarType.String: return "\"" + (StringValue != null ? StringValue.Text : string.Empty) + "\"";
                case PapyrusVarType.Variant: return VariantInner != null ? VariantInner.ToValueDisplay() : "NULL";
                default:
                    return RefType != null
                        ? string.Format("{0} ({1})", RefType.Text, RefId)
                        : (RefId != null ? RefId.ToString() : "0");
            }
        }
    }

    public sealed class OpcodeParameter
    {
        public ParamType Type { get; }
        public TString TextValue { get; }
        public int IntValue { get; }
        public float FloatValue { get; }
        public byte BoolValue { get; }

        private OpcodeParameter(ParamType type, TString text = null, int i = 0, float f = 0f, byte b = 0)
        {
            Type = type;
            TextValue = text;
            IntValue = i;
            FloatValue = f;
            BoolValue = b;
        }

        public static OpcodeParameter Read(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            ParamType t = (ParamType)reader.ReadByte();
            switch (t)
            {
                case ParamType.Null: return new OpcodeParameter(t);
                case ParamType.Identifier:
                case ParamType.String:
                case ParamType.Unknown8: return new OpcodeParameter(t, ctx.Strings.ReadReference(reader));
                case ParamType.Integer: return new OpcodeParameter(t, null, reader.ReadInt32());
                case ParamType.Float: return new OpcodeParameter(t, null, 0, reader.ReadSingle());
                case ParamType.Boolean: return new OpcodeParameter(t, null, 0, 0f, reader.ReadByte());
                default: throw new InvalidDataException("Invalid opcode parameter type: " + (byte)t);
            }
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteByte((byte)Type);
            switch (Type)
            {
                case ParamType.Identifier:
                case ParamType.String:
                case ParamType.Unknown8:
                    TextValue.WriteIndex(writer);
                    break;
                case ParamType.Integer:
                    writer.WriteInt32(IntValue);
                    break;
                case ParamType.Float:
                    writer.WriteSingle(FloatValue);
                    break;
                case ParamType.Boolean:
                    writer.WriteByte(BoolValue);
                    break;
            }
        }

        public int CalculateSize()
        {
            switch (Type)
            {
                case ParamType.Null: return 1;
                case ParamType.Identifier:
                case ParamType.String:
                case ParamType.Unknown8: return 1 + TextValue.CalculateIndexSize();
                case ParamType.Integer:
                case ParamType.Float: return 5;
                case ParamType.Boolean: return 2;
                default: return 1;
            }
        }
    }

    public sealed class OpcodeData
    {
        public static readonly OpcodeData Nop = new OpcodeData();

        public PapyrusOpcode Opcode { get; }
        public List<OpcodeParameter> Parameters { get; }

        private OpcodeData()
        {
            Opcode = PapyrusOpcode.NOP;
            Parameters = new List<OpcodeParameter>(0);
        }

        public OpcodeData(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            byte code = reader.ReadByte();
            if (code > (byte)PapyrusOpcode.ARR_GET_MATCHING)
            {
                throw new InvalidDataException("Invalid Papyrus opcode: " + code);
            }

            Opcode = (PapyrusOpcode)code;
            int fixedCount = Opcode.GetFixedCount();
            Parameters = new List<OpcodeParameter>(fixedCount);

            for (int i = 0; i < fixedCount; i++) Parameters.Add(OpcodeParameter.Read(reader, ctx));

            if (Opcode.HasExtraTerms() && Parameters.Count > 0)
            {
                int varCount = Parameters[Parameters.Count - 1].IntValue;
                for (int i = 0; i < varCount; i++) Parameters.Add(OpcodeParameter.Read(reader, ctx));
            }
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteByte((byte)Opcode);
            foreach (OpcodeParameter p in Parameters) p.Write(writer);
        }

        public int CalculateSize() => 1 + Parameters.Sum(p => p.CalculateSize());
    }
}
