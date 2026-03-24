# mTLS Validation Check: Amba.SituationalAwarenessData

**Parent Investigation**: [ICM 764634026 — MSPKI G1→G2 Root CA Migration](./icm-764634026-investigation.md)  
**Validation Guide Used**: [mTLS Validation Guide](./mtls-validation-guide.md)  
**Executed By**: Aragorn (Operator)  
**Date**: 2025-02-14  
**Repository**: `C:\dev\ti\Amba.SituationalAwarenessData`

---

## Preliminary Assessment

### 🟡 INCONCLUSIVE — No mTLS at the TLS layer, but certificate-based AAD token acquisition is actively used in production via `AddNamedCertificateAccessTokenProvider`. Geneva monitoring uses separate MA-managed certs. Need human verification of whether the PFX cert is G1 or G2.

---

## What Aragorn Found

### Section 2: Geneva Account Configuration

**Geneva/MDM accounts identified** (from Helm values + appsettings + ConfigGen):

| Environment | MDM Account | MDSD Account | Namespace |
|---|---|---|---|
| **Production** | `RomeAmbaProd` | `RomeAmbaProd` | `SituationalAwarenessData` |
| **Staging** | `RomeAmbaDev` | `RomeAmbaDev` | `SituationalAwarenessData` |
| **Canary** | `RomeAmbaStage` | `RomeAmbaStage` | `SituationalAwarenessData` |
| **Local** | `RomeAmbaDev` | `RomeAmbaDev` | `SituationalAwarenessData` |
| **Gov clouds** | `RomeAmbaUSGov` | `RomeAmbaUSGov` | `SituationalAwarenessData` |

**Source files** (representative):
- `ConfigGen/.../UsxProductionConstantProvider.cs:15` — `(CommonConstants.MonitoringAccount, "RomeAmbaProd")`
- `ConfigGen/.../UsxProductionConstantProvider.cs:23` — `(UsxCommonConstants.TelemetryGenevaMdsdMonitoringAccount, "RomeAmbaProd")`
- `appsettings.Production.eus2.json:17` — `"GenevaMdmMonitoringAccount": "RomeAmbaProd"`
- All 20+ production regional configs confirm same pattern

**Geneva certificate for MA/MDSD** (from Helm values and ConfigGen):
- MDM cert: `pfx-Geneva-PRD-{REGION}::mdm-cert.pem:mdm-key.pem` (from `helm-prd-eus2-values.yaml:103`)
- MDSD cert: `pfx-Geneva-PRD-{REGION}::gcscert.pem:gcskey.pem` (from `helm-prd-eus2-values.yaml:122`)
- ConfigGen constant: `MdsCertificateSecretName = "GenevaCertificate::gcscert.pem:gcskey.pem"` (from `UsxProductionConstantProvider.cs:18`)

**Key insight**: Geneva monitoring certs are deployed as Kubernetes secrets and consumed by the MA/MDSD sidecar containers — **not by the application code**. The application publishes metrics/logs to the MA via localhost (confirmed by `useUnixDomainSocket: true` in helm values).

### Section 3: Certificate Chain Analysis

**Certificate inventory** — two distinct certificate systems found:

#### System 1: Geneva Monitoring Certificates (MA-Managed) — LOW RISK

| Secret Name | Used By | Risk |
|---|---|---|
| `pfx-Geneva-PRD-{REGION}::mdm-cert.pem:mdm-key.pem` | MDM sidecar | LOW — MA handles |
| `pfx-Geneva-PRD-{REGION}::gcscert.pem:gcskey.pem` | MDSD sidecar | LOW — MA handles |
| `pfx-sad-ingress-ssl-prd` | Kubernetes ingress TLS | LOW — server cert, not client auth |

#### System 2: WDATP Service Certificate (Application-Level) — NEEDS REVIEW

| Secret Name | Key Vault | Used By | Risk |
|---|---|---|---|
| `pfx-WDATPService-PRD` | `sad-infra-prd-{region}.vault.azure.net` | `MdatpFirstPartyTokenProvider` | **MEDIUM** |
| `pfx-WDATPService-STG` | `sad-infra-stg-{region}.vault.azure.net` | Same (staging) | **MEDIUM** |

**How the WDATP cert is used** (traced through code):

1. **Registration** (`ServiceCollectionExtensions.cs:134`):
   ```csharp
   services.AddNamedCertificateAccessTokenProvider(
       configuration, 
       ServiceConstants.MdatpFirstPartyTokenProvider, 
       ServiceConstants.MdatpFirstPartyTokenProvider);
   ```

2. **Configuration** (all appsettings, e.g. `appsettings.Production.eus2.json:5`):
   ```json
   "MdatpFirstPartyTokenProvider--CertificateSecret": "pfx-WDATPService-PRD"
   ```

