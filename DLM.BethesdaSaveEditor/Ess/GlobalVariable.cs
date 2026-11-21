using System.Collections.Generic;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class GlobalVariable : ObservableBase
    {
        private static readonly Dictionary<int, string> KnownSkyrimGlobals = new Dictionary<int, string>
        {
            [0x00000035] = "GameYear",
            [0x00000036] = "GameMonth",
            [0x00000037] = "GameDay",
            [0x00000038] = "GameHour",
            [0x00000039] = "GameDaysPassed",
            [0x0000003A] = "TimeScale",
            [0x0000003B] = "GameDayOfWeek",
            [0x0001A32F] = "PlayerIsVampire",
            [0x0001A330] = "PlayerIsWerewolf",
            [0x0003B525] = "MQ101DragonAttack",
            [0x000727DE] = "PlayerFollowerCount",
            [0x000750BA] = "PlayerAnimalCount",
            [0x0009CCD7] = "C00CompanionStatus",
            [0x000D5045] = "DBNightMotherCoffin",
            [0x000F25E4] = "PlayerHasHouseWhiterun"
        };

        private float _valor;

        public RefID RefID { get; }
        public string FormIDHex => RefID.ToHex();
        public string PluginNome => RefID.Plugin != null ? RefID.Plugin.Name : "(None)";

        public string NomeConhecido
        {
            get
            {
                string name;
                return KnownSkyrimGlobals.TryGetValue(RefID.FormID, out name) ? name : string.Empty;
            }
        }

        public float Valor
        {
            get => _valor;
            set => SetProperty(ref _valor, value);
        }

        public GlobalVariable(BinaryReaderEx reader, EssFile ess)
        {
            RefID = ess.ReadRefID(reader);
            _valor = reader.ReadSingle();
        }

        public void Write(BinaryWriterEx writer)
        {
            RefID.Write(writer);
            writer.WriteSingle(_valor);
        }

        public int CalculateSize() => 3 + 4;
    }

    public sealed class GlobalVariableTable : IGlobalDataBlock
    {
        public List<GlobalVariable> Variables { get; }

        public GlobalVariableTable()
        {
            Variables = new List<GlobalVariable>();
        }

        public GlobalVariableTable(BinaryReaderEx reader, EssFile ess)
        {
            VSVal count = new VSVal(reader);
            Variables = new List<GlobalVariable>(count.Value);
            for (int i = 0; i < count.Value; i++)
            {
                Variables.Add(new GlobalVariable(reader, ess));
            }
        }

        public void Write(BinaryWriterEx writer)
        {
            VSVal count = new VSVal(Variables.Count);
            count.Write(writer);
            foreach (GlobalVariable v in Variables)
            {
                v.Write(writer);
            }
        }

        public int CalculateSize()
        {
            VSVal count = new VSVal(Variables.Count);
            return count.CalculateSize() + Variables.Count * 7;
        }
    }
}
