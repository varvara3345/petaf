using System.ComponentModel.DataAnnotations;

namespace PetaFF.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Имя пользователя")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Пароль")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; }
    }
} 