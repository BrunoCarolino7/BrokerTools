using BrokerGateway.Application.Messaging;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace BrokerGateway.Application.Billet;

public class CreateBilletCommandHandler(
    ILogger<CreateBilletCommandHandler> logger,
    IHttpBinService service) : ICommandHandler<CreateBilletCommand, int>
{
    public async Task<Result<int>> Handle(CreateBilletCommand command, CancellationToken cancellationToken)
    {
        var quote = await service.GetQuoteAsync();
        
        if(quote.IsFailure)
        {
            logger.LogError("Erro ao obter quote");
            return Result.Failure<int>(quote.Error);
        }

        var simulaProcessamento = 1;
        logger.LogInformation("Quantidade de Itens processados: {Quantidade}", simulaProcessamento);
        return Result.Success(simulaProcessamento);
        
    }
}