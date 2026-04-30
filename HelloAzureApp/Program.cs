using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Read configuration
var connectionString = builder.Configuration.GetConnectionString("BlobStorage");
var containerName = builder.Configuration["BlobStorage:ContainerName"];

// Register Blob services
builder.Services.AddSingleton(_ => new BlobServiceClient(connectionString));
builder.Services.AddSingleton(sp =>
{
    var serviceClient = sp.GetRequiredService<BlobServiceClient>();
    return serviceClient.GetBlobContainerClient(containerName);
});

var app = builder.Build();

// Homepage
app.MapGet("/", async (BlobContainerClient container) =>
{
    await container.CreateIfNotExistsAsync();

    var blobNames = new List<string>();
    await foreach (var blob in container.GetBlobsAsync())
    {
        blobNames.Add(blob.Name);
    }

    return Results.Text(GetHtmlPage(blobNames), "text/html");
});

app.Run();

static string GetHtmlPage(List<string> blobs)
{
    var blobListHtml = blobs.Count == 0
        ? "<li>No blobs in container</li>"
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



