using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace DLM.BethesdaSaveEditor.Core
{
    public sealed class LocalizationService : INotifyPropertyChanged
    {
        public static readonly LocalizationService Instance = new LocalizationService();

        private string _currentLanguage = "en";
        private bool _autoBackup = true;
        private readonly Dictionary<string, Dictionary<string, string>> _tables;
        private readonly string _settingsPath;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                string norm = (value ?? "en").ToLowerInvariant();
                if (!_tables.ContainsKey(norm)) norm = "en";
                if (_currentLanguage != norm)
                {
                    _currentLanguage = norm;
                    SaveSettings();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
                }
            }
        }

        public bool AutoBackup
        {
            get => _autoBackup;
            set
            {
                if (_autoBackup != value)
                {
                    _autoBackup = value;
                    SaveSettings();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoBackup)));
                }
            }
        }

        public string this[string key]
        {
            get
            {
                Dictionary<string, string> dict;
                string val;
                if (_tables.TryGetValue(_currentLanguage, out dict) && dict.TryGetValue(key, out val))
                {
                    return val;
                }
                if (_tables["en"].TryGetValue(key, out val))
                {
                    return val;
                }
                return key;
            }
        }

        private LocalizationService()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DLM.BethesdaSaveEditor");
            _settingsPath = Path.Combine(appData, "settings.ini");
            _tables = BuildTables();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return;
                foreach (string line in File.ReadAllLines(_settingsPath))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    if (k == "Language" && _tables.ContainsKey(v)) _currentLanguage = v;
                    else if (k == "AutoBackup") _autoBackup = !string.Equals(v, "false", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
            }
        }

        private void SaveSettings()
        {
            try
            {
                string dir = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllLines(_settingsPath, new string[]
                {
                    "Language=" + _currentLanguage,
                    "AutoBackup=" + (_autoBackup ? "true" : "false")
                });
            }
            catch
            {
            }
        }

        private static Dictionary<string, Dictionary<string, string>> BuildTables()
        {
            Dictionary<string, string> en = new Dictionary<string, string>
            {
                ["AppTitle"] = "DLM.BethesdaSaveEditor",
                ["DevelopedBy"] = "Developed by xaotix",
                ["MenuFile"] = "_File",
                ["MenuOpen"] = "_Open Save...",
                ["MenuSaveBrowser"] = "Save _Browser...",
                ["MenuSave"] = "_Save",
                ["MenuSaveAs"] = "Save _As...",
                ["MenuExportPlugins"] = "Export _Plugin List...",
                ["MenuExportShot"] = "Export _Screenshot (PNG)...",
                ["MenuExit"] = "E_xit",
                ["MenuClean"] = "_Clean",
                ["MenuFullClean"] = "_Full Clean (Recommended)",
                ["MenuCleanUnattached"] = "Remove _Unattached Instances",
                ["MenuCleanUndefined"] = "Remove _Undefined Elements",
                ["MenuCleanNonExistent"] = "Remove _Non-Existent Created Instances",
                ["MenuPurifyFormLists"] = "Purify _FormLists (Remove Null Refs)",
                ["MenuBatchClean"] = "_Batch Clean Folder...",
                ["MenuTools"] = "_Tools",
                ["MenuEditHeader"] = "Edit _Header & Player Location...",
                ["MenuCompareSaves"] = "_Compare Two Saves...",
                ["MenuOptions"] = "_Options",
                ["MenuSettings"] = "_Settings...",
                ["MenuHelp"] = "_Help",
                ["MenuAbout"] = "_About...",
                ["MenuEdit"] = "_Edit",
                ["MenuUndo"] = "_Undo",
                ["MenuRedo"] = "_Redo",
                ["FooterUndo"] = "Undo",
                ["FooterRedo"] = "Redo",
                ["TabCharacterGlobals"] = "Character & Properties",
                ["TabQuestFixer"] = "Quest Bug Fixer",
                ["GrpBaseAttributes"] = "Character Attributes (Direct Binary Edit)",
                ["GrpSkills"] = "Base Skills (NPC_ DNAM)",
                ["GrpGlobalPresets"] = "Editable Global Properties (Skyrim SE / Fallout 4)",
                ["GrpConsoleExtras"] = "Perks, Carry Weight & Dragon Souls (Console Batch Script)",
                ["BtnApplyCharacter"] = "Apply Character Changes",
                ["BtnApplyGlobalPreset"] = "Apply Selected Property",
                ["BtnExportBat"] = "Generate Console .txt Script...",
                ["BtnFixQuest"] = "Apply Binary Quest Fix",
                ["BtnCopyConsole"] = "Copy Console Command",

                ["NoScreenshot"] = "No screenshot",
                ["LblLevel"] = "Level:",
                ["LblRaceGender"] = "Race/Gender:",
                ["LblLocation"] = "Location:",
                ["LblGameDate"] = "Game Date:",
                ["LblDiskSize"] = "Disk Size:",
                ["LblUncompSize"] = "Uncompressed:",
                ["LblCompression"] = "Compression:",
                ["LblIntegrity"] = "Integrity:",
                ["LblSaveHealth"] = "Save Health",
                ["LblFilter"] = "Filter:",

                ["TabPlugins"] = "Plugins & Impact",
                ["TabMiscStats"] = "Misc Stats",
                ["TabPapyrus"] = "Papyrus Editor",
                ["TabChangeForms"] = "ChangeForms & Hex",
                ["TabGlobals"] = "Global Variables",
                ["TabLog"] = "Log",

                ["SubTabInstances"] = "Script Instances",
                ["SubTabArrays"] = "Arrays",
                ["SubTabScriptsThreads"] = "Scripts & Active Threads",
                ["SubTabStrings"] = "String Table",

                ["ColIndex"] = "Index",
                ["ColPlugin"] = "Plugin",
                ["ColType"] = "Type",
                ["ColChangeForms"] = "ChangeForms",
                ["ColScripts"] = "Scripts",
                ["ColSize"] = "Size",
                ["ColPercentSave"] = "% Save",
                ["ColCategory"] = "Category",
                ["ColStatName"] = "Stat Name",
                ["ColValue"] = "Value",
                ["ColScript"] = "Script",
                ["ColParent"] = "Parent",
                ["ColInstances"] = "Instances",
                ["ColMembers"] = "Members",
                ["ColState"] = "State",
                ["ColVars"] = "Vars",
                ["ColStatus"] = "Status",
                ["ColMember"] = "Member",
                ["ColTopScript"] = "Top Script",
                ["ColEvent"] = "Event",
                ["ColFrames"] = "Frames",
                ["ColDescription"] = "Description",
                ["ColFlags"] = "Flags",
                ["ColBytes"] = "Bytes",
                ["ColComp"] = "Comp.",
                ["ColKnownName"] = "Known Name",
                ["ColValueFloat"] = "Value (Float)",
                ["ColTime"] = "Time",
                ["ColLevel"] = "Level",
                ["ColMessage"] = "Message",
                ["ColLength"] = "Length",
                ["ColText"] = "String Text",

                ["ChkOnlyUnattached"] = "Unattached only",
                ["ChkOnlyUndefined"] = "Undefined only",
                ["BtnDeleteSelected"] = "Delete Selected",
                ["BtnEditVariable"] = "Edit Value...",
                ["BtnClearBounties"] = "Clear All Bounties",
                ["BtnAddEntry"] = "Add Entry",
                ["BtnRemoveEntry"] = "Remove Entry",
                ["BtnShiftUp"] = "Shift Up",
                ["BtnShiftDown"] = "Shift Down",
                ["BtnTerminateThread"] = "Terminate Thread",
                ["BtnDeleteThread"] = "Delete Thread",
                ["BtnEditString"] = "Edit String...",
                ["BtnAddString"] = "Add String...",
                ["BtnEditFlags"] = "Edit ChangeFlags...",
                ["GrpInstanceVars"] = "Selected Instance Variables",
                ["GrpArrayElements"] = "Selected Array Elements",
                ["GrpActiveThreads"] = "Active Scripts (Threads)",
                ["GrpHexInspector"] = "Hexadecimal Inspector",

                ["BtnCancel"] = "Cancel",
                ["BtnConfirm"] = "Confirm",
                ["BtnClose"] = "Close",
                ["BtnBrowse"] = "Browse...",
                ["BtnCompare"] = "Compare",
                ["BtnRunClean"] = "Run Batch Clean",
                ["BtnOpenSave"] = "Open Save",

                ["StatusReady"] = "Ready",
                ["StatusMemory"] = "Memory",
                ["NoFileLoaded"] = "No file loaded",
                ["StatusHealthy"] = "Healthy",
                ["StatusHealthyCosave"] = "Healthy (+ Co-save)",
                ["StatusBroken"] = "Corrupted / Truncated",

                ["OptTitle"] = "Settings",
                ["OptLanguage"] = "Interface Language:",
                ["OptAutoBackup"] = "Automatically create timestamped backup (.bak) before saving",

                ["AboutTitle"] = "About DLM.BethesdaSaveEditor",
                ["AboutSubtitle"] = "Version 1.0.0.0 - WPF Edition (.NET Framework 4.8 / C# 7.3)",
                ["AboutDevLabel"] = "WPF Edition Developer",
                ["AboutOrigLabel"] = "Original Author & Credits",
                ["AboutOrigText"] = "Mark Fairchild - Original ReSaver (Java) & ESS/Papyrus format architecture (Apache License 2.0).",
                ["AboutDesc"] = "Native Windows savegame editor, analyzer, and cleaner for Skyrim (LE, SE, VR, Switch) and Fallout 4.",

                ["HeaderTitle"] = "Edit Header & Player Location",
                ["HeaderTabGeneral"] = "Character & Compression",
                ["HeaderTabCoords"] = "Player Coordinates",
                ["LblCharacter"] = "Character Name:",
                ["LblSaveNumber"] = "Save Number:",
                ["LblRace"] = "Race ID:",
                ["LblGender"] = "Gender:",
                ["LblCurrentXp"] = "Current XP:",
                ["LblNeededXp"] = "XP to Next Level:",
                ["GenderMale"] = "Male",
                ["GenderFemale"] = "Female",

                ["FlagsTitle"] = "ChangeFlags Bit Editor",
                ["FlagsMask"] = "Hex Mask:",

                ["BrowserTitle"] = "Save Browser",
                ["LblFolder"] = "Folder:",
                ["CompareTitle"] = "Compare Two Saves",
                ["BatchTitle"] = "Batch Clean Folder"
            };

            Dictionary<string, string> pt = new Dictionary<string, string>
            {
                ["AppTitle"] = "DLM.BethesdaSaveEditor",
                ["DevelopedBy"] = "Desenvolvido por xaotix",
                ["MenuFile"] = "_Arquivo",
                ["MenuOpen"] = "_Abrir Save...",
                ["MenuSaveBrowser"] = "_Navegador de Saves...",
                ["MenuSave"] = "_Salvar",
                ["MenuSaveAs"] = "Salvar _Como...",
                ["MenuExportPlugins"] = "Exportar Lista de _Plugins...",
                ["MenuExportShot"] = "Exportar Captura de _Tela (PNG)...",
                ["MenuExit"] = "Sai_r",
                ["MenuClean"] = "_Limpeza",
                ["MenuFullClean"] = "Limpeza _Completa (Recomendado)",
                ["MenuCleanUnattached"] = "Remover Instancias _Soltas (Unattached)",
                ["MenuCleanUndefined"] = "Remover Elementos _Indefinidos (Undefined)",
                ["MenuCleanNonExistent"] = "Remover Instancias de Forms _Inexistentes",
                ["MenuPurifyFormLists"] = "Purificar _FormLists (Remover Null Refs)",
                ["MenuBatchClean"] = "Limpeza em _Lote (Pasta)...",
                ["MenuTools"] = "_Ferramentas",
                ["MenuEditHeader"] = "Editar _Cabecalho e Posicao do Jogador...",
                ["MenuCompareSaves"] = "_Comparar Dois Saves...",
                ["MenuOptions"] = "_Opcoes",
                ["MenuSettings"] = "_Configuracoes...",
                ["MenuHelp"] = "_Ajuda",
                ["MenuAbout"] = "_Sobre...",
                ["MenuEdit"] = "_Editar",
                ["MenuUndo"] = "_Desfazer",
                ["MenuRedo"] = "_Refazer",
                ["FooterUndo"] = "Desfazer",
                ["FooterRedo"] = "Refazer",
                ["TabCharacterGlobals"] = "Personagem e Propriedades",
                ["TabQuestFixer"] = "Corretor de Missoes",
                ["GrpBaseAttributes"] = "Atributos do Personagem (Edicao Binaria Direta)",
                ["GrpSkills"] = "Habilidades Base (NPC_ DNAM)",
                ["GrpGlobalPresets"] = "Propriedades Globais Editaveis (Skyrim SE / Fallout 4)",
                ["GrpConsoleExtras"] = "Perks, Carga e Almas de Dragao (Script de Console)",
                ["BtnApplyCharacter"] = "Aplicar Alteracoes do Personagem",
                ["BtnApplyGlobalPreset"] = "Aplicar Propriedade Selecionada",
                ["BtnExportBat"] = "Gerar Script .txt de Console...",
                ["BtnFixQuest"] = "Aplicar Correcao Binaria",
                ["BtnCopyConsole"] = "Copiar Comando de Console",

                ["NoScreenshot"] = "Sem captura",
                ["LblLevel"] = "Nivel:",
                ["LblRaceGender"] = "Raca/Genero:",
                ["LblLocation"] = "Localizacao:",
                ["LblGameDate"] = "Data no Jogo:",
                ["LblDiskSize"] = "Disco:",
                ["LblUncompSize"] = "Descompactado:",
                ["LblCompression"] = "Compressao:",
                ["LblIntegrity"] = "Integridade:",
                ["LblSaveHealth"] = "Saude do Save",
                ["LblFilter"] = "Filtro:",

                ["TabPlugins"] = "Plugins e Impacto",
                ["TabMiscStats"] = "Estatisticas",
                ["TabPapyrus"] = "Editor Papyrus",
                ["TabChangeForms"] = "ChangeForms e Hex",
                ["TabGlobals"] = "Variaveis Globais",
                ["TabLog"] = "Log",

                ["SubTabInstances"] = "Instancias de Script",
                ["SubTabArrays"] = "Arrays",
                ["SubTabScriptsThreads"] = "Scripts e Threads Ativas",
                ["SubTabStrings"] = "Tabela de Strings",

                ["ColIndex"] = "Indice",
                ["ColPlugin"] = "Plugin",
                ["ColType"] = "Tipo",
                ["ColChangeForms"] = "ChangeForms",
                ["ColScripts"] = "Scripts",
                ["ColSize"] = "Tamanho",
                ["ColPercentSave"] = "% Save",
                ["ColCategory"] = "Categoria",
                ["ColStatName"] = "Estatistica",
                ["ColValue"] = "Valor",
                ["ColScript"] = "Script",
                ["ColParent"] = "Pai",
                ["ColInstances"] = "Instancias",
                ["ColMembers"] = "Membros",
                ["ColState"] = "Estado",
                ["ColVars"] = "Vars",
                ["ColStatus"] = "Status",
                ["ColMember"] = "Membro",
                ["ColTopScript"] = "Script no Topo",
                ["ColEvent"] = "Evento",
                ["ColFrames"] = "Frames",
                ["ColDescription"] = "Descricao",
                ["ColFlags"] = "Flags",
                ["ColBytes"] = "Bytes",
                ["ColComp"] = "Comp.",
                ["ColKnownName"] = "Nome Conhecido",
                ["ColValueFloat"] = "Valor (Float)",
                ["ColTime"] = "Hora",
                ["ColLevel"] = "Nivel",
                ["ColMessage"] = "Mensagem",
                ["ColLength"] = "Tamanho",
                ["ColText"] = "Texto da String",

                ["ChkOnlyUnattached"] = "Somente soltas",
                ["ChkOnlyUndefined"] = "Somente indefinidas",
                ["BtnDeleteSelected"] = "Excluir Selecionado",
                ["BtnEditVariable"] = "Editar Valor...",
                ["BtnClearBounties"] = "Zerar Recompensas (Bounties)",
                ["BtnAddEntry"] = "Adicionar Item",
                ["BtnRemoveEntry"] = "Remover Item",
                ["BtnShiftUp"] = "Subir",
                ["BtnShiftDown"] = "Descer",
                ["BtnTerminateThread"] = "Encerrar Thread",
                ["BtnDeleteThread"] = "Excluir Thread",
                ["BtnEditString"] = "Editar String...",
                ["BtnAddString"] = "Nova String...",
                ["BtnEditFlags"] = "Editar ChangeFlags...",
                ["GrpInstanceVars"] = "Variaveis da Instancia Selecionada",
                ["GrpArrayElements"] = "Elementos do Array Selecionado",
                ["GrpActiveThreads"] = "Scripts Ativos (Threads)",
                ["GrpHexInspector"] = "Inspetor Hexadecimal",

                ["BtnCancel"] = "Cancelar",
                ["BtnConfirm"] = "Confirmar",
                ["BtnClose"] = "Fechar",
                ["BtnBrowse"] = "Procurar...",
                ["BtnCompare"] = "Comparar",
                ["BtnRunClean"] = "Executar Limpeza",
                ["BtnOpenSave"] = "Abrir Save",

                ["StatusReady"] = "Pronto",
                ["StatusMemory"] = "Memoria",
                ["NoFileLoaded"] = "Nenhum arquivo carregado",
                ["StatusHealthy"] = "Integro",
                ["StatusHealthyCosave"] = "Integro (+ Co-save)",
                ["StatusBroken"] = "Corrompido / Truncado",

                ["OptTitle"] = "Configuracoes",
                ["OptLanguage"] = "Idioma da Interface:",
                ["OptAutoBackup"] = "Criar backup automatico (.bak) com data/hora antes de salvar",

                ["AboutTitle"] = "Sobre o DLM.BethesdaSaveEditor",
                ["AboutSubtitle"] = "Versao 1.0.0.0 - Edicao WPF (.NET Framework 4.8 / C# 7.3)",
                ["AboutDevLabel"] = "Desenvolvedor da Edicao WPF",
                ["AboutOrigLabel"] = "Autor Original e Creditos",
                ["AboutOrigText"] = "Mark Fairchild - ReSaver original (Java) e especificacao dos formatos ESS/Papyrus (Licenca Apache 2.0).",
                ["AboutDesc"] = "Editor, analisador e limpador nativo para arquivos de save de Skyrim (LE, SE, VR, Switch) e Fallout 4.",

                ["HeaderTitle"] = "Editar Cabecalho e Posicao do Jogador",
                ["HeaderTabGeneral"] = "Personagem e Compressao",
                ["HeaderTabCoords"] = "Coordenadas do Jogador",
                ["LblCharacter"] = "Personagem:",
                ["LblSaveNumber"] = "Numero do Save:",
                ["LblRace"] = "ID da Raca:",
                ["LblGender"] = "Genero:",
                ["LblCurrentXp"] = "XP Atual:",
                ["LblNeededXp"] = "XP Proximo Nivel:",
                ["GenderMale"] = "Masculino",
                ["GenderFemale"] = "Feminino",

                ["FlagsTitle"] = "Editor de Bits ChangeFlags",
                ["FlagsMask"] = "Mascara Hex:",

                ["BrowserTitle"] = "Navegador de Saves",
                ["LblFolder"] = "Pasta:",
                ["CompareTitle"] = "Comparar Dois Saves",
                ["BatchTitle"] = "Limpeza em Lote"
            };

            Dictionary<string, string> es = new Dictionary<string, string>
            {
                ["AppTitle"] = "DLM.BethesdaSaveEditor",
                ["DevelopedBy"] = "Desarrollado por xaotix",
                ["MenuFile"] = "_Archivo",
                ["MenuOpen"] = "_Abrir Partida...",
                ["MenuSaveBrowser"] = "_Explorador de Partidas...",
                ["MenuSave"] = "_Guardar",
                ["MenuSaveAs"] = "Guardar _Como...",
                ["MenuExportPlugins"] = "Exportar Lista de _Plugins...",
                ["MenuExportShot"] = "Exportar _Captura (PNG)...",
                ["MenuExit"] = "Sa_lir",
                ["MenuClean"] = "_Limpiar",
                ["MenuFullClean"] = "Limpieza _Completa (Recomendado)",
                ["MenuCleanUnattached"] = "Eliminar Instancias _Sueltas (Unattached)",
                ["MenuCleanUndefined"] = "Eliminar Elementos _Indefinidos (Undefined)",
                ["MenuCleanNonExistent"] = "Eliminar Instancias de Forms _Inexistentes",
                ["MenuPurifyFormLists"] = "Purificar _FormLists (Eliminar Null Refs)",
                ["MenuBatchClean"] = "Limpieza por _Lotes (Carpeta)...",
                ["MenuTools"] = "_Herramientas",
                ["MenuEditHeader"] = "Editar _Cabecera y Ubicacion...",
                ["MenuCompareSaves"] = "_Comparar Dos Partidas...",
                ["MenuOptions"] = "_Opciones",
                ["MenuSettings"] = "_Configuracion...",
                ["MenuHelp"] = "A_yuda",
                ["MenuAbout"] = "_Acerca de...",
                ["MenuEdit"] = "_Editar",
                ["MenuUndo"] = "_Deshacer",
                ["MenuRedo"] = "_Rehacer",
                ["FooterUndo"] = "Deshacer",
                ["FooterRedo"] = "Rehacer",
                ["TabCharacterGlobals"] = "Personaje y Propiedades",
                ["TabQuestFixer"] = "Reparador de Misiones",
                ["GrpBaseAttributes"] = "Atributos del Personaje (Edicion Binaria Directa)",
                ["GrpSkills"] = "Habilidades Base (NPC_ DNAM)",
                ["GrpGlobalPresets"] = "Propiedades Globales Editables (Skyrim SE / Fallout 4)",
                ["GrpConsoleExtras"] = "Perks, Carga y Almas de Dragon (Script de Consola)",
                ["BtnApplyCharacter"] = "Aplicar Cambios del Personaje",
                ["BtnApplyGlobalPreset"] = "Aplicar Propiedad Seleccionada",
                ["BtnExportBat"] = "Generar Script .txt de Consola...",
                ["BtnFixQuest"] = "Aplicar Correccion Binaria",
                ["BtnCopyConsole"] = "Copiar Comando de Consola",

                ["NoScreenshot"] = "Sin captura",
                ["LblLevel"] = "Nivel:",
                ["LblRaceGender"] = "Raza/Genero:",
                ["LblLocation"] = "Ubicacion:",
                ["LblGameDate"] = "Fecha del Juego:",
                ["LblDiskSize"] = "Disco:",
                ["LblUncompSize"] = "Descomprimido:",
                ["LblCompression"] = "Compresion:",
                ["LblIntegrity"] = "Integridad:",
                ["LblSaveHealth"] = "Salud de la Partida",
                ["LblFilter"] = "Filtro:",

                ["TabPlugins"] = "Plugins e Impacto",
                ["TabMiscStats"] = "Estadisticas",
                ["TabPapyrus"] = "Editor Papyrus",
                ["TabChangeForms"] = "ChangeForms y Hex",
                ["TabGlobals"] = "Variables Globales",
                ["TabLog"] = "Registro",

                ["SubTabInstances"] = "Instancias de Script",
                ["SubTabArrays"] = "Arrays",
                ["SubTabScriptsThreads"] = "Scripts e Hilos Activos",
                ["SubTabStrings"] = "Tabla de Cadenas",

                ["ColIndex"] = "Indice",
                ["ColPlugin"] = "Plugin",
                ["ColType"] = "Tipo",
                ["ColChangeForms"] = "ChangeForms",
                ["ColScripts"] = "Scripts",
                ["ColSize"] = "Tamano",
                ["ColPercentSave"] = "% Partida",
                ["ColCategory"] = "Categoria",
                ["ColStatName"] = "Estadistica",
                ["ColValue"] = "Valor",
                ["ColScript"] = "Script",
                ["ColParent"] = "Padre",
                ["ColInstances"] = "Instancias",
                ["ColMembers"] = "Miembros",
                ["ColState"] = "Estado",
                ["ColVars"] = "Vars",
                ["ColStatus"] = "Estado",
                ["ColMember"] = "Miembro",
                ["ColTopScript"] = "Script Superior",
                ["ColEvent"] = "Evento",
                ["ColFrames"] = "Frames",
                ["ColDescription"] = "Descripcion",
                ["ColFlags"] = "Flags",
                ["ColBytes"] = "Bytes",
                ["ColComp"] = "Comp.",
                ["ColKnownName"] = "Nombre Conocido",
                ["ColValueFloat"] = "Valor (Float)",
                ["ColTime"] = "Hora",
                ["ColLevel"] = "Nivel",
                ["ColMessage"] = "Mensaje",
                ["ColLength"] = "Longitud",
                ["ColText"] = "Texto",

                ["ChkOnlyUnattached"] = "Solo sueltas",
                ["ChkOnlyUndefined"] = "Solo indefinidas",
                ["BtnDeleteSelected"] = "Eliminar Seleccionado",
                ["BtnEditVariable"] = "Editar Valor...",
                ["BtnClearBounties"] = "Limpiar Recompensas (Bounties)",
                ["BtnAddEntry"] = "Agregar Entrada",
                ["BtnRemoveEntry"] = "Quitar Entrada",
                ["BtnShiftUp"] = "Subir",
                ["BtnShiftDown"] = "Bajar",
                ["BtnTerminateThread"] = "Terminar Hilo",
                ["BtnDeleteThread"] = "Eliminar Hilo",
                ["BtnEditString"] = "Editar Cadena...",
                ["BtnAddString"] = "Nueva Cadena...",
                ["BtnEditFlags"] = "Editar ChangeFlags...",
                ["GrpInstanceVars"] = "Variables de la Instancia Seleccionada",
                ["GrpArrayElements"] = "Elementos del Array Seleccionado",
                ["GrpActiveThreads"] = "Scripts Activos (Hilos)",
                ["GrpHexInspector"] = "Inspector Hexadecimal",

                ["BtnCancel"] = "Cancelar",
                ["BtnConfirm"] = "Confirmar",
                ["BtnClose"] = "Cerrar",
                ["BtnBrowse"] = "Examinar...",
                ["BtnCompare"] = "Comparar",
                ["BtnRunClean"] = "Ejecutar Limpieza",
                ["BtnOpenSave"] = "Abrir Partida",

                ["StatusReady"] = "Listo",
                ["StatusMemory"] = "Memoria",
                ["NoFileLoaded"] = "Ningun archivo cargado",
                ["StatusHealthy"] = "Integro",
                ["StatusHealthyCosave"] = "Integro (+ Co-save)",
                ["StatusBroken"] = "Corrupto / Truncado",

                ["OptTitle"] = "Configuracion",
                ["OptLanguage"] = "Idioma de la Interfaz:",
                ["OptAutoBackup"] = "Crear copia de seguridad automatica (.bak) antes de guardar",

                ["AboutTitle"] = "Acerca de DLM.BethesdaSaveEditor",
                ["AboutSubtitle"] = "Version 1.0.0.0 - Edicion WPF (.NET Framework 4.8 / C# 7.3)",
                ["AboutDevLabel"] = "Desarrollador de la Edicion WPF",
                ["AboutOrigLabel"] = "Autor Original y Creditos",
                ["AboutOrigText"] = "Mark Fairchild - ReSaver original (Java) y arquitectura de formatos ESS/Papyrus (Licencia Apache 2.0).",
                ["AboutDesc"] = "Editor, analizador y limpiador nativo para partidas guardadas de Skyrim (LE, SE, VR, Switch) y Fallout 4.",

                ["HeaderTitle"] = "Editar Cabecera y Ubicacion del Jugador",
                ["HeaderTabGeneral"] = "Personaje y Compresion",
                ["HeaderTabCoords"] = "Coordenadas del Jugador",
                ["LblCharacter"] = "Personaje:",
                ["LblSaveNumber"] = "Numero de Partida:",
                ["LblRace"] = "ID de Raza:",
                ["LblGender"] = "Genero:",
                ["LblCurrentXp"] = "XP Actual:",
                ["LblNeededXp"] = "XP Siguiente Nivel:",
                ["GenderMale"] = "Masculino",
                ["GenderFemale"] = "Femenino",

                ["FlagsTitle"] = "Editor de Bits ChangeFlags",
                ["FlagsMask"] = "Mascara Hex:",

                ["BrowserTitle"] = "Explorador de Partidas",
                ["LblFolder"] = "Carpeta:",
                ["CompareTitle"] = "Comparar Dos Partidas",
                ["BatchTitle"] = "Limpieza por Lotes"
            };

            return new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = en,
                ["pt"] = pt,
                ["es"] = es
            };
        }
    }
}

