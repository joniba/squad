# ICM 764634026 — MSPKI G1→G2 Root CA Migration Resolution Walkthrough

> **OceanView SR17 — Full Resolution Guide for Jonathan's Team**
> **Status:** ACTIVE | **Severity:** 25 | **Environment:** PROD (Public Cloud, Global)
> **Created:** 2026-03-18 | **Owner:** AzRel Security Engineering
> **Author:** Aragorn (Operator) | **Last Updated:** 2026-03-22

---

## Executive Summary

### What's Happening

The MSPKI G1 root CA (`MicrosoftXS2028`) is **expiring**. All services must migrate their certificates to the new MSPKI G2 CA (`MicrosoftG2XS`) before expiration or face **service disruptions**.

Your team's services were **excluded from the central CA migration** due to one or more safety concerns (pinning, client auth, SDP violations, etc.). This means you must either:

1. **Clear your blockers** and opt into the **central migration** (deadline: **April 10, 2026**), or
2. Perform a **self-migration** (deadline: **May 16, 2026**)

### Why It Matters

- **Failure to migrate = service outage** when the G1 root CA expires
- Post-deadline, migration will be **enforced** and can cause unplanned outages
- This is an FTE-only incident — do NOT send external customer communications

### Key Deadlines

| Deadline | Path | What Happens If Missed |
|----------|------|----------------------|
| **April 10, 2026** | Central migration | Domain locked/blocked during central migration |
| **May 16, 2026** | Self-migration | Migration enforced, potential outage |

---

## Scope: Jonathan's Team Services

**Filter criteria:**
- Division = Microsoft Security
- Organization = MTP
- Service = USX Threat Intelligence
- Items **NOT** starting with `usx-ta` (~15 items)

### How to Get Your Exact Item List

Run this Kusto query to identify all in-scope items:

```kql
// ⚠️ One-Click to Join Kusto access first: linked in the TSG
cluster('azrelsikusto-dev.westus.kusto.windows.net').database('Security').AzRF_OV_SR17_Scope_Table
| where OrganizationName == "MTP"
| where serviceName !startswith "usx-ta"
| summarize dcount(DomainName) by ServiceOid, serviceName, TeamGroupName
```

> **⚠️ ACTION REQUIRED (Jonathan):** Run this query in the ADX dashboard and paste the results here. The Kusto cluster is not accessible from this automation. The meeting recording with Ligal Tuval may contain additional context about specific items — review and annotate this section.

---

## Step-by-Step Resolution Walkthrough

### Phase 0: Preparation

1. **Join the Kusto cluster** — Use the one-click access link from the TSG to get access to `azrelsikusto-dev.westus.kusto.windows.net`
2. **Review the IcM bridge** — Bridge ID `4219086` is available via Teams link in the ICM
3. **Set your ETA tag** — Add `AzRF.HasETA.YYYY.MM.DD` tag to your IcM with your planned completion date

### Phase 1: Identify Your Blockers (per item)

For **each** in-scope service item, run:

```kql
cluster('azrelsikusto-dev.westus.kusto.windows.net').database('Security').AzRF_OV_SR17_Scope_Table
| where OrganizationName == "MTP"
| where serviceName !startswith "usx-ta"
// Optionally filter to a specific service:
// | where ServiceOid == "<your-service-oid>"
| summarize dcount(DomainName) by ExclusionReason, Incidents, CertCA
```

This tells you **why** each service was excluded from central migration.

#### Blocker Assessment Decision Tree

```
START: What is your ExclusionReason?
│
├─ "Pinning SR03A"
│   └─ Static cert/CA pinning prevents safe rotation
│   └─ ACTION: Follow SR03a TSG → then proceed to Step 2
│   └─ TSG: https://eng.ms/docs/.../sr03a (linked in ICM TSG)
│
├─ "Torus Dependency"
│   └─ Torus-based service, may not follow Azure SDP
│   └─ ACTION: Proceed directly to Step 2 (no prerequisite fix needed)
│
├─ "ClientAuth"
│   └─ MSPKI cert IS used for Client Authentication
│   └─ ⚠️ CRITICAL: G2 certs do NOT have Client Authentication EKU
│   └─ ACTION: Follow SR03C / SR03C.1 / SR03e TSGs → then proceed to Step 2
│
├─ "ClientAuth (Suspected)"
│   └─ MSPKI cert MAY be used for Client Authentication
│   └─ ACTION: Verify usage. If confirmed, follow SR03c.1/SR03e TSGs
│   └─ If NOT using for client auth, proceed to Step 2
│
├─ "SDP Violation"
│   └─ Service not in Canary, may not follow Azure SDP
│   └─ ACTION: Proceed directly to Step 2
│
├─ "Rollback to MSPKI" / "Explicit Rollbacks"
│   └─ Service has explicitly opted out of migration
│   └─ ACTION: Proceed to Step 2
│
└─ Multiple blockers?
    └─ Address ALL applicable safety concerns before proceeding
```

