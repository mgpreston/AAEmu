using System.Net;
using AAEmu.Commons.Network.Core;
using AAEmu.Login.Core.Network.Internal;
using AAEmu.Login.Models;

namespace AAEmu.Login.Core.Network.Connections;

public class InternalConnection(ISession session)
{
    public uint Id => session.SessionId;
    public IPAddress Ip => session.Ip;
    public GameServer? GameServer { get; set; }

    public void SendPacket(InternalPacket packet)
    {
        packet.Connection = this;
        session.TrySend(packet.Encode());
    }

    public void AddAttribute(string name, object value) => session.AddAttribute(name, value);

    public void Shutdown() => session.Close();
}
