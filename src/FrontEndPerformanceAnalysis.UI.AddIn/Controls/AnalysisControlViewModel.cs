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
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis.PageSpeed;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Commands;
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

        private static readonly ReadOnlyDictionary<PageSpeedStrategy, string> PageSpeedStrategyToToolParameterMap =
            new ReadOnlyDictionary<PageSpeedStrategy, string>(
                new Dictionary<PageSpeedStrategy, string>
                {
                    { PageSpeedStrategy.Desktop, "desktop" },
                    { PageSpeedStrategy.Mobile, "mobile" }
                });

        #endregion

        #region Constants and Fields: Dependency Properties

        private static readonly DependencyPropertyKey TransactionsPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.Transactions);

        private static readonly DependencyPropertyKey AnalysisTypesPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.AnalysisTypes);

        private static readonly DependencyPropertyKey ScoreUtilityTypesPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.ScoreUtilityTypes);

        private static readonly DependencyPropertyKey PageSpeedStrategiesPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.PageSpeedStrategies);

        private static readonly DependencyPropertyKey AnalysisResultPropertyKey =
            RegisterReadOnlyDependencyProperty(
                obj => obj.AnalysisResult,
                new PropertyMetadata(OverallAnalysisResult.Empty));

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

        private static readonly DependencyProperty SelectedScoreUtilityTypeProperty =
            RegisterDependencyProperty(
                obj => obj.SelectedScoreUtilityType,
                new PropertyMetadata(null, OnSelectedScoreUtilityTypeChanged, OnCoerceSelectedScoreUtilityType));

        private static readonly DependencyProperty SelectedPageSpeedStrategyProperty =
            RegisterDependencyProperty(
                obj => obj.SelectedPageSpeedStrategy,
                new PropertyMetadata(null, OnSelectedPageSpeedStrategyChanged, OnCoerceSelectedPageSpeedStrategy));

        private static readonly string PageSpeedExecutablePath =
            Path.GetFullPath(Settings.Default.PageSpeedExecutablePath);

        private static readonly TimeSpan PageSpeedRunTimeout = TimeSpan.FromMinutes(1);

        #endregion

        #region Fields: Dependency Properties

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

            AnalysisTypes = CreateCollectionViewForEnumeration<AnalysisType>();
            ScoreUtilityTypes = CreateCollectionViewForEnumeration<ScoreUtilityType>();
            PageSpeedStrategies = CreateCollectionViewForEnumeration<PageSpeedStrategy>();

            AnalyzeCommand = new AsyncRelayCommand(ExecuteAnalyzeCommand, CanExecuteAnalyzeCommand);

            Transactions.CurrentChanged += Transactions_CurrentChanged;
            AnalysisTypes.CurrentChanged += AnalysisTypes_CurrentChanged;
            ScoreUtilityTypes.CurrentChanged += ScoreUtilityTypes_CurrentChanged;
            PageSpeedStrategies.CurrentChanged += PageSpeedStrategies_CurrentChanged;

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
            CoerceValue(SelectedScoreUtilityTypeProperty);
            CoerceValue(SelectedPageSpeedStrategyProperty);
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

        public static DependencyProperty AnalysisTypesProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return AnalysisTypesPropertyKey.DependencyProperty;
            }
        }

        public static DependencyProperty ScoreUtilityTypesProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return ScoreUtilityTypesPropertyKey.DependencyProperty;
            }
        }

        public static DependencyProperty PageSpeedStrategiesProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return PageSpeedStrategiesPropertyKey.DependencyProperty;
            }
        }

        public static DependencyProperty AnalysisResultProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return AnalysisResultPropertyKey.DependencyProperty;
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
                return (ICollectionView)GetValue(AnalysisTypesProperty);
            }

            private set
            {
                SetValue(AnalysisTypesPropertyKey, value);
            }
        }

        [CanBeNull]
        public DescriptiveItem<AnalysisType> SelectedAnalysisType
        {
            get
            {
                return (DescriptiveItem<AnalysisType>)GetValue(SelectedAnalysisTypeProperty);
            }

            set
            {
                SetValue(SelectedAnalysisTypeProperty, value);
            }
        }

        [NotNull]
        public ICollectionView ScoreUtilityTypes
        {
            get
            {
                return (ICollectionView)GetValue(ScoreUtilityTypesProperty);
            }

            private set
            {
                SetValue(ScoreUtilityTypesPropertyKey, value);
            }
        }

        [CanBeNull]
        public ScoreUtilityType? SelectedScoreUtilityType
        {
            get
            {
                return (ScoreUtilityType?)GetValue(SelectedScoreUtilityTypeProperty);
            }

            set
            {
                SetValue(SelectedScoreUtilityTypeProperty, value);
            }
        }

        [NotNull]
        public ICollectionView PageSpeedStrategies
        {
            get
            {
                return (ICollectionView)GetValue(PageSpeedStrategiesProperty);
            }

            private set
            {
                SetValue(PageSpeedStrategiesPropertyKey, value);
            }
        }

        [CanBeNull]
        public PageSpeedStrategy? SelectedPageSpeedStrategy
        {
            get
            {
                return (PageSpeedStrategy?)GetValue(SelectedPageSpeedStrategyProperty);
            }

            set
            {
                SetValue(SelectedPageSpeedStrategyProperty, value);
            }
        }

        [NotNull]
        public OverallAnalysisResult AnalysisResult
        {
            get
            {
                return (OverallAnalysisResult)GetValue(AnalysisResultProperty);
            }

            private set
            {
                SetValue(AnalysisResultPropertyKey, value);
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

        [NotNull]
        public AsyncRelayCommand AnalyzeCommand
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

        private static void OnSelectedScoreUtilityTypeChanged(
            DependencyObject obj,
            DependencyPropertyChangedEventArgs args)
        {
            ((AnalysisControlViewModel)obj).OnSelectedScoreUtilityTypeChanged(args);
        }

        private static object OnCoerceSelectedScoreUtilityType(DependencyObject obj, object baseValue)
        {
            return ((AnalysisControlViewModel)obj).OnCoerceSelectedScoreUtilityType();
        }

        private static void OnSelectedPageSpeedStrategyChanged(
            DependencyObject obj,
            DependencyPropertyChangedEventArgs args)
        {
            ((AnalysisControlViewModel)obj).OnSelectedPageSpeedStrategyChanged(args);
        }

        private static object OnCoerceSelectedPageSpeedStrategy(DependencyObject obj, object baseValue)
        {
            return ((AnalysisControlViewModel)obj).OnCoerceSelectedPageSpeedStrategy();
        }

        [NotNull]
        private static CollectionView CreateCollectionViewForEnumeration<TEnum>() where TEnum : struct
        {
            return EnumFactotum
                .GetAllValues<TEnum>()
                .Select(value => DescriptiveItem.Create(value, ((Enum)(object)value).GetTranslation()))
                .ToArray()
                .ToCollectionView();
        }

        private static PageSpeedOutput RunTools(
            AnalysisType analysisType,
            ScoreUtilityType? selectedScoreUtilityType,
            PageSpeedStrategy? selectedPageSpeedStrategy,
            string inputFilePath,
            string outputFilePath)
        {
            if (analysisType != AnalysisType.ScoreAndRuleCompliance)
            {
                throw analysisType.CreateEnumValueNotImplementedException();
            }

            if (!selectedScoreUtilityType.HasValue)
            {
                throw new NotImplementedException();
            }

            if (selectedScoreUtilityType.Value != ScoreUtilityType.PageSpeed)
            {
                throw selectedScoreUtilityType.Value.CreateEnumValueNotImplementedException();
            }

            if (!selectedPageSpeedStrategy.HasValue)
            {
                throw new NotImplementedException();
            }

            var strategyParameter =
                PageSpeedStrategyToToolParameterMap.GetValueOrDefault(selectedPageSpeedStrategy.Value);

            if (strategyParameter.IsNullOrWhiteSpace())
            {
                throw new NotImplementedException(
                    $@"The strategy '{selectedPageSpeedStrategy.Value.GetQualifiedName()}' is not mapped.");
            }

            var arguments =
                $@"-input_file ""{inputFilePath}"" -output_file ""{outputFilePath
                    }"" -output_format formatted_json -strategy ""{strategyParameter}""";

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
            AnalysisTypes.MoveCurrentTo(args.NewValue);
        }

        private object OnCoerceSelectedAnalysisType()
        {
            return (DescriptiveItem<AnalysisType>)AnalysisTypes.CurrentItem;
        }

        private void OnSelectedScoreUtilityTypeChanged(DependencyPropertyChangedEventArgs args)
        {
            var newValue = (ScoreUtilityType?)args.NewValue;

            var item = newValue.HasValue ? DescriptiveItem.Create(newValue.Value) : null;
            ScoreUtilityTypes.MoveCurrentTo(item);
        }

        private object OnCoerceSelectedScoreUtilityType()
        {
            var currentItem = (DescriptiveItem<ScoreUtilityType>)ScoreUtilityTypes.CurrentItem;
            return currentItem?.Value;
        }

        private void OnSelectedPageSpeedStrategyChanged(DependencyPropertyChangedEventArgs args)
        {
            var newValue = (PageSpeedStrategy?)args.NewValue;

            var item = newValue.HasValue ? DescriptiveItem.Create(newValue.Value) : null;
            PageSpeedStrategies.MoveCurrentTo(item);
        }

        private object OnCoerceSelectedPageSpeedStrategy()
        {
            var currentItem = (DescriptiveItem<PageSpeedStrategy>)PageSpeedStrategies.CurrentItem;
            return currentItem?.Value;
        }

        private void Transactions_CurrentChanged(object sender, EventArgs eventArgs)
        {
            AnalyzeCommand.RaiseCanExecuteChanged();
            CoerceValue(SelectedTransactionProperty);
        }

        private void AnalysisTypes_CurrentChanged(object sender, EventArgs eventArgs)
        {
            CoerceValue(SelectedAnalysisTypeProperty);
            AnalyzeCommand.RaiseCanExecuteChanged();
        }

        private void ScoreUtilityTypes_CurrentChanged(object sender, EventArgs eventArgs)
        {
            CoerceValue(SelectedScoreUtilityTypeProperty);
            AnalyzeCommand.RaiseCanExecuteChanged();
        }

        private void PageSpeedStrategies_CurrentChanged(object sender, EventArgs eventArgs)
        {
            CoerceValue(SelectedPageSpeedStrategyProperty);
            AnalyzeCommand.RaiseCanExecuteChanged();
        }

        private bool CanExecuteAnalyzeCommand(object arg)
        {
            return SelectedAnalysisType != null && SelectedTransaction != null
                && !AnalyzeCommand.IsExecuting;
        }

        private void ExecuteAnalyzeCommand(object obj)
        {
            try
            {
                ExecuteAnalyzeCommandInternal();
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                var errorMessage = $"Error has occurred:{Environment.NewLine}{Environment.NewLine}{ex}";

                Dispatcher.Invoke(() => AnalysisErrorMessage = errorMessage, DispatcherPriority.Render);
                Dispatcher.Invoke(() => AnalysisResult = OverallAnalysisResult.Empty, DispatcherPriority.Render);
            }
        }

        private void ExecuteAnalyzeCommandInternal()
        {
            var selectedTransaction = Dispatcher.Invoke(() => SelectedTransaction, DispatcherPriority.Send);
            var selectedAnalysisType = Dispatcher.Invoke(() => SelectedAnalysisType, DispatcherPriority.Send);
            var selectedScoreUtilityType = Dispatcher.Invoke(() => SelectedScoreUtilityType, DispatcherPriority.Send);
            var selectedPageSpeedStrategy = Dispatcher.Invoke(
                () => SelectedPageSpeedStrategy,
                DispatcherPriority.Send);

            if (selectedTransaction == null || selectedAnalysisType == null)
            {
                return;
            }

            Dispatcher.Invoke(() => AnalysisErrorMessage = null, DispatcherPriority.Render);
            Dispatcher.Invoke(() => AnalysisResult = OverallAnalysisResult.Empty, DispatcherPriority.Render);

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

                pageSpeedOutput = RunTools(
                    selectedAnalysisType.Value,
                    selectedScoreUtilityType,
                    selectedPageSpeedStrategy,
                    inputFilePath,
                    outputFilePath);
            }

            Dispatcher.Invoke(
                () =>
                    AnalysisResult =
                        new OverallAnalysisResult(selectedTransaction, selectedAnalysisType, pageSpeedOutput),
                DispatcherPriority.Render);
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