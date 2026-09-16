using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using DokterAbdurrahman.Controllers;
using DokterAbdurrahman.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DokterAbdurrahman.Tests
{
    public class LogControllerTests : IDisposable
    {
        private readonly string _tempDirectory;
        private readonly Mock<IWebHostEnvironment> _envMock;

        public LogControllerTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            Directory.CreateDirectory(Path.Combine(_tempDirectory, "logs"));

            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(m => m.ContentRootPath).Returns(_tempDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        private LogController GetController(IWebHostEnvironment env)
        {
            var controller = new LogController(env);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "TestUser"),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            return controller;
        }

        [Fact]
        public void Index_ReturnsLogs_IfFileExists()
        {
            // Arrange
            var date = DateTime.Today;
            var fileName = $"nlog-own-{date:yyyy-MM-dd}.log";
            var logPath = Path.Combine(_tempDirectory, "logs", fileName);
            var logLine = $"{date:yyyy-MM-dd HH:mm:ss.ffff}|1|INFO|Logger|Test Message|url:/home/index|action:Index";
            System.IO.File.WriteAllLines(logPath, new[] { logLine });

            var controller = GetController(_envMock.Object);

            // Act
            var result = controller.Index(date);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<LogViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Logs);
            Assert.Equal("Test Message", model.Logs[0].Message);
        }

        [Fact]
        public void Index_ReturnsEmptyList_IfFileDoesNotExist()
        {
            // Arrange
            var controller = GetController(_envMock.Object);

            // Act
            var result = controller.Index(DateTime.Today.AddDays(-1));

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<LogViewModel>(viewResult.ViewData.Model);
            Assert.Empty(model.Logs);
        }
    }
}
