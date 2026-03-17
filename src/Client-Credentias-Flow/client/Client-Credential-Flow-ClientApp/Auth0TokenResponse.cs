using System.Text.Json.Serialization;

namespace Client_Credential_Flow_ClientApp;

public sealed class Auth0TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = null!;

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = null!;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}