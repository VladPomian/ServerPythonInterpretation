using Serilog;
using System.Data;
using System.Data.SqlClient;

namespace Python_Interpretation.Repository
{
    public class ArchiveRepository
    {
        private static string _connectionString = "<YOUR_SQL_CONNECTION_STRING>";

        public void ArchiveForecastData(string data, int sizeData, string status)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("InsertAndUpdateRecords", connection);
                command.CommandType = CommandType.StoredProcedure;

                command.Parameters.AddWithValue("@time_enter", DateTime.Now);
                command.Parameters.AddWithValue("@data", data);
                command.Parameters.AddWithValue("@size_data", sizeData);
                command.Parameters.Add(new SqlParameter("@status", SqlDbType.NVarChar, 50) { Value = status.Trim() });

                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                    Log.Information("Процедура обновления данных прогнозирования успешно выполнена.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка при вызове процедуры InsertAndUpdateRecords");
                }
            }
        }

        public StormPredictionModel RetrieveLastRecordFromTable(string table)
        {
            string query = $@"
                SELECT [time_enter], [data], [size_data], [status]
                FROM [Storm_Prediction].[dbo].[{table}] 
            ";

            StormPredictionModel? record = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand(query, connection);

                try
                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    if (reader.Read())
                    {
                        record = new StormPredictionModel
                        {
                            TimeEnter = reader.GetDateTime(0),
                            Data = reader.GetString(1),
                            SizeData = reader.GetInt32(2),
                            Status = reader.GetString(3)
                        };
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Ошибка при получении данных прогнозирования");
                }
            }

            return record;
        }
    }
}
