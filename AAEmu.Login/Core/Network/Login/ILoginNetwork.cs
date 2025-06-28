namespace AAEmu.Login.Core.Network.Login;

public interface ILoginNetwork
{
    void Start();
    Task StopAsync();
}
