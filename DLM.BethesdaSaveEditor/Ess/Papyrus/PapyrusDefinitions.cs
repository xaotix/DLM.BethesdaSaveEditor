using System;
using System.Collections.Generic;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess.Papyrus
{
    public sealed class MemberDesc
    {
        public TString Name { get; }
        public TString Type { get; }

        public MemberDesc(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            Name = ctx.Strings.ReadReference(reader);
            Type = ctx.Strings.ReadReference(reader);
        }

        public static List<MemberDesc> ReadList(BinaryReaderEx reader, int count, PapyrusBlock ctx)
        {
            List<MemberDesc> list = new List<MemberDesc>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(new MemberDesc(reader, ctx));
            }
            return list;
        }

        public void Write(BinaryWriterEx writer)
        {
            Name.WriteIndex(writer);
            Type.WriteIndex(writer);
        }

        public int CalculateSize() => Name.CalculateIndexSize() + Type.CalculateIndexSize();
    }

    public sealed class PapyrusScript
    {
        private static readonly HashSet<string> NativeScripts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ActiveMagicEffect", "Alias", "Debug", "Form", "Game", "Input",
            "Math", "ModEvent", "SKSE", "StringUtil", "UI", "Utility",
            "CommonArrayFunctions", "ScriptObject", "InputEnableLayer"
        };

        public TString Name { get; }
        public TString ParentType { get; }
        public List<MemberDesc> Members { get; }
        public int InstanceCount { get; set; }

        public string NomeTexto => Name.Text;
        public string PaiTexto => ParentType.Text;
        public int QuantidadeMembros => Members.Count;

        public bool IsNative => NativeScripts.Contains(Name.Text);
        public bool IsUndefined => ParentType.IsEmpty && !IsNative;

        public PapyrusScript(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            Name = ctx.Strings.ReadReference(reader);
            ParentType = ctx.Strings.ReadReference(reader);
            int count = reader.ReadInt32();
            Members = MemberDesc.ReadList(reader, count, ctx);
        }

        public void Write(BinaryWriterEx writer)
        {
            Name.WriteIndex(writer);
            ParentType.WriteIndex(writer);
            writer.WriteInt32(Members.Count);
            foreach (MemberDesc m in Members)
            {
                m.Write(writer);
            }
        }

        public int CalculateSize()
        {
            return 4 + Name.CalculateIndexSize() + ParentType.CalculateIndexSize() + Members.Sum(m => m.CalculateSize());
        }

        public static bool IsNativeName(string name) => NativeScripts.Contains(name ?? string.Empty);
    }

    public sealed class PapyrusStruct
    {
        public TString Name { get; }
        public List<MemberDesc> Members { get; }
        public int InstanceCount { get; set; }

        public bool IsUndefined => Members.Count == 0;

        public PapyrusStruct(BinaryReaderEx reader, PapyrusBlock ctx)
        {
            Name = ctx.Strings.ReadReference(reader);
            int count = reader.ReadInt32();
            Members = MemberDesc.ReadList(reader, count, ctx);
        }

        public void Write(BinaryWriterEx writer)
        {
            Name.WriteIndex(writer);
            writer.WriteInt32(Members.Count);
            foreach (MemberDesc m in Members)
            {
                m.Write(writer);
            }
        }

        public int CalculateSize()
        {
            return 4 + Name.CalculateIndexSize() + Members.Sum(m => m.CalculateSize());
        }
    }
}

