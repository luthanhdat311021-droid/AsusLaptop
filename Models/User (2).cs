using System.ComponentModel.DataAnnotations;

namespace AsusLaptop.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Google OAuth
        public string? GoogleId { get; set; }
        public string? AvatarUrl { get; set; }

        // Facebook OAuth
        public string? FacebookId { get; set; }

        // Face ID
        public string? FaceToken { get; set; }  // Face++ token lưu khuôn mặt đã đăng ký
    }
}
