using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DokterAbdurrahman.Services;

namespace DokterAbdurrahman.Controllers
{
    [Authorize]
    public class DriveController : Controller
    {
        private readonly GoogleDriveService _driveService;
        private readonly ILogger<DriveController> _logger;

        public DriveController(GoogleDriveService driveService, ILogger<DriveController> logger)
        {
            _driveService = driveService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            try
            {
                var files = _driveService.ListFiles();
                return View(files);
            }
            catch (Exception ex)
            {
                // This might happen if credentials.json is missing or token is invalid
                ViewBag.Error = ex.Message;
                return View(new List<Google.Apis.Drive.v3.Data.File>());
            }
        }

        [HttpPost]
        public IActionResult Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                try
                {
                    _driveService.UploadFile(file);
                    TempData["Message"] = "File uploaded successfully!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Upload failed: " + ex.Message;
                }
            }
            else
            {
                TempData["Error"] = "Please select a file to upload.";
            }

            return RedirectToAction("Index");
        }
    }
}
