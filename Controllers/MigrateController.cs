using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HRManagement.API.Data;

namespace HRManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MigrateController : ControllerBase
    {
        private readonly AppDbContext _context;
        public MigrateController(AppDbContext context) => _context = context;

        [HttpPost("hash-passwords")]
        public async Task<IActionResult> HashPasswords()
        {
            var users = await _context.Users.ToListAsync();
            foreach (var user in users)
            {
                if (!user.PasswordHash.StartsWith("$2"))
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = $"{users.Count} users migrated." });
        }

        [HttpPost("reset-testadmin")]
        public async Task<IActionResult> ResetTestAdmin()
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == "testadmin");
            if (user == null) return NotFound(new { message = "User not found" });
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Done! Login with testadmin / Test@123" });
        }

        [HttpPost("reset-admin")]
        public async Task<IActionResult> ResetAdmin()
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == "admin");
            if (user == null) return NotFound(new { message = "User not found" });
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            await _context.SaveChangesAsync();
            return Ok(new { message = "Done! Login with admin / Admin@123" });
        }
    }
}