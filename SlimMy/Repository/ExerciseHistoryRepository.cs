using Oracle.ManagedDataAccess.Client;
using SlimMy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimMy.Repository
{
    class ExerciseHistoryRepository
    {
        private readonly string _connString;

        public ExerciseHistoryRepository(string connString)
        {
            _connString = connString;
        }

        public async Task<List<WorkoutHistoryItem>> GetExerciseHistory(Guid userID)
        {
            var plannerItems = new List<WorkoutHistoryItem>();

            using (var connection = new OracleConnection(_connString))
            {
                await connection.OpenAsync();

                string sql = @"select plg.plannerdate, exin.exercisename, pln.minutes, pln.calories, exin.category
from planner pln join plannergroup plg on pln.plannergroupid = plg.plannergroupid 
join exercise_info exin on pln.exercise_info_id = exin.exercise_info_id where plg.userid = :userid order by plg.plannerdate desc";

                using (var command = new OracleCommand(sql, connection))
                {
                    command.Parameters.Add(new OracleParameter("userid", OracleDbType.Raw)).Value = userID.ToByteArray();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var item = new WorkoutHistoryItem
                            {
                                PlannerDate = reader.GetDateTime(0),
                                ExerciseName = reader.GetString(1),
                                Minutes = reader.GetInt32(2),
                                Calories = reader.GetInt32(3),
                                Category = reader.GetString(4)
                            };

                            plannerItems.Add(item);
                        }
                    }
                }
            }

            return plannerItems;
        }
    }
}
