
using Azure.Identity;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Read settings
var accountName = builder.Configuration["AzureBlobStorage:AccountName"];
var containerName = builder.Configuration["AzureBlobStorage:ContainerName"];

// Blob service using Managed Identity
builder.Services.AddSingleton(_ =>
    new BlobServiceClient(
        new Uri($"https://{accountName}.blob.core.windows.net"),
        new DefaultAzureCredential()));

builder.Services.AddSingleton(sp =>
{
    var serviceClient = sp.GetRequiredService<BlobServiceClient>();
    return serviceClient.GetBlobContainerClient(containerName);
});

var app = builder.Build();

// Home page with blob listing
app.MapGet("/", async (BlobContainerClient container) =>
{
    await container.CreateIfNotExistsAsync();

    var blobNames = new List<string>();
    await foreach (var blob in container.GetBlobsAsync())
        blobNames.Add(blob.Name);

    return Results.Text(GetHtmlPage(blobNames), "text/html");
});

// Upload endpoint
app.MapPost("/upload", async (IFormFile file, BlobContainerClient container) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded");

    var blobClient = container.GetBlobClient(file.FileName);
    await blobClient.UploadAsync(file.OpenReadStream(), overwrite: true);

    return Results.Ok("File uploaded successfully");
});

app.Run();

static string GetHtmlPage(List<string> blobs)
{
    var list = blobs.Count == 0
        ? "<li>No blobs found</li>"
        : string.Join("", blobs.Select(b => $"<li>{b}</li>"));

    return $@"
<!DOCTYPE html>
<html>
<body>
<h1>Hello Continuum MFO</h1>
<h2>Azure Blob Storage Connected ✅</h2>

<h3>Uploaded Files</h3>
<ul>{list}</ul>

<form method='post' enctype='multipart/form-data' action='/upload'>
  <input type='file' name='file' />
  <button type='submit'>Upload</button>
</form>
</body>
</html>";
}
