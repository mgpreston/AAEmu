using System.Net;
using AAEmu.Login.Core.Network.Connections;
using AAEmu.Login.Core.PacketHandlers;
using AAEmu.Login.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AAEmu.Login.Core.Network.Login;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogin(this IServiceCollection services)
    {
        return services
            .AddSingleton<ILoginProtocolHandler, LoginProtocolHandler>()
            .AddSingleton<ILoginConnectionTable, LoginConnectionTable>()
            .AddLoginPacketHandlers()
            .AddLoginNetwork();
    }

    private static IServiceCollection AddLoginNetwork(this IServiceCollection services)
    {
        return services
            .AddSingleton<LoginClientOptions>()
            .AddSingleton<ILoginClientFactory, LoginClientFactory>()
            .AddSingleton(sp =>
            {
                var config = sp.GetRequiredService<IOptions<AppConfiguration>>().Value.Network;
                return new LoginServerOptions
                {
                    ListenAddress = config.Host.Equals("*") ? IPAddress.Any : IPAddress.Parse(config.Host),
                    Port = config.Port
                };
            })
            .AddSingleton<ILoginServer, LoginServer>()
            .AddSingleton<ILoginNetwork, LoginNetwork>();
    }
}
