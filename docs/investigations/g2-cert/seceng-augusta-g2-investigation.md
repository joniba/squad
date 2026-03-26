# SecEng-Augusta & Sentinel-Augusta: ClientCertificateCredential G2 Certificate Compatibility

**Investigator:** Aragorn (Operator)  
**Date:** 2026-03-27  
**Origin:** IcM #764634026 — G2 certificate migration impact assessment  
**Scope:** Medium-risk SDK pattern — `ClientCertificateCredential` usage for Azure resource authentication  
**Not in scope:** mTLS/TAXII server certificate validation (separate CRITICAL finding, tracked independently)

---

## Executive Summary

All `ClientCertificateCredential` usages across SecEng-Augusta and Sentinel-Augusta follow an **identical pattern**: load a certificate from the Windows Certificate Store, create a `ClientCertificateCredential` with `SendCertificateChain = true`, and use it to sign JWT client assertions for Azure AD token acquisition. **No EKU validation exists anywhere in application code.** The Azure SDK does not enforce ClientAuth EKU for JWT signing — it only requires the `DigitalSignature` key usage, which G2 certificates provide.

**Overall verdict: REQUIRES TESTING** — the code is architecturally compatible with G2 certificates, but production validation is needed because Azure AD behavior with G2 cert chains has not been confirmed in these specific sovereign cloud configurations.

---

## Repos Investigated

| Repo | Local Path | Default Branch |
|------|-----------|----------------|
| SecEng-Augusta | `C:\dev\ti\SecEng-Augusta` | `master` |
| Sentinel-Augusta | `C:\dev\ti\Sentinel-Augusta` | `master` |

---

## File-by-File Analysis

### 1. SecEng-Augusta: ISG AugustaRuleProcessor.cs

**File:** `src/StixPipeline/ISGPublisherServices/ISGActorClient/AugustaRuleProcessor.cs`  
**Lines 83–90**

```csharp
ClientCertificateCredentialOptions opt = new ClientCertificateCredentialOptions {
    SendCertificateChain = true,
    AuthorityHost = KeyVaultClient.GetAuthorityHost(this.ConfigManager.ServiceConfig.ServiceTenantId),
    AdditionallyAllowedTenants = { "*" }
};
ClientCertificateCredential cred = new ClientCertificateCredential(
    this.ConfigManager.ServiceConfig.ServiceTenantId,
    this.ConfigManager.ServiceConfig.ServiceApplicationId,
    CertificateStore.FindCertificate(this.ConfigManager.ServiceConfig.ServiceAppCertSubjectName),
    opt);
IAuthorizationHeaderProvider tokenProvider = new SentinelTokenProvider(cred, ...);
this.Clients = TicsClients.Create(configProvider, tokenProvider);
```

**Certificate loading:** `CertificateStore.FindCertificate()` searches `StoreName.My` in both `CurrentUser` and `LocalMachine` store locations, first by `FindBySubjectName`, then by `FindByThumbprint`. Returns first match. No EKU filtering. *(Source: `src/Common/Common/Utilities/CertificateStore.cs`, lines 24–53)*

**Azure resource accessed:** TI Connector Service (TICS) API — via `SentinelTokenProvider` which acquires Bearer tokens using `credential.GetTokenAsync()` with `{audience}/.default` scope. *(Source: `src/Common/Common/Utilities/SentinelTokenProvider.cs`, lines 77–83)*

**Auth flow:** Certificate → sign JWT assertion → Azure AD → Bearer token → TICS API (HTTP Bearer auth)

**Verdict: REQUIRES TESTING**  
- ✅ `SendCertificateChain = true` — G2 intermediate chain will be transmitted  
- ✅ No EKU checks in application code  
- ✅ Uses JWT client assertion signing (requires `DigitalSignature` key usage, not ClientAuth EKU)  
- ⚠️ Supports sovereign clouds (FFx, MNC, RX, EX) — G2 cert trust must be validated per cloud  
- ⚠️ `AdditionallyAllowedTenants = { "*" }` means cross-tenant auth is possible — test with G2 certs in multi-tenant scenarios

