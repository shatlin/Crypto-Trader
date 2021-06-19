using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{
    public class Signal
    {
        public int Id { get; set; }
        public string Symbol { get; set; }

        public decimal CurrPr { get; set; }
        public decimal DayLowPr { get; set; }
        public decimal DayHighPr { get; set; }
        public decimal DayVol { get; set; }
        public decimal DayTradeCount { get; set; }

        public decimal RefAvgCurrPr { get; set; }
        public decimal RefLowPr { get; set; }
        public decimal RefHighPr { get; set; }
        public decimal RefDayVol { get; set; }
        public int RefDayTradeCount { get; set; }

       
        public decimal DayPrDiffPercentage { get; set; }
        public decimal PrDiffCurrAndHighPerc { get; set; }
        public decimal PrDiffCurrAndLowPerc { get; set; }
        public decimal CurrPrDiffSigAndRef { get; set; }
        public bool IsOnUpTrend { get; set; }
        public bool IsBestTimeToBuy { get; set; }
        public bool IsBestTimeToSell { get; set; }
        public bool IsCloseToDayHigh { get; set; }
        public bool IsCloseToDayLow { get; set; }
        public bool IsPicked { get; set; }
        public bool IsIgnored { get; set; }
        public int CandleId { get; set; }
        public DateTime CandleOpenTime { get; set; }
        public DateTime CandleCloseTime { get; set; }
    }

   


}
