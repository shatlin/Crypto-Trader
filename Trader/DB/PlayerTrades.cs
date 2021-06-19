using System;
using System.Runtime.Serialization;
using BinanceExchange.API.Converter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace BinanceExchange.API.Models.Response
{
    public class PlayerTrades
    {

        public int Id { get; set; }

        public string Name { get; set; }

        public string Avatar { get; set; }

        public string Pair { get; set; }

        public string BuyOrSell { get; set; }

        public decimal? DayHigh { get; set; }

        public decimal? DayLow { get; set; }

        public decimal? BuyBelowPerc { get; set; }

        public decimal? DontSellBelowPerc { get; set; }

        public decimal? SellBelowPerc { get; set; }

        public decimal? SellAbovePerc { get; set; }

        public decimal? BuyPricePerCoin { get; set; }

        public decimal CurrentPricePerCoin { get; set; }

        public decimal? QuantityBought { get; set; }

        public decimal? BuyingCommision { get; set; }

        public decimal SoldPricePricePerCoin { get; set; }
        public decimal QuantitySold { get; set; }
        public decimal? SoldCommision { get; set; }
        public decimal? TotalSoldAmount { get; set; }

        public decimal? TotalBuyCost { get; set; }

        public decimal? TotalCurrentValue { get; set; }

        public decimal? TotalCurrentProfit { get; set; }

        public decimal? AvailableAmountForTrading { get; set; }

        public decimal? OriginalAllocatedValue { get; set; }

        public bool IsTrading { get; set; }

        public DateTime? BuyTime { get; set; }

        public DateTime? SellTime { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedTime { get; set; }

        public decimal? TotalExpectedProfit { get; set; }

        public DateTime? CandleOpenTimeAtBuy { get; set; }

        public DateTime? CandleOpenTimeAtSell { get; set; }

        public int BuyCandleId { get; set; }

        public int SellCandleId { get; set; }

        public string LossOrProfit { get; set; }

        public decimal SaleProfitOrLoss { get; set; }
    }

    public partial class PlayerHistConfiguration : IEntityTypeConfiguration<PlayerTrades>
    {
        public void Configure(EntityTypeBuilder<PlayerTrades> builder)
        {
            builder.Property(e => e.BuyOrSell).IsRequired(false);
            builder.Property(e => e.DayHigh).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DayLow).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.BuyBelowPerc).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.SellBelowPerc).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.DontSellBelowPerc).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.BuyPricePerCoin).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.CurrentPricePerCoin).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.QuantityBought).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalBuyCost).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalCurrentValue).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalCurrentProfit).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.OriginalAllocatedValue).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.AvailableAmountForTrading).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.BuyingCommision).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.SoldPricePricePerCoin).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.QuantitySold).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.SoldCommision).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalSoldAmount).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.TotalExpectedProfit).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.SellAbovePerc).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.SaleProfitOrLoss).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.LossOrProfit).IsRequired(false);

        }

    }

}
