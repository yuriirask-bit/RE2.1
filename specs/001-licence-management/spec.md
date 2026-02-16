# Feature Specification: Controlled Drug Licence & GDP Compliance Management System

**Feature Branch**: `001-licence-management`
**Created**: 2026-01-09
**Status**: Draft
**Input**: User description: "A licence management system for a Dutch wholesaler dealing with controlled drugs should cover user stories around: (1) representing legal requirements (Opium Act exemption, wholesale licence, import/export permits, etc.), (2) onboarding and qualifying customers, (3) transaction time checks, (4) ongoing monitoring and audit, and (5) GDP compliance tracking"

## Clarifications

### Session 2026-01-09

- Q: When an order fails compliance checks (missing/expired licence), should the system hard-block the order (prevent submission entirely) or soft-block it (allow submission but require explicit override approval)? → A: Soft-block with override workflow: Order is saved in "Pending Compliance" status. Compliance Manager or authorized approver can review and explicitly approve with documented justification. Creates audit trail.
- Q: What authentication method should the system use for user login and session management? → A: Hybrid approach: Support both local credentials (for external partners/auditors) and enterprise SSO (for internal staff). Configurable per deployment.
- Q: How should the system handle concurrent modifications when two users edit the same licence or customer record at the same time? → A: Optimistic locking with conflict detection: System detects when two users save changes to same record. Second user sees conflict warning showing both versions and must explicitly merge or choose. Prevents silent data loss.
- Q: What are the system availability and uptime requirements during business hours? → A: High availability (99.5%): System designed for minimal downtime (~4 hours/month). Planned maintenance during off-peak hours. Failover capabilities for critical components. Supports continuous operations.
- Q: How does this system integrate with order management and warehouse systems - is it a standalone system where users enter orders directly, or does it integrate with existing systems to provide compliance checks? → A: Integration layer with existing systems: System provides APIs/integration endpoints that existing order management and WMS call for compliance validation. Returns pass/fail/pending status. Compliance users manage licences/GDP in this system; operational users continue using existing systems.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Manage Legal Licence Requirements (Priority: P1)

As a compliance manager, I need to register and maintain all licence types relevant in the Netherlands (wholesale licence under Medicines Act, Opium Act exemptions, import/export permits, manufacturer licences, precursor registrations) with their activities and substance mappings, so that the system can validate whether each transaction is legally authorized.

**Why this priority**: This is the foundational capability - without managing licences and understanding what activities they permit, no other compliance checks are possible. All other stories depend on having valid licence data.

**Independent Test**: Can be fully tested by creating, configuring, and maintaining different licence types with their permitted activities (possess, store, distribute, import, export, manufacture, handle precursors), mapping them to controlled substances (Opium Act Lists I/II, precursor categories), and verifying the data can be retrieved accurately.

**Acceptance Scenarios**:

1. **Given** I need to register a new wholesale licence, **When** I create a licence type record specifying it allows "possess, store, distribute" activities, **Then** the system stores these permissions and makes them available for transaction validation
2. **Given** I have controlled substances categorized by Opium Act Lists, **When** I map an Opium Act exemption to specific substances (A, B, C), **Then** the system restricts that licence to only authorize transactions involving those mapped substances
3. **Given** I have multiple licence types configured, **When** I configure which activities each permits (e.g., import/export vs. domestic distribution), **Then** the system uses these rules to determine whether a given transaction requires that licence type
4. **Given** I maintain a master list of controlled substances, **When** I categorize each by Dutch Opium Act list (I/II), precursor category, and internal codes, **Then** the system can perform substance-specific checks during transactions

---

### User Story 2 - Customer Onboarding & Qualification (Priority: P1)

As a sales or QA user, I need to create customer profiles capturing their legal status and record all their licences (wholesale, pharmacy, Opium Act exemptions, precursor registrations, import/export permits) with issuing authority, scope, and documents, so that I can qualify customers and ensure only authorized entities can purchase controlled drugs.

**Why this priority**: Equally critical to managing own licences - selling to unqualified customers creates immediate legal liability. Cannot process any customer transactions without this capability.

**Independent Test**: Can be fully tested by creating customer profiles for different entity types (hospital pharmacy, community pharmacy, veterinarian, manufacturer, foreign wholesaler), recording their licences with full details, setting required licence types per role and country, and verifying the system flags missing or invalid licences.

**Acceptance Scenarios**:

1. **Given** a new customer applies to purchase controlled drugs, **When** I create their profile capturing legal status (hospital pharmacy, community pharmacy, veterinarian, manufacturer, foreign wholesaler), **Then** the system applies the correct licence expectations for that customer type
2. **Given** I am qualifying a customer, **When** I record their licences (wholesale licence, pharmacy licence, Opium Act exemption, precursor registration, import/export permits) including issuing authority, scope, and attached documents, **Then** the system stores a complete relationship file for audit purposes
3. **Given** I have configured required licence types per customer role and country, **When** I review a Dutch hospital's qualifications, **Then** the system flags any missing required licences (e.g., hospital pharmacy licence, Opium Act exemption if handling List I substances)
4. **Given** a customer's licence has expired or is suspended, **When** the system validates licence dates and status, **Then** it highlights this customer as having invalid licences and blocks sales to them
5. **Given** customer licences need periodic re-verification (e.g., every 12 months), **When** the re-verification due date arrives, **Then** the system generates a task to re-qualify that customer

---

### User Story 3 - Licence Capture, Verification & Maintenance (Priority: P1)

As a compliance user, I need to upload licence documents (PDFs, letters), record verification methods (authority website check, email confirmation from Farmatec, copy of decision) with verification dates and verifiers, and manage licence scope changes with effective dates, so that there is complete traceability and historical record of all authorizations.

**Why this priority**: Critical for audit defense and regulatory compliance - without proper documentation and verification trails, the wholesaler cannot demonstrate due diligence. Supports both customer and own-company licence management.

**Independent Test**: Can be fully tested by uploading various licence documents, recording different verification methods with audit trails, setting up expiry reminders (90/60/30 days), recording scope changes over time, and verifying all evidence is retrievable for audits.

**Acceptance Scenarios**:

1. **Given** I have received a customer's pharmacy licence certificate (PDF), **When** I upload the document and link it to their licence record, **Then** auditors can access the original evidence directly from the customer file
2. **Given** I verified a licence on the IGJ website, **When** I record the verification method ("checked on IGJ authority website"), verification date, and my name as verifier, **Then** there is a traceable verification history for that licence
3. **Given** licences are approaching expiry, **When** the system checks expiry dates daily, **Then** I receive automated reminders at 90, 60, and 30 days before expiry for both customer and our own company licences (wholesale licence, Opium Act exemption)
4. **Given** a customer adds a new substance to their Opium Act exemption, **When** I record the scope change with an effective date, **Then** the system maintains historical authorization records so past transactions can be validated retrospectively
5. **Given** we hold multiple licences as a company (NL wholesale licence, GDP certificate, manufacturer licence, Opium Act exemption, precursor licences), **When** I manage them in the same system, **Then** transaction checks validate our internal eligibility (e.g., import/export of Opium Act drugs) alongside customer eligibility

---

### User Story 4 - Order & Shipment Transaction Checks (Priority: P2)

As an order entry or shipping user, I need the system to automatically check at order entry and shipment release whether the customer holds all required valid licences and exemptions for each controlled product, including import/export permits for cross-border shipments, so that non-compliant orders are blocked or flagged before processing.

**Why this priority**: High priority as it provides real-time protection against non-compliant transactions. Depends on Stories 1-3 establishing licence data, but once in place automates the critical compliance decision at transaction time.

**Independent Test**: Can be fully tested by entering orders with various combinations of products and customers (valid licences, expired licences, missing exemptions, cross-border shipments), setting quantity/frequency thresholds, and verifying the system correctly blocks/flags non-compliant orders with clear error messages.

