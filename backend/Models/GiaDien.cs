using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class GiaDien
    {
        [Key]
        public int MaGiaDien { get; set; }

        [Required]
        public int BacDien { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal GiaTienDien { get; set; }

        [Required]
        public int TuSoDien { get; set; }

        [Required]
        public int DenSoDien { get; set; }

     
    }
} 