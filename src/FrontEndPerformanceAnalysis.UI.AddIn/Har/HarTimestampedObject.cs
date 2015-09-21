using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal abstract class HarTimestampedObject
    {
        #region Constants and Fields

        private string _startedDateTimeAsString;
        private DateTimeOffset? _startedDateTime;

        #endregion

        #region Public Properties

        [DataMember(Name = "startedDateTime")]
        public string StartedDateTimeAsString
        {
            [DebuggerStepThrough]
            get
            {
                return _startedDateTimeAsString;
            }

            set
            {
                if (_startedDateTimeAsString == value)
                {
                    return;
                }

                _startedDateTime = value.ParseIso8601DateTime();
                _startedDateTimeAsString = value;
            }
        }

        public DateTimeOffset? StartedDateTime
        {
            [DebuggerStepThrough]
            get
            {
                return _startedDateTime;
            }

            set
            {
                if (_startedDateTime == value)
                {
                    return;
                }

                _startedDateTimeAsString = value?.ToUtcIso8601String();
                _startedDateTime = value;
            }
        }

        #endregion
    }
}