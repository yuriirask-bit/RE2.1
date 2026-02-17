# RE2 Compliance Web UI - User Guide

This guide covers the web-based compliance management interface available at `https://localhost:5001` (local development) or your deployed App Service URL.

---

## Overview

The compliance web UI is built with ASP.NET MVC and provides visual access to all compliance management functions. It is designed for compliance managers, QA users, sales administrators, and training coordinators who need to manage licences, customers, GDP compliance, and reporting.

---

## Dashboard

The home dashboard (`/Dashboard`) provides an at-a-glance summary of:

- **Expiring licences** -- licences approaching expiry within 30, 60, and 90 days
- **Pending overrides** -- transaction overrides awaiting ComplianceManager approval
- **Overdue CAPAs** -- corrective actions past their due date
- **Equipment requalification** -- equipment due for requalification
- **Alerts** -- recent system alerts

---

## Licence Management

### Viewing Licences

Navigate to **Licences** to see all licences in the system. You can filter by:
- Holder type (Customer, Company)
- Status (Active, Expired, Suspended, Revoked)
- Holder ID

### Creating a Licence

1. Click **Create New Licence**
2. Fill in required fields: licence number, type, holder, validity dates
3. Optionally add permitted activities and scope
4. Click **Save**

### Managing Documents

Each licence can have supporting documents (PDF scans, certificates):

1. Open a licence detail page
2. Click **Documents** tab
3. Use **Upload Document** to attach a file (stored in Azure Blob Storage)
4. Click a document name to download

### Recording Verifications

Periodic licence verifications are tracked for audit purposes:

1. Open a licence detail page
2. Click **Verifications** tab
3. Click **Record Verification** and enter verification details, outcome, and notes

### Scope Changes

When a licence scope is modified by the issuing authority:

1. Open a licence detail page
2. Click **Scope Changes** tab
3. Record the change with effective date and description

---

## Customer Compliance

### Configuring Customer Compliance

Customers originate from D365 Finance & Operations. The compliance system adds compliance extensions:

1. Navigate to **Customers**
2. To configure a new customer, click **Browse D365 Customers** to find the customer in F&O
3. Click **Configure Compliance** on the desired customer
4. Set approval status, customer category, GDP qualification requirements, and re-verification interval
5. Click **Save**

### Compliance Status

Each customer page shows a real-time compliance status including:
- Licence validity (all required licences current)
- GDP qualification status
- Suspension status
- Last verification date and next due date

### Suspend / Reinstate

ComplianceManagers can suspend a customer to block all transactions:

1. Open the customer page
2. Click **Suspend** and provide a reason
3. To reinstate, click **Reinstate** -- this clears the suspension

---

## Transaction Validation

### Reviewing Transactions

Navigate to **Transactions** to see all validated transactions. Filter by:
- Status (Approved, Rejected, PendingOverride)
- Customer account
- Date range

### Approving / Rejecting Overrides

When a transaction fails validation but the ERP requests an override:

1. Navigate to **Transactions > Pending Overrides**
2. Review the violations listed for each transaction
3. Click **Approve** (with justification) or **Reject**

---

## Controlled Substances

### Managing Substances

Navigate to **Substances** to view controlled substances. These are sourced from D365 product attributes with compliance extensions.

- **Configure Compliance**: Set Opium Act list classification, precursor category, storage requirements
- **Deactivate / Reactivate**: Toggle substance active status

### Substance Reclassification

When a substance classification changes:

1. Navigate to **Reclassifications**
2. Click **New Reclassification** for the target substance
3. Enter the new classification, effective date, and authority reference
4. The system generates an **impact analysis** showing affected licences and customers
5. Process the reclassification to apply changes
6. Re-qualify affected customers as needed

---

## Thresholds

Navigate to **Thresholds** to manage quantity and frequency limits:

- **Quantity thresholds**: Maximum amount per transaction/period for a substance
- **Frequency thresholds**: Maximum number of transactions per period

Create thresholds scoped to specific substances, customer categories, or globally.

---

## GDP Compliance

### GDP Sites

GDP sites correspond to D365 F&O warehouses with compliance extensions:

1. Navigate to **GDP Sites**
2. **Browse Warehouses** to find D365 warehouses
3. **Configure GDP** to set site type, permitted activities, and inspection schedules
4. Manage **WDA Coverage** -- link WDA licences to sites

### GDP Service Providers

Manage third-party logistics and transport providers:

1. Navigate to **GDP Providers**
2. Create providers with company details and service types
3. Manage **Credentials** (WDA licences, GDP certificates, ISO certifications)
4. Upload supporting **Documents** for credentials
5. Record **Qualification Reviews** when re-qualifying providers
6. Record **Credential Verifications** (EudraGMDP checks)

The system tracks:
- Providers requiring re-qualification (based on review interval)
- Expiring credentials (configurable look-ahead period)

### GDP Inspections & CAPA

Track inspections and corrective actions:

1. Navigate to **GDP Inspections**
2. Create inspections linked to GDP sites
3. Record **Findings** for each inspection (observations, deficiencies)
4. Create **CAPAs** (Corrective and Preventive Actions) for findings
5. Track CAPA progress and completion
6. Monitor **Overdue CAPAs** from the dashboard

### GDP Equipment

Track equipment qualification status:

1. Navigate to **GDP Equipment**
2. Create equipment records with qualification details
3. Track requalification due dates
4. Monitor equipment due for requalification

### GDP SOPs

Manage Standard Operating Procedures:

1. Navigate to **GDP SOPs**
2. Create SOPs with document references, version tracking, and review dates
3. **Link SOPs to Sites** to track which procedures apply at each location
4. The SOP index provides a complete view of all applicable SOPs by site

### Training Records

Track staff training and assessments:

1. Navigate to **Training**
2. Record training activities with dates, topics, and assessment results
3. Track training completion and certification status

### Change Control

Manage GDP-related changes through a formal approval process:

1. Navigate to **Change Control**
2. Create a change record describing the proposed change
3. Change records go through an approval workflow:
   - **Pending** -- submitted for review
   - **Approved** -- approved by ComplianceManager
   - **Rejected** -- rejected with reason
4. Only ComplianceManagers can approve or reject changes

---

## Reports

Navigate to **Reports** to generate compliance reports:

### Transaction Audit Report
- Filter by date range, customer, substance, status
- Shows all transactions with compliance outcomes
- Target: generated within 2 minutes (SC-009)

### Licence Usage Report
- Shows licence utilization across transactions
- Identifies over-utilized or under-utilized licences

### Customer Compliance History
- Per-customer compliance timeline
- Shows approval status changes, verification history, and transaction outcomes

### Licence Correction Impact
- Analyzes the impact of retroactive licence corrections
- Shows transactions that would have different outcomes under corrected licence data

---

## Alerts

The system generates alerts for:
- Licences expiring within 90/60/30 days
- Credentials expiring within configured period
- Overdue CAPAs
- Equipment requalification due
- Provider re-qualification due

Alerts appear on the Dashboard and can be acknowledged by users.
