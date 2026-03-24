# mTLS Validation Guide: Amba.SituationalAwarenessData Geneva Account

**Parent Investigation**: [ICM 764634026 — MSPKI G1→G2 Root CA Migration](./icm-764634026-investigation.md)  
**Finding Under Review**: `Amba.SituationalAwarenessData` — 50 cert matches, Geneva MDM + pfx references, flagged **NEEDS REVIEW**  
**Author**: Aragorn (Operator)  
**Date**: 2025-02-13  
**Purpose**: Step-by-step operational guide to validate whether the Geneva account/namespace used by Amba.SituationalAwarenessData is configured for mTLS authentication

---

## Why This Matters

In the ICM 764634026 investigation, we catalogued certificate usage across all 21 TI repositories. `Amba.SituationalAwarenessData` showed 50 certificate-related matches including **Geneva MDM certificates** and **pfx file references**. The investigation flagged this as NEEDS REVIEW because:

- If the Geneva account authenticates via **mTLS (certificate-based mutual TLS)**, the MSPKI G2 migration could break metric/log ingestion — G2 certs lack ClientAuth EKU
- If it uses **token-based auth (AAD/managed identity)**, the cert references are likely just for data-plane encryption and are NOT a migration blocker
- The distinction determines whether this service needs code changes before the April 10 / May 16 deadlines

---

## Section 1: What mTLS Means in the Geneva Context

### Geneva Authentication Models

