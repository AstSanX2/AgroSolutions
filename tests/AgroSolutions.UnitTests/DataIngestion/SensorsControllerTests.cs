using AgroSolutions.DataIngestion.API.Controllers;
using AgroSolutions.DataIngestion.API.DTOs;
using AgroSolutions.DataIngestion.Domain.Entities;
using AgroSolutions.DataIngestion.Domain.Enums;
using AgroSolutions.DataIngestion.Domain.Interfaces;
using AgroSolutions.EventBus.RabbitMQ;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace AgroSolutions.UnitTests.DataIngestion;

public class SensorsControllerTests
{
    [Fact]
    public async Task PostHumidity_DevePublicar2MensagensNoRabbitMQ()
    {
        // Arrange
        var mockRepo = new Mock<ISensorReadingRepository>();
        var mockEventBus = new Mock<IEventBus>();
        var mockLogger = new Mock<ILogger<SensorsController>>();

        var controller = new SensorsController(mockRepo.Object, mockEventBus.Object, mockLogger.Object);

        var request = new SensorReadingRequest("prop1", "plot1", 65.5m, DateTime.UtcNow);

        // Act
        var result = await controller.PostHumidity(request);

        // Assert
        result.Should().BeOfType<CreatedResult>();

        // Verify persist
        mockRepo.Verify(r => r.CreateAsync(It.Is<SensorReading>(
            s => s.PropertyId == "prop1" && s.PlotId == "plot1" && s.SensorType == SensorType.Humidity && s.Value == 65.5m
        )), Times.Once);

        // Verify 2 messages published
        mockEventBus.Verify(e => e.Publish("alert-sensor-queue", It.IsAny<AlertSensorMessage>()), Times.Once);
        mockEventBus.Verify(e => e.Publish("property-sensor-update-queue", It.IsAny<PropertySensorUpdateMessage>()), Times.Once);
    }
}
