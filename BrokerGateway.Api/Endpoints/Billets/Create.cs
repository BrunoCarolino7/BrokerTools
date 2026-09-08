using BrokerGateway.Api.Extensions;
using BrokerGateway.Api.Infrastructure;
using BrokerGateway.Application.Billet;
using BrokerGateway.Application.Messaging;
using SharedKernel;

namespace BrokerGateway.Api.Endpoints.Billets;

public class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("todo", async (
                CreateBilletCommand command,
                ICommandHandler<CreateBilletCommand, int> commandHandler,
                CancellationToken cancellationToken) =>
            {
                var result = await commandHandler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithTags(Tags.OrdersAssets);
    }
}