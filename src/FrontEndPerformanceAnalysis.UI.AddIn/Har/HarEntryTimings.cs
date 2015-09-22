using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarEntryTimings
    {
        #region Public Properties

        [DataMember(Name = "blocked")]
        public decimal? Blocked
        {
            get;
            set;
        }

        [DataMember(Name = "dns")]
        public decimal? Dns
        {
            get;
            set;
        }

        [DataMember(Name = "connect")]
        public decimal? Connect
        {
            get;
            set;
        }

        [DataMember(Name = "send")]
        public decimal? Send
        {
            get;
            set;
        }

        [DataMember(Name = "wait")]
        public decimal? Wait
        {
            get;
            set;
        }

        [DataMember(Name = "receive")]
        public decimal? Receive
        {
            get;
            set;
        }

        [DataMember(Name = "ssl")]
        public decimal? Ssl
        {
            get;
            set;
        }

        #endregion
    }
}