using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kursovaiya_Yakovlev
{
    [Table("staff", Schema = "dbpr")]
    public class Staff
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("id_auth", TypeName = "integer")]
        [ForeignKey("Users")]
        public int UserId { get; set; }

        [Column("post", TypeName = "integer")]
        public int Post { get; set; }

        [Column("contract_number", TypeName = "character varying(50)")]
        [Required]
        [StringLength(50)]
        public string ContractNumber { get; set; }

        public virtual Users Users { get; set; }
    }
}