# Sentinel-Synthetics: MSPKI G2 Certificate Compatibility Investigation

**Issue:** #162
**Investigator:** Aragorn (PA-Squad Operator)
**Date:** 2026-03-27
**Repo:** `Sentinel-Synthetics` (`C:\dev\ti\Sentinel-Synthetics`)
**Origin:** IcM #764634026 — MSPKI G2 migration risk assessment

---

## Executive Summary

**Verdict: ⚠️ REQUIRES TESTING — with HIGH risk for legacy cross-tenant auth paths**

Sentinel-Synthetics has **two distinct authentication architectures** living side by side:

| Auth Path | Mechanism | G2 Risk | Status |
|-----------|-----------|---------|--------|
| Legacy cross-tenant (Offboarding, Recommendations) | `ClientCertificateCredential` with Geneva-provisioned certs | **HIGH** | `[Obsolete]` but still active |
| Newer jobs (TICS, Watchlist, StixQueryApi) | `SyntheticsManagedIdentityTokenCredential` (MSI) | **NONE** | Current pattern |

MSPKI G2 certificates will **drop the Client Authentication EKU** (OID `1.3.6.1.5.5.7.3.2`), retaining only Server Authentication EKU. Entra ID requires ClientAuth EKU for `ClientCertificateCredential` authentication. The legacy paths will fail when Geneva provisions G2 certs unless the certificate template explicitly includes ClientAuth EKU.

---

## 1. Certificate Loading Flow Analysis

### 1.1 TokenCredentialCreator.CreateFromLocalCertificate() — LEGACY

**File:** `src/Common/Implementation/TokenCredentialCreator.cs` (lines 24–56)

```csharp
// Loads certificate from Windows certificate store by subject name
using X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
store.Open(OpenFlags.ReadOnly);
X509Certificate2Collection certs = store.Certificates.Find(
    X509FindType.FindBySubjectName, certificateSubjectName, validOnly: false);

// Picks the cert with the latest expiry
certificate = certs.Cast<X509Certificate2>().OrderBy(x => x.NotAfter).Last();

// Creates ClientCertificateCredential — sends to Entra ID for token
return new ClientCertificateCredential(
    tenantId, applicationId, certificate,
    new ClientCertificateCredentialOptions {
        AuthorityHost = new Uri(authorityHost),
        SendCertificateChain = true
    });
```

**Key observations:**
- `validOnly: false` — the code does **no local EKU validation** at all (line 37)
- `SendCertificateChain = true` — sends the full chain to Entra ID (line 55)
- Marked `[Obsolete]` (line 23) — migration to `SyntheticsApplicationTokenCredential` recommended
- The certificate is **Geneva-provisioned** and installed in the `CurrentUser\My` store by the Geneva Synthetics platform

**G2 Impact:** If Geneva provisions a G2 cert without ClientAuth EKU, this code will happily load it (no local checks), but **Entra ID will reject the token request** with `AADSTS700027`.

### 1.2 CrossTenantTokenCredentialCreator.Create() — LEGACY

**File:** `src/Common/Implementation/CrossTenantTokenCredentialCreator.cs` (lines 35–105)

This is a **two-hop authentication flow**:

```
Step 1: Geneva MSI cert (local store) → ClientCertificateCredential → Entra ID token
Step 2: Use token → Key Vault → retrieve dedicated app cert (PFX)
Step 3: Dedicated app cert → ClientCertificateCredential → Entra ID token
```

**Two separate `ClientCertificateCredential` instances** are created:
1. **Line 47:** `TokenCredentialCreator.CreateFromLocalCertificate(multiTenantAppId, ...)` — authenticates the Geneva multi-tenant app
2. **Line 94:** `new ClientCertificateCredential(dedicatedAppTenantId, dedicatedAppId, x509Certificate, ...)` — authenticates the dedicated app

**G2 Impact:** **Both** certificates must have ClientAuth EKU. If either one transitions to G2 without ClientAuth EKU, the entire flow breaks. This is the **highest risk path** because it has two cert-dependent auth points.

**Additional risk factor:** The dedicated app cert is stored in Key Vault as a PFX secret (line 72: `Convert.FromBase64String(secret.Value)`). The EKU profile of this cert depends on how it was issued — if it was issued by an MSPKI G1 CA, it likely has ClientAuth EKU today but will lose it upon renewal under G2.

### 1.3 RecommendationsPartnerClientProviderBase — HYBRID

**File:** `src/RecommendationsTestsSynthetics/Client/RecommendationsPartnerClientProviderBase.cs` (lines 27–57)

