using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetaFF.Models
{
    public class Like
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public int PetAdId { get; set; }

        [ForeignKey("PetAdId")]
        public PetAd PetAd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 