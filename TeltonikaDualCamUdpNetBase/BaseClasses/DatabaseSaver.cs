using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeltonikaDualCamUdpNetBase.BaseClasses
{
    public sealed class SigletonDB
    {
        private static readonly SqlConnection _sqlConnection = new SqlConnection(ConfigurationManager.AppSettings["sqlServer"]);
        public static SqlConnection Instance
        {
            get { return _sqlConnection; }
        }
    }
    public  class DatabaseSaver
    {
        public List<int> BulkWriteToDatabase(string toSaveDb)
        {
            Utils.LogToFile(2, "[INFO]", "Calling BulkWriteToDatabase()");
            List<int> errorIds = new List<int>();
            try
            {
                SqlConnection sqlConnection = SigletonDB.Instance;
                if (sqlConnection.State != System.Data.ConnectionState.Open)
                {
                    sqlConnection.Open();
                }

                SqlCommand sqlCommand = new SqlCommand(toSaveDb, sqlConnection);
                sqlCommand.CommandType = System.Data.CommandType.Text;
                sqlCommand.CommandTimeout = (string.IsNullOrEmpty(ConfigurationManager.AppSettings["DbSaveCommandTimeout"])) ? 30 : Convert.ToInt32(ConfigurationManager.AppSettings["DbSaveCommandTimeout"]);
                SqlDataReader dataReader = sqlCommand.ExecuteReader();
                while (dataReader.Read())
                {
                    errorIds.Add(Convert.ToInt32(dataReader["Id"]));
                }
               //return errorIds;
            }
            catch (Exception ex)
            {
                Utils.LogToFile(1,"[EXCEPTION]" , $"Error In BulkWriteToDatabase(): {ex.Message.ToString()}");
                throw;
            }

            return errorIds;
        }

    }
}
