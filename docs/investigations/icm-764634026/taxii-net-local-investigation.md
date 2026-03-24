# ICM 764634026 — TAXII.NET Local Code Investigation (Definitive)

**IcM Portal:** [IcM#764634026](https://portal.microsofticm.com/imp/v5/incidents/details/764634026/home)  
**Investigator:** Aragorn (Operator)  
**Date:** 2025-07-22  
**Status:** COMPLETE — This supersedes all previous remote/ADO-based findings  

---

## 1. Methodology

This investigation used **LOCAL file access only** against the clone at `C:\dev\ti\SecEng-Augusta`. Every claim below cites an exact local file path and line number. No ADO code search was used. Cross-reference was made against `C:\dev\ti\Sentinel-Common` for shared library context.

**Files read in full:**
- `C:\dev\ti\SecEng-Augusta\src\TAXII.NET\TAXII.NET\Credentials\ClientCertCredential.cs` (68 lines)
- `C:\dev\ti\SecEng-Augusta\src\TAXII.NET\TAXII.NET\Credentials\BasicAuthCredential.cs` (83 lines)
- `C:\dev\ti\SecEng-Augusta\src\TAXII.NET\TAXII.NET\Credentials\ManagedIdentityTaxiiCredential.cs` (63 lines)
- `C:\dev\ti\SecEng-Augusta\src\TAXII.NET\TAXII.NET\Credentials\ITAXIICredential.cs` (21 lines)
- `C:\dev\ti\SecEng-Augusta\src\TAXII.NET\TAXII.NET\Clients\TAXIIRequestSender.cs` (157 lines)
- `C:\dev\ti\SecEng-Augusta\src\TAXII.NET\TAXII.NET\Clients\TAXIIClientFactory.cs` (121 lines)
- `C:\dev\ti\SecEng-Augusta\src\StixPipeline\TAXIIPublisherServices\TAXIIActor\TAXIIActor.cs` (lines 230-260, 900-955)
- `C:\dev\ti\SecEng-Augusta\src\Common\Common\Utilities\KeyVaultClient.cs` (291 lines)
- `C:\dev\ti\SecEng-Augusta\src\Common\Common\Utilities\CertificateStore.cs` (103 lines)
- `C:\dev\ti\SecEng-Augusta\src\Common\Common\Config\ServiceConfig.cs` (70 lines)
- `C:\dev\ti\SecEng-Augusta\src\Common\Common\Schemas\TAXIIScubaRoutingRule.cs` (94 lines)
- `C:\dev\ti\Sentinel-Common\src\Common\ServiceToServiceTokenProvider\Providers\CertStoreAadAppCertificateProvider.cs` (63 lines)

**Searches executed (all local `grep`):**
- `ClientCertCredential` across all .cs files — 7 matches in 2 files
- `new ClientCertCredential(` across all .cs files — **ZERO matches**
- `new ManagedIdentityTaxiiCredential(` across all .cs files — 1 match
- `new TAXIIRequestSender(` across all .cs files — 1 match
- `ClientCertificateCredential` (Azure SDK) in cert/thumbprint search — multiple matches
- `ServiceAppCertSubjectName` across all file types — 60+ matches
- `IsInternalTaxii` across all .cs files — 1 definition, multiple usages
- `ManagedIdentity|BasicAuth|Bearer|X509|SslClientAuth|ClientCertificate` across .cs — 36 files

---

## 2. Q1: ClientCertCredential Code Path

### 2a. The Class Definition

`ClientCertCredential` exists at:
- **File:** `src\TAXII.NET\TAXII.NET\Credentials\ClientCertCredential.cs:14`
- **Class:** `ClientCertCredential : ITAXIICredential, IDisposable`
- **Constructor (line 25):** Takes `X509Certificate2 cert`, stores it as `this.Credential`
- **Behavior (line 39):** `AttachCredentialAsync` is a no-op — the cert is attached at the `HttpClientHandler` level, not per-request

### 2b. Where TAXIIRequestSender Supports It

`TAXIIRequestSender` (file: `src\TAXII.NET\TAXII.NET\Clients\TAXIIRequestSender.cs`) has **three** constructors:

1. **General constructor (line 35):** `TAXIIRequestSender(ITAXIICredential credential = null)`
   - Lines 50-57: Checks `if (credential is ClientCertCredential)`, then adds cert to `handler.ClientCertificates` and sets `ClientCertificateOption.Manual`
   - This is the polymorphic path — works for any `ITAXIICredential` subtype

2. **BasicAuth constructor (line 69):** `TAXIIRequestSender(BasicAuthCredential credential)`
   - No client cert handling

3. **ClientCert constructor (line 87):** `TAXIIRequestSender(ClientCertCredential credential)`
   - Lines 97-98: Sets `ClientCertificateOption.Manual` and adds cert
   - This is a dedicated overload for client cert auth

### 2c. Who Constructs TAXIIRequestSender?

- **`TAXIIClient.cs:49`:** `RequestSender = new TAXIIRequestSender(credential)` — uses the general constructor with `ITAXIICredential`
- **`TAXIIClientFactory.cs:31`:** `new TAXIIClient(credential)` — passes the credential through

### 2d. Who Constructs the Credential? — THE CRITICAL FINDING

**`TAXIIActor.cs:911-935`** — method `TryInitializeTaxiiClient`:

```csharp
// Line 919-925: Internal Microsoft TAXII servers
if (status.ActorRule.Routing.IsInternalTaxii)
{
    string managedIdentityId = this.ConfigManager.ServiceConfig.MSOneTITaxiiManagedIdentityClientId;
    string scope = this.ConfigManager.MicrosoftInternalTaxiiServerConfig.Scope;
    this.TAXIICredential = new ManagedIdentityTaxiiCredential(managedIdentityId, scope);
}
// Line 927-934: External TAXII servers
else
{
    string secret = await this.TryGetKeyVaultSecretAsync(...);
    this.TAXIICredential = new BasicAuthCredential(secret);
}
```

**`IsInternalTaxii`** is defined at `src\Common\Common\Schemas\TAXIIScubaRoutingRule.cs:80` as a per-connector boolean, default `false`. It's a JSON property (`"isInternalTaxii"`) on the routing rule stored in Cosmos DB.

### 2e. Conclusion for Q1

> **`ClientCertCredential` is DEAD CODE in SecEng-Augusta.**

- The class exists in the TAXII.NET library
- `TAXIIRequestSender` has full support for it (lines 50-57, 87-101)
- **BUT: `new ClientCertCredential(` has ZERO instantiations** in the entire codebase — not in production code, not in tests, not anywhere
- The production credential selection in `TAXIIActor.cs` creates ONLY `ManagedIdentityTaxiiCredential` or `BasicAuthCredential`
- No other code path in the repo ever constructs a `ClientCertCredential`

---

## 3. Q2: Certificate Identity

### 3a. TAXII.NET ClientCertCredential — No Cert Used

Since `ClientCertCredential` is never instantiated, **no certificate is ever loaded for TAXII mTLS client authentication**. The question is moot for this code path.

### 3b. Azure SDK `ClientCertificateCredential` — dakotakvreader

There IS a different credential class in active use: `Azure.Identity.ClientCertificateCredential`. This is the **Azure SDK** class for AAD token acquisition using a certificate assertion. It appears in:

| Location | File:Line | Purpose |
|---|---|---|
| Key Vault access | `src\Common\Common\Utilities\KeyVaultClient.cs:63` | Authenticate to Azure Key Vault |
| TAXII publisher rule processing | `src\StixPipeline\TAXIIPublisherServices\TAXIIActorClient\AugustaRuleProcessor.cs:93` | Authenticate to Azure Sentinel API |
| ISG publisher rule processing | `src\StixPipeline\ISGPublisherServices\ISGActorClient\AugustaRuleProcessor.cs:85` | Authenticate to Azure Sentinel API |
| MS Feed real-time processing | `src\StixPipeline\MSFeedRealTimeService\AugustaRuleProcessor.cs:86` | Authenticate to Azure Sentinel API |
| MSTI connectors processing | `src\StixPipeline\MSFeedSnapshotServices\MSFeedActorClient\MstiConnectorsProcessor.cs:148` | Authenticate to Azure services |

**All of these** use the same pattern:
```csharp
ClientCertificateCredential cred = new ClientCertificateCredential(
    this.ConfigManager.ServiceConfig.ServiceTenantId,
    this.ConfigManager.ServiceConfig.ServiceApplicationId,
    CertificateStore.FindCertificate(this.ConfigManager.ServiceConfig.ServiceAppCertSubjectName),
    opt);
```

**Certificate subject name:** `dakotakvreader`
- Default value in all Service Fabric manifests: `DefaultValue="dakotakvreader"` (e.g., `TAXIIPublisherApplication\ApplicationPackageRoot\ApplicationManifest.xml:12`)
- Production value: injected via `__KVREADER_CERT_SUBJECT_NAME__` placeholder (e.g., `TAXIIPublisher.Parameters.json:48`)

### 3c. Is This the SR17-Flagged Cert?

- The `dakotakvreader` cert is used for AAD service identity token acquisition, NOT for mTLS client authentication
- `ClientCertificateCredential` (Azure SDK) presents the cert to Azure AD to prove app identity and get an OAuth token. The cert is used for JWT assertion signing, not TLS mutual authentication
- This pattern does NOT depend on the ClientAuth EKU in the certificate
- **No evidence found** linking `dakotakvreader` to the MSPKI G1 root CA or `MicrosoftXS2028` specifically
- `grep -r "MSPKI\|MicrosoftXS2028"` across all file types: **ZERO matches** in the local clone
- The cert MAY be issued under MSPKI (likely, given it's a Microsoft internal service), but its usage for AAD token acquisition is NOT blocked by the G2 EKU change

---

## 4. Q3: Final Reassessment

### Does SecEng-Augusta Require Client Auth Migration?

### **NO — The SR17 flag for TAXII mTLS client auth is a FALSE POSITIVE.**

**Evidence chain:**

1. **TAXII.NET `ClientCertCredential` is dead code.** The class exists in the library but is never instantiated anywhere in the codebase. Zero matches for `new ClientCertCredential(`. (`TAXIIActor.cs:919-935` — the only production credential creation site — constructs only `ManagedIdentityTaxiiCredential` or `BasicAuthCredential`.)

2. **Production TAXII auth uses Bearer tokens or Basic auth.**
   - Internal Microsoft TAXII: `ManagedIdentityTaxiiCredential` → Azure Managed Identity → Bearer token (`ManagedIdentityTaxiiCredential.cs:54-55`)
   - External TAXII servers: `BasicAuthCredential` → username/password from Key Vault secret (`TAXIIActor.cs:929-934`)

3. **No mTLS client cert authentication occurs in production TAXII flows.** The `handler.ClientCertificates` collection is only populated when a `ClientCertCredential` is passed to `TAXIIRequestSender`, which never happens.

### What About `ClientCertificateCredential` (Azure SDK)?

This is a **SEPARATE concern** from the SR17 TAXII mTLS flag:

- **What it is:** Azure Identity SDK credential used to authenticate as a service principal to Azure AD and get OAuth tokens
- **Where used:** Key Vault access, Sentinel API access (5 locations, see table above)
- **Cert:** `dakotakvreader` (subject name)
- **SR17 relevance:** The cert itself may be an MSPKI cert requiring G1→G2 migration, but the **Azure SDK handles token acquisition internally** and does NOT depend on ClientAuth EKU. The cert is used for JWT assertion signing, not mTLS handshakes.
- **Risk level:** LOW — Azure SDK `ClientCertificateCredential` is designed to work with G2 certs. Microsoft has documented this as a safe migration path.
- **Recommendation:** Verify the `dakotakvreader` cert issuer chain. If it's MSPKI G1, it should be migrated to G2 as part of the standard rollover, but this is a routine cert rotation — not a code change or auth pattern migration.

### Auth Mechanism Summary

| Auth Pattern | Used In Production? | Cert Dependent? | SR17 Blocker? |
|---|---|---|---|
| TAXII.NET `ClientCertCredential` (mTLS) | ❌ NO — dead code | Yes (ClientAuth EKU) | **N/A — not used** |
| `ManagedIdentityTaxiiCredential` (Bearer) | ✅ YES — internal TAXII | No | No |
| `BasicAuthCredential` (username/password) | ✅ YES — external TAXII | No | No |
| Azure SDK `ClientCertificateCredential` | ✅ YES — KV + API auth | Yes (JWT signing, NOT mTLS) | **No — G2 compatible** |

---

## 5. Corrections vs. Previous Investigation

### ❌ CORRECTION 1: "CRITICAL mTLS Client Auth Blocker" — WRONG

**Previous finding (2025-02-13):** "CRITICAL mTLS Client Auth Blocker: TAXIIRequestSender.cs (lines 55-56, 97-98) explicitly configures `ClientCertificateOption.Manual` and `handler.ClientCertificates.Add()`. When G2 certs lacking ClientAuth EKU are loaded here, TLS handshake will FAIL immediately."

**Correction:** The code at those lines IS real and DOES configure mTLS — but **it is never reached in production**. `ClientCertCredential` is never instantiated. The `if (credential is ClientCertCredential)` check at line 50 always evaluates to `false` because only `ManagedIdentityTaxiiCredential` or `BasicAuthCredential` are ever passed. The mTLS code path is dead.

### ❌ CORRECTION 2: "TAXII uses mTLS for threat intelligence sharing" — MISLEADING

**Previous finding:** "TAXII uses mTLS for threat intelligence sharing protocol security. TAXIIRequestSender loads cert from Windows cert store and presents it on every TLS handshake."

**Correction:** The TAXII.NET library *supports* mTLS as a capability, but SecEng-Augusta's production deployment does NOT use it. The library also supports BasicAuth and ManagedIdentity (Bearer tokens), and those are the patterns actually used. The claim that certs are loaded from Windows cert store for TAXII is technically possible but never exercised.

### ❌ CORRECTION 3: "Option A = Remove client cert code" — UNNECESSARY

**Previous finding:** "Option A (RECOMMENDED) = Remove client cert code + implement alternative auth (AAD managed identity or API keys) = 5-8 weeks dev + test"

**Correction:** There is no client cert code to remove from the production path. The alternative auth (Managed Identity for internal, BasicAuth for external) is **already implemented and deployed**. No code changes are needed for TAXII auth.

### Root Cause of Previous Error

The previous investigation relied on ADO code search, which returned grep-like line matches without call chain context. It correctly identified that `TAXIIRequestSender` *supports* mTLS client cert auth, but failed to trace whether anyone actually *calls* it with a `ClientCertCredential`. Reading the full `TAXIIActor.cs` credential creation logic (which is 64KB and wouldn't appear in ADO search snippets) reveals the dead code nature of the mTLS path.

---

## 6. Priority Assessment

- **Recommended priority:** P3 (low)
- **Rationale:** The SR17 flag for TAXII mTLS client auth is a false positive — no production code exercises the `ClientCertCredential` path. The `dakotakvreader` cert used for Azure SDK auth should be verified and rotated as part of standard MSPKI G1→G2 migration, but this is routine cert management, not a code change or architectural migration.
- **Relative priority:** This is the lowest priority among open ICM items. The actual TAXII auth patterns (Managed Identity, BasicAuth) are already modern and SR17-compliant.

### Recommended Actions

1. **Close the SR17 TAXII mTLS flag** as a false positive with this evidence
2. **Verify `dakotakvreader` cert issuer** — confirm whether it's MSPKI G1 and schedule standard rotation to G2 if so (routine, no code changes)
3. **Consider removing dead code** — `ClientCertCredential.cs` and the `ClientCertCredential`-specific constructor in `TAXIIRequestSender.cs` could be removed for code hygiene, but this is optional/low priority