---

### 2. SecEng-Augusta: MstiConnectorsProcessor.cs

**File:** `src/StixPipeline/MSFeedSnapshotServices/MSFeedActorClient/MstiConnectorsProcessor.cs`  
**Lines 147–155**

```csharp
ClientCertificateCredentialOptions opt = new ClientCertificateCredentialOptions {
    SendCertificateChain = true,
    AuthorityHost = KeyVaultClient.GetAuthorityHost(this.configManager.ServiceConfig.ServiceTenantId),
    AdditionallyAllowedTenants = { "*" }
};
ClientCertificateCredential cred = new ClientCertificateCredential(
    this.configManager.ServiceConfig.ServiceTenantId,
    this.configManager.ServiceConfig.ServiceApplicationId,
    CertificateStore.FindCertificate(this.configManager.ServiceConfig.ServiceAppCertSubjectName),
    opt);
IAuthorizationHeaderProvider tokenProvider = new SentinelTokenProvider(cred, ...);
this.clients = TicsClients.Create(configProvider, tokenProvider);
```

**Pattern:** Identical to ISG AugustaRuleProcessor. Same `CertificateStore.FindCertificate()`, same `SentinelTokenProvider`, same TICS API target.

**Additional note:** This processor *also* uses `ManagedIdentityCredential` for a separate CosmosDB status store (line 118: `new ManagedIdentityCredential(this.configManager.ServiceConfig.ManagedIdentityClientId)`). The managed identity path is unaffected by G2 migration.

**Azure resource accessed:** TICS API (same as ISG processor)

**Verdict: REQUIRES TESTING** — identical rationale to ISG AugustaRuleProcessor.

---

### 3. SecEng-Augusta: TAXII AugustaRuleProcessor.cs

**File:** `src/StixPipeline/TAXIIPublisherServices/TAXIIActorClient/AugustaRuleProcessor.cs`  
**Lines 92–100**

```csharp
ClientCertificateCredentialOptions opt = new ClientCertificateCredentialOptions {
    SendCertificateChain = true,
    AuthorityHost = KeyVaultClient.GetAuthorityHost(this.ConfigManager.ServiceConfig.ServiceTenantId),
    AdditionallyAllowedTenants = { "*" }
};
ClientCertificateCredential cred = new ClientCertificateCredential(
    this.ConfigManager.ServiceConfig.ServiceTenantId,
    this.ConfigManager.ServiceConfig.ServiceApplicationId,
    CertificateStore.FindCertificate(this.ConfigManager.ServiceConfig.ServiceAppCertSubjectName),
    opt);
IAuthorizationHeaderProvider tokenProvider = new SentinelTokenProvider(cred, ...);
this.Clients = TicsClients.Create(configProvider, tokenProvider);
```

**Pattern:** Identical to ISG and MSFeed processors.

**⚠️ Important distinction:** This file is in the TAXII publisher service. The TAXII service has a **separate CRITICAL mTLS finding** (TAXIIRequestSender sends client certificates for mTLS connections to external TAXII servers). That mTLS issue is tracked independently. The `ClientCertificateCredential` usage here is for Azure AD authentication to the TICS API — a completely separate auth flow from the mTLS connection.

**Azure resource accessed:** TICS API (same Bearer token flow)

**Verdict: REQUIRES TESTING** — identical rationale. No interaction with the mTLS code path.

---

### 4. Sentinel-Augusta: KeyVaultReader.cs

**File:** `src/Common/Common/Utilities/KeyVaultReader.cs`  
**`GetCredentials()` method, lines ~167–195**

```csharp
var certCollection = CertificateStore.FindCertificateCollection(this.CertHint);
// Iterates through all matching certificates
foreach (var cert in certCollection)
{
    clientCertificateCredential = new ClientCertificateCredential(
        tenantId: this.TenantId,
        clientId: this.AppId,
        clientCertificate: cert,
        options: new ClientCertificateCredentialOptions()
        {
            AuthorityHost = new Uri(this.Authority),
            SendCertificateChain = true,
        });
}
```

