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
    public class TindakanControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private TindakanController GetController(AppDbContext context)
        {
            var controller = new TindakanController(context);
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
            context.Tindakan.Add(new Tindakan { Nama = "Tindakan 1", IsDeleted = false });
            context.Tindakan.Add(new Tindakan { Nama = "Tindakan 2", IsDeleted = true });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Tindakan>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_SavesToDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);
            var tindakan = new Tindakan { Nama = "Tindakan Baru", Code = "TI001" };

            // Act
            var result = await controller.Create(tindakan);

            // Assert
            var saved = await context.Tindakan.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("Tindakan Baru", saved.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Edit_Post_UpdatesDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var tindakan = new Tindakan { Nama = "Tindakan Lama", Code = "TI001" };
            context.Tindakan.Add(tindakan);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            tindakan.Nama = "Tindakan Update";

            // Act
            var result = await controller.Edit(tindakan.Id, tindakan);

            // Assert
            var updated = await context.Tindakan.FindAsync(tindakan.Id);
            Assert.Equal("Tindakan Update", updated.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_SetsIsDeletedTrue()
        {
            // Arrange
            using var context = GetDbContext();
            var tindakan = new Tindakan { Nama = "Tindakan Hapus", IsDeleted = false };
            context.Tindakan.Add(tindakan);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.DeleteConfirmed(tindakan.Id);

            // Assert
            var deleted = await context.Tindakan.FindAsync(tindakan.Id);
            Assert.True(deleted.IsDeleted);
            Assert.IsType<RedirectToActionResult>(result);
        }
    }
}
