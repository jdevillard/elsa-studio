using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Authentication.JwtBearer.Contracts;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Login.Services;

namespace Elsa.Studio.Login.HttpMessageHandlers;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that configures the outgoing HTTP request to use the access token as bearer token.
/// </summary>
public class AuthenticatingApiHttpMessageHandler : DelegatingHandler
{
    private readonly IJwtAccessor _jwtAccessor;
    private readonly IRemoteBackendAccessor _remoteBackendAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticatingHttpMessageHandlerProvider"/> class.
    /// </summary>
    public AuthenticatingApiHttpMessageHandler(IJwtAccessor jwtAccessor, IRemoteBackendAccessor remoteBackendAccessor)
    {
        _jwtAccessor = jwtAccessor;
        _remoteBackendAccessor = remoteBackendAccessor;
        InnerHandler = new HttpClientHandler();
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Refresh token and retry once.
            var tokens = await RefreshTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            // Retry.
            response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException("Unauthorized request after token refresh");
        }

        return response;
    }

    private async Task<LoginResponse> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        // Get refresh token.
        var refreshToken = await _jwtAccessor.ReadTokenAsync(TokenNames.RefreshToken);
        
        // Setup request to get new tokens.
        var url = _remoteBackendAccessor.RemoteBackend.Url + "/identity/refresh-token";
        var refreshRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        refreshRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);
        
        // Send request.
        var response = await base.SendAsync(refreshRequestMessage, cancellationToken);

        // If the refresh token is invalid, we can't do anything.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Unauthorized request after token refresh.");

        // Parse response into tokens.
        var tokens = (await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken))!;
        
        // Store tokens.
        await _jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, tokens.RefreshToken!);
        await _jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, tokens.AccessToken!);
        
        // Return tokens.
        return tokens;
    }
}