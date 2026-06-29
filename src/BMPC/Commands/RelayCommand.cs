using System.Windows.Input;

namespace BMPC.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> execute;
        private readonly Predicate<object?> canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommand(Action<object?> executeMethod)
            : this(executeMethod, _ => true)
        {
        }

        public RelayCommand(Action<object?> executeMethod, Predicate<object?> canExecuteMethod)
        {
            this.execute = executeMethod;
            this.canExecute = canExecuteMethod;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            execute(parameter);
        }
    }
}
