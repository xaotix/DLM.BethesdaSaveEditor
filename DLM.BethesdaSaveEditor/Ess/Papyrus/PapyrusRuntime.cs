using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess.Papyrus
{
    public sealed class FragmentTask
    {
        public byte Unknown3 { get; }
        public byte[] TypeCode { get; }
        public string TypeName { get; }
        public EID RunningId { get; }
        public RefID QuestOrFormId { get; }
        public short Stage { get; }
        public byte FlagsByte { get; }
        public int IntVal { get; }
        public int? UnknownFo4Bytes { get; }
        public TString TerminalStr { get; }
        public PapyrusVariable Variable { get; }

        public FragmentTask(BinaryReaderEx reader, byte unknown3, PapyrusBlock ctx)
        {
            Unknown3 = unknown3;
            bool isFo4 = ctx.Ess.Header.Game.EhFallout4();

            if (unknown3 == 2)
            {
                if (isFo4)
                {
                    RunningId = ctx.ReadEID32(reader);
                }
            }
            else
            {
                TypeCode = reader.ReadLStringRaw();
                TypeName = Encoding.UTF8.GetString(TypeCode);

                switch (TypeName)
                {
                    case "QuestStage":
                        QuestOrFormId = ctx.Ess.ReadRefID(reader);
                        Stage = reader.ReadInt16();
                        FlagsByte = reader.ReadByte();
                        if (isFo4) UnknownFo4Bytes = reader.ReadInt32();
                        break;

                    case "ScenePhaseResults":
                    case "SceneActionResults":
                        QuestOrFormId = ctx.Ess.ReadRefID(reader);
                        IntVal = reader.ReadInt32();
                        if (isFo4) UnknownFo4Bytes = reader.ReadInt32();
                        break;

                    case "SceneResults":
                        QuestOrFormId = ctx.Ess.ReadRefID(reader);
                        if (isFo4) UnknownFo4Bytes = reader.ReadInt32();
                        break;

                    case "TerminalRunResults":
                        FlagsByte = reader.ReadByte();
                        IntVal = reader.ReadInt32();
                        QuestOrFormId = ctx.Ess.ReadRefID(reader);
                        TerminalStr = ctx.Strings.ReadReference(reader);
                        break;

                    case "TopicInfo":
                        break;

                    default:
                        throw new InvalidDataException("Tipo de FragmentTask desconhecido: " + TypeName);
                }
            }

            if (unknown3 == 2 || unknown3 == 3)
            {
                Variable = PapyrusVariable.Read(reader, ctx);
            }
        }

        public void Write(BinaryWriterEx writer)
        {
            if (Unknown3 == 2)
            {
                if (RunningId != null) RunningId.Write(writer);
            }
            else
            {
                writer.WriteInt32(TypeCode.Length);
                writer.WriteBytes(TypeCode);

                switch (TypeName)
                {
                    case "QuestStage":
                        QuestOrFormId.Write(writer);
                        writer.WriteInt16(Stage);
                        writer.WriteByte(FlagsByte);
                        if (UnknownFo4Bytes.HasValue) writer.WriteInt32(UnknownFo4Bytes.Value);
                        break;

                    case "ScenePhaseResults":
                    case "SceneActionResults":
                        QuestOrFormId.Write(writer);
                        writer.WriteInt32(IntVal);
                        if (UnknownFo4Bytes.HasValue) writer.WriteInt32(UnknownFo4Bytes.Value);
                        break;

                    case "SceneResults":
                        QuestOrFormId.Write(writer);
                        if (UnknownFo4Bytes.HasValue) writer.WriteInt32(UnknownFo4Bytes.Value);
                        break;

                    case "TerminalRunResults":
                        writer.WriteByte(FlagsByte);
                        writer.WriteInt32(IntVal);
                        QuestOrFormId.Write(writer);
                        TerminalStr.WriteIndex(writer);
                        break;
                }
            }

            if (Variable != null)
            {
                Variable.Write(writer);
            }
        }

        public int CalculateSize()
        {
            int sum = 0;
            if (Unknown3 == 2)
            {
                if (RunningId != null) sum += RunningId.CalculateSize();
            }
            else
            {
                sum += 4 + TypeCode.Length;
                switch (TypeName)
                {
                    case "QuestStage":
                        sum += 3 + 2 + 1 + (UnknownFo4Bytes.HasValue ? 4 : 0);
                        break;
                    case "ScenePhaseResults":
                    case "SceneActionResults":
                        sum += 3 + 4 + (UnknownFo4Bytes.HasValue ? 4 : 0);
                        break;
                    case "SceneResults":
                        sum += 3 + (UnknownFo4Bytes.HasValue ? 4 : 0);
                        break;
                    case "TerminalRunResults":
                        sum += 1 + 4 + 3 + TerminalStr.CalculateIndexSize();
                        break;
                }
            }
            if (Variable != null) sum += Variable.CalculateSize();
            return sum;
        }
    }

    public sealed class StackFrame
    {
        public byte Flag { get; }
        public PapyrusVarType FnType { get; }
        public TString ScriptName { get; }
        public TString BaseName { get; }
        public TString EventName { get; }
        public TString Status { get; }
        public byte OpcodeMajor { get; }
        public byte OpcodeMinor { get; }
        public TString ReturnType { get; }
        public TString DocString { get; }
        public int UserFlags { get; }
        public byte FnFlags { get; }
        public List<MemberDesc> Params { get; }
        public List<MemberDesc> Locals { get; }
        public List<OpcodeData> Code { get; }
        public int InstructionPtr { get; }
        public PapyrusVariable OwnerField { get; }
        public List<PapyrusVariable> Variables { get; }

        public bool IsNative => (Flag & 0x01) != 0;
        public bool IsZeroed => Code.Count > 0 && Code.All(op => op.Opcode == PapyrusOpcode.NOP);

        public StackFrame(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            int varCount = reader.ReadInt32();
            Flag = reader.ReadByte();
            FnType = (PapyrusVarType)reader.ReadByte();
            ScriptName = ctx.Strings.ReadReference(reader);
            BaseName = ctx.Strings.ReadReference(reader);
            EventName = ctx.Strings.ReadReference(reader);

            if ((Flag & 0x01) == 0 && FnType == PapyrusVarType.Null)
            {
                Status = ctx.Strings.ReadReference(reader);
            }

            OpcodeMajor = reader.ReadByte();
            OpcodeMinor = reader.ReadByte();
            ReturnType = ctx.Strings.ReadReference(reader);
            DocString = ctx.Strings.ReadReference(reader);
            UserFlags = reader.ReadInt32();
            FnFlags = reader.ReadByte();

            int paramCount = reader.ReadUInt16();
            Params = MemberDesc.ReadList(reader, paramCount, ctx);

            int localCount = reader.ReadUInt16();
            Locals = MemberDesc.ReadList(reader, localCount, ctx);

            int opCount = reader.ReadUInt16();
            Code = new List<OpcodeData>(opCount);
            for (int i = 0; i < opCount; i++)
            {
                Code.Add(new OpcodeData(reader, ctx));
            }

            InstructionPtr = reader.ReadInt32();
            OwnerField = PapyrusVariable.Read(reader, ctx);
            Variables = PapyrusVariable.ReadList(reader, varCount, ctx);
        }

        public void Zero()
        {
            for (int i = 0; i < Code.Count; i++)
            {
                Code[i] = OpcodeData.Nop;
            }
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteInt32(Variables.Count);
            writer.WriteByte(Flag);
            writer.WriteByte((byte)FnType);
            ScriptName.WriteIndex(writer);
            BaseName.WriteIndex(writer);
            EventName.WriteIndex(writer);
            if (Status != null) Status.WriteIndex(writer);
            writer.WriteByte(OpcodeMajor);
            writer.WriteByte(OpcodeMinor);
            ReturnType.WriteIndex(writer);
            DocString.WriteIndex(writer);
            writer.WriteInt32(UserFlags);
            writer.WriteByte(FnFlags);

            writer.WriteUInt16((ushort)Params.Count);
            foreach (MemberDesc p in Params) p.Write(writer);

            writer.WriteUInt16((ushort)Locals.Count);
            foreach (MemberDesc l in Locals) l.Write(writer);

            writer.WriteUInt16((ushort)Code.Count);
            foreach (OpcodeData op in Code) op.Write(writer);

            writer.WriteInt32(InstructionPtr);
            OwnerField.Write(writer);
            foreach (PapyrusVariable v in Variables) v.Write(writer);
        }

        public int CalculateSize()
        {
            int sum = 4 + 1 + 1;
            sum += ScriptName.CalculateIndexSize() + BaseName.CalculateIndexSize() + EventName.CalculateIndexSize();
            if (Status != null) sum += Status.CalculateIndexSize();
            sum += 2 + ReturnType.CalculateIndexSize() + DocString.CalculateIndexSize() + 4 + 1;
            sum += 2 + Params.Sum(p => p.CalculateSize());
            sum += 2 + Locals.Sum(l => l.CalculateSize());
            sum += 2 + Code.Sum(c => c.CalculateSize());
            sum += 4 + OwnerField.CalculateSize() + Variables.Sum(v => v.CalculateSize());
            return sum;
        }
    }

    public sealed class ActiveScript
    {
        public EID Id { get; }
        public byte Type { get; }

        public byte MajorVersion { get; private set; }
        public byte MinorVersion { get; private set; }
        public PapyrusVariable UnknownVar { get; private set; }
        public byte Flag { get; private set; }
        public byte UnknownByte { get; private set; }
        public int Unknown2 { get; private set; }
        public byte Unknown3 { get; private set; }
        public FragmentTask Fragment { get; private set; }
        public EID Attached { get; private set; }
        public List<StackFrame> StackFrames { get; private set; }
        public byte? Unknown5 { get; private set; }
        public bool HasData { get; private set; }

        public string IdHex => Id.ToHex();
        public int ProfundidadePilha => StackFrames != null ? StackFrames.Count : 0;
        public string ScriptTopo => (StackFrames != null && StackFrames.Count > 0) ? StackFrames[0].ScriptName.Text : "(Sem pilha)";
        public string EventoTopo => (StackFrames != null && StackFrames.Count > 0) ? StackFrames[0].EventName.Text : string.Empty;

        public bool IsTerminated => StackFrames != null && StackFrames.Count > 0
            && !StackFrames[0].IsNative
            && StackFrames[0].IsZeroed
            && StackFrames.All(f => f.IsZeroed || f.IsNative);

        public ActiveScript(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            Id = ctx.ReadEID32(reader);
            Type = reader.ReadByte();
            StackFrames = new List<StackFrame>();
        }

        public void ReadData(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            MajorVersion = reader.ReadByte();
            MinorVersion = reader.ReadByte();
            UnknownVar = PapyrusVariable.Read(reader, ctx);
            Flag = reader.ReadByte();
            UnknownByte = reader.ReadByte();
            Unknown2 = (Flag & 0x01) == 0x01 ? reader.ReadInt32() : 0;
            Unknown3 = reader.ReadByte();

            if (Unknown3 >= 1 && Unknown3 <= 3)
            {
                Fragment = new FragmentTask(reader, Unknown3, ctx);
            }

            if (ctx.Ess.Header.Game.EhFallout4())
            {
                if (Fragment == null || Fragment.TypeName != "TerminalRunResults")
                {
                    Attached = ctx.ReadEID64(reader);
                }
            }

            int count = reader.ReadInt32();
            StackFrames = new List<StackFrame>(count);
            for (int i = 0; i < count; i++)
            {
                StackFrames.Add(new StackFrame(reader, ctx));
            }

            bool fo4Special = Flag == 0 && ctx.Ess.Header.Game.EhFallout4();
            if (StackFrames.Count > 0 && !fo4Special)
            {
                Unknown5 = reader.ReadByte();
            }

            HasData = true;
        }

        public bool IsUndefined(Dictionary<TString, PapyrusScript> scripts)
        {
            if (StackFrames == null || StackFrames.Count == 0) return false;
            foreach (StackFrame sf in StackFrames)
            {
                PapyrusScript s;
                if (!scripts.TryGetValue(sf.ScriptName, out s))
                {
                    if (!PapyrusScript.IsNativeName(sf.ScriptName.Text)) return true;
                }
                else if (s.IsUndefined)
                {
                    return true;
                }
            }
            return false;
        }

        public void Zero()
        {
            if (StackFrames != null)
            {
                foreach (StackFrame sf in StackFrames)
                {
                    sf.Zero();
                }
            }
        }

        public void WriteHeader(BinaryWriterEx writer)
        {
            Id.Write(writer);
            writer.WriteByte(Type);
        }

        public void WriteData(BinaryWriterEx writer)
        {
            Id.Write(writer);
            writer.WriteByte(MajorVersion);
            writer.WriteByte(MinorVersion);
            UnknownVar.Write(writer);
            writer.WriteByte(Flag);
            writer.WriteByte(UnknownByte);
            if ((Flag & 0x01) == 0x01) writer.WriteInt32(Unknown2);
            writer.WriteByte(Unknown3);
            if (Fragment != null) Fragment.Write(writer);
            if (Attached != null) Attached.Write(writer);
            writer.WriteInt32(StackFrames.Count);
            foreach (StackFrame sf in StackFrames) sf.Write(writer);
            if (Unknown5.HasValue) writer.WriteByte(Unknown5.Value);
        }

        public int CalculateSize()
        {
            int sum = Id.CalculateSize() + 1;
            if (HasData)
            {
                sum += Id.CalculateSize() + 2 + UnknownVar.CalculateSize() + 2;
                sum += (Flag & 0x01) == 0x01 ? 4 : 0;
                sum += 1 + (Fragment != null ? Fragment.CalculateSize() : 0);
                sum += Attached != null ? Attached.CalculateSize() : 0;
                sum += 4 + StackFrames.Sum(f => f.CalculateSize());
                sum += Unknown5.HasValue ? 1 : 0;
            }
            return sum;
        }
    }

    public sealed class FunctionMessageData
    {
        public byte FnFlags { get; }
        public TString ScriptName { get; }
        public TString EventName { get; }
        public PapyrusVariable UnknownVar { get; }
        public List<PapyrusVariable> Variables { get; }

        public FunctionMessageData(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            FnFlags = reader.ReadByte();
            ScriptName = ctx.Strings.ReadReference(reader);
            EventName = ctx.Strings.ReadReference(reader);
            UnknownVar = PapyrusVariable.Read(reader, ctx);
            int count = reader.ReadInt32();
            Variables = PapyrusVariable.ReadList(reader, count, ctx);
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteByte(FnFlags);
            ScriptName.WriteIndex(writer);
            EventName.WriteIndex(writer);
            UnknownVar.Write(writer);
            writer.WriteInt32(Variables.Count);
            foreach (PapyrusVariable v in Variables) v.Write(writer);
        }

        public int CalculateSize()
        {
            return 1 + ScriptName.CalculateIndexSize() + EventName.CalculateIndexSize()
                + UnknownVar.CalculateSize() + 4 + Variables.Sum(v => v.CalculateSize());
        }
    }

    public sealed class FunctionMessage
    {
        public byte Flag { get; }
        public EID Id { get; }
        public byte Flags { get; }
        public FunctionMessageData Message { get; }

        public FunctionMessage(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            Flag = reader.ReadByte();
            Id = Flag <= 2 ? ctx.ReadEID32(reader) : null;
            Flags = reader.ReadByte();
            if (Flags != 0)
            {
                Message = new FunctionMessageData(reader, ctx);
            }
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteByte(Flag);
            if (Id != null) Id.Write(writer);
            writer.WriteByte(Flags);
            if (Message != null) Message.Write(writer);
        }

        public int CalculateSize()
        {
            return 2 + (Id != null ? Id.CalculateSize() : 0) + (Message != null ? Message.CalculateSize() : 0);
        }
    }

    public sealed class SuspendedStack
    {
        public EID Id { get; }
        public byte Flag { get; }
        public FunctionMessageData Message { get; }

        public SuspendedStack(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            Id = ctx.ReadEID32(reader);
            Flag = reader.ReadByte();
            if (Flag != 0)
            {
                Message = new FunctionMessageData(reader, ctx);
            }
        }

        public void Write(BinaryWriterEx writer)
        {
            Id.Write(writer);
            writer.WriteByte(Flag);
            if (Message != null) Message.Write(writer);
        }

        public int CalculateSize()
        {
            return Id.CalculateSize() + 1 + (Message != null ? Message.CalculateSize() : 0);
        }
    }

    public sealed class QueuedUnbind
    {
        public EID Id { get; }
        public int Unknown { get; }

        public QueuedUnbind(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            Id = ctx.ReadEID(reader);
            Unknown = reader.ReadInt32();
        }

        public void Write(BinaryWriterEx writer)
        {
            Id.Write(writer);
            writer.WriteInt32(Unknown);
        }

        public int CalculateSize() => Id.CalculateSize() + 4;
    }
}

