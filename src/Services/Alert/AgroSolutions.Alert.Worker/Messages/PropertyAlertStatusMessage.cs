namespace AgroSolutions.Alert.Worker.Messages;

public record PropertyAlertStatusMessage(
    string PropertyId,
    string PlotId,
    string AlertType,
    string Message,
    bool IsActive
);
