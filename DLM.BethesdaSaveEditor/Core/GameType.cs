using System;
using System.IO;

namespace DLM.BethesdaSaveEditor.Core
{
    public enum GameType
    {
        SkyrimLE,
        SkyrimSE,
        SkyrimSW,
        SkyrimVR,
        Fallout4,
        FalloutVR
    }

    public static class GameTypeExtensions
    {
        public static string ObterNome(this GameType jogo)
        {
            switch (jogo)
            {
                case GameType.SkyrimLE: return "Skyrim Legendary Edition";
                case GameType.SkyrimSE: return "Skyrim Special Edition";
                case GameType.SkyrimSW: return "Skyrim Switch Edition";
                case GameType.SkyrimVR: return "Skyrim VR";
                case GameType.Fallout4: return "Fallout 4";
                case GameType.FalloutVR: return "Fallout 4 VR";
                default: return jogo.ToString();
            }
        }

        public static string ObterExtensaoSave(this GameType jogo)
        {
            switch (jogo)
            {
                case GameType.SkyrimSW: return "sav0";
                case GameType.Fallout4:
                case GameType.FalloutVR: return "fos";
                default: return "ess";
            }
        }

        public static string ObterExtensaoCosave(this GameType jogo)
        {
            switch (jogo)
            {
                case GameType.Fallout4:
                case GameType.FalloutVR: return "f4se";
                default: return "skse";
            }
        }

        public static bool EhSkyrim(this GameType jogo)
        {
            return jogo == GameType.SkyrimLE
                || jogo == GameType.SkyrimSE
                || jogo == GameType.SkyrimSW
                || jogo == GameType.SkyrimVR;
        }

        public static bool EhFallout4(this GameType jogo)
        {
            return jogo == GameType.Fallout4 || jogo == GameType.FalloutVR;
        }

        public static bool EhID64(this GameType jogo)
        {
            return jogo != GameType.SkyrimLE;
        }

        public static bool SuportaCompressao(this GameType jogo)
        {
            return jogo == GameType.SkyrimSE
                || jogo == GameType.SkyrimSW
                || jogo == GameType.SkyrimVR;
        }

        public static string ObterPastaSavesPadrao(this GameType jogo)
        {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            switch (jogo)
            {
                case GameType.SkyrimLE: return Path.Combine(docs, "My Games", "Skyrim", "Saves");
                case GameType.SkyrimSE: return Path.Combine(docs, "My Games", "Skyrim Special Edition", "Saves");
                case GameType.SkyrimVR: return Path.Combine(docs, "My Games", "Skyrim VR", "Saves");
                case GameType.Fallout4: return Path.Combine(docs, "My Games", "Fallout4", "Saves");
                case GameType.FalloutVR: return Path.Combine(docs, "My Games", "Fallout4VR", "Saves");
                default: return Path.Combine(docs, "My Games");
            }
        }
    }
}

