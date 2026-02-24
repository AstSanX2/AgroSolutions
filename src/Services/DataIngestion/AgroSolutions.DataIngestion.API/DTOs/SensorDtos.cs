namespace AgroSolutions.DataIngestion.API.DTOs;

// Request (same for all sensor types)
public record SensorReadingRequest(string PropertyId, string PlotId, decimal Value, DateTime Timestamp);

// Response
public record SensorReadingDto(
    string Id,
    string PropertyId,
    string PlotId,
    string SensorType,
    decimal Value,
    string Unit,
    DateTime Timestamp,
    DateTime ReceivedAt
);

// Paginated response
public record PaginatedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);

// RabbitMQ messages
public record AlertSensorMessage(
    string Id,
    string PropertyId,
    string PlotId,
    string SensorType,
    decimal Value,
    string Unit,
    DateTime Timestamp
);

public record PropertySensorUpdateMessage(
    string PropertyId,
    string PlotId,
    string SensorType,
    decimal Value,
    DateTime Timestamp
);
