using System;
using System.Linq;
using System.Windows.Input;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Commands
{
    internal sealed class AsyncRelayCommand : RelayCommandBase, ICommand
    {
        #region Constructors

        public AsyncRelayCommand([NotNull] Action<object> execute, [CanBeNull] Func<object, bool> canExecute)
            : base(execute, canExecute)
        {
            // Nothing to do
        }

        public AsyncRelayCommand([NotNull] Action<object> execute)
            : this(execute, null)
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        public bool IsExecuting
        {
            get
            {
                return IsExecutingInternal;
            }
        }

        #endregion

        #region ICommand Members

        public void Execute(object parameter)
        {
            ExecuteInternal(parameter, true);
        }

        #endregion
    }
}