**Acceptance Scenarios**:

1. **Given** an order is entered for controlled products, **When** the system validates the order, **Then** it automatically checks whether the customer holds all required valid licences and exemptions for each controlled product on the order line, blocking non-compliant orders
2. **Given** an order cannot proceed due to compliance issues, **When** I review the order status, **Then** I see clear error or warning messages indicating which licence or permit is missing or invalid for each product so I can contact the customer or compliance team
3. **Given** a shipment involves cross-border movement of Opium Act drugs or controlled precursors, **When** the system checks permits before shipment release, **Then** it verifies required import/export permits (export permit from NL, import certificate from destination country) are valid
4. **Given** I have defined quantity or frequency thresholds per customer and substance (e.g., max monthly amount), **When** an order exceeds normal patterns, **Then** the system flags or holds the order, supporting suspicious-order monitoring obligations
5. **Given** an order contains controlled drugs, **When** warehouse staff attempt to pick, pack, or print shipping labels, **Then** the system prevents these actions unless all licence and permit checks pass, ensuring warehouse processing is aligned with legal controls

---

### User Story 5 - Declarations, Reporting & Audits (Priority: P2)

As a compliance or audit user, I need the system to store all licence and permit information that supported each controlled-drug transaction, generate periodic reports (per substance, customer, country) with distributed quantities and associated licences, and maintain audit logs of all data changes, so that I can generate evidence for regulators and track regulatory inspections and corrective actions.

**Why this priority**: Essential for regulatory reporting and inspections. Depends on transactions occurring (Story 4), but critical for demonstrating compliance over time and responding to regulatory requests.

**Independent Test**: Can be fully tested by completing transactions with various licence/permit combinations, generating reports filtering by substance/customer/country, reviewing audit logs showing who changed what data and when, and recording inspection findings with corrective actions.

**Acceptance Scenarios**:

1. **Given** controlled-drug transactions have been completed, **When** the system stores transaction records, **Then** it captures the licence numbers, exemption IDs, and permit numbers that authorized each transaction so I can generate evidence for regulators
2. **Given** I need to submit periodic regulatory reports, **When** I generate a report filtered by substance, customer, or country, **Then** the system shows distributed quantities, associated licences, and any overrides so mandatory reporting and internal monitoring can be performed
3. **Given** licence data may be subject to audit questions, **When** I review the audit log, **Then** it records who changed licence data, when, and what was changed, ensuring data integrity and accountability
4. **Given** we undergo regulatory inspections, **When** I record inspection details (IGJ or Customs findings on Opium Act compliance), **Then** the system stores findings and corrective actions so follow-up is tracked centrally and inspection history is maintained

---

### User Story 6 - Risk Management, Workflows & Access Control (Priority: P3)

As a compliance or system admin user, I need configurable workflows for high-risk events (adding controlled substances to customer scope, approving exception sales, adding new countries) requiring review and approval by designated roles, with role-based access control restricting who can create/modify licences or override blocks, so that risk is controlled and sensitive compliance operations are properly authorized.

**Why this priority**: Important for operational control and audit defense, but can be implemented after core compliance checks are working. Adds governance layer on top of existing functionality.

**Independent Test**: Can be fully tested by configuring approval workflows for specific high-risk events, attempting to perform restricted actions with different user roles, and verifying that only authorized users can approve exceptions, modify licence data, or override system blocks.

**Acceptance Scenarios**:

