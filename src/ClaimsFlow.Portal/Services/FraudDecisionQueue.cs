using System.Threading.Tasks;
using ClaimsFlow.Portal.Contracts;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ClaimsFlow.Portal.Services
{
    public interface IFraudDecisionQueue
    {
        Task EnqueueAsync(FraudDecisionMessage decision);
    }

    /// <summary>
    /// Publishes operator decisions to the fraud-decision queue, which
    /// claimsflow-functions consumes.
    ///
    /// IMPORTANT: CloudQueue.EncodeMessage defaults to true, so every message
    /// goes on the wire base64-encoded. The Functions queue trigger on the other
    /// end decodes on that assumption. Azure.Storage.Queues v12 does NOT encode
    /// by default — swapping this class for a QueueClient without carrying the
    /// encoding across will silently poison every decision message.
    /// </summary>
    public class FraudDecisionQueue : IFraudDecisionQueue
    {
        private readonly CloudQueue _queue;

        public FraudDecisionQueue(IConfiguration configuration)
        {
            var account = CloudStorageAccount.Parse(configuration.GetConnectionString("ClaimsStorage"));
            _queue = account.CreateCloudQueueClient().GetQueueReference("fraud-decision");
        }

        public async Task EnqueueAsync(FraudDecisionMessage decision)
        {
            var payload = JsonConvert.SerializeObject(decision);
            await _queue.AddMessageAsync(new CloudQueueMessage(payload));
        }
    }
}
