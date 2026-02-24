using System.Security.Claims;
using AgroSolutions.Property.API.Controllers;
using AgroSolutions.Property.API.DTOs;
using AgroSolutions.Property.Domain.Entities;
using AgroSolutions.Property.Domain.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AgroSolutions.UnitTests.Property;

public class PropertiesControllerTests
{
    [Fact]
    public async Task GetById_DeveRetornar404_QuandoPropriedadeDeOutroUsuario()
    {
        // Arrange
        var mockRepo = new Mock<IPropertyRepository>();
        mockRepo.Setup(r => r.GetByIdAsync("prop-1"))
            .ReturnsAsync(new FarmProperty
            {
                Id = "prop-1",
                Nome = "Fazenda Alheia",
                ProprietarioId = "outro-usuario-id"
            });

        var controller = new PropertiesController(mockRepo.Object);

        // Simula usuario autenticado com ID diferente
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "meu-usuario-id")
                }, "test"))
            }
        };

        // Act
        var result = await controller.GetById("prop-1");

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
