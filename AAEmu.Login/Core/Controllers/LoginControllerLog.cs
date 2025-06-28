using Microsoft.Extensions.Logging;

namespace AAEmu.Login.Core.Controllers;

public static partial class LoginControllerLog
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Database update failed, error occurred while updating account login IP and time")]
    public static partial void LoginDatabaseUpdateFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "{accountName} connected.")]
    public static partial void PlayerConnected(ILogger logger, string accountName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Created account from invalid username login with value: {username}")]
    public static partial void AccountCreated(ILogger logger, string username);
}
