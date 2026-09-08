using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using BrokerGateway.Infrastructure.Resilience;

namespace BrokerGateway.Infrastructure.Extensions;

/// <summary>
/// Extensões para registrar clientes HTTP com políticas de resiliência
/// </summary>
public static class HttpClientResilienceExtensions
{
    /// <summary>
    /// Registra um HttpClient com política de resiliência padrão
    /// </summary>
    public static IHttpClientBuilder AddResilientHttpClient(
        this IServiceCollection services,
        string clientName,
        string? configurationSectionName = null,
        ResiliencePolicy policyType = ResiliencePolicy.Standard)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        
        var sectionName = configurationSectionName ?? $"Resilience:{clientName}";
        var config = configuration.GetSection(sectionName).Get<ResilienceConfiguration>() 
            ?? new ResilienceConfiguration();

        var policy = PollyPolicies.GetCompositePolicy(config, policyType);

        return services.AddHttpClient(clientName)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
            {
                handlers.Add(new PolicyHttpMessageHandler(policy));
            });
    }

    /// <summary>
    /// Registra um HttpClient com política de resiliência imediata (apenas 1 retry)
    /// </summary>
    public static IHttpClientBuilder AddImmediateResilientHttpClient(
        this IServiceCollection services,
        string clientName,
        string? configurationSectionName = null)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        
        var sectionName = configurationSectionName ?? $"Resilience:{clientName}";
        var config = configuration.GetSection(sectionName).Get<ResilienceConfiguration>() 
            ?? new ResilienceConfiguration();

        var policy = PollyPolicies.GetImmediatePolicy(config);

        return services.AddHttpClient(clientName)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
            {
                handlers.Add(new PolicyHttpMessageHandler(policy));
            });
    }

    /// <summary>
    /// Registra um HttpClient com política de resiliência agressiva (múltiplos retries)
    /// </summary>
    public static IHttpClientBuilder AddAggressiveResilientHttpClient(
        this IServiceCollection services,
        string clientName,
        string? configurationSectionName = null)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        
        var sectionName = configurationSectionName ?? $"Resilience:{clientName}";
        var config = configuration.GetSection(sectionName).Get<ResilienceConfiguration>() 
            ?? new ResilienceConfiguration();

        var policy = PollyPolicies.GetCompositePolicy(config, ResiliencePolicy.Aggressive);

        return services.AddHttpClient(clientName)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
            {
                handlers.Add(new PolicyHttpMessageHandler(policy));
            });
    }

    /// <summary>
    /// Registra um HttpClient com apenas a política de circuit breaker (sem retries)
    /// </summary>
    public static IHttpClientBuilder AddCircuitBreakerHttpClient(
        this IServiceCollection services,
        string clientName,
        string? configurationSectionName = null)
    {
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        
        var sectionName = configurationSectionName ?? $"Resilience:{clientName}";
        var config = configuration.GetSection(sectionName).Get<ResilienceConfiguration>() 
            ?? new ResilienceConfiguration();

        var policy = PollyPolicies.GetCircuitBreakerPolicy(config);

        return services.AddHttpClient(clientName)
            .ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
            {
                handlers.Add(new PolicyHttpMessageHandler(policy));
            });
    }
}

/// <summary>
/// Handler HTTP que aplica a política de resiliência Polly
/// </summary>
internal class PolicyHttpMessageHandler : DelegatingHandler
{
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;

    public PolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy)
    {
        _policy = policy;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await _policy.ExecuteAsync(
            ct => base.SendAsync(request, ct),
            cancellationToken);
    }
}


