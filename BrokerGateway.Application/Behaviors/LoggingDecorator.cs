using BrokerGateway.Application.Messaging;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace BrokerGateway.Application.Behaviors;

internal static class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var commandName = typeof(TCommand).Name;

            logger.LogInformation("[{Command}] Start Processing command", commandName);

            var result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("[{Command}] Completed successfully", commandName);
            }
            else
            {
                // using (LogContext.PushProperty("Error", result.Error, true))
                // {
                    logger.LogError("[{Command}] Completed with error", commandName);
                // }
            }

            return result;
        }
    }

    internal sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var commandName = typeof(TCommand).Name;

            logger.LogInformation("[{Command}] Start Processing command", commandName);

            var result = await innerHandler.Handle(command, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("[{Command}] Completed successfully", commandName);
            }
            else
            {
                // using (LogContext.PushProperty("Error", result.Error, true))
                // {
                    logger.LogError("[{Command}] Completed with error", commandName);
                // }
            }

            return result;
        }
    }

    // internal sealed class QueryHandler<TQuery, TResponse>(
    //     IQueryHandler<TQuery, TResponse> innerHandler,
    //     ILogger<QueryHandler<TQuery, TResponse>> logger)
    //     : IQueryHandler<TQuery, TResponse>
    //     where TQuery : IQuery<TResponse>
    // {
    //     public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    //     {
    //         string queryName = typeof(TQuery).Name;
    //
    //         logger.LogInformation("Processing query {Query}", queryName);
    //
    //         Result<TResponse> result = await innerHandler.Handle(query, cancellationToken);
    //
    //         if (result.IsSuccess)
    //         {
    //             logger.LogInformation("Completed query {Query}", queryName);
    //         }
    //         else
    //         {
    //             using (LogContext.PushProperty("Error", result.Error, true))
    //             {
    //                 logger.LogError("Completed query {Query} with error", queryName);
    //             }
    //         }
    //
    //         return result;
    //     }
    // }
}
