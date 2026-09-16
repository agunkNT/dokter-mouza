using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DokterAbdurrahman.Controllers;
using DokterAbdurrahman.Data;
using DokterAbdurrahman.Models;
using DokterAbdurrahman.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace DokterAbdurrahman.Tests
{
    public class PemeriksaanControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private Mock<LocalMediaService> GetMockLocalMedia()
        {
            return new Mock<LocalMediaService>();
        }

        private Mock<ILogger<PemeriksaanController>> GetMockLogger()
        {
            return new Mock<ILogger<PemeriksaanController>>();
        }

        private PemeriksaanController GetController(AppDbContext context, LocalMediaService localMedia = null, ILogger<PemeriksaanController> logger = null)
        {
            localMedia ??= GetMockLocalMedia().Object;
            logger ??= GetMockLogger().Object;

            var controller = new PemeriksaanController(context, localMedia, logger);

            // Set up mock user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "TestUser"),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            var urlHelperMock = new Mock<IUrlHelper>();
            controller.Url = urlHelperMock.Object;
            controller.TempData = new Mock<ITempDataDictionary>().Object;

            return controller;
        }

        [Fact]
        public async Task Create_Post_SavesToDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);

            var pasien = new Pasien { Nama = "Tono", NoRM = "123" };
            context.Pasien.Add(pasien);
            await context.SaveChangesAsync();

            var pemeriksaan = new Pemeriksaan
            {
                JenisPemeriksaan = "KONSULTASI_PRE",
                Tanggal = DateTime.Now,
                PasienId = pasien.Id,
                Keterangan = "Test"
            };

            // Act
            var result = await controller.Create(pemeriksaan, null, null);

            // Assert
            var saved = await context.Pemeriksaan.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("Test", saved.Keterangan);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenIdIsNull()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);

            // Act
            var result = await controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ReturnsView_WhenIdExists()
        {
            // Arrange
            using var context = GetDbContext();
            var pasien = new Pasien { Nama = "Test Pasien", JenisKelamin = "L" };
            context.Pasien.Add(pasien);
            await context.SaveChangesAsync();

            var pemeriksaan = new Pemeriksaan { PasienId = pasien.Id, Keterangan = "View Test", Tanggal = DateTime.Now, JenisPemeriksaan = "KONSULTASI_PRE" };
            context.Pemeriksaan.Add(pemeriksaan);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            var controller = GetController(context);

            // Act
            var result = await controller.Details(pemeriksaan.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Pemeriksaan>(viewResult.ViewData.Model);
            Assert.Equal("View Test", model.Keterangan);
        }
    }
}
