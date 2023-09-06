using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace BasicWebApi
{
    public class Connect
    {
        public NpgsqlConnection GetConnection()
        {
            try
            {
                string host = "localhost";
                string port = "5432";
                string user = "postgres";
                string password = "per4869";
                string db = "db_cs_api";

                NpgsqlConnection concent = new NpgsqlConnection();
                concent.ConnectionString = $"Server={host};Username={user};Database={db};Port={port};Password={password}";
                concent.Open();

                return concent;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

    }
}