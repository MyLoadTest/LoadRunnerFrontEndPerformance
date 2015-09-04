using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.ObjectMappings
{
    [DataContract]
    [DebuggerDisplay("{PlaceholderKey} = {LocalizedValue} (Type = {Type})")]
    internal sealed class FormattedStringArg
    {
        #region Public Properties

        [DataMember(Name = "localized_value")]
        public string LocalizedValue
        {
            get;
            set;
        }

        [DataMember(Name = "placeholder_key")]
        public string PlaceholderKey
        {
            get;
            set;
        }

        [DataMember(Name = "type")]
        public string Type
        {
            get;
            set;
        }

        [DataMember(Name = "string_value")]
        public string StringValue
        {
            get;
            set;
        }

        [DataMember(Name = "int_value")]
        public int? IntValue
        {
            get;
            set;
        }

        #endregion
    }
}