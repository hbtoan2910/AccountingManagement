using System;
using System.IO;
using System.Text;
using Dropbox.Api;
using Serilog;

namespace AccountingManagement.ErrorReporting
{
    public class ErrorReportingClient
    {
        public ErrorReportingClient()
        {

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
