using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{
    public class MyCoins
    {
        public int Id { get; set; }
        public string Coin { get; set; }
        public int TradePrecision { get; set; }
        public bool IsIncludedForTrading { get; set; }
        public decimal PercBelowDayHighToBuy { get; set; }
        public decimal PercAboveDayLowToSell { get; set; }
        public bool ForceBuy { get; set; }
        public int Rank { get; set; }
        public decimal DayTradeCount { get; set; }
        public decimal DayVolume { get; set; }
    }

    public partial class CoinConfiguration : IEntityTypeConfiguration<MyCoins>
    {
        public void Configure(EntityTypeBuilder<MyCoins> builder)
        {
            builder.Property(e => e.PercBelowDayHighToBuy).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.PercAboveDayLowToSell).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DayTradeCount).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DayVolume).IsRequired().HasColumnType("decimal(30, 12)");
        }

    }
}
