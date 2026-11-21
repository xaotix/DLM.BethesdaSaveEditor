using System;
using System.Windows.Input;

namespace DLM.BethesdaSaveEditor.Core
{
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _executar;
        private readonly Predicate<object> _podeExecutar;

        public RelayCommand(Action<object> executar, Predicate<object> podeExecutar = null)
        {
            _executar = executar ?? throw new ArgumentNullException(nameof(executar));
            _podeExecutar = podeExecutar;
        }

        public RelayCommand(Action executar, Func<bool> podeExecutar = null)
            : this(
                _ => executar(),
                podeExecutar != null ? new Predicate<object>(_ => podeExecutar()) : null)
        {
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _podeExecutar == null || _podeExecutar(parameter);
        }

        public void Execute(object parameter)
        {
            _executar(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}

