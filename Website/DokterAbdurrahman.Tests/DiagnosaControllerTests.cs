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
    public class DiagnosaControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private DiagnosaController GetController(AppDbContext context)
        {
            var controller = new DiagnosaController(context);
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
            context.Diagnosa.Add(new Diagnosa { Nama = "D1", IsDeleted = false });
            context.Diagnosa.Add(new Diagnosa { Nama = "D2", IsDeleted = true });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Diagnosa>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_SavesToDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var controller = GetController(context);
            var diagnosa = new Diagnosa { Nama = "New Diagnosa", Code = "DA001" };

            // Act
            var result = await controller.Create(diagnosa);

            // Assert
            var saved = await context.Diagnosa.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("New Diagnosa", saved.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task Edit_Post_UpdatesDatabase()
        {
            // Arrange
            using var context = GetDbContext();
            var diagnosa = new Diagnosa { Nama = "Old Name", Code = "DA001" };
            context.Diagnosa.Add(diagnosa);
            await context.SaveChangesAsync();

            var controller = GetController(context);
            diagnosa.Nama = "Updated Name";

            // Act
            var result = await controller.Edit(diagnosa.Id, diagnosa);

            // Assert
            var updated = await context.Diagnosa.FindAsync(diagnosa.Id);
            Assert.Equal("Updated Name", updated.Nama);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_SetsIsDeletedTrue()
        {
            // Arrange
            using var context = GetDbContext();
            var diagnosa = new Diagnosa { Nama = "To Delete", IsDeleted = false };
            context.Diagnosa.Add(diagnosa);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.DeleteConfirmed(diagnosa.Id);

            // Assert
            var deleted = await context.Diagnosa.FindAsync(diagnosa.Id);
            Assert.True(deleted.IsDeleted);
            Assert.IsType<RedirectToActionResult>(result);
        }
    }
}
