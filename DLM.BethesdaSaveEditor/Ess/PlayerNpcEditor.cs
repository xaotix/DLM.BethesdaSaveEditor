using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class PlayerSkillItem : ObservableBase
    {
        private float _level;
        private uint _legendaryCount;

        public int Index { get; }
        public uint ActorValueId { get; }
        public string Name { get; }

        public float Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }

        public uint LegendaryCount
        {
            get => _legendaryCount;
            set => SetProperty(ref _legendaryCount, value);
        }

        public PlayerSkillItem(int index, uint avId, string name, float level, uint legendaryCount)
        {
            Index = index;
            ActorValueId = avId;
            Name = name;
            _level = level;
            _legendaryCount = legendaryCount;
        }
    }

    public sealed class PlayerActorValueItem : ObservableBase
    {
        private float _value;

        public uint RawId { get; }
        public int NormalizedId { get; }
        public string Name { get; }
        public int ValueOffset { get; set; }

        public string IdHex => "0x" + RawId.ToString("X4");

        public float Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public PlayerActorValueItem(uint rawId, int normalizedId, string name, int valueOffset, float value)
        {
            RawId = rawId;
            NormalizedId = normalizedId;
            Name = name;
            ValueOffset = valueOffset;
            _value = value;
        }
    }

    public sealed class PlayerSnapshot
    {
        public byte[] AchrBody { get; set; }
        public byte[] NpcBody { get; set; }
        public int HeaderLevel { get; set; }
        public float HeaderCurrentXp { get; set; }
        public float HeaderNeededXp { get; set; }
    }

    /// <summary>
    /// Editor binario direto do personagem do jogador no save:
    /// - ACHR 0x00000014 (PlayerRef): ActorValueStorage (Health, Magicka/AP, Stamina/Rads, CarryWeight, SpeedMult, DragonSouls, Skills/SPECIAL),
    ///   PlayerSkills (18 Skills + Legendary + XP) e Inventario (Gold/Bottlecaps, Lockpicks/Bobby Pins).
    /// - NPC_ 0x00000007 (PlayerBase): sincroniza ACBS Level e DNAM 18 Skills no Skyrim.
    /// - SaveHeader: sincroniza Level, CurrentXp e NeededXp.
    /// Respeita as particularidades binarias de Skyrim (LE/SE/VR/Switch) e Fallout 4 (Standard/VR).
    /// </summary>
    public sealed class PlayerNpcEditor
    {
        // Skyrim ActorValue indices
        public const uint SKYRIM_AV_HEALTH = 24;
        public const uint SKYRIM_AV_MAGICKA = 25;
        public const uint SKYRIM_AV_STAMINA = 26;
        public const uint SKYRIM_AV_SPEEDMULT = 30;
        public const uint SKYRIM_AV_CARRYWEIGHT = 32;
        public const uint SKYRIM_AV_DRAGONSOULS = 0xFFFFFFA2; // sbyte(162) sign-extended to uint32

        // Fallout 4 ActorValue FormIDs (AVIF)
        public const uint FO4_AV_STRENGTH = 0x000002C2;
        public const uint FO4_AV_PERCEPTION = 0x000002C3;
        public const uint FO4_AV_ENDURANCE = 0x000002C4;
        public const uint FO4_AV_CHARISMA = 0x000002C5;
        public const uint FO4_AV_INTELLIGENCE = 0x000002C6;
        public const uint FO4_AV_AGILITY = 0x000002C7;
        public const uint FO4_AV_LUCK = 0x000002C8;
        public const uint FO4_AV_HEALTH = 0x000002D4;
        public const uint FO4_AV_ACTIONPOINTS = 0x000002D5;
        public const uint FO4_AV_SPEEDMULT = 0x000002DA;
        public const uint FO4_AV_CARRYWEIGHT = 0x000002DC;
        public const uint FO4_AV_RADS = 0x000002E0;

        private static readonly string[] SkyrimSkillNames =
        {
            "One-Handed", "Two-Handed", "Archery", "Block", "Smithing", "Heavy Armor",
            "Light Armor", "Pickpocket", "Lockpicking", "Sneak", "Alchemy", "Speech",
            "Alteration", "Conjuration", "Destruction", "Illusion", "Restoration", "Enchanting"
        };

        private static readonly Dictionary<int, string> SkyrimAvNames = new Dictionary<int, string>
        {
            { 0, "Aggression" }, { 1, "Confidence" }, { 2, "Energy" }, { 3, "Morality" },
            { 4, "Mood" }, { 5, "Assistance" },
            { 6, "One-Handed" }, { 7, "Two-Handed" }, { 8, "Archery" }, { 9, "Block" },
            { 10, "Smithing" }, { 11, "Heavy Armor" }, { 12, "Light Armor" }, { 13, "Pickpocket" },
            { 14, "Lockpicking" }, { 15, "Sneak" }, { 16, "Alchemy" }, { 17, "Speech" },
            { 18, "Alteration" }, { 19, "Conjuration" }, { 20, "Destruction" }, { 21, "Illusion" },
            { 22, "Restoration" }, { 23, "Enchanting" },
            { 24, "Health" }, { 25, "Magicka" }, { 26, "Stamina" },
            { 27, "HealRate" }, { 28, "MagickaRate" }, { 29, "StaminaRate" },
            { 30, "SpeedMult" }, { 31, "InventoryWeight" }, { 32, "CarryWeight" },
            { 33, "CritChance" }, { 34, "MeleeDamage" }, { 35, "UnarmedDamage" },
            { 36, "Mass" }, { 37, "VoicePoints" }, { 38, "VoiceRate" }, { 39, "DamageResist (Armor)" },
            { 40, "PoisonResist" }, { 41, "FireResist" }, { 42, "ElectricResist" }, { 43, "FrostResist" },
            { 44, "MagicResist" }, { 45, "DiseaseResist" },
            { 64, "Variable01" }, { 68, "Variable05" }, { 78, "ShoutRecoveryMult" },
            { 82, "BowSpeedBonus" }, { 134, "ShoutRecoveryMult" }, { 162, "DragonSouls" }
        };

        private static readonly Dictionary<uint, string> Fallout4AvNames = new Dictionary<uint, string>
        {
            { 0x000002C2, "Strength" }, { 0x000002C3, "Perception" }, { 0x000002C4, "Endurance" },
            { 0x000002C5, "Charisma" }, { 0x000002C6, "Intelligence" }, { 0x000002C7, "Agility" },
            { 0x000002C8, "Luck" }, { 0x000002C9, "Experience" },
            { 0x000002D4, "Health" }, { 0x000002D5, "Action Points" },
            { 0x000002DA, "SpeedMult" }, { 0x000002DC, "Carry Weight" },
            { 0x000002E0, "Rads" }, { 0x000002E3, "Damage Resist" }, { 0x000002EA, "Energy Resist" }
        };

        private readonly ChangeForm _achrForm;
        private readonly ChangeForm _npcForm;
        private byte[] _achrBody;
        private byte[] _npcBody;

        public bool IsFallout4 { get; }
        public bool IsAvailable => _achrBody != null || _npcBody != null;
        public bool HasActorValues => ActorValues.Count > 0;
        public bool HasPlayerSkills => _playerSkillsOffset >= 0;
        public bool HasCurrency => _currencyCountOffset >= 0;
        public bool HasLockpicks => _lockpicksCountOffset >= 0;
        public bool HasDragonSouls => !IsFallout4 && (_avTableStartOffset >= 0);

        // ACHR offsets
        private int _avTableCountOffset = -1;
        private int _avTableCountBytes = 0;
        private int _avTableStartOffset = -1;
        private int _avTableEntryCount = 0;
        private int _playerSkillsOffset = -1;
        private int _currencyCountOffset = -1;
        private int _lockpicksCountOffset = -1;

        // NPC_ offsets (Skyrim)
        private int _acbsOffset = -1;
        private int _dnamOffset = -1;

        // Editable primary attributes (Direct ACHR 0x00000014!)
        public float Health { get; set; }
        public float SecondaryAttr { get; set; } // Magicka (Skyrim) or Action Points (FO4)
        public float TertiaryAttr { get; set; }  // Stamina (Skyrim) or Rads (FO4)
        public float CarryWeight { get; set; }
        public float SpeedMult { get; set; }
        public float DragonSouls { get; set; }

        // Inventory currency & lockpicks (Direct ACHR 0x00000014!)
        public int CurrencyCount { get; set; }  // Gold (Skyrim) or Bottlecaps (FO4)
        public int LockpicksCount { get; set; } // Lockpicks (Skyrim) or Bobby Pins (FO4)

        public List<PlayerSkillItem> Skills { get; } = new List<PlayerSkillItem>();
        public List<PlayerActorValueItem> ActorValues { get; } = new List<PlayerActorValueItem>();

        public PlayerNpcEditor(EssFile ess)
        {
            if (ess == null) return;
            IsFallout4 = ess.Header.Game.EhFallout4();

            _achrForm = ess.ChangeForms.FirstOrDefault(cf => cf.RefID.FormID == 0x00000014 && cf.TipoCodigo == "ACHR");
            _npcForm = ess.ChangeForms.FirstOrDefault(cf => cf.RefID.FormID == 0x00000007 && cf.TipoCodigo == "NPC_");

            if (_achrForm != null)
            {
                try { _achrBody = _achrForm.GetDecompressedBody(); }
                catch { _achrBody = null; }
            }
            if (_npcForm != null && !IsFallout4)
            {
                try { _npcBody = _npcForm.GetDecompressedBody(); }
                catch { _npcBody = null; }
            }

            ParseAll(ess);
        }

        private void ParseAll(EssFile ess)
        {
            ActorValues.Clear();
            Skills.Clear();
            _avTableCountOffset = -1;
            _avTableCountBytes = 0;
            _avTableStartOffset = -1;
            _avTableEntryCount = 0;
            _playerSkillsOffset = -1;
            _currencyCountOffset = -1;
            _lockpicksCountOffset = -1;
            _acbsOffset = -1;
            _dnamOffset = -1;

            if (_npcBody != null && !IsFallout4)
            {
                ParseNpcOffsets();
            }

            if (_achrBody != null)
            {
                ParseInventoryCurrency();
                if (IsFallout4)
                {
                    ParseFallout4ActorValues();
                }
                else
                {
                    ParseSkyrimActorValues();
                    ParseSkyrimPlayerSkills(ess);
                }
            }

            PopulateCoreFields();
        }

        private void ParseNpcOffsets()
        {
            uint flags = _npcForm.ChangeFlags;
            BinaryReaderEx r = new BinaryReaderEx(_npcBody);

            if ((flags & (1u << 0)) != 0) r.Position += 6;
            if ((flags & (1u << 1)) != 0 && r.Remaining >= 24)
            {
                _acbsOffset = r.Position;
                r.Position += 24;
            }
            if ((flags & (1u << 6)) != 0 && r.Remaining >= 1)
            {
                VSVal c = new VSVal(r);
                r.Position += c.Value * 4;
            }
            if ((flags & (1u << 4)) != 0 && r.Remaining >= 3)
            {
                VSVal s1 = new VSVal(r); r.Position += s1.Value * 3;
                VSVal s2 = new VSVal(r); r.Position += s2.Value * 3;
                VSVal s3 = new VSVal(r); r.Position += s3.Value * 3;
            }
            if ((flags & (1u << 3)) != 0) r.Position += 20;
            if ((flags & (1u << 5)) != 0 && r.Remaining >= 2) WString.Read(r);
            if ((flags & (1u << 9)) != 0 && r.Remaining >= 52)
            {
                _dnamOffset = r.Position;
            }
        }

        private void ParseInventoryCurrency()
        {
            // Player inventory sits near the front of ACHR 0x00000014.
            // RefID for 0x0000000F (Gold / Bottlecaps) in master plugin = 0x40 0x00 0x0F followed by int32 count.
            // RefID for 0x0000000A (Lockpicks / Bobby Pins) in master plugin = 0x40 0x00 0x0A followed by int32 count.
            int scanLimit = Math.Min(_achrBody.Length - 7, 24576);
            for (int i = 0; i < scanLimit; i++)
            {
                if (_achrBody[i] == 0x40 && _achrBody[i + 1] == 0x00)
                {
                    byte low = _achrBody[i + 2];
                    if (low == 0x0F && _currencyCountOffset < 0)
                    {
                        int count = BitConverter.ToInt32(_achrBody, i + 3);
                        if (count >= 0 && count < 1000000000)
                        {
                            _currencyCountOffset = i + 3;
                            CurrencyCount = count;
                        }
                    }
                    else if (low == 0x0A && _lockpicksCountOffset < 0)
                    {
                        int count = BitConverter.ToInt32(_achrBody, i + 3);
                        if (count >= 0 && count < 10000000)
                        {
                            _lockpicksCountOffset = i + 3;
                            LockpicksCount = count;
                        }
                    }
                }
            }
        }

        private void ParseSkyrimActorValues()
        {
            // Locate ActorValueStorage BaseValues table in ACHR 0x00000014:
            // Format: VSVal count + count * (uint32 avId, float value)
            // Anchor on consecutive (AV 24 Health, AV 25 Magicka) or (AV 24 Health, AV 26 Stamina).
            int anchor = -1;
            for (int i = 0; i <= _achrBody.Length - 24; i++)
            {
                uint id0 = BitConverter.ToUInt32(_achrBody, i);
                if (id0 != SKYRIM_AV_HEALTH) continue;

                uint id1 = BitConverter.ToUInt32(_achrBody, i + 8);
                if (id1 != SKYRIM_AV_MAGICKA && id1 != SKYRIM_AV_STAMINA) continue;

                float v0 = BitConverter.ToSingle(_achrBody, i + 4);
                float v1 = BitConverter.ToSingle(_achrBody, i + 12);
                if (IsValidStatFloat(v0, 1f, 500000f) && IsValidStatFloat(v1, 1f, 500000f))
                {
                    anchor = i;
                    break;
                }
            }
            if (anchor < 0) return;

            // Walk backwards in 8-byte steps while avId is strictly decreasing in [0..163]
            int start = anchor;
            while (start >= 8)
            {
                uint prevRaw = BitConverter.ToUInt32(_achrBody, start - 8);
                uint curRaw = BitConverter.ToUInt32(_achrBody, start);
                int prevNorm = NormalizeSkyrimAvId(prevRaw);
                int curNorm = NormalizeSkyrimAvId(curRaw);
                float prevVal = BitConverter.ToSingle(_achrBody, start - 4);
                if (prevNorm >= 0 && prevNorm < curNorm && IsValidStatFloat(prevVal, -100000f, 1000000f))
                {
                    start -= 8;
                }
                else
                {
                    break;
                }
            }

            // Determine VSVal count right before 'start' (1 or 2 bytes)
            int entryCount = 0;
            if (start >= 1)
            {
                byte b1 = _achrBody[start - 1];
                if ((b1 & 0x03) == 0)
                {
                    int c1 = b1 >> 2;
                    if (c1 >= 3 && start + c1 * 8 <= _achrBody.Length && ValidateSkyrimAvTable(start, c1))
                    {
                        _avTableCountOffset = start - 1;
                        _avTableCountBytes = 1;
                        entryCount = c1;
                    }
                }
            }
            if (entryCount == 0 && start >= 2)
            {
                ushort u2 = BitConverter.ToUInt16(_achrBody, start - 2);
                if ((u2 & 0x03) == 1)
                {
                    int c2 = u2 >> 2;
                    if (c2 >= 3 && c2 <= 256 && start + c2 * 8 <= _achrBody.Length && ValidateSkyrimAvTable(start, c2))
                    {
                        _avTableCountOffset = start - 2;
                        _avTableCountBytes = 2;
                        entryCount = c2;
                    }
                }
            }

            // Fallback: walk forward if VSVal wasn't matched
            if (entryCount == 0)
            {
                int pos = start;
                int lastNorm = -1;
                while (pos + 8 <= _achrBody.Length && entryCount < 200)
                {
                    uint raw = BitConverter.ToUInt32(_achrBody, pos);
                    int norm = NormalizeSkyrimAvId(raw);
                    float val = BitConverter.ToSingle(_achrBody, pos + 4);
                    if (norm >= 0 && norm > lastNorm && IsValidStatFloat(val, -100000f, 1000000f))
                    {
                        lastNorm = norm;
                        entryCount++;
                        pos += 8;
                    }
                    else break;
                }
            }

            if (entryCount <= 0) return;

            _avTableStartOffset = start;
            _avTableEntryCount = entryCount;

            for (int k = 0; k < entryCount; k++)
            {
                int off = start + k * 8;
                uint rawId = BitConverter.ToUInt32(_achrBody, off);
                int normId = NormalizeSkyrimAvId(rawId);
                float val = BitConverter.ToSingle(_achrBody, off + 4);
                string name;
                if (!SkyrimAvNames.TryGetValue(normId, out name))
                {
                    name = "ActorValue #" + normId;
                }
                ActorValues.Add(new PlayerActorValueItem(rawId, normId, name, off + 4, val));
            }
        }

        private bool ValidateSkyrimAvTable(int start, int count)
        {
            int lastNorm = -1;
            for (int k = 0; k < count; k++)
            {
                uint raw = BitConverter.ToUInt32(_achrBody, start + k * 8);
                int norm = NormalizeSkyrimAvId(raw);
                float val = BitConverter.ToSingle(_achrBody, start + k * 8 + 4);
                if (norm < 0 || norm <= lastNorm || !IsValidStatFloat(val, -100000f, 1000000f)) return false;
                lastNorm = norm;
            }
            return true;
        }

        private static int NormalizeSkyrimAvId(uint rawId)
        {
            if (rawId <= 165) return (int)rawId;
            // Skyrim sign-extends sbyte AV indices >= 128 into 32-bit int (e.g. 162 -> 0xFFFFFFA2)
            if ((rawId & 0xFFFFFF80u) == 0xFFFFFF80u)
            {
                int b = (int)(rawId & 0xFFu);
                if (b >= 128 && b <= 165) return b;
            }
            return -1;
        }

        private void ParseSkyrimPlayerSkills(EssFile ess)
        {
            byte[] bXp = BitConverter.GetBytes(ess.Header.CurrentXp);
            byte[] bNeed = BitConverter.GetBytes(ess.Header.NeededXp);

            // Layout at offset i: float CurrentXp (4), float NeededXp (4), uint32 unk (4), then 18 * (float level, float xp, float thresh, uint32 legendary)
            int neededLen = 12 + 18 * 16;
            for (int i = 0; i <= _achrBody.Length - neededLen; i++)
            {
                if (_achrBody[i] != bXp[0] || _achrBody[i + 1] != bXp[1] || _achrBody[i + 2] != bXp[2] || _achrBody[i + 3] != bXp[3]) continue;
                if (_achrBody[i + 4] != bNeed[0] || _achrBody[i + 5] != bNeed[1] || _achrBody[i + 6] != bNeed[2] || _achrBody[i + 7] != bNeed[3]) continue;

                bool allSkillsValid = true;
                for (int s = 0; s < 18; s++)
                {
                    int off = i + 12 + s * 16;
                    float lv = BitConverter.ToSingle(_achrBody, off);
                    float xp = BitConverter.ToSingle(_achrBody, off + 4);
                    float th = BitConverter.ToSingle(_achrBody, off + 8);
                    uint leg = BitConverter.ToUInt32(_achrBody, off + 12);
                    if (!IsValidStatFloat(lv, 1f, 1000f) || !IsValidStatFloat(xp, 0f, 1e9f) || !IsValidStatFloat(th, 0f, 1e9f) || leg > 1000)
                    {
                        allSkillsValid = false;
                        break;
                    }
                }
                if (allSkillsValid)
                {
                    _playerSkillsOffset = i;
                    break;
                }
            }

            Skills.Clear();
            for (int s = 0; s < 18; s++)
            {
                uint avId = (uint)(6 + s);
                float lv = 15f;
                uint leg = 0;

                if (_playerSkillsOffset >= 0)
                {
                    int off = _playerSkillsOffset + 12 + s * 16;
                    lv = BitConverter.ToSingle(_achrBody, off);
                    leg = BitConverter.ToUInt32(_achrBody, off + 12);
                }
                else
                {
                    PlayerActorValueItem av = ActorValues.FirstOrDefault(a => a.NormalizedId == (int)avId);
                    if (av != null) lv = av.Value;
                    else if (_npcBody != null && _dnamOffset >= 0 && _dnamOffset + 52 <= _npcBody.Length)
                        lv = _npcBody[_dnamOffset + s];
                }

                Skills.Add(new PlayerSkillItem(s, avId, SkyrimSkillNames[s], lv, leg));
            }
        }

        private void ParseFallout4ActorValues()
        {
            // In Fallout 4 ACHR 0x00000014, ActorValues use AVIF FormIDs (0x000002C2..0x000002EA) paired with float values.
            // Scan for known FO4 AVIF uint32 FormIDs + valid float in reasonable range, verifying cluster proximity.
            HashSet<uint> knownIds = new HashSet<uint>(Fallout4AvNames.Keys);
            Dictionary<uint, PlayerActorValueItem> found = new Dictionary<uint, PlayerActorValueItem>();

            for (int i = 0; i <= _achrBody.Length - 8; i++)
            {
                uint id = BitConverter.ToUInt32(_achrBody, i);
                if (!knownIds.Contains(id)) continue;

                // Confirm neighboring 8-byte slot is also a plausible FO4 AVIF FormID (0x00000200..0x000FFFFF)
                bool neighborOk = false;
                if (i + 16 <= _achrBody.Length)
                {
                    uint nextId = BitConverter.ToUInt32(_achrBody, i + 8);
                    float nextVal = BitConverter.ToSingle(_achrBody, i + 12);
                    if (nextId >= 0x00000200 && nextId <= 0x002FFFFF && IsValidStatFloat(nextVal, -10000f, 1000000f))
                        neighborOk = true;
                }
                if (!neighborOk && i >= 8)
                {
                    uint prevId = BitConverter.ToUInt32(_achrBody, i - 8);
                    float prevVal = BitConverter.ToSingle(_achrBody, i - 4);
                    if (prevId >= 0x00000200 && prevId <= 0x002FFFFF && IsValidStatFloat(prevVal, -10000f, 1000000f))
                        neighborOk = true;
                }
                if (!neighborOk) continue;

                float val = BitConverter.ToSingle(_achrBody, i + 4);
                if (!IsValidStatFloat(val, -10000f, 1000000f)) continue;

                if (!found.ContainsKey(id))
                {
                    string name = Fallout4AvNames[id];
                    found[id] = new PlayerActorValueItem(id, (int)id, name, i + 4, val);
                }
            }

            foreach (PlayerActorValueItem item in found.Values.OrderBy(x => x.RawId))
            {
                ActorValues.Add(item);
            }

            // Populate S.P.E.C.I.A.L. into Skills grid for Fallout 4
            uint[] specialIds =
            {
                FO4_AV_STRENGTH, FO4_AV_PERCEPTION, FO4_AV_ENDURANCE,
                FO4_AV_CHARISMA, FO4_AV_INTELLIGENCE, FO4_AV_AGILITY, FO4_AV_LUCK
            };
            for (int s = 0; s < specialIds.Length; s++)
            {
                uint id = specialIds[s];
                PlayerActorValueItem av;
                float val = found.TryGetValue(id, out av) ? av.Value : 1f;
                Skills.Add(new PlayerSkillItem(s, id, Fallout4AvNames[id], val, 0));
            }
        }

        private void PopulateCoreFields()
        {
            if (IsFallout4)
            {
                Health = GetAvValue(FO4_AV_HEALTH, 100f);
                SecondaryAttr = GetAvValue(FO4_AV_ACTIONPOINTS, 70f);
                TertiaryAttr = GetAvValue(FO4_AV_RADS, 0f);
                CarryWeight = GetAvValue(FO4_AV_CARRYWEIGHT, 200f);
                SpeedMult = GetAvValue(FO4_AV_SPEEDMULT, 100f);
                DragonSouls = 0f;
            }
            else
            {
                Health = GetAvValue(SKYRIM_AV_HEALTH, 100f);
                SecondaryAttr = GetAvValue(SKYRIM_AV_MAGICKA, 100f);
                TertiaryAttr = GetAvValue(SKYRIM_AV_STAMINA, 100f);
                CarryWeight = GetAvValue(SKYRIM_AV_CARRYWEIGHT, 300f);
                SpeedMult = GetAvValue(SKYRIM_AV_SPEEDMULT, 100f);
                DragonSouls = GetAvValue(SKYRIM_AV_DRAGONSOULS, 0f);
            }
        }

        private float GetAvValue(uint rawId, float fallback)
        {
            PlayerActorValueItem item = ActorValues.FirstOrDefault(a => a.RawId == rawId);
            return item != null ? item.Value : fallback;
        }

        private void SetAvValue(uint rawId, float newValue)
        {
            PlayerActorValueItem item = ActorValues.FirstOrDefault(a => a.RawId == rawId);
            if (item != null)
            {
                item.Value = newValue;
            }
        }

        private static bool IsValidStatFloat(float v, float min, float max)
        {
            return !float.IsNaN(v) && !float.IsInfinity(v) && v >= min && v <= max;
        }

        public PlayerSnapshot CaptureSnapshot(EssFile ess)
        {
            PlayerSnapshot snap = new PlayerSnapshot
            {
                HeaderLevel = ess.Header.Level,
                HeaderCurrentXp = ess.Header.CurrentXp,
                HeaderNeededXp = ess.Header.NeededXp
            };
            if (_achrBody != null)
            {
                snap.AchrBody = new byte[_achrBody.Length];
                Buffer.BlockCopy(_achrBody, 0, snap.AchrBody, 0, _achrBody.Length);
            }
            if (_npcBody != null)
            {
                snap.NpcBody = new byte[_npcBody.Length];
                Buffer.BlockCopy(_npcBody, 0, snap.NpcBody, 0, _npcBody.Length);
            }
            return snap;
        }

        public void RestoreSnapshot(PlayerSnapshot snap, EssFile ess)
        {
            if (snap == null || ess == null) return;
            ess.Header.Level = snap.HeaderLevel;
            ess.Header.CurrentXp = snap.HeaderCurrentXp;
            ess.Header.NeededXp = snap.HeaderNeededXp;

            if (_achrForm != null && snap.AchrBody != null)
            {
                _achrBody = new byte[snap.AchrBody.Length];
                Buffer.BlockCopy(snap.AchrBody, 0, _achrBody, 0, snap.AchrBody.Length);
                _achrForm.UpdateBodyData(_achrBody);
            }
            if (_npcForm != null && snap.NpcBody != null)
            {
                _npcBody = new byte[snap.NpcBody.Length];
                Buffer.BlockCopy(snap.NpcBody, 0, _npcBody, 0, snap.NpcBody.Length);
                _npcForm.UpdateBodyData(_npcBody);
            }
            ParseAll(ess);
        }

        public void ApplyToSave(EssFile ess, int newLevel, float newCurrentXp)
        {
            if (ess == null) return;

            ess.Header.Level = Math.Max(1, newLevel);
            ess.Header.CurrentXp = Math.Max(0f, newCurrentXp);

            // Sync core attribute fields into ActorValues list first
            if (IsFallout4)
            {
                SetAvValue(FO4_AV_HEALTH, Health);
                SetAvValue(FO4_AV_ACTIONPOINTS, SecondaryAttr);
                SetAvValue(FO4_AV_RADS, TertiaryAttr);
                SetAvValue(FO4_AV_CARRYWEIGHT, CarryWeight);
                SetAvValue(FO4_AV_SPEEDMULT, SpeedMult);
                foreach (PlayerSkillItem sp in Skills)
                {
                    SetAvValue(sp.ActorValueId, sp.Level);
                }
            }
            else
            {
                SetAvValue(SKYRIM_AV_HEALTH, Health);
                SetAvValue(SKYRIM_AV_MAGICKA, SecondaryAttr);
                SetAvValue(SKYRIM_AV_STAMINA, TertiaryAttr);
                SetAvValue(SKYRIM_AV_CARRYWEIGHT, CarryWeight);
                SetAvValue(SKYRIM_AV_SPEEDMULT, SpeedMult);

                // If DragonSouls is edited and not yet in the Skyrim BaseValues table, insert it safely
                if (DragonSouls > 0f && ActorValues.All(a => a.RawId != SKYRIM_AV_DRAGONSOULS) && _avTableCountOffset >= 0)
                {
                    InsertSkyrimActorValue(SKYRIM_AV_DRAGONSOULS, 162, "DragonSouls", DragonSouls);
                }
                else
                {
                    SetAvValue(SKYRIM_AV_DRAGONSOULS, DragonSouls);
                }

                // Sync 18 Skills to ActorValues (AV 6..23)
                foreach (PlayerSkillItem sk in Skills)
                {
                    SetAvValue(sk.ActorValueId, sk.Level);
                }
            }

            // Write all ActorValues IN-PLACE into ACHR 0x00000014
            if (_achrBody != null)
            {
                foreach (PlayerActorValueItem av in ActorValues)
                {
                    if (av.ValueOffset >= 0 && av.ValueOffset + 4 <= _achrBody.Length)
                    {
                        Buffer.BlockCopy(BitConverter.GetBytes(av.Value), 0, _achrBody, av.ValueOffset, 4);
                    }
                }

                // Write Skyrim PlayerSkills (CurXP + 18 Skills Level & Legendary) IN-PLACE
                if (!IsFallout4 && _playerSkillsOffset >= 0 && _playerSkillsOffset + 12 + 18 * 16 <= _achrBody.Length)
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(ess.Header.CurrentXp), 0, _achrBody, _playerSkillsOffset, 4);
                    for (int s = 0; s < 18 && s < Skills.Count; s++)
                    {
                        int off = _playerSkillsOffset + 12 + s * 16;
                        Buffer.BlockCopy(BitConverter.GetBytes(Skills[s].Level), 0, _achrBody, off, 4);
                        Buffer.BlockCopy(BitConverter.GetBytes(Skills[s].LegendaryCount), 0, _achrBody, off + 12, 4);
                    }
                }

                // Write Gold / Bottlecaps & Lockpicks / Bobby Pins IN-PLACE
                if (_currencyCountOffset >= 0 && _currencyCountOffset + 4 <= _achrBody.Length)
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(Math.Max(0, CurrencyCount)), 0, _achrBody, _currencyCountOffset, 4);
                }
                if (_lockpicksCountOffset >= 0 && _lockpicksCountOffset + 4 <= _achrBody.Length)
                {
                    Buffer.BlockCopy(BitConverter.GetBytes(Math.Max(0, LockpicksCount)), 0, _achrBody, _lockpicksCountOffset, 4);
                }

                _achrForm.UpdateBodyData(_achrBody);
            }

            // Sync NPC_ 0x00000007 (Skyrim ACBS Level + DNAM 18 Base Skills)
            if (_npcBody != null && !IsFallout4)
            {
                if (_acbsOffset >= 0 && _acbsOffset + 24 <= _npcBody.Length)
                {
                    ushort lv = (ushort)Math.Max(1, Math.Min(65535, newLevel));
                    Buffer.BlockCopy(BitConverter.GetBytes(lv), 0, _npcBody, _acbsOffset + 8, 2);
                }
                if (_dnamOffset >= 0 && _dnamOffset + 52 <= _npcBody.Length)
                {
                    for (int s = 0; s < 18 && s < Skills.Count; s++)
                    {
                        byte bSkill = (byte)Math.Max(1, Math.Min(255, (int)Math.Round(Skills[s].Level)));
                        _npcBody[_dnamOffset + s] = bSkill;
                    }
                }
                _npcForm.UpdateBodyData(_npcBody);
            }

            ess.HasChanges = true;
        }

        private void InsertSkyrimActorValue(uint rawId, int normId, string name, float value)
        {
            if (_achrBody == null || _avTableCountOffset < 0 || _avTableStartOffset < 0) return;

            int newCount = _avTableEntryCount + 1;
            byte[] newCountBytes = EncodeVSVal(newCount);
            int insertIndex = _avTableEntryCount;
            for (int i = 0; i < ActorValues.Count; i++)
            {
                if (ActorValues[i].NormalizedId > normId)
                {
                    insertIndex = i;
                    break;
                }
            }

            int insertBytePos = _avTableStartOffset + insertIndex * 8;
            using (MemoryStream ms = new MemoryStream(_achrBody.Length + 12))
            {
                // Before VSVal count
                ms.Write(_achrBody, 0, _avTableCountOffset);
                // New VSVal count
                ms.Write(newCountBytes, 0, newCountBytes.Length);
                // Entries before insertBytePos
                int preEntriesLen = insertBytePos - _avTableStartOffset;
                if (preEntriesLen > 0) ms.Write(_achrBody, _avTableStartOffset, preEntriesLen);
                // New 8-byte entry (rawId + float)
                byte[] idBytes = BitConverter.GetBytes(rawId);
                byte[] valBytes = BitConverter.GetBytes(value);
                ms.Write(idBytes, 0, 4);
                ms.Write(valBytes, 0, 4);
                // Remaining bytes of ACHR
                ms.Write(_achrBody, insertBytePos, _achrBody.Length - insertBytePos);

                _achrBody = ms.ToArray();
            }

            int countDelta = newCountBytes.Length - _avTableCountBytes;
            int totalDelta = countDelta + 8;

            _avTableCountBytes = newCountBytes.Length;
            _avTableStartOffset += countDelta;
            _avTableEntryCount = newCount;
            if (_playerSkillsOffset > insertBytePos) _playerSkillsOffset += totalDelta;
            if (_currencyCountOffset > insertBytePos) _currencyCountOffset += totalDelta;
            if (_lockpicksCountOffset > insertBytePos) _lockpicksCountOffset += totalDelta;

            // Update offsets for existing ActorValues
            foreach (PlayerActorValueItem av in ActorValues)
            {
                if (av.ValueOffset >= insertBytePos) av.ValueOffset += totalDelta;
                else av.ValueOffset += countDelta;
            }

            int newValOffset = _avTableStartOffset + insertIndex * 8 + 4;
            ActorValues.Insert(insertIndex, new PlayerActorValueItem(rawId, normId, name, newValOffset, value));
        }

        private static byte[] EncodeVSVal(int val)
        {
            if (val < 64) return new byte[] { (byte)(val << 2) };
            if (val < 16384)
            {
                ushort v = (ushort)((val << 2) | 1);
                return BitConverter.GetBytes(v);
            }
            uint v32 = (uint)((val << 2) | 2);
            return BitConverter.GetBytes(v32);
        }
    }
}
