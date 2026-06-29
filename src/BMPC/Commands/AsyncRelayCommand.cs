using System.Windows.Input;

namespace BMPC.Commands
{
    public sealed class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> execute;
        private readonly Predicate<object?> canExecute;
        private readonly Action<Exception>? errorHandler;
        private bool isExecuting;

        public AsyncRelayCommand(
            Func<object?, Task> execute,
            Predicate<object?>? canExecute = null,
            Action<Exception>? errorHandler = null)
        {
            this.execute = execute;
            this.canExecute = canExecute ?? (_ => true);
            this.errorHandler = errorHandler;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => !this.isExecuting && this.canExecute(parameter);

        public async void Execute(object? parameter)
        {
            if (!this.CanExecute(parameter))
            {
                return;
            }

            try
            {
                this.isExecuting = true;
                this.RaiseCanExecuteChanged();
                await this.execute(parameter);
            }
            catch (Exception ex) when (this.errorHandler != null)
            {
                this.errorHandler(ex);
            }
            finally
            {
                this.isExecuting = false;
                this.RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            this.CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
