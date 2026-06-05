using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AutoTool.Helpers
{
    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync(object parameter);
    }

    public class AsyncRelayCommand : IAsyncCommand
    {
        private readonly Func<Task> _execute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object parameter)
        {
            return !_isExecuting;
        }

        public event EventHandler CanExecuteChanged;

        public async Task ExecuteAsync(object parameter)
        {
            _isExecuting = true;
            OnCanExecuteChanged();
            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                OnCanExecuteChanged();
            }
        }

        public async void Execute(object parameter)
        {
            await ExecuteAsync(parameter);
        }

        protected virtual void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}