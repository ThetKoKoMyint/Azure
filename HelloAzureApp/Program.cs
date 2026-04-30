using Azure.Identity;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Read configuration
var accountName = builder.Configuration["AzureBlobStorage:AccountName"];
var containerName = builder.Configuration["AzureBlobStorage:ContainerName"];

// Create Blob client using Managed Identity
var blobServiceClient = new BlobServiceClient(
    new Uri($"https://{accountName}.blob.core.windows.net"),
    new DefaultAzureCredential());

var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

var app = builder.Build();

app.MapGet("/", async () =>
{
    await containerClient.CreateIfNotExistsAsync();

    var blobs = new List<string>();
    await foreach (var blob in containerClient.GetBlobsAsync())
        blobs.Add(blob.Name);

    var list = blobs.Count == 0
        ? "<li>No blobs found</li>"
        : string.Join("", blobs.Select(b => $"<li>{b}</li>"));

    return Results.Text($@"
<html>
<body>
<h1>Hello Continuum MFO</h1>
<h2>Azure Blob Connected ✅</h2>
<ul>{list}</ul>
</body>
</html>
", "text/html");
});

app.Run();
``
