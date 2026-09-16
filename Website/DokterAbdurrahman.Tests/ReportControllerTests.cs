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
    public class ReportControllerTests
    {
        private AppDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private ReportController GetController(AppDbContext context)
        {
            var controller = new ReportController(context);
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
        public async Task Index_ReturnsViewWithPopulatedModel()
        {
            // Arrange
            using var context = GetDbContext();
            var rs = new RumahSakit { Nama = "Test RS" };
            context.RumahSakit.Add(rs);
            var pasien = new Pasien { Nama = "Test Pasien", JenisKelamin = "L", TanggalLahir = DateTime.Now.AddYears(-30) };
            context.Pasien.Add(pasien);
            var lo = new LaporanOperasi { Pasien = pasien, RumahSakit = rs, Tanggal = DateTime.Now };
            context.LaporanOperasi.Add(lo);
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.Index(null, null, null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ReportViewModel>(viewResult.ViewData.Model);
            Assert.NotEmpty(model.OperationsByMonth);
            Assert.NotEmpty(model.OperationsByHospital);
        }

        [Fact]
        public async Task Index_FiltersByGender()
        {
            // Arrange
            using var context = GetDbContext();
            var rs = new RumahSakit { Nama = "Test RS" };
            var p1 = new Pasien { Nama = "P1", JenisKelamin = "L" };
            var p2 = new Pasien { Nama = "P2", JenisKelamin = "P" };
            context.Pasien.AddRange(p1, p2);
            context.LaporanOperasi.Add(new LaporanOperasi { Pasien = p1, RumahSakit = rs, Tanggal = DateTime.Now });
            context.LaporanOperasi.Add(new LaporanOperasi { Pasien = p2, RumahSakit = rs, Tanggal = DateTime.Now });
            await context.SaveChangesAsync();

            var controller = GetController(context);

            // Act
            var result = await controller.Index("L", null, null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ReportViewModel>(viewResult.ViewData.Model);
            Assert.Equal(1, model.OperationsByHospital.Sum(x => x.Value));
        }
    }
}
