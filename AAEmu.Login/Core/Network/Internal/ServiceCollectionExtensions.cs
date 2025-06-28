using System.Net;
using AAEmu.Login.Core.Network.Connections;
using AAEmu.Login.Core.PacketHandlers;
using AAEmu.Login.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AAEmu.Login.Core.Network.Internal;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInternal(this IServiceCollection services)
    {
        return services
            .AddSingleton<IInternalProtocolHandler, InternalProtocolHandler>()
            .AddSingleton<IInternalConnectionTable, InternalConnectionTable>()
            .AddInternalPacketHandlers()
            .AddInternalNetwork();
    }

    private static IServiceCollection AddInternalNetwork(this IServiceCollection services)
    {
        return services
            .AddSingleton<InternalClientOptions>()
            .AddSingleton<IInternalClientFactory, InternalClientFactory>()
            .AddSingleton(sp =>
            {
                var config = sp.GetRequiredService<IOptions<AppConfiguration>>().Value.InternalNetwork;
                return new InternalServerOptions
                {
                    ListenAddress = config.Host.Equals("*") ? IPAddress.Any : IPAddress.Parse(config.Host),
                    Port = config.Port
                };
            })
            .AddSingleton<IInternalServer, InternalServer>()
            .AddSingleton<IInternalNetwork, InternalNetwork>();
    }
}
