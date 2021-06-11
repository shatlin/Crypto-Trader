using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using BinanceExchange.API.Converter;
using BinanceExchange.API.Models.Response.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace BinanceExchange.API.Models.Response
{

    public class DailyCandle
    {
        [Column(Order = 1)]
        public int Id { get; set; }

        [Column(Order = 2)]
        public string Symbol { get; set; }


        [Column(Order = 4)]
        public DateTime OpenTime { get; set; }

        [Column(Order = 5)]
        public decimal Open { get; set; }

        [Column(Order = 6)]
        public decimal High { get; set; }

        [Column(Order = 7)]
        public decimal Low { get; set; }

        [Column(Order = 8)]
        public decimal Close { get; set; }

        [Column(Order = 9)]
        public decimal Volume { get; set; }

        [Column(Order = 10)]
        public DateTime CloseTime { get; set; }

        [Column(Order = 11)]
        public decimal QuoteAssetVolume { get; set; }

        [Column(Order = 12)]
        public int NumberOfTrades { get; set; }

        [Column(Order = 13)]
        public decimal TakerBuyBaseAssetVolume { get; set; }

        [Column(Order = 14)]
        public decimal TakerBuyQuoteAssetVolume { get; set; }

        [Column(Order = 15)]
        public decimal CurrentPrice { get; set; }

        [Column(Order = 16)]
        public decimal DayLowPrice { get; set; }

        [Column(Order = 17)]
        public decimal DayHighPrice { get; set; }

        [Column(Order = 18)]
        public decimal DayVolume { get; set; }

        [Column(Order = 19)]
        public int DayTradeCount { get; set; }

        [Column(Order = 20)]
        public decimal Change { get; set; }

        [Column(Order = 21)]
        public decimal PriceChangePercent { get; set; }

        [Column(Order = 22)]
        public decimal WeightedAveragePercent { get; set; }

        [Column(Order = 23)]
        public decimal PreviousClosePrice { get; set; }

        [Column(Order = 24)]
        public decimal OpenPrice { get; set; }

        [Column(Order = 25)]
        public int DataSet { get; set; }
    }

    public partial class DailyCandleConfiguration : IEntityTypeConfiguration<DailyCandle>
    {
        public void Configure(EntityTypeBuilder<DailyCandle> builder)
        {
            builder.Property(e => e.Open).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.High).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.Low).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.Close).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.Volume).IsRequired().HasColumnType("decimal(23, 4)");
            builder.Property(e => e.QuoteAssetVolume).IsRequired().HasColumnType("decimal(23, 4)");
            builder.Property(e => e.TakerBuyBaseAssetVolume).IsRequired().HasColumnType("decimal(23, 4)");
            builder.Property(e => e.TakerBuyQuoteAssetVolume).IsRequired().HasColumnType("decimal(23, 4)");
            builder.Property(e => e.CurrentPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.DayLowPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.DayHighPrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.DayVolume).IsRequired().HasColumnType("decimal(23, 4)");
            builder.Property(e => e.Change).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.PriceChangePercent).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.WeightedAveragePercent).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.PreviousClosePrice).IsRequired().HasColumnType("decimal(18, 9)");
            builder.Property(e => e.OpenPrice).IsRequired().HasColumnType("decimal(18, 9)");
           
        }

    }
}