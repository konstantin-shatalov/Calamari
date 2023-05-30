using System;
using System.Collections.Generic;
using Calamari.Commands;
using Calamari.Common.Commands;
using Calamari.Common.Features.Packages;
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
    [Command(Name, Description = "Apply Raw Yaml to Kubernetes Cluster")]
    public class KubernetesApplyRawYamlCommand : KubernetesDeploymentCommandBase
    {
        public const string Name = "kubernetes-apply-raw-yaml";

        private readonly IVariables variables;
        private readonly Func<GatherAndApplyRawYamlConvention> gatherAndApplyRawYamlFactory;
        private readonly Func<ResourceStatusReportConvention> resourceStatusReportFactory;

        public KubernetesApplyRawYamlCommand(
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
            IExtractPackage extractPackage,
            Func<GatherAndApplyRawYamlConvention> gatherAndApplyRawYamlFactory,
            Func<ResourceStatusReportConvention> resourceStatusReportFactory)
            : base(deploymentJournalWriter, variables, kubectl, delegateInstallFactory,
            substituteInFilesFactory, configurationTransformationFactory, configurationVariablesFactory,
            structuredConfigurationVariablesFactory, awsAuthConventionFactory, kubernetesAuthContextFactory,
            conventionProcessorFactory, runningDeploymentFactory, fileSystem, extractPackage)
        {
            this.variables = variables;
            this.gatherAndApplyRawYamlFactory = gatherAndApplyRawYamlFactory;
            this.resourceStatusReportFactory = resourceStatusReportFactory;
        }

        public override int Execute(string[] commandLineArguments)
        {
            if (!FeatureToggle.MultiGlobPathsForRawYamlFeatureToggle.IsEnabled(variables))
                throw new InvalidOperationException(
                    "Unable to execute the Kubernetes Apply Raw YAML Command because the appropriate feature has not been enabled.");

            return base.Execute(commandLineArguments);
        }

        protected override IEnumerable<IInstallConvention> CommandSpecificConventions()
        {
            return new IInstallConvention[]
            {
                gatherAndApplyRawYamlFactory(),
                resourceStatusReportFactory()
            };
        }
    }
}