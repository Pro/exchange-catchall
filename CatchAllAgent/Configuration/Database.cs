using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace ConfigurationSettings
{
    public class Database : ConfigurationSection
    {
        [ConfigurationProperty("enabled", DefaultValue = false, IsKey = false, IsRequired = true)]
        public bool Enabled
        {
            get
            {
                return (bool)this["enabled"];
            }
            set
            {
                this["enabled"] = value;
            }
        }

        [ConfigurationProperty("type", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string Type
        {
            get
            {
                return (string)this["type"];
            }
            set
            {
                this["type"] = value;
            }
        }

        [ConfigurationProperty("connectionstrings", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string ConnectionStrings
        {
            get
            {
                return (string)this["connectionstrings"];
            }
            set
            {
                this["connectionstrings"] = value;
            }
        }
    }
}