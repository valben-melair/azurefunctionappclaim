using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Storage.DataMovement;
using Microsoft.Azure.Storage.File;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FMGClaimFile.Upload.AzureFileService
{
    public class AzureFile
    {

        #region Properties

        private readonly ILogger _log;
        private static string sharedAccessSignature = Environment.GetEnvironmentVariable("SharedAccessSignature");
        private static string storageAccountName = Environment.GetEnvironmentVariable("StorageAccountName");
        private static string fileShareName = Environment.GetEnvironmentVariable("FileShareName");
        private static string connectionString = string.Format("FileEndpoint=https://{0}.file.core.windows.net/;SharedAccessSignature={1}", storageAccountName, sharedAccessSignature);
        private static ShareClient share = new(connectionString, fileShareName);

        #endregion

        #region Constructor

        public AzureFile(ILogger log)
        {
            _log = log;
        }

        #endregion

        #region Methods

        public async Task<ShareFileClient> GetAzureFileClient(string claimFolder, string documentId, string fileName)
        {
            try
            {
                //create claim directory if it doesn't already exist
                if (!share.GetDirectoryClient(claimFolder).Exists()) { var claimDirectory = await share.CreateDirectoryAsync(claimFolder).ConfigureAwait(false); }

                var documentIdDirectory = await share.CreateDirectoryAsync(claimFolder + "/" + documentId).ConfigureAwait(false);

                var fullDirectoryClient = share.GetDirectoryClient(claimFolder + "/" + documentId);

                var fileClient = fullDirectoryClient.GetFileClient(fileName);

                return fileClient;
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message, null);
                throw new ArgumentNullException(ex.Message);
            }
        }

        public async Task SetMetadata(ShareFileClient shareFileClient, string contentType, string scanResult)
        {
            try
            {
                //set file metadata
                IDictionary<string, string> metadata = new Dictionary<string, string>();
                metadata.Add("ContentType", contentType);
                metadata.Add("ScanResult", scanResult);
                await shareFileClient.SetMetadataAsync(metadata).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message, null);
                throw new ArgumentNullException(ex.Message);
            }
        }

        public async Task<Dictionary<string, string>> GetFile(string filePath)
        {
            try
            {
                var fileDirectoryClient = share.GetDirectoryClient(filePath);

                var remaining = new Queue<ShareDirectoryClient>();
                remaining.Enqueue(fileDirectoryClient);

                Dictionary<string,string> fileProperties = new Dictionary<string, string>();
                while (remaining.Count > 0)
                {
                    ShareDirectoryClient dir = remaining.Dequeue();
                    foreach (ShareFileItem item in dir.GetFilesAndDirectories())
                    {
                        var fileClient = fileDirectoryClient.GetFileClient(item.Name);
                        CloudFile cloudFile = new CloudFile(fileClient.Uri);

                        Stream fileStream = new MemoryStream();
                        fileStream.Position = 0;

                        // Setup the number of the concurrent operations
                        //TransferManager.Configurations.ParallelOperations = 2;
                        await TransferManager.DownloadAsync(cloudFile, fileStream).ConfigureAwait(false);

                        byte[] fileArray = new byte[fileStream.Length];
                        fileStream.Seek(0, SeekOrigin.Begin);
                        await fileStream.ReadAsync(fileArray, 0, fileArray.Length).ConfigureAwait(false);

                        fileProperties.Add("base64File", Convert.ToBase64String(fileArray));
                        string contentType;
                        cloudFile.Metadata.TryGetValue("ContentType", out contentType);
                        fileProperties.Add("contentType", contentType);
                        fileProperties.Add("fileName", item.Name);
                    }
                }

                return fileProperties;

            }
            catch(Exception ex)
            {
                _log.LogError(ex.Message, null);
                throw new ArgumentNullException(ex.Message);
            }
        }

        public async Task RemoveFile(string directoryPath)
        {
            try
            {
                var fileDirectoryClient = share.GetDirectoryClient(directoryPath);
                var remaining = new Queue<ShareDirectoryClient>();
                remaining.Enqueue(fileDirectoryClient);

                while (remaining.Count > 0)
                {
                    ShareDirectoryClient dir = remaining.Dequeue();
                    var filesAndDirectories = dir.GetFilesAndDirectories();
                    foreach (ShareFileItem item in dir.GetFilesAndDirectories())
                    {
                        if (!item.IsDirectory)
                        {
                            //fileName = item.Name;
                            var fileClient = fileDirectoryClient.GetFileClient(item.Name);
                            CloudFile cloudFile = new CloudFile(fileClient.Uri);

                            await cloudFile.DeleteIfExistsAsync().ConfigureAwait(false);
                        }
                    }
                }
                //delete directory fails if directory is not empty
                try { await fileDirectoryClient.DeleteIfExistsAsync().ConfigureAwait(false); } catch { }
            }
            catch(Exception ex)
            {
                _log.LogError(ex.Message, null);
                throw new ArgumentNullException(ex.Message);
            }
        }
 
        #endregion

    }
}
