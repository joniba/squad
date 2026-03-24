# TAXII.NET Deep Investigation — ICM 764634026
**Investigator:** Aragorn (operator agent)  
**Date:** 2025-07-07  
**Purpose:** Evidence-based correction of prior ICM 764634026 investigation findings regarding SecEng-Augusta TAXII.NET mTLS blocking  
**Prior investigation:** `docs/investigations/icm-764634026/icm-764634026-investigation.md`

---

## 1. Investigation Summary

The prior investigation made three central claims about why Microsoft Sentinel's TAXII connector cannot perform mTLS:

1. `CertStoreAadAppCertificateProvider` loads certs from the Windows store and feeds them into `TAXIIRequestSender` in SecEng-Augusta
2. `TAXIIRequestSender.cs` (SecEng-Augusta) contains `ClientCertificateOption.Manual` at lines 55-56 and 97-98 as the mTLS blocking point
3. The production auth flow routes through `ClientCertCredential` → mTLS configuration

**All three claims are materially wrong.** This investigation documents what the code actually does, based on ADO searches and retrieved file contents.

### What Is Actually True

SecEng-Augusta's TAXII stack supports exactly two production credential paths — neither uses mTLS:

| Routing flag | Credential class | Auth mechanism |
|---|---|---|
| `IsInternalTaxii = true` | `ManagedIdentityTaxiiCredential` | Azure MI Bearer token (`Authorization: Bearer <token>`) |
| `IsInternalTaxii = false` | `BasicAuthCredential` | HTTP Basic Auth (password from Key Vault) |

`ClientCertCredential` exists as a class and the `TAXIIRequestSender` constructor handles it — but no production code path in `TAXIIActor.cs` ever creates one. It is unreachable dead code.

---

## 2. Evidence Chain Analysis

### Q1: Does `CertStoreAadAppCertificateProvider` exist in SecEng-Augusta?

**Answer: No.**

ADO search for `CertStoreAadAppCertificateProvider` across all repos returns exactly **one result**: `Sentinel-Common` repo at `/src/Common/ServiceToServiceTokenProvider/Providers/CertStoreAadAppCertificateProvider.cs`.

Zero matches in SecEng-Augusta. The prior investigation's attribution was wrong.

The `Sentinel-Common` class loads certs from the Windows certificate store by subject name and provides them to service-to-service token providers — it is entirely unrelated to the TAXII connector stack.

**Search used:** `ado-search_code` with term `CertStoreAadAppCertificateProvider`  
**Result count:** 1 result, repo: `Sentinel-Common`, path: `/src/Common/ServiceToServiceTokenProvider/Providers/CertStoreAadAppCertificateProvider.cs`

---

### Q2: Does SecEng-Augusta's `TAXIIRequestSender.cs` contain `ClientCertificateOption.Manual`?

**Answer: No.**

SecEng-Augusta's `TAXIIRequestSender.cs` (path: `/src/TAXII.NET/TAXII.NET/Clients/TAXIIRequestSender.cs`, objectId: `b916297c4456542d2487bf2a6a199bc95844e14c`) uses **`AntiSSRFPolicy`/`AntiSSRFHandler`** with `SslClientAuthenticationOptions`, not `HttpClientHandler` with `ClientCertificateOption.Manual`.

Relevant Augusta code (confirmed by file content retrieval):

```csharp
var antiSSRFPolicy = new AntiSSRFPolicy(); // https://aka.ms/antissrf
antiSSRFPolicy.SetDefaults();
var antiSSRFHandler = antiSSRFPolicy.GetHandler();

antiSSRFHandler.AllowAutoRedirect = false;
antiSSRFHandler.UseProxy = false;
antiSSRFHandler.SslOptions ??= new SslClientAuthenticationOptions();
antiSSRFHandler.SslOptions.RemoteCertificateValidationCallback = (message, cert, chain, policyErrors) => true;

if (credential is ClientCertCredential)
{
    ClientCertCredential certCred = credential as ClientCertCredential;
    if (certCred != null)
    {
        antiSSRFHandler.SslOptions.ClientCertificates = new X509CertificateCollection() { certCred.Credential };
    }
}
```

The code with `ClientCertificateOption.Manual` is in **SecEng-Interflow** (`/src/TAXII.NET/Clients/TAXIIRequestSender.cs`, repo ID: `e13eb580-fee6-4cf2-b5c7-4a58c760eb11`), which is a different repository. The prior investigation conflated these two files.

**Search used:** `ado-search_code` with term `TAXIIRequestSender ClientCertificateOption`  
**Result:** Only found in `SecEng-Interflow`; Augusta file content retrieved by objectId shows `AntiSSRFHandler` usage instead

