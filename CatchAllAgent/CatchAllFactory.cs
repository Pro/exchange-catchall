using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Exchange.CatchAll
{
    /// <summary>
    /// CatchAllFactory: The agent factory for the CatchAllAgent.
    /// </summary>
    public class CatchAllFactory : SmtpReceiveAgentFactory
    {

        /// <summary>
        /// Creates a CatchAllAgent instance.
        /// </summary>
        /// <param name="server">The SMTP server.</param>
        /// <returns>An agent instance.</returns>
        public override SmtpReceiveAgent CreateAgent(SmtpServer server)
        {
            return new CatchAllAgent(server.AddressBook);
        }
    }

}
