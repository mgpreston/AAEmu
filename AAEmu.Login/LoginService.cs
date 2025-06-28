using AAEmu.Commons.Utils.DB;
using AAEmu.Commons.Utils.Updater;
using AAEmu.Login.Core.Controllers;
using AAEmu.Login.Core.Network.Internal;
using AAEmu.Login.Core.Network.Login;
using AAEmu.Login.Models;
using AAEmu.Login.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;

namespace AAEmu.Login;

public sealed class LoginService(
    IGameController gameController,
    IRequestController requestController,
    IInternalNetwork internalNetwork,
    ILoginNetwork loginNetwork,
    IOptions<AppConfiguration> appConfig,
    IDbContextFactory<LoginDbContext> dbContextFactory,
    ILogger<LoginService> logger) : IHostedService, IDisposable
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting daemon: AAEmu.Login");
        // Check for updates
        using (var connection = MySQL.CreateConnection())
        {
            if (!MySqlDatabaseUpdater.Run(connection, "aaemu_login",
                    appConfig.Value.Connections.MySQLProvider.Database))
            {
                logger.LogCritical("Failed to update database!");
                logger.LogCritical("Press Ctrl+C to quit");
                return;
            }
        }

        // Apply EF Core migrations after the old-style updates
        logger.LogDebug("Performing EF Core migrations...");
        await using (var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken))
        {
            // Ensure database is created and migrations are applied
            await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
        }
        logger.LogDebug("EF Core migrations done");

        requestController.Initialize();
        await gameController.LoadAsync();
        loginNetwork.Start();
        internalNetwork.Start();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping daemon.");
        await loginNetwork.StopAsync();
        await internalNetwork.StopAsync();
    }

    public void Dispose()
    {
        logger.LogInformation("Disposing....");
        LogManager.Flush();
    }
}
