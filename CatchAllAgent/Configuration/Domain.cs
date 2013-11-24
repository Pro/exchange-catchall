using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace ConfigurationSettings
{
    public class Domain : ConfigurationElement
    {
        [ConfigurationProperty("name", DefaultValue = "", IsKey = true, IsRequired = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }

        [ConfigurationProperty("address", DefaultValue = "", IsKey = false, IsRequired = true)]
        public string Address
        {
            get
            {
                return (string)this["address"];
            }
            set
            {
                this["address"] = value;
            }
        }

    }
}
