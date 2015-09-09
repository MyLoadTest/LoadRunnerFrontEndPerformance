using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarRoot
    {
        #region Public Properties

        [DataMember(Name = "log", IsRequired = true)]
        public HarLog Log
        {
            get;
            set;
        }

        #endregion
    }
}