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
- **Date Basis Toggle** — Filter by System Created Date or Invoice Date
- **Workflow Selector** — Dropdown to switch between different workflows
- **Sortable Columns** — Click any column header to sort ascending/descending
- **Pagination** — 25 items per page with full navigation
- **CSV Export** — UTF-8 with BOM, Excel-compatible, exports all sorted data
- **Encrypted Configuration** — Database credentials encrypted with Windows DPAPI
- **Zero-Install Deployment** — Self-contained .exe or IIS-hosted

---

## Requirements

### Standalone (.exe) Deployment
- Windows 10/11 or Windows Server 2016+
- Network access to the SQL Server databases
- No additional software required

### IIS Deployment
- Windows Server with IIS enabled
- [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) installed
- Network access to the SQL Server databases

### Database Access
The application connects to two SQL Server databases:

| Connection | Purpose |
|------------|---------|
| **Workflow Database** | ECMT Workflow data — workflows, work items, instances, queues, users |
| **AppEnhancer Database** | Document index data — invoice metadata, vendor details |

A SQL login with read access to both databases is required.

---

## Deployment Options

### Option 1: Standalone Executable (Recommended for evaluation)

No installation required. A single self-contained `.exe` that includes the .NET runtime.

**To build:**
```
cd AP_Productivity_Report
publish.bat
```

**Output:** `publish/AP_Productivity_Report.exe` (~100MB, self-contained)

**To deploy:** Copy the entire `publish/` folder to the target machine.

### Option 2: IIS (Recommended for production)

Requires the .NET 8 Hosting Bundle on the server. Much smaller deployment (~5MB).

**To build:**
```
cd AP_Productivity_Report
dotnet publish -c Release -o ./publish-iis
```