### Phase 2: Choose Your Migration Path

#### Option A: Central Migration (Deadline: April 10, 2026)

**Choose this if:** Your blockers can be cleared quickly and you want the central team to handle the actual cert rotation.

**Steps:**

1. **Complete SR14 requirements first:**
   ```kql
   cluster('azrelsikusto-dev.westus.kusto.windows.net').database('Security').AzRF_OV_SR17_Scope_Table
   | where OrganizationName == "MTP"
   | where serviceName !startswith "usx-ta"
   | summarize dcount(DomainName) by ServiceOid, serviceName, TeamGroupName
   ```

2. **Verify ResourceCount = 0** in the SR14 scope table for each service

3. **Tag the SR14 IcM** with `AzRF.DeploymentComplete` and ensure it's Mitigated/Resolved

4. **Address all safety concerns** from Phase 1 for the SR17 workstream

5. **Self-attest** by adding `AzRF.CentralEnforcementOptIn` tag to your SR17 IcM

6. **Mitigate/Resolve** the IcM — central migration train handles the rest

> **⚠️ WARNING:** By opting into central migration, you acknowledge that MSPKI certificates are NOT being used for client authentication. G2 certs do not have ClientAuth EKU.

#### Option B: Self-Migration (Deadline: May 16, 2026)

**Choose this if:** You have confirmed blockers (e.g., client auth usage) that prevent central migration.

**Steps — repeat for each in-scope domain:**

**B.1 — Identify OneCert Domains to Upgrade:**
```kql
cluster('azrelsikusto-dev.westus.kusto.windows.net').database('Security').AzRF_OV_SR17_Scope_Table
| where OrganizationName == "MTP"
| where serviceName !startswith "usx-ta"
// | where ServiceOid == "<your-service-oid>"
| summarize dcount(CertThumbprint) by ServiceOid, DomainName, IssuerV1, PublicIssuerV2, PublicIssuer2V2, PublicIssuer3V2
```

**B.2 — For each domain, follow this decision:**

| Situation | Action |
|-----------|--------|
| Domain is **no longer in use** | Disable it → tag IcM with `AzRF.NotInUse` → Resolve IcM |
| Domain already on **MSPKI G2 CA** | Skip to B.4 (Renew) |
| Domain still on **MSPKI G1 CA** (`MicrosoftXS2028`) | Update domain registration per region (see TSG link) → then B.4 |

**B.3 — Update Domain Registration:**
- Follow the "Update domain registration per region" TSG linked in the OceanView SR17 page
- If you have test domains, **migrate and verify those first** before production

**B.4 — Renew Certificates from G2 CA:**
```kql
// Identify certificates to be renewed
cluster('azrelsikusto-dev.westus.kusto.windows.net').database('Security').AzRF_OV_SR17_Scope_Table
| where OrganizationName == "MTP"
| where serviceName !startswith "usx-ta"
// | where ServiceOid == "<your-service-oid>"
// | where DomainName == "<your-domain>"
| distinct ServiceOid, DomainName, IssuerV1, PublicIssuerV2, PublicIssuer2V2, PublicIssuer3V2, ObjectPath, SecretsStore, CertCA
```

**Renewal method depends on your secrets store:**
- **Azure Key Vault (AKV):** Use the standard AKV certificate renewal process
- **dSMS:** Use the Manual Rollover procedure

**B.5 — Verify renewed certificates** are from G2 CA (see Verification section below)

**B.6 — Distribute and activate** renewed certificates using your service-specific deployment process

**B.7 — Final validation:**
- Re-run the query from B.4 — it should return **no data** for migrated domains
- Tag IcM with `AzRF.DeploymentComplete`
- Resolve the IcM

---

## Verification Commands

### Azure CLI — Check Key Vault Certificate Issuer

```bash
az keyvault certificate show \
  --vault-name <VaultName> \
  --name <CertName> \
  --query "policy.issuerParameters.name"
```

