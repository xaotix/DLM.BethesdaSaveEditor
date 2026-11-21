using System;
using System.Collections.Generic;
using System.Linq;
using DLM.BethesdaSaveEditor.Core;

namespace DLM.BethesdaSaveEditor.Ess
{
    public sealed class QuestBugFixItem : ObservableBase
    {
        private string _statusNoSave = "Not in save";
        private bool _presenteNoSave;

        public bool IsSkyrim { get; set; }
        public int FormID { get; set; }
        public string EditorID { get; set; }
        public string QuestName { get; set; }
        public string BugDescription { get; set; }
        public string FixDescription { get; set; }
        public ushort TargetStage { get; set; }
        public string ConsoleCommand { get; set; }

        public string FormIDHex => FormID.ToString("X8");
        public string GameLabel => IsSkyrim ? "Skyrim SE" : "Fallout 4";

        public bool PresenteNoSave
        {
            get => _presenteNoSave;
            set => SetProperty(ref _presenteNoSave, value);
        }

        public string StatusNoSave
        {
            get => _statusNoSave;
            set => SetProperty(ref _statusNoSave, value);
        }
    }

    public static class QuestFixerCatalog
    {
        public static List<QuestBugFixItem> GetCatalogForGame(GameType game)
        {
            bool isSkyrim = game.EhSkyrim();
            return GetAll().Where(q => q.IsSkyrim == isSkyrim).ToList();
        }

