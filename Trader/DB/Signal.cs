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
        public int CoinId { get; set; }
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

        public decimal PercBelowDayHighToBuy { get; set; }
        public decimal PercAboveDayLowToSell { get; set; }

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

        public bool isLastTwoFiveMinsGoingDown { get; set; }
        public bool isLastThreeFiveMinsGoingDown { get; set; }
        public bool isLastThreeFiveMinsGoingUp { get; set; }

        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }

        public List<SignalCandle> RefDayCandles { get; set; }   // will have 7 candles. Last week
        public List<SignalCandle> RefHourCandles { get; set; }  // will have 24 candles. Last 24 hours
        public List<SignalCandle> Ref5MinCandles { get; set; }  // will have 24 candles. Last 2 hours
        public List<SignalCandle> Ref15MinCandles { get; set; } // will have 24 candles. Last 6 hours
        public List<SignalCandle> Ref30MinCandles { get; set; } // will have 24 candles. Last 12 hours

        public int TotalConsecutiveDayUps { get; set; }
        public int TotalConsecutiveDayDowns { get; set; }

        public int TotalConsecutiveHourUps { get; set; }
        public int TotalConsecutiveHourDowns { get; set; }

        public int TotalConsecutive30MinUps { get; set; }
        public int TotalConsecutive30MinDowns { get; set; }

        public int TotalConsecutive15MinUps { get; set; }
        public int TotalConsecutive15MinDowns { get; set; }

        public int TotalConsecutive5MinUps { get; set; }
        public int TotalConsecutive5MinDowns { get; set; }
     
       
        public Guid TickerSocketGuid { get; set; }
        public Guid KlineSocketGuid { get; set; }

        public decimal PrChPercCurrAndRef5min { get; set; }
        public decimal PrChPercCurrAndRef15min { get; set; }
        public decimal PrChPercCurrAndRef30min { get; set; }
        public decimal PrChPercCurrAndRefHr { get; set; }
        public decimal PrChPercCurrAndRefDay { get; set; }

        public decimal LastTradePrice { get; set; }
    }

    public class SignalCandle
    {
        public int Id { get; set; }
        public string Pair { get; set; }
        public string CandleType { get; set; } //5Min,15Min,30Min,1Hr,Day
        public DateTime CloseTime { get; set; }
        public decimal ClosePrice { get; set; }
    }

    public class GlobalSignal
    {
        public bool IsMarketOnDownTrendToday { get; set; }
        public bool IsMarketOnUpTrendToday { get; set; }
        public bool IsBitCoinGoingUpToday { get; set; }
        public bool IsBitCoinGoingDownToday { get; set; }

        public bool IsMarketOnDownTrendTthisWeek { get; set; }
        public bool IsMarketOnUpTrendThisWeek { get; set; }
        public bool IsBitCoinGoingUpThisWeek { get; set; }
        public bool IsBitCoinGoingDownThisWeek { get; set; }

        public bool AreMostCoinsGoingDownNow { get; set; }
        public bool AreMostCoinsGoingUpNow { get; set; }
    }

    public partial class SignalCandleConfiguration : IEntityTypeConfiguration<SignalCandle>
    {
        public void Configure(EntityTypeBuilder<SignalCandle> builder)
        {
            builder.Property(e => e.ClosePrice).IsRequired().HasColumnType("decimal(30, 12)");
        }
    }
}
