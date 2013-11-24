
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
    using Exchange.CatchAll.Properties;


    /// <summary>
    /// CatchAllAgent: An SmtpReceiveAgent implementing catch-all.
    /// </summary>
    public class CatchAllAgent : SmtpReceiveAgent
    {
        /// <summary>
        /// The address book to be used for lookups.
        /// </summary>
        private AddressBook addressBook;

        /// <summary>
        /// The (domain to) catchall address map
        /// </summary>
        private Dictionary<string, RoutingAddress> addressMap;


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

            DomainSection domains = (DomainSection)ConfigurationManager.GetSection("domainSection");
            if (domains == null)
            {
                Logger.LogError("domainSection not found in configuration.");
                return;
            }

            addressMap = new Dictionary<string, RoutingAddress>();

            if (domains.Domains.Count == 0)
            {
                Logger.LogWarning("No domains configured for CatchAll. I've nothing to do...");
            }
            else
            {
                string domainStr = "";
                foreach (Domain d in domains.Domains)
                {
                    if (!RoutingAddress.IsValidAddress(d.Address))
                    {
                        Logger.LogError("Invalid address for domain: " + d.Name + ". '" + d.Address);
                        continue;
                        break;
                    }
                    addressMap.Add(d.Name.ToLower(), new RoutingAddress(d.Address));

                    if (domainStr.Length > 0)
                        domainStr += ", ";
                    domainStr += d.Name;
                }
                Logger.LogInformation("Following domains configured for CatchAll: " + domainStr);
            }

            databaseConnector = new DatabaseConnector();


            // Register an OnRcptCommand event handler
            this.OnRcptCommand += new RcptCommandEventHandler(this.RcptToHandler);
            if (Settings.Default.AddOrigToHeader)
                this.OnEndOfHeaders += new EndOfHeadersEventHandler(this.EndOfHeadersHandler);
        }

        /// <summary>
        /// Handles the "RCPT TO:" SMTP command
        /// </summary>
        /// <param name="source">The event source.</param>
        /// <param name="eodArgs">The event arguments.</param>
        public void RcptToHandler(ReceiveEventSource source, RcptCommandEventArgs rcptArgs)
        {
            RoutingAddress catchAllAddress;

            // Check whether to handle the recipient's domain
            if (this.addressMap.TryGetValue(
                rcptArgs.RecipientAddress.DomainPart.ToLower(),
                out catchAllAddress))
            {
                // Rewrite the (envelope) recipient address if 'not found'
                if ((this.addressBook != null) &&
                    (this.addressBook.Find(rcptArgs.RecipientAddress) == null) && !this.databaseConnector.isBlocked(rcptArgs.RecipientAddress.ToString().ToLower()))
                {
                    Logger.LogInformation("Message Info: " + rcptArgs.MailItem.EnvelopeId + ", " + rcptArgs.MailItem.Message.MessageId);
                    this.databaseConnector.LogCatch(rcptArgs.RecipientAddress.ToString(), catchAllAddress.ToString(), rcptArgs.MailItem.EnvelopeId, rcptArgs.SmtpSession.SessionId.ToString());
                    rcptArgs.OriginalRecipient = "rfc822;" + rcptArgs.RecipientAddress.ToString();
                    MimeDocument mdMimeDoc = rcptArgs.MailItem.Message.MimeDocument;
                    HeaderList hlHeaderlist = mdMimeDoc.RootPart.Headers;
                    Header mhProcHeader = hlHeaderlist.FindFirst("X-Original-Rcpt");
                    if (mhProcHeader == null)
                    {
                        MimeNode lhLasterHeader = hlHeaderlist.LastChild;
                        TextHeader nhNewHeader = new TextHeader("X-Original-Rcpt", rcptArgs.RecipientAddress.ToString());
                        hlHeaderlist.InsertBefore(nhNewHeader, lhLasterHeader);
                    }
                    rcptArgs.RecipientAddress = catchAllAddress;
                }
            }

            return;
        }

        private void EndOfHeadersHandler(ReceiveMessageEventSource source, EndOfHeadersEventArgs e)
        {
            try
            {
                MimeNode lhLasterHeader = e.Headers.LastChild;
                Logger.LogInformation("Message: " + e.MailItem.ToString() + " | " + e.Headers.FindFirst("To").Value + " | " + e.MailItem.Message.To);
                TextHeader nhNewHeader = new TextHeader("X-OrigTo", e.Headers.FindFirst("To").Value);            
                e.Headers.InsertAfter(nhNewHeader, lhLasterHeader);       
            }
            catch (Exception except)
            {
                Logger.LogError("EndofHeader Exception = " + except.Message);
            }
        }
    }

    
}
