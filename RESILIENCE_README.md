# 🛡️ Guia de Resiliência - Circuit Breaker, Retry com Jitter e Timeout

Documentação completa das configurações de resiliência implementadas no projeto BrokerGateway.

---

## 📋 Visão Geral

Este projeto implementa **padrões de resiliência** para comunicação HTTP confiável usando a biblioteca **Polly**. As políticas implementadas protegem sua aplicação contra:

- 🔄 **Falhas transitórias** (Retry com Backoff Exponencial)
- 🎲 **Thundering Herd** (Jitter aleatório)
- 🚫 **Cascata de falhas** (Circuit Breaker)
- ⏱️ **Requisições travadas** (Timeout)
- 💪 **Sobrecarga** (Bulkhead Isolation)

---

## 🏗️ Estrutura de Arquivos

```
BrokerGateway.Infrastructure/
├── Resilience/
│   ├── ResiliencePolicy.cs              # Enum das políticas
│   ├── ResilienceConfiguration.cs       # Configurações de cada política
│   └── PollyPolicies.cs                 # Factory de políticas Polly
├── Extensions/
│   └── HttpClientResilienceExtensions.cs # Extensões para DI
├── Examples/
│   └── ResilienceUsageExample.cs        # Exemplos de uso
└── DependencyInjection.cs               # Registro de serviços
```

---

## 🚀 Quick Start

### 1. **Registrar no Program.cs**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Registrar resiliência
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClientInfrastructure(builder.Configuration);

// Registrar cliente com resiliência padrão
builder.Services
    .AddResilientHttpClient("BrokerApi")
    .ConfigureHttpClient(client => 
    {
        client.BaseAddress = new Uri("https://api.broker.com");
    });

var app = builder.Build();
app.Run();
```

### 2. **Usar em um Serviço**

```csharp
public class QuoteService
{
    private readonly HttpClient _httpClient;

    public QuoteService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("BrokerApi");
    }

    public async Task<Quote> GetQuoteAsync(string symbol)
    {
        var response = await _httpClient.GetAsync($"/quotes/{symbol}");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Quote>(json);
    }
}
```

### 3. **Configurar no appsettings.json**

```json
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
    }
  }
}
```

---

## 📊 Políticas Disponíveis

### 🟢 **Standard** (Padrão)
Recomendado para a maioria dos casos.

| Configuração | Valor |
|---|---|
| Retries | 3 tentativas |
| Delay inicial | 100ms |
| Backoff | 2.0x exponencial |
| Jitter | até 100ms |
| Circuit Breaker | 5 falhas → abre |
| Timeout | 30s |
| Bulkhead | 10 paralelas, fila 50 |

**Comportamento:**
```
Tentativa 1: Falha imediato
Tentativa 2: Aguarda 100ms + jitter
Tentativa 3: Aguarda 200ms + jitter
Tentativa 4: Aguarda 400ms + jitter
```

---

### 🔴 **Aggressive** (Agressiva)
Para serviços críticos que precisam de mais tentativas.

| Configuração | Valor |
|---|---|
| Retries | 5 tentativas |
| Delay inicial | 50ms |
| Backoff | 1.5x exponencial |
| Jitter | até 50ms |
| Circuit Breaker | 5 falhas → abre |
| Timeout | 30s |
| Bulkhead | 10 paralelas, fila 50 |

---

### ⚡ **Immediate** (Imediata)
Para APIs que falham rápido ou com timeout curto.

| Configuração | Valor |
|---|---|
| Retries | 1 tentativa |
| Delay | 100ms + jitter |
| Sem Circuit Breaker |
| Sem Timeout |

---

### 🔌 **CircuitBreaker** (Apenas Circuit Breaker)
Para APIs que você quer isolar completamente.

| Configuração | Valor |
|---|---|
| Sem Retries |
| Circuit Breaker | 5 falhas → abre |
| Sem Timeout |

---

## 🔧 Configurações Detalhadas

### **ResilienceConfiguration**

```csharp
public class ResilienceConfiguration
{
    // Timeout
    public int TimeoutSeconds { get; set; } = 30;

    // Retry
    public int MaxRetries { get; set; } = 3;
    public int InitialDelayMilliseconds { get; set; } = 100;
    public double BackoffMultiplier { get; set; } = 2.0;
    public int MaxJitterMilliseconds { get; set; } = 100;

    // Circuit Breaker
    public int FailureThreshold { get; set; } = 5;
    public int CircuitBreakerTimeoutSeconds { get; set; } = 30;
    public int SuccessThreshold { get; set; } = 2;

    // Bulkhead
    public int MaxParallelization { get; set; } = 10;
    public int MaxQueued { get; set; } = 50;
}
```

---

## 📖 Mecanismos de Resiliência

### 1️⃣ **Retry com Backoff Exponencial**

Tenta novamente em intervalos crescentes:

```
Tentativa 1 (t=0s):    FALHA
Tentativa 2 (t=0.1s):  FALHA
Tentativa 3 (t=0.3s):  FALHA
Tentativa 4 (t=0.7s):  SUCESSO
```

**Fórmula:**
```
delay = InitialDelay × (BackoffMultiplier ^ tentativa)
delay_final = delay + random(0, MaxJitter)
```

**Exemplo com valores padrão:**
- Retry 1: 100ms + random(0-100ms) = **100-200ms**
- Retry 2: 200ms + random(0-100ms) = **200-300ms**
- Retry 3: 400ms + random(0-100ms) = **400-500ms**

---

### 2️⃣ **Jitter (Aleatoriedade)**

Evita "thundering herd" - quando todos fazem retry no mesmo momento:

```
❌ SEM JITTER:
t=0s:   ██████████ (100 requisições falham)
t=100ms: ██████████ (100 requisições retentam juntas)
         ⚠️  Pico de tráfego

