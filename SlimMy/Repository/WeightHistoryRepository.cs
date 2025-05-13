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

                string sql = @"select body_log_id, log_date, height, weight, bmi, target_weight from body_log where user_id = :user_id order by log_date desc";

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

        // 몸무게 정보 저장
        public async Task InsertWeight(Guid userID, DateTime plannerDate, double weight, double height, double bmi, double targetWeight, string memo)
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

        // 몸무게 정보 여부
        public async Task<int> GetTodayWeightCompleted(DateTime now, Guid userID)
        {
            int totalCalories = 0;

            using (var connection = new OracleConnection(_connString))
            {
                await connection.OpenAsync();

                string sql = @"select count(*) from body_log where user_id = :user_id and log_date = :log_date";

                using (var command = new OracleCommand(sql, connection))
                {

                    command.Parameters.Add(new OracleParameter("user_id", OracleDbType.Raw)).Value = userID.ToByteArray();
                    command.Parameters.Add(new OracleParameter("log_date", OracleDbType.Date)).Value = now.Date;

                    totalCalories = Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
            return totalCalories;
        }

        // 몸무게 정보 수정
        public async Task UpdatetWeight(Guid userID, DateTime plannerDate, double weight, double height, double bmi, double targetWeight, string memo)
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

        // 메모장 내용 가져오기
        public async Task<WeightMemoItem> GetMemoContent(DateTime now, Guid userID)
        {
            var weightMemoItems = new List<WeightMemoItem>();

            using (var connection = new OracleConnection(_connString))
            {
                await connection.OpenAsync();

                string sql = @"select memo_id, content from memo where user_id = :user_id and log_date = :log_date";

                using (var command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add(new OracleParameter("user_id", OracleDbType.Raw)).Value = userID.ToByteArray();
                    command.Parameters.Add("log_date", OracleDbType.Date).Value = now.Date;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var groupIdBinary = (OracleBinary)reader.GetOracleValue(0);
                            var groupId = groupIdBinary.IsNull ? Guid.Empty : new Guid(groupIdBinary.Value);

                            return new WeightMemoItem
                            {
                                MemoID = groupId,
                                Content = reader.GetString(1)
                            };
                        }
                    }
                }
            }
            return null;
        }

        // 몸무게 이력 삭제
        public async Task DeleteWeight(Guid bodyLogID, Guid memoID)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    await connection.OpenAsync();

                    string sql = "delete from body_log where body_log_id = :bodyLogID";

                    using (OracleCommand command = new OracleCommand(sql, connection))
                    {
                        command.Parameters.Add(new OracleParameter("bodyLogID", OracleDbType.Raw, bodyLogID.ToByteArray(), ParameterDirection.Input));

                        await command.ExecuteNonQueryAsync();
                    }

                    string memoSQL = "delete from memo where memo_id = :memo_id";

                    using (OracleCommand memoCommand = new OracleCommand(memoSQL, connection))
                    {
                        memoCommand.Parameters.Add(new OracleParameter("memo_id", OracleDbType.Raw, memoID.ToByteArray(), ParameterDirection.Input));

                        await memoCommand.ExecuteNonQueryAsync();
                    }

                    MessageBox.Show("몸무게 이력이 삭제되었습니다.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DeleteWeight 오류 : " + ex);
                }
            }
        }

        // 메모장 내용 검색
        public async Task<WeightMemoRecord> GetSearchedMemoContent(Guid userID, string searchMemo)
        {
            var weightMemoItems = new List<WeightMemoItem>();

            using (var connection = new OracleConnection(_connString))
            {
                await connection.OpenAsync();

                string sql = @"SELECT bl.body_log_id, bl.log_date, bl.height, bl.weight, bl.bmi, bl.target_weight, mm.memo_id, mm.content 
FROM body_log bl
JOIN memo mm ON bl.user_id = mm.user_id
WHERE mm.user_id = :user_id and mm.log_date = bl.log_date
  AND mm.content LIKE '%' || :content || '%'";

                using (var command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add(new OracleParameter("user_id", OracleDbType.Raw)).Value = userID.ToByteArray();
                    command.Parameters.Add("content", OracleDbType.Clob).Value = string.IsNullOrEmpty(searchMemo) ? DBNull.Value : searchMemo;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var groupIdBinary = (OracleBinary)reader.GetOracleValue(0);
                            var bodyId = groupIdBinary.IsNull ? Guid.Empty : new Guid(groupIdBinary.Value);

                            var targetWeightVal = reader.GetOracleDecimal(5);


                            var memoIdBinary = (OracleBinary)reader.GetOracleValue(6);
                            var memoId = memoIdBinary.IsNull ? Guid.Empty : new Guid(memoIdBinary.Value);

                            var record = new WeightRecordItem
                            {
                                BodyID = bodyId,
                                Date = reader.GetDateTime(1),
                                Height = reader.GetDouble(2),
                                Weight = reader.GetDouble(3),
                                BMI = reader.GetDouble(4),
                                TargetWeight = targetWeightVal.IsNull ? (double?)null : (double)targetWeightVal.Value
                            };

                            var memoItem = new WeightMemoItem
                            {
                                MemoID = memoId,
                                Content = reader.GetString(7)
                            };

                            return new WeightMemoRecord
                            {
                                Record = record,
                                Memo = memoItem
                            };
                        }
                    }
                }
            }
            return null;
        }

        // 메모장 내용 검색
        public async Task<WeightMemoRecord> GetSearchedDate(Guid userID, DateTime now)
        {
            var weightMemoItems = new List<WeightMemoItem>();

            using (var connection = new OracleConnection(_connString))
            {
                await connection.OpenAsync();

                string sql = @"SELECT bl.body_log_id, bl.log_date, bl.height, bl.weight, bl.bmi, bl.target_weight, mm.memo_id, mm.content 
FROM body_log bl
JOIN memo mm ON bl.user_id = mm.user_id
WHERE mm.user_id = :user_id 
  AND TRUNC(bl.log_date) = TRUNC(:log_date)
  AND TRUNC(mm.log_date) = TRUNC(:log_date)";

                using (var command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add(new OracleParameter("user_id", OracleDbType.Raw)).Value = userID.ToByteArray();
                    command.Parameters.Add("log_date", OracleDbType.Date).Value = now.Date;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var groupIdBinary = (OracleBinary)reader.GetOracleValue(0);
                            var bodyId = groupIdBinary.IsNull ? Guid.Empty : new Guid(groupIdBinary.Value);

                            var targetWeightVal = reader.GetOracleDecimal(5);


                            var memoIdBinary = (OracleBinary)reader.GetOracleValue(6);
                            var memoId = memoIdBinary.IsNull ? Guid.Empty : new Guid(memoIdBinary.Value);

                            var record = new WeightRecordItem
                            {
                                BodyID = bodyId,
                                Date = reader.GetDateTime(1),
                                Height = reader.GetDouble(2),
                                Weight = reader.GetDouble(3),
                                BMI = reader.GetDouble(4),
                                TargetWeight = targetWeightVal.IsNull ? (double?)null : (double)targetWeightVal.Value
                            };

                            var memoItem = new WeightMemoItem
                            {
                                MemoID = memoId,
                                Content = reader.GetString(7)
                            };

                            return new WeightMemoRecord
                            {
                                Record = record,
                                Memo = memoItem
                            };
                        }
                    }
                }
            }
            return null;
        }

        // 몸무게 검색
        public async Task<WeightMemoRecord> GetSearchedWeight(Guid userID, double weight)
        {
            var weightMemoItems = new List<WeightMemoItem>();

            using (var connection = new OracleConnection(_connString))
            {
                await connection.OpenAsync();

                string sql = @"SELECT bl.body_log_id, bl.log_date, bl.height, bl.weight, bl.bmi, bl.target_weight, mm.memo_id, mm.content 
FROM body_log bl
JOIN memo mm ON bl.user_id = mm.user_id
WHERE mm.user_id = :user_id and bl.weight = :weight and bl.log_date = mm.log_date";

                using (var command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add(new OracleParameter("user_id", OracleDbType.Raw)).Value = userID.ToByteArray();
                    command.Parameters.Add("weight", OracleDbType.Double).Value = weight;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var groupIdBinary = (OracleBinary)reader.GetOracleValue(0);
                            var bodyId = groupIdBinary.IsNull ? Guid.Empty : new Guid(groupIdBinary.Value);

                            var targetWeightVal = reader.GetOracleDecimal(5);


                            var memoIdBinary = (OracleBinary)reader.GetOracleValue(6);
                            var memoId = memoIdBinary.IsNull ? Guid.Empty : new Guid(memoIdBinary.Value);

                            var record = new WeightRecordItem
                            {
                                BodyID = bodyId,
                                Date = reader.GetDateTime(1),
                                Height = reader.GetDouble(2),
                                Weight = reader.GetDouble(3),
                                BMI = reader.GetDouble(4),
                                TargetWeight = targetWeightVal.IsNull ? (double?)null : (double)targetWeightVal.Value
                            };

                            var memoItem = new WeightMemoItem
                            {
                                MemoID = memoId,
                                Content = reader.GetString(7)
                            };

                            return new WeightMemoRecord
                            {
                                Record = record,
                                Memo = memoItem
                            };
                        }
                    }
                }
            }
            return null;
        }
    }
}
