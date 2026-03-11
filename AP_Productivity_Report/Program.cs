using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5000
builder.WebHost.UseUrls("http://0.0.0.0:5000");

var app = builder.Build();

// Serve static files from wwwroot
app.UseStaticFiles();

// ── Paths ────────────────────────────────────────────────────────────────────
var appDir = AppContext.BaseDirectory;
var connectionsFile = Path.Combine(appDir, "connections.json");

// ── JSON options ─────────────────────────────────────────────────────────────
var jsonOpts = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true
};

// ── Models ───────────────────────────────────────────────────────────────────
ConnectionSettings? cachedSettings = null;

ConnectionSettings? LoadSettings()
{
    if (cachedSettings != null) return cachedSettings;
    if (!File.Exists(connectionsFile)) return null;
    try
    {
        var json = File.ReadAllText(connectionsFile);
        cachedSettings = JsonSerializer.Deserialize<ConnectionSettings>(json, jsonOpts);
        return cachedSettings;
    }
    catch
    {
        return null;
    }
}

string DecryptPassword(string encryptedBase64)
{
    var encryptedBytes = Convert.FromBase64String(encryptedBase64);
    var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
    return Encoding.UTF8.GetString(decryptedBytes);
}

string EncryptPassword(string plainText)
{
    var plainBytes = Encoding.UTF8.GetBytes(plainText);
    var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
    return Convert.ToBase64String(encryptedBytes);
}

string BuildConnectionString(DbConnectionInfo info, string? plainPassword = null)
{
    var password = plainPassword ?? DecryptPassword(info.EncryptedPassword ?? "");
    return new SqlConnectionStringBuilder
    {
        DataSource = info.Server,
        InitialCatalog = info.Database,
        UserID = info.Username,
        Password = password,
        TrustServerCertificate = true,
        ConnectTimeout = 10
    }.ConnectionString;
}

SqlConnection GetWfConnection()
{
    var settings = LoadSettings();
    if (settings?.Workflow == null)
        throw new InvalidOperationException("Database connections not configured. Visit /settings.html to set up.");
    var conn = new SqlConnection(BuildConnectionString(settings.Workflow));
    conn.Open();
    return conn;
}

// ── Routes ───────────────────────────────────────────────────────────────────

// GET / — Serve index.html
app.MapGet("/", () => Results.File(
    Path.Combine(app.Environment.WebRootPath, "index.html"),
    "text/html"));

// GET /settings.html — Serve settings page
app.MapGet("/settings.html", () => Results.File(
    Path.Combine(app.Environment.WebRootPath, "settings.html"),
    "text/html"));

// GET /api/settings — Return current settings (no passwords)
app.MapGet("/api/settings", () =>
{
    var settings = LoadSettings();
    if (settings == null)
    {
        return Results.Json(new
        {
            configured = false,
            workflow = (object?)null,
            appEnhancer = (object?)null
        }, jsonOpts);
    }

    return Results.Json(new
    {
        configured = true,
        workflow = settings.Workflow == null ? null : new
        {
            server = settings.Workflow.Server,
            database = settings.Workflow.Database,
            username = settings.Workflow.Username,
            hasPassword = !string.IsNullOrEmpty(settings.Workflow.EncryptedPassword)
        },
        appEnhancer = settings.AppEnhancer == null ? null : new
        {
            server = settings.AppEnhancer.Server,
            database = settings.AppEnhancer.Database,
            username = settings.AppEnhancer.Username,
            hasPassword = !string.IsNullOrEmpty(settings.AppEnhancer.EncryptedPassword)
        }
    }, jsonOpts);
});

