using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ICSharpCode.Core;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Parsing;
using MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Properties;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.HostCommands
{
    /// <summary>
    ///     Represents the command that is executed automatically when IDE starts.
    /// </summary>
    public sealed class StartupCommand : AbstractMenuCommand
    {
        #region Public Methods

        /// <summary>
        ///     Invokes the command.
        /// </summary>
        public override void Run()
        {
            ResourceService.RegisterNeutralImages(Resources.ResourceManager);

            //// [vitalii.maklai] Temporary code for debugging the output log parser
            const string LogPath = @"D:\output.txt";
            if (File.Exists(LogPath))
            {
                TransactionInfo[] transactionInfos;
                using (var parser = new OutputLogParser(LogPath))
                {
                    transactionInfos = parser.Parse();
                }

                Trace.WriteLine(transactionInfos.Length);
            }
        }

        #endregion
    }
}