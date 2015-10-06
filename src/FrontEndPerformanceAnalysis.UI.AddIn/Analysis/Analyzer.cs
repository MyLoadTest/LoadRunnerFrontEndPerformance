using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis.PageSpeed;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Properties;
using Omnifactotum;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis
{
    internal sealed class Analyzer
    {
        #region Constants and Fields

        private static readonly ReadOnlyDictionary<PageSpeedStrategy, string> PageSpeedStrategyToToolParameterMap =
            new ReadOnlyDictionary<PageSpeedStrategy, string>(
                new Dictionary<PageSpeedStrategy, string>
                {
                    { PageSpeedStrategy.Desktop, "desktop" },
                    { PageSpeedStrategy.Mobile, "mobile" }
                });

        private static readonly string PageSpeedExecutablePath =
            Path.GetFullPath(Settings.Default.PageSpeedExecutablePath);

        private static readonly TimeSpan PageSpeedRunTimeout = TimeSpan.FromMinutes(1);

        #endregion

        #region Public Methods

        public AnalyzerOutput Analyze([NotNull] AnalyzerInput input)
        {
            #region Argument Check

            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            #endregion

            var transaction = input.Transaction.EnsureNotNull();
            var analysisType = input.AnalysisType.EnsureNotNull();
            var scoreUtilityType = input.ScoreUtilityType;
            var pageSpeedStrategy = input.PageSpeedStrategy;

            try
            {
                return AnalyzeInternal(transaction, analysisType, scoreUtilityType, pageSpeedStrategy);
            }
            catch (Exception ex)
            {
                if (ex.IsFatal())
                {
                    throw;
                }

                return new AnalyzerOutput(transaction, analysisType, ex);
            }
        }

        #endregion

        #region Private Methods

        private static SpecificAnalysisResult AnalyzeScore(
            ScoreUtilityType? scoreUtilityType,
            PageSpeedStrategy? pageSpeedStrategy,
            string inputFilePath,
            string outputFilePath)
        {
            if (!scoreUtilityType.HasValue)
            {
                throw new ArgumentNullException(nameof(scoreUtilityType));
            }

            if (scoreUtilityType.Value != ScoreUtilityType.PageSpeed)
            {
                throw scoreUtilityType.Value.CreateEnumValueNotImplementedException();
            }

            if (!pageSpeedStrategy.HasValue)
            {
                throw new ArgumentNullException(nameof(pageSpeedStrategy));
            }

            var strategyParameter =
                PageSpeedStrategyToToolParameterMap.GetValueOrDefault(pageSpeedStrategy.Value);

            if (strategyParameter.IsNullOrWhiteSpace())
            {
                throw new NotSupportedException(
                    $@"The strategy '{pageSpeedStrategy.Value.GetQualifiedName()}' is not mapped.");
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

        private static SpecificAnalysisResult AnalyzeHar(
            [NotNull] DescriptiveItem<AnalysisType> analysisType,
            ScoreUtilityType? scoreUtilityType,
            PageSpeedStrategy? pageSpeedStrategy,
            string inputFilePath,
            string outputFilePath)
        {
            switch (analysisType.Value)
            {
                case AnalysisType.ScoreAndRuleCompliance:
                    return AnalyzeScore(scoreUtilityType, pageSpeedStrategy, inputFilePath, outputFilePath);

                default:
                    throw analysisType.Value.CreateEnumValueNotImplementedException();
            }
        }

        private static AnalyzerOutput AnalyzeInternal(
            TransactionInfo transaction,
            DescriptiveItem<AnalysisType> analysisType,
            ScoreUtilityType? scoreUtilityType,
            PageSpeedStrategy? pageSpeedStrategy)
        {
            using (var tempFileCollection = new TempFileCollection(Path.GetTempPath(), false))
            {
                var fileName = Path.ChangeExtension(Path.GetRandomFileName(), ".har");

                var inputFilePath = Path.Combine(Path.GetTempPath(), fileName);
                tempFileCollection.AddFile(inputFilePath, false);

                var outputFilePath = inputFilePath + ".json";
                tempFileCollection.AddFile(outputFilePath, false);

                using (var stream = File.Create(inputFilePath))
                {
                    transaction.HarRoot.Serialize(stream);
                }

                var analysisResult = AnalyzeHar(
                    analysisType,
                    scoreUtilityType,
                    pageSpeedStrategy,
                    inputFilePath,
                    outputFilePath);

                return new AnalyzerOutput(transaction, analysisType, analysisResult);
            }
        }

        #endregion
    }
}