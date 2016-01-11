/*
 * Free SQL Database and Website Files backup
 * https://bytutorial.com/apps-and-software/sql-server-and-website-backup-solution
 * License under MIT
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Data.SqlClient;

namespace ServerBackup
{
    class Program
    {
        private static string _BackupDirectory = "";
        private static string _SQLServerDataSource = "";
        private static string _SQLServerUsername = "";
        private static string _SQLServerPassword = "";
        private static string _BackupDays = "";
        private static List<string> _SQLDatabaseNames = new List<string>();
        private static List<string> _SourceDirectories = new List<string>();
        private static List<string> _MessageList = new List<string>();
        static void Main(string[] args)
        {
            //get the setting values
            GetSettingValues();

            //check if the backup directory is exists
            if (!Directory.Exists(_BackupDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_BackupDirectory);
                    WriteLine(">>> Backup folder is not exists. The folder has been created successfully.");
                    _MessageList.Add(">>> Backup folder is not exists. The folder has been created successfully.");
                }
                catch (Exception ex)
                {
                    WriteLine("Error: " + ex.Message);
                    _MessageList.Add("Error Creating Backup Folder: " + ex.Message);
                }
            }
            else
            {
                //Create today date backup folder
                if (!Directory.Exists(_BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy")))
                {
                    try
                    {
                        Directory.CreateDirectory(_BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy"));
                        WriteLine(">>> Backup folder (" + DateTime.Now.ToString("dd MMM yyyy") + ") is not exists. The folder has been created successfully.");
                        _MessageList.Add(">>> Backup folder (" + DateTime.Now.ToString("dd MMM yyyy") + ") is not exists. The folder has been created successfully.");
                    }
                    catch (Exception ex)
                    {
                        WriteLine("Error: " + ex.Message);
                        _MessageList.Add("Error Creating Backup Folder (" + DateTime.Now.ToString("dd MMM yyyy") + ") : " + ex.Message);
                    }
                }
                else
                {
                    //backup the source directories
                    foreach (string sourceDir in _SourceDirectories)
                    {
                        DirectoryInfo dir = new DirectoryInfo(sourceDir);
                        _MessageList.Add("Creating Folder Zip => " + dir.Name);
                        WriteLine("Creating Folder Zip => " + dir.Name);
                        ZipOutputStream zip = new ZipOutputStream(File.Create(_BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy") + "/" + dir.Name + ".zip"));
                        
                        zip.SetLevel(9);
                        ZipFolder(sourceDir, sourceDir, zip);
                        zip.Finish();
                        zip.Close();
                    }

                    //backup database
                    foreach (string dbName in _SQLDatabaseNames)
                    {
                        try
                        {
                            WriteLine("BACKUP DATABASE [" + dbName + "] TO DISK = " + @"'" + _BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy") + "/" + dbName + ".bak'" + " WITH FORMAT, MEDIANAME = '" + dbName + "', NAME = '" + dbName + "';");
                            _MessageList.Add("BACKUP DATABASE [" + dbName + "] TO DISK = " + @"'" + _BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy") + "/" + dbName + ".bak'" + " WITH FORMAT, MEDIANAME = '" + dbName + "', NAME = '" + dbName + "';");
                            RunQuery(dbName,"BACKUP DATABASE [" + dbName + "] TO DISK = " + @"'" + _BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy") + "/" + dbName + ".bak'" + " WITH FORMAT, MEDIANAME = '" + dbName + "', NAME = '" + dbName + "';");
                            WriteLine(@">>> Backup file for " + dbName + " has been succesfully created");
                            _MessageList.Add(@">>> Backup database file for " + dbName + " has been succesfully created");

                            ZipOutputStream zipDB = new ZipOutputStream(File.Create(_BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy") + "/DATABASE_" + dbName + ".zip"));
                            _MessageList.Add("Creating Database Zip => " + dbName);
                            zipDB.SetLevel(9);
                            string relativePath = (_BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy")).Substring((_BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy")).Length) + "/";
                            AddFileToZip(zipDB, relativePath, _BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy") + "/" + dbName + ".bak");
                            zipDB.Finish();
                            zipDB.Close();
                        }
                        catch (Exception ex)
                        {
                            WriteLine("Error backing up database (" + dbName + ") : " + ex.Message.ToString());
                            _MessageList.Add("Error backing up databasee (" + dbName + ") :" + ex.Message);
                        }
                    }

                    //Delete the bak file
                    foreach (string dbName in _SQLDatabaseNames)
                    {
                        try
                        {
                            File.Delete(_BackupDirectory + "/" + DateTime.Now.ToString("dd MMM yyyy") + "/" + dbName + ".bak");
                        }
                        catch (Exception ex)
                        {
                            WriteLine("Error deleting database (" + dbName + ").bak file : " + ex.Message.ToString());
                            _MessageList.Add("Error deleting database (" + dbName + ").bak file : " + ex.Message.ToString());
                        }
                    }

                    //check if need to delete backup folder
                    if (_BackupDays != string.Empty)
                    {
                        int days = 0;
                        int.TryParse(_BackupDays, out days);
                        if (days > 0)
                        {
                            DirectoryInfo curDir = new DirectoryInfo(_BackupDirectory);
                            DirectoryInfo[] dirList = curDir.GetDirectories();
                            foreach (DirectoryInfo objDir in dirList)
                            {
                                if ((DateTime.Now - Convert.ToDateTime(objDir.Name)).TotalDays > days)
                                {
                                    DeleteDirectory(objDir);
                                }
                            }
                        }
                    }
                }
            }
        }

        //Delete directory and files inside (recursive)
        static void DeleteDirectory(DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
            foreach (System.IO.DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
            directory.Delete(true);
        }

        public static void ZipFolder(string RootFolder, string CurrentFolder, ZipOutputStream zStream)
        {
            string[] SubFolders = Directory.GetDirectories(CurrentFolder);

            //calls the method recursively for each subfolder
            foreach (string Folder in SubFolders)
            {
                WriteLine("Adding folder: " + Folder);
                _MessageList.Add("Adding folder: " + Folder);
                ZipFolder(RootFolder, Folder, zStream);
            }

            string relativePath = CurrentFolder.Substring(RootFolder.Length) + "/";

            //the path "/" is not added or a folder will be created
            //at the root of the file
            if (relativePath.Length > 1)
            {
                ZipEntry dirEntry;
                dirEntry = new ZipEntry(relativePath);
                dirEntry.DateTime = DateTime.Now;
            }

            //adds all the files in the folder to the zip
            foreach (string file in Directory.GetFiles(CurrentFolder))
            {
                WriteLine("Adding file " + file);
                _MessageList.Add("Adding file " + file);
                AddFileToZip(zStream, relativePath, file);
            }
        }

        private static void AddFileToZip(ZipOutputStream zStream, string relativePath, string file)
        {
            byte[] buffer = new byte[4096];

            //the relative path is added to the file in order to place the file within
            //this directory in the zip
            string fileRelativePath = (relativePath.Length > 1 ? relativePath : string.Empty)
                                      + Path.GetFileName(file);

            ZipEntry entry = new ZipEntry(fileRelativePath);
            entry.DateTime = DateTime.Now;
            zStream.PutNextEntry(entry);

            using (FileStream fs = File.OpenRead(file))
            {
                int sourceBytes;
                do
                {
                    sourceBytes = fs.Read(buffer, 0, buffer.Length);
                    zStream.Write(buffer, 0, sourceBytes);
                } while (sourceBytes > 0);
            }
        }

        static void RunQuery(string databaseName, string sql)
        {
            WriteLine(@"Data Source=" + _SQLServerDataSource + ";Initial Catalog=" + databaseName + ";User ID=" + _SQLServerUsername + ";Password=" + _SQLServerPassword);
            using (SqlConnection objConnection = new SqlConnection(@"Data Source=" + _SQLServerDataSource + ";Initial Catalog=" + databaseName + ";User ID=" + _SQLServerUsername + ";Password=" + _SQLServerPassword))
            {
                if (objConnection.State == System.Data.ConnectionState.Closed)
                {
                    objConnection.Open();
                }
                SqlCommand objCommand = new SqlCommand(sql, objConnection);
                objCommand.CommandTimeout = 100000;
                objCommand.ExecuteNonQuery();
                if (objConnection.State == System.Data.ConnectionState.Open)
                {
                    objConnection.Close();
                }
            }
        }

        static void GetSettingValues()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(AppDomain.CurrentDomain.BaseDirectory + "/setting.config");
                XmlNode nodeParent = doc.SelectSingleNode("/configuration");
                if (nodeParent != null)
                {
                    XmlNodeList childNodes = nodeParent.ChildNodes;
                    foreach (XmlNode node in childNodes)
                    {
                        switch (node.Name)
                        {
                            case "BackupDirectory":
                                _BackupDirectory = node.InnerText;
                                break;
                            case "SQLServerDataSource":
                                _SQLServerDataSource = node.InnerText;
                                break;
                            case "SQLServerUsername":
                                _SQLServerUsername = node.InnerText;
                                break;
                            case "SQLServerPassword":
                                _SQLServerPassword = node.InnerText;
                                break;
                            case "BackupDays":
                                _BackupDays = node.InnerText;
                                break;
                            case "SQLDatabaseNames":
                                XmlNodeList dbNodes = node.ChildNodes;
                                foreach (XmlNode dbNode in dbNodes)
                                {
                                    _SQLDatabaseNames.Add(dbNode.InnerText);
                                }
                                break;
                            case "SourceDirectories":
                                XmlNodeList dirNodes = node.ChildNodes;
                                foreach (XmlNode dirNode in dirNodes)
                                {
                                    _SourceDirectories.Add(dirNode.InnerText);
                                }
                                break;
                        }
                    }
                }

                _MessageList.Add(">>> Configuration settings have been read successfully." + Environment.NewLine);
                WriteLine(">>> Configuration settings have been read successfully.");
                WriteLine(">>> There are " + _SourceDirectories.Count.ToString() + " folder(s) and " + _SQLDatabaseNames.Count.ToString() + " database(s) to backup.");
            }
            catch (Exception ex)
            {
                _MessageList.Add(">>> Error reading configuration: " + ex.Message.ToString() + Environment.NewLine);
                WriteLine(">>> Error reading configuration: " + ex.Message.ToString());
            }
        }

        static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        static void ReadLine(string message)
        {
            WriteLine(message);
            Console.ReadLine();
        }
    }
}
