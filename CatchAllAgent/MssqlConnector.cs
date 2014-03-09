using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Exchange.CatchAll
{
    class MssqlConnector : DatabaseConnector
    {
        public MssqlConnector(string connectionString)
        {
            try
            {
                sqlConnection = new SqlConnection(connectionString);
            }
            catch (SqlException ex)
            {
                Logger.LogError("Couldn't open database connection: " + ex.Message + "\n Not using database");
                Trace.WriteLine("SQL Exception: " + ex.Message);
            }
        }

        public override void LogCatch(string original, string replaced, string subject, string message_id)
        {
            if (sqlConnection == null)
                return;

            try
            {
                sqlConnection.Open();

                SqlCommand command = new SqlCommand();
                command.Connection = (SqlConnection)sqlConnection;
                command.CommandText = "INSERT INTO Cought (date, original, replaced, message_id, subject) " +
                                        "Values(GetDate(), @original, @replaced, @message_id, @subject)";
                command.Parameters.AddWithValue("@original", original);
                command.Parameters.AddWithValue("@replaced", replaced);
                command.Parameters.AddWithValue("@subject", subject);
                command.Parameters.AddWithValue("@message_id", message_id);
                command.ExecuteNonQuery();
                sqlConnection.Close();
            }
            catch (SqlException ex)
            {
                Logger.LogError("SQL LogCatch Exception: " + ex.Message);
            }
        }

        public override bool isBlocked(string address)
        {
            if (sqlConnection == null)
                return false;
            
            try
            {
                sqlConnection.Open();

                SqlCommand command = new SqlCommand("update blocked set hits=hits+1 where address=@address");
                command.Connection = (SqlConnection) sqlConnection;
                command.Parameters.AddWithValue("@address", address);
                bool retVal = (command.ExecuteNonQuery() > 0);
                sqlConnection.Close();
                
                // if we updated a value, the address is blocked.
                return retVal;
            }
            catch (SqlException ex)
            {
                Logger.LogError("SQL Exception in isBlocked: " + ex.Message);
            }

            return false;
        }
    }
}
