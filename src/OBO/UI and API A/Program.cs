using System.Net.Http.Headers;
using ApiA.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Client_Credentias_Flow;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestHeadersTotalSize = 65536; // 64 KB
        });
        
        builder.Services.AddHttpClient();
        builder.Services.AddLogging();
        builder.Services.AddScoped<TokenExchangeService>();
        
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "__Host-OBO-EntraID"; // rename cookie with every run for a clean debug
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var tokenEndpoint = $"{builder.Configuration["Auth:Issuer"]}/v2.0";
                options.Authority = tokenEndpoint;
                options.ClientId = builder.Configuration["Auth:ClientId"];
                options.ClientSecret = builder.Configuration["Auth:ClientSecret"];
                options.ResponseType = "code";
                options.ResponseMode = "query";
                options.Scope.Add("api://880ceb9e-7e3b-4ac2-a0dd-2ed2e2b81b24/Access.All");
                options.Scope.Add("openid profile");
                options.UsePkce = true; 
    
                options.SaveTokens = true; // Saves tokens in the cookie. Important for TOKEN EXCHANGE
                
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        var audience = builder.Configuration["Auth:Audience"];
                        context.ProtocolMessage.SetParameter("audience", audience);
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();


        app.MapGet("/weatherforecast", async (HttpContext httpContext, TokenExchangeService exchangeService) =>
            {
                var userToken = await httpContext.GetTokenAsync("access_token") ?? "";
                var targetToken = await exchangeService.ExchangeTokenAsync(userToken);

                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                
                Console.Clear();
                Console.WriteLine($"Token to call aPI B: {targetToken}");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", targetToken);
                var apiResponse = await httpClient.GetAsync("http://localhost:8000/weatherforecast");

                if (apiResponse.IsSuccessStatusCode)
                {
                    var apiResponsePayload = await apiResponse.Content.ReadFromJsonAsync<WeatherForecast[]>();
                    return Results.Ok(apiResponsePayload);
                }
                else
                {
                    
                    Console.WriteLine($"Error calling downstream API. Status Code={apiResponse.StatusCode}");
                    Console.WriteLine($"{apiResponse.ReasonPhrase}");
                    return Results.Problem("");
                }
            })
            .WithName<IEndpointConventionBuilder>("GetWeatherForecast")
            .RequireAuthorization(); 

        app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties 
            { 
                RedirectUri = "/" 
            },
            [OpenIdConnectDefaults.AuthenticationScheme]));
        app.Run();
    }
}
