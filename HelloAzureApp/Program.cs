var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Text(GetHtmlPage(), "text/html"));

app.Run();

static string GetHtmlPage()
{
    return @"
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
</head>
<body>
    <div class='container'>
        <h1>Hello Continuum MFO</h1>
        <p>Welcome to your simple web page built with ASP.NET Core</p>
        <span class='badge'>ASP.NET Core Application</span>
    </div>
</body>
</html>";
}

