using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Serilog.Sinks.InfluxDB.Sinks.InfluxDB
{
    public class InfluxDbClientWriteException : Exception
    {
        public InfluxDbClientWriteException()
        {
        }

        public InfluxDbClientWriteException(string message) : base(message)
        {
        }

        public InfluxDbClientWriteException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected InfluxDbClientWriteException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
