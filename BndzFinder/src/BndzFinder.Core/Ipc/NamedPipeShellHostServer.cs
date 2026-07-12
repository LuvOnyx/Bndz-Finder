using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace BndzFinder.Core.Ipc;

public sealed class NamedPipeShellHostServer : IShellHostServer, IAsyncDisposable
{
    private readonly List<Stream> _clients = [];
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;

    public event EventHandler<ShellHostMessage>? MessageReceived;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (OperatingSystem.IsWindows())
        {
            _acceptLoop = AcceptWindowsPipesAsync(_cts.Token);
        }
        else
        {
            _acceptLoop = AcceptUnixSocketAsync(_cts.Token);
        }
        await Task.CompletedTask;
    }

    public async Task BroadcastAsync(ShellHostMessage message, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message, ShellHostJson.Options);
        var payload = System.Text.Encoding.UTF8.GetBytes(json);
        var header = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

        foreach (var client in _clients.ToArray())
        {
            try
            {
                await client.WriteAsync(header, cancellationToken).ConfigureAwait(false);
                await client.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                await client.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _clients.Remove(client);
            }
        }
    }

    private async Task AcceptWindowsPipesAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var server = new System.IO.Pipes.NamedPipeServerStream(
                ShellHostPipeProtocol.PipeName,
                System.IO.Pipes.PipeDirection.InOut,
                System.IO.Pipes.NamedPipeServerStream.MaxAllowedServerInstances,
                System.IO.Pipes.PipeTransmissionMode.Byte,
                System.IO.Pipes.PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync(ct).ConfigureAwait(false);
            _clients.Add(server);
            _ = HandleClientAsync(server, ct);
        }
    }

    private async Task AcceptUnixSocketAsync(CancellationToken ct)
    {
        var socketPath = Path.Combine(Path.GetTempPath(), $"{ShellHostPipeProtocol.PipeName}.sock");
        if (File.Exists(socketPath)) File.Delete(socketPath);

        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint(socketPath));
        listener.Listen(8);

        while (!ct.IsCancellationRequested)
        {
            var socket = await listener.AcceptAsync(ct).ConfigureAwait(false);
            var stream = new NetworkStream(socket, ownsSocket: true);
            _clients.Add(stream);
            _ = HandleClientAsync(stream, ct);
        }
    }

    private async Task HandleClientAsync(Stream stream, CancellationToken ct)
    {
        var pipe = new Pipe();
        _ = PumpAsync(stream, pipe.Writer, ct);
        while (!ct.IsCancellationRequested)
        {
            var message = await ShellHostPipeProtocol.ReadMessageAsync(pipe.Reader, ct).ConfigureAwait(false);
            if (message is null) break;
            MessageReceived?.Invoke(this, message);
            if (message.Type == ShellHostMessageType.Ping)
            {
                await WriteToClientAsync(stream, new ShellHostMessage { Type = ShellHostMessageType.Pong }, ct).ConfigureAwait(false);
            }
        }
        _clients.Remove(stream);
    }

    private static async Task WriteToClientAsync(Stream stream, ShellHostMessage message, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(message, ShellHostJson.Options);
        var payload = System.Text.Encoding.UTF8.GetBytes(json);
        var header = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await stream.WriteAsync(header, ct).ConfigureAwait(false);
        await stream.WriteAsync(payload, ct).ConfigureAwait(false);
        await stream.FlushAsync(ct).ConfigureAwait(false);
    }

    private static async Task PumpAsync(Stream stream, PipeWriter writer, CancellationToken ct)
    {
        var buffer = new byte[4096];
        while (!ct.IsCancellationRequested)
        {
            var read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);
            if (read == 0) break;
            await writer.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            await writer.FlushAsync(ct).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_acceptLoop is not null) await _acceptLoop.ConfigureAwait(false);
        foreach (var client in _clients) await client.DisposeAsync().ConfigureAwait(false);
        _clients.Clear();
    }
}

internal static class ShellHostJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
