using DokterAbdurrahman.Models;

namespace DokterAbdurrahman.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            // Look for any admins.
            if (context.Admins.Any())
            {
                return;   // DB has been seeded
            }

            var admin = new Admin
            {
                Username = "admin",
                Password = "adminpassword" // Change this!
            };

            context.Admins.Add(admin);
            context.SaveChanges();
        }
    }
}
