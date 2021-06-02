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
        public decimal BoughtPrice { get; set; }
        public decimal CurrentPrice { get; set; }

        public decimal AverageBuyingCoinPrice { get; set; }
        public decimal CurrentCoinPrice { get; set; }
        public decimal Difference { get; set; }
        public decimal DifferencePercentage { get; set; }
    }

    public partial class MyBalanceConfiguration : IEntityTypeConfiguration<Balance>
    {
        public void Configure(EntityTypeBuilder<Balance> builder)
        {
            builder.Property(e => e.Free).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.Locked).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.BoughtPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.CurrentPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.AverageBuyingCoinPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.CurrentCoinPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.Difference).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.DifferencePercentage).IsRequired().HasColumnType("decimal(18, 9)");
        }

    }
}