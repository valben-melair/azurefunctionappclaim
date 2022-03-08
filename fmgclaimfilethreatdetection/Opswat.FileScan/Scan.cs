using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FMGClaimFile.Upload.Opswat
{
    public class Scan
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _log;

        public Scan(HttpClient httpClient, ILogger log) 
        {
            _httpClient = httpClient;
            _log = log;
        }

        public async Task<string> AnalyzeFile(Stream fileStream, string opswatKey, string APIMKey, string opswatUrl)
        {
            string opwatFileScanId = string.Empty;
            try
            {
                _log.LogInformation("AnalyzeFile -- started");
                var opswatPostAnalylzeUrl = $"{opswatUrl}file";
#if DEBUG
                opswatPostAnalylzeUrl = "https://api.metadefender.com/v4/file";
#endif
                HttpResponseMessage response;
                // Write file into a new stream
                fileStream.Position = 0;
                MemoryStream ms = new MemoryStream();
                await fileStream.CopyToAsync(ms).ConfigureAwait(false);

                // Add the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(ms.ToArray()))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    // Call to API
                    if (!_httpClient.DefaultRequestHeaders.Contains("apikey")) _httpClient.DefaultRequestHeaders.Add("apikey", opswatKey);
                    if (!_httpClient.DefaultRequestHeaders.Contains("Ocp-Apim-Subscription-Key"))  _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", APIMKey);
                    response = await _httpClient.PostAsync(opswatPostAnalylzeUrl, content).ConfigureAwait(false);
                }

                string responseFromServer = await response.Content.ReadAsStringAsync().ConfigureAwait(false); ;
                _log.LogInformation("AnalyzeFile Opswat Response -->" + responseFromServer);
                // Deserialize the json response object
                PostAnalyzeResponse opswat = JsonConvert.DeserializeObject<PostAnalyzeResponse>(responseFromServer);
                opwatFileScanId = opswat.data_id;

                _log.LogInformation("AnalyzeFile -- Completed");
                return opwatFileScanId;
            }
            catch (Exception ex)
            {
                _log.LogError($"AnalyzeFile failed: { ex.Message } \n { ex.StackTrace }", ex);
                throw new ArgumentNullException(ex.Message);
            }
        }

        public async Task<GetResultResponse>  GetResult(string id, string opswatKey, string APIMKey, string opswatUrl)
        {
            int progressPercentage = 0;
            GetResultResponse getResultResponse = null;
            try
            {
                _log.LogInformation("GetResult -- started");
                if (!_httpClient.DefaultRequestHeaders.Contains("x-file-metadata"))  _httpClient.DefaultRequestHeaders.Add("x-file-metadata", "1");
                if (!_httpClient.DefaultRequestHeaders.Contains("apikey")) _httpClient.DefaultRequestHeaders.Add("apikey", opswatKey);
                if (!_httpClient.DefaultRequestHeaders.Contains("Ocp-Apim-Subscription-Key")) _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", APIMKey);

                //var 
                var opswatGetFileUrl = $"{opswatUrl}file/{id}";
#if DEBUG
                opswatGetFileUrl = $"https://api.metadefender.com/v4/file/{id}";
#endif
                //var opswatGetFileUrl = $"https://devapi.npfmgservices.fmg.net/opswat/file/{id}";
                while (progressPercentage < 100)
                {
                    var response = await _httpClient.GetAsync(opswatGetFileUrl).ConfigureAwait(false);
                    var responseDataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    // Read the content
                    StreamReader reader = new StreamReader(responseDataStream);
                    string responseFromServer = await reader.ReadToEndAsync().ConfigureAwait(false); ;
                    //// Deserialize the json response object
                    getResultResponse = JsonConvert.DeserializeObject<GetResultResponse>(responseFromServer);
                    progressPercentage = getResultResponse.scan_results.progress_percentage;
                }
                _log.LogInformation("GetResult Opswat Response -->" + getResultResponse);


                _log.LogInformation("GetResult -- Completed");
                return getResultResponse;
            }
            catch (Exception ex)
            {
                _log.LogError($"GetResult failed: { ex.Message } \n { ex.StackTrace }", ex);
                throw new ArgumentNullException(ex.Message);
            }
            finally
            {
                _httpClient.Dispose();
            }
        }

        public async Task<GetResultResponse> HashLookup(string hashValue, string opswatKey, string APIMKey)
        {
            GetResultResponse getResultResponse = null;
            try
            {
                _log.LogInformation("HashLookup -- started");

                var opswatHashLookupUrl = $"https://devapi.npfmgservices.fmg.net/opswat/hash/{hashValue}";
#if DEBUG
                opswatHashLookupUrl = $"https://api.metadefender.com/v4/hash/{hashValue}";
#endif 
                if (!_httpClient.DefaultRequestHeaders.Contains("x-file-metadata")) _httpClient.DefaultRequestHeaders.Add("x-file-metadata", "1");
                if (!_httpClient.DefaultRequestHeaders.Contains("apikey")) _httpClient.DefaultRequestHeaders.Add("apikey", opswatKey);
                if (!_httpClient.DefaultRequestHeaders.Contains("Ocp-Apim-Subscription-Key")) _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", APIMKey);

                // Make the call and get the response.
                var response = await _httpClient.GetAsync(opswatHashLookupUrl);
                _log.LogInformation("HashLookup response -->" + response);

                var responseDataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                // Read the content
                StreamReader reader = new StreamReader(responseDataStream);
                string responseFromServer = await reader.ReadToEndAsync().ConfigureAwait(false);
                // Deserialize the json response object
                getResultResponse = JsonConvert.DeserializeObject<GetResultResponse>(responseFromServer);

                _log.LogInformation("HashLookup -- Completed");

                return getResultResponse;
            }
            catch (Exception ex)
            {
                _log.LogError($"HashLookup failed: { ex.Message } \n { ex.StackTrace }", ex);
                throw new ArgumentNullException(ex.Message);
            }
        }
    }
}
