using ConfigurationSettings;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Exchange.CatchAll
{
    class DatabaseConnector
    {
        private MySqlConnection sqlConnection = null;

        public DatabaseConnector()
        {

            Database settings = CatchAllFactory.GetCustomConfig<Database>("customSection/database");
            if (settings == null)
            {
                Logger.LogWarning("No database settings found. Not using database connection.");
                return;
            }

            if (settings.Enabled)
            {
                if (settings.Type.Equals("mysql", StringComparison.OrdinalIgnoreCase))
                {
                    string myConnectionString = "SERVER=" + settings.Host + ";PORT=" + settings.Port + ";" +
                                                    "UID=" + settings.User + ";" +
                                                    "PWD=" + settings.Password + ";" +
                                                    "DATABASE=" + settings.Schema + ";";
                    try
                    {
                        sqlConnection = new MySqlConnection(myConnectionString);
                    }
                    catch (MySqlException ex)
                    {
                        Logger.LogError("Couldn't open database connection: " + ex.Message + "\n Not using database");
                        Trace.WriteLine("SQL Exception: " + ex.Message);
                    }
                }
                else
                {
                    Logger.LogError("Configuration: Database has invalid type setting: " + settings.Type + "\nMust be 'mysql'.");
                }
            }
            else
            {
                Logger.LogInformation("Database connection disabled in config file.");
            }

                      
        }

        public void LogCatch(string original, string replaced, string subject, string message_id)
        {
            if (sqlConnection == null)
                return;

            try
            {



                sqlConnection.Open();

                MySqlCommand myCommand = new MySqlCommand();
                myCommand.Connection = sqlConnection;
                myCommand.CommandText = "INSERT INTO Cought (date, original, replaced, message_id, subject) " +
                                    "Values(NOW(), @original, @replaced, @message_id, @subject)";
                myCommand.Parameters.AddWithValue("@original", original);
                myCommand.Parameters.AddWithValue("@replaced", replaced);
                myCommand.Parameters.AddWithValue("@subject", subject);
                myCommand.Parameters.AddWithValue("@message_id", message_id);
                myCommand.ExecuteNonQuery();
                sqlConnection.Close();
            }
            catch (MySqlException ex)
            {
                Logger.LogError("SQL LogCatch Exception: " + ex.Message);
            }
        }

        public bool isBlocked(string address)
        {
            if (sqlConnection == null)
                return false;
            
            try
            {


                sqlConnection.Open();

                MySqlCommand myCommand = new MySqlCommand("update blocked set hits=hits+1 where address=@address");
                myCommand.Connection = sqlConnection;
                myCommand.Parameters.AddWithValue("@address", address);
                bool retVal = (myCommand.ExecuteNonQuery() > 0);
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
