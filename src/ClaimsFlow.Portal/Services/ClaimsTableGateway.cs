using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;

namespace ClaimsFlow.Portal.Services
{
    /// <summary>
    /// A claim row as written by claimsflow-functions. Property names must match
    /// what that repository writes with Azure.Data.Tables.
    /// </summary>
    public class ClaimRow : TableEntity
    {
        public string CorrelationId { get; set; }
        public string Region { get; set; }
        public string PolicyNumber { get; set; }
        public double AmountClaimed { get; set; }
        public string Currency { get; set; }
        public int RiskScore { get; set; }
        public string Status { get; set; }
        public string Decision { get; set; }
        public string DecidedBy { get; set; }
        public DateTimeOffset? DecidedAt { get; set; }
        public DateTimeOffset SubmittedAt { get; set; }
        public DateTimeOffset ManualReviewDeadline { get; set; }
        public string DocumentPath { get; set; }
    }

    public interface IClaimsTableGateway
    {
        Task<IReadOnlyList<ClaimRow>> ListByPartnerAsync(string partnerId, int take = 50);
        Task<ClaimRow> GetAsync(string partnerId, string claimId);
    }

    /// <summary>
    /// Reads the claims table through the deprecated Microsoft.Azure.Cosmos.Table
    /// SDK. The modern replacement is Azure.Data.Tables.
    /// </summary>
    public class ClaimsTableGateway : IClaimsTableGateway
    {
        private readonly CloudTable _table;

        public ClaimsTableGateway(IConfiguration configuration)
        {
            // Baseline: the account key comes straight out of configuration.
            var account = CloudStorageAccount.Parse(configuration.GetConnectionString("ClaimsStorage"));
            _table = account.CreateCloudTableClient().GetTableReference("claims");
        }

        public async Task<IReadOnlyList<ClaimRow>> ListByPartnerAsync(string partnerId, int take = 50)
        {
            var filter = TableQuery.GenerateFilterCondition(
                "PartitionKey", QueryComparisons.Equal, partnerId);

            var query = new TableQuery<ClaimRow>().Where(filter).Take(take);

            var results = new List<ClaimRow>();
            TableContinuationToken token = null;

            do
            {
                var segment = await _table.ExecuteQuerySegmentedAsync(query, token);
                results.AddRange(segment.Results);
                token = segment.ContinuationToken;
            }
            while (token != null && results.Count < take);

            return results;
        }

        public async Task<ClaimRow> GetAsync(string partnerId, string claimId)
        {
            var result = await _table.ExecuteAsync(TableOperation.Retrieve<ClaimRow>(partnerId, claimId));
            return result.Result as ClaimRow;
        }
    }
}
