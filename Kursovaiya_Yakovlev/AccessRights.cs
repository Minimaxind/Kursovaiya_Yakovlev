using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kursovaiya_Yakovlev
{
    [Table("access_rights", Schema = "dbpr")]
    public class AccessRights
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("title", TypeName = "character varying(50)")]
        [Required]
        [StringLength(50)]
        public string Title { get; set; }

        
    }
}