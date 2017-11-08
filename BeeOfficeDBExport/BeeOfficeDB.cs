using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BeeOfficeDBExport
{
    class BeeOfficeDB
    {
        
        public static void ExportSelectedTables (string connectionString, string tableFilter, string logSys, bool includeDeleted, string outFolder)
        {


            SqlConnection sqlConnection = new SqlConnection(connectionString);
            try {
                
                sqlConnection.Open();
                }
            catch (Exception ex)
                {
                    MessageBox.Show("Can't connect to a database! Please check Connection String or credentials. Details: " + ex.Message);
                    return;
                }

            SqlCommand cmd = new SqlCommand();

            // read all tables' names that are tables not views and that have Logical System number
            cmd.CommandText = @"SELECT TABLE_NAME, COUNT(TABLE_NAME) FROM [INFORMATION_SCHEMA].[COLUMNS] WHERE (COLUMN_NAME = 'logSys' OR COLUMN_NAME = 'is_deleted') AND "
               + "TABLE_NAME IN (SELECT TABLE_NAME FROM [INFORMATION_SCHEMA].[TABLES] WHERE table_type = 'BASE TABLE')";
           

            // preparing filter to proper wildcard and ignore case comparison
            if (tableFilter!=null && tableFilter!="")
                { 
                    tableFilter = tableFilter.ToUpper();
                    tableFilter = tableFilter.Replace('*', '%');
                    tableFilter = tableFilter.Replace('?', '_');
                cmd.CommandText = cmd.CommandText + " AND UPPER(TABLE_NAME) LIKE '" + tableFilter + "'";            
                }

            cmd.CommandText = cmd.CommandText + " GROUP BY TABLE_NAME HAVING COUNT(TABLE_NAME)=2 ORDER BY TABLE_NAME";
            cmd.CommandType = CommandType.Text; 
            cmd.Connection = sqlConnection;

            // read all the relevant table names and put them into list
            SqlDataReader reader = cmd.ExecuteReader();
            List<string> tables = new List<string>();

            // 
            string fileNameTables = "_table_list_" + logSys + "_";
            string timeStamp= DateTime.Now.ToString("yyyyMMdd_HHmmss");
            fileNameTables = fileNameTables + timeStamp;

            try
            {
                using (StreamWriter writer = new StreamWriter(outFolder + "\\" + fileNameTables + ".txt"))
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                        writer.WriteLine(reader.GetString(0));
                    }
                }
            }
            catch (Exception ex)
            {
                // throw error if exceptional else report to user or treat it   
                MessageBox.Show("Cannot create/save file! Please check folder name or authorizations. Export cancelled. Details: " + ex.Message);
                return;
            }



            sqlConnection.Close();

            int tablesCount = tables.Count;
            // process tables one by one      
            foreach (var item in tables.Select((value, index) => new { Value = value, Index = index }))
            {
                // get table name and its index from the list
                string currentValue = item.Value;
                int currentIndex = item.Index + 1;
                // if exporting table returns error -- cancel the rest
                if (ExportSingleTable (connectionString,currentValue,logSys,includeDeleted,outFolder,timeStamp,currentIndex,tablesCount) != 0)
                {
                    return;
                }
                
            }

            ((MainWindow)System.Windows.Application.Current.MainWindow).UpdateProgress("", "Export finished successfully!", 100);

        }

        private static int ExportSingleTable(string connectionString, string tableName, string logSys, bool includeDeleted, string outFolder, string timeStamp, int tableIndex, int tablesCount)
        {

            SqlConnection sqlConnection = new SqlConnection(connectionString);
            SqlDataReader reader;
            try
            {

                sqlConnection.Open();
                SqlCommand cmd = new SqlCommand();

                // read all records from passed table with given logSys in XML RAW format
                cmd.CommandText = @"SELECT * FROM " + tableName + " WHERE logSys = " + logSys;
                if (includeDeleted == false)
                {
                    cmd.CommandText = cmd.CommandText + " AND (is_deleted IS NULL OR is_deleted=0)";
                }

                cmd.CommandText = cmd.CommandText + " FOR XML RAW, BINARY BASE64";

                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection;

                reader = cmd.ExecuteReader();

            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't connect to a database! Please check Connection String or credentials. Details: " + ex.Message);
                return -1;
            }


            // save external file only if there is any data
            if (reader.HasRows)
            {
                // preparing file name for particular table and export
                string fileNameTable = tableName + "_" + logSys + "_" + timeStamp;
                double progressPercentage = (((double)tableIndex / tablesCount) * 100);
                try
                {
                    using (StreamWriter writer = new StreamWriter(outFolder + "\\" + fileNameTable + ".xml"))
                    {
                        while (reader.Read())
                        {
                            writer.WriteLine(reader.GetString(0));
                            ((MainWindow)System.Windows.Application.Current.MainWindow).UpdateProgress("Table " + tableIndex + " of " + tablesCount, "Exporting: [" + tableName + "]", (int)Math.Round(progressPercentage));
                        }

                    }

                }

                catch (Exception ex)
                {
                    // throw error if exceptional else report to user or treat it   
                    MessageBox.Show("Cannot create/save file! Please check folder name or authorizations. Export cancelled. Details: " + ex.Message);
                    return -2;
                }
            }
            sqlConnection.Close();
            return 0;


        }


    }
}
