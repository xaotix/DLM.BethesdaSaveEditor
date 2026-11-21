using System;
using System.Collections.Generic;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class GlobalPropertyPreset : ObservableBase
    {
        private float _currentValue;
        private bool _presentInSave;

        public bool IsSkyrim { get; set; }
        public int FormID { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string RecommendedRange { get; set; }

        public string FormIDHex => FormID.ToString("X8");

        public bool PresentInSave
        {
            get => _presentInSave;
            set => SetProperty(ref _presentInSave, value);
        }

        public float CurrentValue
        {
            get => _currentValue;
            set => SetProperty(ref _currentValue, value);
        }
    }

    public static class GlobalPropertiesCatalog
    {
        public static List<GlobalPropertyPreset> GetForGame(GameType game)
        {
            bool isSkyrim = game.EhSkyrim();
            return GetAll().Where(g => g.IsSkyrim == isSkyrim).ToList();
        }

        public static List<GlobalPropertyPreset> GetAll()
        {
            return new List<GlobalPropertyPreset>
            {
                // === SKYRIM SE GLOBALS ===
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x0000003A, Name = "TimeScale", Category = "Time", Description = "Ratio of game minutes per real-world minute (Default: 20). Avoid < 5 to protect quests.", RecommendedRange = "6 - 20" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x00000038, Name = "GameHour", Category = "Time", Description = "Current hour of the day on a 24-hour clock (0.0 to 23.99).", RecommendedRange = "0 - 23.99" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x00000039, Name = "GameDaysPassed", Category = "Time", Description = "Total in-game days elapsed since starting this character.", RecommendedRange = ">= 0" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x00000037, Name = "GameDay", Category = "Time", Description = "Current day of the in-game month.", RecommendedRange = "1 - 31" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x00000036, Name = "GameMonth", Category = "Time", Description = "Current month index (0=Morning Star .. 11=Evening Star).", RecommendedRange = "0 - 11" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x00000035, Name = "GameYear", Category = "Time", Description = "Current Fourth Era year (Default: 201).", RecommendedRange = "201+" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x0001A32F, Name = "PlayerIsVampire", Category = "Character", Description = "Vampire state flag (0 = Mortal, 1-4 = Vampire stages).", RecommendedRange = "0 - 4" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x0001A330, Name = "PlayerIsWerewolf", Category = "Character", Description = "Werewolf curse state (0 = No, 1 = Yes).", RecommendedRange = "0 or 1" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x000727DE, Name = "PlayerFollowerCount", Category = "Companions", Description = "Active humanoid follower count (set to 0 if stuck unable to recruit followers).", RecommendedRange = "0 or 1" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x000750BA, Name = "PlayerAnimalCount", Category = "Companions", Description = "Active animal follower count (set to 0 if stuck unable to recruit pets).", RecommendedRange = "0 or 1" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x000F25E4, Name = "PlayerHasHouseWhiterun", Category = "Housing", Description = "Breezehome ownership state (0 = Not owned, 1 = Owned).", RecommendedRange = "0 or 1" },
                new GlobalPropertyPreset { IsSkyrim = true, FormID = 0x0003B525, Name = "MQ101DragonAttack", Category = "World", Description = "Tracks whether random dragon attacks have been enabled in the world.", RecommendedRange = "0 or 1" },

                // === FALLOUT 4 GLOBALS ===
                new GlobalPropertyPreset { IsSkyrim = false, FormID = 0x0000003A, Name = "TimeScale", Category = "Time", Description = "Ratio of game time to real time (Default: 20). Avoid < 6 to prevent script/cell bugs.", RecommendedRange = "6 - 20" },
                new GlobalPropertyPreset { IsSkyrim = false, FormID = 0x00000038, Name = "GameHour", Category = "Time", Description = "Current hour of the day (0.0 to 23.99).", RecommendedRange = "0 - 23.99" },
                new GlobalPropertyPreset { IsSkyrim = false, FormID = 0x00000039, Name = "GameDaysPassed", Category = "Time", Description = "Total in-game days elapsed in the Commonwealth.", RecommendedRange = ">= 0" },
                new GlobalPropertyPreset { IsSkyrim = false, FormID = 0x00000037, Name = "GameDay", Category = "Time", Description = "Current day of the month.", RecommendedRange = "1 - 31" },
                new GlobalPropertyPreset { IsSkyrim = false, FormID = 0x00000036, Name = "GameMonth", Category = "Time", Description = "Current month (0-11).", RecommendedRange = "0 - 11" },
                new GlobalPropertyPreset { IsSkyrim = false, FormID = 0x00000035, Name = "GameYear", Category = "Time", Description = "Current year (Default: 2287).", RecommendedRange = "2287+" },
                new GlobalPropertyPreset { IsSkyrim = false, FormID = 0x0006E037, Name = "HC_Rule_SurvivalMode", Category = "Gameplay", Description = "Survival difficulty state toggle (0 = Off, 1 = Active).", RecommendedRange = "0 or 1" }
            };
        }

        public static void SyncFromSave(EssFile ess, IEnumerable<GlobalPropertyPreset> presets)
        {
            if (ess == null || ess.GlobalVariables == null || presets == null) return;
            Dictionary<int, GlobalVariable> map = new Dictionary<int, GlobalVariable>();
            foreach (GlobalVariable gv in ess.GlobalVariables.Variables)
            {
                if (!map.ContainsKey(gv.RefID.FormID)) map[gv.RefID.FormID] = gv;
            }

            foreach (GlobalPropertyPreset p in presets)
            {
                GlobalVariable gv;
                if (map.TryGetValue(p.FormID, out gv))
                {
                    p.PresentInSave = true;
                    p.CurrentValue = gv.Valor;
                }
                else
                {
                    p.PresentInSave = false;
                    p.CurrentValue = 0f;
                }
            }
        }
    }
}

