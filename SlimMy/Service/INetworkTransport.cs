using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlimMy.Service
{
    public interface INetworkTransport : IDisposable
    {
        Task ConnectAsync(string host, int port, CancellationToken ct = default);

        Task SendFrameAsync(byte type, ReadOnlyMemory<byte> payload, CancellationToken ct = default);

        Task<(byte type, byte[] payload)> ReadFrameAsync(CancellationToken ct = default);
    }
}
