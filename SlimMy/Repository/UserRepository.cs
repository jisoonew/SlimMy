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

                string userSQL = @"select gender, birth_date, diet_goal, password from users where userid = :user_id";

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
        public async Task<string> AllNickName()
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    await connection.OpenAsync();
                    string sql = "select nickname from Users";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        object resultNickName = await command.ExecuteScalarAsync();

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
    }
}