```csharp
// Step 1: MSI to access Key Vault — SAFE
CertificateClient keyVaultClient = new(vaultUri: parameters.KeyVaultUri,
    credential: new DefaultAzureCredential());

// Step 2: Retrieve cert from Key Vault, create ClientCertificateCredential — AT RISK
TokenCredential tokenCredential = new ClientCertificateCredential(
    tenantId: parameters.RecommendationsPartnerApiTenantId,
    clientId: parameters.ClientId,
    clientCertificate: x509Certificate,
    new ClientCertificateCredentialOptions() {
        SendCertificateChain = true,
        AuthorityHost = new Uri(parameters.AuthorityHost)
    });
```

**G2 Impact:** Step 1 uses MSI (safe). Step 2 uses `ClientCertificateCredential` with a Key Vault cert — at risk if that cert is renewed under G2.

### 1.4 Newer Jobs — SAFE

Jobs that have migrated to `Microsoft.Sentinel.Common.Synthetics` use MSI-based auth:

**File:** `src/TISynthetics/StixApiQuerySynthetics/Jobs/StixQueryApiSyntheticsJob.cs` (line 62)

```csharp
SyntheticsManagedIdentityTokenCredential credential =
    new SyntheticsManagedIdentityTokenCredential(
        authorityHost: new Uri(parameters.StixQueryApiAuthorityRoot).Host,
        logger: this.logger);
```

No certificate dependency. **G2 migration has zero impact on these jobs.**

---

## 2. Cross-Tenant Auth Analysis

The `CrossTenantTokenCredentialCreator` implements **multi-tenant authentication** using Geneva's multi-tenant app pattern:

- **Parameters reveal the pattern** (lines 22–25):
  - `multiTenantAppId` — Geneva Synthetic MSI app ID
  - `multiTenantAppTenantId` — home tenant of the multi-tenant app
  - `dedicatedAppTenantId` — target tenant where resources live

- **Callers:**
  - `OffboardingTestsSynthetics/Client/SettingsARMClient.cs` (line 61)
  - `OffboardingTestsSynthetics/Client/OnboardingStatesClient.cs` (line 58)
  - `RecommendationsTestsSynthetics/Client/RecommendationsSyntheticsClientBase.cs` (line 59)

- **Cross-tenant scenario confirmed:** The dedicated app authenticates in a **different tenant** than the Geneva MSI app. This is the highest-risk configuration because Entra ID applies stricter validation in cross-tenant scenarios.

---

## 3. EKU Validation Search Results

Searched the entire repo for explicit EKU handling:

| Pattern | Matches |
|---------|---------|
| `ClientAuth` | 0 (only in comments about mTLS) |
| `EnhancedKeyUsage` | 0 |
| `X509KeyUsageFlags` | 0 |
| `OidCollection` | 0 |
| `EKU` | 0 |

**Conclusion:** The codebase performs **zero EKU validation**. It relies entirely on Entra ID server-side validation. When Entra ID starts rejecting certs without ClientAuth EKU, there will be no client-side error handling or fallback — just a raw `AuthenticationFailedException`.

---

## 4. Azure SDK / Entra ID Behavior Research

### Does ClientCertificateCredential require ClientAuth EKU?

**Yes.** Entra ID validates the EKU of certificates presented for client authentication. Certificates without the Client Authentication EKU (OID `1.3.6.1.5.5.7.3.2`) will be rejected.

