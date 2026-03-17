using System.Diagnostics;
using System.DirectoryServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 5000
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// ── Authentication (Windows/Negotiate) ──────────────────────────────────────
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();
builder.Services.AddAuthorization();

var app = builder.Build();

// Serve static files from wwwroot (before auth so CSS/JS are always accessible)
app.UseStaticFiles();

// Wire up auth middleware
app.UseAuthentication();
app.UseAuthorization();

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
        throw new InvalidOperationException("Database connections not configured. Visit settings.html to set up.");
    var conn = new SqlConnection(BuildConnectionString(settings.Workflow));
    conn.Open();
    return conn;
}

// ── Auth helpers ─────────────────────────────────────────────────────────────
bool IsUserAuthorized(HttpContext ctx)
{
    var identity = ctx.User.Identity;
    if (identity == null || !identity.IsAuthenticated) return false;

    var settings = LoadSettings();
    var groups = settings?.AllowedAdGroups;

    // Bootstrap mode: no groups configured = all authenticated users allowed
    if (groups == null || groups.Count == 0) return true;

    // Check AD group membership
    if (ctx.User is WindowsPrincipal wp)
    {
        foreach (var group in groups)
        {
            try { if (wp.IsInRole(group)) return true; } catch { }
        }
    }

    return false;
}

string GetDisplayName(HttpContext ctx)
{
    var name = ctx.User.Identity?.Name ?? "";
    // "DOMAIN\username" → just the username part, capitalized nicely
    if (name.Contains('\\'))
        name = name.Substring(name.IndexOf('\\') + 1);
    return name;
}

// ── Routes ───────────────────────────────────────────────────────────────────

// GET /api/auth/me — Return current user info
app.MapGet("/api/auth/me", (HttpContext ctx) =>
{
    var identity = ctx.User.Identity;
    return Results.Json(new
    {
        authenticated = identity?.IsAuthenticated ?? false,
        userName = identity?.Name ?? "",
        displayName = GetDisplayName(ctx),
        authorized = IsUserAuthorized(ctx),
        bootstrapMode = (LoadSettings()?.AllowedAdGroups?.Count ?? 0) == 0
    }, jsonOpts);
}).RequireAuthorization();

// GET /api/settings/auth — Return current auth config
app.MapGet("/api/settings/auth", (HttpContext ctx) =>
{
    if (!IsUserAuthorized(ctx))
        return Results.Json(new { error = "Access denied" }, jsonOpts, statusCode: 403);
    var settings = LoadSettings();
    return Results.Json(new
    {
        allowedAdGroups = settings?.AllowedAdGroups ?? new List<string>()
    }, jsonOpts);
}).RequireAuthorization();