### PowerShell — Check Local Certificate Issuer

```powershell
# Local certificates
Get-ChildItem Cert:\LocalMachine\My | Select-Object Subject, Issuer

# Remote site certificate
$response = Invoke-WebRequest https://<domain>
$cert = $response.BaseResponse.ServicePoint.Certificate
$cert.Issuer
```

### OpenSSL — Check Certificate File or Live Site

```bash
# From certificate file
openssl x509 -in <certfile>.crt -noout -issuer

# From live site
openssl s_client -connect <domain>:443 | openssl x509 -noout -issuer
```

### Azure Portal

Navigate to: **Key Vault → Certificates → [Your Certificate] → Properties** → check the **Issuer** field.

### Microsoft Edge (Browser)

1. Open the site using HTTPS
2. Click the padlock icon → "Connection is secure" → "Certificate is valid"
3. Go to **Details** → Find the **Issuer** field

---

## Expected G2 Issuers After Migration

After migration, your certificate's Issuer field should match one of these:

| Expected MSPKI G2 Issuer |
|---------------------------|
| Microsoft TLS G2 RSA CA OCSP 02 |
| Microsoft TLS G2 RSA CA OCSP 04 |
| Microsoft TLS G2 RSA CA OCSP 06 |
| Microsoft TLS G2 RSA CA OCSP 08 |
| Microsoft TLS G2 RSA CA OCSP 10 |
| Microsoft TLS G2 RSA CA OCSP 12 |
| Microsoft TLS G2 RSA CA OCSP 14 |
| Microsoft TLS G2 RSA CA OCSP 16 |
| Microsoft TLS G2 ECC CA OCSP 02 |
| Microsoft TLS G2 ECC CA OCSP 04 |
| Microsoft TLS G2 ECC CA OCSP 06 |
| Microsoft TLS G2 ECC CA OCSP 08 |

If you see a different issuer → check your OneCert settings and renew the certificate.

---

## Rollback Procedures

### Rollback Globally (All Regions)

1. Follow the **OneCert Issuer Rollback** TSG to reset OneCert to the previous issuer
2. Renew the certificates that were issued from the new issuer to get a certificate from the old MSPKI G1 CA

### Rollback in a Single Region

1. Follow the **Update domain per region** TSG to exclude the affected region
2. Reissue the certificate from the old CA for that region

> **⚠️ Rollbacks are for mitigation only.** The IcM can only be resolved when ALL certificates and domains are fully migrated to G2.

---

## Potential Errors During Migration

### Browser Errors
| Error | Meaning |
|-------|---------|
| `NET::ERR_CERT_AUTHORITY_INVALID` | Certificate chain cannot be validated; root or intermediate CA not trusted |
| `SEC_ERROR_UNKNOWN_ISSUER` (Firefox) | Issuer not recognized or trusted |

### .NET / System API Errors
| Error | Meaning |
|-------|---------|
| `System.Security.Authentication.AuthenticationException: The remote certificate is invalid` | Certificate validation failure |
| `System.Net.Http.HttpRequestException: The SSL connection could not be established` | Trust chain issue — "authority not trusted" |
| `Win32Exception: The certificate chain was issued by an authority that is not trusted` | Missing trusted root CA |
| `CryptographicException: The revocation function was unable to check revocation` | Revocation server offline |

### Other Platform Errors
| Error | Meaning |
|-------|---------|
| `javax.net.ssl.SSLHandshakeException: PKIX path building failed` (Java) | Trust anchor issues |
| `curl: SSL certificate problem: unable to get local issuer certificate` | CA bundle incomplete |

**If you encounter any of these during testing:** Tag your IcM with `AzRF.SMESupport` and join the bridge for assistance.

---

## Red Flag Tags Reference

| Tag | When to Use |
|-----|-------------|
| `AzRF.HasETA.YYYY.MM.DD` | Set your migration completion ETA. If beyond deadline, leadership notified. |
| `AzRF.Misattributed` | Domain/certs shouldn't be in scope. List misattributed certs in IcM Discussion (last 4 discussions pulled into Kusto). |
| `AzRF.SDPInProgress` | Cert migrated to new root, rolling out through SDP. |
| `AzRF.DeploymentComplete` | All certs validated as migrated. Note: validation will reactivate ticket if cert not actually migrated. |
| `AzRF.CentralEnforcementOptIn` | All SR14 certs addressed + SR17 remediation complete. Service opts into central enforcement. |
| `AzRF.SMESupport` | Need Red Flag or Domain SME support due to issues. |
| `AzRF.Dependency.<ServiceName>` | Migration blocked on dependency on another service. |
| `AzRF.NotInUse` | Domain no longer in use and has been disabled. |