1. **Given** a high-risk event occurs (e.g., adding a new controlled substance to a customer's approved scope, approving an exception sale, adding a new country), **When** the event is initiated, **Then** the system triggers a configurable workflow requiring review and approval by designated roles before the change takes effect
2. **Given** the system has role-based access control configured, **When** users attempt to create or modify licence records, approve exceptions, or override system blocks, **Then** only authorized users in designated roles can perform these sensitive actions
3. **Given** I am a compliance or QA manager, **When** I view my dashboard, **Then** I see key risks highlighted (customers with expiring licences, blocked orders due to missing exemptions, abnormal order volumes) so attention is focused where most needed

---

### User Story 7 - GDP Master Data & Authorisations (Priority: P1)

As a QA/compliance manager or data steward, I need to register our Wholesale Distribution Authorisation (WDA) and GDP certificate (scope, sites, activities, inspection dates), maintain a master list of all GDP-relevant sites (warehouses, cross-docks, transport hubs) linked to covering WDAs, and configure GDP-relevant activities per site (storage >72h, temperature-controlled, outsourced, transport-only), so that the system ensures no GDP-relevant activity occurs at an unlicensed site.

**Why this priority**: Foundational for GDP compliance - parallel to licence management but focused on distribution authorization and site/activity controls. Required before any GDP compliance checks can occur.

**Independent Test**: Can be fully tested by registering WDA and GDP certificates with their scope, creating site master data and linking sites to WDAs, configuring activities per site, and verifying the system can validate whether a given activity at a given site is covered by appropriate authorization.

**Acceptance Scenarios**:

1. **Given** we hold a Wholesale Distribution Authorisation and GDP certificate, **When** I register them including scope, sites, activities, and inspection dates, **Then** the system can monitor our own eligibility to distribute medicines
2. **Given** we operate multiple facilities, **When** I maintain a master list of all GDP-relevant sites (warehouses, cross-docks, transport hubs) and link them to the WDA(s) that cover them, **Then** the system ensures no GDP-relevant activity occurs at an unlicensed site
3. **Given** different sites perform different functions, **When** I configure GDP-relevant activities per site (storage >72 hours, temperature-controlled distribution, outsourced warehousing, transport only), **Then** GDP obligations and checks reflect the actual process at each location

---

### User Story 8 - Supplier, Customer & Service-Provider GDP Qualification (Priority: P2)

As a GDP Responsible Person or QA user, I need to record GDP status and WDA/GMP authorisations of suppliers, customers, and service providers (3PLs, transporters, external warehouses) including EudraGMDP entries, track their qualification status (approved, conditionally approved, rejected, under review) based on audits or questionnaires, and set review frequencies with reminders (e.g., every 2-3 years), so that non-approved partners cannot be selected and periodic re-qualification is not missed.

**Why this priority**: Critical for GDP supply chain integrity. Depends on Story 7 establishing GDP framework, then extends qualification requirements to all trading partners and service providers.

**Independent Test**: Can be fully tested by recording GDP credentials for various partners, conducting qualification reviews with different outcomes, setting review frequencies, and verifying the system prevents selection of non-approved partners in transactions while generating re-qualification reminders.

**Acceptance Scenarios**:

1. **Given** we work with suppliers, customers, and service providers, **When** I record their GDP status and WDA/GMP authorisations including links to EudraGMDP entries, **Then** "bona fides" of all partners can be demonstrated during inspections
2. **Given** partners undergo GDP qualification reviews, **When** I track qualification status (approved, conditionally approved, rejected, under review) based on GDP audits or questionnaires, **Then** the system prevents non-approved partners from being selected in orders or contracts
3. **Given** GDP qualifications require periodic review, **When** I set review frequencies (e.g., every 2-3 years) for each supplier and service provider, **Then** I receive reminders so periodic re-qualification is not missed

---

### User Story 9 - GDP Inspections, Audits & CAPA Tracking (Priority: P2)

As a QA/compliance user or manager, I need to register GDP inspections by authorities (IGJ/NVWA) and internal/self-inspections against each site and WDA (including findings and classification: critical/major/other), create and track CAPAs linked to specific GDP findings with owners and due dates, and view dashboards summarizing open/overdue CAPAs and upcoming inspections, so that completion can be demonstrated and resources prioritized to highest-risk issues.

**Why this priority**: Essential for regulatory defense and continuous improvement. Depends on GDP framework (Story 7) being established, then provides the audit and corrective action layer.

**Independent Test**: Can be fully tested by recording various inspection types (authority, internal) with findings at different sites, creating CAPAs linked to findings with ownership and due dates, and verifying dashboards correctly summarize open/overdue CAPAs and inspection schedules.

**Acceptance Scenarios**:

1. **Given** we undergo GDP inspections, **When** I register inspections by competent authorities (e.g., IGJ/NVWA) and internal/self-inspections against each site and WDA including findings and classification (critical/major/other), **Then** inspection history is centralized for regulatory review
2. **Given** inspections identify deficiencies, **When** I create and track CAPAs linked to specific GDP findings (e.g., temperature mapping deficiency, documentation gap) with owners and due dates, **Then** completion can be demonstrated during follow-up inspections
3. **Given** I need to prioritize quality resources, **When** I view my QA manager dashboard, **Then** I see open/overdue GDP CAPAs and upcoming inspections per site so resources can be allocated to highest-risk issues

---

### User Story 10 - GDP Certificates, Validity & Monitoring (Priority: P2)

As a compliance or QA user, I need to record validity periods of GDP certificates and WDAs for our company and key partners with automated expiry alerts, attach GDP certificates/WDA copies/inspection reports to entity records, and log verification of partner GDP status via EudraGMDP or national databases (date, verifier, outcome), so that documentary evidence is readily available and proof of ongoing monitoring exists.

**Why this priority**: Parallel to licence expiry management (Story 3) but for GDP credentials. Critical for maintaining valid GDP status and demonstrating ongoing monitoring.

**Independent Test**: Can be fully tested by recording GDP certificate validity periods for internal and external entities, setting up expiry alerts, attaching various document types to records, and logging verification activities via EudraGMDP or databases.

**Acceptance Scenarios**:

1. **Given** GDP certificates and WDAs have expiry dates, **When** I record validity periods for our company and key partners, **Then** the system generates automated alerts before expiry so renewals or re-inspections can be planned in time
2. **Given** documentary evidence must be available for regulators and customers, **When** I attach GDP certificates, WDA copies, and inspection reports to each entity record (our company, suppliers, 3PLs, warehouses), **Then** all evidence is centralized and quickly accessible
3. **Given** ongoing monitoring of partners is required, **When** I verify partner GDP status via EudraGMDP or national databases, **Then** the system logs the verification (date, verifier, outcome) so proof of ongoing monitoring is available during audits

---

### User Story 11 - GDP Operational Checks & Distribution Controls (Priority: P2)

As an operations/warehouse user or logistics planner, I need the system to prevent assigning product storage or distribution to sites or 3PLs not covered by appropriate WDA/GDP certificates, show which routes and transport providers are GDP-approved (including temperature-controlled capability), and record qualification status of GDP equipment/processes (temperature-controlled vehicles, monitoring systems) with re-qualification due dates, so that only qualified resources are used for medicine distribution.

**Why this priority**: Operationalizes GDP compliance at the execution level. Depends on GDP master data and qualifications (Stories 7-8) but provides real-time controls during warehouse and logistics operations.

**Independent Test**: Can be fully tested by attempting to assign storage/distribution to various sites and 3PLs (approved vs. not approved), selecting transport providers for different shipment types, and recording equipment qualification status with re-qualification tracking.

**Acceptance Scenarios**:

1. **Given** product storage or distribution is being assigned, **When** I select a site or 3PL, **Then** the system prevents assigning to locations not covered by an appropriate WDA/GDP certificate so medicines are not handled in non-compliant facilities
2. **Given** I am planning logistics for a shipment, **When** I view available routes and transport providers, **Then** the system shows which are approved for GDP (including temperature-controlled capability) so I can choose only compliant options
3. **Given** GDP requires qualified equipment and processes, **When** I record qualification/validation status of key resources (e.g., temperature-controlled vehicles, monitoring systems) and their next re-qualification due dates, **Then** the system ensures only qualified resources are used for distribution

---

### User Story 12 - GDP Documentation, Training & Change Control (Priority: P3)

As a QA user or training coordinator, I need to maintain an index of GDP-relevant SOPs (returns, recalls, deviations, temperature excursions, outsourced activities) linked to sites and activities, record GDP-specific training completion for distribution staff linked to their functions, and manage changes impacting GDP (new warehouse, new 3PL, new product type, change in storage condition) via controlled change records with approvals, so that competency evidence is available during inspections and GDP risk assessment/documentation are updated before implementation.

**Why this priority**: Important for inspection readiness and change control, but can be layered on after operational GDP controls are working. Adds governance and training documentation to existing GDP framework.

**Independent Test**: Can be fully tested by maintaining SOP indices linked to sites/activities, recording training completion for staff in distribution roles, and processing change records for GDP-impacting changes with required approvals.

**Acceptance Scenarios**:

1. **Given** we have GDP-relevant procedures, **When** I maintain an index of GDP-relevant SOPs (e.g., returns, recalls, deviations, temperature excursions, outsourced activities) and link them to sites and activities, **Then** inspections can quickly confirm applicable procedures are documented
2. **Given** competency must be demonstrated, **When** I record GDP-specific training completion for staff in distribution roles and link training curricula to their functions, **Then** competency evidence is available during inspection
3. **Given** changes may impact GDP compliance, **When** I manage changes that impact GDP (new warehouse, new 3PL, new product type, change in storage condition) via controlled change records and approvals, **Then** GDP risk assessment and documentation are updated before implementation

---

### Edge Cases

**Licence & Transaction Edge Cases:**

| Edge Case | Disposition | Reference |
|-----------|-------------|-----------|
| Customer's licence expires mid-transaction (order placed while valid but expires before shipment) | **v1.0 SCOPE** - Soft-block with override workflow per clarification Q1 | FR-019a |
| Concurrent modifications when two users edit the same licence or customer record | **v1.0 SCOPE** - Optimistic locking with conflict resolution per clarification Q3 | FR-027a-c |
| Licences with conditional restrictions (approved for certain substance categories but not others) | **v1.0 SCOPE** - Handled via licence-to-substance mappings per FR-004 | FR-004, User Story 1 |
| Regulatory authorities revoke licence unexpectedly (not standard expiry) | **v1.0 SCOPE** - Manual status update to "Revoked", system blocks transactions per FR-005 | FR-005, FR-015 |
| Distinguish jurisdiction requirements (Netherlands domestic vs. EU cross-border vs. non-EU) | **v1.0 SCOPE** - Cross-border indicator in transaction validation per FR-021 | FR-021, Assumption 4 |
| Customer has multiple legal entities with different licence sets | **v1.0 SCOPE** - Separate customer profiles per legal entity per Assumption 17 | Assumption 17 |
| Grace periods where authorities allow continued operation during renewal processing | **v1.0 SCOPE** - Grace period tracking in Licence model per Assumption 16 | Assumption 16 |
| Historical licence data corrected after transactions completed under incorrect information | **v1.0 SCOPE** - Manual impact assessment via correction impact report per SC-038 | Assumption 18, T163a-d |
| Quantity threshold monitoring when customers place multiple small orders across time periods | **v1.0 SCOPE** - Threshold monitoring aggregates quantities per monitoring period (monthly/annual) per FR-022 | FR-022, T132a-g |
| Substance reclassified (e.g., moved from List II to List I), affecting existing licences and orders | **v1.0 SCOPE** - Reclassification workflow with customer impact analysis per FR-066 | FR-066, T080a-o |
| 3PL's GDP certificate suspended mid-shipment while products in their custody | **DEFERRED v2.0** - Real-time shipment tracking out of scope per Assumption 12 | Assumption 12 |
| Temperature excursions during distribution affecting future GDP qualification | **DEFERRED v2.0** - Real-time temperature monitoring via WMS/QMS per Assumption 12 | Assumption 12 |
| Supplier's WDA revoked but products already in inventory | **DEFERRED v2.0** - Inventory recall workflows out of scope per Assumption 9 | Assumption 9 |
| Split shipments where part goes through GDP-approved routes and part doesn't | **DEFERRED v2.0** - Advanced logistics routing out of scope; single route per shipment assumed | Assumption 22 |
| CAPA deadline missed - automatic impact on GDP status or manual review? | **v1.0 SCOPE** - Manual review; dashboard highlights overdue CAPAs per FR-042 but no automatic status change | FR-042, User Story 9 |
| Sites temporarily closed (e.g., maintenance) - marked unavailable for distribution? | **DEFERRED v2.0** - Site availability scheduling out of scope; manual site deactivation available | GDP operational controls |
| EudraGMDP verification shows different status than partner provided | **v1.0 SCOPE** - Manual verification records discrepancy; QA user investigates per FR-045 | FR-045, User Story 10 |
| Training records when staff change roles or leave company | **v1.0 SCOPE** - Training records linked to staff member ID; historical records retained per FR-050 | FR-050, User Story 12 |


## Requirements *(mandatory)*

### Functional Requirements - Licence Management

- **FR-001**: System MUST store and track multiple licence types including Opium Act exemptions, wholesale licences (WDA), GDP certificates, import permits, export permits, pharmacy licences, manufacturer licences, and precursor registrations
- **FR-002**: System MUST configure for each licence type which activities it allows (possess, store, distribute, import, export, manufacture, handle precursors) so transaction validation can check activity authorization
- **FR-003**: System MUST maintain a master list of controlled substances mapped to Dutch Opium Act lists (I/II), precursor categories, and internal product codes for substance-specific checks
- **FR-004**: System MUST define licence-to-substance mappings (e.g., "Opium Act exemption X covers substances A, B, C") to control which products a given licence authorizes
- **FR-005**: System MUST record for each licence: unique licence number, issuing authority, issue date, expiry date, scope/restrictions, current status (valid, expired, suspended, revoked), and holder (internal company or specific customer)
- **FR-006**: System MUST calculate and display remaining validity period for each licence in days
- **FR-007**: System MUST generate alerts when licences are within a configurable period of expiry (default: 90 days for own licences, 60 days for customer licences, 30 days final warning) using unified alert generation service per FR-043
- **FR-008**: System MUST allow attachment of supporting documentation (scanned licences, certificates, PDF permits, letters) to licence records
- **FR-009**: System MUST record verification method (authority website check, email confirmation, copy of decision), verification date, and verifier name for each licence verification
- **FR-010**: System MUST record licence scope changes with effective dates to maintain historical authorization records
- **FR-011**: System MUST manage both customer/partner licences and the wholesaler's own licences in the same system for consistent transaction validation

### Functional Requirements - Customer Onboarding & Qualification

- **FR-012**: System MUST support creating customer profiles capturing legal status (hospital pharmacy, community pharmacy, veterinarian, manufacturer, foreign wholesaler, research institution, etc.)
- **FR-013**: System MUST record all licences, permits, and exemptions held by a customer (wholesale licence, pharmacy licence, Opium Act exemption, precursor registration, import/export permits) including issuing authority, scope, and attached documents
- **FR-014**: System MUST define required licence types per customer role and country (e.g., Dutch hospital vs. German wholesaler vs. French pharmacy) so the system can flag missing licences during onboarding
- **FR-015**: System MUST validate licence dates (issue/expiry) and status (active/suspended/expired) and highlight customers with invalid or missing licences
- **FR-016**: System MUST prevent customer approval if required licences are missing or expired
- **FR-017**: System MUST support periodic re-verification of customer licences with configurable intervals (e.g., every 12 months) and generate tasks when re-verification is due

### Functional Requirements - Transaction Compliance Checks

- **FR-018**: System MUST provide API endpoint for transaction validation that accepts order details (customer ID, product list with substances and quantities, destination country, transaction type) and returns compliance check results (pass/fail/pending status, specific violations, required actions)
- **FR-019**: System MUST return "pending compliance" status for orders where customer lacks required valid licences for any product, including detailed violation information (missing licence type, expired dates, invalid status) that calling system can use to block warehouse processing
- **FR-019a**: System MUST provide compliance override workflow accessible via web UI allowing designated approvers to review pending orders, view specific compliance violations, document justification, and approve order for processing. **Role Configuration**: Override approval roles are configurable per deployment via appsettings.json (default: "ComplianceManager" role). When multiple roles are configured, ANY user in ANY configured role can approve (logical OR). System validates approver has active employee status per FR-031. Required justification fields: reason code (from predefined list: "Emergency Medical Supply", "Licence Renewal In Progress", "Authority Pre-Approval", "Other"), free-text justification (minimum 20 characters), authority reference number (optional).
- **FR-019b**: System MUST expose API endpoint for checking override approval status that external systems can poll or receive webhook notifications when pending orders are approved or rejected
- **FR-020**: System MUST return structured error/warning messages in API responses indicating which specific licence or permit is missing or invalid for each non-compliant product, formatted for display in calling systems
- **FR-021**: System MUST validate required import/export permits for cross-border shipments of Opium Act drugs and controlled precursors (export permit from NL, import certificate from destination country) when transaction validation API is called with cross-border indicator
- **FR-022**: System MUST check quantity or frequency thresholds per customer and substance (e.g., max monthly amount) during transaction validation and return threshold-exceeded warnings in API response for suspicious-order monitoring; Thresholds pre-configured per customer-substance combination per data-model.md entity 10
- **FR-023**: System MUST provide API endpoint for warehouse operation validation (pick, pack, print labels) that checks compliance status and returns block/allow decision for controlled drug shipments
- **FR-024**: System MUST validate both customer licences AND the wholesaler's own licences during transaction validation API calls (e.g., our import/export authority when importing Opium Act drugs)

### Functional Requirements - Reporting, Audit & Compliance

- **FR-025**: System MUST store the licence and permit information that authorized each controlled-drug transaction (licence numbers, exemption IDs, permit numbers) for regulatory evidence
- **FR-026**: System MUST generate periodic reports filtered by substance, customer, or country showing distributed quantities, associated licences, and any overrides for mandatory reporting
- **FR-027**: System MUST maintain complete audit trail recording who changed licence data, when, what was changed, and supporting documentation references
- **FR-027a**: System MUST implement optimistic locking for all data modification operations (licences, customers, GDP sites, CAPAs, etc.) by tracking record version/timestamp and detecting conflicts when multiple users save changes to the same record
- **FR-027b**: System MUST present conflict resolution interface when concurrent modification is detected, showing both the user's changes and the conflicting changes from the other user, allowing the user to choose one version, merge specific fields, or cancel their changes
- **FR-027c**: System MUST log all conflict resolution events in the audit trail, recording both versions of conflicting data, which user resolved the conflict, and what resolution action was taken (chose original, chose their changes, merged)
- **FR-028**: System MUST record regulatory inspections (IGJ, Customs findings on Opium Act compliance) including date, inspector, findings, and corrective actions
- **FR-029**: System MUST generate audit reports for specific customers showing complete compliance history (initial qualification, licence verifications, status changes, incidents)

### Functional Requirements - Risk, Workflow & Access Control

- **FR-030**: System MUST support configurable workflows for high-risk events (adding controlled substance to customer scope, approving exception sale, adding new country) requiring review and approval by designated roles
- **FR-031**: System MUST implement role-based access control so only authorized users can create or modify licence records, approve exceptions, or override system blocks
- **FR-031a**: System MUST support hybrid authentication: Azure Active Directory B2C for enterprise SSO integration (supporting SAML 2.0, OAuth2, OpenID Connect protocols for connecting to organizational identity providers like Active Directory, Azure AD, Okta) AND local username/password authentication (for external partners, auditors, or deployments without enterprise SSO). Technology selection: Azure AD B2C as SSO provider per plan.md technical context.
- **FR-031b**: System MUST manage user sessions securely with configurable timeout periods, automatic logout on inactivity, and audit logging of all authentication events (successful logins, failed attempts, logouts, session expirations)
- **FR-031c**: System MUST allow administrators to configure which authentication method(s) are enabled per deployment and map SSO groups/claims to internal application roles
- **FR-032**: System MUST provide dashboards highlighting key risks (customers with expiring licences, blocked orders due to missing exemptions, abnormal order volumes) for compliance and QA managers. Dashboard MUST load within 5 seconds, display data no older than 15 minutes (cached from source systems), and support filtering by date range and entity type

### Functional Requirements - GDP Master Data & Authorisations

- **FR-033**: System MUST register the wholesaler's Wholesale Distribution Authorisation (WDA) and GDP certificate including scope, sites, activities, and inspection dates
- **FR-034**: System MUST maintain a master list of all GDP-relevant sites (warehouses, cross-docks, transport hubs) and link them to the WDA(s) that cover them
- **FR-035**: System MUST configure GDP-relevant activities per site (storage >72 hours, temperature-controlled distribution, outsourced warehousing, transport only) to reflect actual processes and obligations

### Functional Requirements - Partner GDP Qualification

- **FR-036**: System MUST record GDP status and WDA/GMP authorisations of suppliers, customers, and service providers (3PLs, transporters, external warehouses) including links to EudraGMDP entries
- **FR-037**: System MUST track qualification status of suppliers and service providers (approved, conditionally approved, rejected, under review) based on GDP audits or questionnaires
- **FR-038**: System MUST prevent selection of non-approved partners in orders or contracts
- **FR-039**: System MUST support setting review frequencies (e.g., every 2-3 years) for supplier and service-provider GDP qualification and generate reminders when re-qualification is due

### Functional Requirements - GDP Inspections & CAPA

- **FR-040**: System MUST register GDP inspections by competent authorities (IGJ/NVWA) and internal/self-inspections against each site and WDA including findings and classification (critical/major/other)
- **FR-041**: System MUST support creating and tracking CAPAs linked to specific GDP findings with owners, due dates, and completion status
- **FR-042**: System MUST provide dashboards summarizing open/overdue GDP CAPAs and upcoming inspections per site

### Functional Requirements - GDP Certificates & Monitoring

- **FR-043**: System MUST record validity periods of GDP certificates and WDAs for the company and key partners and generate automated alerts before expiry using unified alert generation service shared with FR-007
- **FR-044**: System MUST allow attaching GDP certificates, WDA copies, and inspection reports to entity records (company, suppliers, 3PLs, warehouses)
- **FR-045**: System MUST log verification of partner GDP status via EudraGMDP or national databases (date, verifier, outcome) to prove ongoing monitoring

### Functional Requirements - GDP Operational Controls

- **FR-046**: System MUST prevent assigning product storage or distribution to a site or 3PL not covered by an appropriate WDA/GDP certificate
- **FR-047**: System MUST indicate which routes and transport providers are approved for GDP (including temperature-controlled capability) during logistics planning
- **FR-048**: System MUST record qualification/validation status of key GDP equipment and processes (temperature-controlled vehicles, monitoring systems) and track re-qualification due dates

### Functional Requirements - GDP Documentation, Training & Change Control

- **FR-049**: System MUST maintain an index of GDP-relevant SOPs (returns, recalls, deviations, temperature excursions, outsourced activities) linked to sites and activities
- **FR-050**: System MUST record GDP-specific training completion for staff in distribution roles and link training curricula to their functions
- **FR-051**: System MUST manage changes impacting GDP (new warehouse, new 3PL, new product type, change in storage condition) via controlled change records and approvals to ensure risk assessment occurs before implementation

### Functional Requirements - System Availability & Reliability

- **FR-052**: System MUST achieve 99.5% uptime during business operations, allowing maximum 4 hours of combined planned and unplanned downtime per month
- **FR-053**: System MUST schedule planned maintenance activities during off-peak hours (configurable maintenance windows, typically evenings/weekends) with advance notification to users
- **FR-054**: System MUST implement failover capabilities for critical compliance check functions (transaction validation, licence lookups, alert generation) to minimize impact of component failures. **Critical functions** (must remain available during partial failures): Transaction validation API (FR-018), customer compliance status lookup API (FR-060), licence lookup, warehouse operation validation (FR-023), alert generation for expiry warnings. **Non-critical functions** (may degrade gracefully during component failures): Report generation (FR-026), audit log queries, dashboard analytics, document upload, workflow approvals. System MUST return HTTP 503 with Retry-After header for degraded non-critical endpoints.
- **FR-055**: System MUST provide graceful degradation when non-critical features are unavailable (e.g., reports, dashboards), allowing core compliance operations (order validation, licence management) to continue
- **FR-056**: System MUST monitor system health metrics (response times, error rates, resource utilization) and alert administrators when thresholds are exceeded or availability is at risk

### Functional Requirements - Integration & APIs

- **FR-057**: System MUST provide RESTful API endpoints for integration with external order management systems (ERP, custom order entry systems) and warehouse management systems (WMS)
- **FR-058**: System MUST support synchronous API calls for real-time transaction validation with response time under 3 seconds at 95th percentile (p95) that block/allow order processing based on compliance checks. Target p50 response time: <1 second. Maximum acceptable p99 response time: 5 seconds.
- **FR-059**: System MUST provide webhook/callback mechanism for asynchronous notifications when compliance status changes (e.g., pending order approved, customer suspended due to licence expiry). Webhook delivery MUST retry failed deliveries up to 3 times with exponential backoff (10s, 60s, 300s). After 3 failures, the subscription is marked as unhealthy and an alert is generated for the SystemAdmin. Subscribers can query missed events via GET /api/v1/webhooks/{subscriptionId}/events endpoint
- **FR-060**: System MUST support customer/product master data synchronization APIs allowing external systems to query customer compliance status, licence validity, and GDP qualifications before displaying options to users
- **FR-061**: System MUST maintain transaction audit records for all API calls including calling system identity, request payload, response, timestamp, and user context (which external user triggered the validation)
- **FR-062**: System MUST provide API versioning and backward compatibility support to minimize disruption when APIs evolve (maintain previous API version for minimum 6 months after new version release)
- **FR-063**: System MUST implement API authentication and authorization (API keys, OAuth2 tokens, or mutual TLS) ensuring only authorized systems can call compliance validation endpoints
- **FR-064**: System MUST return standardized error codes and messages in API responses to enable consistent error handling by calling systems (e.g., "LICENCE_EXPIRED", "SUBSTANCE_NOT_AUTHORIZED", "THRESHOLD_EXCEEDED")
- **FR-065**: System MUST provide API documentation (OpenAPI/Swagger specification) and test sandbox environment for integration development and testing
- **FR-066**: System MUST support controlled substance reclassification (e.g., moving substance from Opium Act List II to List I, or adding substance to precursor category) with effective date management. When substance is reclassified: (1) System records new classification with effective date, (2) System identifies all customers whose licence scope includes the substance, (3) System validates whether customer's existing licences authorize the new classification, (4) System flags affected customers as "Requires Re-Qualification" if existing licences insufficient, (5) System generates notification to compliance team listing affected customers and required actions, (6) System prevents new transactions with reclassified substance for affected customers until licences updated, (7) Historical transactions remain valid under classification at time of transaction. Reclassification audit trail records regulatory authority reference, effective date, substances affected, and customers flagged for re-qualification.

### Non-Functional Requirements - Security

- **NFR-001**: System MUST pass OWASP Top 10 vulnerability scanning with zero critical or high-severity findings before production deployment
- **NFR-002**: System MUST implement input validation on all API endpoints preventing SQL injection, XSS, command injection, and path traversal attacks per OWASP guidelines
- **NFR-003**: System MUST enforce HTTPS-only communication in production environments with TLS 1.2 minimum, rejecting all HTTP requests
- **NFR-004**: System MUST implement API rate limiting per integration system (default: 100 requests/minute per API key) to prevent denial-of-service attacks per FR-063
- **NFR-005**: System MUST perform automated security scanning (SAST/DAST) in CI/CD pipeline with build failure on high-severity vulnerabilities
- **NFR-006**: System MUST log all authentication failures, authorization denials, and API authentication attempts to Azure Application Insights with alerts on suspicious patterns (>10 failed attempts in 5 minutes from single IP)
- **NFR-007**: System MUST encrypt sensitive data at rest (licence documents in Blob Storage using Azure-managed keys) and in transit (API communication via HTTPS/TLS)


### Key Entities

- **Licence**: Represents a legal authorization (permit, exemption, certificate). Key attributes: type, number, issuing authority, issue date, expiry date, scope/restrictions, status (valid/expired/suspended/revoked), holder (company or customer), permitted activities (possess/store/distribute/import/export/manufacture/handle precursors), substance mappings. Relationships: belongs to one Holder (company or customer), has multiple Audit Events, linked to multiple Transactions, may have multiple attached Documents.

- **Licence Type**: Represents a category of legal authorization with defined rules. Key attributes: name, issuing authority, typical validity period, permitted activities, required for which business categories, required for which substance categories. Relationships: referenced by multiple Licences, defines requirements for Customer Roles.

- **Controlled Substance**: Represents a regulated drug or precursor. Key attributes: substance name, Opium Act list (I/II/none), precursor category, internal product code, regulatory restrictions. Relationships: mapped to multiple Licence Types (which licences authorize it), referenced in Transactions, linked to Thresholds.

- **Customer/Trading Partner**: Represents an entity qualified to purchase controlled drugs or provide GDP services. Key attributes: business name, registration number, business category (hospital pharmacy/community pharmacy/veterinarian/manufacturer/foreign wholesaler/research institution), approval status, onboarding date, suspension status, GDP qualification status (approved/conditionally approved/rejected/under review), next re-verification due date. Relationships: has multiple Licences, has multiple GDP Credentials, involved in multiple Transactions, has multiple Audit Events, subject to multiple Qualification Reviews.

- **Transaction**: Represents a compliance validation request/result for a sales order or transfer of controlled drugs (originated from external order management system). Key attributes: external transaction ID (from calling system), customer, date, substance categories involved, quantities, compliance check results (licences validated, permits verified), compliance status (pass/pending/failed), approved by (if override applied), cross-border indicator (domestic/EU/non-EU), calling system identity, external user context. Relationships: belongs to one Customer, validated against multiple Licences (customer and company), references multiple Controlled Substances, generates Audit Events, may have linked Compliance Override approval.

- **Audit Event**: Represents a compliance-related action, verification, or change. Key attributes: event type (inspection, verification, approval, suspension, licence change, GDP qualification), date, performed by (user), details, supporting evidence references, entity affected. Relationships: may be linked to Customer, may be linked to Licence, may be linked to GDP Site, may be linked to Transaction.

- **Threshold**: Represents quantity or frequency limits for suspicious-order monitoring. Key attributes: customer, substance, threshold type (monthly quantity/annual frequency), limit value, monitoring period. Relationships: belongs to one Customer, applies to one Controlled Substance, triggers Alerts when exceeded.

- **Alert/Notification**: Represents a compliance warning or reminder. Key attributes: alert type (expiry warning, expired licence, missing documentation, threshold exceeded, re-verification due), severity (critical/warning/info), target entity (customer or company), generated date, acknowledged status, acknowledged by. Relationships: may reference Customer, may reference Licence, may reference Threshold.

- **Workflow**: Represents an approval process for high-risk events. Key attributes: workflow type (add substance to customer scope, exception sale approval, new country addition, new GDP partner), initiator, initiated date, current status (pending/approved/rejected), required approvers (roles), approval history. Relationships: references entity being changed (Customer/Licence/Substance mapping), generates Audit Events.

- **GDP Site** (Composite Model): Represents a GDP-relevant physical location. Combines read-only D365 F&O warehouse master data (WarehouseId, DataAreaId, WarehouseName, OperationalSiteId, address fields from `Warehouses` OData entity) with GDP-specific extensions stored in Dataverse (`phr_gdpwarehouseextension`: GdpSiteType, PermittedActivities flags, IsGdpActive). Key attributes: warehouse ID + data area (composite key), GDP site type (warehouse/cross-dock/transport hub), permitted GDP activities (storage >72h/temperature-controlled/outsourced/transport-only), inspection history. Relationships: covered by one or more WDAs via GdpSiteWdaCoverage (keyed by WarehouseId+DataAreaId), has multiple Inspections, linked to SOPs, linked to Staff Training Records, used in Transactions.

- **GDP Credential**: Represents a partner's GDP compliance status. Key attributes: entity name, WDA number, GDP certificate number, EudraGMDP entry reference, validity period, qualification status, last verification date, next review due date. Relationships: belongs to one Trading Partner or Service Provider, has Verification Log entries, subject to Qualification Reviews.

- **GDP Credential Verification**: Represents a verification event for a partner's GDP status. Key attributes: verification ID, GDP credential reference, verification method (enum: EudraGMDP, NationalDatabase, DirectAuthorityContact, PhysicalDocumentReview), verification date, verifier name, outcome (Valid, Invalid, Inconclusive, Pending), notes, next verification due date. Relationships: belongs to one GdpCredential, creates Audit Event.

- **GDP Inspection**: Represents a regulatory or internal GDP audit. Key attributes: inspection date, inspector/authority (IGJ/NVWA/internal), site inspected, WDA inspected, findings (list), classification (critical/major/other), corrective actions required, inspection report reference. Relationships: applies to one GDP Site, references one WDA, generates multiple CAPAs, creates Audit Events.

- **CAPA (Corrective and Preventive Action)**: Represents an action to address a GDP deficiency. Key attributes: CAPA ID, linked finding (from inspection), description, owner (person responsible), due date, completion date, status (open/overdue/completed), verification notes. Relationships: originates from one GDP Inspection, assigned to one Owner (user), tracked in dashboards.

- **GDP Service Provider**: Represents 3PLs, transporters, external warehouses. Key attributes: provider name, service type (3PL/transporter/warehouse), GDP qualification status, temperature-controlled capability, approved routes, qualification review frequency, last review date, next review due date. Relationships: has GDP Credentials, subject to Qualification Reviews, selected in Transactions, may operate at GDP Sites.

- **GDP SOP**: Represents a documented procedure for GDP compliance. Key attributes: SOP number, title, category (returns/recalls/deviations/temperature excursions/outsourced activities), applicable sites, applicable activities, version, effective date. Relationships: linked to multiple GDP Sites, linked to multiple Activities, referenced in Training Records.

- **Training Record**: Represents completion of GDP training by staff. Key attributes: staff member name, role, training curriculum, completion date, expiry date (if applicable), trainer/assessor, assessment result. Relationships: linked to one Staff Member, linked to one or more GDP SOPs, linked to Staff Role, provides evidence for Inspections.

- **GDP Change Record**: Represents a change impacting GDP compliance. Key attributes: change ID, change type (new warehouse/new 3PL/new product type/storage condition change), description, risk assessment, approval status, approved by, implementation date, updated documentation references. Relationships: may reference GDP Sites, may reference Service Providers, generates Audit Events, triggers SOP updates or Training requirements.

- **Integration System**: Represents an external system authorized to call compliance APIs. Key attributes: system name, system type (ERP/order management/WMS/custom), authentication credentials (API key/OAuth client ID), authorized endpoints, IP whitelist (optional), active status, integration contact person. Relationships: originates Transaction validation requests, subject to API Audit Events, may have rate limiting policies applied.

Note: Complete catalog in data-model.md (see data-model.md for authoritative entity count)

## Success Criteria *(mandatory)*

### Measurable Outcomes

**Licence Management:**
- **SC-001**: Compliance officers can record a complete new licence (all required fields, activity mappings, substance mappings, and documentation) in under 5 minutes
- **SC-004**: System identifies 100% of licence expiries at least 60 days in advance with zero missed alerts

**Customer Onboarding:**
- **SC-002**: Sales administrators can complete customer onboarding qualification review (profile creation, licence recording, document attachment, verification recording) in under 15 minutes per customer
- **SC-003**: Time to verify a customer's licence via authority website and record verification is under 3 minutes

**Transaction Compliance:**
- **SC-005**: Pre-transaction compliance checks (licence validation, substance authorization, permit verification) complete in under 3 seconds at p95 (95th percentile), under 1 second at p50 (median), allowing seamless order processing
- **SC-006**: System correctly blocks 100% of non-compliant transactions (false negatives rate = 0% for missing/invalid licences)
- **SC-007**: System false-positive rate for compliant transactions is less than 1% (accounting for legitimate edge cases)
- **SC-008**: Reduction in compliance-related incidents (selling to unqualified customers, operating with expired licences, missing permits) by 95% within 6 months of implementation

**Reporting And Audit:**
- **SC-009**: During regulatory inspections, compliance managers can generate complete audit evidence for any customer or licence within 2 minutes
- **SC-010**: Time to respond to regulatory audit requests (gathering all relevant licence records, transaction evidence, verification histories) reduced by 80% compared to manual document gathering
- **SC-011**: 100% of trading relationships have complete, auditable qualification records including initial verification date, verifier name, method used, and ongoing monitoring history
- **SC-011a**: System detects 100% of concurrent modification conflicts with zero silent data overwrites; users can resolve conflicts within 30 seconds using the conflict resolution interface

**GDP Compliance:**
- **SC-012**: QA users can record a complete GDP site (location, WDA coverage, permitted activities, inspection history) in under 5 minutes
- **SC-013**: Operations users attempting to assign non-GDP-compliant sites or providers are blocked 100% of the time with clear explanation
- **SC-014**: Logistics planners can identify GDP-approved transport providers for a given shipment (including temperature-controlled requirements) in under 1 minute
- **SC-015**: Time to prepare for a GDP inspection (gathering site records, inspection history, CAPA status, training records, SOP index) reduced by 75% compared to manual processes
- **SC-016**: GDP Responsible Persons can complete initial qualification of a supplier or service provider (recording GDP credentials, EudraGMDP verification, audit review, approval decision) in under 20 minutes
- **SC-017**: System generates re-qualification reminders for 100% of partners due for review at least 90 days before review due date with zero missed reviews
- **SC-018**: Partner GDP status verification via EudraGMDP and recording takes under 3 minutes per partner
- **SC-019**: QA users can record a complete inspection (authority, site, findings, classifications) and create linked CAPAs (descriptions, owners, due dates) in under 15 minutes
- **SC-020**: QA managers can view current CAPA status (open, overdue, by site, by priority) via dashboard in under 30 seconds to support resource allocation decisions
- **SC-021**: Overdue CAPAs are highlighted to management within 24 hours of due date passing with zero missed escalations

**GDP Documentation & Training:**
- **SC-022**: Training coordinators can record training completion for distribution staff (staff name, curriculum, date, assessment result) in under 2 minutes per record
- **SC-023**: Changes impacting GDP (new site, new 3PL, new product type) are captured with risk assessment and approval workflow, with 100% requiring designated approver authorization before implementation
- **SC-024**: SOP index is complete and current, with links to all applicable sites and activities, enabling inspectors to confirm procedure coverage in under 5 minutes

**Usability:**
- **SC-025**: User satisfaction rating of at least 4.0/5.0 for ease of use in recording licences, managing customers, and generating reports
- **SC-026**: 95% of users successfully complete their primary tasks (record licence, qualify customer, approve order, generate report) on first attempt without support intervention
- **SC-027**: Regulatory inspection findings related to licence management or GDP compliance documentation deficiencies reduced by 90% within 12 months of implementation

**Availability & Reliability:**
- **SC-028**: System achieves 99.5% or higher uptime measured 24/7 monthly (maximum 4 hours combined planned and unplanned downtime per month); critical compliance functions (transaction validation, licence lookups) prioritized per FR-054 during degraded states
- **SC-029**: Planned maintenance windows account for less than 2 hours per month and are scheduled during off-peak hours (per Assumption 21: evenings 19:00-23:00, nights 23:00-07:00, weekends CET) with at least 48 hours advance notice
- **SC-030**: Mean time to recovery (MTTR) from unplanned outages is under 30 minutes for critical compliance functions (transaction validation, licence lookups)
- **SC-031**: System maintains transaction validation response times under 3 seconds even during peak load periods and partial component failures

**Integration And APIs:**
- **SC-032**: Transaction validation API responds within 3 seconds for 99% of requests under normal load (up to 50 concurrent validation requests)
- **SC-033**: Customer compliance status lookup API responds within 1 second for 99% of requests to support real-time customer selection in external order systems
- **SC-034**: API availability matches or exceeds system availability target (99.5%), ensuring external systems can rely on compliance checks
- **SC-035**: Zero data loss or corruption during API communication failures; all transaction validation results are persisted even if calling system fails to receive response (idempotent retry support)
- **SC-036**: Integration systems can be onboarded (credentials provisioned, API documentation provided, sandbox tested) within 2 business days
- **SC-037**: API versioning changes do not break existing integrations; backward compatibility maintained for minimum 6 months allowing gradual migration
- **SC-038**: Compliance officers can generate historical validation report showing all transactions potentially affected by licence data correction (where licence effective dates overlap transaction dates) within 5 minutes, with report indicating whether each transaction would have been compliant under corrected licence data, enabling manual impact assessment per Assumption 18

**Security:**
- **SC-039**: Zero critical or high-severity vulnerabilities detected in OWASP Top 10 security scans before production deployment
- **SC-040**: 100% of API endpoints pass input validation testing against OWASP injection attack patterns (SQL injection, XSS, command injection)
- **SC-041**: Automated security scanning (SAST with SonarQube or Checkmarx, DAST with OWASP ZAP) integrated in CI pipeline and executed on every commit
- **SC-042**: API rate limiting prevents >150 requests/minute from single integration system (50% margin above normal 100 req/min limit) with HTTP 429 response
- **SC-043**: All production communication uses HTTPS with TLS 1.2+ verified via SSL Labs scan scoring A- or higher


## Assumptions

1. **Regulatory Framework**: System assumes compliance with Dutch Opium Act, Medicines Act (Geneesmiddelenwet), EU GDP guidelines (2013/C 68/01), and relevant precursor regulations as of 2026. Any regulatory changes will require specification and implementation updates.

2. **Business Categories**: Standard Dutch pharmaceutical business categories include: hospital pharmacy, community pharmacy, veterinarian practice, research institution, wholesale distributor, manufacturer, foreign wholesaler (EU), foreign wholesaler (non-EU). Detailed mapping of required licence types per category will be confirmed during clarification or planning phase.

3. **Controlled Substance Categories**: Controlled substances are categorized according to: Opium Act List I (high-risk narcotics), Opium Act List II (moderate-risk narcotics), Precursor Categories (as per EU Regulation 273/2004 and 111/2005), and potentially other substance schedules. Specific licence requirements per category will be confirmed during clarification or planning phase.

4. **Cross-Border Transaction Types**: System distinguishes three transaction types with different licence/permit requirements: (1) Domestic Netherlands transactions (require domestic WDA, pharmacy/hospital licence, Opium Act exemptions), (2) EU cross-border transactions (require WDA, mutual recognition of EU licences, possible export notifications), (3) Non-EU international transactions (require import/export permits, potentially additional documentation). Detailed differences will be confirmed during clarification or planning phase.

5. **Licence Verification Methods**: System assumes licence authenticity and validity are verified by authorized personnel through: checking official authority websites (IGJ for pharmacies/wholesalers, Farmatec database for certain permits), email/phone confirmation with issuing authorities, reviewing physical documents (stamps, signatures, watermarks). The system records verification results but does not perform direct API integration with government databases (such integration may be future enhancement).

6. **User Roles & Permissions**: System assumes primary user roles include: Compliance Manager/Officer (manage all licences and compliance data), Sales Administrator (customer onboarding and qualification), Sales Processor (order entry and transaction processing), QA Manager/User (GDP management, inspections, CAPAs, training), GDP Responsible Person (partner qualification, GDP oversight), Operations/Warehouse User (site and distribution operations), Logistics Planner (route and provider selection), Training Coordinator (training records), System Administrator (workflows, access control, configuration). System supports hybrid authentication with both enterprise SSO (SAML/OAuth2/OIDC for internal staff) and local credentials (for external partners/auditors). Detailed permission matrices will be defined during planning phase.

7. **Document Storage**: System assumes supporting documentation (scanned licences, permits, certificates, inspection reports) is stored with references in licence/GDP records. The actual storage mechanism (local filesystem, cloud storage like Azure Blob or S3, document management system integration) will be determined during implementation planning. System must support common formats: PDF, JPEG, PNG, TIFF for scanned documents.

8. **Alert & Notification Delivery**: System assumes alerts are delivered via: in-system notifications (visible in user dashboards and notification centers), email notifications (to individual users and/or distribution groups). Default expiry alert thresholds are 90/60/30 days for own licences and 60/30 days for customer licences, but these thresholds are configurable per licence type. SMS or other notification channels are out of scope for initial implementation.

9. **Transaction Scope & Integration Architecture**: System operates as integration layer providing compliance validation for transactions initiated in external order management systems (ERP, custom order entry, WMS). "Transaction" in this context refers to compliance validation requests for sales orders, transfers, and cross-border shipments of controlled drugs and precursors. External systems retain full order management functionality (customer order entry UI, pricing, inventory allocation, shipment scheduling); this system provides only compliance checking APIs and compliance user interfaces for licence/GDP management and override approvals. The following transaction types are out of scope for initial implementation: returns from customers, destruction of expired/damaged products, internal inventory movements not involving legal entity changes, samples and donations (unless specifically regulated).

10. **Compliance Monitoring Frequency**: System performs automated monitoring jobs on the following schedule: daily checks for licence expiries and status changes, weekly generation of compliance summary reports (upcoming expiries, suspended customers), monthly generation of transaction audit reports (substances distributed, licences used), ad-hoc reports on demand. Exact scheduling and report distribution will be configured during implementation.

11. **Data Retention**: System assumes indefinite retention of all licence, GDP, audit, and transaction records for regulatory compliance. This follows standard pharmaceutical industry practices (minimum 5 years for most records per GDP guidelines, typically 10+ years for controlled substance records per Opium Act requirements, often longer for legal defensibility). Data archival strategy (move old data to archive storage while maintaining accessibility) will be defined during implementation planning.

12. **GDP Compliance Scope**: System tracks GDP certification status, site qualifications, partner qualifications, inspection results, and CAPA management. However, detailed GDP operational controls (real-time temperature monitoring, transportation validation, deviation management workflows, product recall execution) are assumed to be managed through separate quality management systems (QMS) or warehouse management systems (WMS). Integration points with such systems may be defined during planning phase.

13. **EudraGMDP Integration**: System assumes manual verification of partner GDP status via EudraGMDP database (user accesses website, searches for entity, records verification outcome). Automated API integration with EudraGMDP is out of scope for initial implementation but may be future enhancement. System logs verification date, verifier, and outcome for audit purposes.

14. **Training Requirements**: System records completion of GDP-specific training for distribution staff. However, the learning management system (LMS) where training is delivered, training content creation, and assessment management are assumed to be handled by separate HR/training systems. System may integrate with LMS to import completion records or may require manual entry of completion data.

15. **Threshold Monitoring for Suspicious Orders**: System supports defining quantity/frequency thresholds per customer and substance to flag abnormal order patterns. Threshold values are configured by compliance managers based on historical patterns, customer type, and regulatory guidance. When thresholds are exceeded, orders are flagged for review but not automatically blocked (to avoid disrupting legitimate increased demand). Suspicious order reporting to authorities (as required by Opium Act) is assumed to be a manual process performed by compliance officers after reviewing flagged orders.

16. **Licence Grace Periods**: Some regulatory authorities provide grace periods during licence renewal processing, where entities may continue operating under expired licences while renewal is pending. System supports recording such grace periods with start/end dates, and compliance checks account for grace period status. However, identification of which licences qualify for grace periods and grace period duration must be configured manually based on regulatory authority guidance.

17. **Multi-Entity Customers**: Some customers may operate multiple legal entities (e.g., hospital group with separate pharmacy licences per location). System supports recording separate customer profiles per legal entity with their respective licences. Relationship between entities (parent organization, shared ownership) may be recorded as notes but is not structurally modeled in initial implementation.

18. **Historical Data Correction**: If licence data is found to be incorrect after transactions have been completed, system supports correcting the licence record with effective dates to reflect when the correct information became known. Audit trail records both the original incorrect data and the correction. Impact assessment on past transactions (were they compliant under corrected data?) is performed manually by compliance officers; system does not automatically re-validate past transactions.

19. **Performance & Scalability**: System is designed to support a typical wholesale operation with: up to 10,000 customers/partners, up to 50 GDP sites, up to 100 substance categories, up to 100,000 transactions per year, up to 1,000 licences (customer + company), up to 500 GDP credentials (partners/providers). Performance targets (transaction check < 3 seconds, report generation < 2 minutes) assume this scale. Significantly larger operations may require performance optimization during implementation.

20. **Multilingual Support**: System assumes primary language is English with Dutch terminology for regulatory concepts (Opiumwet, Geneesmiddelenwet, GDP/VGP, WDA/WVG). User interface may support Dutch and English language options. Documents (scanned licences, certificates) may be in Dutch, English, or other languages depending on issuing authority. System does not translate document content.

21. **Business Hours & Availability**: System is designed for high availability (99.5% uptime) during business operations. "Business hours" are defined as Monday-Friday 07:00-19:00 CET/CEST (covers order entry, warehouse operations, compliance activities across European time zones). "Off-peak hours" for planned maintenance are evenings (19:00-23:00), nights (23:00-07:00), and weekends. Critical compliance functions (transaction validation, licence lookups, alert generation) must remain available during business hours; non-critical features (complex reports, analytics dashboards) may have brief maintenance windows. Emergency orders or 24/7 operations are supported but not the primary design target.

22. **Integration Systems & API Consumers**: System is designed to integrate with typical pharmaceutical wholesale ERP systems (SAP ECC/S4HANA, Microsoft Dynamics 365, Infor, custom Java/.NET systems) and warehouse management systems (Manhattan, Blue Yonder, SAP EWM, custom WMS). Integration patterns assume RESTful APIs with JSON payloads as primary integration mechanism. External systems are expected to make synchronous API calls during order entry/shipment workflows and handle "pending compliance" status by holding orders in their own databases until webhook notification or polling indicates approval. Batch integration patterns (nightly file exchange, bulk validation) are out of scope for initial implementation but may be future enhancement.

23. **D365 F&O Version Compatibility**: System integrates with Microsoft Dynamics 365 Finance & Operations version 10.0.30 or later via OData v4 virtual data entities. Minimum supported version: 10.0.30 (October 2022 release) which provides stable OData endpoint support, ETag-based concurrency control, and Managed Identity authentication. Older versions (10.0.x < 30) may work but are not tested. Cloud-hosted D365 F&O instances are primary target; on-premises deployments require network connectivity and certificate-based authentication configuration.
