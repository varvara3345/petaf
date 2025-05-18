using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetaFF.Models
{
    public class Volunteer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Пожалуйста, укажите имя")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Пожалуйста, укажите контакты")]
        public string Contacts { get; set; }

        [Required(ErrorMessage = "Пожалуйста, выберите район")]
        public string Districts { get; set; }

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
} 