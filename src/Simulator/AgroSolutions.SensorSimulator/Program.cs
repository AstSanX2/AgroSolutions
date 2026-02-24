using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var apiUrl = config["DataIngestionApiUrl"] ?? "http://localhost:5000/api/sensors";
var intervalSeconds = int.Parse(config["IntervalSeconds"] ?? "30");
var simulateAlerts = bool.Parse(config["SimulateAlertScenarios"] ?? "false");
var sensors = config.GetSection("Sensors").Get<List<SensorConfig>>() ?? new();

if (sensors.Count == 0)
{
    Console.WriteLine("[ERRO] Nenhum sensor configurado em appsettings.json");
    Console.WriteLine("Configure PropertyId e PlotId antes de executar.");
    return;
}

Console.WriteLine("=== AgroSolutions Sensor Simulator ===");
Console.WriteLine($"API URL: {apiUrl}");
Console.WriteLine($"Intervalo: {intervalSeconds}s");
Console.WriteLine($"Sensores configurados: {sensors.Count}");
Console.WriteLine($"Simular alertas: {simulateAlerts}");
Console.WriteLine("Pressione Ctrl+C para parar.\n");

using var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl) };
var random = new Random();
var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var iteration = 0;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        iteration++;
        Console.WriteLine($"--- Iteracao {iteration} ({DateTime.Now:HH:mm:ss}) ---");

        foreach (var sensor in sensors)
        {
            await SendReading(sensor, "humidity", GenerateHumidity(simulateAlerts, iteration, random), httpClient, jsonOptions);
            await SendReading(sensor, "temperature", GenerateTemperature(simulateAlerts, random), httpClient, jsonOptions);
            await SendReading(sensor, "rainfall", GenerateRainfall(random), httpClient, jsonOptions);
        }

        Console.WriteLine();
        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cts.Token);
    }
}
catch (TaskCanceledException)
{
    Console.WriteLine("\nSimulador encerrado.");
}

static async Task SendReading(SensorConfig sensor, string type, decimal value, HttpClient client, JsonSerializerOptions options)
{
    var payload = new
    {
        propertyId = sensor.PropertyId,
        plotId = sensor.PlotId,
        value,
        timestamp = DateTime.UtcNow
    };

    try
    {
        var response = await client.PostAsJsonAsync($"/{type}", payload, options);
        var status = response.IsSuccessStatusCode ? "OK" : $"ERRO ({response.StatusCode})";
        Console.WriteLine($"  [{type.ToUpper()}] {value:F1} -> {status}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [{type.ToUpper()}] FALHA: {ex.Message}");
    }
}

static decimal GenerateHumidity(bool simulateAlerts, int iteration, Random random)
{
    // If simulating alerts, alternate between dry periods
    if (simulateAlerts && iteration % 10 < 5)
        return Math.Round((decimal)(random.NextDouble() * 25 + 5), 1);  // 5-30% (low)

    return Math.Round((decimal)(random.NextDouble() * 70 + 20), 1);  // 20-90%
}

static decimal GenerateTemperature(bool simulateAlerts, Random random)
{
    if (simulateAlerts)
        return Math.Round((decimal)(random.NextDouble() * 10 + 33), 1);  // 33-43 C (high)

    return Math.Round((decimal)(random.NextDouble() * 25 + 15), 1);  // 15-40 C
}

static decimal GenerateRainfall(Random random)
{
    return Math.Round((decimal)(random.NextDouble() * 50), 1);  // 0-50 mm
}

public class SensorConfig
{
    public string PropertyId { get; set; } = string.Empty;
    public string PlotId { get; set; } = string.Empty;
}
