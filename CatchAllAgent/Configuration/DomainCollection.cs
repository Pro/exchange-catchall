using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace ConfigurationSettings
{
    [ConfigurationCollection(typeof(Domain))]
    public class DomainCollection : ConfigurationElementCollection
    {
        internal const string PropertyName = "Domain";

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.BasicMapAlternate;
            }
        }
        protected override string ElementName
        {
            get
            {
                return PropertyName;
            }
        }

        protected override bool IsElementName(string elementName)
        {
            return elementName.Equals(PropertyName, StringComparison.InvariantCultureIgnoreCase);
        }


        public override bool IsReadOnly()
        {
            return false;
        }


        protected override ConfigurationElement CreateNewElement()
        {
            return new Domain();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Domain)(element)).Name;
        }

        public Domain this[int idx]
        {
            get
            {
                return (Domain)BaseGet(idx);
            }
        }
    }
}
