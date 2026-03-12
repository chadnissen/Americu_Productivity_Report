# AP Specialist Productivity Report

A self-service reporting tool that displays invoice processing metrics per AP specialist, with line-level detail, step-level timing, SLA color coding, flexible date filtering, and CSV export.

Built on .NET 8 Minimal API with the EasyFile design system.

---

## Table of Contents

- [Features](#features)
- [Requirements](#requirements)
- [Deployment Options](#deployment-options)
- [Quick Start (Standalone)](#quick-start-standalone)
- [IIS Deployment](#iis-deployment)
- [First-Run Configuration](#first-run-configuration)
- [Using the Report](#using-the-report)
- [Architecture](#architecture)
- [Database Schema](#database-schema)
- [Troubleshooting](#troubleshooting)

---

## Features

- **Invoice Detail Table** — One row per invoice with 15 columns: AP Processor, Vendor, Invoice Date, Invoice #, Amount, System Entry Date, Site Mgr Approver/Date, Mgr Days, AP Approver/Date, AP Days, Total Days, Comments, Status
- **SLA Color Coding** — Green (0–3 days), Yellow (4–30 days), Red (31+ days) on all timing columns
- **KPI Summary Strip** — Total Invoices, Total Dollar Value, Unique Processors, Unique Vendors, Avg Processing Days
- **Flexible Date Filtering** — Presets (MTD, QTD, YTD, Last Month, Last Quarter) or custom date range
- **Date Basis Toggle** — Filter by System Created Date, Invoice Date, Site Mgr Approval Date, or AP Approval Date
- **Dynamic Workflow Mapping** — Configure field and queue mappings per workflow in the settings UI, no code changes needed
- **Smart Workflow Selector** — Only shows configured workflows; hidden entirely if only one is configured
- **Client-Side Filters** — Filter by Processor or Status (Completed/In Progress) without additional database queries
- **Windows Authentication** — SSO via Negotiate (Kerberos/NTLM), seamless for domain-joined machines
- **AD Group Access Control** — Restrict report access to specific Active Directory groups with LDAP browsing
- **Sortable Columns** — Click any column header to sort ascending/descending
- **Pagination** — 25 items per page with full navigation
- **CSV Export** — UTF-8 with BOM, Excel-compatible, exports all sorted data
- **Encrypted Configuration** — Database credentials encrypted with Windows DPAPI
- **Zero-Install Deployment** — Self-contained .exe requires no prerequisites on the target machine

---

## Requirements

### Standalone (.exe) Deployment
- Windows 10/11 or Windows Server 2016+
- Network access to the SQL Server databases
- **No .NET installation required** — the runtime is embedded in the .exe

### IIS Deployment
- Windows Server with IIS enabled
- [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) installed (includes ASP.NET Core Runtime + IIS module)
- Network access to the SQL Server databases

### Windows Authentication (Optional)
- Machine must be domain-joined (or Azure AD hybrid-joined) for SSO
- AD group-based access control requires the app to be able to query Active Directory (LDAP)
- If no AD groups are configured, all authenticated Windows users have access (bootstrap mode)

### Database Access
The application connects to up to two SQL Server databases:

| Connection | Purpose | Required? |
|------------|---------|-----------|
| **Workflow Database** | ECMT Workflow data — workflows, work items, instances, queues, users | Yes |
| **AppEnhancer Database** | Document index data — invoice metadata, vendor details | Optional |

A SQL login with read access to the Workflow database is required.

---

## Deployment Options

### Option 1: Standalone Executable (Recommended for evaluation)

No installation required. A single self-contained `.exe` that includes the .NET 8 runtime.

**To build:**
```
cd AP_Productivity_Report
dotnet publish -c Release -o ./publish
```

**Output:** `publish/AP_Productivity_Report.exe` (~100MB, self-contained)

**To deploy:** Copy the entire `publish/` folder to the target machine. No .NET install needed.

### Option 2: IIS (Recommended for production)

Requires the [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) on the server. Much smaller deployment (~5MB).

**Important:** IIS does not support single-file deployments. You must override the csproj defaults:

**To build:**
```
cd AP_Productivity_Report
dotnet publish -c Release -o ./publish-iis -p:PublishSingleFile=false -p:SelfContained=false
```

See [IIS Deployment](#iis-deployment) for setup instructions.

### Comparison

| | Standalone .exe | IIS |
|---|---|---|
| .NET install required | No | .NET 8 Hosting Bundle |
| Deployment size | ~100MB | ~5MB |
| Windows Auth | Supported | Supported |
| Best for | Evaluation, single-user | Production, multi-user |

---

## Quick Start (Standalone)

1. Copy the `publish/` folder to the target machine
2. Double-click `AP_Productivity_Report.exe`
3. Open a browser to `http://localhost:5000`
4. On first run, you are redirected to configure database connections
5. Enter credentials, click **Test Connection** (auto-saves on success)
6. Configure workflow field and queue mappings
7. The report loads automatically

---

## IIS Deployment

### Prerequisites
1. Install the [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) on the server
2. Enable IIS with the ASP.NET Core Module

### Steps

1. **Publish** the application (single-file must be disabled for IIS):
   ```
   dotnet publish -c Release -o ./publish-iis -p:PublishSingleFile=false -p:SelfContained=false
   ```

2. **Copy** the `publish-iis/` folder to the IIS server (e.g., `C:\inetpub\APProductivityReport\`)

3. **Create an IIS Site:**
   - Open IIS Manager
   - Right-click **Sites** > **Add Website**
   - Site name: `AP Productivity Report`
   - Physical path: `C:\inetpub\APProductivityReport`
   - Port: choose an available port (e.g., 8080)
   - Application Pool: select or create a pool set to **No Managed Code**

4. **Configure the Application Pool:**
   - Set **.NET CLR Version** to **No Managed Code**
   - Set **Identity** to a service account that has network access to the SQL Server databases

5. **Enable Windows Authentication (recommended):**
   - In IIS Manager, select the site
   - Open **Authentication**
   - Enable **Windows Authentication**
   - Disable **Anonymous Authentication**

6. **Browse** to `http://servername:8080` and configure database connections

### Notes
- The `web.config` file included in the publish output configures the ASP.NET Core Module automatically
- Database passwords are encrypted using Windows DPAPI and are tied to the user account running the application pool
- If you change the application pool identity, you will need to re-enter database passwords
- Windows Auth SSO works automatically for users on the same domain — no separate login needed

---

## First-Run Configuration

On first launch (or by clicking the gear icon in the report header), you will see the **Settings** page.

### 1. Database Connections

#### Workflow Database (Required)
| Field | Description | Example |
|-------|-------------|---------|
| Server | SQL Server hostname or IP | `sql-prod-01.ad.contoso.com` |
| Database | Workflow database name | `ECMT_Workflow_v4` |
| Username | SQL login username | `reportuser` |
| Password | SQL login password | `••••••••` |

#### AppEnhancer Database (Optional)
| Field | Description | Example |
|-------|-------------|---------|
| Server | SQL Server hostname or IP | `sql-prod-01.ad.contoso.com` |
| Database | AppEnhancer database name | `AppEnhancerProd` |
| Username | SQL login username | `reportuser` |
| Password | SQL login password | `••••••••` |

Click **Test Connection** to verify connectivity. A successful test auto-saves the connection settings. Passwords are encrypted using Windows DPAPI and stored in `connections.json` alongside the application.

### 2. Workflow Configuration

After a successful Workflow DB connection, the **Workflow Configuration** section appears.

#### Field Mappings
Select a workflow from the dropdown. The application reads all available fields from `wf_WorkflowFields` and presents them in dropdowns. Map each report field to the correct workflow column:

| Report Field | Required? | Description |
|---|---|---|
| Vendor Name | Yes | The vendor/supplier name field |
| Invoice Number | Yes | The invoice number field |
| Amount | Yes | The invoice dollar amount field |
| Invoice Date | Yes | The date on the invoice |
| DocID | No | Links to AppEnhancer document |
| Workflow Status | No | Current status text |
| Workflow Queue | No | Current queue name |

The application auto-detects likely matches based on field names. Review and adjust as needed.

#### Queue Role Mappings
Each queue in the workflow is listed with a role dropdown:

| Role | Meaning |
|---|---|
| Not used | Queue is not relevant to this report |
| First Approval | Site Manager / first-level approval step |
| Second Approval | Senior / AP approval step |

The application auto-detects approval queues by name patterns (e.g., "APPROVAL", "SENIOR").

Click **Save Workflow Mapping** to store. Use **Remove Mapping** to unconfigure a workflow.

### 3. Access Control (Optional)

Configure which AD groups can access the report:

- Type to search Active Directory groups (LDAP lookup, 2+ characters)
- Click a result to add the group
- Click X on a tag to remove a group
- Click **Save Access Control**

**Bootstrap mode:** When no AD groups are configured, all authenticated Windows users have access. Once at least one group is added, only members of those groups can access the report.

---

## Using the Report

### Filter Bar

| Control | Description |
|---------|-------------|
| **Workflow** | Select which workflow to report on. Only configured workflows appear. Hidden if only one is configured. |
| **Preset** | Quick date range selection: Month to Date, Quarter to Date, Year to Date, Last Month, Last Quarter, or Custom. |
| **Start Date / End Date** | Custom date range. Automatically set by presets. |
| **Date Basis** | Filter by **System Created Date**, **Invoice Date**, **Site Mgr Approval Date**, or **AP Approval Date**. |
| **Processor** | Client-side filter — select a specific AP specialist (no database round-trip). |
| **Status** | Client-side filter — All, Completed, or In Progress (no database round-trip). |

Click **Run Report** to execute the query and load results. Filter selections are saved in localStorage and restored on next visit.

### KPI Summary Strip

Five cards showing aggregate metrics for the filtered results:

| KPI | Description |
|-----|-------------|
| **Total Invoices** | Count of invoices in the filtered date range |
| **Total Dollar Value** | Sum of all invoice amounts |
| **Unique Processors** | Number of distinct AP specialists who processed invoices |
| **Unique Vendors** | Number of distinct vendor names |
| **Avg Processing Days** | Average total days from system entry to final approval |

### Report Columns

| Column | Description |
|--------|-------------|
| AP Processor | The AP specialist assigned to the invoice (first workflow instance owner) |
| Vendor Name | Vendor name from the invoice |
| Invoice Date | Date on the invoice |
| Invoice # | Invoice number |
| Invoice Amount | Dollar amount of the invoice |
| System Entry | Date the invoice entered the workflow system |
| Site Mgr Approver | Name of the site manager who approved (or is assigned) |
| Site Mgr Date | Date the site manager approval was completed |
| Mgr Days | Business days the site manager approval step took |
| AP Approver | Name of the senior/AP approver |
| AP Approval Date | Date the AP approval was completed |
| AP Days | Business days the AP approval step took |
| Total Days | Total days from system entry to final approval (or today if in progress) |
| Comments | Notes from approval instances |
| Status | **Completed** (green) or **In Progress** (yellow) |

### SLA Color Coding

Timing columns (Mgr Days, AP Days, Total Days) are color-coded:

| Color | Range | Meaning |
|-------|-------|---------|
| Green | 0–3 days | Within SLA |
| Yellow | 4–30 days | Approaching or past SLA |
| Red | 31+ days | Significantly overdue |

### Sorting

Click any column header to sort. Click again to toggle between ascending and descending. The active sort column is highlighted with a blue arrow indicator.

### Pagination

Results are displayed 25 per page. Use the page navigation at the bottom to move between pages.

### CSV Export

Click **Export CSV** to download all filtered and sorted data as a CSV file. The file:
- Includes UTF-8 BOM for proper Excel handling
- Uses raw numeric values for amounts and day counts
- Names the file `AP_Productivity_Report_[start]_to_[end].csv`

---

## Architecture

```
┌─────────────────────────────────┐
│  Browser (index.html)           │
│  - EasyFile Design System       │
│  - Vanilla JavaScript           │
│  - Client-side sort/pagination  │
│  - Client-side filter/export    │
└──────────┬──────────────────────┘
           │ HTTP (JSON) + Negotiate Auth
┌──────────▼──────────────────────┐
│  .NET 8 Minimal API             │
│  - Windows Authentication       │
│  - AD Group Authorization       │
│  - DPAPI Password Encryption    │
│  - Dynamic Workflow Mapping     │
│  - /api/auth/me                 │
│  - /api/workflows               │
│  - /api/reports/ap-productivity │
│  - /api/settings                │
│  - /api/settings/workflow-*     │
│  - /api/settings/auth/*         │
│  - Static file serving          │
└──────┬──────────┬───────────────┘
       │          │
┌──────▼───┐ ┌───▼──────────┐
│ Workflow │ │ AppEnhancer  │
│    DB    │ │  DB (opt.)   │
│ (SQL)    │ │   (SQL)      │
└──────────┘ └──────────────┘
```

### Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | .NET 8 minimal API — all endpoints, SQL queries, auth, encryption, workflow mapping |
| `wwwroot/index.html` | Report page — filters, KPIs, table, sorting, pagination, CSV export |
| `wwwroot/settings.html` | Settings page — DB connections, workflow mapping, access control |
| `appsettings.json` | ASP.NET configuration (logging, etc.) |
| `web.config` | IIS hosting configuration (auto-generated on publish) |
| `connections.json` | Encrypted settings (DB credentials, workflow mappings, AD groups — created on first save, not in source control) |

### API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Serves the report page (checks authorization) |
| GET | `/settings.html` | Serves the settings page (checks authorization) |
| GET | `/api/auth/me` | Returns current user identity and display name |
| GET | `/api/settings` | Returns connection config (no passwords) and configured workflow list |
| POST | `/api/settings` | Saves encrypted connection settings |
| POST | `/api/settings/test` | Tests a database connection |
| GET | `/api/settings/auth` | Returns configured AD groups |
| POST | `/api/settings/auth` | Saves AD group access list |
| GET | `/api/settings/auth/browse-groups?q=` | LDAP search for AD groups |
| GET | `/api/workflows` | Returns all workflows from the database |
| GET | `/api/workflows/{id}/fields` | Returns field definitions for a workflow |
| GET | `/api/workflows/{id}/queues` | Returns queue definitions for a workflow |
| GET | `/api/settings/workflow-mapping/{id}` | Returns saved field/queue mapping for a workflow |
| POST | `/api/settings/workflow-mapping/{id}` | Saves field/queue mapping for a workflow |
| DELETE | `/api/settings/workflow-mapping/{id}` | Removes a workflow mapping |
| GET | `/api/reports/ap-productivity` | Main report query with date, workflow, and date-basis filters |

---

## Database Schema

### Workflow Database Tables Used

| Table | Description |
|-------|-------------|
| `wf_Workflows` | Workflow definitions — ID, name, integration settings |
| `wf_WorkflowFields` | Field definitions per workflow — field name, column mapping |
| `wf_WorkItems_{id}` | Work items per workflow — invoice fields, timestamps, completion |
| `wf_WorkInstances_{id}` | Queue step instances — owner, start/end times, notes |
| `wf_Queues` | Queue definitions — names, workflow paths |
| `wf_Users` | User accounts — ID, username, full name |

### Dynamic Field Mappings

Field-to-column mappings are configured per workflow in the settings UI. The application reads available fields from `wf_WorkflowFields` and lets the administrator map them to report columns. No hardcoded field assumptions.

### Dynamic Queue Mappings

Queue roles (First Approval, Second Approval) are configured per workflow in the settings UI. The application reads available queues from `wf_Queues` and lets the administrator assign roles. No hardcoded queue ID assumptions.

---

## Troubleshooting

### "Database connections not configured"
Visit `/settings.html` (click the gear icon) and enter your database credentials.

### "Connection failed" on Test Connection
- Verify the SQL Server hostname is reachable from this machine
- Check that the SQL login has access to the specified database
- Ensure TCP/IP is enabled on the SQL Server
- Check firewall rules (default SQL port: 1433)

### Passwords lost after changing IIS Application Pool identity
DPAPI encryption is tied to the Windows user account. If you change the app pool identity, revisit `/settings.html` and re-enter passwords.

### No data returned
- Verify the correct workflow is selected (or configured in settings)
- Try expanding the date range (select "Year to Date" or a wider custom range)
- Check that field and queue mappings are configured for the selected workflow
- Check that the workflow has work items in the `wf_WorkItems_{id}` table

### "Access Denied"
- Your Windows account is not a member of any configured AD group
- Ask an administrator to add your AD group in Settings > Access Control
- If no groups are configured (bootstrap mode), all authenticated users should have access

### Application won't start (standalone .exe)
- Ensure no other process is using port 5000
- Run from a command prompt to see error output: `AP_Productivity_Report.exe`
- Check that `wwwroot/` folder is present alongside the .exe

### Large "Total Dollar Value"
Test/demo databases may contain unrealistic invoice amounts. This is expected in non-production environments.

### CSV opens with garbled characters in Excel
The CSV includes a UTF-8 BOM. If Excel still shows incorrect characters, use **Data > From Text/CSV** import instead of double-clicking the file.