// POST /api/settings/auth — Save auth config
app.MapPost("/api/settings/auth", async (HttpContext ctx) =>
{
    if (!IsUserAuthorized(ctx))
        return Results.Json(new { error = "Access denied" }, jsonOpts, statusCode: 403);

    try
    {
        var body = await JsonSerializer.DeserializeAsync<SaveAuthSettingsRequest>(ctx.Request.Body, jsonOpts);
        if (body == null)
            return Results.BadRequest(new { error = "Invalid request body" });

        var settings = LoadSettings() ?? new ConnectionSettings();
        settings.AllowedAdGroups = body.AllowedAdGroups?
            .Where(g => !string.IsNullOrWhiteSpace(g))
            .Select(g => g.Trim())
            .ToList() ?? new List<string>();

        var json = JsonSerializer.Serialize(settings, jsonOpts);
        await File.WriteAllTextAsync(connectionsFile, json);
        cachedSettings = settings;

        return Results.Json(new { success = true, message = "Access control settings saved." }, jsonOpts);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
}).RequireAuthorization();

// GET /api/settings/auth/browse-groups — Search AD for groups
app.MapGet("/api/settings/auth/browse-groups", (HttpRequest request) =>
{
    var search = request.Query["q"].FirstOrDefault() ?? "";
    if (search.Length < 2)
        return Results.Json(new { groups = new List<object>(), message = "Type at least 2 characters to search." }, jsonOpts);

    try
    {
        var results = new List<object>();
        using var entry = new DirectoryEntry("LDAP://RootDSE");
        var defaultNamingContext = entry.Properties["defaultNamingContext"]?.Value?.ToString() ?? "";
        using var searchRoot = new DirectoryEntry($"LDAP://{defaultNamingContext}");
        using var searcher = new DirectorySearcher(searchRoot)
        {
            Filter = $"(&(objectCategory=group)(cn=*{EscapeLdapFilter(search)}*))",
            SizeLimit = 50,
            PageSize = 50
        };
        searcher.PropertiesToLoad.AddRange(new[] { "cn", "distinguishedName", "description" });

        using var searchResults = searcher.FindAll();
        foreach (SearchResult sr in searchResults)
        {
            var cn = sr.Properties["cn"]?.Count > 0 ? sr.Properties["cn"][0]?.ToString() ?? "" : "";
            var dn = sr.Properties["distinguishedName"]?.Count > 0 ? sr.Properties["distinguishedName"][0]?.ToString() ?? "" : "";
            var desc = sr.Properties["description"]?.Count > 0 ? sr.Properties["description"][0]?.ToString() ?? "" : "";

            // Extract domain from DN (e.g., DC=ad,DC=contoso,DC=com → AD)
            var domain = "";
            var dcMatch = System.Text.RegularExpressions.Regex.Match(dn, @"DC=([^,]+)");
            if (dcMatch.Success) domain = dcMatch.Groups[1].Value.ToUpper();

            results.Add(new
            {
                name = cn,
                fullName = !string.IsNullOrEmpty(domain) ? $"{domain}\\{cn}" : cn,
                description = desc
            });
        }

        results.Sort((a, b) => string.Compare(
            ((dynamic)a).name, ((dynamic)b).name, StringComparison.OrdinalIgnoreCase));

        return Results.Json(new { groups = results }, jsonOpts);
    }
    catch (Exception ex)
    {
        return Results.Json(new { groups = new List<object>(), message = $"AD lookup failed: {ex.Message}" }, jsonOpts);
    }
}).RequireAuthorization();

// GET / — Serve index.html (requires auth + group check)
app.MapGet("/", (HttpContext ctx) =>
{
    if (!IsUserAuthorized(ctx))
        return Results.Text("<html><body style='font-family:Inter,sans-serif;padding:40px;'><h2>Access Denied</h2><p>You do not have permission to view this report. Contact your administrator to be added to the authorized group.</p><p>Logged in as: " + System.Net.WebUtility.HtmlEncode(ctx.User.Identity?.Name ?? "") + "</p></body></html>", "text/html");
    return Results.File(
        Path.Combine(app.Environment.WebRootPath, "index.html"),
        "text/html");
}).RequireAuthorization();

// GET /settings.html — Serve settings page (requires auth, group check in bootstrap-aware mode)
app.MapGet("/settings.html", (HttpContext ctx) =>
{
    if (!IsUserAuthorized(ctx))
        return Results.Text("<html><body style='font-family:Inter,sans-serif;padding:40px;'><h2>Access Denied</h2><p>You do not have permission to access settings. Contact your administrator.</p><p>Logged in as: " + System.Net.WebUtility.HtmlEncode(ctx.User.Identity?.Name ?? "") + "</p></body></html>", "text/html");
    return Results.File(
        Path.Combine(app.Environment.WebRootPath, "settings.html"),
        "text/html");
}).RequireAuthorization();

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

    // List which workflows have mappings configured
    var configuredWorkflows = settings.WorkflowMappings?.Keys.Select(int.Parse).ToList() ?? new List<int>();

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
        },
        configuredWorkflows
    }, jsonOpts);
}).RequireAuthorization();

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
            },
            WorkflowMappings = existing?.WorkflowMappings,
            AllowedAdGroups = existing?.AllowedAdGroups
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
}).RequireAuthorization();

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
}).RequireAuthorization();