**Sources:**
- [Microsoft certificate credentials documentation](https://learn.microsoft.com/entra/identity-platform/certificate-credentials) — Entra ID verifies EKUs on presented certificates
- [Azure Identity authentication best practices](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/best-practices) — certificates must carry ClientAuth EKU
- Expected error: `AADSTS700027: The certificate used for authentication does not have the correct EKU`

### What changes with MSPKI G2?

**G2 certificates will only include Server Authentication EKU.** The Client Authentication EKU is being removed from public/managed TLS certificates industry-wide.

**Sources:**
- [Microsoft managed TLS changes](https://learn.microsoft.com/en-us/azure/security/fundamentals/managed-tls-changes) — G2 migration details
- [RSA Conference: Sunsetting ClientAuth EKU](https://www.rsaconference.com/library/blog/sunsetting-the-clientauth-eku-what-why-and-how-to-prepare-for-the-change) — industry timeline
- [SSLTrust: End of ClientAuth EKU](https://www.ssltrust.com/blog/end-of-client-authentication-eku-in-tls-certificates) — CA/Browser Forum requirements

### Cross-tenant vs single-tenant behavior?

No relaxation of EKU requirements for cross-tenant scenarios. If anything, cross-tenant auth is **more strictly validated**. The ClientAuth EKU requirement applies uniformly.

---

## 5. Risk Assessment by Component

| Component | Auth Method | Cert Source | G2 Risk | Priority |
|-----------|------------|-------------|---------|----------|
| `TokenCredentialCreator` | `ClientCertificateCredential` | Local store (Geneva) | 🔴 HIGH | P1 — migrate to MSI |
| `CrossTenantTokenCredentialCreator` | 2× `ClientCertificateCredential` | Local store + Key Vault | 🔴 HIGH | P1 — migrate to MSI |
| `RecommendationsPartnerClientProviderBase` | `DefaultAzureCredential` + `ClientCertificateCredential` | Key Vault | 🟡 MEDIUM | P2 — Key Vault cert renewal risk |
| `SettingsARMClient` (Offboarding) | via `CrossTenantTokenCredentialCreator` | Local + Key Vault | 🔴 HIGH | P1 |
| `OnboardingStatesClient` (Offboarding) | via `CrossTenantTokenCredentialCreator` | Local + Key Vault | 🔴 HIGH | P1 |
| `RecommendationsSyntheticsClientBase` | Both legacy paths | Local + Key Vault | 🔴 HIGH | P1 |
| `StixQueryApiSyntheticsJob` | `SyntheticsManagedIdentityTokenCredential` | MSI (no cert) | 🟢 SAFE | None |
| `SLOJob` (Watchlist) | `SyntheticJobBase` (MSI) | MSI (no cert) | 🟢 SAFE | None |
| TICS Jobs | `SyntheticJobBase` (MSI) | MSI (no cert) | 🟢 SAFE | None |

---

## 6. Verdict

### ⚠️ REQUIRES TESTING — with HIGH risk on legacy paths

**Why not "BLOCKS G2":** The legacy paths are marked `[Obsolete]` and a migration path exists (`SyntheticsApplicationTokenCredential` in `Microsoft.Sentinel.Common.Synthetics`). The newer jobs have already migrated and are G2-safe. The risk depends on the Geneva Synthetics platform's certificate provisioning timeline — if Geneva switches to G2 certs before these legacy paths are migrated, they will break.

**Why not "SAFE":** Three active callers (`SettingsARMClient`, `OnboardingStatesClient`, `RecommendationsSyntheticsClientBase`) still use the legacy `ClientCertificateCredential` paths. These will fail when certs are renewed under G2 without ClientAuth EKU.

### Recommended Actions

1. **[P0] Confirm Geneva cert EKU timeline** — Contact Geneva Synthetics team to determine when they plan to provision G2 certs for synthetic MSI apps. This sets the deadline.

2. **[P1] Accelerate migration of Offboarding + Recommendations synthetics** — Complete the migration to `SyntheticsApplicationTokenCredential` (MSI-based) that the `[Obsolete]` attributes already recommend. The shared library in `Sentinel-Common` repo already has this solved.

3. **[P1] Audit Key Vault certificates** — Check the EKU profile of dedicated app certs stored in Key Vault. If they were issued by MSPKI G1, they currently have ClientAuth EKU but will lose it on renewal.

4. **[P2] Test with G2 cert in non-prod** — Before any cert renewal, test the Offboarding and Recommendations synthetics with a cert that only has Server Authentication EKU to confirm the failure mode and validate the fix.

5. **[P3] Remove dead code** — After migration, delete `TokenCredentialCreator.cs` and `CrossTenantTokenCredentialCreator.cs` to prevent future confusion.

---

## Appendix: File Index

| File | Path | Role |
|------|------|------|
| TokenCredentialCreator.cs | `src/Common/Implementation/` | Legacy single-tenant cert auth |
| CrossTenantTokenCredentialCreator.cs | `src/Common/Implementation/` | Legacy cross-tenant cert auth (2-hop) |
| RecommendationsPartnerClientProviderBase.cs | `src/RecommendationsTestsSynthetics/Client/` | Hybrid MSI+cert auth |
| SettingsARMClient.cs | `src/OffboardingTestsSynthetics/Client/` | Caller of cross-tenant auth |
| OnboardingStatesClient.cs | `src/OffboardingTestsSynthetics/Client/` | Caller of cross-tenant auth |
| RecommendationsSyntheticsClientBase.cs | `src/RecommendationsTestsSynthetics/Client/` | Caller of both legacy paths |
| StixQueryApiSyntheticsJob.cs | `src/TISynthetics/StixApiQuerySynthetics/Jobs/` | MSI-based (safe) |
| SLOJob.cs | `src/WatchlistSynthetics/Jobs/` | MSI-based (safe) |
