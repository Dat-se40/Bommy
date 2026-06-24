using Nakama;
using System;
using System.Threading.Tasks;

internal sealed class NakamaSocketService
{
    readonly IClient client;

    public ISocket Socket { get; private set; }
    public bool IsConnected { get; private set; }

    public NakamaSocketService(IClient client)
    {
        this.client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public ISocket EnsureSocket()
    {
        if (Socket != null)
            return Socket;

        Socket = client.NewSocket(useMainThread: true);
        Socket.Connected += OnSocketConnected;
        Socket.Closed += OnSocketClosed;
        Socket.ReceivedError += OnSocketError;
        return Socket;
    }

    public async Task<ISocket> ConnectAsync(ISession session)
    {
        if (session == null)
            throw new InvalidOperationException("Nakama authentication is not available.");

        ISocket socket = EnsureSocket();

        if (IsConnected)
            return socket;

        await socket.ConnectAsync(session);
        IsConnected = true;
        return socket;
    }

    public async Task DisconnectAsync()
    {
        IsConnected = false;

        if (Socket != null)
            await Socket.CloseAsync();
    }

    public void CloseAndRelease()
    {
        if (Socket != null)
            _ = Socket.CloseAsync();

        Release();
    }

    public void Release()
    {
        if (Socket == null)
            return;

        Socket.Connected -= OnSocketConnected;
        Socket.Closed -= OnSocketClosed;
        Socket.ReceivedError -= OnSocketError;
        Socket = null;
        IsConnected = false;
    }

    void OnSocketConnected()
    {
        IsConnected = true;
    }

    void OnSocketClosed(string reason)
    {
        IsConnected = false;
    }

    void OnSocketError(Exception exception)
    {
        IsConnected = false;
    }
}
