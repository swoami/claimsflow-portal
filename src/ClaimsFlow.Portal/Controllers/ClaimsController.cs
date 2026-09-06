using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ClaimsFlow.Portal.Contracts;
using ClaimsFlow.Portal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ClaimsFlow.Portal.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly IClaimsTableGateway _claims;
        private readonly IDocumentStore _documents;
        private readonly IFraudDecisionQueue _decisions;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ClaimsController> _logger;

        public ClaimsController(
            IClaimsTableGateway claims,
            IDocumentStore documents,
            IFraudDecisionQueue decisions,
            IConfiguration configuration,
            ILogger<ClaimsController> logger)
        {
            _claims = claims;
            _documents = documents;
            _decisions = decisions;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string partnerId = "northwind")
        {
            ViewBag.PartnerId = partnerId;
            ViewBag.Partners = new List<string> { "northwind", "contoso", "fabrikam" };

            var rows = await _claims.ListByPartnerAsync(partnerId);
            return View(rows);
        }

        public IActionResult Detail(string partnerId, string id)
        {
            // Sync-over-async on a hand-rolled HttpClient. Both are on the list.
            var baseUrl = _configuration["ClaimsApi:BaseUrl"];
            var functionKey = _configuration["ClaimsApi:FunctionKey"];
            var url = $"{baseUrl}/api/partners/{partnerId}/claims/{id}?code={functionKey}";

            string json;
            try
            {
                var http = new HttpClient();
                json = http.GetStringAsync(url).Result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not read claim {ClaimId} from the claims API.", id);
                return View("ApiUnavailable", new ClaimView { ClaimId = id, PartnerId = partnerId });
            }

            var claim = JsonConvert.DeserializeObject<ClaimView>(json);
            return View(claim);
        }

        public async Task<IActionResult> Document(string partnerId, string id)
        {
            var row = await _claims.GetAsync(partnerId, id);
            if (row == null || string.IsNullOrWhiteSpace(row.DocumentPath))
            {
                return NotFound();
            }

            var stream = await _documents.OpenAsync(row.DocumentPath);
            if (stream == null)
            {
                return NotFound();
            }

            return File(stream, "application/json", $"{id}-source.json");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decide(string partnerId, string id, string decision)
        {
            if (decision != ClaimStatuses.Approved && decision != ClaimStatuses.Rejected)
            {
                return BadRequest($"Unsupported decision '{decision}'.");
            }

            var message = new FraudDecisionMessage
            {
                ClaimId = id,
                PartnerId = partnerId,
                Decision = decision,
                DecidedBy = User?.Identity?.Name ?? "operator",
                DecidedAt = DateTimeOffset.UtcNow
            };

            await _decisions.EnqueueAsync(message);

            _logger.LogInformation("Decision {Decision} queued for claim {ClaimId}.", decision, id);
            TempData["Message"] = $"Decision '{decision}' queued for claim {id}.";

            return RedirectToAction(nameof(Index), new { partnerId });
        }

        public IActionResult Error() => View();
    }
}
