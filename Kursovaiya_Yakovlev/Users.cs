using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kursovaiya_Yakovlev
{
    [Table("users", Schema = "dbpr")]
    public class Users
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("email", TypeName = "character varying(255)")]
        public string email { get; set; }

        [Column("password", TypeName = "text")]
        public string password { get; set; }

        [Column("access_r", TypeName = "integer")]
        public int accessR { get; set; }

        [Column("first_name", TypeName = "character varying(255)")]
        public string firstName { get; set; }

        [Column("last_name", TypeName = "character varying(255)")]
        public string lastName { get; set; }

        [Column("sur_name", TypeName = "character varying(255)")]
        public string surname { get; set; }

        [Column("passport_number", TypeName = "character varying(255)")]
        public string passportNumber { get; set; }

        [Column("phone", TypeName = "character varying(20)")]
        public string phone { get; set; }

        [Column("created_at", TypeName = "timestamp with time zone")]
        public DateTime createdAt { get; set; }

        [Column("updated_at", TypeName = "timestamp with time zone")]
        public DateTime? updatedAt { get; set; }

        public string FullName => $"{lastName} {firstName} {surname}";
    }
}