✅ COM JITTER:
t=0s:   ██████████ (100 requisições falham)
t=50-150ms: ██ █ ███ ██ █ (requisições distribuídas)
           ✓ Distribuído no tempo
```

---

### 3️⃣ **Circuit Breaker**

Protege contra cascata de falhas:

```
Estado: CLOSED (Normal) ──────────────┐
                                      │
Requisição → Passa                    │
      ↓                               │
 5 falhas consecutivas                │
      ↓                               ↓
Estado: OPEN (Protegido)              │
                                      │
Próximas requisições                  │
      → Bloqueadas imediatamente      │
      → Sem tentar                    │
      → Por 30 segundos               │
      ↓                               │
Timeout de 30s (Tenta recuperar)      │
      ↓                               │
Estado: HALF-OPEN (Testando)          │
                                      │
Próxima requisição                    │
      → Passa (testa)                 │
      ↓                               │
   Se SUCESSO        Se FALHA         │
      ↓                 ↓             │
   CLOSED ←──────────── OPEN          │
                                      ↑
                                      └──────────
```

**Estados:**
- 🟢 **CLOSED**: Normal, todas as requisições passam
- 🔴 **OPEN**: Sob proteção, requisições bloqueadas
- 🟡 **HALF-OPEN**: Testando se serviço se recuperou

---

### 4️⃣ **Timeout**

Mata requisições que demoram demais:

```
Requisição enviada
      ↓
   0-5s: Aguardando resposta
      ↓
   5-15s: Aguardando resposta
      ↓
   15-30s: Aguardando resposta
      ↓
   30s: TIMEOUT! Requisição cancelada
```

---

### 5️⃣ **Bulkhead Isolation**

Limita paralelização para evitar sobrecargas:

```
MaxParallelization: 10 (máx 10 requisições rodando)
MaxQueued: 50 (máx 50 na fila)

Requisição 1  ┐
Requisição 2  │
Requisição 3  │
...           ├─ Executando (10 rodando)
Requisição 10 │
              ┘
Requisição 11 ┐
Requisição 12 │
...           ├─ Na fila (40 na fila)
Requisição 50 │
              ┘
Requisição 51 → ❌ REJEITADA (fila cheia)
```

---

## 🧪 Tratamento de Erros

### **Exceções Específicas**

```csharp
try
{
    var response = await _httpClient.GetAsync("/quote");
}
catch (BrokenCircuitException ex)
{
    // Circuit breaker está aberto
    logger.LogWarning("Serviço indisponível: {Message}", ex.Message);
    return GetCachedValue();
}
catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException)
{
    // Timeout!
    logger.LogError("Requisição expirou: {Message}", ex.Message);
    return GetDefaultValue();
}
catch (BulkheadRejectedException ex)
{
    // Bulkhead cheio
    logger.LogWarning("Sistema sobrecarregado: {Message}", ex.Message);
    return GetDefaultValue();
}
catch (HttpRequestException ex)
{
    // Erro de rede/HTTP
    logger.LogError(ex, "Erro HTTP: {StatusCode}", ex.StatusCode);
    throw;
}
```

---

## 🎯 Quando Usar Cada Política

| Caso de Uso | Política | Razão |
|---|---|---|
| API externa estável | **Standard** | Bom balanço entre retries e proteção |
| Pagamentos/Transações | **Aggressive** | Crítico, precisa de mais tentativas |
| API rápida/timeout curto | **Immediate** | Não faz sentido esperar muito |
| API muito instável | **CircuitBreaker** | Isola completamente após falhas |

---

## 📈 Monitoramento

As políticas registram eventos no console (pode ser integrado com logging):

```
[INFO] Retry 1 após 100ms. Status: 500
[INFO] Retry 2 após 200ms. Status: 500
[WARN] Circuit breaker aberto por 30s devido a falhas consecutivas
[INFO] Circuit breaker em estado Half-Open, testando
[INFO] Circuit breaker resetado
```

---

## 🔍 Exemplos de Código Completo

Veja `/BrokerGateway.Infrastructure/Examples/ResilienceUsageExample.cs` para exemplos práticos.

---

## 🛠️ Customização

Para criar sua própria política:

```csharp
var config = new ResilienceConfiguration
{
    TimeoutSeconds = 45,
    MaxRetries = 4,
    InitialDelayMilliseconds = 200,
    BackoffMultiplier = 1.5,
    MaxJitterMilliseconds = 50,
    FailureThreshold = 3,
    CircuitBreakerTimeoutSeconds = 60
};

var policy = PollyPolicies.GetCompositePolicy(config, ResiliencePolicy.Standard);
```

---

## 📚 Referências

- [Polly Documentation](https://github.com/App-vNext/Polly)
- [Circuit Breaker Pattern](https://martinfowler.com/bliki/CircuitBreaker.html)
- [Exponential Backoff](https://en.wikipedia.org/wiki/Exponential_backoff)
- [Bulkhead Isolation](https://www.freecodecamp.org/news/bulkhead-pattern/)

---

## ✅ Checklist de Implementação

- [x] Polly instalado (`dotnet add package Polly Polly.Extensions.Http`)
- [x] `ResilienceConfiguration` criada
- [x] `PollyPolicies` factory implementada
- [x] `HttpClientResilienceExtensions` registradas
- [x] `DependencyInjection.cs` atualizado
- [x] `appsettings.json` com configurações
- [ ] Registrar clientes HTTP no seu `Program.cs`
- [ ] Implementar tratamento de erros nos seus serviços
- [ ] Testar com falhas simuladas

---

**Criado em:** 2026-09-07  
**Versão:** 1.0  
**Autor:** Copilot
