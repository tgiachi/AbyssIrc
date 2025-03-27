using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using AbyssIrc.Core.Data.Configs;
using AbyssIrc.Core.Data.Directories;
using AbyssIrc.Network.Interfaces.Parser;
using AbyssIrc.Server.Data.Events;
using AbyssIrc.Server.Data.Events.Irc;
using AbyssIrc.Server.Data.Events.TcpServer;
using AbyssIrc.Server.Interfaces.Services;
using AbyssIrc.Server.Interfaces.Services.Server;
using AbyssIrc.Server.Interfaces.Services.System;
using AbyssIrc.Server.Servers;
using AbyssIrc.Signals.Interfaces.Listeners;
using AbyssIrc.Signals.Interfaces.Services;
using NetCoreServer;
using Serilog;

namespace AbyssIrc.Server.Services;

public class TcpService
    : ITcpService, IAbyssSignalListener<SendIrcMessageEvent>, IAbyssSignalListener<DisconnectedClientSessionEvent>
{
    private readonly AbyssIrcConfig _abyssIrcConfig;
    private readonly ILogger _logger = Log.ForContext<TcpService>();

    private readonly IIrcCommandParser _commandParser;
    private readonly IAbyssSignalService _signalService;
    private readonly DirectoriesConfig _directoriesConfig;

    private readonly Dictionary<int, IrcTcpServer> _plainServers = new();
    private readonly Dictionary<int, IrcTcpSslServer> _sslServers = new();
    private readonly IIrcManagerService _ircManagerService;
    private readonly ISessionManagerService _sessionManagerService;

    private SslContext _sslContext;

    public TcpService(
        AbyssIrcConfig abyssIrcConfig, IIrcCommandParser commandParser,
        IIrcManagerService ircManagerService, IAbyssSignalService signalService,
        ISessionManagerService sessionManagerService, DirectoriesConfig directoriesConfig
    )
    {
        _abyssIrcConfig = abyssIrcConfig;
        _commandParser = commandParser;

        _ircManagerService = ircManagerService;
        _signalService = signalService;
        _sessionManagerService = sessionManagerService;
        _directoriesConfig = directoriesConfig;

        _signalService.Subscribe<SendIrcMessageEvent>(this);
        _signalService.Subscribe<DisconnectedClientSessionEvent>(this);
    }

    public async Task StartAsync()
    {
        if (!string.IsNullOrEmpty(_abyssIrcConfig.Network.SslCertPath))
        {
            var fullPath = Path.Combine(_directoriesConfig.Root, _abyssIrcConfig.Network.SslCertPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("SSL certificate not found", fullPath);
            }

            _sslContext = new SslContext(
                SslProtocols.Tls12,
                new X509Certificate2(fullPath, _abyssIrcConfig.Network.SslCertPassword)
            );
        }


        _logger.Information("Starting TCP service");

        _logger.Information("Server listening on port {Port}", _abyssIrcConfig.Network.Ports);

        foreach (var port in _abyssIrcConfig.Network.Ports.Split(','))
        {
            _plainServers.Add(
                int.Parse(port),
                new IrcTcpServer(this, _sessionManagerService, _signalService, IPAddress.Any, int.Parse(port))
            );
        }


        if (!string.IsNullOrEmpty(_abyssIrcConfig.Network.SslCertPath))
        {
            _logger.Information("Server SSL listening on port {Port}", _abyssIrcConfig.Network.SslPorts);

            foreach (var port in _abyssIrcConfig.Network.SslPorts.Split(','))
            {
                _sslServers.Add(
                    int.Parse(port),
                    new IrcTcpSslServer(
                        _sslContext,
                        this,
                        _sessionManagerService,
                        _signalService,
                        IPAddress.Any,
                        int.Parse(port)
                    )
                );
            }
        }

        foreach (var server in _plainServers.Values)
        {
            server.Start();
        }

        foreach (var server in _sslServers.Values)
        {
            server.Start();
        }
    }

    public async Task StopAsync()
    {
        foreach (var server in _plainServers.Values)
        {
            server.Stop();
        }

        foreach (var server in _sslServers.Values)
        {
            server.Stop();
        }

        _plainServers.Clear();
        _sslServers.Clear();
    }

    public async Task ParseCommandAsync(string sessionId, string command)
    {
        var parsedCommands = await _commandParser.ParseAsync(command);

        foreach (var parsedCommand in parsedCommands)
        {
            await _ircManagerService.DispatchMessageAsync(sessionId, parsedCommand);
        }
    }

    public async Task SendMessagesAsync(string sessionId, List<string> messages)
    {
        var outputMessage = string.Join("\r\n", messages);

        if (!outputMessage.EndsWith("\r\n"))
        {
            outputMessage += "\r\n";
        }

        foreach (var value in _plainServers.Values)
        {
            var tcpSession = value.FindSession(Guid.Parse(sessionId));

            tcpSession?.Send(outputMessage);
        }

        foreach (var value in _sslServers.Values)
        {
            var tcpSession = value.FindSession(Guid.Parse(sessionId));

            tcpSession?.Send(outputMessage);
        }
    }

    public void Disconnect(string sessionId)
    {
        foreach (var value in _plainServers.Values)
        {
            var tcpSession = value.FindSession(Guid.Parse(sessionId));

            if (tcpSession != null)
            {
                tcpSession.Disconnect();
            }
        }
    }

    public Task OnEventAsync(SendIrcMessageEvent signalEvent)
    {
        _signalService.PublishAsync(new IrcMessageSentEvent(signalEvent.Id, signalEvent.Message));
        return SendMessagesAsync(signalEvent.Id, [signalEvent.Message.Write()]);
    }

    public Task OnEventAsync(DisconnectedClientSessionEvent signalEvent)
    {
        Disconnect(signalEvent.Id);

        return Task.CompletedTask;
    }
}
