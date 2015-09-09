using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Omnifactotum.Annotations;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Har
{
    [DataContract]
    internal sealed class HarRoot
    {
        #region Constants and Fields

        private static readonly DataContractJsonSerializer Serializer =
            new DataContractJsonSerializer(typeof(HarRoot));

        #endregion

        #region Public Properties

        [DataMember(Name = "log", IsRequired = true)]
        public HarLog Log
        {
            get;
            set;
        }

        #endregion

        #region Public Methods

        public void Serialize([NotNull] Stream stream)
        {
            #region Argument Check

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanWrite)
            {
                throw new ArgumentException(@"The stream is not writable.", nameof(stream));
            }

            #endregion

            Serializer.WriteObject(stream, this);
        }

        #endregion
    }
}