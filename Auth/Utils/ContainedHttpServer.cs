using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Auth.Utils;

public sealed class ContainedHttpServer : IDisposable
{
    private readonly IWebHost _host;
    private TaskCompletionSource<string> _callbackResultSource = new();
    private readonly string _htmlTitle;
    private readonly string _htmlBody;
    private readonly int _timeoutInSeconds;
    private readonly string _callbackPath;

    public ContainedHttpServer(
        string host,
        string callbackPath,
        string htmlTitle,
        string htmlBody,
        int timeoutInSeconds = 300
    )
    {
        _htmlTitle = htmlTitle;
        _htmlBody = htmlBody;
        _timeoutInSeconds = timeoutInSeconds;
        _callbackPath = callbackPath;

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
            if (ctx.Request.Path.Equals(_callbackPath))
            {
                if (ctx.Request.Method == "GET")
                {
                    await SetResult(ctx.Request.QueryString.Value!, ctx);
                }
                else
                {
                    ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                }
            }
            else
            {
                ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            }
        });
    }

    private async Task SetResult(string value, HttpContext ctx)
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
            await ctx.Response.WriteAsync(html);
            await ctx.Response.Body.FlushAsync();

            var source = _callbackResultSource;

            _callbackResultSource = new TaskCompletionSource<string>();

            // Schedule setting the result on the thread pool so that the request handler can finish before the result is handled.
            _ = Task.Run(() =>
            {
                source.TrySetResult(value);
            });
        }
        catch (Exception)
        {
            ctx.Response.StatusCode = 400;
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync("<h1>Invalid request.</h1>");
            await ctx.Response.Body.FlushAsync();
        }
    }

    public Task<string> WaitForCallbackAsync(int? timeoutInSeconds = null)
    {
        timeoutInSeconds ??= _timeoutInSeconds;

        var source = _callbackResultSource;

        Task.Run(async () =>
        {
            await Task.Delay(timeoutInSeconds.Value * 1000);
            source.TrySetCanceled();
        });

        return source.Task;
    }
}