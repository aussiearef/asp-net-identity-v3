using System.Text.Json.Serialization;

namespace ApiA.Services;

public record TokenExchangeResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType
);