using System.Net;
using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ =>
{
    var configuration = builder.Configuration;
    var connectionString = configuration.GetConnectionString("BlobStorage")
        ?? configuration["BlobStorage:ConnectionString"];

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Blob Storage is not configured. Add ConnectionStrings__BlobStorage or BlobStorage__ConnectionString in Azure App Service Configuration.");
    }

    var containerName = configuration["BlobStorage:ContainerName"];
    if (string.IsNullOrWhiteSpace(containerName))
    {
        throw new InvalidOperationException(
            "Blob Storage container is not configured. Add BlobStorage__ContainerName in Azure App Service Configuration.");
    }

    return new BlobContainerClient(connectionString, containerName);
});

var app = builder.Build();

app.MapGet("/", async (BlobContainerClient containerClient) =>
{
    var model = await GetBlobStatusAsync(containerClient);
    return Results.Text(GetHtmlPage(model), "text/html");
});

app.MapPost("/upload-test", async (BlobContainerClient containerClient) =>
{
    await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

    var blobName = $"webapp-test-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.txt";
    var blobClient = containerClient.GetBlobClient(blobName);
    var content = $"Hello from Azure Web App at {DateTimeOffset.UtcNow:O}";

    await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
    await blobClient.UploadAsync(stream, overwrite: false);

    return Results.Redirect("/");
});

app.Run();

static async Task<BlobStatusModel> GetBlobStatusAsync(BlobContainerClient containerClient)
{
    try
    {
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobNames = new List<string>();
        await foreach (var page in containerClient.GetBlobsAsync().AsPages(pageSizeHint: 10))
        {
            blobNames.AddRange(page.Values.Select(item => item.Name));
            break;
        }

        return new BlobStatusModel(
            true,
            containerClient.AccountName,
            containerClient.Name,
            blobNames,
            null);
    }
    catch (RequestFailedException ex)
    {
        return new BlobStatusModel(false, containerClient.AccountName, containerClient.Name, [], ex.Message);
    }
}

static string GetHtmlPage(BlobStatusModel model)
{
    var statusText = model.IsConnected ? "Connected to Azure Blob Storage" : "Blob Storage connection failed";
    var statusClass = model.IsConnected ? "ok" : "error";
    var blobItems = model.BlobNames.Count == 0
        ? "<li>No blobs found yet. Use the button below to upload a test file.</li>"
        : string.Join("", model.BlobNames.Select(name => $"<li>{WebUtility.HtmlEncode(name)}</li>"));
    var error = string.IsNullOrWhiteSpace(model.ErrorMessage)
        ? ""
        : $"<p class='error-message'>{WebUtility.HtmlEncode(model.ErrorMessage)}</p>";

    return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Hello Continuum MFO</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #2D3E1F;
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 24px;
        }}

        .container {{
            background: white;
            padding: 48px 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
            text-align: center;
            max-width: 640px;
            width: 100%;
        }}

        h1 {{
            color: #333;
            font-size: 2.5em;
            margin-bottom: 16px;
        }}

        p {{
            color: #666;
            font-size: 1.05em;
            line-height: 1.6;
        }}

        .badge {{
            display: inline-block;
            background: #667eea;
            color: white;
            padding: 8px 16px;
            border-radius: 20px;
            margin-top: 18px;
            font-size: 0.9em;
        }}

        .storage-panel {{
            margin-top: 28px;
            padding-top: 24px;
            border-top: 1px solid #e5e7eb;
            text-align: left;
        }}

        .status {{
            display: inline-block;
            margin-bottom: 12px;
            font-weight: 700;
        }}

        .ok {{ color: #166534; }}
        .error {{ color: #b91c1c; }}

        ul {{
            margin: 14px 0 20px 20px;
            color: #444;
            line-height: 1.6;
        }}

        button {{
            background: #2D3E1F;
            color: white;
            border: 0;
            border-radius: 6px;
            cursor: pointer;
            font-size: 1rem;
            padding: 10px 16px;
        }}

        button:hover {{
            background: #3c5229;
        }}

        .error-message {{
            color: #b91c1c;
            overflow-wrap: anywhere;
        }}

        .clock {{
            margin-top: 24px;
            color: #555;
            font-weight: 600;
        }}
    </style>
</head>
<body onload='startClock()'>
    <div class='container'>
        <h1>Hello Continuum MFO</h1>
        <p>Welcome to your ASP.NET Core application running with Azure Blob Storage.</p>
        <span class='badge'>ASP.NET Core on Azure App Service</span>

        <div class='storage-panel'>
            <span class='status {statusClass}'>{statusText}</span>
            <p><strong>Account:</strong> {WebUtility.HtmlEncode(model.AccountName)}</p>
            <p><strong>Container:</strong> {WebUtility.HtmlEncode(model.ContainerName)}</p>
            {error}
            <ul>{blobItems}</ul>
            <form method='post' action='/upload-test'>
                <button type='submit'>Upload test blob</button>
            </form>
        </div>

        <div class='clock' id='clock'></div>
    </div>

<script>
    function startClock() {{
        function updateClock() {{
            const now = new Date();
            document.getElementById('clock').innerText = now.toLocaleTimeString();
        }}
        updateClock();
        setInterval(updateClock, 1000);
    }}
</script>
</body>
</html>";
}

record BlobStatusModel(
    bool IsConnected,
    string AccountName,
    string ContainerName,
    IReadOnlyList<string> BlobNames,
    string? ErrorMessage);
