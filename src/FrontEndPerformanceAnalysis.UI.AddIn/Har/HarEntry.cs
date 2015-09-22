using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    [DebuggerDisplay(
        "{GetType().Name,nq}. PageRef = {PageRef}, ConnectionId = {ConnectionId}"
            + ", ServerIPAddress = {ServerIPAddress}, Time = {Time}, Request.Url = {Request?.Url}")]
    internal sealed class HarEntry : HarTimestampedObject
    {
        #region Public Properties

        [DataMember(Name = "pageref")]
        public string PageRef
        {
            get;
            set;
        }

        [DataMember(Name = "time")]
        public decimal? Time
        {
            get;
            set;
        }

        [DataMember(Name = "request")]
        public HarRequest Request
        {
            get;
            set;
        }

        [DataMember(Name = "response")]
        public HarResponse Response
        {
            get;
            set;
        }

        [DataMember(Name = "cache")]
        public HarCache Cache
        {
            get;
            set;
        }

        [DataMember(Name = "timings")]
        public HarEntryTimings Timings
        {
            get;
            set;
        }

        [DataMember(Name = "serverIPAddress")]
        public string ServerIPAddress
        {
            get;
            set;
        }

        [DataMember(Name = "connection")]
        public string ConnectionId
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public void ComputeTime()
        {
            if (Timings == null)
            {
                Time = HarConstants.NotApplicableTiming;
                return;
            }

            // The Ssl timing is supposed to be included in the Connect timing (for compatibility with HAR 1.1)
            var all = new[]
            {
                Timings.Blocked,
                Timings.Connect,
                Timings.Dns,
                Timings.Receive,
                Timings.Send,
                Timings.Wait
            };

            Time = all.Select(value => value.GetValueOrDefault()).Where(value => value > 0).Sum();
        }

        #endregion
    }
}