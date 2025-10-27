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

        Task NavigateToAddExercise();

        Task NavigateToFrameAsync(Type pageType);

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

        // 로그인 화면
        Task NavigateToCloseAndLoginAsync(string viewCloseName);
    }
}
