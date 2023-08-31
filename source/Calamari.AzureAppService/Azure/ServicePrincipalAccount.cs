﻿using System;
using Calamari.Common.Plumbing.Variables;
using Newtonsoft.Json;

namespace Calamari.AzureAppService.Azure
{
    class ServicePrincipalAccount : IAzureAccount
    {
        [JsonConstructor]
        public ServicePrincipalAccount(
            string subscriptionNumber,
            string clientId,
            string tenantId,
            string password,
            string azureEnvironment,
            string resourceManagementEndpointBaseUri,
            string activeDirectoryEndpointBaseUri)
        {
            this.SubscriptionNumber = subscriptionNumber;
            this.ClientId = clientId;
            this.TenantId = tenantId;
            this.Password = password;
            this.AzureEnvironment = azureEnvironment;
            this.ResourceManagementEndpointBaseUri = resourceManagementEndpointBaseUri;
            this.ActiveDirectoryEndpointBaseUri = activeDirectoryEndpointBaseUri;
        }

        public ServicePrincipalAccount(IVariables variables)
        {
            this.SubscriptionNumber = variables.Get(AccountVariables.SubscriptionId);
            this.ClientId = variables.Get(AccountVariables.ClientId);
            this.TenantId = variables.Get(AccountVariables.TenantId);
            this.Password = variables.Get(AccountVariables.Password);
            this.AzureEnvironment = variables.Get(AccountVariables.Environment);
            this.ResourceManagementEndpointBaseUri = variables.Get(AccountVariables.ResourceManagementEndPoint, DefaultVariables.ResourceManagementEndpoint);
            this.ActiveDirectoryEndpointBaseUri = variables.Get(AccountVariables.ActiveDirectoryEndPoint, DefaultVariables.ActiveDirectoryEndpoint);
        }

        public AccountType AccountType => AccountType.AzureServicePrincipal;
        public string GetCredential => Password;
        public string SubscriptionNumber { get;  }
        public string ClientId { get; }
        public string TenantId { get; }
        private string Password { get; }
        public string AzureEnvironment { get; }
        public string ResourceManagementEndpointBaseUri { get; }
        public string ActiveDirectoryEndpointBaseUri { get; }
    }
}
