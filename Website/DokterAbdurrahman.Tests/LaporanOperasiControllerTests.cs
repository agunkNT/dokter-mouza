using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using DokterAbdurrahman.Controllers;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;
using DokterAbdurrahman.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DokterAbdurrahman.Tests
{
    public class LaporanOperasiControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private LaporanOperasiController GetController(AppDbContext context)
        {
            // Mocks
            var loggerMock = new Mock<ILogger<LaporanOperasiController>>();
            var localMediaMock = new Mock<LocalMediaService>();
            var cacheMock = new Mock<IMemoryCache>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            
            var controller = new LaporanOperasiController(context, loggerMock.Object, localMediaMock.Object, cacheMock.Object, scopeFactoryMock.Object);
            
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
        public async Task Create_SavesWithGOSAndNoRM()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);

            var laporan = new LaporanOperasi
            {
                Tanggal = DateTime.Now,
                Code = "OP-001",
                NoRM = "RM-Hosp-01",
                GOS = 5,
                Keterangan = "Normal"
            };

            // Act
            var result = await controller.Create(laporan, null, null, null, null);

            // Assert
            var saved = await context.LaporanOperasi.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("RM-Hosp-01", saved.NoRM);
            Assert.Equal(5, saved.GOS);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Index_FiltersByYear()
        {
            // Arrange
            using var context = GetDbContext();
            context.LaporanOperasi.Add(new LaporanOperasi { Tanggal = new DateTime(2023, 1, 1), Code = "1" });
            context.LaporanOperasi.Add(new LaporanOperasi { Tanggal = new DateTime(2024, 1, 1), Code = "2" });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.Index(2024, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<LaporanOperasi>>(viewResult.ViewData.Model);
            Assert.Single(model);
            Assert.Equal("2", model.First().Code);
        }
    }
}
