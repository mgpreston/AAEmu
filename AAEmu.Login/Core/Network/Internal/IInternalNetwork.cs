namespace AAEmu.Login.Core.Network.Internal;

public interface IInternalNetwork
{
    void Start();
    Task StopAsync();
}
