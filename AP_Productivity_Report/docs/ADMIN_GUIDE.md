# Administrator Guide — AP Specialist Productivity Report

This guide is for IT administrators and power users who manage the AP Specialist Productivity Report after it has been deployed. It covers database connections, workflow configuration, access control, routine maintenance, and troubleshooting.

For initial deployment and installation, see [DEPLOYMENT.md](DEPLOYMENT.md).

---

## Table of Contents

- [Overview](#overview)
- [Accessing Settings](#accessing-settings)
- [Database Connections](#database-connections)
- [Workflow Configuration](#workflow-configuration)
- [Access Control](#access-control)
- [Routine Maintenance](#routine-maintenance)
- [Troubleshooting](#troubleshooting)

---

## Overview

The AP Specialist Productivity Report is a web-based reporting tool that displays invoice processing metrics per AP specialist. It connects to ECMT Workflow databases, reads invoice and approval data, and presents it in a filterable, sortable table with KPI summaries and CSV export.

As an administrator, you are responsible for:

- **Database connections** — Configuring and maintaining the SQL Server connections the application uses to retrieve data.
- **Workflow configuration** — Mapping workflow fields and approval queues so the report knows how to interpret each workflow's data.
- **Access control** — Determining which Active Directory groups can access the report.
- **Ongoing maintenance** — Updating the application, backing up configuration, and resolving issues as they arise.

All of these tasks are performed through the built-in Settings page. No code changes or server-side file editing is required for day-to-day administration.

---

## Accessing Settings

There are two ways to open the Settings page:

1. **From the report** — Click the gear icon in the top-right corner of the report header.
2. **By URL** — Browse directly to `/settings.html` on the application (e.g., `https://servername:8080/settings.html`).

On first launch, if no database connections have been configured, the report page automatically redirects you to Settings.

---

## Database Connections

The Settings page displays two connection cards side by side at the top: **Workflow Database** and **AppEnhancer Database**.

### Workflow Database (Required)

This is the primary connection. The application reads all workflow definitions, invoice data, approval steps, queue assignments, and user information from this database.

Fill in the following fields:

| Field | Description | Example |
|-------|-------------|---------|
| **Server** | SQL Server hostname or IP address | `sql-prod-01.ad.contoso.com` |
| **Database** | The ECMT Workflow database name | `ECMT_Workflow_v4` |
| **Username** | SQL Server login with read access | `reportuser` |
| **Password** | Password for the SQL login | (hidden after entry) |

### AppEnhancer Database (Optional)

This connection provides access to document index data. If your environment uses OpenText AppEnhancer for document storage, configuring this connection enables additional document-level features. If you do not use AppEnhancer, leave this section blank.

The fields are the same as above (Server, Database, Username, Password).

### Testing Connections

Each connection card has a **Test Connection** button. Always test before relying on a connection.

- Click **Test Connection** next to the appropriate card.
- A success message appears in green below the button if the connection is valid.
- An error message appears in red if the connection fails, with details about what went wrong.
- **A successful test automatically saves the connection.** You do not need to click a separate Save button after a successful test.

### Password Encryption

All database passwords are encrypted using **Windows DPAPI** (Data Protection API) before being stored on disk. This means:

- Passwords are encrypted using a key tied to the specific Windows account running the application (the IIS Application Pool identity, or the logged-in user for standalone deployments).
- The encrypted passwords can only be decrypted on the same machine, by the same Windows account.
- If the Application Pool identity changes, passwords must be re-entered (see [Routine Maintenance](#routine-maintenance)).
- Passwords are never transmitted or stored in plain text.

---

## Workflow Configuration

The Workflow Configuration section appears below the database connection cards after a successful Workflow Database connection. If you do not see this section, test your Workflow Database connection first.

### Selecting a Workflow

At the top of the Workflow Configuration section, a dropdown lists all workflows found in the database. Select the workflow you want to configure for reporting.

### Field Mappings

After selecting a workflow, the application reads all available fields from that workflow and presents them in dropdown menus. You need to map each report column to the correct workflow field.

| Report Field | Required | Description |
|---|---|---|
| **Vendor Name** | Yes | The vendor or supplier name |
| **Invoice Number** | Yes | The invoice identification number |
| **Amount** | Yes | The invoice dollar amount |
| **Invoice Date** | Yes | The date printed on the invoice |
| **DocID** | No | Links the invoice to an AppEnhancer document |
| **Workflow Status** | No | The current status text of the work item |
| **Workflow Queue** | No | The current queue the work item is in |

The application attempts to **auto-detect** the correct mapping based on field names. For example, a field named "Vendor_Name" would automatically map to the Vendor Name report column. Review these suggestions carefully and adjust any that are incorrect.

### Queue Role Mappings

Below the field mappings, each queue defined in the workflow is listed with a role dropdown. Assign a role to each queue:

| Role | Meaning | Typical Queue Names |
|---|---|---|
| **Not used** | This queue is not relevant to the report | Data entry, scanning, exceptions |
| **First Approval** | Site Manager or first-level approval step | "Site Manager Approval", "Manager Review" |
| **Second Approval** | Senior or AP-level approval step | "Senior Approval", "AP Approval" |

The application auto-detects likely approval queues based on name patterns (such as names containing "APPROVAL" or "SENIOR"). Review the suggestions and correct any that do not match your workflow's structure.

### Saving the Workflow Mapping

Click the **Save Workflow Mapping** button to store your field and queue role selections. A confirmation toast appears in the top-right corner when the save is successful.

### Configuring Multiple Workflows

If your organization uses more than one workflow for invoice processing, repeat the steps above for each workflow:

1. Select the next workflow from the dropdown.
2. Map its fields and queue roles.
3. Click **Save Workflow Mapping**.

Each workflow is saved independently. Only workflows that have been configured appear in the report's workflow selector dropdown.

### Removing a Workflow

To unconfigure a workflow and remove it from the report:

1. Select the workflow from the dropdown.
2. Click the **Remove Mapping** button (shown in red).
3. The workflow's field and queue mappings are deleted, and it no longer appears in the report dropdown.

### Workflow Selector Behavior in the Report

- If **multiple workflows** are configured, the report displays a workflow dropdown so users can choose which one to view.
- If **only one workflow** is configured, the dropdown is hidden and that workflow is used automatically.
- If **no workflows** are configured, the report cannot run and users are directed to Settings.

---

## Access Control

The Access Control section is at the bottom of the Settings page. It controls which users can access the report based on Active Directory group membership.

### Bootstrap Mode

When no AD groups have been configured, the application operates in **bootstrap mode**: all authenticated Windows users who can reach the site are granted access. This is the default state after a fresh installation, allowing you to complete initial setup without being locked out.

### Adding AD Groups

1. In the Access Control section, type at least **2 characters** in the search box.
2. The application searches Active Directory via LDAP and displays matching groups in a dropdown below the search box.
3. Click a group name to add it. The group appears as a tag with the group name displayed.
4. Repeat to add additional groups as needed.
5. Click **Save Access Control** to apply.

Once at least one group is saved, only users who belong to one of the configured groups can access the report. All other users see an "Access Denied" message.

### Removing AD Groups

To remove a group, click the **X** on its tag, then click **Save Access Control**.

If you remove all groups and save, the application returns to bootstrap mode (all authenticated users have access).

### Important Warning: Avoid Locking Yourself Out

When you add AD groups to the access list, make sure your own Windows account is a member of at least one of those groups. If you save a group list that does not include your account, you will be locked out of both the report and the Settings page.

**If you are locked out**, see the recovery procedure in the [Troubleshooting](#troubleshooting) section below.

---

## Routine Maintenance

### Updating the Application

When a new version of the application is available:

1. Obtain the new published files from your development team.
2. Stop the application:
   - **IIS**: Stop the Application Pool in IIS Manager, or place an `app_offline.htm` file in the site folder for a maintenance message.
   - **Standalone**: Close the console window or end the process.
3. Copy the new files into the application folder, overwriting existing files.
4. **Do not overwrite `connections.json`.** This file contains all your saved configuration (database connections, workflow mappings, and access control groups). If you accidentally overwrite it, you will need to reconfigure everything.
5. Restart the application:
   - **IIS**: Start the Application Pool, or delete the `app_offline.htm` file.
   - **Standalone**: Run the .exe again.

### Backing Up Configuration

The file `connections.json` (located in the application's root folder) contains all application configuration:

- Database connection details (with encrypted passwords)
- Workflow field and queue role mappings
- Active Directory group access list

**Back up this file regularly**, especially before application updates or server changes. While workflow mappings and group names are stored in plain text and are portable, encrypted passwords are tied to the current machine and service account and cannot be decrypted elsewhere.

### Application Pool Identity Changes

If the IIS Application Pool identity is changed (for example, switching from one service account to another):

1. Browse to the Settings page.
2. Re-enter the password for each database connection.
3. Click **Test Connection** to save.

This is necessary because DPAPI encryption is tied to the specific Windows account. The new account cannot decrypt passwords that were encrypted by the previous account. Workflow mappings and AD group settings are unaffected.

### Checking Access Logs

IIS logs all HTTP requests by default. These logs are located at:

```
%SystemDrive%\inetpub\logs\LogFiles\
```

When Windows Authentication is enabled, each log entry includes the authenticated username, the requested URL, timestamp, and response status code. Use these logs to:

- Audit who is accessing the report and when
- Identify failed access attempts (HTTP 401 or 403 responses)
- Monitor usage patterns

Configure log settings in IIS Manager under the **Logging** feature for the site.

---

## Troubleshooting

### Connection Failed on Test

| Possible Cause | Resolution |
|---|---|
| SQL Server hostname is incorrect or unreachable | Verify the server name and confirm it is reachable from the web server (try `ping` or `telnet servername 1433`) |
| SQL login credentials are wrong | Confirm the username and password by logging in with SQL Server Management Studio |
| TCP/IP is disabled on the SQL Server instance | Open SQL Server Configuration Manager and enable TCP/IP under Protocols |
| Firewall blocking port 1433 | Check Windows Firewall and any network firewalls between the web server and the database server |
| Database name is misspelled | Double-check the exact database name in SQL Server Management Studio |

### Locked Out of the Application

If you configured AD groups in Access Control and can no longer access the application:

1. Log on to the web server (via Remote Desktop or locally).
2. Navigate to the application folder (e.g., `C:\inetpub\APProductivityReport\`).
3. Delete the file `connections.json`.
4. Open IIS Manager and recycle the Application Pool for the site.
5. Browse to the application. It will return to first-run state (bootstrap mode), allowing all authenticated users to access it.
6. Reconfigure database connections, workflow mappings, and access control from scratch.

**Note:** Deleting `connections.json` removes all saved configuration. You will need to re-enter database passwords and re-map all workflows. Keep a backup of this file to minimize reconfiguration effort.

### No Data Returned in the Report

| Possible Cause | Resolution |
|---|---|
| No workflow is configured | Go to Settings and configure at least one workflow with field and queue mappings |
| Date range is too narrow | Select a wider date range preset such as "Year to Date" or enter a broader custom range |
| Wrong date basis selected | Try switching the Date Basis filter (e.g., from "Invoice Date" to "System Created Date") |
| Field mappings are incorrect | Go to Settings and verify that each field mapping points to the correct workflow field |
| Queue roles are not assigned | At least one queue must be mapped to "First Approval" or "Second Approval" for approval data to appear |
| The workflow has no work items | Confirm with your ECMT administrator that the workflow contains data |

### DPAPI Encryption Errors

Symptoms: The application fails to start, or connection tests fail immediately with a cryptographic error.

| Possible Cause | Resolution |
|---|---|
| Application Pool identity was changed | Re-enter all database passwords in Settings |
| `connections.json` was copied from another server | Re-enter passwords on the new server; workflow mappings and AD groups will still work |
| "Load User Profile" is set to False on the App Pool | In IIS Manager, open Application Pool Advanced Settings and set **Load User Profile** to **True** |

### Settings Page Not Loading

| Possible Cause | Resolution |
|---|---|
| Application is not running | Check that the IIS Application Pool is started, or that the standalone .exe is running |
| Browser is not sending Windows credentials | Verify that Windows Authentication is enabled and Anonymous Authentication is disabled in IIS |
| Hosted as IIS sub-application with URL issues | Ensure you are using the latest build, which includes automatic base-path detection for sub-applications |

### "Access Denied" for a User

1. Confirm that AD groups are configured in Settings > Access Control.
2. Verify that the user's Windows account is a member of at least one of those groups.
3. If no groups are configured, the application should allow all authenticated users. Check that Windows Authentication is enabled and Anonymous Authentication is disabled in IIS.
4. AD group membership changes may require the user to log off and back on (or close and reopen their browser) for updated group tokens to take effect.

### Application Pool Crashes Repeatedly

1. Check the Windows Event Log: open **Event Viewer**, navigate to **Windows Logs > Application**, and filter for source **IIS AspNetCore Module V2**.
2. Enable stdout logging for more detail: on the server, edit `web.config` in the application folder and set `stdoutLogEnabled="true"`. Create a `logs` folder in the application directory. Check the log files for error details.
3. Common causes include missing write permissions on the application folder, a corrupted `connections.json` file, or database connectivity issues during startup. Try deleting `connections.json` and recycling the Application Pool.

---

*This guide covers the AP Specialist Productivity Report application administration. For deployment and installation instructions, see [DEPLOYMENT.md](DEPLOYMENT.md). For a full technical reference including API endpoints and database schema, see [README.md](README.md).*
