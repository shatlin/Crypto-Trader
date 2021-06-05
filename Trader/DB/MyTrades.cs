using System;
using System.Runtime.Serialization;
using BinanceExchange.API.Converter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace BinanceExchange.API.Models.Response
{

    public class MyTrade
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }

         

        [DataMember(Order = 1)]
        public string Pair { get; set; }

        [DataMember(Order = 2)]
        public decimal Price { get; set; }

        [DataMember(Order = 3)]
        [JsonProperty(PropertyName = "qty")]
        public decimal Quantity { get; set; }

        [DataMember(Order = 4)]
        public decimal Commission { get; set; }

        [DataMember(Order = 5)]
        public string CommissionAsset { get; set; }

        [DataMember(Order = 5)]
        public DateTime Time { get; set; }

        [DataMember(Order = 6)]
        public bool IsBuyer { get; set; }

        [DataMember(Order = 7)]
        public bool IsMaker { get; set; }

        [DataMember(Order = 8)]
        public bool IsBestMatch { get; set; }

        [DataMember(Order = 9)]
        public long OrderId { get; set; }

        [DataMember(Order = 10)]
        public decimal Amount { get; set; }
    }

    public partial class MyTradeConfiguration : IEntityTypeConfiguration<MyTrade>
    {
        public void Configure(EntityTypeBuilder<MyTrade> builder)
        {
            builder.Property(e => e.Price).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.Quantity).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.Commission).IsRequired().HasColumnType("decimal(30, 12)");
            builder.Property(e => e.Amount).IsRequired().HasColumnType("decimal(30, 12)");
        }

    }

}
