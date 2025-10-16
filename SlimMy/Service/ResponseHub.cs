using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SlimMy.Service
{
    public sealed class ResponseHub
    {
        // 락 전용 오브젝트
        private readonly object _gate = new();
        // 채팅방 목록 응답을 기다리는 대기 슬롯
        private TaskCompletionSource<byte[]> _chatRoomListTcs;
        private TaskCompletionSource<byte[]> _chatRoomUserListTcs;
        private TaskCompletionSource<byte[]> _chatRoomPageListTcs;
        private TaskCompletionSource<byte[]> _mychatRoomSearchWordTcs;
        private TaskCompletionSource<byte[]> _myDataTcs;
        private TaskCompletionSource<byte[]> _todayWeightCompletedTcs;
        private TaskCompletionSource<byte[]> _updateMyPageUserDataTcs;
        private TaskCompletionSource<byte[]> _UpdatetMyPageWeightTcs;
        private TaskCompletionSource<byte[]> _InsertMyPageWeightTcs;
        private TaskCompletionSource<byte[]> _getUserDataTcs;

        // 채팅방 목록 응답을 기다릴 준비를 하고 Task 반환
        public Task<byte[]> WaitChatRoomListAsync(TimeSpan timeout)
        {
            // TCS가 완료될 때 이어지는 await 이후 코드가 현재 쓰레드/락을 점유한 채로 바로 실행되지 않도록 보장해 데드락/지연 줄이기
            // 락/현재 스레드에서 즉시 이어서 실행되는 걸 방지
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

            // 대기 슬롯을 새 TCS로 교체, 다른 스레드(TryResolve나 타임아웃 콜백)가 동시에 만지지 못하게 보호
            lock (_gate) _chatRoomListTcs = tcs;

            // timeout 뒤에 취소 신호를 내는 CTS 생성
            var cts = new CancellationTokenSource(timeout);

            // 타임아웃이 발생하면 TCS를 예외(Faulted) 상태로 만들어 await가 TimeoutException으로 깨어나도록 등록
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_chatRoomListTcs == tcs) _chatRoomListTcs = null; } // ★ 타임아웃 시 슬롯 정리
                tcs.TrySetException(new TimeoutException("ChatRoomListRes timeout"));
                cts.Dispose();
            });

            return tcs.Task;
        }

        public Task<byte[]> WaitChatRoomUserListAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _chatRoomUserListTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_chatRoomUserListTcs == tcs) _chatRoomUserListTcs = null; }
                tcs.TrySetException(new TimeoutException("ChatRoomUserListRes timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> WaitChatRoomPageListAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _chatRoomPageListTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_chatRoomPageListTcs == tcs) _chatRoomPageListTcs = null; }
                tcs.TrySetException(new TimeoutException("ChatRoomPageListRes timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> WaitMyChatRoomSearchWordAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _mychatRoomSearchWordTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_mychatRoomSearchWordTcs == tcs) _mychatRoomSearchWordTcs = null; }
                tcs.TrySetException(new TimeoutException("WaitMyChatRoomSearchWordAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> WaitMyDataAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _myDataTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_myDataTcs == tcs) _myDataTcs = null; }
                tcs.TrySetException(new TimeoutException("WaitMyChatRoomSearchWordAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> TodayWeightCompletedAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _todayWeightCompletedTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_todayWeightCompletedTcs == tcs) _todayWeightCompletedTcs = null; }
                tcs.TrySetException(new TimeoutException("TodayWeightCompletedAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> UpdateMyPageUserDataAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _updateMyPageUserDataTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_updateMyPageUserDataTcs == tcs) _updateMyPageUserDataTcs = null; }
                tcs.TrySetException(new TimeoutException("UpdateMyPageUserDataAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> UpdatetMyPageWeightAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _UpdatetMyPageWeightTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_UpdatetMyPageWeightTcs == tcs) _UpdatetMyPageWeightTcs = null; }
                tcs.TrySetException(new TimeoutException("UpdateMyPageUserDataAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> InsertMyPageWeightAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _InsertMyPageWeightTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_InsertMyPageWeightTcs == tcs) _InsertMyPageWeightTcs = null; }
                tcs.TrySetException(new TimeoutException("UpdateMyPageUserDataAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetUserDataAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getUserDataTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getUserDataTcs == tcs) _getUserDataTcs = null; }
                tcs.TrySetException(new TimeoutException("UpdateMyPageUserDataAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        // 수신 루프가 호출, 들어온 메시지 (type, payload)가 기다리던 응답이면 TCS를 완료
        public bool TryResolve(MessageType type, byte[] payload)
        {
            // 슬롯 읽기/완료/비우기를 원자적으로 수행하기 위해 락 획득
            lock (_gate)
            {
                switch (type)
                {
                    case MessageType.ChatRoomListRes:
                        if (_chatRoomListTcs == null) return false;
                        _chatRoomListTcs.TrySetResult(payload);
                        _chatRoomListTcs = null;
                        return true;

                    case MessageType.ChatRoomUserListRes:
                        if (_chatRoomUserListTcs == null) return false;
                        _chatRoomUserListTcs.TrySetResult(payload);
                        _chatRoomUserListTcs = null;
                        return true;

                    case MessageType.ChatRoomPageListRes:
                        if (_chatRoomPageListTcs == null) return false;
                        _chatRoomPageListTcs.TrySetResult(payload);
                        _chatRoomPageListTcs = null;
                        return true;

                    case MessageType.MyChatRoomSearchWordRes:
                        if (_mychatRoomSearchWordTcs == null) return false;
                        _mychatRoomSearchWordTcs.TrySetResult(payload);
                        _mychatRoomSearchWordTcs = null;
                        return true;
                    case MessageType.MyDataRes:
                        if (_myDataTcs == null) return false;
                        _myDataTcs.TrySetResult(payload);
                        _myDataTcs = null;
                        return true;
                    case MessageType.TodayWeightCompletedRes:
                        if (_todayWeightCompletedTcs == null) return false;
                        _todayWeightCompletedTcs.TrySetResult(payload);
                        _todayWeightCompletedTcs = null;
                        return true;
                    case MessageType.UpdateMyPageUserDataRes:
                        if (_updateMyPageUserDataTcs == null) return false;
                        _updateMyPageUserDataTcs.TrySetResult(payload);
                        _updateMyPageUserDataTcs = null;
                        return true;
                    case MessageType.UpdatetMyPageWeightRes:
                        if (_UpdatetMyPageWeightTcs == null) return false;
                        _UpdatetMyPageWeightTcs.TrySetResult(payload);
                        _UpdatetMyPageWeightTcs = null;
                        return true;
                    case MessageType.InsertMyPageWeightRes:
                        if (_InsertMyPageWeightTcs == null) return false;
                        _InsertMyPageWeightTcs.TrySetResult(payload);
                        _InsertMyPageWeightTcs = null;
                        return true;
                    case MessageType.VerifyPasswordRes:
                        if (_getUserDataTcs == null) return false;
                        _getUserDataTcs.TrySetResult(payload);
                        _getUserDataTcs = null;
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
