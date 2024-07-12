using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

namespace I3S_API.Lib
{
    public class AppDb : IDisposable
    {
        public SqlConnection Connection;
        public AppDb(string sqlmode = "Default")
        {
            string ConnectionStrings = AppConfig.Config[$"ConnectionStrings:{sqlmode}"];
            if (ConnectionStrings.IsNullOrEmpty()) {
                ConnectionStrings = AppConfig.Config[$"ConnectionStrings:Default"];
            }
            Connection = new SqlConnection(ConnectionStrings);
        }
        public void Dispose()
        {

            Connection.Close();
            Connection.Dispose();
        }
    }
}
