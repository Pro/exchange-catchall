using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace ConfigurationSettings
{
    public class General : ConfigurationSection
    {
        [ConfigurationProperty("LogLevel", DefaultValue = 2, IsKey = true, IsRequired = true)]
        [IntegerValidator(ExcludeRange = false, MaxValue = 3, MinValue = 0)]
        public int LogLevel
        {
            get
            {
                return (int)this["LogLevel"];
            }
            set
            {
                this["LogLevel"] = value;
            }
        }

        [ConfigurationProperty("AddOrigToHeader", DefaultValue = true, IsKey = false, IsRequired = true)]
        public bool AddOrigToHeader
        {
            get
            {
                return (bool)this["AddOrigToHeader"];
            }
            set
            {
                this["AddOrigToHeader"] = value;
            }
        }

    }
}
