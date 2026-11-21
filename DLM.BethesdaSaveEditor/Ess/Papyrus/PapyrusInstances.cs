using System.Collections.Generic;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess.Papyrus
{
    public sealed class ScriptInstance
    {
        public EID Id { get; }
        public TString ScriptName { get; }
        public PapyrusScript ScriptDef { get; }
        public short Unknown2Bits { get; }
        public short Unknown { get; }
        public RefID RefID { get; }
        public byte UnknownByte { get; }
        public byte? UnknownFoByte { get; }

        public byte DataFlag { get; private set; }
        public TString State { get; private set; }
        public int DataUnknown1 { get; private set; }
        public int DataUnknown2 { get; private set; }
        public List<PapyrusVariable> Variables { get; private set; }
        public bool HasData { get; private set; }

        public string IdHex => Id.ToHex();
        public string ScriptNome => ScriptName.Text;
        public string RefIDHex => RefID.ToHex();
        public string PluginNome => RefID.Plugin != null ? RefID.Plugin.Name : "(None)";
        public string EstadoTexto => State != null ? State.Text : string.Empty;
        public int QuantidadeVariaveis => Variables != null ? Variables.Count : 0;

        public bool IsUnattached => RefID.IsZero && Unknown != -1;

        public bool IsUndefined
        {
            get
            {
                if (ScriptDef != null) return ScriptDef.IsUndefined;
                return !PapyrusScript.IsNativeName(ScriptName.Text);
            }
        }

        public string StatusTexto
        {
            get
            {
                if (IsUnattached) return "Unattached";
                if (IsUndefined) return "Undefined";
                return "Normal";
            }
        }

        public ScriptInstance(BinaryReaderEx reader, Dictionary<TString, PapyrusScript> scripts, PapyrusBlock ctx)
        {
            Id = ctx.ReadEID(reader);
            ScriptName = ctx.Strings.ReadReference(reader);
            PapyrusScript def;
            if (scripts.TryGetValue(ScriptName, out def))
            {
                ScriptDef = def;
                def.InstanceCount++;
            }

            Unknown2Bits = reader.ReadInt16();
            Unknown = reader.ReadInt16();
            RefID = ctx.Ess.ReadRefID(reader);
            UnknownByte = reader.ReadByte();

            if (ctx.Ess.Header.Game.EhFallout4() && (Unknown2Bits & 0x03) == 0x03)
            {
                UnknownFoByte = reader.ReadByte();
            }
        }

        public void ReadData(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            DataFlag = reader.ReadByte();
            State = ctx.Strings.ReadReference(reader);
            DataUnknown1 = reader.ReadInt32();
            DataUnknown2 = (DataFlag & 0x04) != 0 ? reader.ReadInt32() : 0;
            int count = reader.ReadInt32();
            Variables = PapyrusVariable.ReadList(reader, count, ctx);
            HasData = true;
        }

        public void WriteHeader(BinaryWriterEx writer)
        {
            Id.Write(writer);
            ScriptName.WriteIndex(writer);
            writer.WriteInt16(Unknown2Bits);
            writer.WriteInt16(Unknown);
            RefID.Write(writer);
            writer.WriteByte(UnknownByte);
            if (UnknownFoByte.HasValue) writer.WriteByte(UnknownFoByte.Value);
        }

        public void WriteData(BinaryWriterEx writer)
        {
            Id.Write(writer);
            writer.WriteByte(DataFlag);
            State.WriteIndex(writer);
            writer.WriteInt32(DataUnknown1);
            if ((DataFlag & 0x04) != 0) writer.WriteInt32(DataUnknown2);
            writer.WriteInt32(Variables.Count);
            foreach (PapyrusVariable v in Variables) v.Write(writer);
        }

        public int CalculateSize()
        {
            int sum = Id.CalculateSize() + ScriptName.CalculateIndexSize() + 5 + RefID.CalculateSize();
            if (UnknownFoByte.HasValue) sum++;
            if (HasData)
            {
                sum += Id.CalculateSize() + 1 + State.CalculateIndexSize() + 4 + ((DataFlag & 0x04) != 0 ? 4 : 0) + 4;
                sum += Variables.Sum(v => v.CalculateSize());
            }
            return sum;
        }
    }

    public sealed class PapyrusReference
    {
        public EID Id { get; }
        public TString ScriptName { get; }
        public PapyrusScript ScriptDef { get; }

        public byte DataFlag { get; private set; }
        public TString RefType { get; private set; }
        public int DataUnknown1 { get; private set; }
        public int DataUnknown2 { get; private set; }
        public List<PapyrusVariable> Variables { get; private set; }
        public bool HasData { get; private set; }

        public bool IsUndefined
        {
            get
            {
                if (ScriptDef != null) return ScriptDef.IsUndefined;
                return !PapyrusScript.IsNativeName(ScriptName.Text);
            }
        }

        public PapyrusReference(BinaryReaderEx reader, Dictionary<TString, PapyrusScript> scripts, PapyrusBlock ctx)
        {
            Id = ctx.ReadEID(reader);
            ScriptName = ctx.Strings.ReadReference(reader);
            PapyrusScript def;
            if (scripts.TryGetValue(ScriptName, out def))
            {
                ScriptDef = def;
                def.InstanceCount++;
            }
        }

        public void ReadData(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            DataFlag = reader.ReadByte();
            RefType = ctx.Strings.ReadReference(reader);
            DataUnknown1 = reader.ReadInt32();
            DataUnknown2 = (DataFlag & 0x04) != 0 ? reader.ReadInt32() : 0;
            int count = reader.ReadInt32();
            Variables = PapyrusVariable.ReadList(reader, count, ctx);
            HasData = true;
        }

        public void WriteHeader(BinaryWriterEx writer)
        {
            Id.Write(writer);
            ScriptName.WriteIndex(writer);
        }

        public void WriteData(BinaryWriterEx writer)
        {
            Id.Write(writer);
            writer.WriteByte(DataFlag);
            RefType.WriteIndex(writer);
            writer.WriteInt32(DataUnknown1);
            if ((DataFlag & 0x04) != 0) writer.WriteInt32(DataUnknown2);
            writer.WriteInt32(Variables.Count);
            foreach (PapyrusVariable v in Variables) v.Write(writer);
        }

        public int CalculateSize()
        {
            int sum = Id.CalculateSize() + ScriptName.CalculateIndexSize();
            if (HasData)
            {
                sum += Id.CalculateSize() + 1 + RefType.CalculateIndexSize() + 4 + ((DataFlag & 0x04) != 0 ? 4 : 0) + 4;
                sum += Variables.Sum(v => v.CalculateSize());
            }
            return sum;
        }
    }

    public sealed class StructInstance
    {
        public EID Id { get; }
        public TString StructName { get; }
        public PapyrusStruct StructDef { get; }

        public byte DataFlag { get; private set; }
        public List<PapyrusVariable> Variables { get; private set; }
        public bool HasData { get; private set; }

        public bool IsUndefined => StructDef != null && StructDef.IsUndefined;

        public StructInstance(BinaryReaderEx reader, Dictionary<TString, PapyrusStruct> structs, PapyrusBlock ctx)
        {
            Id = ctx.ReadEID(reader);
            StructName = ctx.Strings.ReadReference(reader);
            PapyrusStruct def;
            if (structs.TryGetValue(StructName, out def))
            {
                StructDef = def;
                def.InstanceCount++;
            }
        }

        public void ReadData(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            DataFlag = reader.ReadByte();
            int count = reader.ReadInt32();
            Variables = PapyrusVariable.ReadList(reader, count, ctx);
            HasData = true;
        }

        public void WriteHeader(BinaryWriterEx writer)
        {
            Id.Write(writer);
            StructName.WriteIndex(writer);
        }

        public void WriteData(BinaryWriterEx writer)
        {
            Id.Write(writer);
            writer.WriteByte(DataFlag);
            writer.WriteInt32(Variables.Count);
            foreach (PapyrusVariable v in Variables) v.Write(writer);
        }

        public int CalculateSize()
        {
            int sum = Id.CalculateSize() + StructName.CalculateIndexSize();
            if (HasData)
            {
                sum += Id.CalculateSize() + 1 + 4 + Variables.Sum(v => v.CalculateSize());
            }
            return sum;
        }
    }

    public sealed class ArrayInfo
    {
        public EID Id { get; }
        public PapyrusVarType ElementType { get; }
        public TString RefType { get; }
        public int Length { get; set; }
        public List<PapyrusVariable> Variables { get; private set; }
        public bool HasData { get; private set; }

        public string IdHex => Id.ToHex();
        public string TipoTexto => RefType != null ? (RefType.Text + "[]") : (ElementType + "[]");

        public ArrayInfo(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            Id = ctx.ReadEID(reader);
            ElementType = (PapyrusVarType)reader.ReadByte();
            RefType = ElementType.IsRefType() ? ctx.Strings.ReadReference(reader) : null;
            Length = reader.ReadInt32();
        }

        public void ReadData(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            Variables = PapyrusVariable.ReadList(reader, Length, ctx);
            HasData = true;
        }

        public void AddEntry(PapyrusBlock ctx)
        {
            if (Variables == null) Variables = new List<PapyrusVariable>();
            Variables.Add(PapyrusVariable.CreateDefault(ElementType, RefType, ctx));
            Length = Variables.Count;
        }

        public bool RemoveEntry(int index)
        {
            if (Variables == null || index < 0 || index >= Variables.Count) return false;
            Variables.RemoveAt(index);
            Length = Variables.Count;
            return true;
        }

        public bool ShiftUp(int index)
        {
            if (Variables == null || index <= 0 || index >= Variables.Count) return false;
            PapyrusVariable tmp = Variables[index - 1];
            Variables[index - 1] = Variables[index];
            Variables[index] = tmp;
            return true;
        }

        public bool ShiftDown(int index)
        {
            if (Variables == null || index < 0 || index >= Variables.Count - 1) return false;
            PapyrusVariable tmp = Variables[index + 1];
            Variables[index + 1] = Variables[index];
            Variables[index] = tmp;
            return true;
        }

        public void WriteHeader(BinaryWriterEx writer)
        {
            Id.Write(writer);
            writer.WriteByte((byte)ElementType);
            if (RefType != null) RefType.WriteIndex(writer);
            writer.WriteInt32(Length);
        }

        public void WriteData(BinaryWriterEx writer)
        {
            Id.Write(writer);
            foreach (PapyrusVariable v in Variables) v.Write(writer);
        }

        public int CalculateSize()
        {
            int sum = Id.CalculateSize() + 1 + (RefType != null ? RefType.CalculateIndexSize() : 0) + 4;
            if (HasData)
            {
                sum += Id.CalculateSize() + Variables.Sum(v => v.CalculateSize());
            }
            return sum;
        }
    }
}
