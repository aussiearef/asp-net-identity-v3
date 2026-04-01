using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

// REMOVE 'Microsoft.AspNetCore.Authentication.JwtBearer' Nuget package.
// ADD 'Microsoft.AspNetCore.Authentication.OpenIdConnect' Nuget package.


namespace Client_Credentias_Flow;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Add Authentication Services
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "__Host-BFF";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = builder.Configuration["Auth:Issuer"];
                options.ClientId = builder.Configuration["Auth:ClientId"];
                options.ClientSecret = builder.Configuration["Auth:ClientSecret"];
                options.ResponseType = "code";
                options.ResponseMode = "query";
    
                // This enables PKCE automatically in ASP.NET Core
                options.UsePkce = true; 
    
                options.SaveTokens = true; // Saves tokens in the cookie
                options.Scope.Add("offline_access"); // Request a refresh token
                
                // Critical
                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        context.ProtocolMessage.SetParameter("audience", builder.Configuration["Auth:Audience"]);
                        return Task.CompletedTask;
                    }
                };
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        // 2. Enable Authentication Middleware
        // Order is critical: Authentication MUST come before Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        // 3. Secure the Endpoint
        app.MapGet("/weatherforecast", (HttpContext httpContext) =>
            {
                var forecast = Enumerable.Range(1, 5).Select(index =>
                        new WeatherForecast
                        {
                            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            TemperatureC = Random.Shared.Next(-20, 55),
                            Summary = summaries[Random.Shared.Next(summaries.Length)]
                        })
                    .ToArray();
                return forecast;
            })
            .WithName<IEndpointConventionBuilder>("GetWeatherForecast")
            .RequireAuthorization(); // This line ensures the JWT is present and valid

        app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties 
            { 
                RedirectUri = "/" 
            },
            [OpenIdConnectDefaults.AuthenticationScheme]));
        app.Run();
    }
}
