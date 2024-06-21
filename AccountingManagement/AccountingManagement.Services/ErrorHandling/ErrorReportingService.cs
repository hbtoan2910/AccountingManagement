using Dropbox.Api;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AccountingManagement.Services.ErrorHandling
{
    public interface IErrorReportingService
    {
        void UploadLatestLogFile(string user);
    }

    public class ErrorReportingService : IErrorReportingService
    {
        public ErrorReportingService()
        {
            
        }

        public void UploadLatestLogFile(string user)
        {
            var latestLogFile = GetLocalLatestLogFile()
                ?? throw new FileNotFoundException("No Local Log file found to report");

            var client = new DropboxClient("-tEQ9ajqPCcAAAAAAAAAAfJonN1n_ZnPXTlsFkTda_5CL8THwxIdZv_t-Yv6HKHa",
                new DropboxClientConfig { });

            var filename = $"{user}-Log-{DateTime.Now:u}.log";

            using var stream = new FileStream(latestLogFile.FullName, FileMode.Open, FileAccess.Read);

            var task = client.Files.UploadAsync($"/{filename}", Dropbox.Api.Files.WriteMode.Overwrite.Instance, body: stream);

            task.Wait();
        }

        private FileInfo GetLocalLatestLogFile()
        {
            var appFolder = Path.GetDirectoryName(Assembly.GetEntryAssembly().ManifestModule.FullyQualifiedName);
            var logFolder = Path.Combine(appFolder, "logs");

            if (Directory.Exists(logFolder))
            {
                var logFolderInfo = new DirectoryInfo(logFolder);
                var logFiles = logFolderInfo.GetFiles("*.log");

                if (logFiles.Any())
                {
                    return logFiles.OrderBy(x => x.Name).LastOrDefault();
                }
            }

            return null;
        }

        public static void ReportException(Exception error, string message)
        {
            try
            {
                var client = new DropboxClient("-tEQ9ajqPCcAAAAAAAAAAfJonN1n_ZnPXTlsFkTda_5CL8THwxIdZv_t-Yv6HKHa",
                    new DropboxClientConfig { });

                var filename = $"Crash-Report-{DateTime.Now:u}.log";
                var stringBuilder = new StringBuilder(message);
                stringBuilder.AppendLine(error.Message);
                stringBuilder.AppendLine(error.ToString());

                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(stringBuilder.ToString()));

                var task = client.Files.UploadAsync($"/{filename}", Dropbox.Api.Files.WriteMode.Overwrite.Instance, body: stream);

                task.Wait();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to upload exception error.");
            }
        }
    }
}
