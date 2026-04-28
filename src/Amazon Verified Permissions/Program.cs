using System.Security.Claims;
using Amazon;
using Amazon.VerifiedPermissions;
using Amazon.VerifiedPermissions.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Client_Credentias_Flow;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "__Host-BFF_AVP";
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

                options.UsePkce = true; 
    
                options.SaveTokens = true; 
                options.Scope.Add("offline_access"); 
                
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.UseAuthentication();
        app.UseAuthorization();


        app.MapGet("/profile", async (HttpContext context, string profileId="") =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Fetch the Profile record of current user from database. 100 is an example of profile id.
            var avpClient = new AmazonVerifiedPermissionsClient(); // Uses Managed Identity (local AWS profile or Instance IAM role)
            
            var isAuthorised = await avpClient.IsAuthorizedAsync(new IsAuthorizedRequest
            {
                PolicyStoreId = builder.Configuration["AVP:PolicyStoreId"],

                // 1. Principal: The ID of the current user from Auth0 - This user is taking action
                Principal = new EntityIdentifier
                {
                    EntityType = "demo_pdp_pep::User",
                    EntityId = userId 
                },

                // 2. Action: The operation being performed
                Action = new ActionIdentifier
                {
                    ActionType = "demo_pdp_pep::Action",
                    ActionId = "Read"
                },

                // 3. Resource ID: The specific profile ID from your API route
                Resource = new EntityIdentifier
                {
                    EntityType = "demo_pdp_pep::user_profile",
                    EntityId = profileId // e.g. "100"
                },

                // 4. OwnerId: Passing the attribute value for the 'when' clause
                Entities = new EntitiesDefinition
                {
                    EntityList =
                    [
                        new EntityItem
                        {
                            Identifier = new EntityIdentifier
                            {
                                EntityType = "demo_pdp_pep::user_profile",
                                EntityId = profileId
                            },
                            Attributes = new Dictionary<string, AttributeValue>
                            {
                                // Mapping the ownerId as an Entity Reference to match your Cedar policy
                                ["ownerId"] = new AttributeValue
                                {
                                    EntityIdentifier = new EntityIdentifier
                                    {
                                        EntityType = "demo_pdp_pep::User",
                                        EntityId = userId
                                    }
                                }
                            }
                        }
                    ]
                }
            });
            
            return isAuthorised.Decision.Value;
        })
        .RequireAuthorization();
        
        app.MapPut("/profile", (HttpContext context)=>{
            
        })
        .RequireAuthorization();
        
        
        
        app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties 
            { 
                RedirectUri = "/" 
            },
            [OpenIdConnectDefaults.AuthenticationScheme]));
        app.Run();
    }
}
