using BinanceExchange.API.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
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
        public bool IsIncludedForTrading { get; set; }
        public bool ForceBuy { get; set; }
        public bool isLastTwoFiveMinsGoingDown { get; set; }
        public bool isLastThreeFiveMinsGoingDown { get; set; }
        public bool isLastThreeFiveMinsGoingUp { get; set; }

        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }


        public List<SignalCandle> Ref1MinCandles { get; set; } // will have 15 candles. Last 15 mins
        public int TotalConsecutive1MinUps { get; set; }
        public int TotalConsecutive1MinDowns { get; set; }
        public decimal PrChPercCurrAndRef1Min { get; set; }
        public decimal MinRef1Min { get; set; }
        public decimal MaxRef1Min { get; set; }

        public List<SignalCandle> Ref5MinCandles { get; set; }  // will have 6 candles. Last 30 mins
        public int TotalConsecutive5MinUps { get; set; }
        public int TotalConsecutive5MinDowns { get; set; }
        public decimal PrChPercCurrAndRef5Min { get; set; }
        public decimal MinRef5Min { get; set; }
        public decimal MaxRef5Min { get; set; }

        public List<SignalCandle> Ref15MinCandles { get; set; } // will have 6 candles. Last 1.5 hours
        public int TotalConsecutive15MinUps { get; set; }
        public int TotalConsecutive15MinDowns { get; set; }
        public decimal PrChPercCurrAndRef15Min { get; set; }
        public decimal MinRef15Min { get; set; }
        public decimal MaxRef15Min { get; set; }

        public List<SignalCandle> Ref30MinCandles { get; set; } // will have 6 candles. Last 3 hours
        public int TotalConsecutive30MinUps { get; set; }
        public int TotalConsecutive30MinDowns { get; set; }
        public decimal PrChPercCurrAndRef30Min { get; set; }
        public decimal MinRef30Min { get; set; }
        public decimal MaxRef30Min { get; set; }

        public List<SignalCandle> Ref1HourCandles { get; set; }  // will have 24 candles. Last 24 hours
        public int TotalConsecutive1HourUps { get; set; }
        public int TotalConsecutive1HourDowns { get; set; }
        public decimal PrChPercCurrAndRef1Hour { get; set; }
        public decimal MinRef1Hour { get; set; }
        public decimal MaxRef1Hour { get; set; }

        public List<SignalCandle> Ref4HourCandles { get; set; }  // will have 24 candles. Last 24 hours
        public int TotalConsecutive4HourUps { get; set; }
        public int TotalConsecutive4HourDowns { get; set; }
        public decimal PrChPercCurrAndRef4Hour { get; set; }
        public decimal MinRef4Hour { get; set; }
        public decimal MaxRef4Hour { get; set; }

        public List<SignalCandle> Ref1DayCandles { get; set; }   // will have 7 candles. Last week
        public int TotalConsecutive1DayUps { get; set; }
        public int TotalConsecutive1DayDowns { get; set; }
        public decimal PrChPercCurrAndRef1Day { get; set; }
        public decimal MinRef1Day { get; set; }
        public decimal MaxRef1Day { get; set; }

        public Guid TickerSocketGuid { get; set; }
        public Guid KlineSocketGuid { get; set; }

        public decimal LastTradePrice { get; set; }
    }

    public class SignalCandle
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }
        public string Pair { get; set; }
        public string CandleType { get; set; } //5Min,15Min,30Min,1Hr,Day
        public string UpOrDown { get; set; }
        public DateTime CloseTime { get; set; }
        public decimal ClosePrice { get; set; }
        public DateTime AddedTime { get; set; }
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
