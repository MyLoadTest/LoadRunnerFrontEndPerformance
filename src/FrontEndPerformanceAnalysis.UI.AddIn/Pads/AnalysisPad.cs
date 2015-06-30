using System;
using System.Diagnostics;
using ICSharpCode.SharpDevelop.Gui;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Controls;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Pads
{
    public sealed class AnalysisPad : AbstractPadContent
    {
        #region Constants and Fields

        private readonly AnalysisControl _innerControl;

        #endregion

        #region Constructors

        public AnalysisPad()
        {
            _innerControl = new AnalysisControl();

            //// TODO: Remove this temporary code used for pre-testing
            _innerControl.ViewModel.SetTransactionNames("Sample".AsArray());
        }

        #endregion

        #region Public Properties

        public override object Control
        {
            [DebuggerStepThrough]
            get
            {
                return _innerControl;
            }
        }

        #endregion
    }
}