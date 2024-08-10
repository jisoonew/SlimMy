using SlimMy.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy
{
    public class MainServer
    {
        ClientManager _clientManager = new ClientManager();

        public MainServer()
        {
            Task serverStart = Task.Run(() =>
            {
                ServerRun();
            });
        }


        private void ServerRun()
        {
            // 포트 번호 9999에서 모든 IP 주소에 대해 TCP 연결을 수신 대기하는 TcpListener 객체를 생성
            // 동기 모드 차단에서 들어오는 연결 요청을 수신 대기하고 수락하는 간단한 메서드를 제공
            TcpListener listener = new TcpListener(new IPEndPoint(IPAddress.Any, 9999));

            // 수신 대기 시작
            listener.Start();

            while (true)
            {
                // 메서드를 호출하여 클라이언트의 TCP 연결을 비동기적으로 수락
                // 동기 차단 모드에서 네트워크를 통해 스트림 데이터를 연결, 전송 및 수신하는 간단한 메서드
                // AcceptTcpClientAsync() => 보류 중인 연결 요청을 비동기 작업으로 허용합니다.
                Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();

                // 태스크가 완료될 때까지 대기
                acceptTask.Wait();

                // acceptTask가 완료되면, acceptTask.Result를 통해 새로 연결된 클라이언트의 TcpClient 객체를 얻습니다. 이 객체는 클라이언트와의 네트워크 통신을 관리
                TcpClient newClient = acceptTask.Result;

                // 새로 연결된 클라이언트를 클라이언트 관리자로 추가합니다.
                _clientManager.AddClient(newClient);
            }
        }
    }
}
