namespace BrokerGateway.Infrastructure.Resilience;

/// <summary>
/// Enum para identificar diferentes políticas de resiliência
/// </summary>
public enum ResiliencePolicy
{
    Immediate,
    Standard,
    Aggressive,
    CircuitBreaker
}
