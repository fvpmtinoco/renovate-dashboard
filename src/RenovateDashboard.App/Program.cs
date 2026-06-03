using Microsoft.AspNetCore.Authentication.JwtBearer;
using RenovateDashboard.App.Endpoints;
using RenovateDashboard.App.Services;
using RenovateDashboard.App.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GitLabOptions>(builder.Configuration.GetSection("GitLab"));
builder.Services.AddHttpClient<GitLabService>();
builder.Services.AddScoped<DigestService>();
builder.Services.AddHostedService<DigestWorker>();

var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

if (string.IsNullOrWhiteSpace(auth0Domain))
    throw new InvalidOperationException("Auth0:Domain is required.");
if (string.IsNullOrWhiteSpace(auth0Audience))
    throw new InvalidOperationException("Auth0:Audience is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{auth0Domain}/";
        options.Audience = auth0Audience;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapMrEndpoints();

app.Run();

public partial class Program { }
