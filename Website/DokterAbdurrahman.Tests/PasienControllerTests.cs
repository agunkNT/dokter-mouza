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
    public class PasienControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private PasienController GetController(AppDbContext context)
        {
            var controller = new PasienController(context);
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
            context.Pasien.Add(new Pasien { Nama = "User 1", IsDeleted = false });
            context.Pasien.Add(new Pasien { Nama = "User 2", IsDeleted = true });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Pasien>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_SavesToDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);
            var pasien = new Pasien { Nama = "Pasien Baru", NoRM = "P001" };

            // Act
            var result = await controller.Create(pasien);

            // Assert
            var saved = await context.Pasien.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("Pasien Baru", saved.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Edit_Post_UpdatesDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var pasien = new Pasien { Nama = "Pasien Lama", NoRM = "P001" };
            context.Pasien.Add(pasien);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            pasien.Nama = "Pasien Update";

            // Act
            var result = await controller.Edit(pasien.Id, pasien);

            // Assert
            var updated = await context.Pasien.FindAsync(pasien.Id);
            Assert.Equal("Pasien Update", updated.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_SetsIsDeletedTrue()
        {
            // Arrange
            using var context = GetDbContext();
            var pasien = new Pasien { Nama = "Pasien Hapus", IsDeleted = false };
            context.Pasien.Add(pasien);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.DeleteConfirmed(pasien.Id);

            // Assert
            var deleted = await context.Pasien.FindAsync(pasien.Id);
            Assert.True(deleted.IsDeleted);
            Assert.IsType<RedirectToActionResult>(result);
        }
    }
}
