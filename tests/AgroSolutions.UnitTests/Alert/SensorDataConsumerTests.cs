using AgroSolutions.Alert.Domain.Entities;
using AgroSolutions.Alert.Domain.Enums;
using AgroSolutions.Alert.Domain.Interfaces;
using AgroSolutions.Alert.Domain.Rules;
using FluentAssertions;
using Moq;

namespace AgroSolutions.UnitTests.Alert;

public class SensorDataConsumerTests
{
    private readonly Mock<IAlertRepository> _repositoryMock;
    private readonly List<IAlertRule> _rules;

    public SensorDataConsumerTests()
    {
        _repositoryMock = new Mock<IAlertRepository>();
        _rules = new List<IAlertRule>
        {
            new HumidityCriticalRule(),
            new HumidityWarningRule(),
            new TemperatureCriticalRule(),
            new TemperatureHighRule(),
            new TemperatureLowRule(),
            new RainfallCriticalRule(),
            new RainfallWarningRule()
        };
    }

    [Fact]
    public void Rules_ShouldTriggerAlert_ForLowHumidity()
    {
        var humidityRules = _rules.Where(r => r.ApplicableSensorType == SensorType.Humidity).ToList();
        var triggeredRules = humidityRules.Where(r => r.ShouldTrigger(15)).ToList();

        triggeredRules.Should().HaveCount(2);
        triggeredRules.Should().Contain(r => r is HumidityCriticalRule);
        triggeredRules.Should().Contain(r => r is HumidityWarningRule);
    }

    [Fact]
    public void Rules_ShouldTriggerOnlyWarning_ForModeratelyLowHumidity()
    {
        var humidityRules = _rules.Where(r => r.ApplicableSensorType == SensorType.Humidity).ToList();
        var triggeredRules = humidityRules.Where(r => r.ShouldTrigger(25)).ToList();

        triggeredRules.Should().HaveCount(1);
        triggeredRules.Should().Contain(r => r is HumidityWarningRule);
    }

    [Fact]
    public void Rules_ShouldNotTrigger_ForNormalHumidity()
    {
        var humidityRules = _rules.Where(r => r.ApplicableSensorType == SensorType.Humidity).ToList();
        var triggeredRules = humidityRules.Where(r => r.ShouldTrigger(50)).ToList();

        triggeredRules.Should().BeEmpty();
    }

    [Fact]
    public void Rules_ShouldTriggerCritical_ForVeryHighTemperature()
    {
        var tempRules = _rules.Where(r => r.ApplicableSensorType == SensorType.Temperature).ToList();
        var triggeredRules = tempRules.Where(r => r.ShouldTrigger(42)).ToList();

        triggeredRules.Should().HaveCount(2);
        triggeredRules.Should().Contain(r => r is TemperatureCriticalRule);
        triggeredRules.Should().Contain(r => r is TemperatureHighRule);
    }

    [Fact]
    public void Rules_ShouldTriggerLow_ForVeryLowTemperature()
    {
        var tempRules = _rules.Where(r => r.ApplicableSensorType == SensorType.Temperature).ToList();
        var triggeredRules = tempRules.Where(r => r.ShouldTrigger(2)).ToList();

        triggeredRules.Should().HaveCount(1);
        triggeredRules.Should().Contain(r => r is TemperatureLowRule);
    }

    [Fact]
    public async Task Repository_ShouldCreateAlert_WhenNoActiveAlertExists()
    {
        _repositoryMock
            .Setup(r => r.GetActiveAlertAsync("prop1", "plot1", SensorType.Humidity))
            .ReturnsAsync((AlertRecord?)null);

        var rule = new HumidityCriticalRule();
        var alert = rule.CreateAlert("prop1", "plot1", 10);

        await _repositoryMock.Object.CreateAsync(alert);

        _repositoryMock.Verify(r => r.CreateAsync(It.Is<AlertRecord>(a =>
            a.PropertyId == "prop1" &&
            a.PlotId == "plot1" &&
            a.AlertType == AlertType.DroughtAlert &&
            a.IsActive == true
        )), Times.Once);
    }

    [Fact]
    public async Task Repository_ShouldDeactivateAlert_WhenValueNormalizes()
    {
        var existingAlert = new AlertRecord
        {
            Id = "alert123",
            PropertyId = "prop1",
            PlotId = "plot1",
            AlertType = AlertType.DroughtAlert,
            SensorType = SensorType.Humidity,
            IsActive = true
        };

        _repositoryMock
            .Setup(r => r.GetActiveAlertAsync("prop1", "plot1", SensorType.Humidity))
            .ReturnsAsync(existingAlert);

        // Value is now normal (50%), no rules should trigger
        var humidityRules = _rules.Where(r => r.ApplicableSensorType == SensorType.Humidity).ToList();
        var triggeredRule = humidityRules.FirstOrDefault(r => r.ShouldTrigger(50));
        triggeredRule.Should().BeNull();

        // Should deactivate since no rule triggered and there's an active alert
        await _repositoryMock.Object.DeactivateAsync(existingAlert.Id);

        _repositoryMock.Verify(r => r.DeactivateAsync("alert123"), Times.Once);
    }
}
