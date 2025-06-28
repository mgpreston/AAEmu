using AAEmu.Commons.Network.Core;
using Microsoft.Extensions.Logging;

namespace AAEmu.Login.Core.Network.Login;

public class LoginClientFactory(LoginClientOptions clientOptions, ILoggerFactory loggerFactory)
    : ClientFactory(clientOptions, loggerFactory), ILoginClientFactory;
