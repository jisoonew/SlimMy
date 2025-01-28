using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Oracle.ManagedDataAccess.Client;
using SlimMy.Model;
using SlimMy.View;
using Dapper;
using System.Diagnostics;

namespace SlimMy
{
    class Repo
    {
        private readonly string _connString;

        public Repo(string connString)
        {
            _connString = connString;
        }

        public void InsertPerson(string name)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    string sql = "INSERT INTO test (name) VALUES (:name)";
                    using (OracleCommand sqlCommand = new OracleCommand(sql, connection))
                    {
                        sqlCommand.Parameters.Add(new OracleParameter("name", name));
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error : " + ex.Message);
                }
            }
        }

        // 이메일 중복 확인
        public bool DuplicateEmail(string email)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    string sql = "select count(*) from Users where email = :email";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("email", email));

                        int count = Convert.ToInt32(command.ExecuteScalar());

                        // 중복이라면 false, 중복이 아니라면 true
                        if (count >= 1)
                        {
                            MessageBox.Show("사용 불가능한 이메일입니다.");
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error : " + ex.Message);

                    return false;
                }
            }
        }

        // 닉네임 중복 확인
        public bool BuplicateNickName(string nickname)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    string sql = "select count(*) from Users where nickname = :nickname";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("nickname", nickname));

                        int count = Convert.ToInt32(command.ExecuteScalar());

                        // 중복이라면 false, 중복이 아니라면 true
                        if (count >= 1)
                        {
                            MessageBox.Show("닉네임이 이미 존재합니다.");
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error : " + ex.Message);

                    return false;
                }
            }
        }

        // 회원가입
        public void InsertUser(string name, string gender, string nickName, string email, string password, DateTime birthDate, int height, int weight, string dietGoal)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    string sql = "insert into Users (userid, email, name, gender, nickname, password, birth_date, height, weight, diet_goal) " +
            "values(:userid, :email, :name, :gender, :nickName, :password, :birthDate, :height, :weight, :dietGoal)";

                    Guid userId = Guid.NewGuid(); // 새로운 GUID 생성
                    byte[] userIdBytes = userId.ToByteArray(); // GUID를 바이트 배열로 변환

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("userid", OracleDbType.Raw, userIdBytes, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("email", email));
                        command.Parameters.Add(new OracleParameter("name", name));
                        command.Parameters.Add(new OracleParameter("gender", gender));
                        command.Parameters.Add(new OracleParameter("nickName", nickName));
                        command.Parameters.Add(new OracleParameter("password", password));
                        command.Parameters.Add(new OracleParameter("birthDate", birthDate));
                        command.Parameters.Add(new OracleParameter("height", height));
                        command.Parameters.Add(new OracleParameter("weight", weight));
                        command.Parameters.Add(new OracleParameter("dietGoal", dietGoal));

                        command.ExecuteNonQuery();

                        MessageBox.Show("회원가입 되었습니다!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error : " + ex.Message);
                }
            }
        }

        // 로그인
        public bool LoginSuccess(string email, string password)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    string sql = "select count(*) from Users where email = :email and password = :password";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("email", email));
                        command.Parameters.Add(new OracleParameter("password", password));

                        return (decimal)command.ExecuteScalar() > 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error : " + ex.Message);
                    return false;
                }
            }
        }

        // 로그인 이후 닉네임 가져오기
        public string NickName(string email)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    string sql = "select nickname from Users where email = :email";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("email", email));

                        object resultNickName = command.ExecuteScalar();

                        return resultNickName.ToString();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("오류 : " + ex);
                    return "false";
                }
            }
        }

        // 로그인 이후 사용자 아이디 가져오기
        public Guid UserID(string email)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    string sql = "select userid from Users where email = :email";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("email", email));

                        // ExecuteScalar() 메서드는 object 타입을 반환하므로,
                        // 이를 byte[]로 캐스팅한 후 Guid로 변환합니다.
                        byte[] userIdBytes = (byte[])command.ExecuteScalar();

                        // byte[]를 Guid로 변환
                        Guid userId = new Guid(userIdBytes);

                        return userId;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("오류 : " + ex);
                    return Guid.Empty;
                }
            }
        }

        // 사용자와 채팅방 관계 아이디 출력
        public Guid ChatRoomUserID(Guid chatRoomId)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    string sql = "select userchatroomid from UserChatRooms where chatRoomId = :chatRoomId";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        // Guid를 byte[]로 변환
                        byte[] chatRoomIdBytes = chatRoomId.ToByteArray();
                        command.Parameters.Add(new OracleParameter("chatRoomId", OracleDbType.Raw, chatRoomIdBytes, ParameterDirection.Input));

                        // ExecuteScalar() 메서드는 object 타입을 반환하므로,
                        // 이를 byte[]로 캐스팅하기 전에 null 체크
                        object result = command.ExecuteScalar();

                        if (result != null && result is byte[] userIdBytes)
                        {
                            // byte[]를 Guid로 변환
                            Guid userId = new Guid(userIdBytes);
                            return userId;
                        }
                        else
                        {
                            throw new Exception("방장 아이디를 가져오는 데 실패했습니다.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("오류 : " + ex);
                    return Guid.Empty;
                }
            }
        }

        // 채팅방 아이디로 채팅방 생성한 사용자 아이디 찾기
        public Guid GetHostUserIdByRoomId(String chatRoomId)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    // Guid를 byte[]로 변환
                    byte[] chatRoomIdBytes = Guid.Parse(chatRoomId).ToByteArray();

                    string chatRoomIdHex = BitConverter.ToString(chatRoomIdBytes).Replace("-", "");

                    string sql = "SELECT USERID FROM userchatrooms WHERE CHATROOMID = :chatRoomId AND isowner = 1";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        // 매개변수 설정
                        command.Parameters.Add(new OracleParameter
                        {
                            ParameterName = "chatRoomId",
                            OracleDbType = OracleDbType.Raw,
                            Value = chatRoomIdBytes,
                            Size = 16 // RAW(16 BYTE)와 맞춤
                        });

                        // OracleDataReader를 사용해 결과 읽기
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // 결과에서 USERID를 가져와 Guid로 변환
                                byte[] userIdBytes = (byte[])reader["USERID"];
                                Guid userId = new Guid(userIdBytes);
                                return userId;
                            }
                            else
                            {
                                // 결과가 없으면 예외 발생
                                throw new Exception($"No data found for CHATROOMID: {chatRoomIdHex}, isowner: 1");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 예외 처리 및 기본값 반환
                    MessageBox.Show($"오류: {ex.Message}");
                    return Guid.Empty;
                }
            }
        }

        // 사용자와 채팅방 간의 관계 정보 저장
        public void InsertUserChatRooms(Guid userId, Guid chatRoomId, DateTime createdAt, int num)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    byte[] userIdBytes = userId.ToByteArray();
                    byte[] chatRoomIdBytes = chatRoomId.ToByteArray();

                    string sql = "insert into UserChatRooms (USERCHATROOMID, USERID, CHATROOMID, CREATEDAT, ISOWNER) values (:USERCHATROOMID, :USERID, :CHATROOMID, :CreatedAt, :Isowner)";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        Guid userChatRoomId = Guid.NewGuid();
                        byte[] userChatRoomIdBytes = userChatRoomId.ToByteArray();

                        command.Parameters.Add(new OracleParameter("USERCHATROOMID", OracleDbType.Raw, userChatRoomIdBytes, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("USERID", OracleDbType.Raw, userIdBytes, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("CHATROOMID", OracleDbType.Raw, chatRoomIdBytes, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("CreatedAt", OracleDbType.TimeStamp, createdAt, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("Isowner", OracleDbType.Decimal, num, ParameterDirection.Input));

                        command.ExecuteNonQuery();
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error : " + ex.Message);
                }
            }
        }

        // UserChatRooms의 사용자와 테이블의 데이터가 존재하는가?
        public bool CheckUserChatRooms(Guid userId, Guid chatRoomId)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    byte[] userIdBytes = userId.ToByteArray();
                    byte[] chatRoomIdBytes = chatRoomId.ToByteArray();

                    string sql = "select count(*) from UserChatrooms where userid = :USERID and chatroomid = :CHATROOMID";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("USERID", OracleDbType.Raw, userIdBytes, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("CHATROOMID", OracleDbType.Raw, chatRoomIdBytes, ParameterDirection.Input));

                        int count = Convert.ToInt32(command.ExecuteScalar());

                        // 중복이라면 false, 중복이 아니라면 true
                        if (count >= 1)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error : " + ex.Message);
                    return false;
                }
            }
        }

        // 채팅방 생성
        public Guid InsertChatRoom(string chatRoomName, string description, string category, DateTime createdAt)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    Guid chatRoomId = Guid.NewGuid(); // 새로운 GUID 생성
                    byte[] chatRoomIdBytes = chatRoomId.ToByteArray(); // GUID를 바이트 배열로 변환

                    string sql = "insert into ChatRooms(ChatRoomId, ChatRoomName, Description, Category, CreatedAt) values(:ChatRoomId, :ChatRoomName, :Description, :Category, :CreatedAt)";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("ChatRoomId", OracleDbType.Raw, chatRoomIdBytes, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("ChatRoomName", chatRoomName));
                        command.Parameters.Add(new OracleParameter("Description", description));
                        command.Parameters.Add(new OracleParameter("Category", category));
                        command.Parameters.Add(new OracleParameter("CreatedAt", OracleDbType.TimeStamp, createdAt, ParameterDirection.Input));

                        command.ExecuteNonQuery();

                        MessageBox.Show("채팅방이 생성되었습니다.");
                    }

                    return chatRoomId;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);

                    return Guid.Empty; // 에러 발생 시 빈 GUID 반환
                }
            }
        }

        public string GetUserUUId(string getUserEmail)
        {
            string userIds = null;

            using (OracleConnection connection = new OracleConnection(_connString))
            {
                connection.Open();

                using (OracleCommand command = new OracleCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT UserId FROM users WHERE Email = :getUserEmail";
                    command.Parameters.Add(new OracleParameter("getUserEmail", getUserEmail));

                    try
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // RAW 데이터 타입을 문자열로 변환하여 읽음 (예: GUID로 변환)
                                byte[] userIdBytes = (byte[])reader["UserId"];
                                string userId = new Guid(userIdBytes).ToString();
                                userIds = userId;
                            }
                        }

                        return userIds;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error details: {ex.Message}");
                        throw new ArgumentException("Error fetching user IDs: Value does not fall within the expected range.", ex);
                    }
                }
            }
        }


        // 특정 채팅방의 클라이언트 모든 아이디 출력
        public List<string> GetChatRoomUserIds(string chatRoomId)
        {
            List<string> userIds = new List<string>();

            // chatRoomId를 byte[]로 변환 (예: GUID로 변환하여 사용)
            byte[] chatRoomIdBytes = Guid.Parse(chatRoomId).ToByteArray();

            using (OracleConnection connection = new OracleConnection(_connString))
            {
                connection.Open();

                using (OracleCommand command = new OracleCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT UserId FROM userchatrooms WHERE ChatRoomId = :ChatRoomId";
                    command.Parameters.Add(new OracleParameter("ChatRoomId", OracleDbType.Raw)).Value = chatRoomIdBytes;

                    try
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // RAW 데이터 타입을 문자열로 변환하여 읽음 (예: GUID로 변환)
                                byte[] userIdBytes = (byte[])reader["UserId"];
                                string userId = new Guid(userIdBytes).ToString();
                                userIds.Add(userId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error details: {ex.Message}");
                        throw new ArgumentException("Error fetching user IDs: Value does not fall within the expected range.", ex);
                    }
                }
            }

            return userIds;
        }

        // 채팅방 출력
        public IEnumerable<ChatRooms> SelectChatRoom()
        {
            var chatRooms = new List<ChatRooms>();

            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    string sql = "select ChatRoomID, ChatRoomName, Description, Category from ChatRooms";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ChatRooms chatRoom = new ChatRooms();
                                chatRoom.ChatRoomId = reader.GetGuid(0);
                                chatRoom.ChatRoomName = reader.GetString(1);
                                chatRoom.Description = reader.GetString(2);
                                chatRoom.Category = reader.GetString(3);
                                chatRooms.Add(chatRoom);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return chatRooms;
        }


        // 같은 채팅방 모든 닉네임 출력
        public IEnumerable<ChatUserList> SelectChatUserNickName(Guid chatRoomId)
        {
            var chatRooms = new List<ChatUserList>();

            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    byte[] chatRoomIdBytes = chatRoomId.ToByteArray(); // GUID를 바이트 배열로 변환

                    string sql = "SELECT u.userid, u.nickname FROM users u INNER JOIN userchatrooms ucr ON ucr.userid = u.userid WHERE ucr.chatroomid = :ChatRoomId";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("ChatRoomId", OracleDbType.Raw, chatRoomIdBytes, ParameterDirection.Input));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // 바이트 배열을 GUID로 변환한 후 표준 문자열 형식으로 변환
                                byte[] userIdBytes = (byte[])reader["userid"];
                                string userIdStr = new Guid(userIdBytes).ToString();

                                string nickName = reader["nickname"].ToString();

                                ChatUserList chatRoom = new ChatUserList(userIdStr, nickName);
                                
                                chatRooms.Add(chatRoom);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return chatRooms;
        }

        // 내가 참여한 채팅방 출력
        public IEnumerable<ChatRooms> MyChatRooms(Guid userID)
        {
            var chatRooms = new List<ChatRooms>();

            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    byte[] userIdBytes = userID.ToByteArray(); // GUID를 바이트 배열로 변환

                    string sql = "SELECT c.ChatRoomID, c.ChatRoomName, c.Description, c.Category FROM chatrooms c WHERE c.ChatRoomID in ( SELECT uc_inner.ChatRoomID FROM userChatrooms uc_inner WHERE uc_inner.UserID = :userIdBytes)";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        // 바인드 변수 추가
                        command.Parameters.Add(new OracleParameter("userIdBytes", OracleDbType.Raw, userIdBytes, ParameterDirection.Input));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ChatRooms chatRoom = new ChatRooms();
                                chatRoom.ChatRoomId = reader.GetGuid(0);
                                chatRoom.ChatRoomName = reader.GetString(1);
                                chatRoom.Description = reader.GetString(2);
                                chatRoom.Category = reader.GetString(3);
                                chatRooms.Add(chatRoom);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return chatRooms;
        }

        // 내가 참여한 채팅방 출력
        public IEnumerable<ChatRooms> MyChatRoomsSearch(String searchWord)
        {
            var chatRooms = new List<ChatRooms>();

            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    string sql = "SELECT c.ChatRoomID, c.ChatRoomName, c.Description, c.Category FROM chatrooms c WHERE c.ChatRoomName = :searchWord or c.Description = :searchWord or c.Category = :searchWord";
                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("searchWord", searchWord));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ChatRooms chatRoom = new ChatRooms();
                                chatRoom.ChatRoomId = reader.GetGuid(0);
                                chatRoom.ChatRoomName = reader.GetString(1);
                                chatRoom.Description = reader.GetString(2);
                                chatRoom.Category = reader.GetString(3);
                                chatRooms.Add(chatRoom);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return chatRooms;
        }

        // 메시지 생성
        public void InsertMessage(Guid chatRoomId, Guid userId, string content)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    Guid messageId = Guid.NewGuid(); // 새로운 GUID 생성
                    byte[] messageIdBytes = messageId.ToByteArray(); // GUID를 바이트 배열로 변환
                    byte[] chatRoomIdBytes = chatRoomId.ToByteArray(); // GUID를 바이트 배열로 변환
                    byte[] userIdBytes = userId.ToByteArray(); // GUID를 바이트 배열로 변환

                    string sql = "insert into Message(MessageId, ChatRoomId, UserId, Content) values(:MessageId, :ChatRoomId, :UserId, :Content)";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("MessageId", OracleDbType.Raw, messageIdBytes, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("ChatRoomId", OracleDbType.Raw, chatRoomIdBytes, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("UserId", OracleDbType.Raw, userIdBytes, ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("Content", content));
                        // command.Parameters.Add(new OracleParameter("CreatedAt", OracleDbType.TimeStamp, createdAt, ParameterDirection.Input));

                        // SQL 명령문을 데이터베이스에 실행하도록 지시하는 메서드
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        // 방장 위임
        public void UpdateHost(Guid chatroomid, string updateHostID)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    // GUID -> 바이트 배열로 변환
                    byte[] chatroomIdBytes = chatroomid.ToByteArray();

                    // GUID 형식 확인 및 변환
                    Guid updateHostGuid = Guid.Parse(updateHostID); // GUID로 변환
                    byte[] updateHostBytes = updateHostGuid.ToByteArray(); // GUID를 byte[]로 변환

                    // 기존 방장의 ISOWNER 제거
                    string sql1 = @"
    UPDATE USERCHATROOMS
    SET ISOWNER = 0
    WHERE CHATROOMID = :chatroomIdBytes AND ISOWNER = 1";

                    // 새로운 방장의 ISOWNER 설정
                    string sql2 = @"
    UPDATE USERCHATROOMS
    SET ISOWNER = 1
    WHERE USERID = :updateHostBytes AND CHATROOMID = :chatroomIdBytes";

                    using (OracleCommand command1 = new OracleCommand(sql1, connection))
                    {
                        command1.Parameters.Add(new OracleParameter("chatroomIdBytes", OracleDbType.Raw, chatroomIdBytes, ParameterDirection.Input));
                        command1.ExecuteNonQuery();
                    }

                    using (OracleCommand command2 = new OracleCommand(sql2, connection))
                    {
                        command2.Parameters.Add(new OracleParameter("updateHostBytes", OracleDbType.Raw, updateHostBytes, ParameterDirection.Input));
                        command2.Parameters.Add(new OracleParameter("chatroomIdBytes", OracleDbType.Raw, chatroomIdBytes, ParameterDirection.Input));
                        command2.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        // 내가 참여한 채팅방 메시지 내역 출력
        public IEnumerable<Message> MessagePrint(Guid chatRoomID, Guid userID)
        {
            var messageList = new List<Message>();

            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    string sql = @"
                SELECT u.userid, u.nickname, m.content 
                FROM message m 
                INNER JOIN users u ON m.userid = u.userid 
                INNER JOIN userchatrooms ucr 
                    ON ucr.userid = :userID AND ucr.chatroomid = m.chatroomid 
                WHERE m.chatroomid = :chatRoomID 
                  AND m.sentat >= ucr.createdat 
                ORDER BY m.sentat";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("userID", OracleDbType.Raw, userID.ToByteArray(), ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("chatRoomID", OracleDbType.Raw, chatRoomID.ToByteArray(), ParameterDirection.Input));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var messageData = new Message
                                {
                                    SendUserID = new Guid(reader.GetFieldValue<byte[]>(0)),
                                    SendUserNickName = reader.GetString(1),
                                    SendMessage = reader.GetString(2)
                                };
                                messageList.Add(messageData);
                            }
                        }
                    }
                }
                catch (OracleException ex)
                {
                    Console.WriteLine($"Oracle Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected Error: {ex.Message}");
                }
            }

            return messageList;
        }

        // 사용자 아이디로 닉네임 출력
        public string SendNickName(string senderID)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    Guid senderIDGuid = Guid.Parse(senderID);
                    byte[] senderIDBytes = senderIDGuid.ToByteArray(); // GUID를 바이트 배열로 변환

                    string sql = "select nickname from Users where userid = :senderID";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("senderID", OracleDbType.Raw, senderIDBytes, ParameterDirection.Input));

                        object resultNickName = command.ExecuteScalar();

                        return resultNickName.ToString();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("오류 : " + ex);
                    return "false";
                }
            }
        }

        // 사용자 : 채팅방 나가기
        public void ExitUserChatRoom(Guid userID, Guid chatRoomID)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();

                    string sql = "delete from userchatrooms where userid = :userid and chatroomid = :chatRoomID";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("userid", OracleDbType.Raw, userID.ToByteArray(), ParameterDirection.Input));
                        command.Parameters.Add(new OracleParameter("chatRoomID", OracleDbType.Raw, chatRoomID.ToByteArray(), ParameterDirection.Input));

                        command.ExecuteNonQuery();
                    }
                } 
                catch (Exception ex)
                {
                    MessageBox.Show("오류 : " + ex);
                }
            }
        }

        // 방장이 채팅방을 나갈 경우
        // 채팅방, 사용자와 채팅방 관계, 메시지 모두 삭제
        public void DeleteChatRoomWithRelations(Guid chatID)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                connection.Open();
                using (OracleTransaction transaction = connection.BeginTransaction()) // 트랜잭션 시작
                {
                    try
                    {
                        // 1. 사용자와 채팅방 관계 삭제
                        string sql1 = "DELETE FROM userchatrooms WHERE chatroomID = :chatID";
                        using (OracleCommand command1 = new OracleCommand(sql1, connection))
                        {
                            command1.Transaction = transaction;
                            command1.Parameters.Add(new OracleParameter("chatID", OracleDbType.Raw, chatID.ToByteArray(), ParameterDirection.Input));
                            command1.ExecuteNonQuery();
                        }

                        // 2. 채팅방 메시지 삭제
                        string sql2 = "DELETE FROM Message WHERE chatroomID = :chatID";
                        using (OracleCommand command2 = new OracleCommand(sql2, connection))
                        {
                            command2.Transaction = transaction;
                            command2.Parameters.Add(new OracleParameter("chatID", OracleDbType.Raw, chatID.ToByteArray(), ParameterDirection.Input));
                            command2.ExecuteNonQuery();
                        }

                        // 3. 채팅방 삭제
                        string sql3 = "DELETE FROM chatrooms WHERE chatroomid = :chatID";
                        using (OracleCommand command3 = new OracleCommand(sql3, connection))
                        {
                            command3.Transaction = transaction;
                            command3.Parameters.Add(new OracleParameter("chatID", OracleDbType.Raw, chatID.ToByteArray(), ParameterDirection.Input));
                            command3.ExecuteNonQuery();
                        }

                        // 모든 작업이 성공하면 커밋
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // 오류 발생 시 롤백
                        transaction.Rollback();
                        MessageBox.Show("오류 : " + ex.Message);
                    }
                }
            }
        }

        public User GetUserData(string email)
        {
            // 데이터베이스에서 사용자 데이터 로드
            return new User
            {
                Email = email
            };
        }
    }
}