3. **Consumption** (`WdatpAuthenticationProvider.cs:38`):
   ```csharp
   _tenantAccessTokenProvider = accessTokenProviderFactory
       .GetNamed<ITenantAccessTokenProvider>(ServiceConstants.MdatpFirstPartyTokenProvider);
   ```

4. **Pattern**: This is a **certificate-based AAD token provider** — the PFX certificate is used to authenticate as a service principal with AAD, which returns an OAuth token. The cert is used at the **application layer** (AAD auth), NOT at the **TLS layer** (no mTLS).

**Critical mTLS pattern search** — ZERO matches:
```
✅ ClientCertificateOption.Manual     — NOT FOUND in entire codebase
✅ ClientCertificates.Add             — NOT FOUND
✅ handler.ClientCertificates         — NOT FOUND
✅ SslStream                          — NOT FOUND
✅ RemoteCertificateValidationCallback — NOT FOUND
```

**Token/identity auth patterns found** (confirming non-mTLS):

| File | Pattern | Purpose |
|---|---|---|
| `Program.cs:40` | `DefaultAzureCredential` | Key Vault access, management plane |
| `ServiceCollectionExtensions.cs:131` | `AddNamedWorkloadIdentityCollectionAccessTokenProviders` | Workload Identity (modern, cert-free) |
| `ServiceCollectionExtensions.cs:134` | `AddNamedCertificateAccessTokenProvider` | **Cert-based AAD token** (NOT mTLS) |
| `ServiceCollectionExtensions.cs:142` | `.AddAzureAdBearer()` | Inbound JWT Bearer validation |
| `DashboardQueryTranslator.cs` | `Bearer` token in dynamic auth | Token passthrough |

**Deployment model**: Kubernetes (AKS) with Helm charts — no legacy Cloud Service configs (.cscfg/.csdef).

### Section 5: Red Flag Evaluation

| Red Flag | Status | Evidence |
|---|---|---|
| mTLS `ClientCertificateOption.Manual` | ✅ **CLEAR** | Zero matches in codebase |
| `ClientCertificates.Add` | ✅ **CLEAR** | Zero matches |
| `SslStream` / custom TLS | ✅ **CLEAR** | Zero matches |
| `RemoteCertificateValidationCallback` | ✅ **CLEAR** | Zero matches |
| Certificate used for AAD auth | ⚠️ **NEEDS REVIEW** | `AddNamedCertificateAccessTokenProvider` uses PFX cert to get AAD tokens |
| Geneva cert for MA | ✅ **LIKELY CLEAR** | MA sidecar manages Geneva certs; app talks to MA via Unix socket |
| Ingress TLS cert | ✅ **CLEAR** | Server cert only (`pfx-sad-ingress-ssl-prd`); not client auth |

---

## Detailed Analysis: Is the WDATP Certificate a G2 Blocker?

**The `AddNamedCertificateAccessTokenProvider` pattern:**

This pattern loads a PFX certificate from Key Vault and uses it to authenticate as a service principal with Azure AD. The flow is:

```
App → loads pfx-WDATPService-PRD from Key Vault
    → creates AAD client assertion using cert private key
    → sends assertion to https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
    → receives OAuth2 access token
    → uses token (Bearer header) for downstream API calls
```

**This is NOT mTLS.** The certificate is not presented at the TLS handshake layer. It's used to **sign a JWT assertion** that AAD validates. The TLS connection to AAD is standard server-authenticated TLS.

**However, there is a nuance**: AAD validates the certificate against the app registration. If the app registration has the G1 cert thumbprint/subject registered and the cert is rotated to G2, the **app registration must also be updated**. This is a configuration change, not a code change.

**G2 compatibility**: The Azure SDK `ClientCertificateCredential` and similar patterns typically work with G2 certs because they don't check for ClientAuth EKU — they use the cert for JWT signing, which only requires a private key. **The cert's EKU is irrelevant for this use case.**

---

## Preliminary Verdict

### 🟢 LIKELY SAFE for G2 Migration (with caveats)

**Reasoning:**

1. **Zero mTLS patterns in application code** — exhaustive search for TLS-layer client cert attachment patterns returned zero matches.

2. **Certificate usage is AAD-layer, not TLS-layer** — the `MdatpFirstPartyTokenProvider` uses the PFX cert to sign JWT assertions for AAD token acquisition. This is application-layer auth, not mTLS. G2 certs work fine for JWT signing because no EKU check is performed.

3. **Workload Identity is already partially deployed** — `AddNamedWorkloadIdentityCollectionAccessTokenProviders` is registered alongside the certificate provider (`ServiceCollectionExtensions.cs:131`). This is the modern, cert-free path.

4. **Geneva monitoring uses MA-managed certs** — the application connects to MA via Unix domain socket (`useUnixDomainSocket: true`). MA handles cert rotation independently.

5. **Ingress TLS is server-only** — `pfx-sad-ingress-ssl-prd` is a server certificate for HTTPS termination, not client auth.

