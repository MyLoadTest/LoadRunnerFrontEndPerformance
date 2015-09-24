using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using HP.LR.VuGen.ServiceCore;
using HP.LR.VuGen.ServiceCore.Interfaces;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Commands;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.ObjectMappings.PageSpeed;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Properties;
using Omnifactotum;
using Omnifactotum.Annotations;
using Omnifactotum.Wpf;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Controls
{
    using static WpfFactotum.For<AnalysisControlViewModel>;

    internal sealed class AnalysisControlViewModel : DependencyObject
    {
        #region Constants and Fields

        private const string OutputLogFileName = @"output.txt";

        private static readonly DependencyPropertyKey TransactionsPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.Transactions);

        private static readonly DependencyPropertyKey ScoreTypesPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.AnalysisTypes);

        private static readonly DependencyPropertyKey PageSpeedResultPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.PageSpeedResult);

        private static readonly DependencyPropertyKey AnalysisErrorMessagePropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.AnalysisErrorMessage);

        private static readonly DependencyProperty SelectedTransactionProperty =
            RegisterDependencyProperty(
                obj => obj.SelectedTransaction,
                new PropertyMetadata(null, OnSelectedTransactionChanged, OnCoerceSelectedTransaction));

        private static readonly DependencyProperty SelectedAnalysisTypeProperty =
            RegisterDependencyProperty(
                obj => obj.SelectedAnalysisType,
                new PropertyMetadata(null, OnSelectedAnalysisTypeChanged, OnCoerceSelectedAnalysisType));

        private static readonly string PageSpeedExecutablePath =
            Path.GetFullPath(Settings.Default.PageSpeedExecutablePath);

        private static readonly TimeSpan PageSpeedRunTimeout = TimeSpan.FromMinutes(1);

        [NotNull]
        private readonly List<TransactionInfo> _transactionInfosInternal;

        [CanBeNull]
        private readonly IVuGenProjectService _projectService;

        #endregion

        #region Constructors

        public AnalysisControlViewModel()
        {
            _projectService = WpfFactotum.IsInDesignMode()
                ? null
                : VuGenServiceManager.GetService<IVuGenProjectService>().EnsureNotNull();

            _transactionInfosInternal = new List<TransactionInfo>();
            Transactions = new CollectionView(_transactionInfosInternal);

            var scoreTypesInternal = EnumFactotum
                .GetAllValues<AnalysisType>()
                .Select(value => ControlItem.Create(value, value.GetTranslation()))
                .ToArray();

            var analysisTypesCollectionViewSource = new CollectionViewSource
            {
                Source = scoreTypesInternal,
                SortDescriptions =
                {
                    new SortDescription(nameof(ControlItem<AnalysisType>.Text), ListSortDirection.Ascending)
                }
            };

            AnalysisTypes = analysisTypesCollectionViewSource.View.EnsureNotNull();

            CheckPerformanceCommand = new AsyncRelayCommand(
                ExecuteCheckPerformanceCommand,
                CanExecuteCheckPerformanceCommand);

            Transactions.CurrentChanged += Transactions_CurrentChanged;
            AnalysisTypes.CurrentChanged += AnalysisTypes_CurrentChanged;

            if (_projectService != null)
            {
                _projectService.LastReplayedRunChanged += (sender, args) => RefreshTransactions();
                _projectService.ActiveScriptChanged += (sender, args) => RefreshTransactions();
                _projectService.ScriptOpened += (sender, args) => RefreshTransactions();
                _projectService.ScriptClosed += (sender, args) => RefreshTransactions();
                _projectService.SolutionClosed += (sender, args) => RefreshTransactions();
            }

            RefreshTransactions();

            CoerceValue(SelectedTransactionProperty);
            CoerceValue(SelectedAnalysisTypeProperty);
        }

        #endregion

        #region Public Properties

        public static DependencyProperty TransactionsProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return TransactionsPropertyKey.DependencyProperty;
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

        public static DependencyProperty PageSpeedResultProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return PageSpeedResultPropertyKey.DependencyProperty;
            }
        }

        public static DependencyProperty AnalysisErrorMessageProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return AnalysisErrorMessagePropertyKey.DependencyProperty;
            }
        }

        [NotNull]
        public CollectionView Transactions
        {
            get
            {
                return (CollectionView)GetValue(TransactionsProperty);
            }

            private set
            {
                SetValue(TransactionsPropertyKey, value);
            }
        }

        [NotNull]
        public ICollectionView AnalysisTypes
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
        public PageSpeedOutput PageSpeedResult
        {
            get
            {
                return (PageSpeedOutput)GetValue(PageSpeedResultProperty);
            }

            private set
            {
                SetValue(PageSpeedResultPropertyKey, value);
            }
        }

        [CanBeNull]
        public string AnalysisErrorMessage
        {
            get
            {
                return (string)GetValue(AnalysisErrorMessageProperty);
            }

            private set
            {
                SetValue(AnalysisErrorMessagePropertyKey, value);
            }
        }

        [CanBeNull]
        public TransactionInfo SelectedTransaction
        {
            get
            {
                return (TransactionInfo)GetValue(SelectedTransactionProperty);
            }

            set
            {
                SetValue(SelectedTransactionProperty, value);
            }
        }

        [CanBeNull]
        public AnalysisType? SelectedAnalysisType
        {
            get
            {
                return (AnalysisType?)GetValue(SelectedAnalysisTypeProperty);
            }

            set
            {
                SetValue(SelectedAnalysisTypeProperty, value);
            }
        }

        [NotNull]
        public AsyncRelayCommand CheckPerformanceCommand
        {
            get;
        }

        #endregion

        #region Public Methods

        public void RefreshTransactions()
        {
            try
            {
                RefreshTransactionsInternal();
            }
            finally
            {
                Transactions.Refresh();
            }

            if (!Transactions.IsEmpty)
            {
                Transactions.MoveCurrentToFirst();
            }
        }

        #endregion

        #region Private Methods

        private static void OnSelectedTransactionChanged(
            DependencyObject obj,
            DependencyPropertyChangedEventArgs args)
        {
            ((AnalysisControlViewModel)obj).OnSelectedTransactionChanged(args);
        }

        private static object OnCoerceSelectedTransaction(DependencyObject obj, object baseValue)
        {
            return ((AnalysisControlViewModel)obj).OnCoerceSelectedTransaction();
        }

        private static void OnSelectedAnalysisTypeChanged(
            DependencyObject obj,
            DependencyPropertyChangedEventArgs args)
        {
            ((AnalysisControlViewModel)obj).OnSelectedAnalysisTypeChanged(args);
        }

        private static object OnCoerceSelectedAnalysisType(DependencyObject obj, object baseValue)
        {
            return ((AnalysisControlViewModel)obj).OnCoerceSelectedAnalysisType();
        }

        private static PageSpeedOutput RunTools(
            AnalysisType analysisType,
            string inputFilePath,
            string outputFilePath)
        {
            if (analysisType != AnalysisType.ScoreByPageSpeed)
            {
                throw analysisType.CreateEnumValueNotImplementedException();
            }

            var arguments =
                $@"-input_file ""{inputFilePath}"" -output_file ""{outputFilePath
                    }"" -output_format formatted_json -strategy desktop";

            var startInfo = new ProcessStartInfo(PageSpeedExecutablePath, arguments)
            {
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException(
                        $@"Unable to run the required tool ""{PageSpeedExecutablePath}"".");
                }

                var waitResult = process.WaitForExit((int)PageSpeedRunTimeout.TotalMilliseconds);
                if (!waitResult)
                {
                    process.KillNoThrow();

                    throw new InvalidOperationException(
                        $@"The tool ""{PageSpeedExecutablePath}"" has not exited after {PageSpeedRunTimeout}.");
                }

                var exitCode = process.ExitCode;
                if (exitCode != 0)
                {
                    throw new InvalidOperationException(
                        $@"The tool ""{PageSpeedExecutablePath}"" has exited with the code {exitCode}.");
                }
            }

            var pageSpeedOutput = PageSpeedOutput.DeserializeFromFile(outputFilePath);
            return pageSpeedOutput;
        }

        private void OnSelectedTransactionChanged(DependencyPropertyChangedEventArgs args)
        {
            Transactions.MoveCurrentTo(args.NewValue);
        }

        private object OnCoerceSelectedTransaction()
        {
            return (TransactionInfo)Transactions.CurrentItem;
        }

        private void OnSelectedAnalysisTypeChanged(DependencyPropertyChangedEventArgs args)
        {
            var newValue = (AnalysisType?)args.NewValue;

            var item = newValue.HasValue ? ControlItem.Create(newValue.Value) : null;
            AnalysisTypes.MoveCurrentTo(item);
        }

        private object OnCoerceSelectedAnalysisType()
        {
            var currentItem = (ControlItem<AnalysisType>)AnalysisTypes.CurrentItem;
            return currentItem?.Value;
        }

        private void Transactions_CurrentChanged(object sender, EventArgs eventArgs)
        {
            CheckPerformanceCommand.RaiseCanExecuteChanged();
            CoerceValue(SelectedTransactionProperty);
        }

        private void AnalysisTypes_CurrentChanged(object sender, EventArgs eventArgs)
        {
            CheckPerformanceCommand.RaiseCanExecuteChanged();
            CoerceValue(SelectedAnalysisTypeProperty);
        }

        private bool CanExecuteCheckPerformanceCommand(object arg)
        {
            return SelectedAnalysisType.HasValue && SelectedTransaction != null
                && !CheckPerformanceCommand.IsExecuting;
        }

        private void ExecuteCheckPerformanceCommand(object obj)
        {
            try
            {
                ExecuteCheckPerformanceCommandInternal();
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                var errorMessage = $"Error has occurred:{Environment.NewLine}{Environment.NewLine}{ex}";

                Dispatcher.Invoke(() => AnalysisErrorMessage = errorMessage, DispatcherPriority.Render);
                Dispatcher.Invoke(() => PageSpeedResult = null, DispatcherPriority.Render);
            }
        }

        private void ExecuteCheckPerformanceCommandInternal()
        {
            var selectedScoreType = Dispatcher.Invoke(() => SelectedAnalysisType, DispatcherPriority.Send);
            var selectedTransaction = Dispatcher.Invoke(() => SelectedTransaction, DispatcherPriority.Send);

            if (!selectedScoreType.HasValue || selectedTransaction == null)
            {
                return;
            }

            Dispatcher.Invoke(() => AnalysisErrorMessage = null, DispatcherPriority.Render);
            Dispatcher.Invoke(() => PageSpeedResult = null, DispatcherPriority.Render);

            PageSpeedOutput pageSpeedOutput;
            using (var tempFileCollection = new TempFileCollection(Path.GetTempPath(), false))
            {
                var fileName = Path.ChangeExtension(Path.GetRandomFileName(), ".har");

                var inputFilePath = Path.Combine(Path.GetTempPath(), fileName);
                tempFileCollection.AddFile(inputFilePath, false);

                var outputFilePath = inputFilePath + ".json";
                tempFileCollection.AddFile(outputFilePath, false);

                using (var stream = File.Create(inputFilePath))
                {
                    selectedTransaction.HarRoot.Serialize(stream);
                }

                pageSpeedOutput = RunTools(selectedScoreType.Value, inputFilePath, outputFilePath);
            }

            Dispatcher.Invoke(() => PageSpeedResult = pageSpeedOutput, DispatcherPriority.Render);
        }

        private void RefreshTransactionsInternal()
        {
            _transactionInfosInternal.Clear();

            var script = _projectService?.GetActiveScript();
            if (script == null)
            {
                return;
            }

            var scriptFilePath = script.FileName;
            if (scriptFilePath.IsNullOrWhiteSpace())
            {
                return;
            }

            var scriptDirectory = Path.GetDirectoryName(scriptFilePath);
            if (scriptDirectory.IsNullOrWhiteSpace())
            {
                return;
            }

            var outputLogFilePath = Path.Combine(scriptDirectory.EnsureNotNull(), OutputLogFileName);
            if (!File.Exists(outputLogFilePath))
            {
                return;
            }

            TransactionInfo[] transactionInfos;
            using (var parser = new OutputLogParser(outputLogFilePath))
            {
                transactionInfos = parser.Parse();
            }

            transactionInfos.DoForEach(_transactionInfosInternal.Add);
        }

        #endregion
    }
}