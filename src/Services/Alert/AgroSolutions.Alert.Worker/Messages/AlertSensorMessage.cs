namespace AgroSolutions.Alert.Worker.Messages;

public record AlertSensorMessage(
    string Id,
    string PropertyId,
    string PlotId,
    string SensorType,
    decimal Value,
    string Unit,
    DateTime Timestamp
);
