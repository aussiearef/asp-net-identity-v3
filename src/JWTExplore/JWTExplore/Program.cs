using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;

using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .Build();

var validIssuer = config["Auth:Issuer"];

Console.Clear();
Console.WriteLine("Paste a valid JWT token here:");
var token = Console.ReadLine();

var handler = new JsonWebTokenHandler();
if (handler.CanReadToken(token))
{
    var jwt = handler.ReadJsonWebToken(token);

    var issuer = jwt.Issuer;
    if (string.IsNullOrEmpty(issuer))
    {
        Console.WriteLine("This token does not include the Issuer (issr) claim. Enter the issuer:");
        issuer = Console.ReadLine();
    }

    var gtyClaim = jwt.GetClaim("gty")?.Value; // Do not trust the value of claims here.

    Console.WriteLine($"This token has been issued by: {issuer}");

    // Now validating the token's signature ...

    // STEP 1
    var metadataAddress = $"{issuer}.well-known/openid-configuration";

    // STEP 2
    var configManager =
        new ConfigurationManager<OpenIdConnectConfiguration>(metadataAddress,
            new OpenIdConnectConfigurationRetriever());

    // STEP 3
    var oidcConfig = await configManager.GetConfigurationAsync();

    var tokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = validIssuer,
        ValidateAudience = false,
        ConfigurationManager = configManager,
        IssuerSigningKeys = oidcConfig.SigningKeys
    };

    // STEP 4
    var tokenValidationResult = await handler.ValidateTokenAsync(token, tokenValidationParameters);
    Console.WriteLine("Token is {0}.", tokenValidationResult.IsValid ? "valid" : "invalid");


    if (tokenValidationResult.IsValid)
    {
        var claimsIdentity = tokenValidationResult.ClaimsIdentity; // Trust this claim.
    }
}