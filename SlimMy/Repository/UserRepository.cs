using Oracle.ManagedDataAccess.Client;
using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy.Repository
{
    public class UserRepository
    {
        private readonly string _connString;

        public UserRepository(string connString)
        {
            _connString = connString;
        }

        // 사용자 정보 가져오기
        public async Task<User> GetUserData(Guid userID)
        {
            User plannerItems = new User();

            using (var connection = new OracleConnection(_connString))
            {
                await connection.OpenAsync();

                string sql = @"select height, weight from body_log where user_id = :user_id and rownum = 1 order by log_date desc";

                using (var command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add(new OracleParameter("user_id", OracleDbType.Raw)).Value = userID.ToByteArray();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            plannerItems.Height = reader.GetDouble(0);
                            plannerItems.Weight = reader.GetDouble(1);
                        }
                    }
                }

                string userSQL = @"select gender, birth_date, diet_goal, password, nickname from users where userid = :user_id";

                using (var command = new OracleCommand(userSQL, connection))
                {
                    command.Parameters.Add(new OracleParameter("user_id", OracleDbType.Raw)).Value = userID.ToByteArray();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            plannerItems.Gender = reader.GetString(0);
                            plannerItems.BirthDate = reader.GetDateTime(1);
                            plannerItems.DietGoal = reader.GetString(2);
                            plannerItems.Password = reader.GetString(3);
                            plannerItems.NickName = reader.GetString(4);
                        }
                    }
                }
                return plannerItems;
            }
        }

        // 사용자 정보 수정
        public async Task UpdateUserData(Guid userID, DateTime plannerDate, double weight, double height, double bmi, double targetWeight, string memo)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (OracleTransaction transaction = connection.BeginTransaction())
                    {
                        Guid plannerGroupId = Guid.NewGuid();
                        byte[] plannerGroupIdBytes = plannerGroupId.ToByteArray();

                        string insertGroupQuery = @"update body_log set weight = :weight, height = :height, bmi = :bmi, target_Weight = :target_Weight where user_id = :user_id and log_date = :log_date";

                        using (OracleCommand groupCmd = new OracleCommand(insertGroupQuery, connection))
                        {
                            groupCmd.Transaction = transaction;
                            groupCmd.Parameters.Add("weight", OracleDbType.Double).Value = weight;
                            groupCmd.Parameters.Add("height", OracleDbType.Double).Value = height;
                            groupCmd.Parameters.Add("bmi", OracleDbType.Double).Value = bmi;
                            groupCmd.Parameters.Add("targetWeight", OracleDbType.Double).Value = targetWeight;
                            groupCmd.Parameters.Add("user_id", OracleDbType.Raw).Value = userID.ToByteArray();
                            groupCmd.Parameters.Add("log_date", OracleDbType.Date).Value = plannerDate.Date;
                            await groupCmd.ExecuteNonQueryAsync();
                        }

                        Guid WeightMemoId = Guid.NewGuid();
                        byte[] WeightMemoIdBytes = WeightMemoId.ToByteArray();

                        DateTime dateTime = DateTime.Now;

                        string insertMemoQuery = @"update memo set content = :content, updated_at = :updated_at where user_id = :user_id and log_date = :log_date";

                        using (OracleCommand memoCmd = new OracleCommand(insertMemoQuery, connection))
                        {
                            memoCmd.Transaction = transaction;

                            memoCmd.Parameters.Add("content", OracleDbType.Clob).Value = string.IsNullOrEmpty(memo) ? DBNull.Value : memo;
                            memoCmd.Parameters.Add("updated_at", OracleDbType.TimeStamp).Value = dateTime;
                            memoCmd.Parameters.Add("user_id", OracleDbType.Raw).Value = userID.ToByteArray();
                            memoCmd.Parameters.Add("log_date", OracleDbType.Date).Value = plannerDate.Date;

                            await memoCmd.ExecuteNonQueryAsync();
                        }

                        // 커밋
                        await transaction.CommitAsync();
                        MessageBox.Show("몸무게 수정이 완료되었습니다.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("UpdateWeight 저장 오류: " + ex.Message);
                }
            }
        }

        // 모든 닉네임
        public async Task<List<string>> AllNickName()
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                var nicknames = new List<string>();

                try
                {
                    await connection.OpenAsync();
                    string sql = "select nickname from Users";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    using (OracleDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            nicknames.Add(reader.GetString(0)); // 닉네임 컬럼
                        }
                    }

                    return nicknames;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("AllNickName 오류 : " + ex.Message);
                    return new List<string>(); // 빈 리스트 반환
                }
            }
        }

        // 닉네임 수정
        public async Task NickNameSave(Guid userID, string nickName)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (OracleTransaction transaction = connection.BeginTransaction())
                    using (OracleCommand command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "UPDATE Users SET nickname = :nickname WHERE userid = :userid";
                        command.Parameters.Add(new OracleParameter("nickname", nickName));
                        command.Parameters.Add("userid", OracleDbType.Raw).Value = userID.ToByteArray();

                        int rows = await command.ExecuteNonQueryAsync();

                        if (rows > 0)
                        {
                            await transaction.CommitAsync();
                            MessageBox.Show("닉네임이 변경되었습니다.");
                        }
                        else
                        {
                            transaction.Rollback();
                            MessageBox.Show("닉네임 변경 실패: 해당 유저를 찾을 수 없습니다.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("NickNameSave 오류: " + ex.Message);
                }
            }
        }

        // 사용자 정보 수정
        public async Task UpdateMyPageUserData(Guid userID, double height, string password, string diet_goal)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (OracleTransaction transaction = connection.BeginTransaction())
                    {
                        Guid plannerGroupId = Guid.NewGuid();
                        byte[] plannerGroupIdBytes = plannerGroupId.ToByteArray();

                        string insertGroupQuery = @"update Users set password = :password, height = :height, Diet_goal = :Diet_goal, updated_at = :updated_at where userid = :user_id";

                        using (OracleCommand groupCmd = new OracleCommand(insertGroupQuery, connection))
                        {
                            groupCmd.Transaction = transaction;
                            groupCmd.Parameters.Add("password", OracleDbType.Varchar2).Value = password;
                            groupCmd.Parameters.Add("height", OracleDbType.Double).Value = height;
                            groupCmd.Parameters.Add("Diet_goal", OracleDbType.Varchar2).Value = diet_goal;
                            groupCmd.Parameters.Add("updated_at", OracleDbType.TimeStamp).Value = DateTime.Now;
                            groupCmd.Parameters.Add("user_id", OracleDbType.Raw).Value = userID.ToByteArray();
                            await groupCmd.ExecuteNonQueryAsync();
                        }
                        // 커밋
                        await transaction.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("UpdateMyPageUserData 저장 오류: " + ex.Message);
                }
            }
        }

        // 탈퇴
        public async Task DeleteAccountView(Guid userID)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (OracleTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            DateTime deletedAt = DateTime.Now;

                            string[] updateQueries = new string[]
                            {
                                "UPDATE users SET status = 'INACTIVE', deleted_at = SYSTIMESTAMP WHERE userid = :user_id",
            "UPDATE chatrooms SET deleted_at = SYSTIMESTAMP WHERE chatroomid in (select chatroomid from userchatrooms where userid = :user_id and isowner = 1)",
            "UPDATE userchatrooms SET deleted_at = SYSTIMESTAMP WHERE userid = :user_id",
            "UPDATE body_log SET deleted_at = SYSTIMESTAMP WHERE user_id = :user_id",
            "UPDATE plannergroup SET deleted_at = SYSTIMESTAMP WHERE userid = :user_id",
            "UPDATE memo SET deleted_at = SYSTIMESTAMP WHERE user_id = :user_id",
            "UPDATE message SET deleted_at = SYSTIMESTAMP WHERE userid = :user_id",
            "UPDATE userconnections SET deleted_at = SYSTIMESTAMP WHERE userid = :user_id"
                            };

                            foreach (var query in updateQueries)
                            {
                                using (OracleCommand cmd = new OracleCommand(query, connection))
                                {
                                    cmd.Transaction = transaction;
                                    cmd.Parameters.Add("user_id", OracleDbType.Raw).Value = userID.ToByteArray();
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            MessageBox.Show("회원 탈퇴가 정상적으로 처리되었습니다.\n그동안 이용해주셔서 감사합니다.");

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("UpdateMyPageUserData 저장 오류: " + ex.Message);
                }
            }
        }
    }
}
