using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SpecimenCheckIn.Api.Data;
using SpecimenCheckIn.Api.Services;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// Configure DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
});

// Configure CORS to allow Angular local development server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Specimen Check-In Multi-Tenant API", 
        Version = "v1",
        Description = "API for checking in and managing specimen shipments across different clinics/tenants."
    });
    
    // Add X-Tenant-ID header parameter to endpoints
    c.OperationFilter<TenantHeaderOperationFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Specimen Check-In API v1");
    c.RoutePrefix = string.Empty; // Swagger UI at root
});

app.UseCors("AngularDevPolicy");

// In production/deployment we'd use HTTPS redirection, but for simple local dev, we make it optional
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Automatically run database migrations and seeding with retries (useful for docker-compose startup)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var context = services.GetRequiredService<ApplicationDbContext>();
    
    int retryCount = 10;
    int delaySeconds = 4;
    for (int i = 0; i < retryCount; i++)
    {
        try
        {
            DbInitializer.Initialize(context);
            logger.LogInformation("Database initialized and seeded successfully.");
            break;
        }
        catch (Exception ex)
        {
            if (i == retryCount - 1)
            {
                logger.LogError(ex, "Failed to initialize and seed database after maximum retries.");
                // We don't crash the application immediately, but log the critical error
            }
            else
            {
                logger.LogWarning($"Database connection failed. SQL Server might still be starting. Retrying in {delaySeconds} seconds... (Attempt {i + 1}/{retryCount})");
                Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
}

app.Run();

// Operation Filter to add X-Tenant-ID header to Swagger UI
public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
        {
            operation.Parameters = new List<OpenApiParameter>();
        }

        // Check if the current controller is TenantsController; if so, do not add the header requirement
        var isTenantController = context.MethodInfo.DeclaringType?.Name.Contains("Tenants") ?? false;
        
        if (!isTenantController)
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "X-Tenant-ID",
                In = ParameterLocation.Header,
                Required = false, // Allow running without header, although actions will validate
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Format = "uuid"
                },
                Description = "Guid of the active Tenant context. Must be a valid tenant ID registered in the DB."
            });
        }
    }
}
