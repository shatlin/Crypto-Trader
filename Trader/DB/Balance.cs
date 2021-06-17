using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using BinanceExchange.API.Models.Response.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BinanceExchange.API.Models.Response
{
 
    public class Balance 
    {


        public int Id { get; set; }
        public string Asset { get; set; }
        public decimal Free { get; set; }
        public decimal Locked { get; set; }
        public decimal TotBoughtPrice { get; set; }
        public decimal TotCurrentPrice { get; set; }

        public decimal AvgBuyCoinPrice { get; set; }
        public decimal CurrCoinPrice { get; set; }
        public decimal Difference { get; set; }
        public decimal DiffPerc { get; set; }
    }

    public partial class MyBalanceConfiguration : IEntityTypeConfiguration<Balance>
    {
        public void Configure(EntityTypeBuilder<Balance> builder)
        {
            builder.Property(e => e.Free).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.Locked).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.TotBoughtPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.TotCurrentPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.AvgBuyCoinPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.CurrCoinPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.Difference).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.DiffPerc).IsRequired().HasColumnType("decimal(18, 9)");
        }

    }
}