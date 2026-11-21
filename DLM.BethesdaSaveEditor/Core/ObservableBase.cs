using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DLM.BethesdaSaveEditor.Core
{
    public abstract class ObservableBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T campo, T valor, [CallerMemberName] string propriedade = null)
        {
            if (EqualityComparer<T>.Default.Equals(campo, valor))
            {
                return false;
            }

            campo = valor;
            OnPropertyChanged(propriedade);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propriedade = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propriedade));
        }
    }
}

