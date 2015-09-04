using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Commands;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.ObjectMappings.PageSpeed;
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

        private static readonly DependencyPropertyKey TransactionNamesPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.TransactionNames);

        private static readonly DependencyPropertyKey ScoreTypesPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.AnalysisTypes);

        private static readonly DependencyPropertyKey AnalysisResultPropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.AnalysisResult);

        private static readonly DependencyPropertyKey AnalysisErrorMessagePropertyKey =
            RegisterReadOnlyDependencyProperty(obj => obj.AnalysisErrorMessage);

        private static readonly DependencyProperty SelectedTransactionNameProperty =
            RegisterDependencyProperty(
                obj => obj.SelectedTransactionName,
                new PropertyMetadata(null, OnSelectedTransactionNameChanged, OnCoerceSelectedTransactionName));

        private static readonly DependencyProperty SelectedAnalysisTypeProperty =
            RegisterDependencyProperty(
                obj => obj.SelectedAnalysisType,
                new PropertyMetadata(null, OnSelectedAnalysisTypeChanged, OnCoerceSelectedAnalysisType));

        private static readonly string PageSpeedExecutablePath =
            Path.GetFullPath(Settings.Default.PageSpeedExecutablePath);

        private static readonly TimeSpan PageSpeedRunTimeout = TimeSpan.FromMinutes(1);

        private readonly List<string> _transactionNamesInternal;

        #endregion

        #region Constructors

        public AnalysisControlViewModel()
        {
            _transactionNamesInternal = new List<string>();
            TransactionNames = new CollectionView(_transactionNamesInternal);

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

            TransactionNames.CurrentChanged += TransactionNames_CurrentChanged;
            AnalysisTypes.CurrentChanged += AnalysisTypes_CurrentChanged;

            CoerceValue(SelectedTransactionNameProperty);
            CoerceValue(SelectedAnalysisTypeProperty);
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

        public string AnalysisResult
        {
            get
            {
                return (string)GetValue(AnalysisResultProperty);
            }

            private set
            {
                SetValue(AnalysisResultPropertyKey, value);
            }
        }

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
        public string SelectedTransactionName
        {
            get
            {
                return (string)GetValue(SelectedTransactionNameProperty);
            }

            set
            {
                SetValue(SelectedTransactionNameProperty, value);
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

        public void SetTransactionNames([NotNull] ICollection<string> transactionNames)
        {
            #region Argument Check

            if (transactionNames == null)
            {
                throw new ArgumentNullException(nameof(transactionNames));
            }

            if (transactionNames.Any(item => item == null))
            {
                throw new ArgumentException(@"The collection contains a null element.", nameof(transactionNames));
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

        private static void OnSelectedTransactionNameChanged(
            DependencyObject obj,
            DependencyPropertyChangedEventArgs args)
        {
            ((AnalysisControlViewModel)obj).OnSelectedTransactionNameChanged(args);
        }

        private static object OnCoerceSelectedTransactionName(DependencyObject obj, object baseValue)
        {
            return ((AnalysisControlViewModel)obj).OnCoerceSelectedTransactionName();
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

            var arguments = string.Format(
                CultureInfo.InvariantCulture,
                @"-input_file ""{0}"" -output_file ""{1}"" -output_format formatted_json -strategy desktop",
                inputFilePath,
                outputFilePath);

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
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"Unable to run the required tool ""{0}"".",
                            PageSpeedExecutablePath));
                }

                var waitResult = process.WaitForExit((int)PageSpeedRunTimeout.TotalMilliseconds);
                if (!waitResult)
                {
                    process.KillNoThrow();

                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            @"The tool ""{0}"" has not exited after {1}.",
                            PageSpeedExecutablePath,
                            PageSpeedRunTimeout));
                }
            }

            var pageSpeedOutput = PageSpeedOutput.DeserializeFromFile(outputFilePath);
            return pageSpeedOutput;
        }

        private void OnSelectedTransactionNameChanged(DependencyPropertyChangedEventArgs args)
        {
            TransactionNames.MoveCurrentTo(args.NewValue);
        }

        private object OnCoerceSelectedTransactionName()
        {
            return (string)TransactionNames.CurrentItem;
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

        private void TransactionNames_CurrentChanged(object sender, EventArgs eventArgs)
        {
            CheckPerformanceCommand.RaiseCanExecuteChanged();
            CoerceValue(SelectedTransactionNameProperty);
        }

        private void AnalysisTypes_CurrentChanged(object sender, EventArgs eventArgs)
        {
            CheckPerformanceCommand.RaiseCanExecuteChanged();
            CoerceValue(SelectedAnalysisTypeProperty);
        }

        private bool CanExecuteCheckPerformanceCommand(object arg)
        {
            return SelectedAnalysisType.HasValue && !SelectedTransactionName.IsNullOrWhiteSpace()
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

                var errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "Error has occurred:{0}{0}{1}",
                    Environment.NewLine,
                    ex);

                Dispatcher.Invoke(() => AnalysisErrorMessage = errorMessage, DispatcherPriority.Render);
                Dispatcher.Invoke(() => AnalysisResult = errorMessage, DispatcherPriority.Render);
            }
        }

        private void ExecuteCheckPerformanceCommandInternal()
        {
            var selectedScoreType = Dispatcher.Invoke(() => SelectedAnalysisType, DispatcherPriority.Send);
            var selectedTransactionName = Dispatcher.Invoke(() => SelectedTransactionName, DispatcherPriority.Send);

            if (!selectedScoreType.HasValue || selectedTransactionName == null)
            {
                return;
            }

            Dispatcher.Invoke(() => AnalysisErrorMessage = null, DispatcherPriority.Render);
            Dispatcher.Invoke(() => AnalysisResult = "Analyzing...", DispatcherPriority.Render);

            PageSpeedOutput pageSpeedOutput;
            using (var tempFileCollection = new TempFileCollection(Path.GetTempPath(), false))
            {
                var fileName = Path.ChangeExtension(Path.GetRandomFileName(), ".har");

                var inputFilePath = Path.Combine(Path.GetTempPath(), fileName);
                tempFileCollection.AddFile(inputFilePath, false);

                var outputFilePath = inputFilePath + ".json";
                tempFileCollection.AddFile(outputFilePath, false);

                var fileData = LocalHelper.GetTestHarFile(selectedTransactionName);
                File.WriteAllBytes(inputFilePath, fileData);

                pageSpeedOutput = RunTools(selectedScoreType.Value, inputFilePath, outputFilePath);
            }

            var analysisResult = string.Format(CultureInfo.InvariantCulture, "Score = {0}", pageSpeedOutput.Score);
            Dispatcher.Invoke(() => AnalysisResult = analysisResult, DispatcherPriority.Render);
        }

        #endregion
    }
}