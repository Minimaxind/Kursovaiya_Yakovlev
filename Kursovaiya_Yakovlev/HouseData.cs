using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Collections.Generic;

namespace Kursovaiya_Yakovlev
{
    [Table("house_data", Schema = "dbpr")]
    public class HouseData
    {
    



        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("title", TypeName = "character varying(100)")]
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Column("description", TypeName = "text")]
        public string Description { get; set; }

        [Column("price", TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

   
        [Column("address", TypeName = "text")] 
        public string Address { get; set; }


        [Column("created_at", TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at", TypeName = "timestamp without time zone")]
        public DateTime? UpdatedAt { get; set; }

        [Column("properties", TypeName = "jsonb")]
        public string Properties { get; set; }

        [Column("status", TypeName = "integer")]
        public int Status { get; set; }

        [Column("images", TypeName = "jsonb")]
        public string? Images { get; set; }

        public virtual ICollection<Transactions> Transactions { get; set; }

    
    }
    public class AddressInfo
    {
        public string zip { get; set; }       // Почтовый индекс
        public string city { get; set; }      // Город
        public string street { get; set; }    // Улица
        public string house_number { get; set; }  // Номер дома
        public string apartment_number { get; set; } // Номер квартиры
    }
}