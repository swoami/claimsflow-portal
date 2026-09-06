using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;

namespace ClaimsFlow.Portal.Services
{
    public interface IDocumentStore
    {
        Task<Stream> OpenAsync(string documentPath);
    }

    /// <summary>
    /// Downloads the original claim document through the deprecated
    /// Microsoft.Azure.Storage.Blob SDK. Modern replacement: Azure.Storage.Blobs.
    /// </summary>
    public class DocumentStore : IDocumentStore
    {
        private readonly CloudBlobContainer _container;

        public DocumentStore(IConfiguration configuration)
        {
            var account = CloudStorageAccount.Parse(configuration.GetConnectionString("ClaimsStorage"));
            _container = account.CreateCloudBlobClient().GetContainerReference("claims");
        }

        public async Task<Stream> OpenAsync(string documentPath)
        {
            var blob = _container.GetBlockBlobReference(documentPath);

            if (!await blob.ExistsAsync())
            {
                return null;
            }

            var stream = new MemoryStream();
            await blob.DownloadToStreamAsync(stream);
            stream.Position = 0;
            return stream;
        }
    }
}
