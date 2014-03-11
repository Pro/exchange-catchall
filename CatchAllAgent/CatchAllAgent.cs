// It's possible to dump traces to a file (traces.log in the 
// example below) by adding the following to the application 
// configuration file (edgetransport.exe.config):
//
//    <configuration>
//      <system.diagnostics>
//        <trace autoflush="false" indentsize="4">
//          <listeners>
//            <add name="myListener" 
//             type="System.Diagnostics.TextWriterTraceListener" 
//             initializeData="traces.log" />
//            <remove name="Default" />
//          </listeners>
//        </trace>
//      </system.diagnostics>
//    </configuration>
//
// ***************************************************************

namespace Exchange.CatchAll
{
    using ConfigurationSettings;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Reflection;
    using System.IO;
    using System.Xml;
    using System.Globalization;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Threading;

    using Microsoft.Exchange.Data.Transport;
    using Microsoft.Exchange.Data.Transport.Smtp;
    using Microsoft.Exchange.Data.Mime;

    /// <summary>
    /// CatchAllAgent: An SmtpReceiveAgent implementing catch-all.
    /// </summary>
    public class CatchAllAgent : SmtpReceiveAgent
    {
        /// <summary>
        /// The address book to be used for lookups.
        /// </summary>
        private AddressBook addressBook;

        private static SmtpResponse rejectResponse = new SmtpResponse("550", "5.1.1", "Recipient rejected");

        private Dictionary<string, string[]> origToMapping;

        private List<DomainElement> domainList;

        /// <summary>
        /// The database connector for logging and blocked check
        /// </summary>
        private DatabaseConnector databaseConnector;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="catchAllConfig">The configuration.</param>
        /// <param name="addressBook">The address book to perform lookups.</param>
        public CatchAllAgent(AddressBook addressBook)
        {
            // Save the address book and configuration
            this.addressBook = addressBook;

            this.origToMapping = new Dictionary<string, string[]>();

            DomainSection domains = CatchAllFactory.GetCustomConfig<DomainSection>("domainSection");
            if (domains == null)
            {
                Logger.LogError("domainSection not found in configuration.");
                return;
            }

            domainList = new List<DomainElement>();

            if (domains.Domains.Count == 0)
            {
                Logger.LogWarning("No domains configured for CatchAll. I've nothing to do...");
            }
            else
            {
                string domainStr = "";
                foreach (DomainElement d in domains.Domains)
                {
                    if (!d.Regex && !RoutingAddress.IsValidAddress(d.Address))
                    {
                        Logger.LogError("Invalid address for domain: " + d.Name + ". '" + d.Address);
                        continue;
                    }
                    else if (d.Regex && !d.compileRegex())
                    {
                        Logger.LogError("Invalid regex for domain: " + d.Name + ". '" + d.Address);
                        continue;
                    }
                    domainList.Add(d);

                    if (domainStr.Length > 0)
                        domainStr += ", ";
                    domainStr += d.Name;
                }
                Logger.LogInformation("Following domains configured for CatchAll: " + domainStr, 50);
            }

            Database settings = CatchAllFactory.GetCustomConfig<Database>("customSection/database");
            if (settings == null)
            {
                Logger.LogWarning("No database settings found. Not using database connection.");
                return;
            }

            if (settings.Enabled)
            {
                switch (settings.Type.ToLower())
                {
                    case "mysql":
                        databaseConnector = new MysqlConnector(settings.ConnectionStrings);
                        break;
                    case "mssql":
                        databaseConnector = new MssqlConnector(settings.ConnectionStrings);
                        break;
                    //case "oracle":
                    //    databaseConnector = new OracleConnector(settings.ConnectionStrings);
                    //    break;
                    //case "db2":
                    //    databaseConnector = new Db2Connector(settings.ConnectionStrings);
                    //    break;
                    default:
                        Logger.LogError("Configuration: Database has invalid type setting: " + settings.Type + "\nMust be 'mysql' or 'mssql'.");
                        break;
                }
            }
            else
            {
                Logger.LogInformation("Database connection disabled in config file.", 60);
            }

            this.OnEndOfData += new EndOfDataEventHandler(this.OnEndOfDataHandler);
            this.OnRcptCommand += new RcptCommandEventHandler(this.RcptToHandler);
        }

