using System.Diagnostics;
using System.Runtime.InteropServices;
using IdentityModel.OidcClient.Browser;

namespace Auth.Utils;

// This class opens up the system browser in order to log in a user and get the authorization code back
public sealed class SystemBrowserRunner(string htmlTitle, string htmlBody) : IBrowser, IDisposable
{
    private ContainedHttpServer? _listener;

    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken)
    {
        var callbackUri = new Uri(options.EndUrl);

        _listener?.Dispose();
        _listener = new($"{callbackUri.Scheme}://{callbackUri.Host}:{callbackUri.Port}", callbackUri.AbsolutePath, htmlTitle, htmlBody);

        OpenBrowser(options.StartUrl);

        try
        {
            var result = await _listener.WaitForCallbackAsync();

            if (string.IsNullOrWhiteSpace(result))
            {
                return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = "Empty response." };
            }

            return new BrowserResult { Response = result, ResultType = BrowserResultType.Success };
        }
        catch (TaskCanceledException ex)
        {
            return new BrowserResult { ResultType = BrowserResultType.Timeout, Error = ex.Message };
        }
        catch (Exception ex)
        {
            return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = ex.Message };
        }
    }

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
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
                throw;
            }
        }
    }

    public void Dispose()
    {
        _listener?.Dispose();
    }
}
