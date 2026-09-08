using System.Windows.Input;
using BrokerGateway.Application.Messaging;

namespace BrokerGateway.Application.Billet;

public sealed record CreateBilletCommand(int Id, string Description) : ICommand<int>;