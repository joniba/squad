# Sentinel-TiAutomation: MSPKI G2 Certificate Compatibility Investigation

**Investigator:** Aragorn (Operator)  
**Date:** 2026-03-27  
**Issue:** #161  
**Origin:** IcM #764634026 G2 migration risk assessment — Sentinel-TiAutomation rated Medium-risk  
**Verdict:** ✅ **SAFE for G2** (no production certificate dependency)

---

## Executive Summary

Sentinel-TiAutomation's `ClientCertificateCredential` usage is **local development only**. Production authentication uses **Managed Identity**, which is completely unaffected by the MSPKI G2 certificate migration. No certificate validation, EKU checking, or certificate pinning exists anywhere in the codebase. The original Medium-risk rating from IcM #764634026 is **downgraded to No Risk** for production.

---

## 1. Certificate Usage Analysis

### 1.1 The `ClientCertificateCredential` — Local Dev Only

The sole `ClientCertificateCredential` usage is in `src/TiBulkActions/Program.cs:394-407`. It sits inside a ternary that gates on environment:

```csharp
// src/TiBulkActions/Program.cs:394-407
TokenCredential tokenProvider = ThreatIntelligenceAutomationConfig.EnvironmentConfig.EnvironmentName != Sentinel.Common.Constants.EnvironmentName.Local ?
    TokenProvider.CreateWithManagedIdentity(                          // ← PRODUCTION PATH
        configProvider.AuthenticationAuthorityHostUri,
        identityClientId: ThreatIntelligenceAutomationConfig.TiConnectorServiceRwMIClientId,
        logger: logger) :
    new ClientCertificateCredential(                                  // ← LOCAL DEV ONLY
        tenantId: Environment.GetEnvironmentVariable("TenantId"),
        clientId: Environment.GetEnvironmentVariable("LocalDevAadClientId"),
        clientCertificatePath: Environment.GetEnvironmentVariable("LocalDevCertificatePath"),
        new ClientCertificateCredentialOptions
        {
            AuthorityHost = configProvider.AuthenticationAuthorityHostUri,
            SendCertificateChain = true,
        });
```

**Key observation:** The condition `!= EnvironmentName.Local` means:
- **All deployed environments** (INT, PPE, PROD, Canary, etc.) → `ManagedIdentityCredential`
- **Only local developer machines** → `ClientCertificateCredential`

> *Source: `src/TiBulkActions/Program.cs:394-407`*

### 1.2 Certificate Loading Method

The certificate is loaded via **file path** from the `LocalDevCertificatePath` environment variable (`Program.cs:402`). This is a developer's local `.pfx` or `.pem` file — not fetched from Key Vault, not loaded by thumbprint from Windows certificate store.

- No `X509Store`, `FindByThumbprint`, or `GetCertificate` calls exist in the codebase (confirmed by grep — zero matches).
- The `.gitignore` excludes `*.pfx` files (`C:\dev\ti\Sentinel-TiAutomation\.gitignore:249`).

### 1.3 Production Authentication Path

All production services use `ManagedIdentityCredential` exclusively:

| Service | Auth Method | Source |
|---------|-------------|--------|
| TiBulkActions (TICS client) | `TokenProvider.CreateWithManagedIdentity()` | `Program.cs:395` |
| TiBulkActions (EventHub) | `TokenCredentialProvider.CreateManagedIdentityCredentialChain()` | `Program.cs:128` |
| TiNormalization (EventHub) | `TokenCredentialProvider.CreateManagedIdentityCredentialChain()` | `TiNormalization/Program.cs:153` |
| TiAutomationApis | `TokenProviderUtils.GetAuthorizationHeaderProvider()` | `TiAutomationApis/Program.cs:113` |
| Common config (Cosmos, etc.) | `TokenCredentialProvider.CreateManagedIdentityCredentialChain()` | `ThreatIntelligenceAutomationConfig.cs:114,272` |

The `TokenCredentialProvider` class (`src/Common/Authentication/TokenCredentialProvider.cs:16-26`) confirms the pattern:

```csharp
// src/Common/Authentication/TokenCredentialProvider.cs:16-26
public static TokenCredential CreateManagedIdentityCredentialChain(string managedIdentityClientId)
{
    if (EnvironmentName == Local)
        return new ChainedTokenCredential(new AzureCliCredential(), new VisualStudioCredential());
    return new ManagedIdentityCredential(managedIdentityClientId);
}
```

Even the local fallback for most services uses `AzureCliCredential` / `VisualStudioCredential`, not certificates.

---

## 2. Certificate Validation Scan

### 2.1 No EKU or Certificate Property Checks

Searched the entire `Sentinel-TiAutomation` repository for:

