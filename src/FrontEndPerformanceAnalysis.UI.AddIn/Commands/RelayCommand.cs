using System;
using System.Linq;
using System.Windows.Input;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Commands
{
    internal sealed class RelayCommand : ICommand
    {
        #region Constants and Fields

        [NotNull]
        private readonly Action<object> _execute;

        [CanBeNull]
        private readonly Func<object, bool> _canExecute;

        #endregion

        #region Constructors

        public RelayCommand([NotNull] Action<object> execute, [CanBeNull] Func<object, bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand([NotNull] Action<object> execute)
            : this(execute, null)
        {
            // Nothing to do
        }

        #endregion

        #region ICommand Members

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        #endregion

        #region Public Methods

        public void RaiseCanExecuteChanged()
        {
            RaiseCanExecuteChangedInternal();
        }

        #endregion

        #region Private Methods

        private void RaiseCanExecuteChangedInternal()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}