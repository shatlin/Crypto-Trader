using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{
    public class SignalIndicator
    {
        public int Id { get; set; }
        public string Symbol { get; set; }

        public decimal CurrentPrice { get; set; }
        public decimal DayLowPrice { get; set; }
        public decimal DayHighPrice { get; set; }
        public decimal DayVolume { get; set; }
        public decimal DayTradeCount { get; set; }

        public decimal ReferenceSetAverageCurrentPrice { get; set; }
        public decimal ReferenceSetLowPrice { get; set; }
        public decimal ReferenceSetHighPrice { get; set; }
        public decimal ReferenceSetDayVolume { get; set; }
        public int ReferenceSetDayTradeCount { get; set; }

       
        public decimal DayPriceDifferencePercentage { get; set; }
        public decimal PriceDifferenceCurrentAndHighPercentage { get; set; }
        public decimal PriceDifferenceCurrentAndLowPercentage { get; set; }

        public bool IsOnUpTrend { get; set; }
        public bool IsBestTimeToBuy { get; set; }
        public bool IsBestTimeToSell { get; set; }
        public bool IsCloseToDayHigh { get; set; }
        public bool IsCloseToDayLow { get; set; }

        public bool IsPicked { get; set; }
        public bool IsIgnored { get; set; }

        public DateTime CandleOpenTime { get; set; }
    }

   


}
