namespace BrokerGateway.Infrastructure.Resilience;

/// <summary>
/// Configurações de resiliência para requisições HTTP
/// </summary>
public class ResilienceConfiguration
{
    /// <summary>
    /// Tempo máximo de espera para uma requisição (em segundos)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Número máximo de tentativas
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Número de falhas consecutivas antes de abrir o circuit breaker
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Tempo de espera antes de tentar fechar o circuit breaker (em segundos)
    /// </summary>
    public int CircuitBreakerTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Número de requisições bem-sucedidas para fechar o circuit breaker
    /// </summary>
    public int SuccessThreshold { get; set; } = 2;

    /// <summary>
    /// Delay inicial para retry (em milissegundos)
    /// </summary>
    public int InitialDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// Multiplicador para backoff exponencial
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Máximo jitter para adicionar ao delay (em milissegundos)
    /// </summary>
    public int MaxJitterMilliseconds { get; set; } = 100;

    /// <summary>
    /// Número máximo de requisições paralelas (Bulkhead)
    /// </summary>
    public int MaxParallelization { get; set; } = 10;

    /// <summary>
    /// Número máximo de requisições na fila (Bulkhead)
    /// </summary>
    public int MaxQueued { get; set; } = 50;
}
