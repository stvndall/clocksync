using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Azure.Identity;
using Microsoft.Graph;
using msgraph;
using Newtonsoft.Json;
using Process = System.Diagnostics.Process;

var tenantId = Environment.GetEnvironmentVariable("CLOCKSYNC_TENANT");
var clientId = Environment.GetEnvironmentVariable("CLOCKSYNC_CLIENT") ;

var options = new InteractiveBrowserCredentialOptions
{
    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
};

Func<DeviceCodeInfo, CancellationToken, Task> callback = (code, cancellation) =>
{
    OpenBrowser(code.VerificationUri.ToString());
    Console.WriteLine(code.Message);
    return Task.CompletedTask;
};

var client = ClientFactory.createClient(new DeviceCodeCredential(callback, tenantId, clientId, options));

try
{
    var start = DateTime.Parse("2022-08-01");
    var end = DateTime.Parse("2022-09-01");
    var events = await client.GetCalendarView("", start, end ).Map(x => x.ToList());
    
    foreach (var e in events)
    {
        Console.WriteLine(
            $"start {e.Start} end {e.End} name {e.EntryTitle} in categories {e.Category} in series {e.Series}");
    }
    Console.WriteLine(events.Count);
}
catch (Exception e)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine(e.InnerException?.Message);
    Console.Error.WriteLine(e.StackTrace);
}

static void OpenBrowser(string url)
{
    try
    {
        Process.Start(url);
    }
    catch
    {
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

public static class Ext
{
    public static U Map<T, U>(this T from, Func<T, U> map)
    {
        return map(from);
    }

    public static Task<U> Map<T, U>(this Task<T> from, Func<T, U> map)
    {
        return from.ContinueWith(x => map(x.Result));
    }
}