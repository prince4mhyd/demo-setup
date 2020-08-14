using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CytelDBDeployment
{
    public class DBDeployment
    {
        private readonly ILogger appLogger;
        public DBDeployment(ILogger<DBDeployment> logger)
        {
           this.appLogger = logger;
        }

        internal void Run()
        {
            appLogger.LogInformation("Application {applicationevent} at {dateTime}", "Started", DateTime.UtcNow);
            bool isDbExists = false;
            Console.WriteLine("Please enter the solaris folder path");
            string folderPath = Console.ReadLine();
            Console.WriteLine("Please enter  database name");
            string dbname = Console.ReadLine();
            if (CytelDBManager.chkDBExists(dbname, appLogger))
            {
                Console.WriteLine(String.Format("{0} Already exists", dbname));
                isDbExists = true;
            }
            else
            {
                isDbExists = CytelDBManager.CreateDatabase(dbname, appLogger);
                if (isDbExists)
                {
                    Console.WriteLine(String.Format("{0} Database Created", dbname));
                }

            }
            if (isDbExists)
            {
                if (CytelDBManager.CreateDBObjects(dbname, folderPath, appLogger))
                {
                    Console.WriteLine("All database objects created.......");
                }
                else
                {
                    Console.WriteLine("There is some issue with database object creation.....");
                }

            }
            else
                Console.WriteLine("There is some issue with database creation");

        }
    }
}
