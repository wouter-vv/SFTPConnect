using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFTPConnect
{
    class Connection
    {
        static void Main(string[] args)
        {
            Connection c = new Connection();
            c.Connect();
        }

        int attemptCounter = 0;
        int maxRetries = 5;
        string servername = ConfigurationManager.AppSettings["host"];
        int serverport = Int16.Parse(ConfigurationManager.AppSettings["port"]);

        /// <summary>
        /// Connects to the client SFTP server, calls the method to get the files
        /// </summary>
        /// <returns>boolean if connection succeeded</returns>
        public void Connect()
        {
            Boolean Connected = false;
            string error = "";

            while (!Connected && attemptCounter < maxRetries)
            {
                try
                {
                    var connectionInfo = new ConnectionInfo(servername, serverport, ConfigurationManager.AppSettings["username"],
                        new PasswordAuthenticationMethod(
                            ConfigurationManager.AppSettings["username"],
                            ConfigurationManager.AppSettings["password"]
                        ));

                    using (var client = new SftpClient(connectionInfo))
                    {
                        client.Connect();
                        if (client.IsConnected)
                        {
                            GetFile(client);
                            Connected = true;
                            client.Disconnect();
                        }

                    }
                    attemptCounter = 0;
                }
                // catch and write authentication errors (this error occurs even if the password is correct)
                catch (SshAuthenticationException e)
                {
                    attemptCounter++;
                    error += "Renci.SshNet.Common.SshAuthenticationException: Permission denied (password).\n";
                    if (attemptCounter >= maxRetries)
                    {
                        using (EventLog eventLog = new EventLog("Application"))
                        {
                            eventLog.Source = "Application Error";
                            eventLog.WriteEntry("Connection to Esker SFTP server" + servername + ":" + serverport + error + "Exceeded maximum amount of retries: " + maxRetries + "\n retrying in 5 minutes.", EventLogEntryType.Information, 1000, 100);
                        }
                    }
                }
                // catch and write other exceptions
                catch (Exception e)
                {
                    attemptCounter++;
                    using (EventLog eventLog = new EventLog("Application"))
                    {
                        eventLog.Source = "Application Error";
                        eventLog.WriteEntry("Connection to Esker SFTP server" + servername + ":" + serverport + ". \n" + e.ToString(), EventLogEntryType.Error, 1000, 100);
                    }
                }

            }

        }

        /// <summary>
        /// get all files that end with ".xml" from the SFTP and place them on a local folder
        /// </summary>
        /// <param name="client">connection data</param>
        public void GetFile(SftpClient client)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = ".NET Runtime";
                eventLog.WriteEntry("Connection to Esker SFTP server" + servername + ":" + serverport + ". \nConnection succeeded, start reading files", EventLogEntryType.Information, 1023);
            }
            int amountFiles = 0;
            foreach (var ftpfile in client.ListDirectory("./BizTalkTESTONLY"))
            {
                if (ftpfile.Name.EndsWith(".xml"))
                {
                    amountFiles++;
                    var destinationFile = Path.Combine("C:/BizTalk/Customers/Esker", ftpfile.Name);
                    using (var fs = new FileStream(destinationFile, FileMode.OpenOrCreate))
                    {
                        var data = client.ReadAllText(ftpfile.FullName);

                        if (!string.IsNullOrEmpty(data))
                        {
                            client.DownloadFile(ftpfile.FullName, fs);
                            client.DeleteFile(ftpfile.FullName);
                        }
                    }
                }
            }
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = ".NET Runtime";
                if (amountFiles == 0)
                {
                    eventLog.WriteEntry("Connection to Esker SFTP server" + servername + ":" + serverport + ". \nNo files were found on the Esker SFTP Server", EventLogEntryType.Information, 1023);
                }
                else
                {
                    eventLog.WriteEntry("Connection to Esker SFTP server" + servername + ":" + serverport + ". \n" + amountFiles + " file(s) were found and moved", EventLogEntryType.Information, 1023);
                }
            }


        }
    }
}
