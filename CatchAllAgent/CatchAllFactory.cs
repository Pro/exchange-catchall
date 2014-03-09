using Microsoft.Exchange.Data.Transport;
using Microsoft.Exchange.Data.Transport.Smtp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
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
            AppSettings = GetCustomConfig<ConfigurationSettings.General>("customSection/general");
            return new CatchAllAgent(server.AddressBook);
        }

        private static Assembly configurationDefiningAssembly;

        public static ConfigurationSettings.General AppSettings;

        public static TConfig GetCustomConfig<TConfig>(string sectionName) where TConfig : ConfigurationSection
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ConfigResolveEventHandler);
            configurationDefiningAssembly = Assembly.LoadFrom(Assembly.GetExecutingAssembly().Location);
            var exeFileMap = new ExeConfigurationFileMap();
            exeFileMap.ExeConfigFilename = Assembly.GetExecutingAssembly().Location+".config";
            var customConfig = ConfigurationManager.OpenMappedExeConfiguration(exeFileMap, ConfigurationUserLevel.None);
            var returnConfig = customConfig.GetSection(sectionName) as TConfig;
            AppDomain.CurrentDomain.AssemblyResolve -= ConfigResolveEventHandler;
            return returnConfig;
        }

        protected static Assembly ConfigResolveEventHandler(object sender, ResolveEventArgs args)
        {
            return configurationDefiningAssembly;
        }
    }
}