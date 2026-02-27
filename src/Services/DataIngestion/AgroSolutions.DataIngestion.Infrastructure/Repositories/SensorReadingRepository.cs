using AgroSolutions.DataIngestion.Domain.Entities;
using AgroSolutions.DataIngestion.Domain.Enums;
using AgroSolutions.DataIngestion.Domain.Interfaces;
using AgroSolutions.DataIngestion.Infrastructure.Data;
using MongoDB.Driver;

namespace AgroSolutions.DataIngestion.Infrastructure.Repositories;

public class SensorReadingRepository : ISensorReadingRepository
{
    private readonly MongoDbContext _context;

    public SensorReadingRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(SensorReading reading)
    {
        await _context.SensorReadings.InsertOneAsync(reading);
    }

    public async Task<List<SensorReading>> GetByPlotAsync(
        string propertyId, string plotId,
        SensorType? sensorType, DateTime? startDate, DateTime? endDate,
        int page, int pageSize)
    {
        var filter = BuildFilter(propertyId, plotId, sensorType, startDate, endDate);

        return await _context.SensorReadings
            .Find(filter)
            .SortByDescending(r => r.Timestamp)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountByPlotAsync(
        string propertyId, string plotId,
        SensorType? sensorType, DateTime? startDate, DateTime? endDate)
    {
        var filter = BuildFilter(propertyId, plotId, sensorType, startDate, endDate);
        return (int)await _context.SensorReadings.CountDocumentsAsync(filter);
    }

    public async Task<List<SensorReading>> GetLatestByPlotAsync(string propertyId, string plotId)
    {
        var results = new List<SensorReading>();

        foreach (var type in Enum.GetValues<SensorType>())
        {
            var latest = await _context.SensorReadings
                .Find(r => r.PropertyId == propertyId && r.PlotId == plotId && r.SensorType == type)
                .SortByDescending(r => r.Timestamp)
                .Limit(1)
                .FirstOrDefaultAsync();

            if (latest != null)
                results.Add(latest);
        }

        return results;
    }

    public async Task<List<SensorReading>> GetByPropertyAsync(
        string propertyId, IEnumerable<string>? plotIds, SensorType? sensorType, int limit)
    {
        var builder = Builders<SensorReading>.Filter;
        var filter = builder.Eq(r => r.PropertyId, propertyId);

        if (plotIds != null)
        {
            var plotIdList = plotIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
            if (plotIdList.Count == 1)
                filter &= builder.Eq(r => r.PlotId, plotIdList[0]);
            else if (plotIdList.Count > 1)
                filter &= builder.In(r => r.PlotId, plotIdList);
        }

        if (sensorType.HasValue)
            filter &= builder.Eq(r => r.SensorType, sensorType.Value);

        return await _context.SensorReadings
            .Find(filter)
            .SortByDescending(r => r.Timestamp)
            .Limit(limit)
            .ToListAsync();
    }

    private static FilterDefinition<SensorReading> BuildFilter(
        string propertyId, string plotId,
        SensorType? sensorType, DateTime? startDate, DateTime? endDate)
    {
        var builder = Builders<SensorReading>.Filter;
        var filter = builder.Eq(r => r.PropertyId, propertyId) & builder.Eq(r => r.PlotId, plotId);

        if (sensorType.HasValue)
            filter &= builder.Eq(r => r.SensorType, sensorType.Value);
        if (startDate.HasValue)
            filter &= builder.Gte(r => r.Timestamp, startDate.Value);
        if (endDate.HasValue)
            filter &= builder.Lte(r => r.Timestamp, endDate.Value);

        return filter;
    }
}
