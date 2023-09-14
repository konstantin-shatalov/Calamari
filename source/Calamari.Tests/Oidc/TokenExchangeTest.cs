﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Calamari.CloudAccounts;
using Calamari.Testing.Helpers;
using FluentAssertions;
using NUnit.Framework;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Calamari.Tests.Oidc
{
    [TestFixture]
    public class TokenExchangeTest
    {
        const string TestAccessToken = "access-token-123";
        
        [Category(TestCategory.CompatibleOS.OnlyWindows)]
        [Test]
        public async Task ShouldGetAccessToken()
        {
            using (var server = WireMockServer.Start(ssl: true))
            {
                server.Given(
                             Request.Create()
                                    .WithPath("/tenant-xxx/oauth2/v2.0/token")
                                    .UsingPost()
                                    .WithHeader(headers => headers["Content-Type"].Contains("application/x-www-form-urlencoded"))
                                    .WithBody(inputs => inputs["scope"] == "https://management-url/.default")
                                    .WithBody(inputs => inputs["client_id"] == "client-xxx")
                                    .WithBody(inputs => inputs["grant_type"] == "client_credentials")
                            )
                      .RespondWith(
                                   Response.Create()
                                           .WithSuccess()
                                           .WithBodyAsJson(new
                                           {
                                               token_type = "Bearer",
                                               expires_in = 3599,
                                               access_token = TestAccessToken
                                           })
                                  );

                var serverHost = new Uri(server.Url).Host + $":{server.Port}";

                server.Given(Request.Create().WithPath("/discovery").UsingGet())
                      .RespondWith(
                                   Response.Create()
                                           .WithSuccess()
                                           .WithBodyAsJson(new Dictionary<string, object>
                                           {
                                               { "api-version", "1.1" },
                                               {
                                                   "metadata", new[]
                                                   {
                                                       new
                                                       {
                                                           preferred_network = serverHost,
                                                           preferred_cache = serverHost,
                                                           aliases = new[] { serverHost }
                                                       }
                                                   }
                                               }
                                           }));

                var account = new AzureOidcAccount(
                                                   "1111-111111111111-11111111",
                                                   "client-xxx",
                                                   "tenant-xxx",
                                                   "i'm a random jwt",
                                                   "fake env",
                                                   "https://management-url/.default",
                                                   server.Url);

                var token = await account.GetAuthorizationToken(CancellationToken.None);

                token.Should().Be(TestAccessToken);
            }
        }
    }
}