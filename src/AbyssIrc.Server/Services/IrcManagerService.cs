using AbyssIrc.Network.Interfaces.Commands;
using AbyssIrc.Server.Data.Events.Irc;
using AbyssIrc.Server.Data.Internal.ServiceCollection;
using AbyssIrc.Server.Interfaces.Listener;
using AbyssIrc.Server.Interfaces.Services.Server;
using AbyssIrc.Signals.Interfaces.Services;
using Microsoft.Extensions.Logging;


namespace AbyssIrc.Server.Services;

public class IrcManagerService : IIrcManagerService
{
    private readonly IAbyssSignalService _signalService;
    private readonly ILogger _logger;

    private readonly Dictionary<string, List<IIrcMessageListener>> _listeners = new();

    private readonly List<IrcHandlerDefinitionData> _ircHandlers;

    private readonly IServiceProvider _serviceProvider;

    public IrcManagerService(
        ILogger<IrcManagerService> logger, IAbyssSignalService signalService, List<IrcHandlerDefinitionData> ircHandlers,
        IServiceProvider serviceProvider
    )
    {
        _signalService = signalService;
        _ircHandlers = ircHandlers;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchMessageAsync(string id, IIrcCommand command)
    {
        await _signalService.PublishAsync(new IrcMessageReceivedEvent(id, command));


        if (_listeners.TryGetValue(command.Code, out var listeners))
        {
            foreach (var listener in listeners)
            {
                await listener.OnMessageReceivedAsync(id, command);
            }
        }
    }

    public void RegisterListener(IIrcCommand command, IIrcMessageListener listener)
    {
        if (!_listeners.ContainsKey(command.Code))
        {
            _listeners.Add(command.Code, []);
        }

        _logger.LogDebug(
            "Registering listener for command '{Command}'({Code}) with listener '{Listener}'",
            command.GetType().Name,
            command.Code,
            listener.GetType().Name
        );

        _listeners[command.Code].Add(listener);
    }

    public void RegisterListener(string commandCode, Func<string, IIrcCommand, Task> callback)
    {
        if (string.IsNullOrEmpty(commandCode))
        {
            throw new ArgumentNullException(nameof(commandCode), "Command code cannot be null or empty");
        }

        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback), "Callback function cannot be null");
        }

        if (!_listeners.ContainsKey(commandCode))
        {
            _listeners.Add(commandCode, new List<IIrcMessageListener>());
        }

        // Create a wrapper that implements IIrcMessageListener
        var callbackWrapper = new IrcCallbackListener(callback);

        _logger.LogDebug(
            "Registering callback listener for command code '{Code}'",
            commandCode
        );

        _listeners[commandCode].Add(callbackWrapper);
    }

    public Task StartAsync()
    {
        foreach (var ircHandler in _ircHandlers)
        {
            _logger.LogDebug("Starting handler '{Handler}'", ircHandler.HandlerType.Name);
            _serviceProvider.GetService(ircHandler.HandlerType);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}