Geneva (Azure's internal monitoring platform, also called MDM — Metrics and Diagnostics Monitoring) supports two primary authentication methods for services publishing metrics and logs:

| Auth Method | How It Works | Certificate Dependency | G2 Impact |
|---|---|---|---|
| **mTLS (Certificate Auth)** | Service presents an X.509 client certificate during TLS handshake. Geneva endpoint validates the cert's subject, issuer chain, and **Extended Key Usage (EKU)** — specifically the `ClientAuth` OID (`1.3.6.1.5.5.7.3.2`). Both sides verify each other's identity. | **Hard dependency** on cert having ClientAuth EKU | **BLOCKS G2** — G2 certs lack ClientAuth EKU; TLS handshake will fail |
| **Token Auth (AAD/Managed Identity)** | Service obtains an OAuth token from Azure Active Directory (using managed identity, service principal, or certificate-based AAD credential). Token is passed in HTTP headers. TLS is still used for transport encryption, but the server does NOT require the client to present a cert for identity. | No ClientAuth EKU dependency; cert may still be used for AAD token acquisition | **Compatible with G2** — cert is used for AAD auth, not TLS-layer client auth |

### The Critical Distinction

- **mTLS**: The certificate is presented **at the TLS layer** as part of the handshake. The server's TLS configuration requires client certs and validates the ClientAuth EKU. If the EKU is missing, the handshake fails before any HTTP traffic flows.
- **Token Auth**: The certificate may be used **at the application layer** to authenticate to AAD and get a token. TLS still encrypts the connection, but the server doesn't require a client cert. The cert's EKU doesn't matter for the TLS handshake itself.

### Geneva-Specific Terminology

- **Geneva Account**: The top-level container for monitoring data (e.g., metrics, logs). Each account has authentication settings that determine how publishing services authenticate.
- **Geneva Namespace**: A logical grouping within an account (e.g., `Amba.SituationalAwarenessData`). Authentication is typically configured at the account level, not per-namespace.
- **MDM (Metrics and Diagnostics Monitoring)**: The underlying platform; "Geneva" is the product name, "MDM" is the system name. Used interchangeably.
- **MA (Monitoring Agent)**: The Geneva agent running on VMs/containers that collects and publishes metrics. MA handles certificate management and auth to Geneva endpoints.

---

## Section 2: How to Check the Geneva Account Configuration

### Step 2.1: Find the Geneva Account Name

Before you can check the configuration, you need to identify which Geneva account `Amba.SituationalAwarenessData` publishes to.

**Where to look in the service code/config:**

1. **Search the repo for Geneva/MDM configuration files:**
   ```powershell
   # In the Amba.SituationalAwarenessData repo root
   Get-ChildItem -Recurse -Include "*.xml","*.json","*.config","*.cscfg","*.csdef","*.yaml","*.yml" |
     Select-String -Pattern "MdmAccount|GenevaAccount|MonitoringAccount|MetricAccount" |
     Select-Object Path, LineNumber, Line
   ```

2. **Search for the monitoring agent configuration:**
   ```powershell
   # MA (Monitoring Agent) config usually specifies the account
   Get-ChildItem -Recurse -Include "*.xml","*.json" |
     Select-String -Pattern "monitoringGCSAccount|GCS_ACCOUNT|MONITORING_GCS_ACCOUNT|gcsaccount" |
     Select-Object Path, LineNumber, Line
   ```

3. **Search for MDM metric namespace declarations:**
   ```powershell
   # The namespace often appears in code that publishes metrics
   Get-ChildItem -Recurse -Include "*.cs","*.json","*.xml" |
     Select-String -Pattern "SituationalAwareness" |
     Select-Object Path, LineNumber, Line
   ```

4. **Check ServiceConfiguration (.cscfg) or ARM templates:**
   ```powershell
   Get-ChildItem -Recurse -Include "*.cscfg","*.csdef","*.bicep","*.json" |
     Select-String -Pattern "Geneva|MDM|Monitoring" |
     Select-Object Path, LineNumber, Line
   ```

### Step 2.2: Check Account Settings in Jarvis / Geneva Portal

Once you have the account name:

1. **Open Jarvis (Geneva Portal):**
   - URL: `https://jarvis-west.dc.ad.msft.net/` (or your regional Jarvis endpoint)
   - Navigate to: **Manage** → **Account** → search for your account name

2. **Check the Account's Authentication Configuration:**
   - In the account settings, look for the **Authentication** or **Security** section
   - Key fields to inspect:

   | Field | mTLS Value | Token Auth Value |
   |---|---|---|
   | `AuthenticationType` or `AuthMode` | `Certificate` or `MutualTLS` | `AAD` or `Token` or `ManagedIdentity` |
   | `ClientCertificateRequired` | `true` | `false` or absent |
   | `AllowedCertificateThumbprints` | List of specific thumbprints | Empty or absent |
   | `AllowedCertificateSubjects` | CN patterns (e.g., `CN=*.microsoft.com`) | Empty or absent |
   | `AADAppId` or `AADResourceId` | Empty or absent | Populated with AAD app/resource IDs |

3. **Alternative: Check via Geneva Config Service (GCS) API:**
   ```powershell
   # If you have GCS access, query the account config directly
   # Replace <ACCOUNT_NAME> with the actual Geneva account name
   $account = "<ACCOUNT_NAME>"
   $gcsEndpoint = "https://gcs.prod.monitoring.core.windows.net"

   # This requires appropriate authentication (cert or token depending on GCS config)
   Invoke-RestMethod -Uri "$gcsEndpoint/accounts/$account/configuration" -Method GET
   ```

### Step 2.3: Check via ARM Resource (if Geneva is ARM-managed)

Some Geneva accounts are managed as ARM resources:

```powershell
# List Geneva/MDM account resources in the subscription
az resource list --resource-type "Microsoft.Monitor/accounts" --output table

# Or search more broadly
az resource list --query "[?contains(type, 'monitor') || contains(type, 'Monitor')]" --output table

# Get specific account details
az resource show --ids "<resource-id>" --output json
```

Look for `properties.authentication` or `properties.ingestion.authentication` in the ARM resource properties.

---

## Section 3: How to Check the Certificate Chain

### Step 3.1: Identify Which Certificate the Service Uses

**Find the certificate reference in deployment config:**

```powershell
# Search for thumbprint references (most common cert identifier in Azure services)
Get-ChildItem -Recurse -Include "*.cscfg","*.xml","*.json","*.yaml","*.yml","*.bicep" |
  Select-String -Pattern "thumbprint|Thumbprint|THUMBPRINT" |
  Select-Object Path, LineNumber, Line

# Search for pfx references (indicates cert files bundled with deployment)
Get-ChildItem -Recurse -Include "*.cs","*.xml","*.json","*.yaml","*.ps1" |
  Select-String -Pattern "\.pfx|\.cer|\.pem|PKCS|X509" |
  Select-Object Path, LineNumber, Line

# Search for Key Vault certificate references
Get-ChildItem -Recurse -Include "*.json","*.bicep","*.yaml","*.cs" |
  Select-String -Pattern "KeyVault.*cert|certificateUrl|vaultCertificates|SecretUri" |
  Select-Object Path, LineNumber, Line
```

**Common locations for certificate configuration:**

| Deployment Type | Where Cert Is Configured | What to Look For |
|---|---|---|
| Cloud Service (Classic) | `.cscfg` / `.csdef` files | `<Certificate>` elements with thumbprints |
| Service Fabric | `ApplicationManifest.xml`, `Settings.xml` | `<SecretsCertificate>`, `<EndpointCertificate>` |
| Kubernetes / AKS | Helm charts, YAML manifests | `secretName`, `tls.crt`, `tls.key` references |
| VM / VMSS | ARM templates, cloud-init | `vaultCertificates`, Key Vault references |
| App Service | App Settings, ARM template | `WEBSITE_LOAD_CERTIFICATES`, thumbprint in settings |

### Step 3.2: Retrieve and Inspect the Certificate

Once you have the thumbprint or Key Vault reference:

**Option A: Certificate is in Key Vault:**
```powershell
# List certificates in the Key Vault used by the service
az keyvault certificate list --vault-name "<VAULT_NAME>" --output table

# Get specific certificate details
az keyvault certificate show --vault-name "<VAULT_NAME>" --name "<CERT_NAME>" --output json

# Check the certificate's properties — look for EKU
az keyvault certificate show --vault-name "<VAULT_NAME>" --name "<CERT_NAME>" \
  --query "{subject: policy.x509CertificateProperties.subject, ekus: policy.x509CertificateProperties.ekus, issuer: policy.issuerParameters.name, expiry: attributes.expires}" \
  --output json
```

**Option B: Certificate is in local cert store (Windows):**
```powershell
# Find cert by thumbprint
$thumbprint = "<THUMBPRINT_FROM_CONFIG>"
$cert = Get-ChildItem -Path Cert:\LocalMachine\My\$thumbprint

# Display key properties
$cert | Format-List Subject, Issuer, NotAfter, NotBefore, Thumbprint, EnhancedKeyUsageList

# Specifically check for ClientAuth EKU
$cert.EnhancedKeyUsageList | Where-Object { $_.FriendlyName -eq "Client Authentication" }
# If this returns nothing → cert does NOT have ClientAuth EKU → NOT usable for mTLS
```

**Option C: Certificate is a pfx file in the repo:**
```powershell
# Find pfx files
Get-ChildItem -Recurse -Filter "*.pfx"

# If you have the password, inspect the cert
$cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2
$cert.Import("path\to\cert.pfx", "password", [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::DefaultKeySet)
$cert | Format-List Subject, Issuer, NotAfter, EnhancedKeyUsageList
```

### Step 3.3: Verify the Certificate Trust Chain

```powershell
# Build and verify the certificate chain
$chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
$chain.Build($cert)

# Display chain status
$chain.ChainStatus | Format-Table Status, StatusInformation

# Display chain elements (root → intermediate → leaf)
$chain.ChainElements | ForEach-Object {
    [PSCustomObject]@{
        Subject = $_.Certificate.Subject
        Issuer  = $_.Certificate.Issuer
        NotAfter = $_.Certificate.NotAfter
        IsG1 = $_.Certificate.Issuer -match "Microsoft RSA Root Certificate Authority 2017"
        IsG2 = $_.Certificate.Issuer -match "Microsoft Identity Verification Root Certificate Authority 2020"
    }
} | Format-Table -AutoSize
```

**What to look for:**
- If `IsG1 = True` anywhere in the chain → currently using G1 (pre-migration)
- If `IsG2 = True` anywhere in the chain → already on G2
- If `ChainStatus` shows errors → cert chain is broken (expired intermediate, missing root, etc.)

---

## Section 4: How to Test the Connection

### Step 4.1: Identify the Geneva Ingestion Endpoint

Geneva metrics/logs are published to specific endpoints. Find the endpoint from the service configuration:

```powershell
# Common endpoint patterns in config
Get-ChildItem -Recurse -Include "*.xml","*.json","*.yaml","*.cs" |
  Select-String -Pattern "monitoring\.core\.windows\.net|prod\.microsoftmetrics\.com|shoebox|gcs\.prod" |
  Select-Object Path, LineNumber, Line
```

Common Geneva endpoints:
- Metrics: `https://<region>.prod.microsoftmetrics.com`
- Logs: `https://<region>.prod.warm.ingest.monitor.core.windows.net`
- GCS (Config): `https://gcs.prod.monitoring.core.windows.net`

### Step 4.2: Test the mTLS Handshake

**PowerShell — Test with client certificate:**
```powershell
# Load the certificate (adjust path/thumbprint as needed)
$thumbprint = "<THUMBPRINT>"
$cert = Get-ChildItem -Path Cert:\LocalMachine\My\$thumbprint

# Target the Geneva endpoint
$endpoint = "https://<REGION>.prod.microsoftmetrics.com"

# Attempt connection with client cert
try {
    $response = Invoke-WebRequest -Uri $endpoint `
        -Certificate $cert `
        -Method GET `
        -UseBasicParsing `
        -ErrorAction Stop
    Write-Host "SUCCESS: HTTP $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Server accepted client certificate" -ForegroundColor Green
} catch {
    $err = $_.Exception
    Write-Host "FAILED: $($err.Message)" -ForegroundColor Red
    if ($err.InnerException) {
        Write-Host "Inner: $($err.InnerException.Message)" -ForegroundColor Yellow
    }
}
```

**curl (if available) — Test TLS handshake with verbose output:**
```bash
# Test with client certificate (converts pfx to pem first)
# --cert = client cert, --key = client key, -v = verbose TLS output
curl -v \
  --cert client.pem \
  --key client-key.pem \
  "https://<REGION>.prod.microsoftmetrics.com" \
  2>&1 | head -50
```

**OpenSSL — Detailed TLS handshake inspection:**
```bash
# This shows the full TLS negotiation including cert exchange
openssl s_client -connect <REGION>.prod.microsoftmetrics.com:443 \
  -cert client.pem \
  -key client-key.pem \
  -state -debug 2>&1 | head -80
```

### Step 4.3: Interpret the Results

| Outcome | What You See | What It Means |
|---|---|---|
| **mTLS Success** | HTTP 200/401/403 (any HTTP response) | TLS handshake completed — server accepted the client cert. HTTP errors may be auth/authz issues but mTLS itself works. |
| **mTLS Failure — Missing ClientAuth EKU** | `The credentials supplied to the package were not recognized` or `SEC_E_NO_CREDENTIALS` or `SSL/TLS handshake failed` | Server required ClientAuth EKU and cert doesn't have it. **This is the G2 blocker.** |
| **mTLS Failure — Expired cert** | `The remote certificate is invalid according to the validation procedure` or `CERT_HAS_EXPIRED` | Certificate has expired. Not a G2 issue — just operational cert management. |
| **mTLS Failure — Untrusted CA** | `The certificate chain was issued by an authority that is not trusted` | Server doesn't trust the cert's issuer. Could indicate G1 CA has been decommissioned or intermediate is missing. |
| **No mTLS required** | Connection succeeds **without** presenting a client cert | Server doesn't require client certs — this endpoint uses token auth. **NOT an mTLS endpoint.** |
| **Token auth endpoint** | HTTP 401 with `WWW-Authenticate: Bearer` header | Server expects an OAuth token, not a client cert. mTLS is not in play. |

**Quick test — Does the endpoint even require client certs?**
```powershell
# Connect WITHOUT a client certificate
try {
    $response = Invoke-WebRequest -Uri "https://<REGION>.prod.microsoftmetrics.com" `
        -Method GET `
        -UseBasicParsing `
        -ErrorAction Stop
    Write-Host "Endpoint does NOT require client cert (responded without one)" -ForegroundColor Yellow
} catch {
    $err = $_.Exception.Message
    if ($err -match "SSL|TLS|certificate|credentials") {
        Write-Host "Endpoint REQUIRES client cert (TLS handshake failed without one)" -ForegroundColor Red
    } else {
        Write-Host "HTTP error but TLS succeeded — endpoint does NOT require client cert" -ForegroundColor Yellow
        Write-Host "Error: $err"
    }
}
```

### Step 4.4: Capture the Full TLS Handshake for Debugging

```powershell
# Enable .NET TLS logging for detailed handshake capture
# Run this in the same PowerShell session before your test
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

