using Microsoft.Data.Sqlite;

namespace ProGlassAutomation.Data.Database
{
    public static class DbFactory
    {
        private static readonly string connStr = "Data Source=glass.db";

        public static SqliteConnection CreateConnection()
        {
            var conn = new SqliteConnection(connStr);
            conn.Open();
            return conn;
        }
    }
}