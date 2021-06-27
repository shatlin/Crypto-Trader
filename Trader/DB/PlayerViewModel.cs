using System;
using System.Runtime.Serialization;
using BinanceExchange.API.Converter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace BinanceExchange.API.Models.Response
{
    public class PlayerViewModel
    {
       
        public string Name { get; set; }

        public string Pair { get; set; }

        public decimal? DayHigh { get; set; }

        public decimal? DayLow { get; set; }

        public decimal? SellBelowPerc { get; set; }

        public decimal? SellAbovePerc { get; set; }

        public string IsTrading { get; set; }

        public string BuyTime { get; set; }

        public decimal? QuantityBought { get; set; }

        public decimal? BuyPricePerCoin { get; set; }

        public decimal CurrentPricePerCoin { get; set; }

        public decimal? TotalBuyCost { get; set; }

        public decimal? TotalSoldAmount { get; set; }
        
        public decimal? TotalCurrentValue { get; set; }

        public decimal? LastRoundProfitPerc { get; set; }

        public decimal? CurrentRoundProfitPerc { get; set; }

        public string ProfitLossChanges { get; set; }
    }

  

}
