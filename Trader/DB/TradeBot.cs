﻿using System;
using System.Runtime.Serialization;
using BinanceExchange.API.Converter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace BinanceExchange.API.Models.Response
{
    public class TradeBot
    {
       
        public int Id { get; set; }

        public int Order { get; set; }

        public string Name { get; set; }

        public string Avatar { get; set; }
      
        public string Pair { get; set; }

        public decimal? DayHigh { get; set; }

        public decimal? DayLow { get; set; }

        public decimal? BuyWhenValuePercentageIsBelow { get; set; }

        public decimal? SellWhenProfitPercentageIsAbove { get; set; }

        public decimal? BuyPricePerCoin { get; set; }

        public decimal CurrentPricePerCoin { get; set; }
       
        public decimal? QuantityBought { get; set; }

        public decimal? TotalBuyCost { get; set; }

        public decimal? TotalCurrentValue { get; set; }

        public decimal? TotalCurrentProft { get; set; }

        public decimal? OriginalAllocatedValue { get; set; }

        public bool IsActivelyTrading { get; set; }

        public DateTime? BuyTime { get; set; }

        public DateTime? SellTime { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedTime { get; set; }

    }

    public partial class TradeBotConfiguration : IEntityTypeConfiguration<TradeBot>
    {
        public void Configure(EntityTypeBuilder<TradeBot> builder)
        {
            builder.Property(e => e.DayHigh).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DayLow).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.BuyWhenValuePercentageIsBelow).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.SellWhenProfitPercentageIsAbove).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.BuyPricePerCoin).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.CurrentPricePerCoin).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.QuantityBought).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalBuyCost).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalCurrentValue).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalCurrentProft).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.OriginalAllocatedValue).IsRequired().HasColumnType("decimal(30, 12)");
        }

    }

}