---

## Exit Criteria

Your IcM can be closed when ALL of the following are true:

- [ ] `ResourceCount = 0` in the scope table (`AzRF_OV_SR17_Scope_Table`)
- [ ] IcM tagged with `AzRF.DeploymentComplete`
- [ ] IcM in Resolved or Mitigated state

### Validation Query (Run Daily — Data Refreshes Daily)

```kql
cluster('azrelsikusto-dev.westus.kusto.windows.net').database('Security').AzRF_OV_SR17_Scope_Table
| where OrganizationName == "MTP"
| where serviceName !startswith "usx-ta"
| summarize ResourceCount = dcount(DomainName) by ServiceOid, serviceName
| where ResourceCount > 0
```

When this returns **no results**, you're done.

---

## Deployment Guidelines (Mandatory)

Before making ANY changes:

1. **Safe Deployment Practices (SDP):** All production changes must follow SDP
2. **C+AI Services:** Every change must go through **R2D** — submit via R2D SafeFly
3. **Critical Change Only Advisory (CCoA):** Follow Azure change approval guidelines
4. **Establish warm bridge** for leadership and DRIs in case of outage
5. **End-to-end testing** including KPI impact descriptions
6. **Rollback validation** — test rollback capability before proceeding
7. **No ClickOps** — avoid Azure Portal manual changes; use CLI/automation

---

## Similar Incidents (Resolved) — Resolution Patterns

These resolved incidents used similar mitigation strategies:

| ICM ID | Type | Resolution Pattern |
|--------|------|-------------------|
| 704837743 | SR08 Cert Revocation | Validated cert revocation/renewal status before closure |
| 723741367 | SR01A AME Self Migration | Safe deployment + rollback validation |
| 723741389 | SR01A AME Self Migration | Safe deployment + rollback validation |
| 736689472 | SR14 Central Migration Blockers | Bulk closed tickets with zero resource count |
| 736689577 | SR14 Central Migration Blockers | Bulk closed tickets with zero resource count |
| 720955985 | SR09 Un-Managed Certs | Migrated unmanaged certificates following change review |

**Key pattern from resolved incidents:**
1. Bulk close tickets where ResourceCount = 0 to clear migration blockers
2. Complete migration with change review process
3. Validate cert status before closing

---

## Items Requiring Jonathan's Manual Input

> **These items cannot be determined from ICM + TSG alone:**

- [ ] **ADX Dashboard results:** Run the scoping queries above and paste the ~15 in-scope service items here
- [ ] **Meeting insights from Ligal Tuval recording:** Any service-specific guidance, exceptions, or deadlines discussed
- [ ] **Blocker assessment per item:** After running the blocker query, document which blocker applies to each of your ~15 items
- [ ] **Migration path decision per item:** Central vs Self-migration for each service
- [ ] **ETA per item:** Set `AzRF.HasETA.YYYY.MM.DD` tags on each IcM
- [ ] **SR14 status:** Verify whether SR14 requirements are already met for services choosing central migration
- [ ] **Client Auth usage:** Confirm whether any services actually use MSPKI certs for client authentication (critical for path selection — G2 does NOT have ClientAuth EKU)

---

## Quick Reference

| Resource | Link |
|----------|------|
| ICM Incident | [764634026](https://portal.microsofticm.com/imp/v5/incidents/details/764634026) |
| OceanView SR17 TSG | [eng.ms](https://eng.ms/docs/cloud-ai-platform/azure-cxp/cxp-azrel/azrel-risk-sre-pillar/red-flag-automation/risk-sre/troubleshooting/oceanview/oceanviewsr17tsg) |
| OceanView FAQ | Linked from TSG |
| Engineering Bridge | Teams meeting link in ICM (Bridge ID 4219086) |
| Kusto Cluster | `azrelsikusto-dev.westus.kusto.windows.net` / Database: `Security` |
| Scope Table | `AzRF_OV_SR17_Scope_Table` |

---

*Document generated by Aragorn (Operator) from ICM 764634026 incident data and OceanView SR17 TSG. For questions, join the engineering bridge or tag `AzRF.SMESupport` on your IcM.*
