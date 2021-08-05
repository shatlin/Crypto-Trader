using System;
using System.Runtime.Serialization;
using BinanceExchange.API.Converter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace BinanceExchange.API.Models.Response
{
    public class Player
    {


        public int Id { get; set; }
        public string Name { get; set; }
        public string Pair { get; set; }
        public bool IsTrading { get; set; }
        public bool isBuyAllowed { get; set; }
        public bool isSellAllowed { get; set; }
        public bool isBuyCostAccurated { get; set; }
        public bool isSellAmountAccurated { get; set; }

        public decimal? DayHigh { get; set; }
        public decimal? DayLow { get; set; }

        public decimal? BuyBelowPerc { get; set; }
        public decimal? SellBelowPerc { get; set; }
        public decimal? SellAbovePerc { get; set; }
        public decimal? DontSellBelowPerc { get; set; }
        public decimal? HardSellPerc { get; set; }
        public bool isBuyOrderCompleted { get; set; }
        public decimal? BuyCoinPrice { get; set; }

        public decimal CurrentCoinPrice { get; set; }

        public decimal SellCoinPrice { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? BuyCommision { get; set; }
        public decimal? SellCommision { get; set; }

        public decimal? LastRoundProfitPerc { get; set; }

        public decimal? TotalBuyCost { get; set; }
        public decimal? TotalCurrentValue { get; set; }
        public decimal? TotalSellAmount { get; set; }
        public decimal? AvailableAmountToBuy { get; set; }

        public DateTime? BuyTime { get; set; }
        public DateTime? SellTime { get; set; }
        public DateTime? WaitTillCancellingOrderTime { get; set; }

        public long BuyOrderId { get; set; }
        public long SellOrderId { get; set; }

        public DateTime? UpdatedTime { get; set; }

        public string ProfitLossChanges { get; set; }
        public string BuyOrSell { get; set; }
        public decimal ProfitLossAmt { get; set; }


    }

    public partial class PlayerConfiguration : IEntityTypeConfiguration<Player>
    {
        public void Configure(EntityTypeBuilder<Player> builder)
        {
            builder.Property(e => e.BuyOrSell).IsRequired(false);
            builder.Property(e => e.ProfitLossChanges).IsRequired(false);
            builder.Property(e => e.DayHigh).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DayLow).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.BuyBelowPerc).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.SellBelowPerc).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DontSellBelowPerc).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.BuyCoinPrice).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.CurrentCoinPrice).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.Quantity).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalBuyCost).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalCurrentValue).IsRequired().HasColumnType("decimal(30, 12)");

            builder.Property(e => e.AvailableAmountToBuy).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.BuyCommision).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.SellCoinPrice).IsRequired().HasColumnType("decimal(30, 12)");

            builder.Property(e => e.SellCommision).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalSellAmount).IsRequired().HasColumnType("decimal(30, 12)");

            builder.Property(e => e.SellAbovePerc).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.HardSellPerc).IsRequired().HasColumnType("decimal(30, 12)");

            builder.Property(e => e.ProfitLossAmt).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.LastRoundProfitPerc).IsRequired().HasColumnType("decimal(30, 12)");
        }

    }

}

