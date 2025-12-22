using SlimMy.Model;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlimMy.Service
{
    public class TlsTcpTransport : INetworkTransport
    {
        private readonly TcpClient _tcp = new();
        private SslStream _ssl;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
        {
            await _tcp.ConnectAsync(host, port);
            _ssl = new SslStream(_tcp.GetStream(), false, (s, cert, chain, errs) => true);
            await _ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
            });
        }

        public async Task SendFrameAsync(MessageType type, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
        {
            await _sendLock.WaitAsync(ct);
            try
            {
                int totalLen = 2 + payload.Length;

                byte[] lenBuf = BitConverter.GetBytes(totalLen);
                byte[] typeBuf = BitConverter.GetBytes((ushort)type);

                await _ssl.WriteAsync(lenBuf, ct);
                await _ssl.WriteAsync(typeBuf, ct);

                if (payload.Length > 0)
                    await _ssl.WriteAsync(payload, ct);

                await _ssl.FlushAsync(ct);
            }
            finally { _sendLock.Release(); }
        }

        public async Task<(MessageType type, byte[] payload)> ReadFrameAsync(CancellationToken ct = default)
        {
            byte[] lenBuf = await ReadExactAsync(_ssl, 4, ct);
            int totalLen = BitConverter.ToInt32(lenBuf, 0);
            if (totalLen < 2) throw new IOException($"invalid frame length: {totalLen}");

            byte[] typeBuf = await ReadExactAsync(_ssl, 2, ct);
            ushort t = BitConverter.ToUInt16(typeBuf, 0);
            var type = (MessageType)t;

            int pl = totalLen - 2;
            byte[] payload = pl > 0 ? await ReadExactAsync(_ssl, pl, ct) : Array.Empty<byte>();

            return (type, payload);
        }

        public Task SendFrameAsync(byte type, ReadOnlyMemory<byte> payload, CancellationToken ct = default)
            => SendFrameAsync((MessageType)type, payload, ct);

        private static async Task<byte[]> ReadExactAsync(Stream s, int n, CancellationToken ct)
        {
            byte[] buf = new byte[n]; int off = 0;
            while (off < n)
            {
                int r = await s.ReadAsync(buf, off, n - off, ct);
                if (r == 0) throw new IOException("remote closed");
                off += r;
            }
            return buf;
        }

        public void Dispose()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[TlsTcpTransport] Dispose called");
                _ssl?.Dispose();
            }
            catch { }

            try { _tcp?.Dispose(); } catch { }
        }
    }
}
