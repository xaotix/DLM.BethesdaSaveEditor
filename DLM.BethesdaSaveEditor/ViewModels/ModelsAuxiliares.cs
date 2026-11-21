using System;
using System.Windows.Media.Imaging;
using DLM.BethesdaSaveEditor.Ess.Papyrus;

namespace DLM.BethesdaSaveEditor.ViewModels
{
    public sealed class PluginImpactItem
    {
        public string Indice { get; set; }
        public string Nome { get; set; }
        public string Tipo { get; set; }
        public int ChangeForms { get; set; }
        public int InstanciasScript { get; set; }
        public long BytesChangeForms { get; set; }
        public double PercentualTamanho { get; set; }

        public string TamanhoFormatado => string.Format("{0:N1} KB", BytesChangeForms / 1024.0);
        public string PercentualFormatado => string.Format("{0:N2}%", PercentualTamanho);
    }

    public sealed class SaveSummaryItem
    {
        public string CaminhoCompleto { get; set; }
        public string NomeArquivo { get; set; }
        public string Jogo { get; set; }
        public string Personagem { get; set; }
        public int Nivel { get; set; }
        public string Localizacao { get; set; }
        public DateTime DataModificacao { get; set; }
        public long TamanhoBytes { get; set; }
        public BitmapSource Miniatura { get; set; }

        public string TamanhoMB => string.Format("{0:N2} MB", TamanhoBytes / 1048576.0);
    }

    public sealed class SaveDiffItem
    {
        public string Categoria { get; set; }
        public string Metrica { get; set; }
        public string ValorSaveA { get; set; }
        public string ValorSaveB { get; set; }
        public string Diferenca { get; set; }
    }

    public sealed class LogEntryItem
    {
        public string Hora { get; set; }
        public string Nivel { get; set; }
        public string Mensagem { get; set; }
    }

    public sealed class PapyrusVarDisplayItem
    {
        public int Indice { get; set; }
        public string NomeMembro { get; set; }
        public string Tipo { get; set; }
        public string Valor { get; set; }
        public bool Editavel { get; set; }
        public PapyrusVariable VariavelRef { get; set; }
    }
}