See [IIS Deployment](#iis-deployment) for setup instructions.

---

## Quick Start (Standalone)

1. Copy the `publish/` folder to the target machine
2. Double-click `AP_Productivity_Report.exe`
3. A browser window opens to `http://localhost:5000`
4. On first run, you are prompted to configure database connections
5. Enter credentials, click **Test Connection**, then **Save**
6. The report loads automatically

---

## IIS Deployment

### Prerequisites
1. Install the [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) on the server
2. Enable IIS with the ASP.NET Core Module

### Steps

1. **Publish** the application:
   ```
   dotnet publish -c Release -o ./publish-iis
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

5. **Browse** to `http://servername:8080` and configure database connections

### Notes
- The `web.config` file included in the publish output configures the ASP.NET Core Module automatically
- Database passwords are encrypted using Windows DPAPI and are tied to the user account running the application pool
- If you change the application pool identity, you will need to re-enter database passwords

---

## First-Run Configuration

On first launch (or by clicking the gear icon in the report header), you will see the **Settings** page.

### Database Connections

Configure two connections:

#### Workflow Database
| Field | Description | Example |
|-------|-------------|---------|
| Server | SQL Server hostname or IP | `sql-prod-01.ad.contoso.com` |
| Database | Workflow database name | `ECMT_Workflow_v4` |
| Username | SQL login username | `reportuser` |
| Password | SQL login password | `••••••••` |

#### AppEnhancer Database
| Field | Description | Example |
|-------|-------------|---------|
| Server | SQL Server hostname or IP | `sql-prod-01.ad.contoso.com` |
| Database | AppEnhancer database name | `AppEnhancerProd` |
| Username | SQL login username | `reportuser` |
| Password | SQL login password | `••••••••` |

### Test Connection
Click **Test Connection** for each database to verify connectivity before saving. A green success message confirms the connection works.

### Save
Click **Save Settings** to encrypt and store the credentials. Passwords are encrypted using Windows DPAPI and stored in `connections.json` alongside the application. The encrypted passwords can only be decrypted on the same machine by the same user account.

---

## Using the Report

### Filter Bar

| Control | Description |
|---------|-------------|
| **Workflow** | Select which workflow to report on. Each workflow corresponds to a set of work items and approval queues. |
| **Preset** | Quick date range selection: Month to Date, Quarter to Date, Year to Date, Last Month, Last Quarter, or Custom. |
| **Start Date / End Date** | Custom date range. Automatically set by presets. |
| **Date Basis** | Filter by **System Created Date** (when the invoice entered the workflow) or **Invoice Date** (the date on the invoice). Default: System Created Date. |

Click **Run Report** to execute the query and load results.

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
└──────────┬──────────────────────┘
           │ HTTP (JSON)
┌──────────▼──────────────────────┐
│  .NET 8 Minimal API             │
│  - /api/workflows               │
│  - /api/reports/ap-productivity │
│  - /api/settings                │
│  - Static file serving          │
└──────┬──────────┬───────────────┘
       │          │
┌──────▼───┐ ┌───▼──────────┐
│ Workflow │ │ AppEnhancer  │
│    DB    │ │     DB       │
│ (SQL)    │ │   (SQL)      │
└──────────┘ └──────────────┘
```

### Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | .NET 8 minimal API — all endpoints, SQL queries, encryption |
| `wwwroot/index.html` | Report page — filters, KPIs, table, sorting, pagination, CSV export |
| `wwwroot/settings.html` | Database connection configuration page |
| `appsettings.json` | ASP.NET configuration (logging, etc.) |
| `publish.bat` | Build script for self-contained .exe |
| `web.config` | IIS hosting configuration (auto-generated on publish) |
| `connections.json` | Encrypted database credentials (created on first save, not in source control) |

### API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/` | Serves the report page |
| GET | `/settings.html` | Serves the settings page |
| GET | `/api/settings` | Returns connection config (no passwords) |
| POST | `/api/settings` | Saves encrypted connection settings |
| POST | `/api/settings/test` | Tests a database connection |
| GET | `/api/workflows` | Returns all workflows for the dropdown |
| GET | `/api/reports/ap-productivity` | Main report query with filters |

---

## Database Schema

### Workflow Database Tables Used

| Table | Description |
|-------|-------------|
| `wf_Workflows` | Workflow definitions — ID, name, integration settings |
| `wf_WorkItems_{id}` | Work items per workflow — invoice fields, timestamps, completion |
| `wf_WorkInstances_{id}` | Queue step instances — owner, start/end times, notes |
| `wf_Queues` | Queue definitions — names, workflow paths |
| `wf_Users` | User accounts — ID, username, full name |

### Key Field Mappings (Workflow 7 — AP DEMO)

| WorkItem Column | Field Name |
|-----------------|------------|
| f14 | Vendor Name |
| f15 | Invoice Number |
| f16 | Amount |
| f24 | Invoice Date |
| f11 | DocID (links to AppEnhancer) |
| f18 | Workflow Status |
| f19 | Workflow Queue |

### Queue Mappings (Workflow 7)

| QueueID | Queue Name |
|---------|------------|
| 4 | 10 - BATCH INDEXING |
| 5 | 15 - BATCH INDEX ROUTING |
| 0 | 20 - Invoice Review |
| 6 | 30 - INVOICE APPROVAL |
| 1 | 30 - APPROVAL |
| 2 | SENIOR APPROVER |
| 7 | 40 - APPROVED |

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
- Verify the correct workflow is selected in the dropdown
- Try expanding the date range (select "Year to Date" or a wider custom range)
- Check that the workflow has work items in the `wf_WorkItems_{id}` table

### Large "Total Dollar Value"
Test/demo databases may contain unrealistic invoice amounts. This is expected in non-production environments.

### Application won't start (standalone .exe)
- Ensure no other process is using port 5000
- Run from a command prompt to see error output: `AP_Productivity_Report.exe`
- Check that `wwwroot/` folder is present alongside the .exe

### CSV opens with garbled characters in Excel
The CSV includes a UTF-8 BOM. If Excel still shows incorrect characters, use **Data > From Text/CSV** import instead of double-clicking the file.
