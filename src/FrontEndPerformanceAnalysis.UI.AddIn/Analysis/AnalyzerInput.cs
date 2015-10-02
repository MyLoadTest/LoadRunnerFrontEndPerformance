using System;
using System.Collections.Generic;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis
{
    internal sealed class AnalyzerInput
    {
        #region Constructors

        public AnalyzerInput(
            [NotNull] TransactionInfo transaction,
            [NotNull] DescriptiveItem<AnalysisType> analysisType)
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

            #endregion

            Transaction = transaction;
            AnalysisType = analysisType;
        }

        #endregion

        #region Public Properties

        public TransactionInfo Transaction
        {
            get;
            private set;
        }

        public DescriptiveItem<AnalysisType> AnalysisType
        {
            get;
            private set;
        }

        public ScoreUtilityType? ScoreUtilityType
        {
            get;
            set;
        }

        public PageSpeedStrategy? PageSpeedStrategy
        {
            get;
            set;
        }

        #endregion
    }
}