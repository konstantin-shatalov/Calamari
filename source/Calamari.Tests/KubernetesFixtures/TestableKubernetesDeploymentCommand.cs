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
using Calamari.Kubernetes.Commands;
using Calamari.Kubernetes.Conventions;
using Calamari.Kubernetes.Integration;

namespace Calamari.Tests.KubernetesFixtures
{
    public class TestableKubernetesDeploymentCommand : KubernetesDeploymentCommandBase
    {
        private readonly Kubectl kubectl;

        public TestableKubernetesDeploymentCommand(IDeploymentJournalWriter deploymentJournalWriter,
            IVariables variables,
            Kubectl kubectl, DelegateInstallConvention.Factory delegateInstallFactory,
            Func<SubstituteInFilesConvention> substituteInFilesFactory,
            Func<ConfigurationTransformsConvention> configurationTransformationFactory,
            Func<ConfigurationVariablesConvention> configurationVariablesFactory,
            Func<StructuredConfigurationVariablesConvention> structuredConfigurationVariablesFactory,
            IAwsAuthConventionFactory awsAuthConventionFactoryFactory,
            Func<KubernetesAuthContextConvention> kubernetesAuthContextFactory,
            ConventionProcessor.Factory conventionProcessorFactory, RunningDeployment.Factory runningDeploymentFactory,
            ICalamariFileSystem fileSystem, IExtractPackage extractPackage) : base(deploymentJournalWriter, variables,
            kubectl, delegateInstallFactory, substituteInFilesFactory, configurationTransformationFactory,
            configurationVariablesFactory, structuredConfigurationVariablesFactory, awsAuthConventionFactoryFactory,
            kubernetesAuthContextFactory, conventionProcessorFactory, runningDeploymentFactory, fileSystem,
            extractPackage)
        {
            this.kubectl = kubectl;
        }

        protected override IEnumerable<IInstallConvention> CommandSpecificConventions()
        {
            yield return new TestKubectlAuthConvention(kubectl);
        }

        private class TestKubectlAuthConvention : IInstallConvention
        {
            private readonly Kubectl kubectl;

            public TestKubectlAuthConvention(Kubectl kubectl)
            {
                this.kubectl = kubectl;
            }

            public void Install(RunningDeployment deployment)
            {
                if (!kubectl.TrySetKubectl())
                {
                    throw new InvalidOperationException("Unable to set KubeCtl");
                }

                kubectl.ExecuteCommand("cluster-info");
            }
        }
    }
}

