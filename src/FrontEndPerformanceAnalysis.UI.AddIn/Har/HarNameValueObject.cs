using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}. {Name,nq}: {Value,nq}")]
    internal abstract class HarNameValueObject
    {
        #region Public Properties

        [DataMember(Name = "name")]
        public string Name
        {
            get;
            set;
        }

        [DataMember(Name = "value")]
        public string Value
        {
            get;
            set;
        }

        #endregion
    }
}