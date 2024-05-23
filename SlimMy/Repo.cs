using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Oracle.ManagedDataAccess.Client;

namespace SlimMy
{
    class Repo
    {
        private readonly string _connString;

        public Repo(string connString)
        {
            _connString = connString;
        }

        public void InsertPerson(string name)
        {
            using (OracleConnection connection = new OracleConnection(_connString))
            {
                try
                {
                    connection.Open();
                    string sql = "INSERT INTO test (name) VALUES (:name)";
                    using (OracleCommand sqlCommand = new OracleCommand(sql, connection))
                    {
                        sqlCommand.Parameters.Add(new OracleParameter("name", name));
                        sqlCommand.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error : " + ex.Message);
                }
            }
        }
    }
}
