using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Analysis.PageSpeed
{
    [DataContract]
    [DebuggerDisplay("{Major}.{Minor} (OfficialRelease = {OfficialRelease})")]
    internal sealed class VersionInfo
    {
        #region Public Properties

        [DataMember(Name = "major")]
        public int Major
        {
            get;
            set;
        }

        [DataMember(Name = "minor")]
        public int Minor
        {
            get;
            set;
        }

        [DataMember(Name = "official_release")]
        public bool OfficialRelease
        {
            get;
            set;
        }

        #endregion
    }
}