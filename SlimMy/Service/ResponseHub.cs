using SlimMy.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SlimMy.Service
{
    public readonly struct MessageKey : IEquatable<MessageKey>
    {
        public readonly MessageType Type;
        public readonly Guid RequestId;
        public MessageKey(MessageType type, Guid requestId) { Type = type; RequestId = requestId; }
        public bool Equals(MessageKey o) => Type == o.Type && RequestId == o.RequestId;
        public override bool Equals(object obj) => obj is MessageKey o && Equals(o);
        public override int GetHashCode() => HashCode.Combine((int)Type, RequestId);
        public override string ToString() => $"{Type}/{RequestId}";
    }

    public sealed class ResponseHub
    {
        // 대기자 테이블 각 요청은 응답 타입과 requestId로 고유 슬롯
        private readonly ConcurrentDictionary<MessageKey, TaskCompletionSource<byte[]>> _waiters = new();
        // 수신 루프 중복 실행 방지
        private volatile bool _started;

        // 요청 보내기 전에 호출해 대기 슬롯 등록
        public Task<byte[]> WaitAsync(MessageType responseType, Guid requestId, TimeSpan timeout, CancellationToken ct = default)
        {
            var key = new MessageKey(responseType, requestId);
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_waiters.TryAdd(key, tcs))
                throw new InvalidOperationException($"Duplicated waiter: {key}");

            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);
            cts.Token.Register(() =>
            {
                if (_waiters.TryRemove(key, out var pending))
                    pending.TrySetException(new TimeoutException($"{key} timeout"));
                cts.Dispose();
            });
            _ = tcs.Task.ContinueWith(_ => cts.Dispose(), TaskScheduler.Default);
            return tcs.Task;
        }

        // 수신 루프 한 번만 시작
        public void StartReceiveLoopIfNeeded(INetworkTransport transport, CancellationToken ct = default)
        {
            if (_started) return;
            _started = true;

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        var (type, payload) = await transport.ReadFrameAsync(ct);
                        var reqId = TryExtractRequestId(payload); // 응답이면 동일 requestId가 들어있어야 함
                        if (TryResolve(type, reqId, payload)) continue;
                        HandlePush(type, payload); // requestId 없는 푸시
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { FailAll(ex); }
            }, ct);
        }

        public bool TryResolve(MessageType type, Guid requestId, byte[] payload)
        {
            var key = new MessageKey(type, requestId);
            if (_waiters.TryRemove(key, out var tcs)) { tcs.TrySetResult(payload); return true; }
            return false;
        }

        // 연결 종료/치명적 예외 시, 모든 대기자를 일괄적으로 실패 처리하여 무한 대기 방지
        public void FailAll(Exception ex)
        {
            foreach (var kv in _waiters.ToArray())
                if (_waiters.TryRemove(kv.Key, out var tcs)) tcs.TrySetException(ex);
        }

        private static Guid TryExtractRequestId(byte[] payload)
        {
            if (payload == null || payload.Length < 2) return Guid.Empty;
            byte first = payload[0], last = payload[^1];
            if (first != (byte)'{' || last != (byte)'}') return Guid.Empty;

            if (payload.Length == 0) return Guid.Empty;
            if (payload[0] != '{') return Guid.Empty;

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(payload);
                if (doc.RootElement.TryGetProperty("requestID", out var p) &&
                    p.ValueKind == System.Text.Json.JsonValueKind.String &&
                    Guid.TryParse(p.GetString(), out var g))
                    return g;
            }
            catch (System.Text.Json.JsonException) { }
            return Guid.Empty;
        }

        private void HandlePush(MessageType type, byte[] payload)
        {
            // TODO: 채팅/알림 등 브로드캐스트 처리
        }
    }
}
