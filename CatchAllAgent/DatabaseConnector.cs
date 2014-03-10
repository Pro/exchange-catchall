using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Exchange.CatchAll
{
    abstract class DatabaseConnector
    {
        protected DbConnection sqlConnection = null;

        abstract public void LogCatch(string original, string replaced, string subject, string message_id);

        abstract public bool isBlocked(string address);
    }
}