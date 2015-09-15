using System;
using System.Diagnostics;
using System.Linq;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing
{
    [DebuggerDisplay("{GetType().Name,nq}: Name = {Name}")]
    internal sealed class TransactionInfo
    {
        #region Constructors

        public TransactionInfo(string name)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    nameof(name));
            }

            #endregion

            Name = name;
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

        [NotNull]
        public HarRoot HarRoot
        {
            get;
            private set;
        }

        #endregion
    }
}