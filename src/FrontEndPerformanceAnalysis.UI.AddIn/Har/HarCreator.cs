using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}. Name = {Name}, Version = {Version}")]
    internal sealed class HarCreator
    {
        #region Public Properties

        [DataMember(Name = "name")]
        public string Name
        {
            get;
            set;
        }

        [DataMember(Name = "version")]
        public string Version
        {
            get;
            set;
        }

        #endregion
    }
}