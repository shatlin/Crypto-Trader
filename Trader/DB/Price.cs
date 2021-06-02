using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{
    public class Price : BaseModel
    {
        public int id { get; set; }
        public DateTime date { get; set; }
        public string pair { get; set; }
        public decimal price { get; set; }
    }

    public partial class PriceConfiguration : IEntityTypeConfiguration<Price>
    {
        public void Configure(EntityTypeBuilder<Price> builder)
        {
            builder.Property(e => e.date).HasColumnType("datetime");

            builder.Property(e => e.pair)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(e => e.price).IsRequired().HasColumnType("decimal(18, 9)");
        }

    }

}
