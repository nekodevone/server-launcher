using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using ServerLauncher.Logger;
using ServerLauncher.Server.EventArgs;
using ServerLauncher.Server.Handlers.Enums;

namespace ServerLauncher.Server;

public class ServerSocket : IDisposable
{
    /// <summary>
    ///     Размер инта в байтах
    /// </summary>
    private const int IntSize = sizeof(int);

    public static readonly UTF8Encoding Encoding = new(false, false);

    /// <summary>
    ///     Отменитель
    /// </summary>
    private readonly CancellationTokenSource _disposeCancellationSource = new();

    /// <summary>
    ///     Слушатель
    /// </summary>
    private readonly TcpListener _listener;

    /// <summary>
    ///     Клиент
    /// </summary>
    private TcpClient _client;

    /// <summary>
    ///     Был ли диспоснут
    /// </summary>
    private readonly bool _isDisposed = false;

    /// <summary>
    ///     Сетевой поток
    /// </summary>
    private NetworkStream _networkStream;

    public ServerSocket(int port = 0)
    {
        _listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, port));
    }

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public bool IsConnected => _client?.Connected ?? false;

    public void Dispose()
    {
        if (_isDisposed) return;

        _disposeCancellationSource.Cancel();
        _disposeCancellationSource.Dispose();

        _networkStream?.Close();
        _client?.Close();
        _listener?.Stop();
    }

    public event EventHandler<MessageEventArgs> OnReceiveMessage;

    public event EventHandler<byte> OnReceiveAction;

    public void Connect()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(ServerSocket));

        _listener.Start();
        _listener.BeginAcceptTcpClient(result =>
        {
            try
            {
                _client = _listener.EndAcceptTcpClient(result);
                _client.NoDelay = true;

                _client.ReceiveBufferSize = Server.TxBufferSize;
                _client.SendBufferSize = Server.RxBufferSize;

                _networkStream = _client.GetStream();

                Task.Run(MessageListener, _disposeCancellationSource.Token);
            }
            catch (ObjectDisposedException)
            {
                // IGNORE
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }, _listener);
    }

    public void Disconnect()
    {
        Dispose();
    }

    public async void MessageListener()
    {
        // Взято из LocalAdmin
        const int offset = sizeof(int);
        var codeBuffer = new byte[1];
        var lengthBuffer = new byte[offset];
        var restartReceived = false;

        while (true)
        {
            // Задержка 10 чтобы не было бесконечно лупа.
            await Task.Delay(10);

            // Если диспоснут или нет сетевого потока, то выходим из цикла.
            if (_isDisposed || _networkStream is null)
            {
                return;
            }
            
            // Если нет данных, то пропускаем итерацию.
            if (!_networkStream.DataAvailable)
            {
                continue;
            }

            // Читаем длину сообщения.
            var readAmount = await _networkStream.ReadAsync(codeBuffer.AsMemory(0, 1));

            if (readAmount == 0)
            {
                continue;
            }

            if (codeBuffer[0] < 16)
            {
                readAmount = await _networkStream.ReadAsync(lengthBuffer.AsMemory(0, offset));

                if (readAmount < 4)
                {
                    continue;
                }

                var length = MemoryMarshal.Cast<byte, int>(lengthBuffer)[0];
                var buffer = ArrayPool<byte>.Shared.Rent(length);

                while (_client.Available < length)
                    await Task.Delay(20);

                readAmount = await _networkStream.ReadAsync(buffer.AsMemory(0, length));

                if (readAmount != length)
                {
                    continue;
                }

                var message = $"{codeBuffer[0]:X}{Encoding.GetString(buffer, 0, length)}";
                ArrayPool<byte>.Shared.Return(buffer);

                Log.Info(message);
            }
            else
            {
                switch ((OutputCodes)codeBuffer[0])
                {
                    case OutputCodes.RoundRestart:
                        break;
                    case OutputCodes.IdleEnter:
                        break;
                    case OutputCodes.IdleExit:
                        break;
                    case OutputCodes.ExitActionReset:
                        break;
                    case OutputCodes.ExitActionShutdown:
                        break;
                    case OutputCodes.ExitActionSilentShutdown:
                        break;
                    case OutputCodes.ExitActionRestart:
                        break;
                    case OutputCodes.Heartbeat:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
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
            Log.Error(e.ToString());
        }
    }
}