using System.ComponentModel.DataAnnotations;

namespace PetaFF.Models
{
    public class PetAd
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Кличка")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Вид животного")]
        public string Type { get; set; }

        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Display(Name = "Фото")]
        public string? PhotoPath { get; set; }

        [Required]
        [Display(Name = "Район/место пропажи")]
        public string Location { get; set; }

        [Display(Name = "Контактный телефон")]
        public string ContactPhone { get; set; }
    }
} 