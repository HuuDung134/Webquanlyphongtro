using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnCoSo.Models
{
    public class GiaNuoc
    {
        [Key]
        public int MaGiaNuoc { get; set; }

        [Required]
        public int BacNuoc { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Required]
        public decimal GiaTienNuoc { get; set; }

        [Required]
        public int TuSoNuoc { get; set; }

        [Required]
        public int DenSoNuoc { get; set; }
    }
} 