| Pattern | Matches |
|---------|---------|
| `ClientAuth` | 0 |
| `EKU` | 0 |
| `EnhancedKeyUsage` | 0 |
| `X509KeyUsageFlags` | 0 |
| `CertificateValidation` | 0 |
| `OidCollection` | 0 |
| `FindByThumbprint` | 0 |
| `X509Store` | 0 |

**No code in this repo validates certificate properties, pins certificate roots, or checks EKUs.**

### 2.2 `SendCertificateChain = true`

The local dev credential sets `SendCertificateChain = true` (`Program.cs:406`). This sends the full certificate chain (x5c claim) to Entra ID during token acquisition. This is actually **beneficial** for G2 migration — it ensures Entra ID can validate the full chain regardless of intermediate CA changes.

> *Source: [Microsoft Docs — SendCertificateChain](https://learn.microsoft.com/en-us/dotnet/api/azure.identity.clientcertificatecredentialoptions.sendcertificatechain?view=azure-dotnet)*

---

## 3. Azure SDK G2 Compatibility Research

### 3.1 ClientCertificateCredential Does NOT Require ClientAuth EKU

`ClientCertificateCredential` uses the certificate to **sign JWT assertions** (private_key_jwt flow), not for mutual TLS. The Azure SDK:
- Does **not** check the certificate's EKU fields
- Only requires the private key to sign the JWT
- Azure AD (Entra ID) validates the signed JWT against the registered app's public key — it does not check EKUs

> *Source: [Microsoft — Certificate Credentials](https://learn.microsoft.com/en-us/entra/identity-platform/certificate-credentials)*  
> *Source: [Azure SDK — ClientCertificateCredential Samples](https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/identity/Azure.Identity/samples/ClientCertificateCredentialSamples.md)*

### 3.2 G2 Migration Impact on Azure SDK

The MSPKI G2 migration changes which **root CA** signs Entra ID's **server-side** TLS certificates. Impact:
- **ManagedIdentityCredential** — unaffected if OS trust store has DigiCert G2 root (standard on Azure VMs/containers)
- **ClientCertificateCredential** — the TLS connection to Entra ID must trust G2, but the **client certificate itself** is unrelated to the G2 migration
- **No certificate pinning** exists in Azure.Identity SDK — it uses the OS trust store

> *Source: [Azure Managed TLS PKI Changes](https://learn.microsoft.com/en-us/azure/security/fundamentals/managed-tls-changes)*  
> *Source: [Petri — Entra Moves to DigiCert G2](https://petri.com/microsoft-entra-digicert-g2-root-certificate/)*

---

## 4. Verdict

### ✅ SAFE for G2 — No Production Impact

| Risk Factor | Assessment | Evidence |
|-------------|-----------|----------|
| Production auth method | ✅ Managed Identity (no certs) | `Program.cs:394-395`, `TokenCredentialProvider.cs:25` |
| ClientCertificateCredential scope | ✅ Local dev only | `Program.cs:394` — gated on `EnvironmentName.Local` |
| Certificate EKU validation | ✅ None exists | Grep: 0 matches for EKU/ClientAuth/EnhancedKeyUsage |
| Certificate root pinning | ✅ None exists | Grep: 0 matches for thumbprint/store operations |
| Azure SDK compatibility | ✅ JWT signing only, no EKU check | Azure docs confirm no EKU requirement |
| SendCertificateChain | ✅ Beneficial for G2 | Sends x5c claim — aids chain validation |

### Risk Downgrade

- **Previous rating:** Medium-risk (from IcM #764634026 triage — flagged due to `ClientCertificateCredential` presence)
- **Updated rating:** **No Risk** for production
- **Local dev note:** Developers should ensure their local dev certificates are issued from a trusted CA, but this has zero production impact

### Recommended Actions

1. **No code changes required** — production is certificate-free (Managed Identity only)
2. **Verify Azure VM/container trust stores** include DigiCert Global Root G2 — this is standard but worth confirming in deployment pipeline
3. **Close IcM #764634026 finding** for Sentinel-TiAutomation as "No Action Required"

---

## Appendix: Files Examined

| File | Relevance |
|------|-----------|
| `src/TiBulkActions/Program.cs` | Contains sole `ClientCertificateCredential` usage (local dev only) |
| `src/Common/Authentication/TokenCredentialProvider.cs` | Production credential factory — ManagedIdentity only |
| `src/Common/Config/ThreatIntelligenceAutomationConfig.cs` | Credential configuration — ManagedIdentity chains |
| `src/TiAutomationApis/Program.cs` | API service auth — no certificate usage |
| `src/TiNormalization/Program.cs` | Normalization service auth — ManagedIdentity + DefaultAzureCredential |
| `.gitignore` | Confirms `.pfx` excluded from repo |
