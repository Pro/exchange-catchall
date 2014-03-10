using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Exchange.CatchAll
{
    class MysqlConnector : DatabaseConnector
    {
        public MysqlConnector(string connectionString)
        {
            try
            {
                sqlConnection = new MySqlConnection(connectionString);
            }
            catch (MySqlException ex)
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

                MySqlCommand command = new MySqlCommand();
                command.Connection = (MySqlConnection) sqlConnection;
                command.CommandText = "INSERT INTO Caught (date, original, replaced, message_id, subject) " +
                                        "Values(NOW(), @original, @replaced, @message_id, @subject)";
                command.Parameters.AddWithValue("@original", original);
                command.Parameters.AddWithValue("@replaced", replaced);
                command.Parameters.AddWithValue("@subject", subject);
                command.Parameters.AddWithValue("@message_id", message_id);
                command.ExecuteNonQuery();
                sqlConnection.Close();
            }
            catch (MySqlException ex)
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

                MySqlCommand command = new MySqlCommand("update blocked set hits=hits+1 where address=@address");
                command.Connection = (MySqlConnection) sqlConnection;
                command.Parameters.AddWithValue("@address", address);
                bool retVal = (command.ExecuteNonQuery() > 0);
                sqlConnection.Close();
                
                // if we updated a value, the address is blocked.
                return retVal;
            }
            catch (MySqlException ex)
            {
                Logger.LogError("SQL Exception in isBlocked: " + ex.Message);
            }

            return false;
        }
    }
}
