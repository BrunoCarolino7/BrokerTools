using System.Reflection;
using BrokerGateway.Api.Extensions;
using BrokerGateway.Application;
using BrokerGateway.Infrastructure;
using Scalar.AspNetCore;

namespace BrokerGateway.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddPresentation()
            .AddApplication()
            .AddInfrastructure();
        
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
        
        var app = builder.Build();

        app.MapEndpoints();
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
        
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/openapi/v1.json",
                "Management"
            );

            options.RoutePrefix = "swagger";
        });
        
        //app.MapScalarApiReference();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.Run();
    }
}