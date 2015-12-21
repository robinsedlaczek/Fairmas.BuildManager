using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Web.Http;
using Fairmas.BuildManager.Backend.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using System.Xml.Linq;
using System.Threading;

namespace Fairmas.BuildManager.Backend.Controllers
{
    public class InstallerController : ApiController
    {
        #region Fields

        private const string PathToConfiguration = @"E:\GIT Repositories\Fairmas.BuildManager\Fairmas.BuildManager.Backend\App_Start";
        private const string ConfigurationFilename = "InstallerBuildConfigurations.xml";

        private static FileSystemWatcher s_fileSystemWatcher;
        private static IList<InstallerBuildConfiguration> s_buildConfigurations;

        #endregion

        #region Construction & Destruction

        static InstallerController()
        {
            LoadInstallerBuildConfigurations();

            if (s_fileSystemWatcher == null)
            {
                s_fileSystemWatcher = new FileSystemWatcher(PathToConfiguration);
                s_fileSystemWatcher.Changed += OnFileSystemWatcherChanged;
                s_fileSystemWatcher.EnableRaisingEvents = true;
            }
        }

        #endregion

        #region Public Actions

        [HttpGet]
        [ActionName("GetBuildConfigurations")]
        public IEnumerable<InstallerBuildConfiguration> GetBuildConfigurations()
        {
            return s_buildConfigurations;
        }

        [HttpPost]
        [ActionName("CreateInstaller")]
        public HttpResponseMessage CreateInstaller(InstallerBuildConfiguration configuration)
        {
            try
            {
                if (configuration == null)
                    throw new ArgumentException("Installer build configuration info was not transmitted correctly from the client side. No installer can be build.");

                var installerToBuild = s_buildConfigurations.Where(c => c.Id == configuration.Id).FirstOrDefault();

                if (installerToBuild == null)
                    throw new InvalidOperationException($"Configuration for installer of '{configuration.Name}' with id '{configuration.Id}' could not be found. No installer will be created.");

                if (installerToBuild.IsRunning)
                    return ActionContext.Request.CreateResponse(HttpStatusCode.BadRequest, $"Installer build process for '{configuration.Name}' already running and cannot be restarted until the running build process will finish or be cancelled.");

                if (!File.Exists(installerToBuild.PathToBatch))
                    throw new FileNotFoundException($"The installer build batch could not be found at '{installerToBuild.PathToBatch}'. No installer will be created.");

                lock(s_buildConfigurations)
                {
                    installerToBuild.IsRunning = true;

                    ThreadPool.QueueUserWorkItem(state => BuildProcessCallback((InstallerBuildConfiguration)state), installerToBuild);
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception exception)
            {
                var response = ActionContext.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exception.Message, exception);
                throw new HttpResponseException(response);
            }
        }

        [HttpPost]
        [ActionName("IsInstallerBuildProcessRunning")]
        public bool IsInstallerBuildProcessRunning(InstallerBuildConfiguration configuration)
        {
            try
            {
                if (configuration == null)
                    throw new ArgumentException("Installer build configuration info was not transmitted correctly from the client side.");

                var installerToCheck = s_buildConfigurations.Where(c => c.Id == configuration.Id).FirstOrDefault();

                if (installerToCheck == null)
                    throw new InvalidOperationException($"Configuration for installer of '{configuration.Name}' with id '{configuration.Id}' could not be found and so the check cannot be done.");

                return installerToCheck.IsRunning;
            }
            catch (Exception exception)
            {
                var response = ActionContext.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exception.Message, exception);
                throw new HttpResponseException(response);
            }
        }

        #endregion

        #region Private Members

        private void BuildProcessCallback(InstallerBuildConfiguration installerToBuild)
        {
            try
            {
                var process = Process.Start(installerToBuild.PathToBatch);
                process.WaitForExit();
            }
            finally
            {
                lock (s_buildConfigurations)
                {
                    installerToBuild.IsRunning = false;
                }
            }
        }

        #region Static Members

        private static void LoadInstallerBuildConfigurations()
        {
            var configuration = XDocument.Load(Path.Combine(PathToConfiguration, ConfigurationFilename));

            s_buildConfigurations = configuration.Descendants("Installer")
                .Select(installerConfiguration =>
                    new InstallerBuildConfiguration
                    {
                        Id = Guid.Parse(installerConfiguration.Attribute("Id").Value),
                        Name = installerConfiguration.Attribute("Name").Value,
                        Description = installerConfiguration.Attribute("Description").Value,
                        PathToBatch = installerConfiguration.Attribute("PathToBatch").Value
                    })
                .ToList();
        }

        private static void OnFileSystemWatcherChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed && e.Name.ToLower() == ConfigurationFilename.ToLower())
                LoadInstallerBuildConfigurations();
        }

        #endregion  

        #endregion
    }
}
