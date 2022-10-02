using Microsoft.FSharp.Core;
using Newtonsoft.Json;
using syncer;

namespace clockSync;

public class FileReader : IFileReader
{
    class Schema
    {
        public string ClockifyKey { get; set; }
        public Dictionary<string, string> SeriesToProject { get; set; } = new();
    }

    private readonly string _path;
    private readonly Schema _config;

    public FileReader(string path = "./config.json")
    {
        _path = path;
        if (Path.Exists(path))
        {
            using var r = new StreamReader(path);
            string json = r.ReadToEnd();
            _config = JsonConvert.DeserializeObject<Schema>(json) ?? new Schema();
        }
        else
        {
            _config = new Schema();
        }
    }

    public Task<FSharpOption<string>> fetchIfExists(string key)
    {
        return Task.FromResult(_config.SeriesToProject.ContainsKey(key)
            ? FSharpOption<string>.Some(_config.SeriesToProject[key])
            : FSharpOption<string>.None);
    }

    private async Task UpdateFile()
    {
        var json = JsonConvert.SerializeObject(_config);
        var options = new FileStreamOptions
        {
            Mode = FileMode.Create,
        };
        await using (var w = new StreamWriter(_path))
        {
            await w.WriteAsync(json);
            await w.FlushAsync();
        }
    }

    public async Task<Unit> upsertValue(string key, string value)
    {
        try
        {
            _config.SeriesToProject[key] = value;
            await UpdateFile();
            return (Unit)Activator.CreateInstance(typeof(Unit), true);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            throw;
        }
    }
}