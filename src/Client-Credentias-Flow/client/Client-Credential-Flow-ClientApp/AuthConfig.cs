namespace Client_Credential_Flow_ClientApp;

public class AuthConfig
{
    public string Domain { get; init; } = null!;
    public string ClientId { get; init; } = null!;
    public string ClientSecret { get; init; } = null!;
    public string Audience { get; init; } = null!;
}