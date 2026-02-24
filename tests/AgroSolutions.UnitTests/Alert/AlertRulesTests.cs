using AgroSolutions.Alert.Domain.Enums;
using AgroSolutions.Alert.Domain.Rules;
using FluentAssertions;

namespace AgroSolutions.UnitTests.Alert;

public class AlertRulesTests
{
    [Theory]
    [InlineData(15, true)]
    [InlineData(19, true)]
    [InlineData(20, false)]
    [InlineData(50, false)]
    public void HumidityCriticalRule_ShouldTrigger_WhenValueBelow20(decimal value, bool expected)
    {
        var rule = new HumidityCriticalRule();
        rule.ApplicableSensorType.Should().Be(SensorType.Humidity);
        rule.ShouldTrigger(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(25, true)]
    [InlineData(29, true)]
    [InlineData(30, false)]
    [InlineData(50, false)]
    public void HumidityWarningRule_ShouldTrigger_WhenValueBelow30(decimal value, bool expected)
    {
        var rule = new HumidityWarningRule();
        rule.ShouldTrigger(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(41, true)]
    [InlineData(45, true)]
    [InlineData(40, false)]
    [InlineData(30, false)]
    public void TemperatureCriticalRule_ShouldTrigger_WhenValueAbove40(decimal value, bool expected)
    {
        var rule = new TemperatureCriticalRule();
        rule.ApplicableSensorType.Should().Be(SensorType.Temperature);
        rule.ShouldTrigger(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(36, true)]
    [InlineData(39, true)]
    [InlineData(35, false)]
    [InlineData(25, false)]
    public void TemperatureHighRule_ShouldTrigger_WhenValueAbove35(decimal value, bool expected)
    {
        var rule = new TemperatureHighRule();
        rule.ShouldTrigger(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(0, true)]
    [InlineData(-5, true)]
    [InlineData(5, false)]
    [InlineData(10, false)]
    public void TemperatureLowRule_ShouldTrigger_WhenValueBelow5(decimal value, bool expected)
    {
        var rule = new TemperatureLowRule();
        rule.ShouldTrigger(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(101, true)]
    [InlineData(150, true)]
    [InlineData(100, false)]
    [InlineData(50, false)]
    public void RainfallCriticalRule_ShouldTrigger_WhenValueAbove100(decimal value, bool expected)
    {
        var rule = new RainfallCriticalRule();
        rule.ApplicableSensorType.Should().Be(SensorType.Rainfall);
        rule.ShouldTrigger(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(51, true)]
    [InlineData(80, true)]
    [InlineData(50, false)]
    [InlineData(30, false)]
    public void RainfallWarningRule_ShouldTrigger_WhenValueAbove50(decimal value, bool expected)
    {
        var rule = new RainfallWarningRule();
        rule.ShouldTrigger(value).Should().Be(expected);
    }

    [Fact]
    public void HumidityCriticalRule_CreateAlert_ShouldReturnCorrectAlert()
    {
        var rule = new HumidityCriticalRule();
        var alert = rule.CreateAlert("prop1", "plot1", 15);

        alert.PropertyId.Should().Be("prop1");
        alert.PlotId.Should().Be("plot1");
        alert.AlertType.Should().Be(AlertType.DroughtAlert);
        alert.Message.Should().Be("Alerta de Seca Critico");
        alert.SensorType.Should().Be(SensorType.Humidity);
        alert.SensorValue.Should().Be(15);
        alert.Threshold.Should().Be(20);
        alert.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TemperatureCriticalRule_CreateAlert_ShouldReturnCorrectAlert()
    {
        var rule = new TemperatureCriticalRule();
        var alert = rule.CreateAlert("prop1", "plot1", 42);

        alert.PropertyId.Should().Be("prop1");
        alert.PlotId.Should().Be("plot1");
        alert.AlertType.Should().Be(AlertType.HighTemperature);
        alert.Message.Should().Be("Temperatura Critica");
        alert.SensorValue.Should().Be(42);
        alert.Threshold.Should().Be(40);
    }

    [Fact]
    public void RainfallCriticalRule_CreateAlert_ShouldReturnCorrectAlert()
    {
        var rule = new RainfallCriticalRule();
        var alert = rule.CreateAlert("prop1", "plot1", 120);

        alert.AlertType.Should().Be(AlertType.HeavyRain);
        alert.Message.Should().Be("Chuva Muito Intensa");
        alert.SensorValue.Should().Be(120);
        alert.Threshold.Should().Be(100);
    }
}
