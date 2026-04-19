// src/RenovateDashboard.App/Program.cs
using RenovateDashboard.App.Endpoints;
using RenovateDashboard.App.Services;
using RenovateDashboard.App.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<GitLabService>();
builder.Services.AddScoped<DigestService>();
builder.Services.AddHostedService<DigestWorker>();

var app = builder.Build();

app.MapMrEndpoints();

app.Run();
