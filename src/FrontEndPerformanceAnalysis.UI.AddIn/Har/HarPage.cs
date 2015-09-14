using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    [DebuggerDisplay("{GetType().Name,nq}. Id = {Id}, Title = {Title}")]
    internal sealed class HarPage
    {
        #region Public Properties

        [DataMember(Name = "startedDateTime")]
        public string StartedDateTimeAsString
        {
            get;
            set;
        }

        [DataMember(Name = "id")]
        public string Id
        {
            get;
            set;
        }

        [DataMember(Name = "title")]
        public string Title
        {
            get;
            set;
        }

        [DataMember(Name = "pageTimings")]
        public HarPageTimings PageTimings
        {
            get;
            set;
        }

        #endregion
    }
}