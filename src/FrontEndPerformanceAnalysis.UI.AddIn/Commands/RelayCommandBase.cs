using System;
using System.Linq;
using System.Windows.Input;
using Omnifactotum.Annotations;
using Task = System.Threading.Tasks.Task;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Commands
{
    internal abstract class RelayCommandBase
    {
        #region Constants and Fields

        [NotNull]
        private readonly Action<object> _execute;

        [CanBeNull]
        private readonly Func<object, bool> _canExecute;

        #endregion

        #region Constructors

        protected RelayCommandBase([NotNull] Action<object> execute, [CanBeNull] Func<object, bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        protected RelayCommandBase([NotNull] Action<object> execute)
            : this(execute, null)
        {
            // Nothing to do
        }

        #endregion

        #region Events

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        #endregion

        #region Public Methods

        public bool CanExecute(object parameter)
        {
            return !IsExecutingInternal && (_canExecute == null || _canExecute(parameter));
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Protected Properties

        protected bool IsExecutingInternal
        {
            get;
            private set;
        }

        #endregion

        #region Protected Methods

        protected async void ExecuteInternal(object parameter, bool async)
        {
            if (IsExecutingInternal)
            {
                return;
            }

            IsExecutingInternal = true;
            try
            {
                RaiseCanExecuteChanged();

                if (async)
                {
                    await Task.Run(() => _execute(parameter));
                }
                else
                {
                    _execute(parameter);
                }
            }
            finally
            {
                IsExecutingInternal = false;
                RaiseCanExecuteChanged();
            }
        }

        #endregion
    }
}