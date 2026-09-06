# claimsflow-portal

Operator console for ClaimsFlow. **ASP.NET Core 6.0 MVC**, hosted on Azure App Service.

> This repository is a demo baseline for GitHub Copilot modernization. Every
> deprecated package and dated pattern here is deliberate. Do not modernize it
> ahead of the demo.

## State of this application

- Targets `net6.0` — **out of support since 12 November 2024**
- App Service no longer offers a .NET 6 stack, so it is published **self-contained** and carries its own runtime
- Classic `Program.cs` + `Startup.cs` hosting, not minimal hosting
- `Microsoft.Azure.Cosmos.Table`, `Microsoft.Azure.Storage.Blob`, `Microsoft.Azure.Storage.Queue` — all deprecated
- `Microsoft.Azure.KeyVault` + `AzureServiceTokenProvider` — superseded by `Azure.Security.KeyVault.Secrets` + `Azure.Identity`
- `Newtonsoft.Json` on the wire, including `AddNewtonsoftJson()`
- `new HttpClient()` per request with `.Result`
- Storage account key in configuration rather than a managed identity

## What it does

| Route | Purpose |
|---|---|
| `GET /Claims/Index?partnerId=` | Lists claims from the `claims` table |
| `GET /Claims/Detail/{id}` | Reads one claim from the **claimsflow-functions** HTTP API |
| `GET /Claims/Document/{id}` | Downloads the original document from Blob storage |
| `POST /Claims/Decide` | Publishes an operator decision to the `fraud-decision` queue |

## Contracts owned jointly with claimsflow-functions

- Queue `fraud-decision` — JSON shape **and base64 encoding**. `CloudQueue.EncodeMessage`
  defaults to `true`; `Azure.Storage.Queues` v12 does not encode at all. Porting
  `FraudDecisionQueue` without carrying the encoding across poisons every decision
  message silently.
- Table `claims` — `PartitionKey` = partnerId, `RowKey` = claimId, property names
- `GET /api/partners/{partnerId}/claims/{claimId}` response JSON

`tests/ClaimsFlow.Portal.Tests/CrossRepoContractTests.cs` pins these.

## Build and test

```bash
dotnet build ClaimsFlow.Portal.sln -c Release
dotnet test  ClaimsFlow.Portal.sln
```

## Run locally

Requires [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite)
and the claimsflow-functions app running on `ClaimsApi:BaseUrl`.

```bash
dotnet run --project src/ClaimsFlow.Portal
```

## Deploy

Shared infrastructure must exist first (see the demo runbook), then:

```bash
az deployment group create -g am_copilot_demo -f infra/main.bicep \
  -p storageAccountName=<name> appInsightsName=<name> keyVaultName=<name> \
     claimsApiBaseUrl=https://<function-app>.azurewebsites.net skuProfile=dev
```

`skuProfile=dev` is B1 with no slots. `skuProfile=demo` is S1 with a staging slot.
