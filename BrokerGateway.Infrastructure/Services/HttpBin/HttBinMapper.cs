using BrokerGateway.Contracts.Application;

namespace BrokerGateway.Infrastructure.Services.HttpBin;

internal static class HttpBinMapper
{
    public static BinObject ToApplication(HttpBinResponse response)
    {
        var binObject = new BinObject();
        binObject.SetData(response.Data);
        return binObject;
    }
}