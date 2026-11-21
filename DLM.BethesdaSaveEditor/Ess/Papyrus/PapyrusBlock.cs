using System;
using System.Collections.Generic;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess.Papyrus
{
    public sealed class PapyrusBlock : IGlobalDataBlock
    {
        public EssFile Ess { get; }
        public short Header { get; }
        public StringTable Strings { get; }
        public Dictionary<TString, PapyrusScript> Scripts { get; }
        public Dictionary<TString, PapyrusStruct> Structs { get; }
        public Dictionary<EID, ScriptInstance> ScriptInstances { get; }
        public Dictionary<EID, PapyrusReference> References { get; }
        public Dictionary<EID, StructInstance> StructInstances { get; }
        public Dictionary<EID, ArrayInfo> Arrays { get; }
        public EID PapyrusRuntime { get; }
        public Dictionary<EID, ActiveScript> ActiveScripts { get; }
        public List<FunctionMessage> FunctionMessages { get; }
        public List<SuspendedStack> SuspendedStacks1 { get; }
        public List<SuspendedStack> SuspendedStacks2 { get; }
        public int Unk1 { get; }
        public int? Unk2 { get; }
        public List<EID> UnknownIDs { get; }
        public List<QueuedUnbind> Unbinds { get; }
        public short? SaveFileVersion { get; }
        public byte[] TrailingArraysBlock { get; }

        public bool IsBroken => Strings == null || Strings.HasStringTableBug;

        private bool IsId64 => Ess.Header.Game.EhID64();
        private bool IsFo4 => Ess.Header.Game.EhFallout4();

        public EID ReadEID(BinaryReaderEx reader) => EID.Read(reader, IsId64);
        public EID ReadEID32(BinaryReaderEx reader) => EID.Read(reader, false);
        public EID ReadEID64(BinaryReaderEx reader) => EID.Read(reader, true);

        public PapyrusBlock(BinaryReaderEx reader, EssFile ess)
        {
            Ess = ess;
            Header = reader.ReadInt16();
            Strings = new StringTable(reader, ess);

            int scriptCount = reader.ReadInt32();
            int structCount = IsFo4 ? reader.ReadInt32() : 0;

            Scripts = new Dictionary<TString, PapyrusScript>(scriptCount);
            for (int i = 0; i < scriptCount; i++)
            {
                PapyrusScript s = new PapyrusScript(reader, this);
                Scripts[s.Name] = s;
            }

            if (IsFo4)
            {
                Structs = new Dictionary<TString, PapyrusStruct>(structCount);
                for (int i = 0; i < structCount; i++)
                {
                    PapyrusStruct st = new PapyrusStruct(reader, this);
                    Structs[st.Name] = st;
                }
            }

            int instCount = reader.ReadInt32();
            ScriptInstances = new Dictionary<EID, ScriptInstance>(instCount);
            for (int i = 0; i < instCount; i++)
            {
                ScriptInstance inst = new ScriptInstance(reader, Scripts, this);
                ScriptInstances[inst.Id] = inst;
            }

            int refCount = reader.ReadInt32();
            References = new Dictionary<EID, PapyrusReference>(refCount);
            for (int i = 0; i < refCount; i++)
            {
                PapyrusReference r = new PapyrusReference(reader, Scripts, this);
                References[r.Id] = r;
            }

            if (IsFo4)
            {
                int structInstCount = reader.ReadInt32();
                StructInstances = new Dictionary<EID, StructInstance>(structInstCount);
                for (int i = 0; i < structInstCount; i++)
                {
                    StructInstance si = new StructInstance(reader, Structs, this);
                    StructInstances[si.Id] = si;
                }
            }

            int arrCount = reader.ReadInt32();
            Arrays = new Dictionary<EID, ArrayInfo>(arrCount);
            for (int i = 0; i < arrCount; i++)
            {
                ArrayInfo arr = new ArrayInfo(reader, this);
                Arrays[arr.Id] = arr;
            }

            PapyrusRuntime = ReadEID32(reader);

            int activeCount = reader.ReadInt32();
            ActiveScripts = new Dictionary<EID, ActiveScript>(activeCount);
            for (int i = 0; i < activeCount; i++)
            {
                ActiveScript act = new ActiveScript(reader, this);
                ActiveScripts[act.Id] = act;
            }

            // Dados separados
            for (int i = 0; i < ScriptInstances.Count; i++)
            {
                EID id = ReadEID(reader);
                ScriptInstances[id].ReadData(reader, this);
            }

            for (int i = 0; i < References.Count; i++)
            {
                EID id = ReadEID(reader);
                References[id].ReadData(reader, this);
            }

            if (StructInstances != null)
            {
                for (int i = 0; i < StructInstances.Count; i++)
                {
                    EID id = ReadEID(reader);
                    StructInstances[id].ReadData(reader, this);
                }
            }

            for (int i = 0; i < Arrays.Count; i++)
            {
                EID id = ReadEID(reader);
                Arrays[id].ReadData(reader, this);
            }

            for (int i = 0; i < ActiveScripts.Count; i++)
            {
                EID id = ReadEID32(reader);
                ActiveScripts[id].ReadData(reader, this);
            }

            int fmCount = reader.ReadInt32();
            FunctionMessages = new List<FunctionMessage>(fmCount);
            for (int i = 0; i < fmCount; i++)
            {
                FunctionMessages.Add(new FunctionMessage(reader, this));
            }

            int ss1Count = reader.ReadInt32();
            SuspendedStacks1 = new List<SuspendedStack>(ss1Count);
            for (int i = 0; i < ss1Count; i++)
            {
                SuspendedStacks1.Add(new SuspendedStack(reader, this));
            }

            int ss2Count = reader.ReadInt32();
            SuspendedStacks2 = new List<SuspendedStack>(ss2Count);
            for (int i = 0; i < ss2Count; i++)
            {
                SuspendedStacks2.Add(new SuspendedStack(reader, this));
            }

            Unk1 = reader.ReadInt32();
            Unk2 = Unk1 == 0 ? (int?)null : reader.ReadInt32();

            int unkIdCount = reader.ReadInt32();
            UnknownIDs = new List<EID>(unkIdCount);
            for (int i = 0; i < unkIdCount; i++)
            {
                UnknownIDs.Add(ReadEID(reader));
            }

            int unbindCount = reader.ReadInt32();
            Unbinds = new List<QueuedUnbind>(unbindCount);
            for (int i = 0; i < unbindCount; i++)
            {
                Unbinds.Add(new QueuedUnbind(reader, this));
            }

            if (ess.Header.Game.EhSkyrim())
            {
                SaveFileVersion = reader.ReadInt16();
            }

            TrailingArraysBlock = reader.ReadBytes(reader.Remaining);
        }

        public void Write(BinaryWriterEx writer)
        {
            writer.WriteInt16(Header);
            Strings.Write(writer);

            if (Structs != null)
            {
                writer.WriteInt32(Scripts.Count);
                writer.WriteInt32(Structs.Count);
                foreach (PapyrusScript s in Scripts.Values) s.Write(writer);
                foreach (PapyrusStruct st in Structs.Values) st.Write(writer);
            }
            else
            {
                writer.WriteInt32(Scripts.Count);
                foreach (PapyrusScript s in Scripts.Values) s.Write(writer);
            }

            writer.WriteInt32(ScriptInstances.Count);
            foreach (ScriptInstance inst in ScriptInstances.Values) inst.WriteHeader(writer);

            writer.WriteInt32(References.Count);
            foreach (PapyrusReference r in References.Values) r.WriteHeader(writer);

            if (StructInstances != null)
            {
                writer.WriteInt32(StructInstances.Count);
                foreach (StructInstance si in StructInstances.Values) si.WriteHeader(writer);
            }

            writer.WriteInt32(Arrays.Count);
            foreach (ArrayInfo arr in Arrays.Values) arr.WriteHeader(writer);

            PapyrusRuntime.Write(writer);

            writer.WriteInt32(ActiveScripts.Count);
            foreach (ActiveScript act in ActiveScripts.Values) act.WriteHeader(writer);

            foreach (ScriptInstance inst in ScriptInstances.Values) inst.WriteData(writer);
            foreach (PapyrusReference r in References.Values) r.WriteData(writer);
            if (StructInstances != null)
            {
                foreach (StructInstance si in StructInstances.Values) si.WriteData(writer);
            }
            foreach (ArrayInfo arr in Arrays.Values) arr.WriteData(writer);
            foreach (ActiveScript act in ActiveScripts.Values) act.WriteData(writer);

            writer.WriteInt32(FunctionMessages.Count);
            foreach (FunctionMessage fm in FunctionMessages) fm.Write(writer);

            writer.WriteInt32(SuspendedStacks1.Count);
            foreach (SuspendedStack ss in SuspendedStacks1) ss.Write(writer);

            writer.WriteInt32(SuspendedStacks2.Count);
            foreach (SuspendedStack ss in SuspendedStacks2) ss.Write(writer);

            writer.WriteInt32(Unk1);
            if (Unk2.HasValue) writer.WriteInt32(Unk2.Value);

            writer.WriteInt32(UnknownIDs.Count);
            foreach (EID id in UnknownIDs) id.Write(writer);

            writer.WriteInt32(Unbinds.Count);
            foreach (QueuedUnbind u in Unbinds) u.Write(writer);

            if (SaveFileVersion.HasValue) writer.WriteInt16(SaveFileVersion.Value);
            writer.WriteBytes(TrailingArraysBlock);
        }

        public int CalculateSize()
        {
            int sum = 2;
            sum += Strings.CalculateSize();
            sum += 4 + Scripts.Values.Sum(s => s.CalculateSize());
            if (Structs != null) sum += 4 + Structs.Values.Sum(s => s.CalculateSize());
            sum += 4 + ScriptInstances.Values.Sum(i => i.CalculateSize());
            sum += 4 + References.Values.Sum(r => r.CalculateSize());
            if (StructInstances != null) sum += 4 + StructInstances.Values.Sum(s => s.CalculateSize());
            sum += 4 + Arrays.Values.Sum(a => a.CalculateSize());
            sum += PapyrusRuntime.CalculateSize();
            sum += 4 + ActiveScripts.Values.Sum(a => a.CalculateSize());
            sum += 4 + FunctionMessages.Sum(f => f.CalculateSize());
            sum += 4 + SuspendedStacks1.Sum(s => s.CalculateSize());
            sum += 4 + SuspendedStacks2.Sum(s => s.CalculateSize());
            sum += 4 + (Unk2.HasValue ? 4 : 0);
            sum += 4 + UnknownIDs.Sum(u => u.CalculateSize());
            sum += 4 + Unbinds.Sum(u => u.CalculateSize());
            sum += SaveFileVersion.HasValue ? 2 : 0;
            sum += TrailingArraysBlock != null ? TrailingArraysBlock.Length : 0;
            return sum;
        }

        public int CountUnattachedInstances()
        {
            return ScriptInstances.Values.Count(i => i.IsUnattached);
        }

        public int[] CountUndefinedElements()
        {
            int count = 0;
            count += Scripts.Values.Count(s => s.IsUndefined);
            count += ScriptInstances.Values.Count(i => i.IsUndefined);
            count += References.Values.Count(r => r.IsUndefined);
            if (Structs != null) count += Structs.Values.Count(s => s.IsUndefined);
            if (StructInstances != null) count += StructInstances.Values.Count(s => s.IsUndefined);
            int threads = ActiveScripts.Values.Count(a => a.IsUndefined(Scripts) && !a.IsTerminated);
            return new int[] { count, threads };
        }

        public int CountNonExistentCreatedInstances(HashSet<RefID> existingChangeFormRefs)
        {
            return ScriptInstances.Values.Count(i => i.RefID.Type == RefIDType.Created && !existingChangeFormRefs.Contains(i.RefID));
        }

        public int RemoveUnattachedInstances()
        {
            List<EID> keys = ScriptInstances.Values.Where(i => i.IsUnattached).Select(i => i.Id).ToList();
            foreach (EID id in keys)
            {
                ScriptInstances.Remove(id);
            }
            return keys.Count;
        }

        public int RemoveUndefinedElements()
        {
            int removed = 0;

            List<EID> instKeys = ScriptInstances.Values.Where(i => i.IsUndefined).Select(i => i.Id).ToList();
            foreach (EID id in instKeys) { ScriptInstances.Remove(id); removed++; }

            List<EID> refKeys = References.Values.Where(r => r.IsUndefined).Select(r => r.Id).ToList();
            foreach (EID id in refKeys) { References.Remove(id); removed++; }

            List<TString> scriptKeys = Scripts.Values.Where(s => s.IsUndefined).Select(s => s.Name).ToList();
            foreach (TString name in scriptKeys) { Scripts.Remove(name); removed++; }

            if (StructInstances != null)
            {
                List<EID> siKeys = StructInstances.Values.Where(s => s.IsUndefined).Select(s => s.Id).ToList();
                foreach (EID id in siKeys) { StructInstances.Remove(id); removed++; }
            }

            if (Structs != null)
            {
                List<TString> stKeys = Structs.Values.Where(s => s.IsUndefined).Select(s => s.Name).ToList();
                foreach (TString name in stKeys) { Structs.Remove(name); removed++; }
            }

            foreach (ActiveScript act in ActiveScripts.Values)
            {
                if (act.IsUndefined(Scripts) && !act.IsTerminated)
                {
                    act.Zero();
                    removed++;
                }
            }

            return removed;
        }

        public int RemoveNonExistentCreatedInstances(HashSet<RefID> existingChangeFormRefs)
        {
            List<EID> keys = ScriptInstances.Values
                .Where(i => i.RefID.Type == RefIDType.Created && !existingChangeFormRefs.Contains(i.RefID))
                .Select(i => i.Id)
                .ToList();

            foreach (EID id in keys)
            {
                ScriptInstances.Remove(id);
            }
            return keys.Count;
        }
    }
}

