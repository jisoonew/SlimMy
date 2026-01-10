using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Service
{
    public interface INavigationService
    {
        void NavigateToClose(string viewCloseName);

        void NavigateToFrame(Type page);

        // 운동 선택
        Task NavigateToAddExerciseViewAsync(PlannerViewModel plannerVm);

        Task NavigateToFrameAsync(Type pageType);

        // 목표 설정
        Task NavigateToDietGoalViewAsync();

        Task NavigateToCommunityFrameAsync(Type pageType);

        void NavigateToMainWindow(MainPageViewModel mainPageViewModel);

        void NavigateToExerciseWindow();

        Task NavigateToDashBoardFrameAsync(Type pageType);

        Task NavigateToExerciseHistoryFrameAsync(Type pageType);

        Task NavigateToWeightHistoryFrameAsync(Type pageType);

        Task NavigateToMyPageFrameAsync(Type pageType);

        Task NavigateToPlannerFrameAsync(Type pageType);

        // 닉네임 변경 화면 전환
        Task NavigateToNickName();

        // 닉네임 변경 화면 닫기
        Task NavigateToNickNameClose();

        // 모든 창을 닫고 로그인 창만 생성
        void NavigateToLoginOnly();

        // 로그인 화면
        Task NavigateToCloseAndLoginAsync(string viewCloseName);
    }
}
