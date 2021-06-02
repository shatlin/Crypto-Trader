using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using BinanceExchange.API.Converter;
using Newtonsoft.Json;

namespace BinanceExchange.API.Models.Response
{
   
    public class PriceChange
    {
        [Column("Id", Order = 0)]
        public int Id { get; set; }

        [Column("Pair", Order = 1)]
        public string Pair { get; set; }

        [Column("CurrentPrice", Order = 2)]
        public decimal CurrentPrice { get; set; }

        [Column("Date", Order = 3)]
        public DateTime Date { get; set; }

        [Column("LowPrice", Order = 4)]
        public decimal LowPrice { get; set; }

        [Column("HighPrice", Order = 5)]
        public decimal HighPrice { get; set; }

        [Column("Volume", Order = 6)]
        public decimal Volume { get; set; }

        [Column("TradeCount", Order = 7)]
        public int TradeCount { get; set; }

        [Column("Change", Order = 8)]
        public decimal Change { get; set; }
      
        public decimal PriceChangePercent { get; set; }
        public decimal WeightedAveragePercent { get; set; }

        public decimal PreviousClosePrice { get; set; }

        public decimal OpenPrice { get; set; }

       
    }
}
