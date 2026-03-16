// See https://aka.ms/new-console-template for more information

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Client_Credential_Flow_ClientApp;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();


var authConfig = configuration.GetSection("Auth");
var issuer = $"{authConfig["Issuer"]}auth/token";

var payload = new
{
    client_id     = authConfig["ClientId"],
    client_secret = authConfig["ClientSecret"],
    audience      = authConfig["Audience"],
    grant_type    = "client_credentials"
};



var http = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)
};
http.DefaultRequestHeaders.Accept.Clear();
http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


var jsonPayload = JsonSerializer.Serialize(payload);
using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");


using var resp = await http.PostAsync(issuer, content);
var body = await resp.Content.ReadAsStringAsync();


var token = JsonSerializer.Deserialize<Auth0TokenResponse>(body, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});


Console.WriteLine("Access Token:");
Console.WriteLine(token?.AccessToken);
Console.WriteLine();
Console.WriteLine($"Type: {token?.TokenType} | Expires In: {token?.ExpiresIn}s");

Console.WriteLine("Press any keys...");
Console.ReadKey();