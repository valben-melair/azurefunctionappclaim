using FMG.UnitTests.Common;
using FMGClaimFile.Upload.Opswat;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace FMG.UnitTests
{
    public class ParallelScanUnitTest1 
    {
        private HttpClient client;
        private HttpClientHandler _handler;

        [Theory]
        [InlineData("sample4.bmp")]
        [InlineData("sample5.bmp")]
        public async void WithOpswatAPICalls_ReturnOKTest(string filename)
        {
            // Arrange
            var _log = new Mock<ILogger<Scan>>();
            var fileStream = new FileStream(@"data/" + filename, FileMode.Open);

            //_handler.ServerCertificateCustomValidationCallback +=
                            //(sender, certificate, chain, errors) =>
                            //{
                            //    return true;
                            //};
            //client = new HttpClient(_handler);
            client = new HttpClient();
            Scan opswatScan = new Scan(client, _log.Object);

            // Act
            var OpswatScanFileId = await opswatScan.AnalyzeFile(fileStream, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            GetResultResponse getResultResponse = await opswatScan.GetResult(OpswatScanFileId, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            int scanResultVal = getResultResponse.scan_results.scan_all_result_i;

            // Assert
            Assert.Equal(0, scanResultVal);
        }

    }

    public class ParallelScanUnitTest2
    {
        private HttpClient client;
        private HttpClientHandler _handler;

        [Theory]
        [InlineData("sample6.bmp")]
        [InlineData("sample7.bmp")]
        [InlineData("sample8.bmp")]
        public async void WithOpswatAPICalls_ReturnOKTest(string filename)
        {
            // Arrange
            var _log = new Mock<ILogger<Scan>>();
            var fileStream = new FileStream(@"data/" + filename, FileMode.Open);

            //_handler.ServerCertificateCustomValidationCallback +=
            //                (sender, certificate, chain, errors) =>
            //                {
            //                    return true;
            //                };
            client = new HttpClient();
            Scan opswatScan = new Scan(client, _log.Object);

            // Act
            var OpswatScanFileId = await opswatScan.AnalyzeFile(fileStream, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            GetResultResponse getResultResponse = await opswatScan.GetResult(OpswatScanFileId, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            int scanResultVal = getResultResponse.scan_results.scan_all_result_i;

            // Assert
            Assert.Equal(0, scanResultVal);
        }

    }

    public class ParallelScanUnitTest3
    {
        private HttpClient client;
        private HttpClientHandler _handler;

        [Theory]
        [InlineData("sample9.bmp")]
        [InlineData("sample10.bmp")]
        public async void WithOpswatAPICalls_ReturnOKTest(string filename)
        {
            // Arrange
            var _log = new Mock<ILogger<Scan>>();
            var fileStream = new FileStream(@"data/" + filename, FileMode.Open);

            //_handler.ServerCertificateCustomValidationCallback +=
            //                (sender, certificate, chain, errors) =>
            //                {
            //                    return true;
            //                };
            client = new HttpClient();
            Scan opswatScan = new Scan(client, _log.Object);

            // Act
            var OpswatScanFileId = await opswatScan.AnalyzeFile(fileStream, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            GetResultResponse getResultResponse = await opswatScan.GetResult(OpswatScanFileId, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            int scanResultVal = getResultResponse.scan_results.scan_all_result_i;

            // Assert
            Assert.Equal(0, scanResultVal);
        }
    }
    public class ParallelScanUnitTest4
    {
        private HttpClient client;
        private HttpClientHandler _handler;

        [Theory]
        [InlineData("sample11.bmp")]
        [InlineData("sample12.bmp")]
        [InlineData("sample13.bmp")]
        public async void WithOpswatAPICalls_ReturnOKTest(string filename)
        {
            // Arrange
            var _log = new Mock<ILogger<Scan>>();
            var fileStream = new FileStream(@"data/" + filename, FileMode.Open);

            //_handler.ServerCertificateCustomValidationCallback +=
            //                (sender, certificate, chain, errors) =>
            //                {
            //                    return true;
            //                };
            client = new HttpClient();
            Scan opswatScan = new Scan(client, _log.Object);

            // Act
            var OpswatScanFileId = await opswatScan.AnalyzeFile(fileStream, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            GetResultResponse getResultResponse = await opswatScan.GetResult(OpswatScanFileId, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            int scanResultVal = getResultResponse.scan_results.scan_all_result_i;

            // Assert
            Assert.Equal(0, scanResultVal);
        }
    }

}
