using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Auth.Utils;

public class ContainedHttpServer : IDisposable
{
    private IWebHost _host;
    private TaskCompletionSource<string> _source = new TaskCompletionSource<string>();
    private readonly string _htmlTitle;
    private readonly string _htmlBody;
    private readonly int _timeoutInSeconds;
    private readonly string _callbackUrl;
    private readonly Dictionary<string, Action<HttpContext>>? _routes;

    public ContainedHttpServer(
        string host,
        string callbackUrl,
        string htmlTitle,
        string htmlBody,
        int timeoutInSeconds = 300,
        Dictionary<string, Action<HttpContext>>? routes = null)
    {
        _htmlTitle = htmlTitle;
        _htmlBody = htmlBody;
        _timeoutInSeconds = timeoutInSeconds;
        _callbackUrl = callbackUrl;
        _routes = routes;

        _host = new WebHostBuilder()
            .UseKestrel()
            .UseUrls(host)
            .Configure(Configure)
            .Build();

        _host.Start();
    }

    public void Dispose()
    {
        using (_host) { }
    }

    private void Configure(IApplicationBuilder app)
    {
        app.Run(async ctx =>
        {
            if (_routes != null && _routes.ContainsKey(ctx.Request.Path.Value!))
            {
                _routes[ctx.Request.Path.Value!](ctx);
            }
            else if (ctx.Request.Path.Equals(_callbackUrl))
            {
                if (ctx.Request.Method == "GET")
                {
                    SetResult(ctx.Request.QueryString.Value!, ctx);
                }
                else if (ctx.Request.Method == "POST")
                {
                    if (!ctx.Request.ContentType!.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.StatusCode = 415;
                    }
                    else
                    {
                        using (var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8))
                        {
                            var body = await sr.ReadToEndAsync();
                            SetResult(body, ctx);
                        }
                    }
                }
                else
                {
                    ctx.Response.StatusCode = 405;
                }
            }
            else
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        });
    }

    private void SetResult(string value, HttpContext ctx)
    {
        string html = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset=""UTF-8"">
<title>{_htmlTitle}</title>
</head>
<body>
{_htmlBody}
</body>
</html>";
        try
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/html";
            ctx.Response.WriteAsync(html);
            ctx.Response.Body.FlushAsync();

            var source = _source;

            _source = new TaskCompletionSource<string>();

            source.TrySetResult(value);
        }
        catch (Exception)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "text/html";
            ctx.Response.WriteAsync("<h1>Invalid request.</h1>");
            ctx.Response.Body.Flush();
        }
    }

    public Task<string> WaitForCallbackAsync(int? timeoutInSeconds = null)
    {
        timeoutInSeconds = timeoutInSeconds ?? _timeoutInSeconds;

        Task.Run(async () =>
        {
            await Task.Delay(timeoutInSeconds.Value * 1000);
            _source.TrySetCanceled();
        });

        return _source.Task;
    }
}