---

### Q3: Is `ClientCertCredential` instantiated in any production code path in SecEng-Augusta?

**Answer: No.**

`TAXIIActor.cs` (`/src/StixPipeline/TAXIIPublisherServices/TAXIIActor/TAXIIActor.cs`) contains the entire credential selection logic in `TryInitializeTaxiiClient`. The complete method (confirmed by file content retrieval):

```csharp
private async Task<bool> TryInitializeTaxiiClient(
    List<TAXII.NET.Models.Error> errorsEncountered, TaxiiClientStatus status)
{
    if (this.TAXIICredential == null)
    {
        // ... compute secret name hash ...

        if (status.ActorRule.Routing.IsInternalTaxii)
        {
            string managedIdentityId = this.ConfigManager.ServiceConfig.MSOneTITaxiiManagedIdentityClientId;
            string scope = this.ConfigManager.MicrosoftInternalTaxiiServerConfig.Scope;

            this.Logger.LogInfo(/* ... */ $"isMicrosoftTaxiiServer, using ManagedIdentityTaxiiCredential");
            this.TAXIICredential = new ManagedIdentityTaxiiCredential(managedIdentityId, scope);
        }
        else
        {
            string secret = await this.TryGetKeyVaultSecretAsync(
                keyVaultUrl: this.ConfigManager.RegionalKeyVaultConfig.KeyVaultUrl,
                secretName: newSecretName).ConfigureAwait(false);

            this.Logger.LogInfo(/* ... */ $"using BasicAuthCredential for authentication");
            this.TAXIICredential = new BasicAuthCredential(secret);
        }
    }

    if (this.TAXIIClient == null)
    {
        (TAXII.NET.Models.Error error, this.TAXIIClient) =
            await TAXIIClientFactory.TryCreateTAXIIClientAsync(
                status.ActorRule.Routing.TaxiiApiRoot,
                status.ActorRule.Routing.TaxiiCollectionId,
                this.TAXIICredential);
        // ...
    }

    return true;
}
```

`ClientCertCredential` is not mentioned. There is no third branch. The only entry points to the TAXII credential are `ManagedIdentityTaxiiCredential` and `BasicAuthCredential`.

ADO search for `new ClientCertCredential` across all repos finds it only in class definition files (`ClientCertCredential.cs`) and test code — not in any production caller within SecEng-Augusta.

---

### Q4: Does SecEng-Augusta use MSPKI, MicrosoftXS2028, or any specific cert thumbprints for TAXII auth?

**Answer: No evidence found.**

ADO search for `MSPKI MicrosoftXS2028 G2 certificate TAXII` returns **zero results**.

The only cert-adjacent usage in `TAXIIActor.cs` is the `KeyVaultReader` initialization:

```csharp
this.KeyVaultReader = new KeyVaultReader(
    this.Logger,
    this.ConfigManager.ServiceConfig.ServiceApplicationId,
    this.ConfigManager.ServiceConfig.ServiceAppCertSubjectName,  // ← service identity cert
    this.ConfigManager.ServiceConfig.ServiceTenantId);
```

This cert is used to authenticate the *service itself* to Key Vault (to retrieve the TAXII password secret). It is not used for TAXII authentication. TAXII auth uses whichever credential `TryInitializeTaxiiClient` creates.

---

## 3. Corrections to Previous Investigation

| # | Prior claim | Correction | Evidence |
|---|---|---|---|
| 1 | "`CertStoreAadAppCertificateProvider` is part of the SecEng-Augusta cert flow into TAXIIRequestSender" | `CertStoreAadAppCertificateProvider` exists only in `Sentinel-Common`, not SecEng-Augusta. No production code path connects it to TAXII auth. | ADO search: 1 result, Sentinel-Common only |
| 2 | "`TAXIIRequestSender.cs` in SecEng-Augusta (lines 55-56, 97-98) contains `ClientCertificateOption.Manual`" | Augusta's `TAXIIRequestSender.cs` uses `AntiSSRFHandler` with `SslOptions.ClientCertificates`. The `ClientCertificateOption.Manual` code is in SecEng-Interflow's version of the file. | File content retrieved by objectId `b916297c...` |
| 3 | "The production auth flow routes through `ClientCertCredential`" | `TAXIIActor.TryInitializeTaxiiClient` has two branches only: `ManagedIdentityTaxiiCredential` (internal) and `BasicAuthCredential` (external). `ClientCertCredential` has no production caller. | `TAXIIActor.cs` content retrieved |
| 4 | "mTLS is the current auth mechanism and the blocker" | Neither production credential type uses mTLS. MI uses Bearer tokens; Basic Auth uses password. mTLS is not part of the production flow. | `ManagedIdentityTaxiiCredential.cs` and `TAXIIActor.cs` content |
| 5 | "TAXII.NET is specific to SecEng-Augusta" | TAXII.NET is a shared library that exists in both SecEng-Augusta and SecEng-Interflow (with slightly different implementations). The prior investigation cited Interflow code paths as Augusta code paths. | Dual repo hits on multiple searches |

