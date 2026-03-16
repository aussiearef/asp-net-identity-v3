namespace Client_Credential_Flow_ClientApp;

public sealed class Auth0TokenResponse
{
    public string AccessToken { get; init; } = null!;
    public string TokenType { get; init; } = null!;
    public int ExpiresIn { get; init; }
}