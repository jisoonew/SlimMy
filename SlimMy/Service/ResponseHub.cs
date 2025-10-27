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
        private TaskCompletionSource<byte[]> _deleteAccountViewTcs;
        private TaskCompletionSource<byte[]> _nickNameCheckPrintTcs;
        private TaskCompletionSource<byte[]> _nickNameSaveTcs;
        private TaskCompletionSource<byte[]> _insertPlannerPrintTcs;
        private TaskCompletionSource<byte[]> _deletePlannerListTcs;
        private TaskCompletionSource<byte[]> _exerciseCheckTcs;
        private TaskCompletionSource<byte[]> _updatePlannerTcs;
        private TaskCompletionSource<byte[]> _insertPlannerTcs;
        private TaskCompletionSource<byte[]> _plannerPrintTcs;
        private TaskCompletionSource<byte[]> _deletePlannerTcs;
        private TaskCompletionSource<byte[]> _exerciseListTcs;
        private TaskCompletionSource<byte[]> _getWeightHistoryTcs;
        private TaskCompletionSource<byte[]> _getTodayWeightCompletedTcs;
        private TaskCompletionSource<byte[]> _insertWeightTcs;
        private TaskCompletionSource<byte[]> _updatetWeightWeightTcs;
        private TaskCompletionSource<byte[]> _getMemoContentTcs;
        private TaskCompletionSource<byte[]> _getSearchedMemoContentTcs;
        private TaskCompletionSource<byte[]> _getSearchedDateTcs;
        private TaskCompletionSource<byte[]> _getSearchedWeightTcs;
        private TaskCompletionSource<byte[]> _deleteWeightTcs;
        private TaskCompletionSource<byte[]> _allExerciseListTcs;
        private TaskCompletionSource<byte[]> _selectUserWeightTcs;
        private TaskCompletionSource<byte[]> _getExerciseHistoryTcs;
        private TaskCompletionSource<byte[]> _getTodayCaloriesTcs;
        private TaskCompletionSource<byte[]> _getTodayDurationTcs;
        private TaskCompletionSource<byte[]> _getTodayCompletedTcs;
        private TaskCompletionSource<byte[]> _getTotalExerciseTcs;
        private TaskCompletionSource<byte[]> _getWeeklyCaloriesTcs;
        private TaskCompletionSource<byte[]> _getTotalSessionsTcs;
        private TaskCompletionSource<byte[]> _getTotalCaloriesTcs;

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

        public Task<byte[]> DeleteAccountViewAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _deleteAccountViewTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_deleteAccountViewTcs == tcs) _deleteAccountViewTcs = null; }
                tcs.TrySetException(new TimeoutException("UpdateMyPageUserDataAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> NickNameCheckPrintAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _nickNameCheckPrintTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_nickNameCheckPrintTcs == tcs) _nickNameCheckPrintTcs = null; }
                tcs.TrySetException(new TimeoutException("UpdateMyPageUserDataAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> NickNameSaveAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _nickNameSaveTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_nickNameSaveTcs == tcs) _nickNameSaveTcs = null; }
                tcs.TrySetException(new TimeoutException("UpdateMyPageUserDataAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> InsertPlannerPrintAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _insertPlannerPrintTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_insertPlannerPrintTcs == tcs) _insertPlannerPrintTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> DeletePlannerListAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _deletePlannerListTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_deletePlannerListTcs == tcs) _deletePlannerListTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> ExerciseCheckAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _exerciseCheckTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_exerciseCheckTcs == tcs) _exerciseCheckTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> UpdatePlannerAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _updatePlannerTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_updatePlannerTcs == tcs) _updatePlannerTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> InsertPlannerAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _insertPlannerTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_insertPlannerTcs == tcs) _insertPlannerTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> PlannerPrintAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _plannerPrintTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_plannerPrintTcs == tcs) _plannerPrintTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> DeletePlannerAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _deletePlannerTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_deletePlannerTcs == tcs) _deletePlannerTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> ExerciseListAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _exerciseListTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_exerciseListTcs == tcs) _exerciseListTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetWeightHistoryAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getWeightHistoryTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getWeightHistoryTcs == tcs) _getWeightHistoryTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetTodayWeightCompletedAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getTodayWeightCompletedTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getTodayWeightCompletedTcs == tcs) _getTodayWeightCompletedTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> InsertWeightAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _insertWeightTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_insertWeightTcs == tcs) _insertWeightTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> UpdateWeightWeightAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _updatetWeightWeightTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_updatetWeightWeightTcs == tcs) _updatetWeightWeightTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetMemoContentAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getMemoContentTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getMemoContentTcs == tcs) _getMemoContentTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetSearchedMemoContentAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getSearchedMemoContentTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getSearchedMemoContentTcs == tcs) _getSearchedMemoContentTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetSearchedDateAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getSearchedDateTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getSearchedDateTcs == tcs) _getSearchedDateTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetSearchedWeightAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getSearchedWeightTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getSearchedWeightTcs == tcs) _getSearchedWeightTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> DeleteWeightAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _deleteWeightTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_deleteWeightTcs == tcs) _deleteWeightTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> AllExerciseListAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _allExerciseListTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_allExerciseListTcs == tcs) _allExerciseListTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> SelectUserWeightAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _selectUserWeightTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_selectUserWeightTcs == tcs) _selectUserWeightTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetExerciseHistoryAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getExerciseHistoryTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getExerciseHistoryTcs == tcs) _getExerciseHistoryTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetTodayCaloriesAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getTodayCaloriesTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getTodayCaloriesTcs == tcs) _getTodayCaloriesTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetTodayDurationAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getTodayDurationTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getTodayDurationTcs == tcs) _getTodayDurationTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetTodayCompletedAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getTodayCompletedTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getTodayCompletedTcs == tcs) _getTodayCompletedTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetTotalExerciseAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getTotalExerciseTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getTotalExerciseTcs == tcs) _getTotalExerciseTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetWeeklyCaloriesAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getWeeklyCaloriesTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getWeeklyCaloriesTcs == tcs) _getWeeklyCaloriesTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetTotalSessionsAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getTotalSessionsTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getTotalSessionsTcs == tcs) _getTotalSessionsTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
                cts.Dispose();
            });
            return tcs.Task;
        }

        public Task<byte[]> GetTotalCaloriesAsync(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_gate) _getTotalCaloriesTcs = tcs;

            var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() =>
            {
                lock (_gate) { if (_getTotalCaloriesTcs == tcs) _getTotalCaloriesTcs = null; }
                tcs.TrySetException(new TimeoutException("InsertPlannerPrintAsync timeout"));
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
                    case MessageType.DeleteAccountViewRes:
                        if (_deleteAccountViewTcs == null) return false;
                        _deleteAccountViewTcs.TrySetResult(payload);
                        _deleteAccountViewTcs = null;
                        return true;
                    case MessageType.NickNameCheckPrintRes:
                        if (_nickNameCheckPrintTcs == null) return false;
                        _nickNameCheckPrintTcs.TrySetResult(payload);
                        _nickNameCheckPrintTcs = null;
                        return true;
                    case MessageType.NickNameSaveRes:
                        if (_nickNameSaveTcs == null) return false;
                        _nickNameSaveTcs.TrySetResult(payload);
                        _nickNameSaveTcs = null;
                        return true;
                    case MessageType.InsertPlannerPrintRes:
                        if (_insertPlannerPrintTcs == null) return false;
                        _insertPlannerPrintTcs.TrySetResult(payload);
                        _insertPlannerPrintTcs = null;
                        return true;
                    case MessageType.DeletePlannerListRes:
                        if (_deletePlannerListTcs == null) return false;
                        _deletePlannerListTcs.TrySetResult(payload);
                        _deletePlannerListTcs = null;
                        return true;
                    case MessageType.ExerciseCheckRes:
                        if (_exerciseCheckTcs == null) return false;
                        _exerciseCheckTcs.TrySetResult(payload);
                        _exerciseCheckTcs = null;
                        return true;
                    case MessageType.UpdatePlannerRes:
                        if (_updatePlannerTcs == null) return false;
                        _updatePlannerTcs.TrySetResult(payload);
                        _updatePlannerTcs = null;
                        return true;
                    case MessageType.InsertPlannerRes:
                        if (_insertPlannerTcs == null) return false;
                        _insertPlannerTcs.TrySetResult(payload);
                        _insertPlannerTcs = null;
                        return true;
                    case MessageType.PlannerPrintRes:
                        if (_plannerPrintTcs == null) return false;
                        _plannerPrintTcs.TrySetResult(payload);
                        _plannerPrintTcs = null;
                        return true;
                    case MessageType.DeletePlannerRes:
                        if (_deletePlannerTcs == null) return false;
                        _deletePlannerTcs.TrySetResult(payload);
                        _deletePlannerTcs = null;
                        return true;
                    case MessageType.ExerciseListRes:
                        if (_exerciseListTcs == null) return false;
                        _exerciseListTcs.TrySetResult(payload);
                        _exerciseListTcs = null;
                        return true;
                    case MessageType.GetWeightHistoryRes:
                        if (_getWeightHistoryTcs == null) return false;
                        _getWeightHistoryTcs.TrySetResult(payload);
                        _getWeightHistoryTcs = null;
                        return true;
                    case MessageType.GetTodayWeightCompletedRes:
                        if (_getTodayWeightCompletedTcs == null) return false;
                        _getTodayWeightCompletedTcs.TrySetResult(payload);
                        _getTodayWeightCompletedTcs = null;
                        return true;
                    case MessageType.InsertWeightRes:
                        if (_insertWeightTcs == null) return false;
                        _insertWeightTcs.TrySetResult(payload);
                        _insertWeightTcs = null;
                        return true;
                    case MessageType.UpdateWeightRes:
                        if (_updatetWeightWeightTcs == null) return false;
                        _updatetWeightWeightTcs.TrySetResult(payload);
                        _updatetWeightWeightTcs = null;
                        return true;
                    case MessageType.GetMemoContentRes:
                        if (_getMemoContentTcs == null) return false;
                        _getMemoContentTcs.TrySetResult(payload);
                        _getMemoContentTcs = null;
                        return true;
                    case MessageType.GetSearchedMemoContentRes:
                        if (_getSearchedMemoContentTcs == null) return false;
                        _getSearchedMemoContentTcs.TrySetResult(payload);
                        _getSearchedMemoContentTcs = null;
                        return true;
                    case MessageType.GetSearchedDateRes:
                        if (_getSearchedDateTcs == null) return false;
                        _getSearchedDateTcs.TrySetResult(payload);
                        _getSearchedDateTcs = null;
                        return true;
                    case MessageType.GetSearchedWeightRes:
                        if (_getSearchedWeightTcs == null) return false;
                        _getSearchedWeightTcs.TrySetResult(payload);
                        _getSearchedWeightTcs = null;
                        return true;
                    case MessageType.DeleteWeightRes:
                        if (_deleteWeightTcs == null) return false;
                        _deleteWeightTcs.TrySetResult(payload);
                        _deleteWeightTcs = null;
                        return true;
                    case MessageType.AllExerciseListRes:
                        if (_allExerciseListTcs == null) return false;
                        _allExerciseListTcs.TrySetResult(payload);
                        _allExerciseListTcs = null;
                        return true;
                    case MessageType.SelectUserWeightRes:
                        if (_selectUserWeightTcs == null) return false;
                        _selectUserWeightTcs.TrySetResult(payload);
                        _selectUserWeightTcs = null;
                        return true;
                    case MessageType.GetExerciseHistoryRes:
                        if (_getExerciseHistoryTcs == null) return false;
                        _getExerciseHistoryTcs.TrySetResult(payload);
                        _getExerciseHistoryTcs = null;
                        return true;
                    case MessageType.GetTodayCaloriesRes:
                        if (_getTodayCaloriesTcs == null) return false;
                        _getTodayCaloriesTcs.TrySetResult(payload);
                        _getTodayCaloriesTcs = null;
                        return true;
                    case MessageType.GetTodayDurationRes:
                        if (_getTodayDurationTcs == null) return false;
                        _getTodayDurationTcs.TrySetResult(payload);
                        _getTodayDurationTcs = null;
                        return true;
                    case MessageType.GetTodayCompletedRes:
                        if (_getTodayCompletedTcs == null) return false;
                        _getTodayCompletedTcs.TrySetResult(payload);
                        _getTodayCompletedTcs = null;
                        return true;
                    case MessageType.GetTotalExerciseRes:
                        if (_getTotalExerciseTcs == null) return false;
                        _getTotalExerciseTcs.TrySetResult(payload);
                        _getTotalExerciseTcs = null;
                        return true;
                    case MessageType.GetWeeklyCalories:
                        if (_getWeeklyCaloriesTcs == null) return false;
                        _getWeeklyCaloriesTcs.TrySetResult(payload);
                        _getWeeklyCaloriesTcs = null;
                        return true;
                    case MessageType.GetTotalSessionsRes:
                        if (_getTotalSessionsTcs == null) return false;
                        _getTotalSessionsTcs.TrySetResult(payload);
                        _getTotalSessionsTcs = null;
                        return true;
                    case MessageType.GetTotalCaloriesRes:
                        if (_getTotalCaloriesTcs == null) return false;
                        _getTotalCaloriesTcs.TrySetResult(payload);
                        _getTotalCaloriesTcs = null;
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
