using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Clockify.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FSharp.Core;
using Microsoft.Graph;
using syncer;
using Newtonsoft.Json;
using Process = System.Diagnostics.Process;

var tenantId = Environment.GetEnvironmentVariable("CLOCKSYNC_TENANT").Expect("Tenant no set");
var clientId = Environment.GetEnvironmentVariable("CLOCKSYNC_CLIENT").Expect("Client not set");
var clockifyKey = Environment.GetEnvironmentVariable("CLOCKSYNC_CLOCKIFY_KEY").Expect("Client not set");

var options = new InteractiveBrowserCredentialOptions
{
    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
};

Task HandleCallBack(DeviceCodeInfo code, CancellationToken cancellation)
{
    OpenBrowser(code.VerificationUri.ToString());
    Console.WriteLine(code.Message);
    return Task.CompletedTask;
}

TokenCredential deviceCodeCredential = new DeviceCodeCredential(HandleCallBack, tenantId, clientId, options);

var services = new ServiceCollection();
var providor = services.AddSingleton<ICoordinator, Coordinator>()
    .AddSingleton<ICalendarConnector, MsGraph.GraphConnector>(prov =>
        ClientFactory.createClient(prov.GetService<TokenCredential>()))
    .AddSingleton<IClockifyConnector>(_ => new ClockifyConnector(clockifyKey))
    .AddSingleton(deviceCodeCredential)
    .AddSingleton<IMerger, Merger>()
    .AddSingleton<IProjectFinder, ProjectFinder>()
    .AddSingleton<IFileReader, FileReader>()
    .BuildServiceProvider();

[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

try
{
    var start = DateTime.Parse("2022-09-01");
    var end = DateTime.Parse("2022-10-01");
    // var events = await client.GetCalendarView("", start, end).Map(x => x.ToList());
    //
    // foreach (var e in events)
    // {
    //     Console.WriteLine(
    //         $"start {e.Start} end {e.End} name {e.EntryTitle} in categories {e.Category} in series {e.Series}");
    // }
    //
    // Console.WriteLine(events.Count);
    //
    // Console.ReadKey();
    // var clockify = new syncer.Clockify.ClockifyClient(clockifyKey);
    // var entries = await clockify.FetchEntries(start, end).Expect("Invalid return from entries");
    // foreach (var e in entries)
    // {
    //     Console.WriteLine(
    //         $"start {e.Start} end {e.End} name {e.EntryTitle} in categories {e.Project}");
    // }
    //
    // IMerger merger = new Merger();
    //
    //
    // Console.WriteLine(entries.Count);
    var coordinator = providor.GetService<ICoordinator>();
    var requiresSync = await coordinator.SyncFor(new RunOptions(true), FSharpOption<string>.None, start, end);
}
catch (Exception e)
{
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine(e.InnerException?.Message);
    Console.Error.WriteLine(e.StackTrace);
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

    public static T Expect<T>(this T? val, string message = "Value was expected")
    {
        if (val == null)
        {
            throw new ArgumentNullException(message);
        }

        return val;
    }
}