**Certificate loading:** Uses `CertificateStore.FindCertificateCollection()` (Sentinel-Augusta version at `src/Common/Common/Utilities/CertificateStore.cs`) — same logic as SecEng-Augusta: searches `StoreName.My` by subject name and thumbprint across both store locations. No EKU filtering. Returns a collection and iterates until a working credential is found.

**Azure resource accessed:** Azure Key Vault — via `SecretClient` (line ~49: `this.SecretClient = new SecretClient(new Uri(keyVaultUri), this.GetCredentials())`). Used for reading, writing, and deleting secrets.

**Key difference from SecEng-Augusta pattern:**
- Does NOT set `AdditionallyAllowedTenants` — single-tenant only
- Default authority is `https://login.microsoftonline.com/common` (public cloud only)
- Iterates through ALL matching certs (not just first match) — more resilient during cert rotation

**Auth flow:** Certificate → sign JWT assertion → Azure AD → Bearer token → Key Vault API

**Verdict: REQUIRES TESTING**  
- ✅ `SendCertificateChain = true` — G2 chain transmitted  
- ✅ No EKU checks in code  
- ✅ JWT client assertion flow (same Azure SDK mechanism)  
- ✅ Cert iteration provides rotation resilience  
- ⚠️ No sovereign cloud support (hardcoded public cloud authority) — lower risk surface

---

## Bonus Finding: MSFeedRealTimeService AugustaRuleProcessor.cs

**File:** `src/StixPipeline/MSFeedRealTimeService/AugustaRuleProcessor.cs`  
**Lines 85–86** — identical `ClientCertificateCredential` pattern

Not in the original 4-file list but discovered during comprehensive grep. Same pattern, same `SentinelTokenProvider`, same TICS API target.

**Verdict: REQUIRES TESTING** — same rationale as the other SecEng-Augusta processors.

---

## Shared Infrastructure Analysis

### KeyVaultClient.cs (SecEng-Augusta)

**File:** `src/Common/Common/Utilities/KeyVaultClient.cs`  
**Lines 60–63**

All SecEng-Augusta `ClientCertificateCredential` instances share `KeyVaultClient.GetAuthorityHost()` for authority resolution. This method maps tenant IDs to sovereign cloud authority hosts (Fairfax, EagleX, SCloud, Mooncake). The `KeyVaultClient` class itself also creates a `ClientCertificateCredential` with `SendCertificateChain = true` for Key Vault access (secrets and certificates).

**Verdict: REQUIRES TESTING** — same pattern, adds sovereign cloud dimension.

### CertificateStore.cs (both repos)

Both repos have equivalent `CertificateStore` implementations. Neither performs any EKU, key usage, or issuer validation. They simply search the Windows cert store by subject name or thumbprint and return matches. **This is G2-neutral** — it will find G2 certificates just as readily as G1 certificates.

### SentinelTokenProvider.cs

Wraps any `TokenCredential` (including `ClientCertificateCredential`) to provide Bearer tokens for the TICS API. Calls `credential.GetTokenAsync()` with `{audience}/.default` scope. No certificate inspection occurs at this layer.

---

## Certificate Validation Search Results

### SecEng-Augusta

Searched for: `ClientAuth`, `EnhancedKeyUsage`, `EKU`, `X509KeyUsage`, `keyUsage`

**Results:** The only matches were in `src/STIX.NET/STIX.Net/Extensions/X509V3Extensions.cs` — a STIX data model class that *describes* certificate extensions as data fields (properties like `KeyUsage`, `ExtendedKeyUsage`). This is a **data model for STIX indicator objects**, not runtime certificate validation code. No application code validates EKU on certificates used for authentication.

### Sentinel-Augusta

