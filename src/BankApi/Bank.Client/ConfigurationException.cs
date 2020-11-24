using System;

namespace Bank.Client
{
    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}