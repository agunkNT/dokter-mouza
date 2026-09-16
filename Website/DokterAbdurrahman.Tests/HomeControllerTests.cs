using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DokterAbdurrahman.Controllers;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DokterAbdurrahman.Tests
{
    public class HomeControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private HomeController GetController(AppDbContext context)
        {
            var controller = new HomeController(context);
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
        public void Index_ReturnsRedirect_WhenNotAuthenticated()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = new HomeController(context);
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            // Act
            var result = controller.Index();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public void Index_ReturnsView_WhenAuthenticated()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);

            // Act
            var result = controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }
    }
}
