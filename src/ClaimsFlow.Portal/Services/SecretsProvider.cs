using System;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClaimsFlow.Portal.Services
{
    public interface ISecretsProvider
    {
        Task<string> GetSecretAsync(string name);
    }

    /// <summary>
    /// Reads secrets through the deprecated Microsoft.Azure.KeyVault SDK using
    /// AzureServiceTokenProvider. The modern pairing is
    /// Azure.Security.KeyVault.Secrets + Azure.Identity.
    /// </summary>
    public class SecretsProvider : ISecretsProvider
    {
        private readonly IKeyVaultClient _client;
        private readonly string _vaultUri;
        private readonly ILogger<SecretsProvider> _logger;

        public SecretsProvider(IKeyVaultClient client, IConfiguration configuration,
            ILogger<SecretsProvider> logger)
        {
            _client = client;
            _vaultUri = configuration["KeyVaultUri"];
            _logger = logger;
        }

        public async Task<string> GetSecretAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(_vaultUri))
            {
                _logger.LogWarning("KeyVaultUri is not configured; returning null for secret {Secret}.", name);
                return null;
            }

            try
            {
                var secret = await _client.GetSecretAsync(_vaultUri, name);
                return secret.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not read secret {Secret} from {Vault}.", name, _vaultUri);
                return null;
            }
        }
    }
}
