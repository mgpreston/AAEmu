using System.Diagnostics.CodeAnalysis;
using System.Net;
using AAEmu.Commons.Models;
using AAEmu.Commons.Network.Core;
using AAEmu.Login.Core.Network.Login;
using AAEmu.Login.Models;

namespace AAEmu.Login.Core.Network.Connections;

public class LoginConnection(ISession session)
{
    public ConnectionId Id => new(session.SessionId);
    public IPAddress Ip => session.Ip;

    public AccountId AccountId { get; private set; }
    public string? AccountName { get; private set; }
    public DateTime LastLogin { get; private set; }
    public IPAddress? LastIp { get; private set; }
    public bool IsLocallyConnected { get; } = IsSameMachine(session);

    public Dictionary<GameServerId, List<LoginCharacterInfo>> Characters { get; } = [];

    private static bool IsSameMachine(ISession session)
    {
        // checks if a connection is from the same machine
        var localIp = ((IPEndPoint?)session.Socket?.LocalEndPoint)?.Address;
        var remoteIp = ((IPEndPoint?)session.Socket?.RemoteEndPoint)?.Address;
        return Equals(localIp, remoteIp);
    }

    public void SendPacket(LoginPacket packet) => session.TrySend(packet.Encode());

    public void Shutdown() => session.Close();

    public List<LoginCharacterInfo> GetCharacters()
    {
        var res = new List<LoginCharacterInfo>();
        foreach (var characters in Characters.Values)
        {
            res.AddRange(characters);
        }
        return res;
    }

    public void AddCharacters(GameServerId gsId, List<LoginCharacterInfo> characterInfos)
    {
        foreach (var character in characterInfos)
            character.GsId = gsId.Value;
        Characters.Add(gsId, characterInfos);
    }

    [MemberNotNull(nameof(LastIp))]
    [MemberNotNull(nameof(AccountName))]
    public void OnLogin(AccountId accountId, string username, DateTime lastLogin)
    {
        AccountId = accountId;
        AccountName = username;
        LastLogin = lastLogin;
        LastIp = Ip;
    }
}
