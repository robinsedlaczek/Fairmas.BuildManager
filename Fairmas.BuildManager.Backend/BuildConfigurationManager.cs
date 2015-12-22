using Fairmas.BuildManager.Backend.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Fairmas.BuildManager.Backend
{
    internal class BuildConfigurationManager
    {
        #region Private Fields

        private const string PathToConfiguration = @"F:\GIT Repositories\Fairmas.BuildManager\Fairmas.BuildManager.Backend\App_Start";
        private const string ConfigurationFilename = "BuildConfigurations.xml";

        private static FileSystemWatcher s_fileSystemWatcher;

        #endregion

        #region Construction

        static BuildConfigurationManager()
        {
            LoadBuildConfigurations();

            if (s_fileSystemWatcher == null)
            {
                s_fileSystemWatcher = new FileSystemWatcher(PathToConfiguration);
                s_fileSystemWatcher.Changed += OnFileSystemWatcherChanged;
                s_fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        #endregion

        #region Internal Methods

        internal static IList<BuildConfiguration> BuildConfigurations
        {
            get;
            private set;
        }

        #endregion

        #region Static Members

        private static void LoadBuildConfigurations()
        {
            var configuration = XDocument.Load(Path.Combine(PathToConfiguration, ConfigurationFilename));

            BuildConfigurations = configuration.Descendants("Installer")
                .Select(installerConfiguration => InitializeBuildConfiguration(installerConfiguration))
                .ToList();
        }

        private static BuildConfiguration InitializeBuildConfiguration(XElement installerConfiguration)
        {
            if (installerConfiguration == null)
                throw new ArgumentNullException(nameof(installerConfiguration));

            var configuration = new BuildConfiguration
            {
                Id = Guid.Parse(installerConfiguration.Attribute("Id").Value),
                Name = installerConfiguration.Attribute("Name").Value,
                Description = installerConfiguration.Attribute("Description").Value,
                PathToBatch = installerConfiguration.Attribute("PathToBatch").Value,
                Parameters = new List<BuildConfigurationParameter<string>>()
            };

            foreach (var element in installerConfiguration.Elements("Parameter"))
            {
                var parameter = new BuildConfigurationParameter<string>
                {
                    Name = element.Attribute("Name").Value,
                    Description = element.Attribute("Description").Value,
                };

                configuration.Parameters.Add(parameter);

                InitializeParameterSourceValues(element, parameter, configuration.Name);
            }

            return configuration;
        }

        private static void InitializeParameterSourceValues(XElement parameterElement, BuildConfigurationParameter<string> parameter, string configurationName)
        {
            var valueSource = parameterElement.Attribute("ValueSource").Value;

            switch (valueSource)
            {
                case "List":
                    var values = parameterElement.Attribute("Values").Value.Split(',');
                    parameter.SourceValues = values.ToList();
                    break;
                case "File":
                    var filename = parameterElement.Attribute("Values").Value;

                    if (!File.Exists(filename))
                        throw new FileNotFoundException($"The file containing the source values for parameter '{parameter.Name}' could not be found.", filename);

                    parameter.SourceValues = File.ReadAllLines(filename);

                    break;
                default:
                    throw new ArgumentException($"The 'ValueSource' attribute for parameter '{parameter.Name}' of the '{configurationName}' installer has an invalid value.");
            }
        }

        private static void OnFileSystemWatcherChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed && e.Name.ToLower() == ConfigurationFilename.ToLower())
                LoadBuildConfigurations();
        }

        #endregion  

    }
}
