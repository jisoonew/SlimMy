using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SlimMy.Repository
{
    class WeightHistoryRepository
    {
        private readonly string _connString;

        public WeightHistoryRepository(string connString)
        {
            _connString = connString;
        }

        // 몸무게 정보 가져오기
        public async Task<List<WeightRecordItem>> GetWeightHistory(Guid userID)
        {
            var plannerItems = new List<WeightRecordItem>();

            using (var connection = new OracleConnection(_connString))
            {
                await connection.OpenAsync();

                string sql = @"select body_log_id, log_date, height, weight, bmi, target_weight from body_log where user_id = :user_id";

                using (var command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add(new OracleParameter("user_id", OracleDbType.Raw)).Value = userID.ToByteArray();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var groupIdBinary = (OracleBinary)reader.GetOracleValue(0);
                            var groupId = groupIdBinary.IsNull ? Guid.Empty : new Guid(groupIdBinary.Value);

                            var targetWeightVal = reader.GetOracleDecimal(5);

                            var item = new WeightRecordItem
                            {
                                BodyID = groupId,
                                Date = reader.GetDateTime(1),
                                Height = reader.GetDouble(2),
                                Weight = reader.GetDouble(3),
                                BMI = reader.GetDouble(4),
                                TargetWeight = targetWeightVal.IsNull ? (double?)null : (double)targetWeightVal.Value
                        };

                            plannerItems.Add(item);
                        }
                    }
                }
            }
            return plannerItems;
        }

        // 몸무게 정보 저장하기
        public async Task InsertWeight(Guid userID, DateTime plannerDate, double weight, double height, double bmi, double targetWeight, string memo)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    await connection.OpenAsync();
                    using (OracleTransaction transaction = connection.BeginTransaction())
                    {
                        // 1. PlannerGroup 저장
                        Guid plannerGroupId = Guid.NewGuid();
                        byte[] plannerGroupIdBytes = plannerGroupId.ToByteArray();

                        string insertGroupQuery = @"INSERT INTO body_log (body_log_id, user_id, log_date, weight, height, bmi, target_Weight) VALUES (:body_log_id, :user_id, :log_date, :weight, :height, :bmi, :targetWeight)";

                        using (OracleCommand groupCmd = new OracleCommand(insertGroupQuery, connection))
                        {
                            groupCmd.Transaction = transaction;
                            groupCmd.Parameters.Add("body_log_id", OracleDbType.Raw).Value = plannerGroupIdBytes;
                            groupCmd.Parameters.Add("user_id", OracleDbType.Raw).Value = userID.ToByteArray();
                            groupCmd.Parameters.Add("log_date", OracleDbType.Date).Value = plannerDate.Date;
                            groupCmd.Parameters.Add("weight", OracleDbType.Double).Value = weight;
                            groupCmd.Parameters.Add("height", OracleDbType.Double).Value = height;
                            groupCmd.Parameters.Add("bmi", OracleDbType.Double).Value = bmi;
                            groupCmd.Parameters.Add("targetWeight", OracleDbType.Double).Value = targetWeight;
                            await groupCmd.ExecuteNonQueryAsync();
                        }

                        // 1. PlannerGroup 저장
                        Guid WeightMemoId = Guid.NewGuid();
                        byte[] WeightMemoIdBytes = WeightMemoId.ToByteArray();

                        string insertMemoQuery = @"INSERT INTO memo (MEMO_ID, USER_ID, LOG_DATE, CONTENT) VALUES (:MEMO_ID, :USER_ID, :LOG_DATE, :CONTENT)";

                        using (OracleCommand memoCmd = new OracleCommand(insertMemoQuery, connection))
                        {
                            memoCmd.Transaction = transaction;
                            memoCmd.Parameters.Add("MEMO_ID", OracleDbType.Raw).Value = WeightMemoIdBytes;
                            memoCmd.Parameters.Add("USER_ID", OracleDbType.Raw).Value = userID.ToByteArray();
                            memoCmd.Parameters.Add("LOG_DATE", OracleDbType.Date).Value = plannerDate.Date;
                            memoCmd.Parameters.Add("CONTENT", OracleDbType.Clob).Value = string.IsNullOrEmpty(memo) ? DBNull.Value : memo;

                            await memoCmd.ExecuteNonQueryAsync();
                        }

                        // 커밋
                        await transaction.CommitAsync();
                        MessageBox.Show("몸무게 저장이 완료되었습니다.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("InsertWeight 저장 오류: " + ex.Message);
                }
            }
        }
    }
}
