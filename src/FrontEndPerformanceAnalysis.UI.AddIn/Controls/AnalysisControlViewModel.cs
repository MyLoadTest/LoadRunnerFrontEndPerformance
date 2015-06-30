using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Commands;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Controls
{
    internal sealed class AnalysisControlViewModel : DependencyObject
    {
        #region Constants and Fields

        private static readonly DependencyPropertyKey TransactionNamesPropertyKey =
            WpfHelper.For<AnalysisControlViewModel>.RegisterReadOnlyDependencyProperty(
                obj => obj.TransactionNames);

        private static readonly DependencyPropertyKey ScoreTypesPropertyKey =
            WpfHelper.For<AnalysisControlViewModel>.RegisterReadOnlyDependencyProperty(
                obj => obj.ScoreTypes);

        private readonly List<string> _transactionNamesInternal;

        #endregion

        #region Constructors

        public AnalysisControlViewModel()
        {
            _transactionNamesInternal = new List<string>();
            TransactionNames = new CollectionView(_transactionNamesInternal);

            var scoreTypesInternal = EnumFactotum
                .GetAllValues<ScoreType>()
                .Select(value => ControlItem.Create(value, value.GetTranslation()))
                .ToArray();

            ScoreTypes = new CollectionView(scoreTypesInternal);

            CheckPerformanceCommand = new RelayCommand(
                ExecuteCheckPerformanceCommand,
                CanExecuteCheckPerformanceCommand);

            TransactionNames.CurrentChanged += TransactionNames_CurrentChanged;
            ScoreTypes.CurrentChanged += ScoreTypes_CurrentChanged;
        }

        #endregion

        #region Public Properties

        public static DependencyProperty TransactionNamesProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return TransactionNamesPropertyKey.DependencyProperty;
            }
        }

        public static DependencyProperty ScoreTypesProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return ScoreTypesPropertyKey.DependencyProperty;
            }
        }

        [NotNull]
        public ICollectionView TransactionNames
        {
            get
            {
                return (ICollectionView)GetValue(TransactionNamesProperty);
            }

            private set
            {
                SetValue(TransactionNamesPropertyKey, value);
            }
        }

        [NotNull]
        public ICollectionView ScoreTypes
        {
            get
            {
                return (ICollectionView)GetValue(ScoreTypesProperty);
            }

            private set
            {
                SetValue(ScoreTypesPropertyKey, value);
            }
        }

        [CanBeNull]
        public string SelectedTransactionName
        {
            get
            {
                return (string)TransactionNames.CurrentItem;
            }

            set
            {
                TransactionNames.MoveCurrentTo(value);
            }
        }

        [CanBeNull]
        public ScoreType? SelectedScoreType
        {
            get
            {
                var currentItem = (ControlItem<ScoreType>)ScoreTypes.CurrentItem;
                return currentItem == null ? default(ScoreType?) : currentItem.Value;
            }

            set
            {
                var item = value.HasValue ? ControlItem.Create(value.Value) : null;
                ScoreTypes.MoveCurrentTo(item);
            }
        }

        [NotNull]
        public RelayCommand CheckPerformanceCommand
        {
            get;
            private set;
        }

        #endregion

        #region Public Methods

        public void SetTransactionNames([NotNull] ICollection<string> transactionNames)
        {
            #region Argument Check

            if (transactionNames == null)
            {
                throw new ArgumentNullException("transactionNames");
            }

            if (transactionNames.Any(item => item == null))
            {
                throw new ArgumentException(@"The collection contains a null element.", "transactionNames");
            }

            #endregion

            using (TransactionNames.DeferRefresh())
            {
                _transactionNamesInternal.Clear();
                transactionNames.DoForEach(_transactionNamesInternal.Add);
            }
        }

        #endregion

        #region Private Methods

        private void TransactionNames_CurrentChanged(object sender, EventArgs eventArgs)
        {
            CheckPerformanceCommand.RaiseCanExecuteChanged();
        }

        private void ScoreTypes_CurrentChanged(object sender, EventArgs eventArgs)
        {
            CheckPerformanceCommand.RaiseCanExecuteChanged();
        }

        private bool CanExecuteCheckPerformanceCommand(object arg)
        {
            return SelectedScoreType.HasValue && !SelectedTransactionName.IsNullOrWhiteSpace();
        }

        private void ExecuteCheckPerformanceCommand(object obj)
        {
            if (!SelectedScoreType.HasValue || SelectedTransactionName.IsNullOrWhiteSpace())
            {
                return;
            }

            var fileName = Path.ChangeExtension(Path.GetRandomFileName(), ".har");
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            var fileData = LocalHelper.GetTestHarFile(SelectedTransactionName);
            File.WriteAllBytes(filePath, fileData);

            RunTools(filePath);
        }

        //// ReSharper disable once MemberCanBeMadeStatic.Local - TEMP
        //// ReSharper disable once UnusedParameter.Local - TEMP
        private void RunTools(string filePath)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}