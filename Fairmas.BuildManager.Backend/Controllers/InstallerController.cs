using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Web.Http;
using Fairmas.BuildManager.Backend.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Net;
using System.Threading;

namespace Fairmas.BuildManager.Backend.Controllers
{
    public class InstallerController : ApiController
    {
        #region Public Actions

        [HttpGet]
        [ActionName("GetBuildConfigurations")]
        public IEnumerable<BuildConfiguration> GetBuildConfigurations()
        {
            return BuildConfigurationManager.BuildConfigurations;
        }

        [HttpPost]
        [ActionName("CreateInstaller")]
        public HttpResponseMessage CreateInstaller(BuildConfiguration configuration)
        {
            try
            {
                if (configuration == null)
                    throw new ArgumentException("Installer build configuration info was not transmitted correctly from the client side. No installer can be build.");

                var installerToBuild = BuildConfigurationManager.BuildConfigurations.Where(c => c.Id == configuration.Id).FirstOrDefault();

                if (installerToBuild == null)
                    throw new InvalidOperationException($"Configuration for installer of '{configuration.Name}' with id '{configuration.Id}' could not be found. No installer will be created.");

                if (installerToBuild.IsRunning)
                    return ActionContext.Request.CreateResponse(HttpStatusCode.BadRequest, $"Installer build process for '{configuration.Name}' already running and cannot be restarted until the running build process will finish or be cancelled.");

                if (!File.Exists(installerToBuild.PathToBatch))
                    throw new FileNotFoundException($"The installer build batch could not be found at '{installerToBuild.PathToBatch}'. No installer will be created.");

                lock(BuildConfigurationManager.BuildConfigurations)
                {
                    installerToBuild.IsRunning = true;

                    ThreadPool.QueueUserWorkItem(state => BuildProcessCallback((BuildConfiguration)state), installerToBuild);
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
        public bool IsInstallerBuildProcessRunning(BuildConfiguration configuration)
        {
            try
            {
                if (configuration == null)
                    throw new ArgumentException("Installer build configuration info was not transmitted correctly from the client side.");

                var installerToCheck = BuildConfigurationManager.BuildConfigurations.Where(c => c.Id == configuration.Id).FirstOrDefault();

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

        private void BuildProcessCallback(BuildConfiguration installerToBuild)
        {
            try
            {
                var process = Process.Start(installerToBuild.PathToBatch);
                process.WaitForExit();
            }
            finally
            {
                lock (BuildConfigurationManager.BuildConfigurations)
                {
                    installerToBuild.IsRunning = false;
                }
            }
        }

        #endregion
    }
}