        public static List<QuestBugFixItem> GetAll()
        {
            return new List<QuestBugFixItem>
            {
                // === SKYRIM SE ===
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x0001F7A3, EditorID = "MS11",
                    QuestName = "Blood on the Ice",
                    BugDescription = "Windhelm murder scene fails to trigger, Calixto/Jorleif dialogue locks, or blood trail stalls.",
                    FixDescription = "Advances quest stage to 10 (start investigation) or marks stuck trigger stage active.",
                    TargetStage = 10, ConsoleCommand = "setstage MS11 10"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x0003372B, EditorID = "MQ101",
                    QuestName = "Unbound",
                    BugDescription = "Intro carriage glitches, Hadvar/Ralof freezes, or Helgen exit fails to complete.",
                    FixDescription = "Marks stage 255 / completion progression to unblock main storyline.",
                    TargetStage = 250, ConsoleCommand = "setstage MQ101 250"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x0001E56C, EditorID = "MQ103",
                    QuestName = "Bleak Falls Barrow",
                    BugDescription = "Farengar/Jarl will not accept Dragonstone or Arvel the Swift stays stuck.",
                    FixDescription = "Sets stage 190 to advance past Dragonstone hand-in.",
                    TargetStage = 190, ConsoleCommand = "setstage MQ103 190"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x0002610C, EditorID = "MQ104",
                    QuestName = "Dragon Rising",
                    BugDescription = "Mirmulnir does not give dragon soul or Irileth/Jarl Balgruuf won't advance.",
                    FixDescription = "Sets stage 90 (absorb dragon soul / return to Jarl).",
                    TargetStage = 90, ConsoleCommand = "setstage MQ104 90"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x000242BA, EditorID = "MQ105",
                    QuestName = "The Way of the Voice",
                    BugDescription = "Greybeards stop demonstrating shouts or fail to teach the final word of Unrelenting Force.",
                    FixDescription = "Sets stage 150 to advance past Greybeard demonstration.",
                    TargetStage = 150, ConsoleCommand = "setstage MQ105 150"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x00032926, EditorID = "MQ106",
                    QuestName = "A Blade in the Dark",
                    BugDescription = "Delphine refuses to travel to Kynesgrove or Sahloknir fails to resurrect.",
                    FixDescription = "Sets stage 150 (after Kynesgrove dragon battle).",
                    TargetStage = 150, ConsoleCommand = "setstage MQ106 150"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x00035D5F, EditorID = "MQ201",
                    QuestName = "Diplomatic Immunity",
                    BugDescription = "Malborn fails to open the Thalmor Embassy door or gear handoff breaks.",
                    FixDescription = "Sets stage 80 to advance inside Thalmor Embassy.",
                    TargetStage = 80, ConsoleCommand = "setstage MQ201 80"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x0004D8E0, EditorID = "MQ206",
                    QuestName = "Alduin's Bane",
                    BugDescription = "Reading Elder Scroll at Time-Wound freezes cutscene; Alduin never lands.",
                    FixDescription = "Sets stage 100 (Dragonrend learned, battle Alduin).",
                    TargetStage = 100, ConsoleCommand = "setstage MQ206 100"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x000252C2, EditorID = "MS08",
                    QuestName = "In My Time of Need",
                    BugDescription = "Saadia or Kematu dialogue loops or Alik'r warriors never appear.",
                    FixDescription = "Sets stage 100 to progress confrontation.",
                    TargetStage = 100, ConsoleCommand = "setstage MS08 100"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x0002AC68, EditorID = "DA15",
                    QuestName = "The Mind of Madness",
                    BugDescription = "Pelagius Wing events stall or Wabbajack puzzles stop responding.",
                    FixDescription = "Sets stage 200 to complete Pelagius mindscape.",
                    TargetStage = 200, ConsoleCommand = "setstage DA15 200"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = true, FormID = 0x0002D516, EditorID = "DA04",
                    QuestName = "Discerning the Transmundane",
                    BugDescription = "Septimus Signus fails to open Oghma Infinium box after blood harvesting.",
                    FixDescription = "Sets stage 50 to trigger Dwarven lockbox opening.",
                    TargetStage = 50, ConsoleCommand = "setstage DA04 50"
                },

                // === FALLOUT 4 ===
                new QuestBugFixItem
                {
                    IsSkyrim = false, FormID = 0x001A001C, EditorID = "Min00",
                    QuestName = "When Freedom Calls",
                    BugDescription = "Preston Garvey refuses to speak after clearing Museum of Freedom / Deathclaw.",
                    FixDescription = "Sets stage 100 to unblock Preston dialogue and Sanctuary transition.",
                    TargetStage = 100, ConsoleCommand = "setstage Min00 100"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = false, FormID = 0x0001C22A, EditorID = "MQ102",
                    QuestName = "Out of Time",
                    BugDescription = "Codsworth gets stuck searching Sanctuary Hills neighborhood.",
                    FixDescription = "Sets stage 50 to advance past Sanctuary search.",
                    TargetStage = 50, ConsoleCommand = "setstage MQ102 50"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = false, FormID = 0x000229E5, EditorID = "MQ103",
                    QuestName = "Jewel of the Commonwealth",
                    BugDescription = "Diamond City gate stays closed or Piper/Ellie dialogue stalls.",
                    FixDescription = "Sets stage 50 to complete Diamond City entry.",
                    TargetStage = 50, ConsoleCommand = "setstage MQ103 50"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = false, FormID = 0x0001F25E, EditorID = "MQ104",
                    QuestName = "Unlikely Valentine",
                    BugDescription = "Skinny Malone dialogue locks in Vault 114 or Nick Valentine stops following.",
                    FixDescription = "Sets stage 100 to escape Vault 114 with Nick.",
                    TargetStage = 100, ConsoleCommand = "setstage MQ104 100"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = false, FormID = 0x000229E9, EditorID = "MQ106",
                    QuestName = "Reunions",
                    BugDescription = "Fort Hagen elevator fails to unlock or Kellogg dialogue freezes.",
                    FixDescription = "Sets stage 150 to advance past Kellogg encounter.",
                    TargetStage = 150, ConsoleCommand = "setstage MQ106 150"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = false, FormID = 0x0005DDAB, EditorID = "BoS100",
                    QuestName = "Fire Support",
                    BugDescription = "Paladin Danse repeats 'Check your fire, we've got hostiles!' forever.",
                    FixDescription = "Sets stage 40 to clear ghoul wave bug and talk to Danse.",
                    TargetStage = 40, ConsoleCommand = "setstage BoS100 40"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = false, FormID = 0x000229EC, EditorID = "MQ206",
                    QuestName = "The Molecular Level",
                    BugDescription = "Faction leaders will not recognize built Signal Interceptor or power grid.",
                    FixDescription = "Sets stage 200 to advance teleportation readiness.",
                    TargetStage = 200, ConsoleCommand = "setstage MQ206 200"
                },
                new QuestBugFixItem
                {
                    IsSkyrim = false, FormID = 0x00022A08, EditorID = "MS09",
                    QuestName = "The Secret of Cabot House",
                    BugDescription = "Emogene Cabot is missing behind locked door in Amphitheater.",
                    FixDescription = "Sets stage 60 to progress past Emogene rescue.",
                    TargetStage = 60, ConsoleCommand = "setstage MS09 60"
                }
            };
        }

        public static void RefreshStatus(EssFile ess, IEnumerable<QuestBugFixItem> items)
        {
            if (ess == null || items == null) return;
            Dictionary<int, ChangeForm> questMap = new Dictionary<int, ChangeForm>();
            foreach (ChangeForm cf in ess.ChangeForms)
            {
                if (cf.TipoCodigo == "QUST" && !questMap.ContainsKey(cf.RefID.FormID))
                {
                    questMap[cf.RefID.FormID] = cf;
                }
            }

            foreach (QuestBugFixItem item in items)
            {
                ChangeForm cf;
                if (questMap.TryGetValue(item.FormID, out cf))
                {
                    item.PresenteNoSave = true;
                    item.StatusNoSave = InspectQuestBinaryStatus(cf);
                }
                else
                {
                    item.PresenteNoSave = false;
                    item.StatusNoSave = "Not started / not in ChangeForms";
                }
            }
        }

