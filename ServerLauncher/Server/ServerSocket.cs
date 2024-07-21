using System.Net;
using System.Net.Sockets;
using System.Text;
using ServerLauncher.Server.EventArgs;

namespace ServerLauncher.Server;

public class ServerSocket : IDisposable
{
    public static readonly UTF8Encoding Encoding = new(false, true);

    public ServerSocket(int port = 0)
    {
        _listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, port));
    }

    public event EventHandler<MessageEventArgs> OnReceiveMessage;
    
    public event EventHandler<byte> OnReceiveAction;
    
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
    
    public bool IsConnected => _client?.Connected ?? false;
    
    /// <summary>
    /// Отменитель
    /// </summary>
    private readonly CancellationTokenSource _disposeCancellationSource = new CancellationTokenSource();
    
    /// <summary>
    /// Был ли диспоснут
    /// </summary>
    private bool _isDisposed = false;

    /// <summary>
    /// Слушатель
    /// </summary>
    private readonly TcpListener _listener;
    
    /// <summary>
    /// Клиент
    /// </summary>
    private TcpClient _client;
    
    /// <summary>
    /// Сетевой поток
    /// </summary>
    private NetworkStream _networkStream;
    
    /// <summary>
    /// Размер инта в байтах
    /// </summary>
    private const int IntSize = sizeof(int);

    public void Connect()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(ServerSocket));
        }
            
        _listener.Start();
        _listener.BeginAcceptTcpClient(result =>
        {
            try
            {
                _client = _listener.EndAcceptTcpClient(result);
                _networkStream = _client.GetStream();

                Task.Run(MessageListener, _disposeCancellationSource.Token);
            }
            catch (ObjectDisposedException)
            {
                // IGNORE
            }
            catch (Exception e)
            {
                Program.Logger.Error(nameof(Connect), e.ToString());
            }
            
        }, _listener);
    }

    public void Disconnect()
    {
        Dispose();
    }
    
    public async void MessageListener()
    {
        var typeBuffer = new byte[1];
        var intBuffer = new byte[IntSize];

        while (!_isDisposed && _networkStream is not null)
        {
            var messageTypeBytesRead = await _networkStream.ReadAsync(typeBuffer, 0, 1, _disposeCancellationSource.Token);

            if (messageTypeBytesRead <= 0)
            {
                Disconnect();
                break;
            }

            var messageType = typeBuffer[0];

            if (messageType >= 16)
            {
                OnReceiveAction?.Invoke(this, messageType);
            }
            
            var lengthBytesRead = await _networkStream.ReadAsync(intBuffer, 0, IntSize, _disposeCancellationSource.Token);
            
            if (lengthBytesRead != IntSize)
            {
                Disconnect();
                break;
            }
            
            var length = (intBuffer[0] << 24) | (intBuffer[1] << 16) | (intBuffer[2] << 8) | intBuffer[3];
            
            switch (length)
            {
                case 0:
                    OnReceiveMessage?.Invoke(this, new MessageEventArgs("", messageType));
                    break;
                case < 0:
                    OnReceiveMessage?.Invoke(this, new MessageEventArgs(null, messageType));
                    break;
            }

            var messageBuffer = new byte[length];
            var messageBytesRead = await _networkStream.ReadAsync(messageBuffer, 0, length, _disposeCancellationSource.Token);

            // Socket has been disconnected
            if (messageBytesRead <= 0)
            {
                Disconnect();
                break;
            }

            var message = Encoding.GetString(messageBuffer, 0, length);

            OnReceiveMessage?.Invoke(this, new MessageEventArgs(message, messageType));
        }
    }
    
    public void SendMessage(string message)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(ServerSocket));

        if (_networkStream is null)
            throw new NullReferenceException($"{nameof(_networkStream)} hasn't been initialized");

        var messageBuffer = new byte[Encoding.GetMaxByteCount(message.Length) + IntSize];

        var actualMessageLength = Encoding.GetBytes(message, 0, message.Length, messageBuffer, IntSize);
        Array.Copy(BitConverter.GetBytes(actualMessageLength), messageBuffer, IntSize);

        try
        {
            _networkStream.Write(messageBuffer, 0, actualMessageLength + IntSize);
        }
        catch (Exception e)
        {
            Program.Logger.Error(nameof(SendMessage), e.ToString());
        }
    }
    
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }
        
        _disposeCancellationSource.Cancel();
        _disposeCancellationSource.Dispose();
        
        _networkStream?.Close();
        _client?.Close();
        _listener?.Stop();
    }
}