# Enable System.Net tracing (creates detailed log file)
$traceConfig = @"
<configuration>
  <system.diagnostics>
    <sources>
      <source name="System.Net" tracemode="includehex" maxdatasize="1024">
        <listeners>
          <add name="System.Net" type="System.Diagnostics.TextWriterTraceListener" initializeData="network-trace.log" />
        </listeners>
      </source>
    </sources>
    <switches>
      <add name="System.Net" value="Verbose"/>
    </switches>
  </system.diagnostics>
</configuration>
"@

# Alternative: Use Wireshark/tcpdump for packet-level capture
# Wireshark filter: `tls.handshake.type == 13` shows CertificateRequest (server asking for client cert)
# If you see CertificateRequest → mTLS is required
# If no CertificateRequest → server-only TLS (no mTLS)
```

---

## Section 5: Red Flags to Watch For

### 5.1 Common Misconfigurations

| Red Flag | How to Detect | Impact |
|---|---|---|
| **Expired certificate** | `$cert.NotAfter -lt (Get-Date)` returns `True` | All auth (mTLS and token) fails. Most common operational issue. |
| **Wrong SAN (Subject Alternative Name)** | `$cert.DnsNameList` doesn't include the service hostname | mTLS handshake may succeed but server rejects cert at validation — depends on server's SAN checking policy. |
| **Missing intermediate CA** | `$chain.ChainStatus` shows `PartialChain` or `UntrustedRoot` | Chain validation fails — server can't verify cert back to root CA. Fix by installing the intermediate cert. |
| **ClientAuth EKU missing** | `$cert.EnhancedKeyUsageList` doesn't contain OID `1.3.6.1.5.5.7.3.2` | **THE G2 BLOCKER.** Cert cannot be used for mTLS client authentication. Server rejects during TLS handshake. |
| **Cert in wrong store** | Cert is in `Cert:\CurrentUser\My` instead of `Cert:\LocalMachine\My` | Service (running as SYSTEM or a service account) can't access the cert. Works in dev, fails in prod. |
| **Private key not exportable / not accessible** | `$cert.HasPrivateKey` returns `False` | Cert can't sign the TLS handshake → mTLS fails. |

### 5.2 Signs That a Namespace Is Using Token Auth (Not mTLS)

If you see these patterns in the codebase, the service is likely using **token-based auth** and mTLS is NOT a concern:

```csharp
// Token auth patterns — NOT mTLS blockers
new DefaultAzureCredential()                          // Managed identity
new ManagedIdentityCredential()                       // Explicit managed identity
new ClientSecretCredential(tenantId, clientId, secret) // AAD service principal with secret
TokenCredential                                        // Any Azure SDK token credential
"Authorization: Bearer"                                // HTTP bearer token header
```

If you see these patterns, the service IS using **mTLS** and is a potential G2 blocker:

```csharp
// mTLS patterns — POTENTIAL G2 BLOCKERS
handler.ClientCertificateOptions = ClientCertificateOption.Manual  // Explicit mTLS
handler.ClientCertificates.Add(cert)                               // Client cert attached to HTTP handler
HttpClientHandler { ClientCertificates = ... }                     // Client cert on handler
X509Certificate2 + HttpClient                                      // Cert loaded for HTTP client use
SslStream + RemoteCertificateValidationCallback                    // Custom TLS with cert validation
```

### 5.3 How This Relates to the ICM 764634026 Certificate Expiry Finding

From the investigation:

1. **MSPKI G2 certs permanently lack ClientAuth EKU** — this is an architectural decision by Microsoft, not a temporary gap
2. **The TAXII service (SecEng-Augusta) is confirmed as a CRITICAL mTLS blocker** — code explicitly loads client certs for mTLS handshakes
3. **Geneva/MDM cert usage in Amba.SituationalAwarenessData is UNCONFIRMED** — flagged as NEEDS REVIEW because:
   - 50 cert matches were found (non-trivial count)
   - pfx file references suggest certificates are bundled with the deployment
   - Geneva MDM references suggest the cert may be used for metric publishing auth
   - **BUT**: Many Geneva deployments use the Monitoring Agent (MA) which handles cert management separately from service code. If MA handles the cert, the service code doesn't need ClientAuth EKU — MA does.

### Decision Framework After Validation

After completing the steps above, you'll land in one of these outcomes:

| Finding | Action | Timeline Pressure |
|---|---|---|
| **Geneva account uses mTLS AND service holds the cert** | Must refactor to token auth or obtain mTLS-compatible cert before May 16 | **HIGH** — start immediately |
| **Geneva account uses mTLS BUT Monitoring Agent handles the cert** | Validate MA cert is G2-compatible (MA team usually handles this centrally) | **MEDIUM** — check with Geneva/MA team |
| **Geneva account uses token auth (AAD/managed identity)** | No mTLS blocker. Cert references are for AAD auth or transport encryption only | **LOW** — close the NEEDS REVIEW flag |
| **Cannot determine auth method from code/config** | Escalate to the Geneva/MDM team with account name and namespace | **MEDIUM** — need answer before April 10 |

---

## Appendix: Quick Reference Commands

```powershell
# === FIND THE CERT ===
# Search for thumbprints in config
Get-ChildItem -Recurse -Include "*.xml","*.json","*.cscfg" | Select-String "thumbprint" -CaseSensitive:$false

