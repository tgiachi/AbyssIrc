using AbyssIrc.Server.Data.Internal;
using AbyssIrc.Server.Data.Internal.Sessions;

namespace AbyssIrc.Server.Interfaces.Services.System;

public interface ISessionManagerService
{
    void AddSession(string id, string ipEndpoint, IrcSession? session = null);

    IrcSession? GetSession(string id);

    List<IrcSession> GetSessions();
}