**Caveats:**
- ⚠️ When rotating `pfx-WDATPService-PRD` to a G2 cert, the **AAD app registration** must be updated with the new cert thumbprint/subject
- ⚠️ The Geneva MA certs (`pfx-Geneva-PRD-{REGION}`) need to be G2-compatible — verify with the Geneva team that MA cert rotation is centrally managed

---

## Remaining Human Steps

These are the ONLY steps Aragorn could not execute. Copy-paste ready.

### Step 1: Verify the WDATP Certificate Is G2-Compatible

```powershell
# Check the current certificate in any production Key Vault:
az keyvault secret show \
  --vault-name "sad-infra-prd-eus2" \
  --name "pfx-WDATPService-PRD" \
  --query "{contentType: contentType, created: attributes.created, expires: attributes.expires}" \
  --output json

# If the secret is a PFX, download and inspect:
$secret = az keyvault secret show --vault-name "sad-infra-prd-eus2" --name "pfx-WDATPService-PRD" --query "value" -o tsv
$bytes = [Convert]::FromBase64String($secret)
$cert = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($bytes)
$cert | Format-List Subject, Issuer, NotAfter, EnhancedKeyUsageList

# Check issuer: 
#   Contains "Microsoft RSA Root Certificate Authority 2017" → G1 (pre-migration)
#   Contains "Microsoft Identity Verification Root" → G2 (already migrated)
```

### Step 2: Verify Geneva Account Auth Mode in Jarvis

```
1. Open https://jarvis-west.dc.ad.msft.net/
2. Navigate to: Manage → Account → search for "RomeAmbaProd"
3. Check the Authentication section:
   - If AuthenticationType = "Certificate" or "MutualTLS" → Verify MA handles this, not app code
   - If AuthenticationType = "AAD" or "Token" → SAFE (confirms MA uses token auth)
4. Also check: "RomeAmbaDev", "RomeAmbaStage"
```

### Step 3: Verify Geneva MA Certs Are G2-Compatible

```powershell
# Check the Geneva cert for any production region:
az keyvault secret show \
  --vault-name "sad-infra-prd-eus2" \
  --name "pfx-Geneva-PRD-EUS" \
  --query "{contentType: contentType, created: attributes.created, expires: attributes.expires}" \
  --output json

# If concerned about MA cert migration:
# Contact the Geneva/MA team to confirm that MA cert rotation to G2 is centrally managed
# The MA team typically handles this across all services — no per-service action needed
```

### Step 4: Verify AAD App Registration Has Correct Cert

```powershell
# Check the AAD app registration used by MdatpFirstPartyTokenProvider:
# The app client ID should be in the AadConfiguration section of appsettings
# Verify the cert thumbprint registered matches the current pfx-WDATPService-PRD cert

az ad app list --filter "displayName eq '<APP_NAME>'" --query "[].{appId: appId, keyCredentials: keyCredentials}" -o json
# Look for keyCredentials — ensure the cert thumbprint matches what's in Key Vault
```

### Step 5: Test Geneva Endpoint (Optional)

```powershell
# Test if the Geneva endpoint requires client cert:
try {
    Invoke-WebRequest -Uri "https://eastus2.prod.microsoftmetrics.com" -UseBasicParsing -ErrorAction Stop
    Write-Host "Endpoint does NOT require client cert" -ForegroundColor Yellow
} catch {
    if ($_.Exception.Message -match "SSL|TLS|certificate") {
        Write-Host "Endpoint REQUIRES client cert — verify MA handles this" -ForegroundColor Red
    } else {
        Write-Host "HTTP error but TLS succeeded — no mTLS required" -ForegroundColor Yellow
    }
}
```

---

## Decision After Human Steps

| If You Find... | Then... |
|---|---|
| `pfx-WDATPService-PRD` cert issuer is G1 AND you're doing central migration | ⚠️ **Update AAD app registration** after cert rotation to G2 — no code changes needed |
| `pfx-WDATPService-PRD` cert issuer is already G2 | ✅ **Already migrated** — close the NEEDS REVIEW flag |
| Geneva account `RomeAmbaProd` auth is cert-based AND MA handles it | ✅ **MA-managed** — verify with Geneva team that MA G2 rotation is covered |
| Geneva account auth is cert-based AND app code presents the cert | 🔴 **ESCALATE** — but this contradicts our code analysis (zero mTLS patterns, app uses Unix socket to MA) |
| AAD app registration has mismatched cert thumbprint after rotation | ⚠️ **Update app registration** with new G2 cert thumbprint |

**Bottom line**: Based on code analysis, this service does NOT use mTLS at the TLS layer. The certificate is used for AAD service principal authentication (JWT signing), which is G2-compatible. The 50 cert matches from the original investigation are explained by: (1) Geneva MA sidecar certs, (2) WDATP AAD auth certs, (3) ingress TLS certs — none of which are mTLS client auth blockers.

**Recommendation**: Update NEEDS REVIEW flag to **LIKELY SAFE** pending human verification of steps 1-2 above.
