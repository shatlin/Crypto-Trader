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
        public bool ClimbingFast { get; set; }
        public bool ClimbedHigh { get; set; }
        public bool SuperHigh { get; set; }
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
        public decimal CurrentPrice { get; set; }

        public decimal FiveMinChange { get; set; }
        public decimal TenMinChange { get; set; }
        public decimal FifteenMinChange { get; set; }
        public decimal ThirtyMinChange { get; set; }
        public decimal FourtyFiveMinChange { get; set; }
        public decimal OneHourChange { get; set; }
        public decimal FourHourChange { get; set; }
        public decimal TwentyFourHourChange { get; set; } //24 hour change
        public decimal FortyEightHourChange { get; set; } //48 hour change
        public decimal OneWeekChange { get; set; } // 7 day change

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
            builder.Property(e => e.CurrentPrice).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.FiveMinChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TenMinChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.FifteenMinChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.ThirtyMinChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.FourtyFiveMinChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.OneHourChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.FourHourChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TwentyFourHourChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.FortyEightHourChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.OneWeekChange).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.PrecisionDecimals).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.MarketCap).IsRequired().HasColumnType("decimal(30, 12)");
        }

    }
}
