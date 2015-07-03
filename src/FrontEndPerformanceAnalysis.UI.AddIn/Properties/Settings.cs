using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MyLoadTest.LoadRunnerFrontEndPerformanceAnalysis.UI.AddIn.Properties
{
    internal sealed partial class Settings
    {
        #region Constants and Fields

        private const string DefaultPageSpeedExecutableRelativePath = @"lib\PageSpeed\pagespeed_bin.exe";

        #endregion

        #region Constructors

        public Settings()
        {
            var dllConfigPath = Assembly.GetExecutingAssembly().GetLocalPath() + ".config";

            var configuration =
                ConfigurationManager.OpenMappedExeConfiguration(
                    new ExeConfigurationFileMap { ExeConfigFilename = dllConfigPath },
                    ConfigurationUserLevel.None);

            var setting = configuration.AppSettings.Settings["PageSpeedExecutableRelativePath"];
            var pageSpeedExecutableRelativePath = setting == null
                || setting.Value.IsNullOrWhiteSpace()
                || LocalHelper.PathHasInvalidChars(setting.Value)
                || Path.IsPathRooted(setting.Value)
                ? DefaultPageSpeedExecutableRelativePath
                : setting.Value;

            PageSpeedExecutablePath = Path.Combine(
                Path.GetDirectoryName(dllConfigPath).EnsureNotNull(),
                pageSpeedExecutableRelativePath);
        }

        #endregion

        #region Public Properties

        public string PageSpeedExecutablePath
        {
            get;
            private set;
        }

        #endregion
    }
}