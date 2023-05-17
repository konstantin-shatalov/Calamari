using System;
using System.Collections.Generic;
using System.IO;
using Calamari.Commands;
using Calamari.Commands.Support;
using Calamari.Common.Commands;
using Calamari.Common.Features.Packages;
using Calamari.Common.Plumbing.Deployment;
using Calamari.Common.Plumbing.Deployment.Journal;
using Calamari.Common.Plumbing.FileSystem;
using Calamari.Common.Plumbing.Variables;
using Calamari.Deployment;
using Calamari.Deployment.Conventions;
using Calamari.FeatureToggles;
using Calamari.Kubernetes.Conventions;
using Calamari.Kubernetes.Integration;

namespace Calamari.Kubernetes.Commands
{
    public abstract class KubernetesDeploymentCommandBase  : Command
    {
        private readonly IDeploymentJournalWriter deploymentJournalWriter;
        private readonly IVariables variables;
        private readonly Kubectl kubectl;
        private readonly DelegateInstallConvention.Factory delegateInstallFactory;
        private readonly Func<SubstituteInFilesConvention> substituteInFilesFactory;
        private readonly Func<ConfigurationTransformsConvention> configurationTransformationFactory;
        private readonly Func<ConfigurationVariablesConvention> configurationVariablesFactory;
        private readonly Func<StructuredConfigurationVariablesConvention> structuredConfigurationVariablesFactory;
        private readonly IAwsAuthConventionFactory awsAuthConventionFactory;
        private readonly Func<KubernetesAuthContextConvention> kubernetesAuthContextFactory;
        private readonly ConventionProcessor.Factory conventionProcessorFactory;
        private readonly RunningDeployment.Factory runningDeploymentFactory;
        private readonly ICalamariFileSystem fileSystem;
        private readonly IExtractPackage extractPackage;
        private PathToPackage pathToPackage;

        protected KubernetesDeploymentCommandBase(
            IDeploymentJournalWriter deploymentJournalWriter,
            IVariables variables,
            Kubectl kubectl,
            DelegateInstallConvention.Factory delegateInstallFactory,
            Func<SubstituteInFilesConvention> substituteInFilesFactory,
            Func<ConfigurationTransformsConvention> configurationTransformationFactory,
            Func<ConfigurationVariablesConvention> configurationVariablesFactory,
            Func<StructuredConfigurationVariablesConvention> structuredConfigurationVariablesFactory,
            IAwsAuthConventionFactory awsAuthConventionFactory,
            Func<KubernetesAuthContextConvention> kubernetesAuthContextFactory,
            ConventionProcessor.Factory conventionProcessorFactory,
            RunningDeployment.Factory runningDeploymentFactory,
            ICalamariFileSystem fileSystem,
            IExtractPackage extractPackage)
        {
            this.deploymentJournalWriter = deploymentJournalWriter;
            this.variables = variables;
            this.kubectl = kubectl;
            this.delegateInstallFactory = delegateInstallFactory;
            this.substituteInFilesFactory = substituteInFilesFactory;
            this.configurationTransformationFactory = configurationTransformationFactory;
            this.configurationVariablesFactory = configurationVariablesFactory;
            this.structuredConfigurationVariablesFactory = structuredConfigurationVariablesFactory;
            this.awsAuthConventionFactory = awsAuthConventionFactory;
            this.kubernetesAuthContextFactory = kubernetesAuthContextFactory;
            this.conventionProcessorFactory = conventionProcessorFactory;
            this.runningDeploymentFactory = runningDeploymentFactory;
            this.fileSystem = fileSystem;
            this.extractPackage = extractPackage;

            Options.Add("package=", "Path to the NuGet package to install.",
                v => pathToPackage = new PathToPackage(Path.GetFullPath(v)));
        }

        protected abstract IEnumerable<IInstallConvention> CommandSpecificConventions();

        public override int Execute(string[] commandLineArguments)
        {
            if (!FeatureToggle.MultiGlobPathsForRawYamlFeatureToggle.IsEnabled(variables))
                throw new InvalidOperationException(
                    "Unable to execute the Kubernetes Apply Raw YAML Command because the appropriate feature has not been enabled.");

            Options.Parse(commandLineArguments);
            var conventions = new List<IConvention>
            {
                delegateInstallFactory(d =>
                {
                    if (pathToPackage != null)
                    {
                        extractPackage.ExtractToStagingDirectory(pathToPackage, workingDirectory: d.CurrentDirectory);
                    }
                    else
                    {
                        //If using the inline yaml, copy it to the staging directory.
                        var inlineFile = Path.Combine(d.CurrentDirectory, "customresource.yml");
                        var stagingDirectory = Path.Combine(d.CurrentDirectory, "staging");
                        fileSystem.EnsureDirectoryExists(stagingDirectory);
                        if (fileSystem.FileExists(inlineFile))
                        {
                            fileSystem.MoveFile(inlineFile, Path.Combine(stagingDirectory, "customresource.yml"));
                        }
                        d.StagingDirectory = stagingDirectory;
                        d.CurrentDirectoryProvider = DeploymentWorkingDirectory.StagingDirectory;
                    }

                    kubectl.WorkingDirectory = d.CurrentDirectory;
                    kubectl.EnvironmentVariables = d.EnvironmentVariables;
                }),
                substituteInFilesFactory(),
                configurationTransformationFactory(),
                configurationVariablesFactory(),
                structuredConfigurationVariablesFactory()
            };

            if (variables.Get(Deployment.SpecialVariables.Account.AccountType) == "AmazonWebServicesAccount")
            {
                conventions.Add(awsAuthConventionFactory.Create());
            }

            conventions.Add(kubernetesAuthContextFactory());

            conventions.AddRange(CommandSpecificConventions());

            var conventionRunner = conventionProcessorFactory(runningDeploymentFactory(pathToPackage), conventions);
            try
            {
                conventionRunner.RunConventions();
                deploymentJournalWriter.AddJournalEntry(conventionRunner.Deployment, true, pathToPackage);
            }
            catch (Exception)
            {
                deploymentJournalWriter.AddJournalEntry(conventionRunner.Deployment, false, pathToPackage);
                throw;
            }

            return 0;
        }
    }
}