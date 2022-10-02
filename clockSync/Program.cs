using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Clockify.Net;
using clockSync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FSharp.Core;
using Microsoft.Graph;
using syncer;
using Newtonsoft.Json;
using Spectre.Console;
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
    Utilities.OpenBrowser(code.VerificationUri.ToString());
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
    .AddSingleton<RunOptions>(new RunOptions(false))
    .BuildServiceProvider();

AnsiConsole.Write(new FigletText("ClockSync").Centered().Color(Color.Aqua));
// var selected = AnsiConsole.Prompt(new SelectionPrompt<string>()
//     .Title("select a Client")
//     .UseConverter(s => $"{s} test")
//     .PageSize(5)
//     .Mode(SelectionMode.Leaf)
// );
// AnsiConsole.MarkupInterpolated($"selected {selected}");
// return;
try
{
    var start = DateTime.Parse("2022-09-01");
    var end = DateTime.Parse("2022-10-01");

    var coordinator = providor.GetService<ICoordinator>();
    var requiresSync = await coordinator.SyncFor(FSharpOption<string>.None, start, end);
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