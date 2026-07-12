using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using BndzFinder.Core.Ipc;

namespace BndzFinder.Core.Ipc;

public static class ShellHostPipeProtocol
{
    public const string PipeName = "BndzFinder.ShellHost";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task WriteMessageAsync(PipeWriter writer, ShellHostMessage message, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var payload = Encoding.UTF8.GetBytes(json);
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);
        await writer.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await writer.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ShellHostMessage?> ReadMessageAsync(PipeReader reader, CancellationToken cancellationToken)
    {
        var headerResult = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        var headerBuffer = headerResult.Buffer;
        if (headerBuffer.Length < 4)
        {
            reader.AdvanceTo(headerBuffer.Start, headerBuffer.End);
            return null;
        }

        var header = headerBuffer.Slice(0, 4);
        var length = BinaryPrimitives.ReadInt32LittleEndian(header.FirstSpan);
        if (headerBuffer.Length < 4 + length)
        {
            reader.AdvanceTo(headerBuffer.Start, headerBuffer.End);
            return null;
        }

        var payload = headerBuffer.Slice(4, length);
        var json = Encoding.UTF8.GetString(payload.FirstSpan);
        reader.AdvanceTo(headerBuffer.GetPosition(4 + length));

        return JsonSerializer.Deserialize<ShellHostMessage>(json, JsonOptions);
    }
}

public sealed class NamedPipeShellHostClient : IShellHostClient, IAsyncDisposable
{
    private Stream? _stream;
    private readonly PipeReader _reader;
    private readonly PipeWriter _writer;

    public NamedPipeShellHostClient()
    {
        var pipe = new Pipe();
        _reader = pipe.Reader;
        _writer = pipe.Writer;
    }

    public bool IsConnected => _stream is not null;

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (OperatingSystem.IsWindows())
        {
            _stream = new FileStream(
                $"\\\\.\\pipe\\{ShellHostPipeProtocol.PipeName}",
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite);
        }
        else
        {
            var socketPath = Path.Combine(Path.GetTempPath(), $"{ShellHostPipeProtocol.PipeName}.sock");
            _stream = new FileStream(socketPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        _ = PumpReceiveAsync(cancellationToken);
        await Task.CompletedTask;
    }

    public async Task SendAsync(ShellHostMessage message, CancellationToken cancellationToken = default)
    {
        if (_stream is null) throw new InvalidOperationException("Not connected.");
        await ShellHostPipeProtocol.WriteMessageAsync(_writer, message, cancellationToken).ConfigureAwait(false);
        var readResult = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (!readResult.Buffer.IsEmpty && _stream.CanWrite)
        {
            foreach (var segment in readResult.Buffer)
            {
                await _stream.WriteAsync(segment, cancellationToken).ConfigureAwait(false);
            }
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        _reader.AdvanceTo(readResult.Buffer.End);
    }

    public async IAsyncEnumerable<ShellHostMessage> ReceiveAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var receivePipe = new Pipe();
        while (!cancellationToken.IsCancellationRequested && _stream is not null)
        {
            var bytesRead = await _stream.ReadAsync(receivePipe.Writer.GetMemory(4096), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0) break;
            receivePipe.Writer.Advance(bytesRead);
            await receivePipe.Writer.FlushAsync(cancellationToken).ConfigureAwait(false);

            while (true)
            {
                var message = await ShellHostPipeProtocol.ReadMessageAsync(receivePipe.Reader, cancellationToken).ConfigureAwait(false);
                if (message is null) break;
                yield return message;
            }
        }
    }

    public Task DisconnectAsync()
    {
        _stream?.Dispose();
        _stream = null;
        return Task.CompletedTask;
    }

    private async Task PumpReceiveAsync(CancellationToken cancellationToken)
    {
        if (_stream is null) return;
        var buffer = new byte[4096];
        while (!cancellationToken.IsCancellationRequested)
        {
            var read = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            await _writer.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        await _reader.CompleteAsync();
        await _writer.CompleteAsync();
    }
}
