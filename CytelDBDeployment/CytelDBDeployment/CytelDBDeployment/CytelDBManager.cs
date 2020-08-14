using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CytelDBDeployment
{
    public static class CytelDBManager
    {
        

        public static string username= CytelStaging.username;
        public static string servername= CytelStaging.servername;
        public static string hostname= CytelStaging.hostname;
        public static string password= CytelStaging.password;

       

        static int portno = 5432;
        static string connectionString = String.Format("User ID={0}@{1};Password='{2}';Host={3};Port={4};Database={5};SslMode={6}", username,servername,password, hostname,portno, "postgres", "Require");
        static string dbMainConnectionString = String.Format("User ID={0}@{1};Password='{2}';Host={3};Port={4};", username, servername, password, hostname, portno);
        static string dbConnectionString = dbMainConnectionString + "Database={0};SslMode=Require";

        
        static string dbPath = @"solaris\services\database\postgresql\";
        


        static string ownerName = "SolarisAdmin";
        static string encoding = "UTF8";
        static string lccollate = "English_United States.1252";
        static string tableSpace = "pg_default";
        static int connectionLimit = -1;

        public static bool chkDBExists(string dbname, ILogger logger)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(connectionString))
            {
                using (NpgsqlCommand command = new NpgsqlCommand
                    ($"SELECT DATNAME FROM pg_catalog.pg_database WHERE DATNAME = '{dbname}'", conn))
                {
                    try
                    {
                        conn.Open();
                        var i = command.ExecuteScalar();
                        conn.Close();
                        if (i.ToString().Equals(dbname)) //always 'true' (if it exists) or 'null' (if it doesn't)
                            return true;
                        else return false;
                    }
                    catch (Exception e) {
                        logger.LogError("Error While Checking Database {0}", e.Message.ToString());
                        return false; }
                }
            }
        }

        public static bool CreateDatabase(string dbname, ILogger logger)
        {
            bool isDbCreated;
            // creating a database in Postgresql
            string createDbCommand = String.Format("CREATE DATABASE \"{0}\" WITH OWNER = \"{1}\" ENCODING = '{2}' LC_COLLATE = '{3}' LC_CTYPE = '{3}' CONNECTION LIMIT = {4}",
                  dbname,
                  ownerName,
                  encoding,
                  lccollate,
                  connectionLimit
                );


            try
            {
                NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                NpgsqlCommand cmd = new NpgsqlCommand(createDbCommand, connection);

                connection.Open();
                cmd.ExecuteNonQuery();
                connection.Close();
                isDbCreated = true;
            }
            catch (Exception ex)
            {
                logger.LogError("Error While Creating Database {0}", ex.Message.ToString());
                isDbCreated = false;
            }
            return isDbCreated;
        }

        public static bool CreateSchemas(string dbName, string schemaPath, ILogger logger)
        {
            bool isSchemaCreated = false;
            try
            {
                isSchemaCreated = ExecuteScript(schemaPath, dbName, logger);
            }
            catch(Exception ex)
            {
                logger.LogError("Error While Creating Database {0}", ex.Message.ToString());
                isSchemaCreated = false;
            }
            return isSchemaCreated;
        }


        public static bool ExecuteScript(string filename, string dbName, ILogger logger)
        {
           bool isScriptExecuted = false;
            try
            {
                FileInfo file = new FileInfo(filename);
                string script = File.ReadAllText(filename);
                string newScript= script.Replace(@"\t","  ").Replace("\\n"," ").Replace("\\r"," ");
                NpgsqlConnection connection = new NpgsqlConnection(String.Format(dbConnectionString, dbName));
                NpgsqlCommand cmd = new NpgsqlCommand(newScript, connection);

                connection.Open();
                cmd.ExecuteNonQuery();
                connection.Close();
                logger.LogInformation("Completed Execution {0}", filename);
                isScriptExecuted = true;
            }
            catch (Exception ex)
            {
                logger.LogError("Error While Executing script {0} {1}", filename, ex.Message.ToString());
                isScriptExecuted = false;
            }

            return isScriptExecuted;

        }


        public static bool ReadAndExceuteFileContent(string path,string dbName, ILogger logger)
        {
           string [] files = System.IO.Directory.GetFiles(path, "*.sql");
            try
            {
                NpgsqlConnection connection = new NpgsqlConnection(String.Format(dbConnectionString, dbName));
                connection.Open();
                NpgsqlCommand cmd = null;
                foreach (var tableScript in files)
                {
                    try
                    {
                        FileInfo file = new FileInfo(tableScript);
                        string script = File.ReadAllText(tableScript);
                        string newScript = script.Replace(@"\t", "  ").Replace("\\n", " ").Replace("\\r", " ");
                        cmd = new NpgsqlCommand(newScript, connection);
                        cmd.ExecuteNonQuery();
                        logger.LogInformation("Completed Execution {0}", tableScript);
                    }
                    catch(Exception ex)
                    {
                        logger.LogInformation("Error while executing file {0} {1}", tableScript, ex.Message);
                    }
                }

                connection.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        public static bool CreateDBObjects(string dbName,string folderPath, ILogger logger)
        {
            bool isAllDbObjectsCreated = false;
             string projectSchemaPath = String.Format("{0}{1}{2}", folderPath, dbPath, @"schema\project");
             string resultSchemaPath = String.Format("{0}{1}{2}", folderPath, dbPath, @"schema\result");
             string simulationSchemaPath = String.Format("{0}{1}{2}", folderPath, dbPath, @"schema\simulation");
            try
            {
                CreateProectSchemaAndDBObjects(projectSchemaPath, dbName, folderPath,logger);
                CreateSimulationSchemaAndDBObjects(simulationSchemaPath, dbName, logger);
                CreateResultSchemaandDBObjects(resultSchemaPath,dbName, logger);

            }
            catch (Exception ex)
            {
                isAllDbObjectsCreated = false;

            }
            return isAllDbObjectsCreated;
        }

        public static bool CreateProectSchemaAndDBObjects(string schemaPath, string dbName, string folderPath, ILogger logger)
        {
            bool isAllDbObjectsCreated = false;
            string projectSchemaFile = String.Format(@"{0}\Project.schema.sql", schemaPath);
            if (CreateSchemas(dbName, projectSchemaFile, logger))
            {
                isAllDbObjectsCreated = CreateSchemaDBObjects(schemaPath, dbName, logger);

                if (isAllDbObjectsCreated)
                {
                    isAllDbObjectsCreated = ReadAndExceuteFileContent(String.Format(@"{0}{1}\static data scripts", folderPath, dbPath), dbName, logger);
                }
            }

            return isAllDbObjectsCreated;
        }

        public static bool CreateResultSchemaandDBObjects(string schemaPath, string dbName, ILogger logger)
        {
            bool isAllDbObjectsCreated = false;
            string resultSchemaFile = String.Format(@"{0}\Result.schema.sql", schemaPath);
            if (CreateSchemas(dbName, resultSchemaFile, logger))
            {
                isAllDbObjectsCreated = CreateSchemaDBObjects(schemaPath, dbName, logger);

            }

            return isAllDbObjectsCreated;
        }

        public static bool CreateSimulationSchemaAndDBObjects(string schemaPath, string dbName, ILogger logger)
        {
            bool isAllDbObjectsCreated = false;
            string simulationSchemaFile = String.Format(@"{0}\Simulation.schema.sql", schemaPath);
            if (CreateSchemas(dbName, simulationSchemaFile, logger))
            {
                isAllDbObjectsCreated = CreateSchemaDBObjects(schemaPath, dbName, logger);
            }

            return isAllDbObjectsCreated;
        }
        public static bool CreateSchemaDBObjects(string schemaPath, string dbName, ILogger logger)
        {
            bool isAllDbObjectsCreated = false;
            try
            {
                isAllDbObjectsCreated = ReadAndExceuteFileContent(String.Format(@"{0}\tables", schemaPath), dbName, logger);
                if (isAllDbObjectsCreated)
                {
                    isAllDbObjectsCreated = ReadAndExceuteFileContent(String.Format(@"{0}\functions", schemaPath), dbName, logger);
                    if (isAllDbObjectsCreated)
                    {
                        isAllDbObjectsCreated = ReadAndExceuteFileContent(String.Format(@"{0}\stored procedures", schemaPath), dbName, logger);
                       
                    }
                }
            }
            catch (Exception ex)
            {
                isAllDbObjectsCreated = false;

            }

            return isAllDbObjectsCreated;
        }

    }

    public static class CytelDev
    {
       public static string username = "SolarisAdmin";
       public static string servername = "psql-11-dev-cyt-solaris-eastus";
       public static string hostname = "psql-11-dev-cyt-solaris-eastus.postgres.database.azure.com";
       public static string password = "s8l7_i#=P)3#";
    }
    public static class CytelQA
    {
        public static string username = "SolarisAdmin";
        public static string servername = "psql-qa-cyt-solaris-eastus";
        public static string hostname = "psql-qa-cyt-solaris-eastus.postgres.database.azure.com";
        public static string password = "pds4Aha$";
    }

    public static class CytelStaging
    {
        public static string username = "SolarisAdmin";
        public static string servername = "psql-stg-cyt-solara-eastus";
        public static string hostname = "psql-stg-cyt-solara-eastus.postgres.database.azure.com";
        public static string password = "iPY&YLR@O#42";
    }
}
