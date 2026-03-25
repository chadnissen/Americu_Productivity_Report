# AP Specialist Productivity Report — Installation Guide

Welcome! This guide walks you through installing the AP Specialist Productivity Report application on a Windows machine. Whether you are setting it up for a quick evaluation or deploying it for a production team, you will find step-by-step instructions below.

---

## Table of Contents

1. [What You Need](#what-you-need)
2. [Option A: Standalone Installation](#option-a-standalone-installation-simplest)
3. [Option B: IIS Installation](#option-b-iis-installation-recommended-for-production)
4. [First-Time Setup](#first-time-setup)
5. [Updating the Application](#updating-the-application)
6. [Uninstalling](#uninstalling)
7. [Common Installation Issues](#common-installation-issues)

---

## What You Need

Before you begin, make sure you have the following:

### For a Standalone Installation (Option A)

- **A Windows computer** — Windows 10, Windows 11, or Windows Server 2016 or later. That's it. The application includes everything it needs to run; you do not need to install any additional software.
- **Network access to your SQL Server database(s)** — The machine running the application must be able to reach the SQL Server(s) that hold your ECMT Workflow data.

### For an IIS Installation (Option B)

- **Windows Server with IIS enabled** — IIS (Internet Information Services) is Microsoft's built-in web server. If it is not already enabled on your server, you can turn it on through Server Manager > Add Roles and Features > Web Server (IIS).
- **The .NET 8 Hosting Bundle** — This is a small installer from Microsoft that allows IIS to run the application. Download it here: [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0). Look for **"Hosting Bundle"** under the ASP.NET Core Runtime section. This is a one-time install — just run the installer and restart IIS (details below).
- **Network access to your SQL Server database(s)** — Same as above.

---

## Option A: Standalone Installation (Simplest)

This is the easiest way to get the application running. It is ideal for evaluation, demos, or single-user scenarios. No installation is required — you just copy files and run.

### Steps

1. **Copy the application folder** to the machine where you want to run it. You should have received a folder (often called `publish`) containing `AP_Productivity_Report.exe` and supporting files. Copy the entire folder to any location you like — for example, `C:\APProductivityReport\`.

2. **Run the application.** Double-click `AP_Productivity_Report.exe`. A console window will appear showing the application starting up.

3. **Open your web browser** and go to:
   ```
   http://localhost:5000
   ```
   You should see the application load.

4. **Complete the first-time setup** (see [First-Time Setup](#first-time-setup) below).

### Changing the Default Port

By default, the application listens on port 5000. If that port is already in use or you prefer a different one, open a Command Prompt in the application folder and run:

```
set ASPNETCORE_URLS=http://0.0.0.0:8080
AP_Productivity_Report.exe
```

Replace `8080` with whatever port number you prefer. Then open your browser to `http://localhost:8080` instead.

> **Note:** There is no install or uninstall process for the standalone version. To remove it, simply close the application and delete the folder.

---

## Option B: IIS Installation (Recommended for Production)

IIS is the recommended approach when multiple users will access the application over the network. IIS handles things like SSL certificates, Windows single sign-on, and running the application reliably as a background service.

### Step 1: Install the .NET 8 Hosting Bundle

Before IIS can run the application, you need to install a small component from Microsoft called the Hosting Bundle.

1. On your server, open a browser and go to [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Under **ASP.NET Core Runtime**, look for the **Hosting Bundle** download and run the installer.
3. Follow the prompts — the defaults are fine.
4. **After the installer finishes, restart IIS.** Open a Command Prompt as Administrator and run:
   ```
   iisreset
   ```
   Alternatively, you can restart the server entirely.

> **Important:** If you skip the IIS restart, the application will not work and you will see a 500.19 error.

### Step 2: Copy the Application Files to the Server

You should have received a set of application files built for IIS deployment (this is different from the standalone .exe — it will be a folder of smaller files including `.dll` files and a `web.config`).

1. Create a folder on the server for the application — for example: `C:\inetpub\APProductivityReport\`
2. Copy all the application files into that folder.

### Step 3: Create the Site or Application in IIS Manager

You have two options here. Choose the one that best fits your environment.

#### Option 1: Create a New IIS Website

Use this if the application will have its own URL or port (for example, `http://servername:8080`).

1. Open **IIS Manager** (search for "IIS" in the Start menu or run `inetmgr`).
2. In the left panel, right-click **Sites** and choose **Add Website**.
3. Fill in the fields:
   - **Site name:** `AP Productivity Report`
   - **Physical path:** Browse to the folder where you copied the files (e.g., `C:\inetpub\APProductivityReport`)
   - **Port:** Choose a port number (e.g., `8080` for HTTP, or `443` if you are configuring HTTPS)
4. Click **OK**.

#### Option 2: Add as a Sub-Application Under an Existing Site

Use this if you want the application to be accessible as a path under an existing website (for example, `https://servername/WorkflowReport`).

1. Open **IIS Manager**.
2. In the left panel, expand the site you want to add to (e.g., **Default Web Site**).
3. Right-click the site and choose **Add Application**.
4. Fill in the fields:
   - **Alias:** The path name you want (e.g., `WorkflowReport`)
   - **Physical path:** Browse to the folder where you copied the files
5. Click **OK**.

### Step 4: Configure the Application Pool

The Application Pool controls how IIS runs the application. This step is important.

1. In **IIS Manager**, click **Application Pools** in the left panel.
2. Find the pool associated with your site (it usually has the same name as the site you just created).
3. Click on the pool name, then click **Basic Settings** in the right panel.
4. Set **.NET CLR Version** to **"No Managed Code"**.
   - This tells IIS that the application manages its own .NET runtime — IIS does not need to load one.
5. Click **OK**.
6. With the pool still selected, click **Advanced Settings** in the right panel.
7. Scroll down to the **Process Model** section.
8. Set **Load User Profile** to **True**.

> **Important:** If "Load User Profile" is not set to True, the application will not be able to save database passwords securely. You will see a DPAPI encryption error.

### Step 5: Set Folder Permissions

The application needs permission to write to its own folder (to save its configuration file). Here is how to grant that permission:

1. Open **File Explorer** and navigate to the application folder (e.g., `C:\inetpub\APProductivityReport`).
2. **Right-click** the folder and choose **Properties**.
3. Go to the **Security** tab.
4. Click **Edit**, then click **Add**.
5. In the dialog that appears, **click the "Locations" button**. This is a critical step.

> **Important:** In the Locations dialog, select **the local server name** — not the domain name. Application Pool identities are local accounts, not domain accounts. If you search the domain, Windows will not find the account.

6. In the "Enter the object names" box, type:
   ```
   IIS AppPool\AP Productivity Report
   ```
   Replace `AP Productivity Report` with the actual name of your Application Pool.
7. Click **Check Names**. The name should become underlined, confirming it was found.
8. Click **OK** to add the account.
9. With the new account selected, check the **Modify** permission checkbox.
10. Click **OK** on all dialogs to apply.

### Step 6: Enable Windows Authentication

Windows Authentication allows users to sign in automatically using their Windows credentials — no separate login page needed.

1. In **IIS Manager**, select your site or application in the left panel.
2. In the center panel, double-click **Authentication** (under the IIS section).
3. Set **Windows Authentication** to **Enabled** (right-click it and choose Enable, or select it and click Enable in the right panel).
4. Set **Anonymous Authentication** to **Disabled**.

> **Note:** With Windows Authentication enabled, users on the network will be signed in automatically. Their Windows display name will appear in the report header.

### Step 7: Test It

1. Open a browser on the server (or from another machine on the network).
2. Navigate to your site URL — for example:
   - `http://servername:8080` (if you created a new website on port 8080)
   - `https://servername/WorkflowReport` (if you added a sub-application)
3. You should see the application load. Proceed to [First-Time Setup](#first-time-setup).

---

## First-Time Setup

When you open the application for the first time, you will see a banner indicating that database connections have not been configured yet. Click the gear icon or navigate to the Settings page to set up:

- **Database connections** — Enter your SQL Server details and test each connection.
- **Workflow mappings** — Select a workflow and map its fields so the report knows which columns to use.
- **Access control** (optional) — Restrict access to specific Active Directory groups.

For detailed instructions on configuring these settings, refer to the **Admin Guide** section of the [User Guide](USER_GUIDE.md) or the [Deployment Guide](DEPLOYMENT.md#first-run-configuration).

---

## Updating the Application

When a new version of the application is available, follow these steps to update.

### Standalone Update

1. **Close the application** by closing the console window.
2. **Replace the files** in the application folder with the new version.
3. **Restart** by double-clicking `AP_Productivity_Report.exe`.

### IIS Update

1. **Stop the Application Pool.** In IIS Manager, go to Application Pools, select the pool, and click **Stop**. Alternatively, you can place a file named `app_offline.htm` in the application folder — IIS will immediately take the application offline and show that page to users instead.

2. **Copy the new files** into the application folder, overwriting the existing ones.

> **Important:** Do **not** overwrite `connections.json`. This file contains your saved database connections, workflow mappings, and access control settings. If you accidentally overwrite it, you will need to reconfigure everything in Settings.

3. **Start the Application Pool** again (or delete the `app_offline.htm` file).

4. **Browse to the site** to verify the update was successful.

---

## Uninstalling

### Standalone

Simply delete the application folder. There is nothing else to clean up.

### IIS

1. **Remove the site or application.** In IIS Manager, right-click the site or application and choose **Remove**. This only removes the IIS configuration — it does not delete files.
2. **Delete the application folder** from the server (e.g., `C:\inetpub\APProductivityReport`).
3. **(Optional) Remove the Application Pool.** In IIS Manager, go to Application Pools, right-click the pool that was used by the application, and choose **Remove**. Only do this if the pool is not shared with other applications.

---

## Common Installation Issues

### Error 500.38 — "Failed to locate ASP.NET Core app"

**What it means:** The application files were built in "single-file" mode, which IIS cannot use. This happens if the standalone version was copied to IIS by mistake.

**How to fix it:** Make sure you are using the IIS-specific build of the application (the one with multiple `.dll` files and a `web.config`), not the standalone `.exe`. If you are building from source, the application must be published with single-file mode turned off.

---

### Error 500.19 — "Configuration Error"

**What it means:** IIS does not have the component it needs to run the application. Either the .NET 8 Hosting Bundle is not installed, or IIS was not restarted after installing it.

**How to fix it:**

1. Install the [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) if you have not already.
2. Restart IIS by running `iisreset` from an Administrator Command Prompt.

---

### DPAPI Encryption Error When Saving Settings

**What it means:** The application cannot securely encrypt database passwords because the "Load User Profile" setting is not enabled on the Application Pool.

**How to fix it:**

1. In IIS Manager, go to **Application Pools**.
2. Select the pool used by the application.
3. Click **Advanced Settings**.
4. Under **Process Model**, set **Load User Profile** to **True**.
5. Restart the Application Pool.

---

### "Access Denied" When Saving Settings or Connections

**What it means:** The application does not have permission to write files to its own folder. The folder permissions from [Step 5](#step-5-set-folder-permissions) are missing or incorrect.

**How to fix it:**

1. Go back to [Step 5](#step-5-set-folder-permissions) and follow the instructions to grant the Application Pool identity **Modify** permission on the application folder.
2. Double-check that you selected the **local server** (not the domain) in the Locations dialog when adding the account.

---

### 404 Error When Clicking the Settings (Gear) Icon

**What it means:** The application's internal links are not resolving correctly. This can happen with older builds when the application is hosted as a sub-application under an existing IIS site.

**How to fix it:** Make sure you are using the latest version of the application, which includes a fix for URL resolution under sub-applications. See [Updating the Application](#updating-the-application).

---

### Passwords Lost After Changing the Application Pool Identity

**What it means:** Database passwords are encrypted using a Windows security feature called DPAPI, which ties encrypted data to a specific Windows account. If you change which account the Application Pool runs under, previously saved passwords can no longer be decrypted.

**How to fix it:** Go to the Settings page and re-enter the database passwords. Everything else (workflow mappings, access control groups) will still be intact.

---

> **Need more help?** For detailed troubleshooting, logging configuration, and backup guidance, see the full [Deployment & Operations Guide](DEPLOYMENT.md).
