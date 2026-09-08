namespace BrokerGateway.Infrastructure.Examples;

/// <summary>
/// EXEMPLO DE COMO USAR RESILÊNCIA NO SEU PROJETO
/// 
/// Este arquivo contém exemplos práticos de como integrar as políticas de resiliência
/// no seu projeto usando Polly com Retry, Circuit Breaker, Jitter e Timeout.
/// </summary>
public static class ResilienceUsageExample
{
    /*
    
    ╔═══════════════════════════════════════════════════════════════════════════════╗
    ║ 1. CONFIGURAR NO Program.cs                                                    ║
    ╚═══════════════════════════════════════════════════════════════════════════════╝
    
    var builder = WebApplication.CreateBuilder(args);
    
    // Adicionar configurações de resiliência
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddHttpClientInfrastructure(builder.Configuration);
    
    // Registrar clientes HTTP específicos com suas políticas
    builder.Services
        .AddResilientHttpClient("ExternalBrokerApi")        // Política padrão (3 retries)
        .ConfigureHttpClient(client => 
        {
            client.BaseAddress = new Uri("https://api.broker.com");
            client.DefaultRequestHeaders.Add("Authorization", "Bearer token");
        });
    
    builder.Services
        .AddAggressiveResilientHttpClient("CriticalPaymentApi")  // Política agressiva (5 retries)
        .ConfigureHttpClient(client => 
        {
            client.BaseAddress = new Uri("https://payments.broker.com");
        });
    
    builder.Services
        .AddCircuitBreakerHttpClient("FallibleServiceApi")   // Apenas circuit breaker
        .ConfigureHttpClient(client => 
        {
            client.BaseAddress = new Uri("https://fallible.broker.com");
        });
    
    var app = builder.Build();
    app.Run();
    
    
    ╔═══════════════════════════════════════════════════════════════════════════════╗
    ║ 2. USAR EM UM SERVIÇO                                                          ║
    ╚═══════════════════════════════════════════════════════════════════════════════╝
    
    public class BrokerQuoteService
    {
        private readonly HttpClient _httpClient;
        
        public BrokerQuoteService(IHttpClientFactory factory)
        {
            // Obtém o cliente HTTP com políticas de resiliência configuradas
            _httpClient = factory.CreateClient("ExternalBrokerApi");
        }
        
        public async Task<QuoteResponse> GetQuoteAsync(string symbol)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/quotes/{symbol}");
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<QuoteResponse>(content);
            }
            catch (HttpRequestException ex)
            {
                // Aqui você pode lidar com erros específicos
                Console.WriteLine($"Erro ao obter quote: {ex.Message}");
                throw;
            }
        }
    }
    
    
    ╔═══════════════════════════════════════════════════════════════════════════════╗
    ║ 3. POLÍTICAS DISPONÍVEIS                                                       ║
    ╚═══════════════════════════════════════════════════════════════════════════════╝
    
    PADRÃO (Standard):
    - Retry: 3 tentativas com backoff exponencial
    - Delay: 100ms, 200ms, 400ms (com jitter aleatório até 100ms)
    - Circuit Breaker: Abre após 5 falhas, fecha após 2 sucessos
    - Timeout: 30 segundos
    - Bulkhead: Máx 10 requisições paralelas, fila de 50
    
    AGRESSIVA (Aggressive):
    - Retry: 5 tentativas com backoff exponencial menor
    - Delay: 50ms, 75ms, 112ms, 168ms, 252ms (com jitter até 50ms)
    - Circuit Breaker: Abre após 5 falhas, fecha após 2 sucessos
    - Timeout: 30 segundos
    - Bulkhead: Máx 10 requisições paralelas, fila de 50
    
    IMEDIATA (Immediate):
    - Retry: Apenas 1 retry
    - Delay: 100ms (com jitter até 100ms)
    - Sem Circuit Breaker
    - Sem Timeout
    
    CIRCUIT BREAKER (CircuitBreaker):
    - Sem retries automáticos
    - Apenas circuit breaker: abre após 5 falhas
    - Sem timeout
    
    
    ╔═══════════════════════════════════════════════════════════════════════════════╗
    ║ 4. CONFIGURAR NO appsettings.json                                              ║
    ╚═══════════════════════════════════════════════════════════════════════════════╝
    
    {
      "Resilience": {
        "Default": {
          "TimeoutSeconds": 30,
          "MaxRetries": 3,
          "FailureThreshold": 5,
          "CircuitBreakerTimeoutSeconds": 30,
          "SuccessThreshold": 2,
          "InitialDelayMilliseconds": 100,
          "BackoffMultiplier": 2.0,
          "MaxJitterMilliseconds": 100,
          "MaxParallelization": 10,
          "MaxQueued": 50
        },
        "ExternalBrokerApi": {
          "TimeoutSeconds": 30,
          "MaxRetries": 3,
          "FailureThreshold": 5,
          "CircuitBreakerTimeoutSeconds": 30,
          "SuccessThreshold": 2,
          "InitialDelayMilliseconds": 100,
          "BackoffMultiplier": 2.0,
          "MaxJitterMilliseconds": 100,
          "MaxParallelization": 10,
          "MaxQueued": 50
        }
      }
    }
    
    
    ╔═══════════════════════════════════════════════════════════════════════════════╗
    ║ 5. MECANISMOS DE RESILIÊNCIA EXPLICADOS                                        ║
    ╚═══════════════════════════════════════════════════════════════════════════════╝
    
    A. RETRY COM BACKOFF EXPONENCIAL
    ────────────────────────────────────
    Tenta novamente em intervalos crescentes:
    - Tentativa 1: Falha imediatamente
    - Tentativa 2: Aguarda 100ms
    - Tentativa 3: Aguarda 200ms
    - Tentativa 4: Aguarda 400ms
    
    B. JITTER
    ──────────
    Adiciona aleatoriedade para evitar "thundering herd" (todos retentando ao mesmo tempo):
    - Delay = 100ms + random(0-100ms)
    - Evita picos de tráfego simultâneos
    
    C. CIRCUIT BREAKER
    ───────────────────
    Estados:
    1. CLOSED (normal): Requisições passam normalmente
    2. OPEN (sob ataque): Bloqueia requisições após N falhas consecutivas
    3. HALF-OPEN: Testa se o serviço se recuperou
    
    Exemplo:
    - Falha 1: Requisição passa, erro ocorre
    - Falha 2-5: Requisições passam, erros ocorrem
    - Estado OPEN: Próximas requisições são bloqueadas por 30s
    - Após 30s: Estado HALF-OPEN, próxima requisição testa o serviço
    - Se passar: Estado CLOSED (volta ao normal)
    - Se falhar: Volta ao OPEN
    
    D. TIMEOUT
    ──────────
    Mata requisições que demoram muito:
    - Padrão: 30 segundos
    - Evita recursos presos indefinidamente
    
    E. BULKHEAD ISOLATION
    ─────────────────────
    Limita paralelização para evitar que um serviço lento mate toda aplicação:
    - Máx 10 requisições paralelas
    - Fila de 50 requisições esperando
    - Requisições excedentes são rejeitadas com erro imediato
    
    
    ╔═══════════════════════════════════════════════════════════════════════════════╗
    ║ 6. COMPOSIÇÃO DE POLÍTICAS                                                     ║
    ╚═══════════════════════════════════════════════════════════════════════════════╝
    
    As políticas são compostas na seguinte ordem (de fora para dentro):
    
    Requisição
        ↓
    [Bulkhead Isolation]  ← Limita paralelização
        ↓
    [Retry]               ← Retenta com backoff + jitter
        ↓
    [Timeout]             ← Mata se demorar muito
        ↓
    [Circuit Breaker]     ← Bloqueia se houver muitas falhas
        ↓
    Serviço Externo
    
    
    ╔═══════════════════════════════════════════════════════════════════════════════╗
    ║ 7. TRATAMENTO DE ERROS                                                         ║
    ╚═══════════════════════════════════════════════════════════════════════════════╝
    
    Catch específico para cada tipo de erro:
    
    try
    {
        var response = await _httpClient.GetAsync("/data");
    }
    catch (BrokenCircuitException ex)
    {
        // Circuit breaker está aberto - serviço está fora
        logger.LogWarning("Circuit breaker aberto: {Message}", ex.Message);
        return GetCachedFallback();
    }
    catch (HttpRequestException ex) when (ex.InnerException is TimeoutException)
    {
        // Requisição demorou demais
        logger.LogWarning("Timeout na requisição: {Message}", ex.Message);
        return GetDefaultFallback();
    }
    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
    {
        // Rate limiting - fila cheia do bulkhead
        logger.LogWarning("Rate limited: {Message}", ex.Message);
        await Task.Delay(TimeSpan.FromSeconds(10)); // Aguarde antes de retentar
        throw;
    }
    catch (Exception ex)
    {
        // Erro genérico
        logger.LogError(ex, "Erro ao fazer requisição");
        throw;
    }
    
    */
}
