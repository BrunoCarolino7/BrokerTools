using System.Text.Json;
using BrokerGateway.Application;
using BrokerGateway.Contracts.Application;
using BrokerGateway.Infrastructure.Resilience;
using BrokerGateway.Infrastructure.Services.HttpBin;
using SharedKernel;

namespace BrokerGateway.Infrastructure.ExternalServices.HttpBin;

public sealed class HttpBinService : IHttpBinService
{
    private readonly HttpClient _httpClient;

    public HttpBinService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient(Clients.Bin);
    }

    public async Task<Result<BinObject>> GetQuoteAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("status/429");

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var externalResponse =
                JsonSerializer.Deserialize<HttpBinResponse>(content);

            if (externalResponse is null)
                return Result.Failure<BinObject>(Error.Problem("429","Resposta inválida do HttpBin."));

            var result = HttpBinMapper.ToApplication(externalResponse);

            return result;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine(
                $"Erro ao obter quote: {ex.Message}");

            throw;
        }
    }
}