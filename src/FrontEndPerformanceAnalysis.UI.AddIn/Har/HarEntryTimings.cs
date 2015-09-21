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
        public long? Blocked
        {
            get;
            set;
        }

        [DataMember(Name = "dns")]
        public long? Dns
        {
            get;
            set;
        }

        [DataMember(Name = "connect")]
        public long? Connect
        {
            get;
            set;
        }

        [DataMember(Name = "send")]
        public long? Send
        {
            get;
            set;
        }

        [DataMember(Name = "wait")]
        public long? Wait
        {
            get;
            set;
        }

        [DataMember(Name = "receive")]
        public long? Receive
        {
            get;
            set;
        }

        [DataMember(Name = "ssl")]
        public long? Ssl
        {
            get;
            set;
        }

        #endregion
    }
}