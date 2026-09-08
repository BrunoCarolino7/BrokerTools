namespace BrokerGateway.Contracts.Application;

public class BinObject
{
    public string Data { get; private set; }

    public void SetData(string data)
    {
        Data = data;
    }
}