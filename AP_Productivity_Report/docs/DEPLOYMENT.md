# Deployment & Operations Guide

## Table of Contents

- [Deployment Options](#deployment-options)
- [Standalone Deployment](#standalone-deployment)
- [IIS Deployment](#iis-deployment)
- [Windows Authentication](#windows-authentication)
- [First-Run Configuration](#first-run-configuration)
- [Updating the Application](#updating-the-application)
- [Backup & Recovery](#backup--recovery)
- [Auditing & Logging](#auditing--logging)
- [Troubleshooting](#troubleshooting)

---

## Deployment Options

| | Standalone (.exe) | IIS |
|---|---|---|
| **Prerequisites** | None — .NET runtime is embedded | [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Deployment size** | ~100 MB | ~5 MB |
| **Windows Auth** | Supported | Supported |
| **HTTPS** | Manual (reverse proxy or cert config) | IIS handles SSL binding |
| **Runs as** | Current logged-in user | IIS Application Pool identity |
| **Best for** | Evaluation, single-user, quick demos | Production, multi-user, client delivery |

---

## Standalone Deployment

### Prerequisites

- Windows 10/11 or Windows Server 2016+
- Network access to the SQL Server database(s)
- **No .NET installation required** — the runtime is embedded in the executable

### Build

```
cd AP_Productivity_Report
dotnet publish -c Release -o ./publish
```

Output: `publish/AP_Productivity_Report.exe` (~100 MB, self-contained)

### Deploy

1. Copy the entire `publish/` folder to the target machine
2. Double-click `AP_Productivity_Report.exe` (or run from command prompt)
3. Open a browser to `http://localhost:5000`
4. Complete [First-Run Configuration](#first-run-configuration)

### Notes

- The application listens on port 5000 by default
- To change the port, set the `ASPNETCORE_URLS` environment variable:
  ```
  set ASPNETCORE_URLS=http://0.0.0.0:8080
  AP_Productivity_Report.exe
  ```
- DPAPI-encrypted passwords are tied to the Windows user who runs the .exe
- To run as a Windows Service, use `sc create` or NSSM

---

## IIS Deployment

### Prerequisites

1. Windows Server with IIS enabled
2. Install the [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)
   - This includes the ASP.NET Core Runtime and the IIS hosting module (ANCM)
   - **Restart IIS after installing** (`iisreset` or restart the server)
3. Network access to the SQL Server database(s)

### Build

**Important:** IIS does not support single-file deployments. Override the project defaults:

```
cd AP_Productivity_Report
dotnet publish -c Release -o ./publish-iis -p:PublishSingleFile=false -p:SelfContained=false
```

### Deploy as IIS Site (root)

1. Copy `publish-iis/` contents to the server (e.g., `C:\inetpub\APProductivityReport\`)

2. Open **IIS Manager** → right-click **Sites** → **Add Website**:
   - Site name: `AP Productivity Report`
   - Physical path: `C:\inetpub\APProductivityReport`
   - Binding: choose port (e.g., 443 with SSL, or 8080 for HTTP)
   - Application Pool: select or create a pool

3. Configure the **Application Pool**:
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: **Integrated**
   - Identity: a service account with network access to SQL Server

4. Configure the **Application Pool Advanced Settings**:
   - Open IIS Manager → Application Pools → select the pool → **Advanced Settings**
   - Set **Load User Profile** to **True** (required for DPAPI password encryption)

5. Grant the Application Pool identity **write access** to the application folder:
   - Right-click the application folder (e.g., `C:\inetpub\APProductivityReport`) → **Properties** → **Security** tab
   - Click **Edit** → **Add**
   - **Important:** Click **Locations** and select the **local server name** (not the domain) — Application Pool identities are local accounts
   - Enter `IIS AppPool\AP Productivity Report` (use your Application Pool name)
   - Click **Check Names** to verify, then **OK**
   - Grant **Modify** permission
   - Click **OK** to apply

   Or via PowerShell:
   ```powershell
   icacls "C:\inetpub\APProductivityReport" /grant "IIS AppPool\AP Productivity Report:(OI)(CI)M"
   ```

6. Browse to `http://servername:port` and complete [First-Run Configuration](#first-run-configuration)

### Deploy as IIS Sub-Application

If adding to an existing IIS site (e.g., under Default Web Site):

1. Copy `publish-iis/` contents to the server (e.g., `C:\inetpub\wwwroot\WorkflowReport\`)

2. In **IIS Manager**, expand the site → right-click → **Add Application**:
   - Alias: `WorkflowReport`
   - Physical path: `C:\inetpub\wwwroot\WorkflowReport`
   - Application Pool: select or create a pool set to **No Managed Code**

3. Configure the **Application Pool Advanced Settings**:
   - Open IIS Manager → Application Pools → select the pool → **Advanced Settings**
   - Set **Load User Profile** to **True** (required for DPAPI password encryption)

4. Grant the Application Pool identity **write access** to the application folder:
   - Right-click `C:\inetpub\wwwroot\WorkflowReport` → **Properties** → **Security** tab
   - Click **Edit** → **Add**
   - **Important:** Click **Locations** and select the **local server name** (not the domain) — Application Pool identities are local accounts, not domain accounts
   - Enter `IIS AppPool\WorkflowReport` (use your Application Pool name)
   - Click **Check Names** to verify, then **OK**
   - Grant **Modify** permission
   - Click **OK** to apply

   Or via PowerShell:
   ```powershell
   icacls "C:\inetpub\wwwroot\WorkflowReport" /grant "IIS AppPool\WorkflowReport:(OI)(CI)M"
   ```

5. Browse to `https://servername/WorkflowReport`

**Note:** The application automatically detects its base path when hosted as a sub-application. All relative URLs (settings page, API calls) resolve correctly regardless of the application alias.

### Enable Windows Authentication in IIS

1. Select the site or application in IIS Manager
2. Open **Authentication**
3. **Enable** Windows Authentication
4. **Disable** Anonymous Authentication
5. Recycle the application pool

### IIS Configuration Notes

- The `web.config` in the publish output configures the ASP.NET Core Module (ANCM) automatically — do not modify it unless troubleshooting
- **Load User Profile** must be **True** on the Application Pool — DPAPI encryption will fail without it
- DPAPI-encrypted passwords are tied to the Application Pool identity. If you change the pool identity, re-enter passwords in Settings
- The `connections.json` file is created alongside the application on first save. It contains encrypted credentials and workflow mappings
- The Application Pool identity needs **Modify** permission on the application folder to write `connections.json`. Use **Locations → local server name** when adding the `IIS AppPool\<PoolName>` account

---

## Windows Authentication

### How It Works

The application uses **Negotiate authentication** (Kerberos with NTLM fallback). When a user browses to the report:

1. The browser sends Windows credentials automatically (SSO)
2. The server validates the identity against Active Directory
3. If AD group restrictions are configured, group membership is checked
4. The user's display name appears in the report header

### Requirements

- The client machine must be **domain-joined** or **Azure AD hybrid-joined**
- The browser must support Negotiate auth (all major browsers do)
- For Kerberos (preferred): the server needs an SPN registered for the service account
- For NTLM (fallback): works without SPN configuration

### Bootstrap Mode

When no AD groups are configured in Settings > Access Control, **all authenticated Windows users** have access. This allows initial setup without being locked out.

Once at least one AD group is added, only members of those groups can access the report.

### AD Group Configuration

1. Navigate to **Settings** (gear icon)
2. Scroll to **Access Control**
3. Type 2+ characters to search Active Directory groups (LDAP lookup)
4. Click a group to add it
5. Click **Save Access Control**

Groups are stored in `connections.json`. The LDAP search requires the application to run under an identity that can query Active Directory.

---

## First-Run Configuration

On first launch, the report page shows a configuration banner. Click through to Settings or navigate directly to `/settings.html`.

### Step 1: Database Connections

| Field | Description | Example |
|-------|-------------|---------|
| Server | SQL Server hostname or IP | `sql-prod-01.ad.contoso.com` |
| Database | Database name | `ECMT_Workflow_v4` |
| Username | SQL login | `reportuser` |
| Password | SQL login password | (encrypted on save) |

- **Workflow Database** (required) — ECMT Workflow data
- **AppEnhancer Database** (optional) — Document index data

Click **Test Connection** for each. A successful WF test automatically saves the connection and reveals the Workflow Configuration section.

### Step 2: Workflow Configuration

1. Select a workflow from the dropdown
2. **Map fields** — The application reads fields from `wf_WorkflowFields` and presents them in dropdowns. Four fields are required (Vendor Name, Invoice Number, Amount, Invoice Date). Three are optional (DocID, Status, Queue).
3. **Assign queue roles** — Each queue is listed with a role dropdown: Not used, First Approval (Site Manager), or Second Approval (Senior/AP). Auto-detection suggests roles based on queue names.
4. Click **Save Workflow Mapping**

Repeat for additional workflows if needed. Only configured workflows appear in the report dropdown. If only one workflow is configured, the dropdown is hidden entirely.

### Step 3: Access Control (Optional)

See [AD Group Configuration](#ad-group-configuration) above.

---

## Updating the Application

### Standalone

1. Stop the running .exe (close the console window or `taskkill`)
2. Replace the `publish/` folder contents with the new build
3. Restart the .exe

`connections.json` is preserved — no need to reconfigure.

### IIS

1. Build: `dotnet publish -c Release -o ./publish-iis -p:PublishSingleFile=false -p:SelfContained=false`
2. Stop the IIS Application Pool (or use `app_offline.htm` for zero-downtime)
3. Copy new `publish-iis/` contents to the site folder, overwriting existing files
4. **Do not overwrite `connections.json`** — it contains your saved configuration
5. Start the Application Pool

### Using app_offline.htm

To show a maintenance page during updates:

1. Create a file named `app_offline.htm` in the site root with a friendly message
2. IIS will immediately stop routing to the ASP.NET Core app and serve this file instead
3. Update the application files
4. Delete `app_offline.htm` to bring the app back online

---

## Backup & Recovery

### What to Back Up

| File | Contains | Critical? |
|------|----------|-----------|
| `connections.json` | Encrypted DB credentials, workflow mappings, AD group config | Yes |
| Application files | Published .dll/.exe and static files | No (rebuild from source) |

### Recovery

- If `connections.json` is lost, reconfigure via `/settings.html`. Passwords must be re-entered.
- If `connections.json` is moved to a different machine or user account, DPAPI decryption will fail — passwords must be re-entered. Workflow mappings and AD group names are stored in plain text and will survive the move.

---

## Auditing & Logging

### Current State

The application currently uses the default ASP.NET Core logging infrastructure (console/event log). There is no built-in audit trail for:

- User access (who viewed the report and when)
- Report execution (which queries were run, by whom, with what parameters)
- Settings changes (who modified connections, mappings, or access control)
- Failed access attempts (unauthorized users who were denied)

### Enabling stdout Logging

For troubleshooting, enable stdout logging by editing `web.config`:

```xml
<aspNetCore processPath=".\AP_Productivity_Report.exe"
            stdoutLogEnabled="true"
            stdoutLogFile=".\logs\stdout"
            hostingModel="inprocess" />
```

Create the `logs/` directory first. This captures application startup errors and request-level logging. **Disable in production** — these logs grow quickly and are not rotated.

### IIS Request Logging

IIS logs all HTTP requests by default to `%SystemDrive%\inetpub\logs\LogFiles\`. These logs include:

- Timestamp, client IP, username (when Windows Auth is enabled)
- Requested URL and HTTP method
- Response status code

This provides a basic access audit trail at the web server level. Configure in IIS Manager under **Logging**.

### Windows Event Log

ASP.NET Core application errors are written to the Windows Application Event Log under the source `IIS AspNetCore Module V2`. Check Event Viewer for startup failures or unhandled exceptions.

### Future Enhancement

A dedicated audit log feature could be added to track:
- User login events with timestamps
- Report query execution with parameters
- Settings modifications with before/after values
- Access denied events

---

## Troubleshooting

### HTTP Error 500.38 — Failed to locate ASP.NET Core app

**Cause:** The application was published as a single-file executable, which IIS cannot load in-process.

**Fix:** Republish with single-file disabled:
```
dotnet publish -c Release -o ./publish-iis -p:PublishSingleFile=false -p:SelfContained=false
```

### HTTP Error 500.19 — Configuration Error

**Cause:** The .NET 8 Hosting Bundle is not installed, or IIS was not restarted after installation.

**Fix:** Install the [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) and run `iisreset`.

### HTTP Error 502.5 — Process Failure

**Cause:** The application crashed on startup. Common reasons:
- Missing `connections.json` write permissions
- Port conflict (standalone only)
- Missing dependency

**Fix:** Enable stdout logging (see above) and check the `logs/` folder for details.

### HTTP Error 404 — Settings page not found when clicking gear icon

**Cause:** The application is hosted as an IIS sub-application (e.g., `/WorkflowReport`) and relative URLs are not resolving correctly.

**Fix:** Ensure you are using the latest build, which includes a `<base>` tag for correct URL resolution under sub-applications.

### "Database connections not configured"

Visit Settings (gear icon) and enter database credentials.

### "Connection failed" on Test Connection

- Verify the SQL Server hostname is reachable from the server
- Check that the SQL login has access to the specified database
- Ensure TCP/IP is enabled on the SQL Server instance
- Check firewall rules (default SQL port: 1433)

### Passwords lost after changing Application Pool identity

DPAPI encryption is tied to the Windows user account. Re-enter passwords in Settings.

### No data returned in the report

- Check that workflow field and queue mappings are configured in Settings
- Expand the date range (try "Year to Date" or a wider custom range)
- Verify the workflow has work items in the database

### "Access Denied" page

- Your Windows account is not a member of any configured AD group
- Ask an administrator to add your group in Settings > Access Control
- If no groups are configured (bootstrap mode), all authenticated users should have access — check that Windows Auth is enabled and Anonymous Auth is disabled in IIS

### Locked out by AD group restrictions

If you configured AD groups in Access Control and can no longer access the application:

1. On the server, delete `connections.json` from the application folder:
   ```
   del C:\inetpub\wwwroot\WorkflowReport\connections.json
   ```
2. Recycle the Application Pool in IIS Manager
3. Browse to the application — it returns to first-run state (bootstrap mode: all authenticated users have access)
4. Reconfigure database connections, workflow mappings, and access control

**Note:** You will need to re-enter all settings including database passwords, since they are stored in `connections.json`.

### Application Pool crashes repeatedly

Check the Windows Event Log (Event Viewer > Application) for `IIS AspNetCore Module V2` errors. Common causes:
- Application folder permissions (pool identity needs read + write)
- Database connectivity issues during startup
- Corrupted `connections.json` — delete it and reconfigure