        private static string InspectQuestBinaryStatus(ChangeForm cf)
        {
            try
            {
                byte[] body = cf.GetDecompressedBody();
                if (body == null || body.Length < 2) return "Present (empty)";

                int pos = 0;
                if ((cf.ChangeFlags & (1u << 0)) != 0) pos += 6; // CHANGE_FORM_FLAGS
                if ((cf.ChangeFlags & (1u << 1)) != 0 && pos + 2 <= body.Length)
                {
                    ushort qFlags = BitConverter.ToUInt16(body, pos);
                    bool running = (qFlags & 0x0001) != 0;
                    bool completed = (qFlags & 0x0002) != 0;
                    if (completed) return string.Format("Completed (Flags 0x{0:X4})", qFlags);
                    if (running) return string.Format("Active / Running (Flags 0x{0:X4})", qFlags);
                    return string.Format("Inactive (Flags 0x{0:X4})", qFlags);
                }
                return "Present in Save";
            }
            catch
            {
                return "Present in Save";
            }
        }

        /// <summary>
        /// Aplica correcao binaria direta no ChangeForm QUST: marca o estagio alvo como concluido (status=1)
        /// se encontrado na tabela de estagios do QUST, ou garante flag Active (bit 0) ligada.
        /// Retorna snapshot anterior para Undo/Redo.
        /// </summary>
        public static bool ApplyBinaryStageFix(EssFile ess, QuestBugFixItem item, out ChangeForm targetForm, out byte[] previousBody, out string resultMsg)
        {
            targetForm = null;
            previousBody = null;
            resultMsg = string.Empty;

            if (ess == null || item == null) return false;
            targetForm = ess.ChangeForms.FirstOrDefault(cf => cf.TipoCodigo == "QUST" && cf.RefID.FormID == item.FormID);
            if (targetForm == null)
            {
                resultMsg = string.Format("Quest {0} ({1}) is not yet in this save's ChangeForms.\nUse in-game console: {2}",
                    item.EditorID, item.FormIDHex, item.ConsoleCommand);
                return false;
            }

            byte[] body = targetForm.GetDecompressedBody();
            if (body == null || body.Length < 4)
            {
                resultMsg = "Quest ChangeForm body is too small to patch safely.";
                return false;
            }

            previousBody = new byte[body.Length];
            Buffer.BlockCopy(body, 0, previousBody, 0, body.Length);

            bool patchedStage = false;
            byte lo = (byte)(item.TargetStage & 0xFF);
            byte hi = (byte)((item.TargetStage >> 8) & 0xFF);

            // Procura a entrada (uint16 StageID, byte Status=0x00) no bloco de estagios do QUST
            int searchStart = ((targetForm.ChangeFlags & (1u << 0)) != 0 ? 6 : 0) + ((targetForm.ChangeFlags & (1u << 1)) != 0 ? 2 : 0);
            int searchLimit = Math.Min(body.Length - 3, searchStart + 240);
            for (int i = searchStart; i <= searchLimit; i++)
            {
                if (body[i] == lo && body[i + 1] == hi && body[i + 2] == 0x00)
                {
                    body[i + 2] = 0x01; // Marca estagio como concluido
                    patchedStage = true;
                    break;
                }
            }

            // Garante que QuestFlags tenha bit 0 (Running/Enabled) ativo se nao estiver Completed
            int flagPos = (targetForm.ChangeFlags & (1u << 0)) != 0 ? 6 : 0;
            if ((targetForm.ChangeFlags & (1u << 1)) != 0 && flagPos + 2 <= body.Length)
            {
                ushort qFlags = BitConverter.ToUInt16(body, flagPos);
                if ((qFlags & 0x0002) == 0) // Nao concluida
                {
                    qFlags |= 0x0001; // Running
                    Buffer.BlockCopy(BitConverter.GetBytes(qFlags), 0, body, flagPos, 2);
                }
            }

            targetForm.UpdateBodyData(body);
            ess.HasChanges = true;

            resultMsg = patchedStage
                ? string.Format("Patched {0} ({1}): Stage {2} marked completed in binary save.", item.QuestName, item.EditorID, item.TargetStage)
                : string.Format("Updated {0} ({1}) QuestFlags to Active. If stage {2} was already set, you can also run: {3}",
                    item.QuestName, item.EditorID, item.TargetStage, item.ConsoleCommand);
            return true;
        }
    }
}

