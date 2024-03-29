﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{

    public class Config : BaseModel
    {
        public int id { get; set; }
        public int MaxPauses { get; set; }
        public int MaxRepsBeforeCancelOrder { get; set; }
        public int IntervalMinutes { get; set; }
        public int TotalConsecutiveLosses { get; set; }
        public int TotalCurrentPauses { get; set; }
        public int MaxConsecutiveLossesBeforePause { get; set; }
        public string Botname { get; set; }
        public bool IsProd { get; set; }
        public bool IsBuyingAllowed { get; set; }
        public bool IsSellingAllowed { get; set; }
        public bool ShowScalpBuyLogs { get; set; }
        public bool ShowNoScalpBuyLogs { get; set; }
        public bool ShowBuyLogs { get; set; }
        public bool ShowNoBuyLogs { get; set; }
        public bool ShowSellLogs { get; set; }
        public bool ShowNoSellLogs { get; set; }
        public bool ShowBuyingFlowLogs { get; set; }
        public bool ShowSellingFlowLogs { get; set; }
        public decimal MinimumAmountToTradeWith { get; set; }

        public decimal DayLowGreaterthanTobuy { get; set; }
        public decimal DayLowLessthanTobuy { get; set; }
        public decimal DayHighLessthanToSell { get; set; }
        public decimal DayHighGreaterthanToSell { get; set; }


        public decimal MaximumAmountForaBot { get; set; }
        public decimal DivideHighAndAverageBy { get; set; }
        public decimal BufferPriceForBuyAndSell { get; set; }
        public decimal CommisionAmount { get; set; }

        public bool IsReducingSellAbvAllowed { get; set; }
        public int ReduceSellAboveAtMinute { get; set; }
        public int ReduceSellAboveFromSecond { get; set; }
        public int ReduceSellAboveToSecond { get; set; }
        public decimal ReduceSellAboveBy { get; set; }
        public decimal MinSellAbovePerc { get; set; }
        public decimal ReducePriceDiffPercBy { get; set; }
        public decimal DefaultSellAbovePerc { get; set; }
        public decimal MinAllowedTradeCount { get; set; }

        public bool ShouldSellWhenAllBotsAtLoss { get; set; }
        public decimal SellWhenAllBotsAtLossBelow { get; set; }
        public bool CrashSell { get; set; }

        public decimal ScalpFourHourDiffLessThan { get; set; }
        public decimal ScalpOneHourDiffLessThan { get; set; }
        public decimal ScalpThirtyMinDiffLessThan { get; set; }
        public decimal ScalpFifteenMinDiffLessThan { get; set; }
        public decimal ScalpFiveMinDiffLessThan { get; set; }
        public int ScalpFourHourDownMoreThan { get; set; }
        public int ScalpOneHourDownMoreThan { get; set; }
        public int ScalpThirtyMinDownMoreThan { get; set; }
        public int ScalpFifteenMinDownMoreThan { get; set; }
        public int ScalpFiveMinDownMoreThan { get; set; }
        public bool UpdateCoins { get; set; }
    }

    public partial class ConfigConfiguration : IEntityTypeConfiguration<Config>
    {
        public void Configure(EntityTypeBuilder<Config> builder)
        {
            builder.Property(e => e.MinimumAmountToTradeWith).IsRequired().HasColumnType("decimal(18, 2)");
            builder.Property(e => e.MaximumAmountForaBot).IsRequired().HasColumnType("decimal(18, 2)");
            builder.Property(e => e.BufferPriceForBuyAndSell).IsRequired().HasColumnType("decimal(18, 12)");
            builder.Property(e => e.CommisionAmount).IsRequired().HasColumnType("decimal(18, 12)");
            builder.Property(e => e.DivideHighAndAverageBy).IsRequired().HasColumnType("decimal(18, 12)");
            builder.Property(e => e.DayLowGreaterthanTobuy).IsRequired().HasColumnType("decimal(18, 2)");
            builder.Property(e => e.DayLowLessthanTobuy).IsRequired().HasColumnType("decimal(18, 2)");
            builder.Property(e => e.DayHighLessthanToSell).IsRequired().HasColumnType("decimal(18, 12)");
            builder.Property(e => e.DayHighGreaterthanToSell).IsRequired().HasColumnType("decimal(18, 12)");
            builder.Property(e => e.ReduceSellAboveBy).IsRequired().HasColumnType("decimal(6, 4)");
            builder.Property(e => e.MinSellAbovePerc).IsRequired().HasColumnType("decimal(6, 4)");
            builder.Property(e => e.ReducePriceDiffPercBy).IsRequired().HasColumnType("decimal(6, 4)");
            builder.Property(e => e.DefaultSellAbovePerc).IsRequired().HasColumnType("decimal(6, 4)");
            builder.Property(e => e.MinAllowedTradeCount).IsRequired().HasColumnType("decimal(18, 12)");
            builder.Property(e => e.ScalpFourHourDiffLessThan).IsRequired().HasColumnType("decimal(6, 4)");
            builder.Property(e => e.ScalpOneHourDiffLessThan).IsRequired().HasColumnType("decimal(6, 4)");
            builder.Property(e => e.ScalpThirtyMinDiffLessThan).IsRequired().HasColumnType("decimal(6, 4)");
            builder.Property(e => e.ScalpFifteenMinDiffLessThan).IsRequired().HasColumnType("decimal(6, 4)");
            builder.Property(e => e.ScalpFiveMinDiffLessThan).IsRequired().HasColumnType("decimal(6, 4)");
            builder.Property(e => e.SellWhenAllBotsAtLossBelow).IsRequired().HasColumnType("decimal(4, 2)");
        }
    }

    public static partial class Seeder
    {
        public static void SeedConfig(this ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Config>().HasData(
            //    new Config { id = 1,
            //        MaxPauses = 1,
            //        IntervalMinutes = 5,
            //        TotalConsecutiveLosses = 0,
            //        TotalCurrentPauses=0,
            //         MaxConsecutiveLossesBeforePause = 3,
            //          Botname = "DIANA",
            //           IsProd = false,
            //           MinimumAmountToTradeWith = 70,
            //          BufferPriceForBuyAndSell = 0.075M,
            //          CommisionAmount =0.075M
            //    }
            //  );
        }
    }

}
