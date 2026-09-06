using System;
using Newtonsoft.Json;

namespace ClaimsFlow.Portal.Contracts
{
    /// <summary>
    /// Copy of the claim payload served by claimsflow-functions.
    ///
    /// This is deliberately a COPY, not a project reference: the two repositories
    /// are deployed independently and the JSON on the wire is the only thing that
    /// binds them. Duplication here is what makes it a real contract.
    /// </summary>
    public class ClaimView
    {
        [JsonProperty("claimId")] public string ClaimId { get; set; }
        [JsonProperty("partnerId")] public string PartnerId { get; set; }
        [JsonProperty("correlationId")] public string CorrelationId { get; set; }
        [JsonProperty("region")] public string Region { get; set; }
        [JsonProperty("policyNumber")] public string PolicyNumber { get; set; }
        [JsonProperty("amountClaimed")] public double AmountClaimed { get; set; }
        [JsonProperty("currency")] public string Currency { get; set; }
        [JsonProperty("riskScore")] public int RiskScore { get; set; }
        [JsonProperty("status")] public string Status { get; set; }
        [JsonProperty("decision")] public string Decision { get; set; }
        [JsonProperty("decidedBy")] public string DecidedBy { get; set; }
        [JsonProperty("decidedAt")] public DateTimeOffset? DecidedAt { get; set; }
        [JsonProperty("submittedAt")] public DateTimeOffset SubmittedAt { get; set; }
        [JsonProperty("manualReviewDeadline")] public DateTimeOffset ManualReviewDeadline { get; set; }
        [JsonProperty("documentPath")] public string DocumentPath { get; set; }
    }

    /// <summary>
    /// Message written to the fraud-decision queue and consumed by
    /// claimsflow-functions. Shape AND encoding are a cross-repo contract.
    /// </summary>
    public class FraudDecisionMessage
    {
        [JsonProperty("claimId")] public string ClaimId { get; set; }
        [JsonProperty("partnerId")] public string PartnerId { get; set; }
        [JsonProperty("decision")] public string Decision { get; set; }
        [JsonProperty("decidedBy")] public string DecidedBy { get; set; }
        [JsonProperty("decidedAt")] public DateTimeOffset DecidedAt { get; set; }
    }

    public static class ClaimStatuses
    {
        public const string Accepted = "accepted";
        public const string AwaitingReview = "awaiting-review";
        public const string Approved = "approved";
        public const string Rejected = "rejected";
        public const string Expired = "expired";
    }
}
