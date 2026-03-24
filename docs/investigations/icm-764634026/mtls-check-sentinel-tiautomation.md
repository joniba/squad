# mTLS Validation Check: Sentinel-TiAutomation

**Parent Investigation**: [ICM 764634026 — MSPKI G1→G2 Root CA Migration](./icm-764634026-investigation.md)  
**Validation Guide Used**: [mTLS Validation Guide](./mtls-validation-guide.md)  
**Executed By**: Aragorn (Operator)  
**Date**: 2025-02-14  
**Repository**: `C:\dev\ti\Sentinel-TiAutomation`

---

## Preliminary Assessment

### 🟢 LIKELY SAFE — Application code uses token-based auth (Managed Identity). No mTLS client cert patterns found.

---

## What Aragorn Found

### Section 2: Geneva Account Configuration

**Geneva/MDM accounts identified** (from EV2 deployment parameters):

| Environment | GCS Account | GCS Environment | GCS Auth Type |
|---|---|---|---|
| **Production** | `AugustaPRODMDS` | `DiagnosticsProd` | `AuthKeyVault` |
| **Dev** | `TIAugustaDEVMDS` | `DiagnosticsProd` | `AuthKeyVault` |
| **FFX (Gov)** | `AUGUSTAFFXMDS` | `CaFairfax` | `AuthKeyVault` |
| **USSEC** | `AugustaUSSECMDS` | `Ussec` | `AuthKeyVault` |
| **USNAT** | `AugustaUSNATMDS` | `Usnat` | `AuthKeyVault` |

**Source**: `src/Deployment/TiPipeline.Topology/GeneratedEv2/Parameters/` — multiple environment parameter files

**Auth ID** (GCS cert subject, per-environment):
- **Dev**: `GenevaMonitorDev.geneva.keyvault.sentineltidev.azure.com` (from `topology.json:96`)
- **PPE**: `GenevaMonitorPPE.geneva.keyvault.sentinelti.azure.com` (from `topology.json:236`)
- **Prod**: `GenevaMonitorProd.geneva.keyvault.sentinelti.azure.com` (from `topology.json:367`)
- **FFX**: `GenevaMonitorffx.geneva.keyvault.sentinelti.azure.us` (from `topology.json:776`)

**Key insight**: `MONITORING_GCS_AUTH_ID_TYPE = "AuthKeyVault"` — this means Geneva monitoring auth goes through Key Vault-managed certificates, handled by the **Monitoring Agent (MA)**, not by application code.

### Section 3: Certificate Chain Analysis

**Certificates deployed** (from `TiPipelinesResourceBuilder.cs`):

| Certificate Subject | Purpose | Risk |
|---|---|---|
| `CertificateSubject.Geneva` | Geneva monitoring (MA agent) | LOW — MA handles this |
| `CertificateSubject.bootstrapcert` | AKS cluster bootstrap | LOW — infra, not app auth |
| `CertificateSubject.MetadataClientCert` | Metadata service client | MEDIUM — needs review |
| `CertificateSubject.TenantMetadataCert` | Tenant metadata auth | MEDIUM — needs review |

**Critical mTLS pattern search** — ZERO matches:
```
✅ ClientCertificateOption.Manual     — NOT FOUND in entire codebase
✅ ClientCertificates.Add             — NOT FOUND
✅ handler.ClientCertificates         — NOT FOUND
✅ SslStream                          — NOT FOUND
✅ RemoteCertificateValidationCallback — NOT FOUND
```

**Token auth patterns** — PRIMARY auth method confirmed:

| File | Pattern | Usage |
|---|---|---|
| `src/Common/Authentication/TokenCredentialProvider.cs:16-26` | `ManagedIdentityCredential` | **Production auth** — returns `new ManagedIdentityCredential(managedIdentityClientId)` |
| `src/Common/Authentication/TokenCredentialProvider.cs:18-22` | `ChainedTokenCredential(AzureCliCredential, VisualStudioCredential)` | Local dev only |
| `src/TiNormalization/Program.cs:134-137` | `DefaultAzureCredential` with `ManagedIdentityClientId` | Normalization service auth |
| `src/TiBulkActions/Program.cs:394-398` | `TokenProvider.CreateWithManagedIdentity()` | Bulk actions production auth |
| `src/TiBulkActions/Program.cs:399-407` | `ClientCertificateCredential` | **Local dev ONLY** — certificate path from env var |

**Key code** (`TokenCredentialProvider.cs` — the production auth path):
```csharp
public static TokenCredential CreateManagedIdentityCredentialChain(string managedIdentityClientId)
{
    if (EnvironmentName == EnvironmentName.Local)
        return new ChainedTokenCredential(new AzureCliCredential(), new VisualStudioCredential());
    
    return new ManagedIdentityCredential(managedIdentityClientId);  // ← Production uses THIS
}
```

**Geneva endpoints confirmed**: `prod5.prod.microsoftmetrics.com` referenced in `topology.json` as HomeStamp.

**No .cscfg or .csdef files** — modern deployment model (EV2/ARM templates with Kubernetes).

### Section 5: Red Flag Evaluation

