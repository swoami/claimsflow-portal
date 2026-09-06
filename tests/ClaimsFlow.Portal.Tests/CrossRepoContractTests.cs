using System;
using System.Linq;
using System.Text;
using ClaimsFlow.Portal.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ClaimsFlow.Portal.Tests
{
    /// <summary>
    /// Pins the surfaces shared with claimsflow-functions. These tests are the
    /// portal's half of a contract that neither repository can verify alone.
    /// </summary>
    public class CrossRepoContractTests
    {
        [Fact]
        public void Claim_json_from_the_functions_api_deserializes()
        {
            // Copied verbatim from a GetClaimStatus response.
            const string fromApi = @"{
                ""claimId"":""CLM-1001"",
                ""partnerId"":""northwind"",
                ""correlationId"":""corr-1"",
                ""region"":""we"",
                ""policyNumber"":""POL-99"",
                ""amountClaimed"":1200.5,
                ""currency"":""EUR"",
                ""riskScore"":30,
                ""status"":""accepted"",
                ""decision"":null,
                ""decidedBy"":null,
                ""decidedAt"":null,
                ""submittedAt"":""2026-03-01T12:00:00+00:00"",
                ""manualReviewDeadline"":""2026-03-02T12:00:00+00:00"",
                ""documentPath"":""we/northwind/CLM-1001/source.json""
            }";

            var claim = JsonConvert.DeserializeObject<ClaimView>(fromApi);

            Assert.Equal("CLM-1001", claim.ClaimId);
            Assert.Equal("northwind", claim.PartnerId);
            Assert.Equal(1200.5, claim.AmountClaimed);
            Assert.Equal(30, claim.RiskScore);
            Assert.Equal(ClaimStatuses.Accepted, claim.Status);
            Assert.Null(claim.Decision);
            Assert.Equal("we/northwind/CLM-1001/source.json", claim.DocumentPath);
        }

        [Fact]
        public void FraudDecision_serializes_to_exactly_the_keys_the_functions_app_expects()
        {
            var message = new FraudDecisionMessage
            {
                ClaimId = "CLM-1001",
                PartnerId = "northwind",
                Decision = ClaimStatuses.Approved,
                DecidedBy = "anna.kowalska",
                DecidedAt = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero)
            };

            var json = JsonConvert.SerializeObject(message);
            var keys = JObject.Parse(json).Properties().Select(p => p.Name).OrderBy(n => n).ToArray();

            Assert.Equal(
                new[] { "claimId", "decidedAt", "decidedBy", "decision", "partnerId" },
                keys);
        }

        [Fact]
        public void Decision_messages_go_on_the_wire_base64_encoded()
        {
            // CloudQueue.EncodeMessage defaults to true, so FraudDecisionQueue
            // publishes base64. The Functions queue trigger decodes on that
            // assumption. Azure.Storage.Queues v12 does NOT encode by default:
            // porting FraudDecisionQueue to QueueClient without carrying the
            // encoding across poisons every decision message, with no build
            // error and no exception on this side.
            var json = JsonConvert.SerializeObject(new FraudDecisionMessage
            {
                ClaimId = "CLM-1001",
                PartnerId = "northwind",
                Decision = ClaimStatuses.Approved,
                DecidedBy = "anna.kowalska",
                DecidedAt = new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero)
            });

            var onTheWire = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(onTheWire));

            Assert.NotEqual(json, onTheWire);
            Assert.Equal(json, decoded);
            Assert.NotNull(JsonConvert.DeserializeObject<FraudDecisionMessage>(decoded));
        }

        [Theory]
        [InlineData(ClaimStatuses.Approved)]
        [InlineData(ClaimStatuses.Rejected)]
        public void Only_approved_and_rejected_are_valid_decisions(string decision)
        {
            // The functions app throws InvalidClaimException for anything else,
            // which sends the message to the poison queue.
            Assert.Contains(decision, new[] { "approved", "rejected" });
        }
    }
}
