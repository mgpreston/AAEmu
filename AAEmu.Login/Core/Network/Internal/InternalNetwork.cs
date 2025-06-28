using Microsoft.Extensions.Logging;

namespace AAEmu.Login.Core.Network.Internal;

public class InternalNetwork(IInternalServer server, ILogger<InternalNetwork> logger) : IInternalNetwork
{
    public void Start()
    {
        server.Start();

        logger.LogInformation("InternalNetwork started");
    }

    public async Task StopAsync()
    {
        await server.ShutdownAsync();

        logger.LogInformation("InternalNetwork stopped");
    }
}
