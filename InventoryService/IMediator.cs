using Microsoft.Extensions.Logging;
using Shared.Models;

namespace InventoryService.CQRS;

public interface ICommandHandler<TCommand> where TCommand : ICommand
{
    Task<CommandResult> HandleAsync(TCommand command);
}

public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult?> HandleAsync(TQuery query);
}

public interface IMediator
{
    Task<CommandResult> SendAsync<TCommand>(TCommand command) where TCommand : ICommand;
    Task<TResult?> QueryAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;
}

public class ServiceMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceMediator> _logger;

    public ServiceMediator(IServiceProvider serviceProvider, ILogger<ServiceMediator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<CommandResult> SendAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        var commandType = typeof(TCommand);
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            var message = $"No handler found for command {commandType.Name}";
            _logger.LogError(message);
            return CommandResult.CreateFailure(message);
        }

        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
        {
            var message = $"HandleAsync method not found on handler for {commandType.Name}";
            _logger.LogError(message);
            return CommandResult.CreateFailure(message);
        }

        try
        {
            var result = method.Invoke(handler, new[] { (object)command });
            if (result is Task<CommandResult> taskResult)
            {
                return await taskResult;
            }
            return CommandResult.CreateFailure("Invalid command handler result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing command {commandType.Name}");
            return CommandResult.CreateFailure($"Command execution failed: {ex.Message}");
        }
    }

    public async Task<TResult?> QueryAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>
    {
        var queryType = typeof(TQuery);
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null) return default;

        var method = handlerType.GetMethod("HandleAsync");
        if (method == null) return default;

        try
        {
            var result = method.Invoke(handler, new[] { (object)query });
            if (result is Task<TResult?> taskResult) return await taskResult;
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing query {queryType.Name}");
            return default;
        }
    }
}