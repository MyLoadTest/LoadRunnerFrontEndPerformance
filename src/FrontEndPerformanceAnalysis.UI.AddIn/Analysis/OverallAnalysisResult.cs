using System;
using System.Diagnostics;
using System.Linq;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis
{
    [DebuggerDisplay(
        "{GetType().Name,nq}. AnalysisType.Value = {AnalysisType.Value}, Transaction.Name = {Transaction?.Name}")]
    internal sealed class OverallAnalysisResult
    {
        #region Constants and Fields

        public static readonly OverallAnalysisResult Empty = new OverallAnalysisResult();

        #endregion

        #region Constructors

        public OverallAnalysisResult(
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

        private OverallAnalysisResult()
        {
            // Nothing to do
        }

        #endregion

        #region Public Properties

        [CanBeNull]
        public DescriptiveItem<AnalysisType> AnalysisType
        {
            get;
        }

        [CanBeNull]
        public TransactionInfo Transaction
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