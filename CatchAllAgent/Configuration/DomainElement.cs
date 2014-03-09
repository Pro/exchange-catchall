using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Text.RegularExpressions;

namespace ConfigurationSettings
{
    public class DomainElement : ConfigurationElement
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

        [ConfigurationProperty("regex", DefaultValue = false, IsKey = false, IsRequired = true)]
        public bool Regex
        {
            get
            {
                return (bool)this["regex"];
            }
            set
            {
                this["regex"] = value;
            }
        }

        private Regex regexCompiled = null;

        public bool compileRegex() {
            if (!this.Regex)
                return false;
            try
            {
                regexCompiled = new Regex(this.Name);
                return true;
            }
            catch { }
            
            return false;
        }

        public Regex RegexCompiled
        {
            get
            {
                return regexCompiled;
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