// POST /api/settings — Save connection settings
app.MapPost("/api/settings", async (HttpRequest request) =>
{
    try
    {
        var body = await JsonSerializer.DeserializeAsync<SaveSettingsRequest>(request.Body, jsonOpts);
        if (body == null)
            return Results.BadRequest(new { error = "Invalid request body" });

        // Load existing settings to preserve passwords if not changed
        var existing = LoadSettings();

        var settings = new ConnectionSettings
        {
            Workflow = new DbConnectionInfo
            {
                Server = body.Workflow?.Server ?? "",
                Database = body.Workflow?.Database ?? "",
                Username = body.Workflow?.Username ?? "",
                EncryptedPassword = !string.IsNullOrEmpty(body.Workflow?.Password)
                    ? EncryptPassword(body.Workflow!.Password!)
                    : existing?.Workflow?.EncryptedPassword ?? ""
            },
            AppEnhancer = new DbConnectionInfo
            {
                Server = body.AppEnhancer?.Server ?? "",
                Database = body.AppEnhancer?.Database ?? "",
                Username = body.AppEnhancer?.Username ?? "",
                EncryptedPassword = !string.IsNullOrEmpty(body.AppEnhancer?.Password)
                    ? EncryptPassword(body.AppEnhancer!.Password!)
                    : existing?.AppEnhancer?.EncryptedPassword ?? ""
            }
        };

        var json = JsonSerializer.Serialize(settings, jsonOpts);
        await File.WriteAllTextAsync(connectionsFile, json);
        cachedSettings = settings;

        return Results.Json(new { success = true, message = "Settings saved successfully." }, jsonOpts);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
});

// POST /api/settings/test — Test a database connection
app.MapPost("/api/settings/test", async (HttpRequest request) =>
{
    try
    {
        var body = await JsonSerializer.DeserializeAsync<TestConnectionRequest>(request.Body, jsonOpts);
        if (body == null)
            return Results.BadRequest(new { error = "Invalid request body" });

        var connStr = new SqlConnectionStringBuilder
        {
            DataSource = body.Server,
            InitialCatalog = body.Database,
            UserID = body.Username,
            Password = body.Password,
            TrustServerCertificate = true,
            ConnectTimeout = 10
        }.ConnectionString;

        using var conn = new SqlConnection(connStr);
        await conn.OpenAsync();
        await conn.CloseAsync();

        return Results.Json(new { success = true, message = "Connection successful!" }, jsonOpts);
    }
    catch (SqlException ex)
    {
        return Results.Json(new { success = false, message = $"Connection failed: {ex.Message}" }, jsonOpts);
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, message = $"Error: {ex.Message}" }, jsonOpts);
    }
});

