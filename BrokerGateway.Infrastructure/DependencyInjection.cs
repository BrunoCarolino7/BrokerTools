using BrokerGateway.Application;
using BrokerGateway.Infrastructure.Extensions;
using BrokerGateway.Infrastructure.ExternalServices.HttpBin;
using BrokerGateway.Infrastructure.Resilience;
using BrokerGateway.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BrokerGateway.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services
            //.AddResilientHttpClient("HttpBin")
            //.AddAggressiveResilientHttpClient("HttpBin")
            .AddCircuitBreakerHttpClient("HttpBin")
            .ConfigureHttpClient(client => 
            {
                client.BaseAddress = new Uri("https://httpbin.org/");
            });
    
        services
            .AddAggressiveResilientHttpClient("CriticalPaymentApi")  
            .ConfigureHttpClient(client => 
            {
                client.BaseAddress = new Uri("https://payments.broker.com");
            });
    
        services
            .AddCircuitBreakerHttpClient("FallibleServiceApi")  
            .ConfigureHttpClient(client => 
            {
                client.BaseAddress = new Uri("https://fallible.broker.com");
            });
        
        services.AddScoped<IHttpBinService, HttpBinService>();
        
        return services;
    }
}