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
                } catch (Exception ex)
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
                        if(count >= 1)
                        {
                            MessageBox.Show("닉네임이 이미 존재합니다.");
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                } catch (Exception ex)
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
                catch(Exception ex)
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
                catch(Exception ex)
                {
                    MessageBox.Show("오류 : " + ex);
                    return "false";
                }
            }
        }

        // 채팅방 생성
        public void InsertChatRoom(string chatRoomName, string description, string category, DateTime createdAt)
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
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        // 채팅방 출력
        public List<ChatRooms> SelectChatRoom()
        {
            List<ChatRooms> chatRooms = new List<ChatRooms>();
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
                            while(reader.Read())
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
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            return chatRooms;
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
