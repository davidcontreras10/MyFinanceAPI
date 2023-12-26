using System.Configuration;

namespace MyFinanceBackend.Services
{
    public static class ConnectionStrings
    {
        public static string ReadGenericConnectionString(string connectionName)
        {
            var connectionStrings = ConfigurationManager.ConnectionStrings;
            var connectionString = connectionStrings[connectionName].ConnectionString;
            return connectionString;
        }

        #region Constants

        public const string DEFAULT_STRING_CONNECTION = "SqlServerLocalConnection";

        #endregion
    }
}
