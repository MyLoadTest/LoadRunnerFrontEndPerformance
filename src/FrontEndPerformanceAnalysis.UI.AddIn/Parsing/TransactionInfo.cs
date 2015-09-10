using System;
using System.Diagnostics;
using System.Linq;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    [DebuggerDisplay("{GetType().Name,nq}: Name = {Name}, Index = {Index}")]
    internal sealed class TransactionInfo
    {
        #region Constructors

        public TransactionInfo(string name, long index)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    nameof(name));
            }

            if (index <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    @"The value must be positive.");
            }

            #endregion

            Name = name;
            Index = index;
            HarRoot = new HarRoot();
        }

        #endregion

        #region Public Properties

        [NotNull]
        public string Name
        {
            get;
            private set;
        }

        public long Index
        {
            get;
            private set;
        }

        [NotNull]
        public HarRoot HarRoot
        {
            get;
            private set;
        }

        #endregion
    }
}