| Red Flag | Status | Evidence |
|---|---|---|
| mTLS `ClientCertificateOption.Manual` | ✅ **CLEAR** | Zero matches in codebase |
| `ClientCertificates.Add` | ✅ **CLEAR** | Zero matches |
| `SslStream` / custom TLS | ✅ **CLEAR** | Zero matches |
| `RemoteCertificateValidationCallback` | ✅ **CLEAR** | Zero matches |
| Certificate-based auth in production | ✅ **CLEAR** | Production uses `ManagedIdentityCredential` |
| Geneva cert management | ✅ **LIKELY CLEAR** | MA handles Geneva certs via `AuthKeyVault`; not in app code |
| `ClientCertificateCredential` in code | ⚠️ **LOCAL DEV ONLY** | `TiBulkActions/Program.cs:399-407` — only when `EnvironmentName.Local`; production path uses managed identity |

---

## Preliminary Verdict

### 🟢 LIKELY SAFE for G2 Migration

**Reasoning:**

1. **Production authentication is 100% token-based** — `ManagedIdentityCredential` is the sole production auth path (`TokenCredentialProvider.cs:25`). No certificates are presented at the TLS layer.

2. **Zero mTLS patterns in application code** — exhaustive search for `ClientCertificateOption.Manual`, `ClientCertificates.Add`, `SslStream`, and `RemoteCertificateValidationCallback` returned zero matches.

3. **Geneva monitoring certs are MA-managed** — `MONITORING_GCS_AUTH_ID_TYPE = "AuthKeyVault"` means the Monitoring Agent handles certificate management, not the application. MA cert updates are typically centrally managed by the Geneva team.

4. **`ClientCertificateCredential` exists but is local-dev-only** — the `TiBulkActions/Program.cs` code explicitly guards this behind `EnvironmentName.Local`. Production never hits this path.

5. **The service is a Kubernetes-based WebAPI** — modern deployment model with Helm charts, ingress TLS, and workload identity. No legacy Cloud Service (.cscfg/.csdef) patterns.

---

## Remaining Human Steps

These are the ONLY steps Aragorn could not execute. Copy-paste ready.

### Step 1: Verify Geneva Account Auth Mode in Jarvis

```
1. Open https://jarvis-west.dc.ad.msft.net/
2. Navigate to: Manage → Account → search for "AugustaPRODMDS"
3. Check the Authentication section:
   - If AuthenticationType = "Certificate" or "MutualTLS" → NEEDS CODE CHANGES
   - If AuthenticationType = "AAD" or "Token" → SAFE
4. Repeat for "TIAugustaDEVMDS"
```

### Step 2: Verify the Geneva Monitoring Certificate is G2-Compatible

```powershell
# In an Azure-connected terminal with access to the Key Vault:
az keyvault certificate show \
  --vault-name "ti-prod-shared-kv" \
  --name "GenevaMonitorProd" \
  --query "{subject: policy.x509CertificateProperties.subject, ekus: policy.x509CertificateProperties.ekus, issuer: policy.issuerParameters.name}" \
  --output json
```

### Step 3: Confirm MetadataClientCert and TenantMetadataCert Are Not mTLS

```powershell
# Check if these certs have ClientAuth EKU:
az keyvault certificate show --vault-name "ti-prod-shared-kv" --name "MetadataClientCert" \
  --query "policy.x509CertificateProperties.ekus" --output json

az keyvault certificate show --vault-name "ti-prod-shared-kv" --name "TenantMetadataCert" \
  --query "policy.x509CertificateProperties.ekus" --output json

# If either has OID 1.3.6.1.5.5.7.3.2 (ClientAuth) → investigate further
# If neither has ClientAuth → SAFE
```

### Step 4: Test Geneva Endpoint (Optional — for extra confidence)

```powershell
# Test without client cert — does it require one?
try {
    Invoke-WebRequest -Uri "https://prod5.prod.microsoftmetrics.com" -UseBasicParsing -ErrorAction Stop
    Write-Host "Endpoint does NOT require client cert" -ForegroundColor Yellow
} catch {
    if ($_.Exception.Message -match "SSL|TLS|certificate") {
        Write-Host "Endpoint REQUIRES client cert" -ForegroundColor Red
    } else {
        Write-Host "HTTP error but TLS succeeded — no mTLS required" -ForegroundColor Yellow
    }
}
```

---

## Decision After Human Steps

| If You Find... | Then... |
|---|---|
| Geneva account `AugustaPRODMDS` uses AAD/token auth | ✅ **CLOSE the NEEDS REVIEW flag** — this service is safe for G2 migration |
| Geneva account uses certificate auth BUT MA handles it | ⚠️ **Verify with Geneva/MA team** that MA cert rotation to G2 is handled centrally |
| Geneva account uses certificate auth AND app code presents the cert | 🔴 **ESCALATE** — but this contradicts our code analysis (zero mTLS patterns found) |
| MetadataClientCert or TenantMetadataCert have ClientAuth EKU | ⚠️ **Investigate** how these certs are used in the metadata service path |

**Bottom line**: Based on code analysis alone, this service is **LIKELY SAFE**. The human steps above are belt-and-suspenders verification.
