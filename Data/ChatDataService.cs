using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace BlazorStack.Data
{
    public class ChatDataService
    {
        private string connectionString = "Data Source = DESKTOP-UKI1896\\SQLEXPRESS; Integrated Security = True; Connect Timeout = 30; Encrypt = False; Trust Server Certificate = False; Application Intent = ReadWrite; Multi Subnet Failover = False";
        private StringBuilder ChatLog = new StringBuilder();
        object _myLockToken = new object();
        private string lastLine = "";
        public Task<string> GetChatAsync()
        {
            ReadData();
            return Task.FromResult(ChatLog.ToString());
        }
        public Task<string> GetLastLine()
        {
            return Task.FromResult(lastLine);
        }
        public void SendMessage(string? message)
        {
            if (message != null && message != "")
                InsertData(message);
        }
        private void ReadData()
        {
            SqlConnection cnn = new SqlConnection(connectionString);
            cnn.Open();
            SqlCommand command;
            string sql = "USE [julibank] SELECT * FROM Chat";
            command = new SqlCommand(sql, cnn);
            SqlDataReader reader = command.ExecuteReader();
            lock (_myLockToken)
            {

                while (reader.Read())
                {
                    if (!ChatLog.ToString().Contains(reader.GetString(1) + "   " + reader.GetString(2) + "\n"))
                    {
                        lastLine = reader.GetString(1) + "   " + reader.GetString(2) + "\n";
                        ChatLog.AppendLine(reader.GetString(1) + "   " + reader.GetString(2) + "\n");
                    }
                }
                if (lastLine != "")
                    ChatLog.Replace(lastLine + "\r\n", "");
            }
            cnn.Close();
        }
        private void InsertData(string message)
        {
            SqlConnection cnn = new SqlConnection(connectionString);
            cnn.Open();
            SqlCommand command;
            string sql = $"USE [julibank] INSERT INTO Chat ([chatMessage],[date]) VAlUES (N'{message}',N'{DateTime.Now}')";
            command = new SqlCommand(sql, cnn);
            SqlDataReader reader = command.ExecuteReader();
            cnn.Close();
        }
    }
}
