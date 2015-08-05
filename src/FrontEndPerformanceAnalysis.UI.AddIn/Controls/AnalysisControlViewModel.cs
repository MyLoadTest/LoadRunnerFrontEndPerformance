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
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.ObjectMappings;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Properties;
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

        private static readonly DependencyPropertyKey AnalysisResultPropertyKey =
            WpfHelper.For<AnalysisControlViewModel>.RegisterReadOnlyDependencyProperty(
                obj => obj.AnalysisResult);

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

            ScoreTypes = new CollectionView(scoreTypesInternal);

            CheckPerformanceCommand = new AsyncRelayCommand(
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

        public static DependencyProperty AnalysisResultProperty
        {
            [DebuggerNonUserCode]
            get
            {
                return AnalysisResultPropertyKey.DependencyProperty;
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
        public AnalysisType? SelectedAnalysisType
        {
            get
            {
                var currentItem = (ControlItem<AnalysisType>)ScoreTypes.CurrentItem;
                return currentItem == null ? default(AnalysisType?) : currentItem.Value;
            }

            set
            {
                var item = value.HasValue ? ControlItem.Create(value.Value) : null;
                ScoreTypes.MoveCurrentTo(item);
            }
        }

        [NotNull]
        public AsyncRelayCommand CheckPerformanceCommand
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

        private static PageSpeedOutput RunTools(
            AnalysisType analysisType,
            string inputFilePath,
            string outputFilePath)
        {
            if (analysisType != AnalysisType.Score)
            {
                throw analysisType.CreateEnumValueNotImplementedException();
            }

            var arguments = string.Format(
                CultureInfo.InvariantCulture,
                @"-input_file ""{0}"" -output_file ""{1}"" -output_format unformatted_json",
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
            return SelectedAnalysisType.HasValue && !SelectedTransactionName.IsNullOrWhiteSpace();
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

            Dispatcher.Invoke(() => AnalysisResult = "Analysing...", DispatcherPriority.Render);

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