
// 2) Add entries to the config.xml for the domains for which
//    you want catch all to be working. Entries look like this:
//    <config>
//	    <domain name="domain1.com" address="catchall@domain1.com" />
// 	    <domain name="domain2.com" address="admin@domain2.com" />
//    </config>
//    Note: The messages will be redirected to the specified 
//    'address' and it's important that those addresses really 
//    exist. Otherwise the message will NDR, or Recipient 
//    Filtering will reject the message.
//
// 3) Use the install-transportagent task to install the agent
//
// 4) Use the enable-transportagent task to enable the agent. 
//    It's important to specify a priority which is higher than
//    the priority of the Recipient Filtering agent.
//
// Features:
//
// 1. Changes to the configuration will be picked up 
//    automatically.
//
// 2. If the new configuration is invalid, it will ignore the new
//    configuration.
//
// 3. It's possible to dump traces to a file (traces.log in the 
//    example below) by adding the following to the application 
//    configuration file (edgetransport.exe.config):
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
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.IO;
    using System.Xml;
    using System.Globalization;
    using System.Diagnostics;
    using System.Threading;

    using Microsoft.Exchange.Data.Transport;
    using Microsoft.Exchange.Data.Transport.Smtp;

    using MySql.Data.MySqlClient;
    using Microsoft.Exchange.Data.Mime;
    using ConfigurationSettings;
    using System.Configuration;
    using System.Text.RegularExpressions;


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

         private Dictionary<int, string[]> origToMapping;

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

            this.origToMapping = new Dictionary<int, string[]>();

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
                Logger.LogInformation("Following domains configured for CatchAll: " + domainStr);
            }

            databaseConnector = new DatabaseConnector();

            
            this.OnEndOfData += new EndOfDataEventHandler(this.OnEndOfDataHandler);

            this.OnRcptCommand += new RcptCommandEventHandler(this.RcptToHandler);
        }

        private void OnEndOfDataHandler(
            ReceiveMessageEventSource source,
            EndOfDataEventArgs e)
        {
            string[] addrs;
            if (this.origToMapping.TryGetValue(e.MailItem.GetHashCode(), out addrs))
            {
                this.origToMapping.Remove(e.MailItem.GetHashCode());
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
                    if (d.RegexCompiled.Match(rcptArgs.RecipientAddress.ToString().ToLower()).Success){
                        replaceWith = d.RegexCompiled.Replace(rcptArgs.RecipientAddress.ToString().ToLower(), d.Address.ToLower());

                        break;
                    }
                }
            }

            if (replaceWith != null)
            {
                RoutingAddress catchAllAddress = new RoutingAddress(replaceWith);

                if (!this.databaseConnector.isBlocked(rcptArgs.RecipientAddress.ToString().ToLower()))
                {
                    Logger.LogInformation("Cought: " + rcptArgs.RecipientAddress.ToString().ToLower() + " -> " + catchAllAddress.ToString());
                    string[] addrs = new string[] { rcptArgs.RecipientAddress.ToString().ToLower(), catchAllAddress.ToString().ToLower() };
                    origToMapping.Add(rcptArgs.MailItem.GetHashCode(), addrs);
                    rcptArgs.RecipientAddress = catchAllAddress;
                }
                else
                {
                    // reject email, because address is blocked

                    Logger.LogInformation("Recipient blocked: " + rcptArgs.RecipientAddress.ToString().ToLower());

                    if (CatchAllFactory.AppSettings.RejectIfBlocked)
                        source.RejectCommand(CatchAllAgent.rejectResponse);              
                       
                } 
            }

            return;
        }
    }
    
    
}
