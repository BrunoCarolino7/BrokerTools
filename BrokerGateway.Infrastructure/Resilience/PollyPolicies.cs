using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;

namespace BrokerGateway.Infrastructure.Resilience;

/// <summary>
/// Factory para criar políticas de resiliência Polly
/// </summary>
public static class PollyPolicies
{
    /// <summary>
    /// Cria uma política de retry com backoff exponencial e jitter
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ResilienceConfiguration config)
    {
        var random = new Random();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: config.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var exponentialDelay = TimeSpan.FromMilliseconds(
                        config.InitialDelayMilliseconds * Math.Pow(config.BackoffMultiplier, retryAttempt - 1)
                    );

                    // Adiciona jitter randomizado
                    var jitter = TimeSpan.FromMilliseconds(random.Next(0, config.MaxJitterMilliseconds));

                    return exponentialDelay.Add(jitter);
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} após {timespan.TotalMilliseconds}ms. Status: {outcome.Result?.StatusCode}");
                });
    }

    /// <summary>
    /// Cria uma política de circuit breaker
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ResilienceConfiguration config)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: config.FailureThreshold,
                durationOfBreak: TimeSpan.FromSeconds(config.CircuitBreakerTimeoutSeconds),
                onBreak: (outcome, timespan) =>
                {
                    Console.WriteLine($"Circuit breaker aberto por {timespan.TotalSeconds}s devido a falhas consecutivas");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker resetado");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("Circuit breaker em estado Half-Open, testando");
                });
    }

    /// <summary>
    /// Cria uma política de timeout
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(ResilienceConfiguration config)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(config.TimeoutSeconds));
    }

    /// <summary>
    /// Cria uma política de bulkhead isolation
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetBulkheadPolicy(ResilienceConfiguration config)
    {
        return Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: config.MaxParallelization,
            maxQueuingActions: config.MaxQueued,
            onBulkheadRejectedAsync: context =>
            {
                Console.WriteLine("Bulkhead rejeitou a requisição - limite de paralelização atingido");
                return Task.CompletedTask;
            });
    }

    /// <summary>
    /// Cria uma política de retry agressiva com mais tentativas
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetAggressiveRetryPolicy(ResilienceConfiguration config)
    {
        var random = new Random();
        var aggressiveConfig = new ResilienceConfiguration
        {
            MaxRetries = 5,
            InitialDelayMilliseconds = 50,
            BackoffMultiplier = 1.5,
            MaxJitterMilliseconds = 50
        };

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: aggressiveConfig.MaxRetries,
                sleepDurationProvider: retryAttempt =>
                {
                    var exponentialDelay = TimeSpan.FromMilliseconds(
                        aggressiveConfig.InitialDelayMilliseconds * Math.Pow(aggressiveConfig.BackoffMultiplier, retryAttempt - 1)
                    );
                    var jitter = TimeSpan.FromMilliseconds(random.Next(0, aggressiveConfig.MaxJitterMilliseconds));
                    return exponentialDelay.Add(jitter);
                });
    }

    /// <summary>
    /// Cria uma política combinada que inclui retry, timeout e circuit breaker
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCompositePolicy(
        ResilienceConfiguration config,
        ResiliencePolicy policyType = ResiliencePolicy.Standard)
    {
        var retryPolicy = policyType switch
        {
            ResiliencePolicy.Aggressive => GetAggressiveRetryPolicy(config),
            _ => GetRetryPolicy(config)
        };

        var timeoutPolicy = GetTimeoutPolicy(config);
        var bulkheadPolicy = GetBulkheadPolicy(config);
        var circuitBreakerPolicy = GetCircuitBreakerPolicy(config);

        // Composição: Bulkhead > Retry > Timeout > Circuit Breaker
        // A ordem importa! Bulkhead é o mais externo
        return Policy.WrapAsync(
            bulkheadPolicy,
            retryPolicy,
            timeoutPolicy,
            circuitBreakerPolicy);
    }

    /// <summary>
    /// Cria apenas uma política de retry sem circuit breaker (Immediate)
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetImmediatePolicy(ResilienceConfiguration config)
    {
        var random = new Random();

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 1,
                sleepDurationProvider: _ =>
                {
                    var jitter = TimeSpan.FromMilliseconds(random.Next(0, config.MaxJitterMilliseconds));
                    return TimeSpan.FromMilliseconds(config.InitialDelayMilliseconds).Add(jitter);
                });
    }
}
