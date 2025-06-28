using AAEmu.Commons.Network.Core;
using Microsoft.Extensions.Logging;

namespace AAEmu.Login.Core.Network.Login;

public class LoginServer(
    LoginServerOptions options,
    ILoginProtocolHandler protocolHandler,
    ILoginClientFactory clientFactory,
    ILogger<LoginServer> logger) : TcpPipelineServer(options, protocolHandler, clientFactory, logger), ILoginServer;
