﻿using System.IO;
using System.Threading.Tasks;
using Calamari.Commands.Support;
using Calamari.Common.Variables;
using Calamari.CommonTemp;
using Calamari.Deployment;
using Calamari.Integration.FileSystem;

namespace Calamari.AzureCloudService
{
    public class ChooseCloudServiceConfigurationFileBehaviour : IAfterPackageExtractionBehaviour
    {
        static readonly string FallbackFileName = "ServiceConfiguration.Cloud.cscfg"; // Default from Visual Studio
        static readonly string EnvironmentFallbackFileName = "ServiceConfiguration.{0}.cscfg";
        readonly ILog log;
        readonly ICalamariFileSystem fileSystem;

        public ChooseCloudServiceConfigurationFileBehaviour(ILog log, ICalamariFileSystem fileSystem)
        {
            this.log = log;
            this.fileSystem = fileSystem;
        }

        public bool IsEnabled(RunningDeployment context)
        {
            return true;
        }

        public Task Execute(RunningDeployment context)
        {
            var configurationFile = ChooseWhichConfigurationFileToUse(context);
            log.SetOutputVariable(SpecialVariables.Action.Azure.Output.ConfigurationFile,
                configurationFile, context.Variables);

            return this.CompletedTask();
        }

        string ChooseWhichConfigurationFileToUse(RunningDeployment deployment)
        {
            var configurationFilePath = GetFirstExistingFile(deployment,
                deployment.Variables.Get(SpecialVariables.Action.Azure.CloudServiceConfigurationFileRelativePath),
                BuildEnvironmentSpecificFallbackFileName(deployment),
                FallbackFileName);

            return configurationFilePath;
        }

        string GetFirstExistingFile(RunningDeployment deployment, params string[] fileNames)
        {
            foreach (var name in fileNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var path = Path.Combine(deployment.CurrentDirectory, name);
                if (fileSystem.FileExists(path))
                {
                    Log.Verbose("Found Azure Cloud Service Configuration file: " + path);
                    return path;
                }

                Log.Verbose("Azure Cloud Service Configuration file (*.cscfg) not found: " + path);
            }

            throw new CommandException(
                "Could not find an Azure Cloud Service Configuration file (*.cscfg) in the package.");
        }

        static string BuildEnvironmentSpecificFallbackFileName(RunningDeployment deployment)
        {
            return string.Format(EnvironmentFallbackFileName,
                deployment.Variables.Get(DeploymentEnvironment.Name));
        }
    }
}