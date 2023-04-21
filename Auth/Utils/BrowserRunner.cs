using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Auth.Utils;

public sealed class BrowserRunner : IDisposable
{
    private const string StartPage = "/start";
    private readonly bool _hasStartPage;
    private readonly ContainedHttpServer _listener;
    private readonly string _localhostWithPort;

    public BrowserRunner(string localhostWithPort, string callbackPath, string htmlTitle, string htmlBody, string? targetUrl = null)
    {
        _localhostWithPort = localhostWithPort;

        if (targetUrl != null)
        {
            // Build a HTML form that does a POST of the data from the url
            // This is a workaround since the url may be too long to pass to the browser directly
            var startPageHtml = UrlToHtmlForm.Parse(targetUrl);

            // Setup a temporary http server that listens to the given redirect uri and to
            // the given start page. At the start page we can publish the html that we
            // generated from the StartUrl and at the redirect uri we can retrieve the
            // authorization code and return it to the application
            _listener = new ContainedHttpServer(localhostWithPort, callbackPath, htmlTitle, htmlBody,
                routes: new Dictionary<string, Action<HttpContext>> {
                { StartPage, async ctx => await ctx.Response.WriteAsync(startPageHtml) }
                });

            _hasStartPage = true;
        }
        else
        {
            _listener = new ContainedHttpServer(localhostWithPort, callbackPath, htmlTitle, htmlBody);
        }
    }

    public async Task<string> PostAndRunUntilCallback()
    {
        if (!_hasStartPage)
        {
            throw new NotImplementedException();
        }

        Run(_localhostWithPort + StartPage);

        return await _listener.WaitForCallbackAsync();
    }

    public async Task<string> RunUntilCallback(string targetUrl)
    {
        Run(targetUrl);

        return await _listener.WaitForCallbackAsync();
    }

    public static void Run(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // Thanks Brock! https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw new Exception("Unrecognized operating system");
            }
        }
    }

    public void Dispose()
    {
        using (_listener) { }
    }
}