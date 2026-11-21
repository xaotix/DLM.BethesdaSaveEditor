using System;
using System.Collections.Generic;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class PlayerSkillItem : ObservableBase
    {
        private byte _baseValue;
        private sbyte _offsetValue;

        public int Index { get; }
        public string Name { get; }

        public byte BaseValue
        {
            get => _baseValue;
            set
            {
                if (SetProperty(ref _baseValue, value))
                {
                    OnPropertyChanged(nameof(TotalValue));
                }
            }
        }

        public sbyte OffsetValue
        {
            get => _offsetValue;
            set
            {
                if (SetProperty(ref _offsetValue, value))
                {
                    OnPropertyChanged(nameof(TotalValue));
                }
            }
        }

        public int TotalValue => _baseValue + _offsetValue;

        public PlayerSkillItem(int index, string name, byte baseVal, sbyte offsetVal)
        {
            Index = index;
            Name = name;
            _baseValue = baseVal;
            _offsetValue = offsetVal;
        }
    }

    /// <summary>
    /// Leitor e editor binario direto do registro NPC_ do jogador (FormID 0x00000007).
    /// Suporta ACBS (Level, MagickaOffset, StaminaOffset) e DNAM (18 Skills + Base Health/Magicka/Stamina).
    /// </summary>
    public sealed class PlayerNpcEditor
    {
        private static readonly string[] SkyrimSkillNames =
        {
            "One-Handed", "Two-Handed", "Archery", "Block", "Smithing", "Heavy Armor",
            "Light Armor", "Pickpocket", "Lockpicking", "Sneak", "Alchemy", "Speech",
            "Alteration", "Conjuration", "Destruction", "Illusion", "Restoration", "Enchanting"
        };

        private readonly ChangeForm _npcForm;
        private byte[] _body;

        public bool IsAvailable => _npcForm != null && _body != null;
        public bool HasAcbs { get; private set; }
        public bool HasDnam { get; private set; }

        private int _acbsOffset = -1;
        private int _dnamOffset = -1;

        public ushort AcbsMagickaOffset { get; set; }
        public ushort AcbsStaminaOffset { get; set; }
        public ushort AcbsLevel { get; set; }

        public ushort BaseHealth { get; set; }
        public ushort BaseMagicka { get; set; }
        public ushort BaseStamina { get; set; }

        public List<PlayerSkillItem> Skills { get; } = new List<PlayerSkillItem>();

        public PlayerNpcEditor(EssFile ess)
        {
            if (ess == null || !ess.Header.Game.EhSkyrim()) return;
            _npcForm = ess.ChangeForms.FirstOrDefault(cf => cf.RefID.FormID == 0x00000007 && cf.TipoCodigo == "NPC_");
            if (_npcForm == null) return;

            try
            {
                _body = _npcForm.GetDecompressedBody();
                ParseOffsets(ess);
            }
            catch
            {
                _body = null;
            }
        }

        private void ParseOffsets(EssFile ess)
        {
            uint flags = _npcForm.ChangeFlags;
            BinaryReaderEx r = new BinaryReaderEx(_body);

            // Bit 0: CHANGE_FORM_FLAGS (6 bytes)
            if ((flags & (1u << 0)) != 0) r.Position += 6;

            // Bit 1: CHANGE_ACTOR_BASE_DATA -> ACBS (24 bytes)
            if ((flags & (1u << 1)) != 0)
            {
                _acbsOffset = r.Position;
                HasAcbs = true;
                byte[] acbs = r.ReadBytes(24);
                AcbsMagickaOffset = BitConverter.ToUInt16(acbs, 4);
                AcbsStaminaOffset = BitConverter.ToUInt16(acbs, 6);
                AcbsLevel = BitConverter.ToUInt16(acbs, 8);
            }

            // Bit 6: CHANGE_ACTOR_BASE_FACTIONS -> VSVal count * (RefID 3 + byte rank)
            if ((flags & (1u << 6)) != 0)
            {
                VSVal c = new VSVal(r);
                r.Position += c.Value * 4;
            }

            // Bit 4: CHANGE_ACTOR_BASE_SPELLLIST -> 3 VSVal arrays of RefID (3 bytes each)
            if ((flags & (1u << 4)) != 0)
            {
                VSVal s1 = new VSVal(r); r.Position += s1.Value * 3;
                VSVal s2 = new VSVal(r); r.Position += s2.Value * 3;
                VSVal s3 = new VSVal(r); r.Position += s3.Value * 3;
            }

            // Bit 3: CHANGE_ACTOR_BASE_AIDATA -> AIDT (20 bytes)
            if ((flags & (1u << 3)) != 0)
            {
                r.Position += 20;
            }

            // Bit 5: CHANGE_ACTOR_BASE_FULLNAME -> WString
            if ((flags & (1u << 5)) != 0)
            {
                WString.Read(r);
            }

            // Bit 9: CHANGE_NPC_SKILLS -> DNAM (52 bytes)
            if ((flags & (1u << 9)) != 0 && r.Remaining >= 52)
            {
                _dnamOffset = r.Position;
                HasDnam = true;
                byte[] dnam = r.ReadBytes(52);
                Skills.Clear();
                for (int i = 0; i < 18; i++)
                {
                    Skills.Add(new PlayerSkillItem(i, SkyrimSkillNames[i], dnam[i], (sbyte)dnam[18 + i]));
                }
                BaseHealth = BitConverter.ToUInt16(dnam, 36);
                BaseMagicka = BitConverter.ToUInt16(dnam, 38);
                BaseStamina = BitConverter.ToUInt16(dnam, 40);
            }
        }

        public byte[] CaptureSnapshot()
        {
            if (_body == null) return null;
            byte[] copy = new byte[_body.Length];
            Buffer.BlockCopy(_body, 0, copy, 0, _body.Length);
            return copy;
        }

        public void RestoreSnapshot(byte[] snapshot, EssFile ess)
        {
            if (_npcForm == null || snapshot == null) return;
            _body = new byte[snapshot.Length];
            Buffer.BlockCopy(snapshot, 0, _body, 0, snapshot.Length);
            _npcForm.UpdateBodyData(_body);
            ParseOffsets(ess);
        }

        public void ApplyToSave(EssFile ess)
        {
            if (!IsAvailable) return;

            if (HasAcbs && _acbsOffset >= 0 && _acbsOffset + 24 <= _body.Length)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(AcbsMagickaOffset), 0, _body, _acbsOffset + 4, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(AcbsStaminaOffset), 0, _body, _acbsOffset + 6, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(AcbsLevel), 0, _body, _acbsOffset + 8, 2);
            }

            if (HasDnam && _dnamOffset >= 0 && _dnamOffset + 52 <= _body.Length)
            {
                for (int i = 0; i < 18 && i < Skills.Count; i++)
                {
                    _body[_dnamOffset + i] = Skills[i].BaseValue;
                    _body[_dnamOffset + 18 + i] = unchecked((byte)Skills[i].OffsetValue);
                }
                Buffer.BlockCopy(BitConverter.GetBytes(BaseHealth), 0, _body, _dnamOffset + 36, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(BaseMagicka), 0, _body, _dnamOffset + 38, 2);
                Buffer.BlockCopy(BitConverter.GetBytes(BaseStamina), 0, _body, _dnamOffset + 40, 2);
            }

            _npcForm.UpdateBodyData(_body);
            ess.HasChanges = true;
        }
    }
}

