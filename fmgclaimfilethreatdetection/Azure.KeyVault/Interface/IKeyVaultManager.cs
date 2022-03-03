using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FMG.ClaimFileUpload.Interface
{
    public interface IKeyVaultManager
    {
        public Task<string> GetSecret(string keyVaultUrl, string secretName, string secretVersion);
    }
}
