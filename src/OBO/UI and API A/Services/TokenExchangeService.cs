namespace ApiA.Services;

public class TokenExchangeService(
    IHttpClientFactory httpClientFactory, 
    IConfiguration config, 
    ILogger<TokenExchangeService> logger)
{
    public async Task<string?> ExchangeTokenAsync(string userAccessToken)
    {
        using var client = httpClientFactory.CreateClient();
        
        var authority = config["Auth:Issuer"];
        var tokenEndpoint = $"{authority?.TrimEnd('/')}/oauth2/v2.0/token";
        
        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
            { "client_id", config["Auth:ClientId"]! },
            { "client_secret", config["Auth:ClientSecret"]! },
            { "assertion", userAccessToken },
            { "scope", config["Auth:APIB_Scope"]! },
            { "requested_token_use", "on_behalf_of" } 
        };

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(parameters));

        if (!response.IsSuccessStatusCode)
        {
            var errorDetails = await response.Content.ReadAsStringAsync();
            logger.LogError("OBO Exchange failed. Status: {StatusCode}, Details: {Details}", 
                response.StatusCode, errorDetails);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>();

        logger.LogInformation($"Exchanged Token:{result}");
        return result?.AccessToken;
    }
}