using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

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

        [Column("address", TypeName = "jsonb")]
        public string Address { get; set; } = "{}";

        // Добавьте это свойство для удобной работы с адресом
        [NotMapped]
        public AddressInfo AddressData
        {
            get
            {
                try
                {
                    return JsonSerializer.Deserialize<AddressInfo>(Address) ?? new AddressInfo();
                }
                catch
                {
                    return new AddressInfo();
                }
            }
            set
            {
                Address = JsonSerializer.Serialize(value);
            }
        }

        [Column("created_at", TypeName = "timestamp without time zone")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at", TypeName = "timestamp without time zone")]
        public DateTime? UpdatedAt { get; set; }

        [Column("properties", TypeName = "jsonb")]
        public string Properties { get; set; } = "{}";

        [ForeignKey("Status")]
        [Column("status", TypeName = "integer")]
        public int StatusId { get; set; }

        public virtual Status Status { get; set; }

        [Column("images", TypeName = "jsonb")]
        public string Images { get; set; } = "{}";

        public class PropertyDetailsData
        {
            public string floor { get; set; }
            public string square { get; set; }
            public bool balcony { get; set; }
            public bool elevator { get; set; }
            public bool decoration { get; set; }
            public string room_count { get; set; }
            public string living_area { get; set; }
        }

        [NotMapped]
        public PropertyDetailsData PropertyDetails
        {
            get
            {
                try
                {
                    return JsonSerializer.Deserialize<PropertyDetailsData>(Properties) ?? new PropertyDetailsData();
                }
                catch
                {
                    return new PropertyDetailsData();
                }
            }
            set
            {
                Properties = JsonSerializer.Serialize(value);
            }
        }

        [NotMapped]
        public List<string> ImageUrls
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(Images))
                        return new List<string>();

                    var imageDict = JsonSerializer.Deserialize<Dictionary<string, string>>(Images);
                    return imageDict?.Values.ToList() ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                var dict = new Dictionary<string, string>();
                for (int i = 0; i < value.Count; i++)
                {
                    dict[$"url{i}"] = value[i];
                }
                Images = JsonSerializer.Serialize(dict);
            }
        }

        public virtual ICollection<Transactions> Transactions { get; set; }

        public void AddImageUrl(string imageUrl)
        {
            var urls = ImageUrls;
            urls.Add(imageUrl);
            ImageUrls = urls;
        }

        public void RemoveImageUrl(int index)
        {
            var urls = ImageUrls;
            if (index >= 0 && index < urls.Count)
            {
                urls.RemoveAt(index);
                ImageUrls = urls;
            }
        }

    }

    public class AddressInfo
    {
        public string zip { get; set; }
        public string city { get; set; }
        public string street { get; set; }
        public string house_number { get; set; }
        public string apartment_number { get; set; }
    }
}