// GET /api/workflows — Return all workflows
app.MapGet("/api/workflows", () =>
{
    try
    {
        using var conn = GetWfConnection();
        using var cmd = new SqlCommand(
            "SELECT WorkflowID, WorkflowName, IntegrationSettings FROM wf_Workflows WHERE IsEnabled = 1 ORDER BY WorkflowID",
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
}).RequireAuthorization();

// GET /api/workflows/{id}/fields — Return workflow field mappings
app.MapGet("/api/workflows/{id}/fields", (int id) =>
{
    try
    {
        using var conn = GetWfConnection();
        using var cmd = new SqlCommand(
            "SELECT FieldName, ColumnName, MappedFieldName FROM wf_WorkflowFields WHERE WorkflowID = @wfId ORDER BY ColumnName",
            conn);
        cmd.Parameters.AddWithValue("@wfId", id);
        using var reader = cmd.ExecuteReader();

        var result = new List<object>();
        while (reader.Read())
        {
            result.Add(new
            {
                fieldName = reader.IsDBNull(0) ? "" : reader.GetString(0),
                columnName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                mappedFieldName = reader.IsDBNull(2) ? "" : reader.GetString(2)
            });
        }

        return Results.Json(result, jsonOpts);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
}).RequireAuthorization();

// GET /api/workflows/{id}/queues — Return workflow queues
app.MapGet("/api/workflows/{id}/queues", (int id) =>
{
    try
    {
        using var conn = GetWfConnection();
        using var cmd = new SqlCommand(
            "SELECT QueueID, QueueName FROM wf_Queues WHERE WorkflowID = @wfId ORDER BY QueueID",
            conn);
        cmd.Parameters.AddWithValue("@wfId", id);
        using var reader = cmd.ExecuteReader();

        var result = new List<object>();
        while (reader.Read())
        {
            result.Add(new
            {
                queueId = Convert.ToInt32(reader[0]),
                queueName = reader.IsDBNull(1) ? "" : reader.GetString(1)
            });
        }

        return Results.Json(result, jsonOpts);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
}).RequireAuthorization();

// GET /api/settings/workflow-mapping/{id} — Return saved mapping for a workflow
app.MapGet("/api/settings/workflow-mapping/{id}", (int id) =>
{
    var settings = LoadSettings();
    var key = id.ToString();
    if (settings?.WorkflowMappings != null && settings.WorkflowMappings.TryGetValue(key, out var mapping))
    {
        return Results.Json(new { configured = true, mapping }, jsonOpts);
    }
    return Results.Json(new { configured = false }, jsonOpts);
}).RequireAuthorization();

// POST /api/settings/workflow-mapping/{id} — Save mapping for a workflow
app.MapPost("/api/settings/workflow-mapping/{id}", async (int id, HttpRequest request) =>
{
    try
    {
        var body = await JsonSerializer.DeserializeAsync<SaveWorkflowMappingRequest>(request.Body, jsonOpts);
        if (body == null)
            return Results.BadRequest(new { error = "Invalid request body" });

        // Validate column names to prevent SQL injection (must match f\d+ pattern)
        var colPattern = new System.Text.RegularExpressions.Regex(@"^f\d+$");
        foreach (var kv in body.FieldMappings)
        {
            if (!colPattern.IsMatch(kv.Value))
                return Results.BadRequest(new { error = $"Invalid column name '{kv.Value}'. Expected format: f1, f2, etc." });
        }

        var settings = LoadSettings() ?? new ConnectionSettings();
        settings.WorkflowMappings ??= new Dictionary<string, WorkflowMapping>();

        settings.WorkflowMappings[id.ToString()] = new WorkflowMapping
        {
            WorkflowId = id,
            WorkflowName = body.WorkflowName,
            FieldMappings = body.FieldMappings,
            SiteMgrQueueIds = body.SiteMgrQueueIds,
            SeniorApprovalQueueIds = body.SeniorApprovalQueueIds
        };

        var json = JsonSerializer.Serialize(settings, jsonOpts);
        await File.WriteAllTextAsync(connectionsFile, json);
        cachedSettings = settings;

        return Results.Json(new { success = true, message = "Workflow mapping saved." }, jsonOpts);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
}).RequireAuthorization();

// DELETE /api/settings/workflow-mapping/{id} — Remove mapping for a workflow
app.MapDelete("/api/settings/workflow-mapping/{id}", async (int id) =>
{
    try
    {
        var settings = LoadSettings();
        if (settings?.WorkflowMappings == null || !settings.WorkflowMappings.ContainsKey(id.ToString()))
            return Results.Json(new { success = false, error = "No mapping found for this workflow." }, jsonOpts);

        settings.WorkflowMappings.Remove(id.ToString());

        var json = JsonSerializer.Serialize(settings, jsonOpts);
        await File.WriteAllTextAsync(connectionsFile, json);
        cachedSettings = settings;

        return Results.Json(new { success = true, message = "Workflow mapping removed." }, jsonOpts);
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = ex.Message }, jsonOpts, statusCode: 500);
    }
}).RequireAuthorization();

// ── Column validation helper ─────────────────────────────────────────────
var colPattern = new Regex(@"^f\d+$");
string SafeCol(string col, string fallback)
{
    return colPattern.IsMatch(col) ? col : fallback;
}

// ── Default mappings (backward-compatible with Workflow 7) ───────────────
WorkflowMapping GetMappingForWorkflow(int wfId)
{
    var settings = LoadSettings();
    var key = wfId.ToString();
    if (settings?.WorkflowMappings != null && settings.WorkflowMappings.TryGetValue(key, out var mapping))
        return mapping;

    // Fallback to Workflow 7 hardcoded defaults
    return new WorkflowMapping
    {
        WorkflowId = wfId,
        WorkflowName = "Default (Workflow 7)",
        FieldMappings = new Dictionary<string, string>
        {
            ["VendorName"] = "f14",
            ["InvoiceNumber"] = "f15",
            ["Amount"] = "f16",
            ["InvoiceDate"] = "f24",
            ["DocID"] = "f11",
            ["WorkflowStatus"] = "f18",
            ["WorkflowQueue"] = "f19"
        },
        SiteMgrQueueIds = new List<int> { 1, 6 },
        SeniorApprovalQueueIds = new List<int> { 2 }
    };
}

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

    // Load dynamic field/queue mapping
    var mapping = GetMappingForWorkflow(wfIdInt);
    var fVendor = SafeCol(mapping.FieldMappings.GetValueOrDefault("VendorName", "f14"), "f14");
    var fInvNum = SafeCol(mapping.FieldMappings.GetValueOrDefault("InvoiceNumber", "f15"), "f15");
    var fAmount = SafeCol(mapping.FieldMappings.GetValueOrDefault("Amount", "f16"), "f16");
    var fInvDate = SafeCol(mapping.FieldMappings.GetValueOrDefault("InvoiceDate", "f24"), "f24");
    var fDocId = SafeCol(mapping.FieldMappings.GetValueOrDefault("DocID", "f11"), "f11");
    var fStatus = SafeCol(mapping.FieldMappings.GetValueOrDefault("WorkflowStatus", "f18"), "f18");
    var fQueue = SafeCol(mapping.FieldMappings.GetValueOrDefault("WorkflowQueue", "f19"), "f19");

    var siteMgrQueueIds = mapping.SiteMgrQueueIds.Count > 0
        ? string.Join(",", mapping.SiteMgrQueueIds)
        : "-1";
    var seniorQueueIds = mapping.SeniorApprovalQueueIds.Count > 0
        ? string.Join(",", mapping.SeniorApprovalQueueIds)
        : "-1";

    try
    {
        using var conn = GetWfConnection();

        var sqlParams = new List<SqlParameter>();

        // Build the inner query (CTE) with dynamic columns
        var innerSql = $@"
        SELECT
            wi.WorkItemID,
            wi.StartTime       AS SystemEntryDate,
            wi.EndTime          AS WorkItemEndTime,
            wi.Complete         AS WorkItemComplete,
            wi.{fVendor}        AS VendorName,
            wi.{fInvNum}        AS InvoiceNumber,
            wi.{fAmount}        AS Amount,
            wi.{fInvDate}       AS InvoiceDate,
            wi.{fDocId}         AS DocID,
            wi.{fStatus}        AS WorkflowStatus,
            wi.{fQueue}         AS WorkflowQueue,

            proc_u.FullName     AS APProcessor,
            proc_inst.StartTime AS ProcStartTime,

            appr_u.FullName     AS SiteMgrApprover,
            appr_inst.StartTime AS ApprStartTime,
            appr_inst.EndTime   AS SiteMgrDate,

            sr_u.FullName       AS APApprover,
            sr_inst.StartTime   AS SrStartTime,
            sr_inst.EndTime     AS APApprovalDate,

            COALESCE(appr_inst.Note, '') + CASE WHEN appr_inst.Note IS NOT NULL AND sr_inst.Note IS NOT NULL THEN '; ' ELSE '' END + COALESCE(sr_inst.Note, '') AS Comments,

            q.QueueName         AS CurrentQueue

        FROM {wiTable} wi

        OUTER APPLY (
            SELECT TOP 1 inst.Owner, inst.StartTime
            FROM {winstTable} inst
            WHERE inst.WorkItemID = wi.WorkItemID
              AND inst.Owner IS NOT NULL
              AND inst.Owner != 0
            ORDER BY inst.StartTime ASC
        ) proc_inst
        LEFT JOIN wf_Users proc_u ON proc_u.UserID = proc_inst.Owner

        OUTER APPLY (
            SELECT TOP 1 inst.Owner, inst.StartTime, inst.EndTime, inst.Note
            FROM {winstTable} inst
            WHERE inst.WorkItemID = wi.WorkItemID
              AND inst.QueueID IN ({siteMgrQueueIds})
            ORDER BY inst.StartTime DESC
        ) appr_inst
        LEFT JOIN wf_Users appr_u ON appr_u.UserID = appr_inst.Owner

        OUTER APPLY (
            SELECT TOP 1 inst.Owner, inst.StartTime, inst.EndTime, inst.Note
            FROM {winstTable} inst
            WHERE inst.WorkItemID = wi.WorkItemID
              AND inst.QueueID IN ({seniorQueueIds})
            ORDER BY inst.StartTime DESC
        ) sr_inst
        LEFT JOIN wf_Users sr_u ON sr_u.UserID = sr_inst.Owner

        OUTER APPLY (
            SELECT TOP 1 inst.QueueID
            FROM {winstTable} inst
            WHERE inst.WorkItemID = wi.WorkItemID
            ORDER BY inst.StartTime DESC
        ) latest_inst
        LEFT JOIN wf_Queues q ON q.QueueID = latest_inst.QueueID AND q.WorkflowID = {wfIdInt}";

        // Date filter — use CTE so we can filter on computed columns (SiteMgrDate, APApprovalDate)
        string dateCol;
        switch (dateBasis)
        {
            case "invoice":       dateCol = "r.InvoiceDate"; break;
            case "siteMgrApproval": dateCol = "r.SiteMgrDate"; break;
            case "apApproval":    dateCol = "r.APApprovalDate"; break;
            default:              dateCol = "r.SystemEntryDate"; break;
        }

        var whereClauses = new List<string>();
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
        ;WITH cte AS (
            {innerSql}
        )
        SELECT * FROM cte r
        {whereSql}
        ORDER BY r.SystemEntryDate DESC";

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

            int? mgrDays = DaysBetween(apprStartTime, siteMgrDate);
            int? apDays = DaysBetween(srStartTime, apApprovalDate);

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
}).RequireAuthorization();

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

static string EscapeLdapFilter(string input)
{
    return input
        .Replace("\\", "\\5c")
        .Replace("*", "\\2a")
        .Replace("(", "\\28")
        .Replace(")", "\\29")
        .Replace("\0", "\\00");
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
    public Dictionary<string, WorkflowMapping>? WorkflowMappings { get; set; }
    public List<string>? AllowedAdGroups { get; set; }
}

public class WorkflowMapping
{
    public int WorkflowId { get; set; }
    public string WorkflowName { get; set; } = "";
    public Dictionary<string, string> FieldMappings { get; set; } = new();
    public List<int> SiteMgrQueueIds { get; set; } = new();
    public List<int> SeniorApprovalQueueIds { get; set; } = new();
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

public class SaveWorkflowMappingRequest
{
    public int WorkflowId { get; set; }
    public string WorkflowName { get; set; } = "";
    public Dictionary<string, string> FieldMappings { get; set; } = new();
    public List<int> SiteMgrQueueIds { get; set; } = new();
    public List<int> SeniorApprovalQueueIds { get; set; } = new();
}

public class SaveAuthSettingsRequest
{
    public List<string>? AllowedAdGroups { get; set; }
}
