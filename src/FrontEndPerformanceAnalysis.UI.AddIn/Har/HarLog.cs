using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarLog
    {
        #region Public Properties

        [DataMember(Name = "version")]
        public string Version
        {
            get;
            set;
        }

        [DataMember(Name = "creator")]
        public HarCreator Creator
        {
            get;
            set;
        }

        [DataMember(Name = "pages")]
        public HarPage[] Pages
        {
            get;
            set;
        }

        [DataMember(Name = "entries")]
        public HarEntry[] Entries
        {
            get;
            set;
        }

        #endregion
    }
}