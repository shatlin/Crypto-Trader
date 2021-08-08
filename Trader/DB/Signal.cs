using BinanceExchange.API.Models.Response;
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
        public long CurrentTradeCount { get; set; }
        public decimal CurrPr { get; set; }
        public decimal DayLowPr { get; set; }
        public decimal DayHighPr { get; set; }
        public decimal DayAveragePr { get; set; }
        public decimal DayVol { get; set; }
        public decimal DayTradeCount { get; set; }
        
        public decimal RefAvgCurrPr { get; set; }
        public decimal RefLowPr { get; set; }
        public decimal RefHighPr { get; set; }
        public decimal RefVol { get; set; }
        public int RefTradeCount { get; set; }

        public decimal PrDiffHighAndLowPerc { get; set; }
        public decimal PrDiffCurrAndHighPerc { get; set; }
        
        public decimal PrDiffCurrAndLowPerc { get; set; }
        public decimal CurrPrDiffSigAndRef { get; set; }

        public bool IsOnUpTrend { get; set; }
        public bool IsBestTimeToScalpBuy { get; set; }
        public bool IsBestTimeToScalpBuy2 { get; set; }
        public bool IsBestTimeToBuyAtDayLowest { get; set; }
        public bool IsBestTimeToSellAtDayHighest { get; set; }
        public bool IsBestTimeToScalpSell { get; set; }
        public bool IsSymbolTickerSocketRunning { get; set; }
        public bool IsDailyKlineSocketRunning { get; set; }
        public bool IsAtDayHigh { get; set; }
        public bool IsAtDayLow { get; set; }
        public bool IsCloseToDayLow { get; set; }
        public bool JustRecoveredFromDayLow { get; set; }
        public bool IsPicked { get; set; }
        public bool IsIgnored { get; set; }
       
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }

        public List<SignalCandle> RefDayCandles { get; set; }
        public List<SignalCandle> RefHourCandles { get; set; }
        public List<SignalCandle> RefFiveMinCandles { get; set; }

        public int TotalPreviousUps { get; set; }
        public int TotalPreviousDowns { get; set; }

        public Guid TickerSocketGuid { get; set; }
        public Guid KlineSocketGuid { get; set; }
        public bool isLastTwoFiveMinsGoingDown { get; set; }
        public bool isLastThreeFiveMinsGoingDown { get; set; }
        public bool isLastThreeFiveMinsGoingUp { get; set; }

        public decimal PriceChangeInLastHour { get; set; }
        public decimal LastTradePrice { get; set; }
    }

    public class SignalCandle
    {
        public int Id { get; set; }
        public int SeqNo { get; set; }
        public string Pair { get; set; }
        public DateTime CloseTime { get; set; }
        public decimal ClosePrice { get; set; }
    }

    public partial class SignalCandleConfiguration : IEntityTypeConfiguration<SignalCandle>
    {
        public void Configure(EntityTypeBuilder<SignalCandle> builder)
        {
      
            builder.Property(e => e.ClosePrice).IsRequired().HasColumnType("decimal(30, 12)");
            
        }

    }

}
