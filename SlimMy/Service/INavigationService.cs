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

        void NavigateToAddExercise();

        Task NavigateToFrameAsync(Type pageType);

        Task NavigateToCommunityFrameAsync(Type pageType);
    }
}
