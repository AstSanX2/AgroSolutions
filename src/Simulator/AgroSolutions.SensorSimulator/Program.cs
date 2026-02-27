using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var mongoConnectionString = config["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017/agrosolutions";
var apiUrl = config["DataIngestionApiUrl"] ?? "http://localhost:5000/api/sensors";
var intervalSeconds = int.Parse(config["IntervalSeconds"] ?? "5");
var refreshEveryNCycles = int.Parse(config["RefreshEveryNCycles"] ?? "10");
var alertChance = double.Parse(config["AlertChancePercent"] ?? "15") / 100.0;

Console.WriteLine("=== AgroSolutions Sensor Simulator ===");
Console.WriteLine($"MongoDB: {mongoConnectionString}");
Console.WriteLine($"API URL: {apiUrl}");
Console.WriteLine($"Intervalo: {intervalSeconds}s");
Console.WriteLine($"Refresh properties a cada {refreshEveryNCycles} ciclos");
Console.WriteLine($"Chance de alerta: {alertChance:P0}");
Console.WriteLine("Pressione Ctrl+C para parar.\n");

// MongoDB setup
var mongoClient = new MongoClient(mongoConnectionString);
var databaseName = MongoUrl.Create(mongoConnectionString).DatabaseName ?? "agrosolutions";
var database = mongoClient.GetDatabase(databaseName);
var propertiesCollection = database.GetCollection<FarmPropertyDoc>("properties");

using var httpClient = new HttpClient();
var random = new Random();
var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

// Random walk state per property+plot+sensorType
var sensorState = new Dictionary<string, decimal>();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var iteration = 0;
List<FarmPropertyDoc> properties = new();

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        iteration++;

        // Refresh properties from MongoDB periodically
        if (iteration == 1 || iteration % refreshEveryNCycles == 0)
        {
            Console.WriteLine("[AUTO-DISCOVERY] Buscando properties/plots no MongoDB...");
            try
            {
                properties = await propertiesCollection
                    .Find(p => p.Ativo)
                    .ToListAsync(cts.Token);
                var totalPlots = properties.Sum(p => p.Talhoes.Count);
                Console.WriteLine($"[AUTO-DISCOVERY] Encontradas {properties.Count} properties com {totalPlots} talhoes\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AUTO-DISCOVERY] Erro ao buscar properties: {ex.Message}");
                if (properties.Count == 0)
                {
                    Console.WriteLine("[AUTO-DISCOVERY] Aguardando properties serem cadastradas...\n");
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cts.Token);
                    continue;
                }
            }
        }

        if (properties.Count == 0)
        {
            Console.WriteLine($"[Ciclo {iteration}] Nenhuma property encontrada. Aguardando...");
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cts.Token);
            continue;
        }

        Console.WriteLine($"--- Ciclo {iteration} ({DateTime.Now:HH:mm:ss}) ---");

        foreach (var property in properties)
        {
            foreach (var plot in property.Talhoes)
            {
                var forceAlert = random.NextDouble() < alertChance;

                var humidity = GenerateValue(sensorState, $"{property.Id}:{plot.Id}:humidity",
                    baseMin: 40, baseMax: 70, variation: 5, min: 15, max: 95,
                    forceAlert, alertMin: 8, alertMax: 18, random);

                var temperature = GenerateValue(sensorState, $"{property.Id}:{plot.Id}:temperature",
                    baseMin: 20, baseMax: 32, variation: 2, min: 5, max: 45,
                    forceAlert, alertMin: 36, alertMax: 42, random);

                var rainfall = GenerateValue(sensorState, $"{property.Id}:{plot.Id}:rainfall",
                    baseMin: 0, baseMax: 30, variation: 10, min: 0, max: 80,
                    forceAlert, alertMin: 55, alertMax: 110, random);

                var alertTag = forceAlert ? " [ALERTA]" : "";
                Console.WriteLine($"  Property={property.Nome}, Talhao={plot.Nome}{alertTag}");

                await SendReading(property.Id, plot.Id, "humidity", humidity, apiUrl, httpClient, jsonOptions);
                await SendReading(property.Id, plot.Id, "temperature", temperature, apiUrl, httpClient, jsonOptions);
                await SendReading(property.Id, plot.Id, "rainfall", rainfall, apiUrl, httpClient, jsonOptions);
            }
        }

        Console.WriteLine();
        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cts.Token);
    }
}
catch (TaskCanceledException)
{
    Console.WriteLine("\nSimulador encerrado.");
}

static decimal GenerateValue(
    Dictionary<string, decimal> state, string key,
    decimal baseMin, decimal baseMax, decimal variation,
    decimal min, decimal max,
    bool forceAlert, decimal alertMin, decimal alertMax,
    Random random)
{
    if (forceAlert)
    {
        var alertValue = Math.Round((decimal)(random.NextDouble() * (double)(alertMax - alertMin)) + alertMin, 1);
        state[key] = alertValue;
        return alertValue;
    }

    if (!state.TryGetValue(key, out var current))
    {
        // Initialize with random value in base range
        current = Math.Round((decimal)(random.NextDouble() * (double)(baseMax - baseMin)) + baseMin, 1);
        state[key] = current;
        return current;
    }

    // Random walk
    var delta = (decimal)((random.NextDouble() * 2 - 1) * (double)variation);
    var next = Math.Round(Math.Clamp(current + delta, min, max), 1);
    state[key] = next;
    return next;
}

static async Task SendReading(string propertyId, string plotId, string type, decimal value,
    string apiUrl, HttpClient client, JsonSerializerOptions options)
{
    var payload = new
    {
        propertyId,
        plotId,
        value,
        timestamp = DateTime.UtcNow
    };

    try
    {
        var response = await client.PostAsJsonAsync($"{apiUrl}/{type}", payload, options);
        var status = response.IsSuccessStatusCode ? "OK" : $"ERRO ({response.StatusCode})";
        Console.WriteLine($"    [{type.ToUpper()}] {value:F1} -> {status}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    [{type.ToUpper()}] FALHA: {ex.Message}");
    }
}

// Minimal MongoDB document classes (mirror of Property domain)
[BsonIgnoreExtraElements]
public class FarmPropertyDoc
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;

    [BsonElement("ativo")]
    public bool Ativo { get; set; } = true;

    [BsonElement("talhoes")]
    public List<PlotDoc> Talhoes { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class PlotDoc
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("nome")]
    public string Nome { get; set; } = string.Empty;
}
