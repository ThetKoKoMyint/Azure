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
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Hello Continuum MFO</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #2D3E1F;
            min-height: 100vh;
            display: flex;
            justify-content: center;
            align-items: center;
        }
        
        .container {
            background: white;
            padding: 60px 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
            text-align: center;
            max-width: 500px;
        }
        
        h1 {
            color: #333;
            font-size: 2.5em;
            margin-bottom: 20px;
        }
        
        p {
            color: #666;
            font-size: 1.1em;
            line-height: 1.6;
        }
        
        .badge {
            display: inline-block;
            background: #667eea;
            color: white;
            padding: 8px 16px;
            border-radius: 20px;
            margin-top: 20px;
            font-size: 0.9em;
        }
    </style>





<form method='post' enctype='multipart/form-data' action='/upload'>
  <input type='file' name='file' />
  <button type='submit'>Upload</button>
</form>
</body>
</html>";
}


