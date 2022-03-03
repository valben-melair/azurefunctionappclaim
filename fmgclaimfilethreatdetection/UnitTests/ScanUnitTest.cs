using FMG.UnitTests.Common;
using FMGClaimFile.Upload.Opswat;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO;
using System.Net.Http;
using Xunit;

namespace FMG.UnitTests
{
    public class ScanUnitTest 
    {
        private HttpClient client;
        private HttpClientHandler _handler;

        [Fact]
        public async void WithAnalyzeFile_ReturnNOTNull()
        {
            // Arrange
            var _log = new Mock<ILogger<Scan>>();
            var fileStream = new FileStream(@"data/sample.bmp", FileMode.Open);
            client = new HttpClient();
            Scan opswatScan = new Scan(client, _log.Object);

            // Act
            var OpswatScanFileId = await opswatScan.AnalyzeFile(fileStream, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);

            // Assert
            Assert.NotNull(OpswatScanFileId);
            fileStream.Close();
        }

        //[Fact]
        //public async void WithAnalyzeFile_ReturnNull()
        //{
        //    // Arrange
        //    var _log = new Mock<ILogger<Scan>>();
        //    var fileStream = new FileStream(@"data/sample1.bmp", FileMode.Open);
        //    client = new HttpClient();
        //    Scan opswatScan = new Scan(client, _log.Object);

        //    // Act
        //    var OpswatScanFileId = await opswatScan.AnalyzeFile(fileStream, Constants.opswatKey, Constants.apimkey, Constants.invOpswatAPIM);

        //    // Assert
        //    Assert.Null(OpswatScanFileId);
        //    fileStream.Close();
        //}

        [Fact]
        public async void WithGetResult_ReturnOK()
        {
            // Arrange
            var _log = new Mock<ILogger<Scan>>();
            var fileStream = new FileStream(@"data/sample2.bmp", FileMode.Open);
            client = new HttpClient();
            Scan opswatScan = new Scan(client, _log.Object);

            // Act
            var OpswatScanFileId = await opswatScan.AnalyzeFile(fileStream, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            GetResultResponse getResultResponse = await opswatScan.GetResult(OpswatScanFileId, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            int scanResultVal = getResultResponse.scan_results.scan_all_result_i;

            // Assert
            Assert.Equal(0, scanResultVal);
            fileStream.Close();
        }

        [Fact]
        public async void WithGetResult_ReturnNotOK()
        {
            // Arrange
            var _log = new Mock<ILogger<Scan>>();
            var fileStream = new FileStream(@"data/sample3.bmp", FileMode.Open);
            client = new HttpClient();
            Scan opswatScan = new Scan(client, _log.Object);

            // Act
            var OpswatScanFileId = await opswatScan.AnalyzeFile(fileStream, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            GetResultResponse getResultResponse = await opswatScan.GetResult(OpswatScanFileId, Constants.opswatKey, Constants.apimkey, Constants.opswatAPIM);
            int scanResultVal = getResultResponse.scan_results.scan_all_result_i;

            // Assert
            Assert.True(scanResultVal==0);
            fileStream.Close();
        }
    }
}
