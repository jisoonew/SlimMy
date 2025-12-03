using SlimMy.Model;
using SlimMy.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy
{
    public class UserSession
    {
        private static UserSession _instance;
        public User CurrentUser { get; set; }
        public INetworkTransport Transport { get; set; }
        public ResponseHub Responses { get; } = new ResponseHub();
        public Guid? AccessToken { get; set; }

        private UserSession() { }

        public static UserSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UserSession();
                }
                return _instance;
            }
        }

        public void Clear()
        {
            try
            {
                // 네트워크 연결 정리
                (Transport as IDisposable)?.Dispose();
            }
            catch
            {
                // Dispose 중 예외는 무시
            }

            Transport = null;
            CurrentUser = null;
            AccessToken = null;
        }
    }
}
