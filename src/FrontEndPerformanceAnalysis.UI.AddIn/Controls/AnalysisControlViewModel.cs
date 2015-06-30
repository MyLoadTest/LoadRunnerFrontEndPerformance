using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
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
    }
}