        private void OnEndOfDataHandler(ReceiveMessageEventSource source, EndOfDataEventArgs e)
        {
            string[] addrs;
            //hash code is not guaranteed to be unique. Add time to fix uniqueness
            string itemId = e.MailItem.GetHashCode().ToString() + e.MailItem.FromAddress.ToString();
            if (this.origToMapping.TryGetValue(itemId, out addrs))
            {
                this.origToMapping.Remove(itemId);
                if (this.databaseConnector != null)
                    this.databaseConnector.LogCatch(addrs[0], addrs[1], e.MailItem.Message.Subject, e.MailItem.Message.MessageId);

                //Add / update orig to header
                if (CatchAllFactory.AppSettings.AddOrigToHeader)
                {
                    MimeDocument mdMimeDoc = e.MailItem.Message.MimeDocument;
                    HeaderList hlHeaderlist = mdMimeDoc.RootPart.Headers;
                    Header origToHeader = hlHeaderlist.FindFirst("X-OrigTo");

                    if (origToHeader == null)
                    {
                        MimeNode lhLasterHeader = hlHeaderlist.LastChild;
                        TextHeader nhNewHeader = new TextHeader("X-OrigTo", addrs[0]);
                        hlHeaderlist.InsertBefore(nhNewHeader, lhLasterHeader);
                    }
                    else
                    {
                        origToHeader.Value += ", " + addrs[0];
                    }
                }
            }
        }

        /// <summary>
        /// Handles the "RCPT TO:" SMTP command
        /// </summary>
        /// <param name="source">The event source.</param>
        /// <param name="eodArgs">The event arguments.</param>
        public void RcptToHandler(ReceiveCommandEventSource source, RcptCommandEventArgs rcptArgs)
        {
            //check if recipient exists in exchange address book
            if (this.addressBook != null && this.addressBook.Find(rcptArgs.RecipientAddress) != null)
            {
                //recipient is an existing user
                return;
            }

            string replaceWith = null;
            foreach (DomainElement d in domainList)
            {
                if (!d.Regex && d.Name.ToLower().Equals(rcptArgs.RecipientAddress.DomainPart.ToLower()))
                {
                    replaceWith = d.Address;
                    break;
                }
                else if (d.Regex)
                {
                    if (d.RegexCompiled.Match(rcptArgs.RecipientAddress.ToString().ToLower()).Success)
                    {
                        replaceWith = d.RegexCompiled.Replace(rcptArgs.RecipientAddress.ToString().ToLower(), d.Address.ToLower());

                        break;
                    }
                }
            }

            if (replaceWith != null)
            {
                RoutingAddress catchAllAddress = new RoutingAddress(replaceWith);

                if (this.databaseConnector == null || !this.databaseConnector.isBlocked(rcptArgs.RecipientAddress.ToString().ToLower()))
                {
                    Logger.LogInformation("Caught: " + rcptArgs.RecipientAddress.ToString().ToLower() + " -> " + catchAllAddress.ToString(), 100);
                    // on Exchange 2013 SP1 it seems the RcptToHandler is called multiple times for the same MailItem
                    // hash code is not guaranteed to be unique. Add time to fix uniqueness
                    string itemId = rcptArgs.MailItem.GetHashCode().ToString() + rcptArgs.MailItem.FromAddress.ToString();
                    if (!origToMapping.ContainsKey(itemId))
                    {
                        string[] addrs = new string[] { rcptArgs.RecipientAddress.ToString().ToLower(), catchAllAddress.ToString().ToLower() };
                        origToMapping.Add(itemId, addrs);
                    }
                    rcptArgs.RecipientAddress = catchAllAddress;
                }
                else
                {
                    // reject email, because address is blocked
                    Logger.LogInformation("Recipient blocked: " + rcptArgs.RecipientAddress.ToString().ToLower(), 200);

                    if (CatchAllFactory.AppSettings.RejectIfBlocked)
                        source.RejectCommand(CatchAllAgent.rejectResponse);
                }
            }

            return;
        }
    }
}
