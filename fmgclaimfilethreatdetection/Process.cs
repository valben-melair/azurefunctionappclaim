using FMGClaimFile.Upload.Azure.KeyVault;
using FMGClaimFile.Upload.AzureFileService;
using FMGClaimFile.Upload.Opswat;
using HttpMultipartParser;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.Storage.DataMovement;
using Microsoft.Azure.Storage.File;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FMG.ClaimFileUpload
{
    public class ProcessFile
    {

        #region Properties

        private int[] SafeResults = { 0, 7 };
        private HttpClient _httpClient;
        private KeyVaultClient keyVaultClient;
        private static string opswatUrl = Environment.GetEnvironmentVariable("opswatAPIM");
        private static string keyVaultUrl = Environment.GetEnvironmentVariable("keyVaultUrl");
        private static string keyVaultOpswatSecretName = Environment.GetEnvironmentVariable("keyVaultOpswatSecretName");
        private static string keyVaultOpswatSecretVersion = Environment.GetEnvironmentVariable("keyVaultOpswatSecretVersion");
        private static string keyVaultAPIMSecretName = Environment.GetEnvironmentVariable("keyVaultAPIMSecretName");
        private static string keyVaultAPIMSecretVersion = Environment.GetEnvironmentVariable("keyVaultAPIMSecretVersion");

        #endregion

        public ProcessFile(IHttpClientFactory clientFactory)
        {
            _httpClient = clientFactory.CreateClient("opswatServiceClient");
        }

        #region Main Method

        [FunctionName("ProcessFile")]
        public async Task<HttpResponseMessage> Run(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
           ILogger log)
        {
            if (req.Method == "GET") return new(HttpStatusCode.OK);

            //set variables
            string ClaimNumber = req.Headers["claimNumber"];
            string FileName = req.Headers["fileName"];
            string ContentType = req.Headers["contentType"];
            string DocumentID = Guid.NewGuid().ToString();
            string OpswatScanFileId;
            GetResultResponse getResultResponse;

            try
            {
                ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount*8;
                ServicePointManager.Expect100Continue = false;

                //Get the request stream
                MemoryStream fileStream = new MemoryStream();
                await req.Body.CopyToAsync(fileStream).ConfigureAwait(false);
                fileStream.Position = 0;
                MultipartFormDataParser parser = null;
                FilePart file = null;
                try
                {
                    parser = await MultipartFormDataParser.ParseAsync(fileStream).ConfigureAwait(false);
                    file = parser.Files.FirstOrDefault();
                    if (file.Data != null)
                    {
                        FileName = file.FileName;
                        ContentType = file.ContentType;
                        file.Data.Position = 0;
                        fileStream = new MemoryStream();
                        await file.Data.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                }
                catch {}

                //Upload File to Azure Storage
                AzureFile azureFile = new AzureFile(log);
                string claimFolder = $"scanned/{ClaimNumber}";
                var azureFileClient = await azureFile.GetAzureFileClient(claimFolder, DocumentID, FileName).ConfigureAwait(false);
                CloudFile scannedCloudFile = new(azureFileClient.Uri);
                fileStream.Position = 0;
                // Create a TransferContext
                SingleTransferContext uploadFileContext = new SingleTransferContext();
                // Record the overall progress
                ProgressRecorder recorder = new ProgressRecorder();
                uploadFileContext.ProgressHandler = recorder;

                //TransferManager.Configurations.ParallelOperations = Environment.ProcessorCount*8;
                //log.LogInformation("Transfer Configuration ParallelOperations: {0}", TransferManager.Configurations.ParallelOperations);
                await TransferManager.UploadAsync(fileStream, scannedCloudFile, null, uploadFileContext, CancellationToken.None).ConfigureAwait(false);
                log.LogInformation("Final azure file upload state: {0}", recorder.ToString());

                //Get opswat api key from AzureKeyVault
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                KeyVaultManager keyVaultManager = new KeyVaultManager(keyVaultClient);
                string opswatKey = await keyVaultManager.GetSecret(keyVaultUrl, keyVaultOpswatSecretName, keyVaultOpswatSecretVersion).ConfigureAwait(false);
                string apimKey = await keyVaultManager.GetSecret(keyVaultUrl, keyVaultAPIMSecretName, keyVaultAPIMSecretVersion).ConfigureAwait(false);

                //Post File to Opswat
                Scan opswatScan = new Scan(_httpClient, log);
                OpswatScanFileId = await opswatScan.AnalyzeFile(fileStream, opswatKey, apimKey, opswatUrl).ConfigureAwait(false);

                //Get Opswat File Scan Result
                getResultResponse = await opswatScan.GetResult(OpswatScanFileId, opswatKey, apimKey, opswatUrl).ConfigureAwait(false);

                //Set File Metadata - ContentType & ScanResult
                string scanResultDesc = getResultResponse.scan_results.scan_all_result_a;
                await azureFile.SetMetadata(azureFileClient, ContentType, scanResultDesc).ConfigureAwait(false);

                int scanResultVal = getResultResponse.scan_results.scan_all_result_i;
                var response = new { claimNumber = ClaimNumber, documentID = DocumentID, scanResult = scanResultVal };
                var jsonResponse = JsonConvert.SerializeObject(response);

                //Handle infected file
                if (Array.IndexOf(SafeResults, scanResultVal) < 0)
                {
                    //Write file to the quarantine directory
                    claimFolder = $"quarantine/{ClaimNumber}";
                    azureFileClient = await azureFile.GetAzureFileClient(claimFolder, DocumentID, FileName).ConfigureAwait(false);
                    CloudFile quarantinedCloudFile = new(azureFileClient.Uri);
                    await TransferManager.CopyAsync(scannedCloudFile, quarantinedCloudFile, CopyMethod.ServiceSideAsyncCopy).ConfigureAwait(false);

                    //Remove file and subdirectory from the scanned directory
                    string fileName = string.Empty;
                    var FileFolder = $"scanned/{ClaimNumber}/{DocumentID}";
                    await azureFile.RemoveFile(FileFolder).ConfigureAwait(false);

                    throw new Exception(scanResultVal.ToString());
                }

                HttpResponseMessage httpResponseMessage = new(HttpStatusCode.OK) { Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json") };
                httpResponseMessage.Headers.Add("contentType", ContentType);
                httpResponseMessage.Headers.Add("fileName", FileName);
                return httpResponseMessage;
            }
            catch (Exception ex)
            {
                var response = new { claimNumber = ClaimNumber, documentID = DocumentID, scanResult = ex.Message };
                var jsonResponse = JsonConvert.SerializeObject(response);

                HttpResponseMessage httpResponseMessage = new(HttpStatusCode.BadRequest) { Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json") };
                httpResponseMessage.Headers.Add("contentType", ContentType);
                httpResponseMessage.Headers.Add("fileName", FileName);
                return httpResponseMessage;
            }
            finally
            {
                _httpClient.Dispose();
                keyVaultClient.Dispose();
            }
        }

        #endregion

        private class ProgressRecorder : IProgress<TransferStatus>
        {
            private long latestBytesTransferred;
            private long latestNumberOfFilesTransferred;
            private long latestNumberOfFilesSkipped;
            private long latestNumberOfFilesFailed;

            public void Report(TransferStatus progress)
            {
                this.latestBytesTransferred = progress.BytesTransferred;
                this.latestNumberOfFilesTransferred = progress.NumberOfFilesTransferred;
                this.latestNumberOfFilesSkipped = progress.NumberOfFilesSkipped;
                this.latestNumberOfFilesFailed = progress.NumberOfFilesFailed;
            }

            public override string ToString()
            {
                return string.Format("Transferred bytes: {0}; Transfered: {1}; Skipped: {2}, Failed: {3}",
                    this.latestBytesTransferred,
                    this.latestNumberOfFilesTransferred,
                    this.latestNumberOfFilesSkipped,
                    this.latestNumberOfFilesFailed);
            }
        }
    }

    public class GetFile
    {
        #region Main Method

        [FunctionName("GetFile")]
        public async Task<HttpResponseMessage> Run(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
          ILogger log)
        {
            if (req.Method == "GET") return new(HttpStatusCode.OK); 

            //set variables
            string ClaimNumber = req.Headers["claimNumber"];
            string DocumentID = "";

            try
            {
                //Get request content
                StreamReader requestStream = new StreamReader(req.Body);
                var readRequestBody = await requestStream.ReadToEndAsync();
                //dynamic data = JsonConvert.DeserializeObject(ReadRequestBody.Result);
                JObject jo = JObject.Parse(readRequestBody);
                dynamic data = JsonConvert.DeserializeObject(jo.SelectToken("Attachments[0]").ToString());
                DocumentID = data.DocumentID;

                //Get File from Azure Storage
                string FileFolder = $"scanned/{ClaimNumber}/{DocumentID}";
                AzureFile azureFile = new AzureFile(log);
                var downloadFileFromAzureResult = await azureFile.GetFile(FileFolder).ConfigureAwait(false);

                //Set Response Properties
                string FileName;
                downloadFileFromAzureResult.TryGetValue("fileName", out FileName);
                string ContentType;
                downloadFileFromAzureResult.TryGetValue("contentType", out ContentType);
                string FileContent;
                downloadFileFromAzureResult.TryGetValue("base64File", out FileContent);

                var response = new { claimNumber = ClaimNumber, documentID = DocumentID, fileName = FileName, contentType = ContentType, fileContent = FileContent };
                var jsonResponse = JsonConvert.SerializeObject(response);

                HttpResponseMessage httpResponseMessage = new(HttpStatusCode.OK) { Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json") };

                return httpResponseMessage;
            }
            catch (Exception ex)
            {
                var response = new { claimNumber = ClaimNumber, documentID = DocumentID, error = ex.Message };
                var jsonResponse = JsonConvert.SerializeObject(response);

                return new(HttpStatusCode.BadRequest) { Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json") };
            }
        }
        #endregion
    }

    public class RemoveFile
    {

        #region Main Method

        [FunctionName("RemoveFile")]
        public async Task<HttpResponseMessage> Run(
         [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
         ILogger log)
        {
            try
            {
                //set variables
                string ClaimNumber = req.Headers["claimNumber"];
                string DocumentID = req.Headers["documentId"];
                string FileFolder = "";

                string fileName = string.Empty;
                AzureFile azureFile = new AzureFile(log);
                //Remove file and subdirectory from Azure
                FileFolder = $"scanned/{ClaimNumber}/{DocumentID}";
                await azureFile.RemoveFile(FileFolder);
                //Remove claimNumber directory from Azure
                FileFolder = $"scanned/{ClaimNumber}";
                await azureFile.RemoveFile(FileFolder);

                return new(HttpStatusCode.OK) { Content = new StringContent($"file id {DocumentID} has been deleted") };
            }
            catch (Exception ex)
            {
                return new(HttpStatusCode.BadRequest) { Content = new StringContent(ex.Message) };
            }
        }

        #endregion
    }

    public class EndPointKeepWarm
    {
        private static HttpClient _httpClient = new HttpClient();
        private static string _endPointsToHit = Environment.GetEnvironmentVariable("EndPointUrls");

        [FunctionName("EndPointKeepWarm")]
        // run every 15 minutes..
        public static async Task Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Run(): EndPointKeepWarm function executed at: {DateTime.Now}. Past due? {myTimer.IsPastDue}");

            if (!string.IsNullOrEmpty(_endPointsToHit))
            {
                string[] endPoints = _endPointsToHit.Split(';');
                foreach (string endPoint in endPoints)
                {
                    string tidiedUrl = endPoint.Trim();

                    log.LogInformation($"Run(): About to hit URL: '{tidiedUrl}'");

                    HttpResponseMessage response = await hitUrl(tidiedUrl, log).ConfigureAwait(false);
                }
            }
            else
            {
                log.LogError($"Run(): No URLs specified in environment variable 'EndPointUrls'. Expected a single URL or multiple URLs " +
                    "separated with a semi-colon (;). Please add this config to use the tool.");
            }

            log.LogInformation($"Run(): Completed..");
        }

        private static async Task<HttpResponseMessage> hitUrl(string url, ILogger log)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                log.LogInformation($"hitUrl(): Successfully hit URL: '{url}'");
            }
            else
            {
                log.LogError($"hitUrl(): Failed to hit URL: '{url}'. Response: {(int)response.StatusCode + " : " + response.ReasonPhrase}");
            }

            return response;
        }
    }
}
