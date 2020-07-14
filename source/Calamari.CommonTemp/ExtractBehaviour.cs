﻿using System.Threading.Tasks;
using Calamari.Deployment;
using Calamari.Deployment.Conventions;

namespace Calamari.CommonTemp
{
    internal class ExtractBehaviour: IBehaviour
    {
        readonly IExtractPackage extractPackage;

        public ExtractBehaviour(IExtractPackage extractPackage)
        {
            this.extractPackage = extractPackage;
        }

        public bool IsEnabled(RunningDeployment context)
        {
            return !string.IsNullOrWhiteSpace(context.PackageFilePath);
        }

        public Task Execute(RunningDeployment context)
        {
            extractPackage.ExtractToStagingDirectory(context.PackageFilePath);
            return this.CompletedTask();
        }
    }
}