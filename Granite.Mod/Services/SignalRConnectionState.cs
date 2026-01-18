using System.Threading;

namespace GraniteServer.Services;

public class SignalRConnectionState
{
    private int _isConnected; // 0 = false, 1 = true

    public bool IsConnected => _isConnected == 1;

    public void SetConnected(bool connected)
    {
        Interlocked.Exchange(ref _isConnected, connected ? 1 : 0);
    }
}
