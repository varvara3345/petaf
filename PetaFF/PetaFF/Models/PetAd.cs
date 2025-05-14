using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PetaFF.Models;

namespace PetaFF.Models
{
    public class PetAd
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Пожалуйста, введите имя животного")]
        [Display(Name = "Имя животного")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Пожалуйста, выберите тип животного")]
        [Display(Name = "Тип животного")]
        public string Type { get; set; }

        [Required(ErrorMessage = "Пожалуйста, введите описание")]
        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Display(Name = "Фото")]
        public string PhotoPath { get; set; }

        [Required(ErrorMessage = "Пожалуйста, введите улицу")]
        [Display(Name = "Улица")]
        public string Street { get; set; }

        [Required(ErrorMessage = "Пожалуйста, введите контактный телефон")]
        [Display(Name = "Контактный телефон")]
        public string ContactPhone { get; set; }

        [Display(Name = "Статус")]
        public PetStatus Status { get; set; } = PetStatus.InSearch;

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
    }

    public enum PetStatus
    {
        [Display(Name = "В поиске")]
        InSearch,
        [Display(Name = "Найдено")]
        Found,
        [Display(Name = "Временный приют")]
        TemporaryShelter,
        [Display(Name = "Передано в приют")]
        InShelter
    }
} 