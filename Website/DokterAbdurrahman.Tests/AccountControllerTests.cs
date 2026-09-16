using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DokterAbdurrahman.Controllers;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DokterAbdurrahman.Tests
{
    public class AccountControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private AccountController GetController(AppDbContext context, Mock<IAuthenticationService> authServiceMock = null)
        {
            var controller = new AccountController(context);
            
            var httpContext = new DefaultHttpContext();
            if (authServiceMock != null)
            {
                var serviceProvider = new ServiceCollection()
                    .AddSingleton(authServiceMock.Object)
                    .BuildServiceProvider();
                httpContext.RequestServices = serviceProvider;
            }

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            var urlHelperMock = new Mock<IUrlHelper>();
            controller.Url = urlHelperMock.Object;

            return controller;
        }

        [Fact]
        public async Task Login_Post_RedirectsToHome_OnSuccess()
        {
            // Arrange
            using var context = GetDbContext();
            context.Admins.Add(new Admin { Username = "admin", Password = "password" });
            await context.SaveChangesAsync();

            var authServiceMock = new Mock<IAuthenticationService>();
            var controller = GetController(context, authServiceMock);
            var model = new LoginViewModel { Username = "admin", Password = "password" };

            // Act
            var result = await controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
            authServiceMock.Verify(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
        }

        [Fact]
        public async Task Login_Post_ReturnsView_OnFailure()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);
            var model = new LoginViewModel { Username = "wrong", Password = "wrong" };

            // Act
            var result = await controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Logout_RedirectsToLogin()
        {
            // Arrange
            using var context = GetDbContext();
            var authServiceMock = new Mock<IAuthenticationService>();
            var controller = GetController(context, authServiceMock);

            // Act
            var result = await controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            authServiceMock.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Once);
        }
    }
}