# === INSPECT THE CERT ===
# Load by thumbprint and check EKU
$cert = Get-ChildItem Cert:\LocalMachine\My\<THUMBPRINT>
$cert.EnhancedKeyUsageList  # Look for "Client Authentication" (OID 1.3.6.1.5.5.7.3.2)
$cert.NotAfter              # Check expiry
$cert.Issuer                # Check if G1 or G2 root

# === CHECK THE CHAIN ===
$chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
$chain.Build($cert)
$chain.ChainElements | ForEach-Object { $_.Certificate.Subject }

# === TEST THE ENDPOINT ===
# Without client cert (tests if mTLS is required)
Invoke-WebRequest -Uri "https://<endpoint>" -UseBasicParsing -ErrorAction SilentlyContinue

# With client cert (tests if cert is accepted)
Invoke-WebRequest -Uri "https://<endpoint>" -Certificate $cert -UseBasicParsing

# === CHECK CODE FOR AUTH PATTERN ===
# mTLS pattern (blocker)
Select-String -Path "*.cs" -Pattern "ClientCertificateOption|ClientCertificates\.Add" -Recurse
# Token auth pattern (not a blocker)
Select-String -Path "*.cs" -Pattern "DefaultAzureCredential|ManagedIdentityCredential|Bearer" -Recurse
```

---

*This guide was produced as part of the ICM 764634026 investigation. The NEEDS REVIEW flag on Amba.SituationalAwarenessData should be updated to CONFIRMED BLOCKER or CLEARED based on the findings from following these steps.*
