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
        public string Pair { get; set; }
        public string CoinName { get; set; }
        public string CoinSymbol { get; set; }
        public int TradePrecision { get; set; }
        public bool IsIncludedForTrading { get; set; }
        public decimal PercBelowDayHighToBuy { get; set; }
        public decimal PercAboveDayLowToSell { get; set; }
        public bool ForceBuy { get; set; }
        public int Rank { get; set; }
        public decimal DayTradeCount { get; set; }
        public decimal DayVolume { get; set; }
        public decimal DayVolumeUSDT { get; set; }
        public decimal DayOpenPrice { get; set; }
        public decimal DayHighPrice { get; set; }
        public decimal DayLowPrice { get; set; }
        public decimal FiveMinChange { get; set; }
        public decimal TenMinChange { get; set; }
        public decimal FifteenMinChange { get; set; }
        public decimal ThirtyMinChange { get; set; }
        public decimal OneHourChange { get; set; }
        public decimal FourHourChange { get; set; }
        public decimal OneDayChange { get; set; }
        public decimal TwoDayChange { get; set; }
        public decimal OneWeekChange { get; set; }
        public decimal PrecisionDecimals { get; set; }
        public decimal MarketCap { get; set; }
        public string TradeSuggestion { get; set; }
    }

    public partial class CoinConfiguration : IEntityTypeConfiguration<MyCoins>
    {
        public void Configure(EntityTypeBuilder<MyCoins> builder)
        {
            builder.Property(e => e.PercBelowDayHighToBuy).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.PercAboveDayLowToSell).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DayTradeCount).IsRequired().HasColumnType("decimal(30, 12)");

            builder.Property(e => e.DayVolume).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DayVolumeUSDT).IsRequired().HasColumnType("decimal(30, 12)");


            builder.Property(e => e.DayOpenPrice).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DayHighPrice).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DayLowPrice).IsRequired().HasColumnType("decimal(30, 12)");

            builder.Property(e => e.FiveMinChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TenMinChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.FifteenMinChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.ThirtyMinChange).IsRequired().HasColumnType("decimal(30, 12)");

            builder.Property(e => e.OneHourChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.FourHourChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.OneDayChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TwoDayChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.OneWeekChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.PrecisionDecimals).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.MarketCap).IsRequired().HasColumnType("decimal(30, 12)");
        }

    }
}
