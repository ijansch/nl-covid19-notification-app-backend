// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using System.Collections.Generic;
using System.Security.Claims;
using NL.Rijksoverheid.ExposureNotification.BackEnd.Icc.Commands.Authorisation;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.IccPortal.Components.Tests
{
    public class TheIdentityHubServiceTests
    {
        private readonly ITheIdentityHubService _theIdentityHubService;

        private readonly WireMockServer _server;

        public TheIdentityHubServiceTests()
        {
            _server = WireMockServer.Start();
            Assert.False(_server.Urls.Length < 1, "WireMockServer not started correctly");
            _theIdentityHubService = TestTheIdentityHubServiceCreator.CreateInstance(_server);
        }

        [Fact]
        public void Validate_If_Service_Is_Initialized()
        {
            Assert.NotNull(_theIdentityHubService);
        }


        [Fact]
        public void VerifyTokenShouldReturnTrueOnValidToken()
        {
            var validToken = "valid_access_token";

            _server.Reset();
            _server.Given(
                    Request.Create()
                        .WithHeader("Authorization", "Bearer " + validToken)
                        .WithPath("/ggdghornl_test/oauth2/v1/verify").UsingGet()
                )
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{\"audience\":1234}")
                );

            Assert.True(_theIdentityHubService.VerifyTokenAsync(validToken).Result);
        }

        [Fact]
        public void VerifyTokenShouldReturnFalseOnInValidToken()
        {
            var validToken = "invalid_access_token";

            _server.Reset();
            _server.Given(
                    Request.Create()
                        .WithHeader("Authorization", "Bearer " + validToken)
                        .WithPath("/ggdghornl_test/oauth2/v1/verify").UsingGet()
                )
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(401)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{\"error\":\"Invalid Access Token\"}")
                );

            Assert.False(_theIdentityHubService.VerifyTokenAsync(validToken).Result);
        }


        [Fact]
        public void VerifyAccessTokenRevocation()
        {
            var validToken = "valid_access_token";

            _server.Reset();
            _server.Given(
                    Request.Create().UsingPost()
                        .WithHeader("Authorization", "Bearer " + validToken)
                        .WithPath("/ggdghornl_test/oauth2/v1/revoke")
                )
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{\"msg\":\"Access Token revoked\"}")
                );
            Assert.True(_theIdentityHubService.RevokeAccessTokenAsync(validToken).Result);
        }

        [Fact]
        public void VerifyInvalidAccessTokenRevocation()
        {
            var invalidAccessToken = "invalid_access_token";

            _server.Reset();
            _server.Given(
                    Request.Create().UsingPost()
                        .WithHeader("Authorization", "Bearer " + invalidAccessToken)
                        .WithPath("/ggdghornl_test/oauth2/v1/revoke")
                )
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(401)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{\"error\":\"Invalid AccessToken\"}")
                );
            Assert.False(_theIdentityHubService.RevokeAccessTokenAsync(invalidAccessToken).Result);
        }


        [Fact]
        public void ValidateValidAccessTokenWithUserClaims()
        {
            var validToken = "valid_access_token";
            var testClaim = new Claim("http://schemas.u2uconsult.com/ws/2014/03/identity/claims/accesstoken", validToken, "string");
            var identity = new ClaimsIdentity(new List<Claim>() { testClaim }, "test");

            _server.Reset();
            _server.Given(
                    Request.Create()
                        .WithHeader("Authorization", "Bearer " + validToken)
                        .WithPath("/ggdghornl_test/oauth2/v1/verify").UsingGet()
                )
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{\"audience\":1234}")
                );

            Assert.True(_theIdentityHubService.VerifyClaimTokenAsync(identity.Claims).Result);
        }

        [Fact]
        public void ValidateInValidAccessTokenWithUserClaims()
        {
            var validToken = "valid_access_token";
            var testClaim = new Claim("http://schemas.u2uconsult.com/ws/2014/03/identity/claims/accesstoken", validToken, "string");
            var identity = new ClaimsIdentity(new List<Claim>() { testClaim }, "test");

            _server.Reset();
            _server.Given(
                    Request.Create()
                        .WithHeader("Authorization", "Bearer " + "invalid_" + validToken)
                        .WithPath("/ggdghornl_test/oauth2/v1/verify").UsingGet()
                )
                .RespondWith(
                    Response.Create()
                        .WithStatusCode(200)
                        .WithHeader("Content-Type", "application/json")
                        .WithBody("{\"audience\":1234}")
                );

            Assert.False(_theIdentityHubService.VerifyClaimTokenAsync(identity.Claims).Result);
        }

    }
}
