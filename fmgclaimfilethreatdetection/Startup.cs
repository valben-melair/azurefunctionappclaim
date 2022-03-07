using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Net.Security;

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
                            (sender, certificate, chain, errors) =>
                            {
                                // local dev, just approve all certs
                                if (Environment.GetEnvironmentVariable("IsDev") == "1") return true;
                                return errors == SslPolicyErrors.None;
                            }
        });
        }
    }
}