**Results:** No matches. Zero certificate validation code.

---

## Azure SDK Behavior Research

### How `ClientCertificateCredential` Uses the Certificate

The Azure Identity SDK's `ClientCertificateCredential` uses the certificate to:
1. **Sign a JWT client assertion** — creates a JWT with claims (iss, sub, aud, exp) and signs it with the certificate's private key
2. **Send the signed assertion to Azure AD** — as part of the OAuth2 client credentials flow
3. **Receive an access token** — which is then used for API calls

The signing operation requires:
- **Private key** — must be available (RSA or ECDSA)
- **DigitalSignature key usage** — the certificate must support signing operations

### Does Azure AD Require ClientAuth EKU?

Per Microsoft documentation and community testing:
- Azure AD / Entra ID **does not enforce** the ClientAuth EKU (`1.3.6.1.5.5.7.3.2`) for JWT client assertion authentication
- The `DigitalSignature` key usage is **sufficient** for the signing operation
- `SendCertificateChain = true` (already set in all files) ensures G2 intermediate certificates are transmitted, which Azure AD needs to build the trust chain
- Including ClientAuth EKU is **recommended best practice** but not enforced

### G2 Certificate Specific Considerations

| Property | G1 (Current) | G2 (New) | Impact |
|----------|-------------|----------|--------|
| Root CA | DigiCert Baltimore Root | Microsoft RSA Root CA 2017 | Trust chain changes |
| Intermediate | Microsoft IT TLS CA | Microsoft Azure RSA TLS Issuing CA | `SendCertificateChain=true` handles this |
| Key Usage | DigitalSignature | DigitalSignature | ✅ No change |
| EKU | Varies | Varies | Not enforced by SDK |
| Key Size | RSA 2048+ | RSA 2048+ | ✅ No change |

---

## Summary Verdict Table

| File | Repo | Azure Resource | Verdict | Risk |
|------|------|---------------|---------|------|
| `ISGActorClient/AugustaRuleProcessor.cs` | SecEng-Augusta | TICS API | REQUIRES TESTING | Medium |
| `MSFeedActorClient/MstiConnectorsProcessor.cs` | SecEng-Augusta | TICS API | REQUIRES TESTING | Medium |
| `TAXIIActorClient/AugustaRuleProcessor.cs` | SecEng-Augusta | TICS API | REQUIRES TESTING | Medium |
| `KeyVaultReader.cs` | Sentinel-Augusta | Key Vault | REQUIRES TESTING | Medium |
| `MSFeedRealTimeService/AugustaRuleProcessor.cs`* | SecEng-Augusta | TICS API | REQUIRES TESTING | Medium |
| `KeyVaultClient.cs`* | SecEng-Augusta | Key Vault | REQUIRES TESTING | Medium |

*Bonus finds not in original scope

**No files are SAFE (untestable)** — all use certificate-based Azure AD auth that traverses a trust chain.  
**No files BLOCK** — no code-level incompatibility exists. The risk is entirely in the Azure AD trust chain validation of G2 certificates.

---

## Recommended Testing Plan

1. **Deploy a G2 certificate** to one PPE environment (e.g., `ppe` tenant)
2. **Register the G2 cert** with the service principal's App Registration in Azure AD
3. **Test each processor** — ISG, MSFeed Snapshot, MSFeed RealTime, TAXII — can they acquire tokens?
4. **Test KeyVaultReader** (Sentinel-Augusta) — can it read secrets from Key Vault with a G2 cert?
5. **Test sovereign clouds** (FFx, MNC) — these have separate trust stores and may not trust G2 roots yet

---

## Key Takeaway

The code is **architecturally sound** for G2 migration. All files already set `SendCertificateChain = true`, no code performs EKU validation, and the Azure SDK uses certificates only for JWT signing (which requires `DigitalSignature`, not ClientAuth EKU). The remaining risk is Azure AD's server-side trust chain validation of G2 certificates in each cloud environment — this must be validated through testing, not code review alone.
