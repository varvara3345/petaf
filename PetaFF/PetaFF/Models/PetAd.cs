using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PetaFF.Models
{
    public class PetAd
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Пожалуйста, введите имя питомца")]
        [Display(Name = "Имя питомца")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Пожалуйста, выберите тип питомца")]
        [Display(Name = "Тип питомца")]
        public string Type { get; set; }

        [Required(ErrorMessage = "Пожалуйста, введите описание")]
        [Display(Name = "Описание")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Пожалуйста, выберите статус")]
        [Display(Name = "Статус")]
        public PetStatus Status { get; set; }

        [Required(ErrorMessage = "Пожалуйста, введите адрес")]
        [Display(Name = "Адрес")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Пожалуйста, выберите район")]
        [Display(Name = "Район")]
        public string District { get; set; }

        [Required(ErrorMessage = "Пожалуйста, введите контактный телефон")]
        [Display(Name = "Контактный телефон")]
        [Phone(ErrorMessage = "Некорректный формат телефона")]
        public string ContactPhone { get; set; }

        [Display(Name = "Дата пропажи")]
        public DateTime? DateLost { get; set; }

        [Display(Name = "Место, где видели в последний раз")]
        public string? LastSeenAddress { get; set; }

        [Display(Name = "Фото")]
        public string? PhotoPath { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public ICollection<Comment> Comments { get; set; }
        public ICollection<Like> Likes { get; set; }
        public ICollection<Favorite> Favorites { get; set; }

        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
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