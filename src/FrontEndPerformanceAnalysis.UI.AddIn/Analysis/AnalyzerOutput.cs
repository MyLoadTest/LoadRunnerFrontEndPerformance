using System;
using System.Diagnostics;
using System.Linq;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis
{
    [DebuggerDisplay(
        "{GetType().Name,nq}. Transaction.Name = {Transaction?.Name}, AnalysisType.Value = {AnalysisType.Value}")]
    internal sealed class AnalyzerOutput
    {
        #region Constants and Fields

        public static readonly AnalyzerOutput Empty = new AnalyzerOutput();

        #endregion

        #region Constructors

        public AnalyzerOutput(
            [NotNull] TransactionInfo transaction,
            [NotNull] DescriptiveItem<AnalysisType> analysisType,
            [NotNull] Exception exception)
        {
            #region Argument Check

            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (analysisType == null)
            {
                throw new ArgumentNullException(nameof(analysisType));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            #endregion

            Transaction = transaction;
            AnalysisType = analysisType;
            Exception = exception;
        }

        public AnalyzerOutput(
            [NotNull] TransactionInfo transaction,
            [NotNull] DescriptiveItem<AnalysisType> analysisType,
            [NotNull] SpecificAnalysisResult specificResult)
        {
            #region Argument Check

            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            if (analysisType == null)
            {
                throw new ArgumentNullException(nameof(analysisType));
            }

            if (specificResult == null)
            {
                throw new ArgumentNullException(nameof(specificResult));
            }

            #endregion

            Transaction = transaction;
            AnalysisType = analysisType;
            SpecificResult = specificResult;
        }

        private AnalyzerOutput()
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        [CanBeNull]
        public TransactionInfo Transaction
        {
            get;
        }

        [CanBeNull]
        public DescriptiveItem<AnalysisType> AnalysisType
        {
            get;
        }

        [CanBeNull]
        public Exception Exception
        {
            get;
        }

        [CanBeNull]
        public SpecificAnalysisResult SpecificResult
        {
            get;
        }

        #endregion
    }
}