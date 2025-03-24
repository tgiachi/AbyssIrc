using AbyssIrc.Core.Data.Configs;
using AbyssIrc.Core.Data.Directories;
using AbyssIrc.Server.Interfaces;
using AbyssIrc.Server.Services;
using AbyssIrc.Signals.Data.Configs;
using AbyssIrc.Signals.Interfaces.Services;
using AbyssIrc.Signals.Services;
using Jab;

namespace AbyssIrc.Server.ServiceProvider;

[ServiceProvider]
[Singleton<ITcpService, TcpService>]

[Singleton<IAbyssIrcSignalEmitterService, AbyssIrcSignalEmitter>]

[Singleton(typeof(AbyssIrcSignalConfig), Instance = nameof(AbyssIrcSignalConfig))]
[Singleton(typeof(DirectoriesConfig), Instance = nameof(DirectoriesConfig))]
[Singleton(typeof(AbyssIrcConfig), Instance = nameof(AbyssIrcConfig))]
public partial class AbyssIrcServerProvider
{
    public DirectoriesConfig DirectoriesConfig { get; set; }

    public AbyssIrcConfig AbyssIrcConfig { get; set; }

    public AbyssIrcSignalConfig AbyssIrcSignalConfig { get; set; }
}
