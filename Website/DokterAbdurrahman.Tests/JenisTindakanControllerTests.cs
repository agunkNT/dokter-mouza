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
    public class JenisTindakanControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private JenisTindakanController GetController(AppDbContext context)
        {
            var controller = new JenisTindakanController(context);
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
            context.JenisTindakan.Add(new JenisTindakan { Nama = "Jenis 1", IsDeleted = false });
            context.JenisTindakan.Add(new JenisTindakan { Nama = "Jenis 2", IsDeleted = true });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<JenisTindakan>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_SavesToDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);
            var jenis = new JenisTindakan { Nama = "Jenis Baru", Code = "JT001" };

            // Act
            var result = await controller.Create(jenis);

            // Assert
            var saved = await context.JenisTindakan.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("Jenis Baru", saved.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Edit_Post_UpdatesDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var jenis = new JenisTindakan { Nama = "Jenis Lama", Code = "JT001" };
            context.JenisTindakan.Add(jenis);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            jenis.Nama = "Jenis Update";

            // Act
            var result = await controller.Edit(jenis.Id, jenis);

            // Assert
            var updated = await context.JenisTindakan.FindAsync(jenis.Id);
            Assert.Equal("Jenis Update", updated.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_SetsIsDeletedTrue()
        {
            // Arrange
            using var context = GetDbContext();
            var jenis = new JenisTindakan { Nama = "Jenis Hapus", IsDeleted = false };
            context.JenisTindakan.Add(jenis);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.DeleteConfirmed(jenis.Id);

            // Assert
            var deleted = await context.JenisTindakan.FindAsync(jenis.Id);
            Assert.True(deleted.IsDeleted);
            Assert.IsType<RedirectToActionResult>(result);
        }
    }
}
