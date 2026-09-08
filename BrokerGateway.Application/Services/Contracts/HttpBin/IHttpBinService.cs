using BrokerGateway.Contracts.Application;
using SharedKernel;

namespace BrokerGateway.Application;

public interface IHttpBinService
{
    Task<Result<BinObject>> GetQuoteAsync();
}