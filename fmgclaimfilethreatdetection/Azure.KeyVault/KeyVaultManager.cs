using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using FMG.ClaimFileUpload.Interface;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMGClaimFile.Upload.Azure.KeyVault
{
    public class KeyVaultManager : IKeyVaultManager
    {
        private readonly KeyVaultClient _keyVaultClient;
        public KeyVaultManager(KeyVaultClient keyVaultClient) 
        {
            _keyVaultClient = keyVaultClient;
        }

        public async Task<string> GetSecret(string keyVaultUrl, string secretName, string secretVersion)
        {
            try
            {
                SecretBundle keyVaultSecret = await _keyVaultClient.GetSecretAsync(keyVaultUrl + "secrets/" + secretName + "/" + secretVersion).ConfigureAwait(false);
                return keyVaultSecret.Value;
            }
            catch
            {
                throw;
            }
        }
    }
}
