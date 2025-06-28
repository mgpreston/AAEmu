using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using AAEmu.Login.Core.Network.Connections;
using AAEmu.Login.Core.Network.Internal;
using AAEmu.Login.Core.Packets.L2C;
using AAEmu.Login.Core.Packets.L2G;
using AAEmu.Login.Models;
using AAEmu.Login.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using GameServer = AAEmu.Login.Models.GameServer;

namespace AAEmu.Login.Core.Controllers;

public class GameController(
    IRequestController requestController,
    IOptions<AppConfiguration> appConfig,
    IDbContextFactory<LoginDbContext> dbFactory,
    ILogger<GameController> logger) : IGameController
{
    private readonly ConcurrentDictionary<GameServerId, GameServer> _gameServers = [];
    private readonly Dictionary<GameServerId, GameServerId> _mirrorsId = [];

    public bool TryGetParentId(GameServerId gsId, out GameServerId id) => _mirrorsId.TryGetValue(gsId, out id);

    private static async Task SendPacketWithDelay(InternalConnection connection, int delay, InternalPacket message)
    {
        await Task.Delay(delay);
        connection.SendPacket(message);
    }

    private async Task<string> ResolveHostName(string host)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork);
            if (addresses is [var firstIPv4Address, ..])
            {
                logger.LogDebug("Resolved {Host} to {FirstIPv4Address}", host, firstIPv4Address);
                return firstIPv4Address.ToString();
            }

            logger.LogWarning("Unable to resolve {Host}", host);
            return host;
        }
        catch (Exception e)
        {
            // in case of errors, just return it un-parsed
            logger.LogError(e, "Exception resolving {Host}", host);
            return host;
        }
    }

    public async Task LoadAsync()
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        var gameServers = await dbContext.GameServers
            .AsNoTracking()
            .Where(gs => !gs.Hidden)
            .ToListAsync();

        foreach (var dbGameServer in gameServers)
        {
            var host = appConfig.Value.SkipHostResolve ? dbGameServer.Host : await ResolveHostName(dbGameServer.Host);
            var gameServer = new GameServer(dbGameServer.Id, dbGameServer.Name, host, dbGameServer.Port);
            if (!_gameServers.TryAdd(gameServer.Id, gameServer))
            {
                logger.LogError("Game Server {Id} ({Name}) already exists in the game_servers table!",
                    gameServer.Id.Value,
                    gameServer.Name);
            }

            var extraInfo = host != dbGameServer.Host ? "from " + dbGameServer.Host :
                appConfig.Value.SkipHostResolve ? " (unresolved)" : "";
            logger.LogInformation("Game Server {Id}: {Name} -> {Host}:{Port} {ExtraInfo}", dbGameServer.Id.Value,
                dbGameServer.Name, host, dbGameServer.Port, extraInfo);
        }

        if (_gameServers.IsEmpty)
        {
            logger.LogCritical("No servers have been defined in the game_servers table!");
            return;
        }

        logger.LogInformation("Loaded {GameServersCount} game server(s)", _gameServers.Count);
    }

    public void Add(GameServerId gsId, List<GameServerId> mirrorsId, InternalConnection connection)
    {
        if (!_gameServers.TryGetValue(gsId, out var gameServer))
        {
            logger.LogError("GameServer connection from {ConnectionIp} is requesting an invalid WorldId {GameServerId}",
                connection.Ip, gsId);

            _ = SendPacketWithDelay(connection, 5000, new LGRegisterGameServerPacket(GSRegisterResult.Error));
            // connection.SendPacket(new LGRegisterGameServerPacket(GSRegisterResult.Error));
            return;
        }

        gameServer.Connection = connection;
        gameServer.MirrorsId.AddRange(mirrorsId);
        connection.GameServer = gameServer;
        connection.AddAttribute("gsId", gameServer.Id);
        gameServer.SendPacket(new LGRegisterGameServerPacket(GSRegisterResult.Success));

        foreach (var mirrorId in mirrorsId)
        {
            _gameServers[mirrorId].Connection = connection;
            _mirrorsId.Add(mirrorId, gsId);
        }

        logger.LogInformation("Registered GameServer {IdValue} ({GameServerName}) from {ConnectionIp}",
            gameServer.Id.Value, gameServer.Name, connection.Ip);
    }

    public void Remove(GameServerId gsId)
    {
        if (!_gameServers.TryGetValue(gsId, out var gameServer))
            return;
        gameServer.Connection = null;

        foreach (var mirrorId in gameServer.MirrorsId)
        {
            if (_gameServers.TryGetValue(mirrorId, out var server))
                server.Connection = null;

            _mirrorsId.Remove(mirrorId);
        }

        gameServer.MirrorsId.Clear();
    }

    public async Task RequestWorldListAsync(LoginConnection connection)
    {
        var gameServers = _gameServers.Values.ToList();
        if (_gameServers.Values.Any(x => x.Active))
        {
            var (requestIds, creationTask) =
                requestController.Create(gameServers.Count, 20000); // TODO Request 20s
            for (var i = 0; i < gameServers.Count; i++)
            {
                var value = gameServers[i];
                if (!value.Active)
                {
                    requestController.ReleaseId(requestIds[i]);
                    continue;
                }

                var loaded = connection.Characters.ContainsKey(value.Id);
                if (loaded)
                {
                    requestController.ReleaseId(requestIds[i]);
                    continue;
                }

                value.SendPacket(
                    new LGRequestInfoPacket(connection.Id, requestIds[i], connection.AccountId));

            }

            await creationTask;
        }

        connection.SendPacket(new ACWorldListPacket(gameServers, connection.GetCharacters()));
    }

    public void SetLoad(GameServerId gsId, byte load)
    {
        _gameServers[gsId].Load = (GSLoad)load;
    }

    public void RequestEnterWorld(LoginConnection connection, GameServerId gsId)
    {
        if (!_gameServers.TryGetValue(gsId, out var gs))
            return;
        if (!gs.Active)
            return;
        gs.SendPacket(new LGPlayerEnterPacket(connection.AccountId, connection.Id));
    }

    public void EnterWorld(LoginConnection connection, GameServerId gsId, byte result)
    {
        switch (result)
        {
            case 0 when _gameServers.TryGetValue(gsId, out var server):
                connection.SendPacket(new ACWorldCookiePacket(connection, server));
                break;
            case 0:
                // TODO ...
                break;
            case 1:
                connection.SendPacket(new ACEnterWorldDeniedPacket(0)); // TODO change reason
                break;
            default:
                // TODO ...
                break;
        }
    }
}
