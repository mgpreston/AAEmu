using Microsoft.Extensions.Logging;

namespace AAEmu.Login.Core.Network.Login;

public sealed class LoginNetwork(ILoginServer server, ILogger<LoginNetwork> logger) : ILoginNetwork
{
    public void Start()
    {
        server.Start();

        logger.LogInformation("Network started");
    }

    public async Task StopAsync()
    {
        await server.ShutdownAsync();

        logger.LogInformation("Network stopped");
    }
}
