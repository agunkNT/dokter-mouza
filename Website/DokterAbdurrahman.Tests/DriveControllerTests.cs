using System;
using System.Collections.Generic;
using System.Security.Claims;
using DokterAbdurrahman.Controllers;
using DokterAbdurrahman.Services;
using Google.Apis.Drive.v3.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace DokterAbdurrahman.Tests
{
    public class DriveControllerTests
    {
        private Mock<GoogleDriveService> GetMockDriveService()
        {
            return new Mock<GoogleDriveService>();
        }

        private Mock<ILogger<DriveController>> GetMockLogger()
        {
            return new Mock<ILogger<DriveController>>();
        }

        private DriveController GetController(GoogleDriveService driveService, ILogger<DriveController> logger)
        {
            var controller = new DriveController(driveService, logger);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "TestUser"),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            controller.TempData = new Mock<ITempDataDictionary>().Object;

            return controller;
        }

        [Fact]
        public void Index_ReturnsViewWithFileList()
        {
            // Arrange
            var driveMock = GetMockDriveService();
            var loggerMock = GetMockLogger();
            var expectedFiles = new List<Google.Apis.Drive.v3.Data.File> { new Google.Apis.Drive.v3.Data.File { Name = "test.jpg" } };
            driveMock.Setup(s => s.ListFiles()).Returns(expectedFiles);

            var controller = GetController(driveMock.Object, loggerMock.Object);

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IList<Google.Apis.Drive.v3.Data.File>>(viewResult.ViewData.Model);
            Assert.Equal(expectedFiles.Count, model.Count);
        }

        [Fact]
        public void Upload_Post_CallsDriveService()
        {
            // Arrange
            var driveMock = GetMockDriveService();
            var loggerMock = GetMockLogger();
            var controller = GetController(driveMock.Object, loggerMock.Object);
            
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(100);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");

            // Act
            var result = controller.Upload(fileMock.Object);

            // Assert
            driveMock.Verify(s => s.UploadFile(It.IsAny<IFormFile>()), Times.Once);
            Assert.IsType<RedirectToActionResult>(result);
        }
    }
}
