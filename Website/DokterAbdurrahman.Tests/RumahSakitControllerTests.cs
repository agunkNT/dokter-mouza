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
    public class RumahSakitControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private RumahSakitController GetController(AppDbContext context)
        {
            var controller = new RumahSakitController(context);
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
            context.RumahSakit.Add(new RumahSakit { Nama = "RS 1", IsDeleted = false });
            context.RumahSakit.Add(new RumahSakit { Nama = "RS 2", IsDeleted = true });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<RumahSakit>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_SavesToDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);
            var rs = new RumahSakit { Nama = "RS Baru", Alamat = "Jl. Test" };

            // Act
            var result = await controller.Create(rs);

            // Assert
            var saved = await context.RumahSakit.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("RS Baru", saved.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Edit_Post_UpdatesDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var rs = new RumahSakit { Nama = "RS Lama", Alamat = "Jl. Test" };
            context.RumahSakit.Add(rs);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            rs.Nama = "RS Update";

            // Act
            var result = await controller.Edit(rs.Id, rs);

            // Assert
            var updated = await context.RumahSakit.FindAsync(rs.Id);
            Assert.Equal("RS Update", updated.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_SetsIsDeletedTrue()
        {
            // Arrange
            using var context = GetDbContext();
            var rs = new RumahSakit { Nama = "RS Hapus", IsDeleted = false };
            context.RumahSakit.Add(rs);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.DeleteConfirmed(rs.Id);

            // Assert
            var deleted = await context.RumahSakit.FindAsync(rs.Id);
            Assert.True(deleted.IsDeleted);
            Assert.IsType<RedirectToActionResult>(result);
        }
    }
}
