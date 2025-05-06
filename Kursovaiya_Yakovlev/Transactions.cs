using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kursovaiya_Yakovlev
{
    [Table("transactions", Schema = "dbpr")]
    public class Transactions
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("property_id")]
        [ForeignKey("Property")]
        public int PropertyId { get; set; }

        [Column("service_id")]
        [ForeignKey("Service")]
        public int ServiceId { get; set; }

        [Column("owner_id")]
        [ForeignKey("Owner")]
        public int OwnerId { get; set; }

        [Column("client_id")]
        [ForeignKey("Client")]
        public int ClientId { get; set; }

        [Column("transaction_date", TypeName = "timestamp without time zone")]
        public DateTime TransactionDate { get; set; }

        [Column("amount", TypeName = "numeric")]
        public decimal Amount { get; set; }

        [Column("status")]
        [ForeignKey("Status")]
        public int StatusId { get; set; }

        // Навигационные свойства
        public virtual HouseData Property { get; set; }
        public virtual Service Service { get; set; }
        public virtual Users Owner { get; set; }
        public virtual Users Client { get; set; }
        public virtual Status Status { get; set; }
    }
}