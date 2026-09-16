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
using Moq;
using Xunit;

namespace DokterAbdurrahman.Tests
{
    public class DokterControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private DokterController GetController(AppDbContext context)
        {
            var controller = new DokterController(context);
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
        public async Task Index_ReturnsViewWithList()
        {
            // Arrange
            using var context = GetDbContext();
            context.Dokter.Add(new Dokter { Nama = "Dr. Tono", IsDeleted = false });
            context.Dokter.Add(new Dokter { Nama = "Dr. Tini", IsDeleted = true });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Dokter>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_SavesToDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);
            var dokter = new Dokter { Nama = "Dr. Baru", Spesialisasi = "Anestesi" };

            // Act
            var result = await controller.Create(dokter);

            // Assert
            var saved = await context.Dokter.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("Dr. Baru", saved.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Edit_Post_UpdatesDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var dokter = new Dokter { Nama = "Dr. Lama", Spesialisasi = "Urologi" };
            context.Dokter.Add(dokter);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            dokter.Nama = "Dr. Update";

            // Act
            var result = await controller.Edit(dokter.Id, dokter);

            // Assert
            var updated = await context.Dokter.FindAsync(dokter.Id);
            Assert.Equal("Dr. Update", updated.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_SetsIsDeletedTrue()
        {
            // Arrange
            using var context = GetDbContext();
            var dokter = new Dokter { Nama = "Dr. Pensiun", IsDeleted = false };
            context.Dokter.Add(dokter);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.DeleteConfirmed(dokter.Id);

            // Assert
            var deleted = await context.Dokter.FindAsync(dokter.Id);
            Assert.True(deleted.IsDeleted);
            Assert.IsType<RedirectToActionResult>(result);
        }
    }
}
