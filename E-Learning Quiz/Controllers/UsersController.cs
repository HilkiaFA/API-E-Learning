using E_Learning_Quiz.Data;
using E_Learning_Quiz.Models;
using E_Learning_Quiz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace E_Learning_Quiz.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DBText _context;
        private readonly IPasswordService _passwordService;

        public UsersController(DBText context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            var users = await _context.Users
                .Select(u => new UserResponseDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return users;
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserResponseDto>> GetUser(int id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var userResponse = new UserResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return userResponse;
        }

        // POST: api/Users/Register
        [HttpPost("Register")]
        public async Task<ActionResult<UserResponseDto>> Register(UserRegisterDto userDto)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'DBText.Users' is null.");
            }

            // Cek apakah email sudah terdaftar
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                return BadRequest(new { message = "Email already exists" });
            }

            // Hash password menggunakan SHA256
            var hashedPassword = _passwordService.HashPassword(userDto.Password);

            // Buat user baru
            var user = new Users
            {
                FullName = userDto.FullName,
                Email = userDto.Email,
                PasswordHash = hashedPassword,
                Role = userDto.Role ?? "Student",
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var userResponse = new UserResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, userResponse);
        }

        // POST: api/Users/Login
        [HttpPost("Login")]
        public async Task<ActionResult<UserLoginResponseDto>> Login(UserLoginDto loginDto)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'DBText.Users' is null.");
            }

            // Cari user berdasarkan email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Verify password
            if (!_passwordService.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Login berhasil
            var response = new UserLoginResponseDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Message = "Login successful"
            };

            return Ok(response);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, UserUpdateDto userDto)
        {
            var existingUser = await _context.Users.FindAsync(id);

            if (existingUser == null)
            {
                return NotFound();
            }

            // Update field yang tidak null
            if (!string.IsNullOrEmpty(userDto.FullName))
            {
                existingUser.FullName = userDto.FullName;
            }

            if (!string.IsNullOrEmpty(userDto.Email))
            {
                // Cek apakah email sudah digunakan user lain
                if (await _context.Users.AnyAsync(u => u.Email == userDto.Email && u.UserId != id))
                {
                    return BadRequest(new { message = "Email already exists" });
                }
                existingUser.Email = userDto.Email;
            }

            if (!string.IsNullOrEmpty(userDto.Password))
            {
                // Hash password baru
                existingUser.PasswordHash = _passwordService.HashPassword(userDto.Password);
            }

            if (!string.IsNullOrEmpty(userDto.Role))
            {
                existingUser.Role = userDto.Role;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsersExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users/5/ChangePassword
        [HttpPost("{id}/ChangePassword")]
        public async Task<IActionResult> ChangePassword(int id, ChangePasswordDto changePasswordDto)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Verify old password
            if (!_passwordService.VerifyPassword(changePasswordDto.OldPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Old password is incorrect" });
            }

            // Hash new password
            user.PasswordHash = _passwordService.HashPassword(changePasswordDto.NewPassword);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UsersExists(int id)
        {
            return (_context.Users?.Any(e => e.UserId == id)).GetValueOrDefault();
        }
    }

    // DTOs
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Full Name is required")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password minimum 6 characters")]
        public string Password { get; set; }

        public string? Role { get; set; }
    }

    public class UserLoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }

    public class UserLoginResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Message { get; set; }
    }

    public class UserResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserUpdateDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required]
        public string OldPassword { get; set; }

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }
}