using System;
using System.IO;
using Serilog;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System.Reflection;

namespace AccountingManagement.Services
{
    public interface IDbBackupTool
    {
        void BackupMainDatabase();
        bool CheckBackupExistence();
    }

    public class DbBackupTool : IDbBackupTool
    {
        public const string BackupFolder = @"Backup";
        public const string DatabaseName = "HRKAccounting";

        public DbBackupTool()
        { }

        public void BackupMainDatabase()
        {
            var backupFile = GetBackupFilename();

            if (CheckBackupExistence())
            {
                Log.Information($"Backup file {backupFile} already exists. Stop backup sequence.");
                return;
            }

            Log.Information($"Start backup sequence for {backupFile}");

            var serverConnection = new ServerConnection("DESKTOP-QBCTR", "sa", "HRKAccounting!");

            var server = new Server(serverConnection);

            var backup = new Backup()
            {
                Action = BackupActionType.Database,
                BackupSetDescription = $"Full backup of {DatabaseName}",
                BackupSetName = $"{DatabaseName} backup",
                Database = DatabaseName,
                ExpirationDate = DateTime.Now.AddDays(360),
                // Set Incremental property to false to specify this is a full backup
                Incremental = false,
                LogTruncation = BackupTruncateLogType.Truncate,
            };

            var backupDeviceItem = new BackupDeviceItem(backupFile, DeviceType.File);

            try
            {
                backup.Devices.Add(backupDeviceItem);

                backup.SqlBackup(server);

                Log.Information($"Database backup completed.");
            }
            catch (Exception ex)
            {
                Log.Information($"Unable to backup database. {ex}");
            }
        }

        public bool CheckBackupExistence()
        {
            var directoryInfo = new DirectoryInfo(BackupFolder);
            if (directoryInfo.Exists == false)
            {
                directoryInfo.Create();
            }

            var fileInfo = new FileInfo(GetBackupFilename());

            return fileInfo.Exists;
        }

        private string GetBackupFilename()
        {
            var applicationPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().ManifestModule.FullyQualifiedName);
            var backupDate = DateTime.Now.ToString("yyyyMMdd");

            return @$"{applicationPath}\{BackupFolder}\{DatabaseName}_{backupDate}.bak";
        }
    }
}
