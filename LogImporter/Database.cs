
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace LogImporter
{
    public static class Database
    {
        private static string _connectionString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            }
        }

        public static DataTable GetAllMediaOrderDetail()
        {
            DataTable returnTable;

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("GetMODsForImporter", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                returnTable = new DataTable();
                returnTable.Load(reader);
            }

            return returnTable;
        }

        public static void UpdateMODFromLogs(int modId, DateTime airDate, DateTime airTime, string eType)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                SqlCommand cmd = new SqlCommand("UpdateMODFromLog", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@modid", modId);
                cmd.Parameters.AddWithValue("@airDate", airDate);
                cmd.Parameters.AddWithValue("@airTime", airTime);
                cmd.Parameters.AddWithValue("@eType", eType);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}