---

## 4. Actual Auth Architecture (Evidence-Based)

```
TAXIIActor.TryInitializeTaxiiClient()
│
├─ IsInternalTaxii == true?
│   └─ YES → ManagedIdentityTaxiiCredential
│               Uses Azure SDK ManagedIdentityCredential
│               Acquires OAuth2 Bearer token for configured scope
│               Attaches: Authorization: Bearer <access_token>
│               NOT mTLS
│
└─ IsInternalTaxii == false?
    └─ YES → BasicAuthCredential
                Retrieves password from Key Vault (KeyVaultReader)
                Secret name: SHA256 hash of workspace+collection path
                Attaches: Authorization: Basic base64(secret)
                NOT mTLS

TAXIIClientFactory.TryCreateTAXIIClientAsync(apiRootUrl, collectionId, credential)
│   Passes credential through unchanged
│   Detects TAXII version (2.0 or 2.1) from API root response
└─ Returns TAXII20Client or TAXII21Client

TAXIIRequestSender(ITAXIICredential credential)
│   Uses AntiSSRFPolicy/AntiSSRFHandler (SSRF protection)
│   if (credential is ClientCertCredential):
│       Sets SslOptions.ClientCertificates      ← NEVER REACHED in production
│   Calls credential.AttachCredentialAsync(request) per HTTP call
└─ Returns HttpResponseMessage
```

### `ClientCertCredential` Status

The class exists in SecEng-Augusta at `/src/TAXII.NET/TAXII.NET/Credentials/ClientCertCredential.cs` and wraps an `X509Certificate2`. The `TAXIIRequestSender` handles it via `SslOptions.ClientCertificates`. But no production code (no configuration, no factory, no actor) ever creates a `ClientCertCredential` instance. It appears to be preparatory/dead code.

---

## 5. Verification Walkthrough

To independently verify these findings, an engineer can run the following ADO code searches:

### Step 1 — Confirm `CertStoreAadAppCertificateProvider` is NOT in SecEng-Augusta

```
ADO Code Search: CertStoreAadAppCertificateProvider
Expected result: 1 result in Sentinel-Common, 0 in SecEng-Augusta
```

### Step 2 — Confirm `TAXIIRequestSender` mTLS impl difference between repos

```
ADO Code Search: ClientCertificateOption TAXIIRequestSender
Expected: results only in SecEng-Interflow

ADO Code Search: AntiSSRFHandler TAXIIRequestSender
Expected: results in SecEng-Augusta (uses AntiSSRFPolicy instead)
```

### Step 3 — Confirm production credential selection in TAXIIActor

```
ADO Code Search: TryInitializeTaxiiClient managedIdentityId
Expected: TAXIIActor.cs in SecEng-Augusta
Look for: two branches only (ManagedIdentityTaxiiCredential, BasicAuthCredential)
Confirm: no ClientCertCredential branch
```

### Step 4 — Confirm `ClientCertCredential` has no production callers

```
ADO Code Search: new ClientCertCredential
Expected: only class definition files and test code — no production callers
```

### Step 5 — Confirm `ManagedIdentityTaxiiCredential` uses Bearer tokens

```
ADO Code Search: ManagedIdentityTaxiiCredential AttachCredential Bearer
Look at: ManagedIdentityTaxiiCredential.cs in SecEng-Augusta
Confirm: Authorization: Bearer header, no cert configuration
```

---

## Unanswered Questions (Honest Gaps)

1. **Why does `ClientCertCredential` exist?** It may be intended for future use, customer-facing cert auth scenarios, or was used historically and removed from the caller side without removing the credential class.

2. **Can SecEng-Augusta be configured to use `ClientCertCredential`?** Not through `TAXIIActor.TryInitializeTaxiiClient` as written. There is no config switch or `IsInternalTaxii`-equivalent that routes to it. Adding such a path would require code changes.

3. **Is the ICM issue (764634026) about mTLS at all?** The prior investigation assumed mTLS was the auth mechanism but the code shows it is not. The actual ICM issue may be about something else entirely — this investigation only establishes what the TAXII auth code does, not what caused the original incident.
