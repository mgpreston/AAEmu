using System.Collections.Concurrent;
using AAEmu.Login.Core.Network.Connections;
using AAEmu.Login.Core.Packets.L2C;
using AAEmu.Login.Core.Packets.L2G;
using AAEmu.Login.Models;
using AAEmu.Login.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AAEmu.Login.Core.Controllers;

public class LoginController(
    IGameController gameController,
    IOptions<AppConfiguration> appConfig,
    IDbContextFactory<LoginDbContext> dbFactory,
    TimeProvider timeProvider,
    ILogger<LoginController> logger) : ILoginController
{
    private readonly bool _autoAccount = appConfig.Value.AutoAccount;

    private readonly ConcurrentDictionary<GameServerId, ConcurrentDictionary<uint, AccountId>>
        _tokens = []; // gsId, [token, accountId]

    /// <summary>
    /// Kr Method Auth
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="username"></param>
    public void Login(LoginConnection connection, string username)
    {
        using var dbContext = dbFactory.CreateDbContext();
        var user = dbContext.Users
            .FirstOrDefault(u => u.Username == username);

        if (user == null)
        {
            connection.SendPacket(new ACLoginDeniedPacket(2));
            return;
        }

        // TODO ... validation password

        connection.OnLogin(user.Id, user.Username, timeProvider.GetUtcNow().UtcDateTime);

        connection.SendPacket(new ACJoinResponsePacket(0, 6));
        connection.SendPacket(new ACAuthResponsePacket(connection.AccountId, 6));

        user.LastIp = connection.LastIp.ToString();
        user.LastLogin = connection.LastLogin;
        user.UpdatedAt = connection.LastLogin;

        try
        {
            dbContext.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            LoginControllerLog.LoginDatabaseUpdateFailed(logger, ex);
        }
    }

    /// <summary>
    /// Eu Method Auth
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    public async Task LoginAsync(LoginConnection connection, string username, ReadOnlyMemory<byte> password)
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            if (_autoAccount)
            {
                user = await CreateAndLoginInvalid(dbContext, connection, username, password);

                // Failed to create account
                if (user == null)
                {
                    return;
                }
            }
            else
            {
                connection.SendPacket(new ACLoginDeniedPacket(2));
                return;
            }
        }

        var expectedPassword = Convert.FromBase64String(user.Password);
        if (!password.Span.SequenceEqual(expectedPassword))
        {
            connection.SendPacket(new ACLoginDeniedPacket(2));
            return;
        }

        if (user.Banned)
        {
            connection.SendPacket(new ACLoginDeniedPacket(user.BanReason));
            return;
        }

        connection.OnLogin(user.Id, username, timeProvider.GetUtcNow().UtcDateTime);

        LoginControllerLog.PlayerConnected(logger, connection.AccountName);
        connection.SendPacket(new ACJoinResponsePacket(0, 6));
        connection.SendPacket(new ACAuthResponsePacket(connection.AccountId, 6));

        user.LastIp = connection.LastIp.ToString();
        user.LastLogin = connection.LastLogin;
        user.UpdatedAt = connection.LastLogin;

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            LoginControllerLog.LoginDatabaseUpdateFailed(logger, ex);
        }
    }

    private async Task<User?> CreateAndLoginInvalid(LoginDbContext dbContext, LoginConnection connection,
        string username,
        ReadOnlyMemory<byte> password)
    {
        var pass = Convert.ToBase64String(password.Span);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var newUser = new User
        {
            Username = username,
            Password = pass,
            Email = "",
            LastIp = connection.Ip.ToString(),
            LastLogin = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Users.Add(newUser);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            connection.SendPacket(new ACLoginDeniedPacket(2));
            return null;
        }

        LoginControllerLog.AccountCreated(logger, username);
        return newUser;
    }

    public void AddReconnectionToken(InternalConnection connection, GameServerId gsId, AccountId accountId, uint token)
    {
        var tokensForGameServer = _tokens.GetOrAdd(gsId, static _ => []);
        tokensForGameServer.TryAdd(token, accountId);
        connection.SendPacket(new LGPlayerReconnectPacket(token));
    }

    public void Reconnect(LoginConnection connection, GameServerId gsId, AccountId accountId, uint token)
    {
        if (!_tokens.TryGetValue(gsId, out var tokensForGameServer))
        {
            if (!gameController.TryGetParentId(gsId, out var parentGameServerId))
            {
                // TODO ...
                return;
            }

            gsId = parentGameServerId;
            tokensForGameServer = _tokens[gsId];
        }

        if (!tokensForGameServer.TryGetValue(token, out var storedAccountIdForToken))
        {
            // TODO ...
            return;
        }

        if (storedAccountIdForToken == accountId)
        {
            connection.SendPacket(new ACJoinResponsePacket(0, 6));
            connection.SendPacket(new ACAuthResponsePacket(connection.AccountId, 6));
        }
        else
        {
            // TODO ...
        }
    }
}
