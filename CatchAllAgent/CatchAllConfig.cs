using Microsoft.Exchange.Data.Transport;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace Exchange.CatchAll
{
    /// <summary>
    /// CatchAllConfig: The configuration for the CatchAllAgent.
    /// </summary>
    public class CatchAllConfig
    {
        /// <summary>
        ///  The name of the configuration file.
        /// </summary>
        private static readonly string configFileName = "config.xml";

        /// <summary>
        /// Point out the directory with the configuration file (= assembly location)
        /// </summary>
        private string configDirectory;

        /// <summary>
        /// The filesystem watcher to monitor configuration file updates.
        /// </summary>
        private FileSystemWatcher configFileWatcher;

        /// <summary>
        /// The (domain to) catchall address map
        /// </summary>
        private Dictionary<string, RoutingAddress> addressMap;


        MySqlConnection sqlConnection = null;

        /// <summary>
        /// Whether reloading is ongoing
        /// </summary>
        private int reLoading = 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        public CatchAllConfig()
        {
            // Setup a file system watcher to monitor the configuration file
            this.configDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.configFileWatcher = new FileSystemWatcher(this.configDirectory);
            this.configFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
            this.configFileWatcher.Filter = "config.xml";
            this.configFileWatcher.Changed += new FileSystemEventHandler(this.OnChanged);

            // Create an initially empty map
            this.addressMap = new Dictionary<string, RoutingAddress>();

            // Load the configuration
            this.Load();

            // Now start monitoring
            this.configFileWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// The mapping between domain to catchall address.
        /// </summary>
        public Dictionary<string, RoutingAddress> AddressMap
        {
            get { return this.addressMap; }
        }

        /// <summary>
        /// Configuration changed handler.
        /// </summary>
        /// <param name="source">Event source.</param>
        /// <param name="e">Event arguments.</param>
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            // Ignore if load ongoing
            if (Interlocked.CompareExchange(ref this.reLoading, 1, 0) != 0)
            {
                Trace.WriteLine("load ongoing: ignore");
                return;
            }

            // (Re) Load the configuration
            this.Load();

            // Reset the reload indicator
            this.reLoading = 0;
        }

        public void LogCatch(string original, string replaced, string session_id)
        {
            if (sqlConnection == null)
                return;

            string myInsertQuery = "INSERT INTO Cought (date, original, replaced, session_id) " +
                                    "Values(NOW(), '" + original + "', '" + replaced + "', '" + session_id + "')";
            try
            {



                sqlConnection.Open();

                if (!sqlConnection.Ping())
                {
                    Trace.WriteLine("SQL Ping is false");
                    return;
                }

                MySqlCommand myCommand = new MySqlCommand(myInsertQuery);
                myCommand.Connection = sqlConnection;
                myCommand.ExecuteNonQuery();
                sqlConnection.Close();
            }
            catch (MySqlException ex)
            {
                Trace.WriteLine("SQL Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception: " + ex.Message);
            }


        }

        public bool isBlocked(string address)
        {
            if (sqlConnection == null)
                return false;

            string myCntQuery = "update blocked set hits=hits+1 where address=\"" + address + "\"";

            try
            {


                sqlConnection.Open();

                if (!sqlConnection.Ping())
                {
                    Trace.WriteLine("SQL Ping is false");
                    return false;
                }

                MySqlCommand myCommand = new MySqlCommand(myCntQuery);
                myCommand.Connection = sqlConnection;
                bool retVal = (myCommand.ExecuteNonQuery() > 0);
                sqlConnection.Close();
                return retVal;

            }
            catch (MySqlException ex)
            {
                Trace.WriteLine("SQL Exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception: " + ex.Message);
            }

            return false;


        }

        /// <summary>
        /// Load the configuration file. If any errors occur, does nothing.
        /// </summary>
        private void Load()
        {
            // Load the configuration
            XmlDocument doc = new XmlDocument();
            bool docLoaded = false;
            string fileName = Path.Combine(
                this.configDirectory,
                CatchAllConfig.configFileName);

            try
            {
                doc.Load(fileName);
                docLoaded = true;
            }
            catch (FileNotFoundException)
            {
                Trace.WriteLine("configuration file not found: {0}", fileName);
            }
            catch (XmlException e)
            {
                Trace.WriteLine("XML error: {0}", e.Message);
            }
            catch (IOException e)
            {
                Trace.WriteLine("IO error: {0}", e.Message);
            }

            // If a failure occured, ignore and simply return
            if (!docLoaded || doc.FirstChild == null)
            {
                Trace.WriteLine("configuration error: either no file or an XML error");
                return;
            }

            // Create a dictionary to hold the mappings
            Dictionary<string, RoutingAddress> map = new Dictionary<string, RoutingAddress>(100);

            // Track whether there are invalid entries
            bool invalidEntries = false;

            // Validate all entries and load into a dictionary
            foreach (XmlNode node in doc.FirstChild.ChildNodes)
            {
                if (string.Compare(node.Name, "domain", true, CultureInfo.InvariantCulture) == 0)
                {

                    XmlAttribute domain = node.Attributes["name"];
                    XmlAttribute address = node.Attributes["address"];

                    // Validate the data
                    if (domain == null || address == null)
                    {
                        invalidEntries = true;
                        Trace.WriteLine("reject configuration due to an incomplete entry. (Either or both domain and address missing.)");
                        break;
                    }

                    if (!RoutingAddress.IsValidAddress(address.Value))
                    {
                        invalidEntries = true;
                        Trace.WriteLine(String.Format("reject configuration due to an invalid address ({0}).", address));
                        break;
                    }

                    // Add the new entry
                    string lowerDomain = domain.Value.ToLower();
                    map[lowerDomain] = new RoutingAddress(address.Value);

                    Trace.WriteLine(String.Format("added entry ({0} -> {1})", lowerDomain, address.Value));
                }
                else if (string.Compare(node.Name, "logging", true, CultureInfo.InvariantCulture) == 0)
                {
                    if (sqlConnection != null && sqlConnection.State == System.Data.ConnectionState.Open)
                    {
                        sqlConnection.Close();
                    }

                    XmlAttribute enabled = node.Attributes["enabled"];

                    if (string.Compare(enabled.Value, "true", true, CultureInfo.InvariantCulture) != 0)
                    {
                        sqlConnection = null;
                    }
                    else
                    {
                        XmlAttribute host = node.Attributes["host"];
                        XmlAttribute database = node.Attributes["database"];
                        XmlAttribute user = node.Attributes["user"];
                        XmlAttribute password = node.Attributes["password"];



                        string myConnectionString = "SERVER=" + host.Value + ";" +
                                "UID=" + user.Value + ";" +
                                "PWD=" + password.Value + ";" +
                                "DATABASE=" + database.Value + ";";

                        try
                        {
                            sqlConnection = new MySqlConnection(myConnectionString);
                        }
                        catch (MySqlException ex)
                        {
                            Trace.WriteLine("SQL Exception: " + ex.Message);
                        }
                    }

                }
            }

            // If there are no invalid entries, swap in the map
            if (!invalidEntries)
            {
                Interlocked.Exchange<Dictionary<string, RoutingAddress>>(ref this.addressMap, map);
                Trace.WriteLine("accepted configuration");
            }
        }
    }
}
