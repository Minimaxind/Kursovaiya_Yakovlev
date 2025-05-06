    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace Kursovaiya_Yakovlev
    {
        [Table("service", Schema = "dbpr")]
        public class Service
        {
            [Key]
            [Column("id")]
            public int Id { get; set; }

            [Column("title", TypeName = "character varying(100)")]
            [Required]
            [StringLength(100)]
            public string Title { get; set; }

            [Column("price", TypeName = "integer")]
            public int Price { get; set; }

            [Column("id_staff")]  // Явно указываем имя столбца в БД
            [ForeignKey("Staff")]
            public int StaffId { get; set; }
            public virtual Staff Staff { get; set; }
        }
    }
