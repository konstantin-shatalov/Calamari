﻿using System;
using System.Threading.Tasks;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Calamari.AzureAppService.Azure;
using Calamari.Common.Commands;
using Calamari.Common.Plumbing.Logging;
using Calamari.Common.Plumbing.Pipeline;

namespace Calamari.AzureAppService.Behaviors
{
    class AzureAppServiceDeployContainerBehavior : IDeployBehaviour
    {
        private ILog Log { get; }

        public AzureAppServiceDeployContainerBehavior(ILog log)
        {
            Log = log;
        }

        public bool IsEnabled(RunningDeployment context)
        {
            return true;
        }

        public async Task Execute(RunningDeployment context)
        {
            var variables = context.Variables;

            var principalAccount = ServicePrincipalAccount.CreateFromKnownVariables(variables);
            var webAppName = variables.Get(SpecialVariables.Action.Azure.WebAppName);
            var slotName = variables.Get(SpecialVariables.Action.Azure.WebAppSlot);
            var resourceGroupName = variables.Get(SpecialVariables.Action.Azure.ResourceGroupName);
            
            var targetSite = new AzureTargetSite(principalAccount.SubscriptionNumber, resourceGroupName, webAppName, slotName);

            var image = variables.Get(SpecialVariables.Action.Package.Image);
            var registryHost = variables.Get(SpecialVariables.Action.Package.Registry);
            var regUsername = variables.Get(SpecialVariables.Action.Package.Feed.Username);
            var regPwd = variables.Get(SpecialVariables.Action.Package.Feed.Password);

            var armClient = principalAccount.CreateArmClient();

            Log.Info($"Updating web app to use image {image} from registry {registryHost}");

            Log.Verbose("Retrieving config (this is required to update image)");
            var config = targetSite.HasSlot switch
                         {
                             true => (await armClient.GetWebSiteSlotConfigResource(WebSiteSlotConfigResource.CreateResourceIdentifier(principalAccount.SubscriptionNumber, targetSite.ResourceGroupName, targetSite.Site, targetSite.Slot))
                                                     .GetAsync()).Value.Data,
                             false => (await armClient.GetWebSiteConfigResource(WebSiteConfigResource.CreateResourceIdentifier(principalAccount.SubscriptionNumber, targetSite.ResourceGroupName, targetSite.Site))
                                                      .GetAsync()).Value.Data
                         };
            config.LinuxFxVersion = $@"DOCKER|{image}";

            Log.Verbose("Retrieving app settings");
            var appSettings = await AppSettingsManagement.GetAppSettings(armClient, principalAccount.SubscriptionNumber, targetSite);

            appSettings.Properties["DOCKER_REGISTRY_SERVER_URL"] = "https://" + registryHost;
            appSettings.Properties["DOCKER_REGISTRY_SERVER_USERNAME"] = regUsername;
            appSettings.Properties["DOCKER_REGISTRY_SERVER_PASSWORD"] = regPwd;

            Log.Info("Updating app settings with container registry");
            await AppSettingsManagement.PutAppSettingsAsync(armClient, principalAccount.SubscriptionNumber, appSettings, targetSite);

            Log.Info("Updating configuration with container image");
            switch (targetSite.HasSlot)
            {
                case true:
                    await armClient.GetWebSiteSlotConfigResource(WebSiteSlotConfigResource.CreateResourceIdentifier(principalAccount.SubscriptionNumber, targetSite.ResourceGroupName, targetSite.Site, targetSite.Slot))
                                   .UpdateAsync(config);
                    break;
                case false:
                    await armClient.GetWebSiteConfigResource(WebSiteConfigResource.CreateResourceIdentifier(principalAccount.SubscriptionNumber, targetSite.ResourceGroupName, targetSite.Site))
                                   .UpdateAsync(config);
                    break;
            }
        }
    }
}