// GET /api/workflows — Return all workflows
app.MapGet("/api/workflows", () =>
{
    try
    {
        using var conn = GetWfConnection();
        using var cmd = new SqlCommand(
            "SELECT WorkflowID, WorkflowName, IntegrationSettings FROM wf_Workflows ORDER BY WorkflowID",
            conn);
        using var reader = cmd.ExecuteReader();

        var result = new List<object>();
        while (reader.Read())
        {
            result.Add(new
            {
                workflowId = Convert.ToInt32(reader[0]),
                workflowName = reader.IsDBNull(1) ? "(unnamed)" : reader.GetString(1),
                integrationSettings = reader.IsDBNull(2) ? "" : reader.GetString(2)
            });
        }

        return Results.Json(result, jsonOpts);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
});

// GET /api/reports/ap-productivity — Main report endpoint
app.MapGet("/api/reports/ap-productivity", (HttpRequest request) =>
{
    var wfIdStr = request.Query["workflowId"].FirstOrDefault() ?? "7";
    var start = request.Query["start"].FirstOrDefault() ?? "";
    var end = request.Query["end"].FirstOrDefault() ?? "";
    var dateBasis = request.Query["dateBasis"].FirstOrDefault() ?? "system";

    // Sanitize workflow id to integer to prevent SQL injection
    if (!int.TryParse(wfIdStr, out int wfIdInt))
        return Results.Json(new { error = "Invalid workflowId" }, jsonOpts, statusCode: 400);

    var wiTable = $"wf_WorkItems_{wfIdInt}";
    var winstTable = $"wf_WorkInstances_{wfIdInt}";

    try
    {
        using var conn = GetWfConnection();

        // Date filter clause
        var dateCol = dateBasis == "invoice" ? "wi.f24" : "wi.StartTime";

        var whereClauses = new List<string>();
        var sqlParams = new List<SqlParameter>();

        if (!string.IsNullOrEmpty(start))
        {
            whereClauses.Add($"{dateCol} >= @start");
            sqlParams.Add(new SqlParameter("@start", start));
        }
        if (!string.IsNullOrEmpty(end))
        {
            whereClauses.Add($"{dateCol} < DATEADD(day, 1, CAST(@end AS date))");
            sqlParams.Add(new SqlParameter("@end", end));
        }

        var whereSql = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        var sql = $@"
        SELECT
            wi.WorkItemID,
            wi.StartTime       AS SystemEntryDate,
            wi.EndTime          AS WorkItemEndTime,
            wi.Complete         AS WorkItemComplete,
            wi.f14              AS VendorName,
            wi.f15              AS InvoiceNumber,
            wi.f16              AS Amount,
            wi.f24              AS InvoiceDate,
            wi.f11              AS DocID,
            wi.f18              AS WorkflowStatus,
            wi.f19              AS WorkflowQueue,

            -- AP Processor: first instance owner (earliest instance by StartTime)
            proc_u.FullName     AS APProcessor,
            proc_inst.StartTime AS ProcStartTime,

            -- Site Mgr Approver: instance in QueueID 1 or 6
            appr_u.FullName     AS SiteMgrApprover,
            appr_inst.StartTime AS ApprStartTime,
            appr_inst.EndTime   AS SiteMgrDate,

            -- Senior Approver: instance in QueueID 2
            sr_u.FullName       AS APApprover,
            sr_inst.StartTime   AS SrStartTime,
            sr_inst.EndTime     AS APApprovalDate,

            -- Notes: combine notes from instances
            COALESCE(appr_inst.Note, '') + CASE WHEN appr_inst.Note IS NOT NULL AND sr_inst.Note IS NOT NULL THEN '; ' ELSE '' END + COALESCE(sr_inst.Note, '') AS Comments,

            -- Current queue
            q.QueueName         AS CurrentQueue

        FROM {wiTable} wi

        -- AP Processor: the owner of the earliest instance for this work item
        OUTER APPLY (
            SELECT TOP 1 inst.Owner, inst.StartTime
            FROM {winstTable} inst
            WHERE inst.WorkItemID = wi.WorkItemID
              AND inst.Owner IS NOT NULL
              AND inst.Owner != 0
            ORDER BY inst.StartTime ASC
        ) proc_inst
        LEFT JOIN wf_Users proc_u ON proc_u.UserID = proc_inst.Owner

        -- Site Mgr Approver: instance in approval queues (QueueID 1 or 6)
        OUTER APPLY (
            SELECT TOP 1 inst.Owner, inst.StartTime, inst.EndTime, inst.Note
            FROM {winstTable} inst
            WHERE inst.WorkItemID = wi.WorkItemID
              AND inst.QueueID IN (1, 6)
            ORDER BY inst.StartTime DESC
        ) appr_inst
        LEFT JOIN wf_Users appr_u ON appr_u.UserID = appr_inst.Owner

        -- Senior Approver: instance in QueueID 2
        OUTER APPLY (
            SELECT TOP 1 inst.Owner, inst.StartTime, inst.EndTime, inst.Note
            FROM {winstTable} inst
            WHERE inst.WorkItemID = wi.WorkItemID
              AND inst.QueueID = 2
            ORDER BY inst.StartTime DESC
        ) sr_inst
        LEFT JOIN wf_Users sr_u ON sr_u.UserID = sr_inst.Owner

        -- Current queue (latest instance)
        OUTER APPLY (
            SELECT TOP 1 inst.QueueID
            FROM {winstTable} inst
            WHERE inst.WorkItemID = wi.WorkItemID
            ORDER BY inst.StartTime DESC
        ) latest_inst
        LEFT JOIN wf_Queues q ON q.QueueID = latest_inst.QueueID AND q.WorkflowID = {wfIdInt}

        {whereSql}
        ORDER BY wi.StartTime DESC";

        using var cmd = new SqlCommand(sql, conn);
        foreach (var p in sqlParams)
            cmd.Parameters.Add(p);

        using var reader = cmd.ExecuteReader();

        var items = new List<Dictionary<string, object?>>();
        var totalDollars = 0.0;
        var processorsSet = new HashSet<string>();
        var vendorsSet = new HashSet<string>();
        var totalDaysList = new List<int>();

        while (reader.Read())
        {
            var amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? (double?)null : Convert.ToDouble(reader["Amount"]);
            if (amount.HasValue) totalDollars += amount.Value;

            var apProcessor = reader.IsDBNull(reader.GetOrdinal("APProcessor")) ? "" : reader["APProcessor"]?.ToString() ?? "";
            var vendorName = reader.IsDBNull(reader.GetOrdinal("VendorName")) ? "" : reader["VendorName"]?.ToString() ?? "";

            if (!string.IsNullOrEmpty(apProcessor)) processorsSet.Add(apProcessor);
            if (!string.IsNullOrEmpty(vendorName)) vendorsSet.Add(vendorName);

            var systemEntryDate = reader.IsDBNull(reader.GetOrdinal("SystemEntryDate")) ? (DateTime?)null : Convert.ToDateTime(reader["SystemEntryDate"]);
            var workItemEndTime = reader.IsDBNull(reader.GetOrdinal("WorkItemEndTime")) ? (DateTime?)null : Convert.ToDateTime(reader["WorkItemEndTime"]);
            var workItemComplete = !reader.IsDBNull(reader.GetOrdinal("WorkItemComplete")) && Convert.ToBoolean(reader["WorkItemComplete"]);

            var apprStartTime = reader.IsDBNull(reader.GetOrdinal("ApprStartTime")) ? (DateTime?)null : Convert.ToDateTime(reader["ApprStartTime"]);
            var siteMgrDate = reader.IsDBNull(reader.GetOrdinal("SiteMgrDate")) ? (DateTime?)null : Convert.ToDateTime(reader["SiteMgrDate"]);
            var srStartTime = reader.IsDBNull(reader.GetOrdinal("SrStartTime")) ? (DateTime?)null : Convert.ToDateTime(reader["SrStartTime"]);
            var apApprovalDate = reader.IsDBNull(reader.GetOrdinal("APApprovalDate")) ? (DateTime?)null : Convert.ToDateTime(reader["APApprovalDate"]);

            var invoiceDate = reader.IsDBNull(reader.GetOrdinal("InvoiceDate")) ? (DateTime?)null : Convert.ToDateTime(reader["InvoiceDate"]);

            // Mgr Days: days the approval step took
            int? mgrDays = DaysBetween(apprStartTime, siteMgrDate);

            // AP Days: days the senior approval step took
            int? apDays = DaysBetween(srStartTime, apApprovalDate);

            // Total Days: from workflow start to final approval (or today)
            int? totalDays;
            var finalDate = apApprovalDate ?? siteMgrDate ?? workItemEndTime;
            if (workItemComplete && finalDate.HasValue)
                totalDays = DaysBetween(systemEntryDate, finalDate);
            else if (systemEntryDate.HasValue)
                totalDays = DaysBetween(systemEntryDate, DateTime.Now);
            else
                totalDays = null;

            if (totalDays.HasValue) totalDaysList.Add(totalDays.Value);

            var status = workItemComplete ? "Completed" : "In Progress";

            var comments = reader.IsDBNull(reader.GetOrdinal("Comments")) ? "" : reader["Comments"]?.ToString() ?? "";
            comments = comments.Trim(' ', ';');

            items.Add(new Dictionary<string, object?>
            {
                ["workItemId"] = reader.IsDBNull(reader.GetOrdinal("WorkItemID")) ? null : reader["WorkItemID"],
                ["apProcessor"] = apProcessor,
                ["vendorName"] = vendorName,
                ["invoiceDate"] = FmtDate(invoiceDate),
                ["invoiceNumber"] = reader.IsDBNull(reader.GetOrdinal("InvoiceNumber")) ? "" : reader["InvoiceNumber"]?.ToString() ?? "",
                ["amount"] = amount,
                ["systemEntryDate"] = FmtDate(systemEntryDate),
                ["siteMgrApprover"] = reader.IsDBNull(reader.GetOrdinal("SiteMgrApprover")) ? "" : reader["SiteMgrApprover"]?.ToString() ?? "",
                ["siteMgrDate"] = FmtDate(siteMgrDate),
                ["mgrDays"] = mgrDays,
                ["apApprover"] = reader.IsDBNull(reader.GetOrdinal("APApprover")) ? "" : reader["APApprover"]?.ToString() ?? "",
                ["apApprovalDate"] = FmtDate(apApprovalDate),
                ["apDays"] = apDays,
                ["totalDays"] = totalDays,
                ["comments"] = comments,
                ["status"] = status
            });
        }

        var avgDays = totalDaysList.Count > 0
            ? Math.Round((double)totalDaysList.Sum() / totalDaysList.Count, 1)
            : 0.0;

        return Results.Json(new
        {
            items,
            summary = new
            {
                totalInvoices = items.Count,
                totalDollars = Math.Round(totalDollars, 2),
                uniqueProcessors = processorsSet.Count,
                uniqueVendors = vendorsSet.Count,
                avgProcessingDays = avgDays
            }
        }, jsonOpts);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
});

// ── Auto-open browser ────────────────────────────────────────────────────────
_ = Task.Run(async () =>
{
    await Task.Delay(1500);
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://localhost:5000",
            UseShellExecute = true
        });
    }
    catch { /* Ignore if browser can't open */ }
});

app.Run();

// ── Helper functions ─────────────────────────────────────────────────────────
static string? FmtDate(DateTime? val)
{
    return val?.ToString("yyyy-MM-dd");
}

static int? DaysBetween(DateTime? startDt, DateTime? endDt)
{
    if (!startDt.HasValue || !endDt.HasValue) return null;
    var days = (endDt.Value.Date - startDt.Value.Date).Days;
    return Math.Max(days, 0);
}

// ── DTO classes ──────────────────────────────────────────────────────────────
public class DbConnectionInfo
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";
    public string Username { get; set; } = "";
    public string? EncryptedPassword { get; set; }
}

public class ConnectionSettings
{
    public DbConnectionInfo? Workflow { get; set; }
    public DbConnectionInfo? AppEnhancer { get; set; }
}

public class SaveSettingsConnectionInfo
{
    public string? Server { get; set; }
    public string? Database { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class SaveSettingsRequest
{
    public SaveSettingsConnectionInfo? Workflow { get; set; }
    public SaveSettingsConnectionInfo? AppEnhancer { get; set; }
}

public class TestConnectionRequest
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
