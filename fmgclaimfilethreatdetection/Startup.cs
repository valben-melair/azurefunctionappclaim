using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

[assembly: FunctionsStartup(typeof(FMG.ClaimFileUpload.Startup))]
namespace FMG.ClaimFileUpload
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient("opswatServiceClient")
                .ConfigurePrimaryHttpMessageHandler(
                    () => new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback =
                                (sender, certificate, chain, errors) => { return true; }
                    });
        }


    }
}
