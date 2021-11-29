
using AutoMapper;
using BinanceExchange.API.Client;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Models.Response.Abstract;
using BinanceExchange.API.Websockets;
using log4net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Trader.Models;

namespace Trader.MVVM.View
{
    public static class Extensions
    {
        public static decimal Rnd(this decimal value, int places = 5)
        {
            return Math.Round(value, places);
        }

        public static decimal Deci(this decimal? value)
        {
            return Convert.ToDecimal(value);
        }

        public static int GetAllowedPrecision(this decimal value)
        {
            if (value >= 1)
            {
                return 0;
            }
            int totalPrecision = 0;
            int j = 0;

            string tempValue = value.ToString();
            char[] tempCharArray = tempValue.ToCharArray();

            while (tempCharArray[j] != '.')
            {
                j++;

                if (j > (tempValue.Length - 1))
                    break;
            }

            for (int i = j + 1; i < tempValue.Length; i++)
            {
                if (tempCharArray[i] == '1')
                {
                    totalPrecision++;
                    break;
                }

                totalPrecision++;

            }
            return totalPrecision;
        }

        public static decimal GetDiffPercBetnNewAndOld(this decimal newValue, decimal OldValue)
        {
            if (OldValue == 0) return 0M;
            return ((newValue - OldValue) / (OldValue)) * 100;
        }

        public static decimal GetDiffPercBetnNewAndOld(this int newValue, int OldValue)
        {
            if (OldValue == 0) return 0M;
            return ((newValue - OldValue) / (OldValue)) * 100;
        }

        public static decimal? GetDiffPercBetnNewAndOld(this decimal? newValue, decimal? OldValue)
        {
            if (OldValue == 0) return 0M;
            return ((newValue - OldValue) / (OldValue)) * 100M;
        }

        public static string GetURL(this string pair)
        {
            return "https://www.binance.com/en/trade/" + pair.Replace("USDT", "_USDT") + "?layout=pro&type=spot";
        }

        public static string GetLast(this string source, int tail_length)
        {
            if (source == null) return null;

            if (tail_length >= source.Length)
                return source;
            return source.Substring(source.Length - tail_length, tail_length);
        }
    }

    public class CoinData
    {
        public string pair { get; set; } //s
        public string coinSymbol { get; set; } //b
        public decimal precision { get; set; } //i
        public string coinName { get; set; } //an
        public decimal openprice { get; set; } //o
        public decimal dayhigh { get; set; } //h
        public decimal daylow { get; set; } //l
        public decimal currentprice { get; set; } //c
        public decimal volume { get; set; } //v
        public decimal USDTVolume { get; set; } //qv
        public decimal totalCoinsInStorage { get; set; } //cs 
        public decimal MarketCap { get; set; }
    }

    public static class CandleLimit
    {
        public const int OneMinLimit = 30;
        public const int FiveMinLimit = 288;
        public const int FifteenMinLimit = 16;
        public const int ThirtyMinLimit = 24;
        public const int OneHourLimit = 24;
        public const int FourHourLimit = 12;
        public const int OneDayLimit = 30;
    }

    public partial class HomeView : UserControl
    {
        public List<MyCoins> myCoins { get; set; }
        public DispatcherTimer TradeTimer;
        public DispatcherTimer CollectTimer;
        public DispatcherTimer CheckSocketsTimer;
        public DateTime TradeTime { get; set; }
        public string StrTradeTime { get; set; }
        public string NextTradeTime { get; set; }
        BinanceClient client;
        public List<PlayerViewModel> PlayerViewModels;
        public int UpdatePrecisionCounter = 0;
        ILog logger;
        IMapper iPlayerMapper;
        IMapper iPlayerQAMapper;
        List<string> boughtCoins = new List<string>();
        List<Signal> MySignals = new List<Signal>();
        public Config configr = new Config();
        InstanceBinanceWebSocketClient socket;
        public ExchangeInfoResponse exchangeInfo = new ExchangeInfoResponse();
        public bool isControlCurrentlyInTradeMethod = false;
        public bool isRunning = false;

        public HomeView()
        {
            InitializeComponent();
            Startup();
        }

        private async void Startup()
        {
            if (isRunning == false)
            {
                isRunning = true;

                logger = LogManager.GetLogger(typeof(MainWindow));

                logger.Info("App Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
                logger.Info("");

                DB db = new DB();
                configr = await db.Config.FirstOrDefaultAsync();
                lblBotName.Text = configr.Botname;

                TradeTimer = new DispatcherTimer();
                TradeTimer.Tick += new EventHandler(TraderTimer_Tick);
                TradeTimer.Interval = new TimeSpan(0, 0, configr.IntervalMinutes);

                //CollectTimer = new DispatcherTimer();
                //CollectTimer.Tick += new EventHandler(CollectTimer_Tick);
                //CollectTimer.Interval = new TimeSpan(0, 2, 0);

                //CheckSocketsTimer = new DispatcherTimer();
                //CheckSocketsTimer.Tick += new EventHandler(CheckSocketsTimer_Tick);
                //CheckSocketsTimer.Interval = new TimeSpan(0, 30, 0);

               
                var api = await db.API.FirstOrDefaultAsync();

                client = new BinanceClient(new ClientConfiguration()
                {
                    ApiKey = api.key,
                    SecretKey = api.secret,
                    Logger = logger,
                });


                var playerMapConfig = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<Player, PlayerTrades>();
                });

                var playerQAMapConfig = new MapperConfiguration(cfg =>
                {
                    cfg.CreateMap<PlayerQA, PlayerTradesQA>();
                });

                iPlayerMapper = playerMapConfig.CreateMapper();
                iPlayerQAMapper = playerQAMapConfig.CreateMapper();

              //  socket = new InstanceBinanceWebSocketClient(client);

                MySignals = new List<Signal>();

              //  await GetMyCoins();

                await RedistributeBalances();

                //logger.Info("Getting signal streams Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
                //logger.Info("");
                //CreateSignals();

                TradeTimer.Start();

                //CheckSocketsTimer.Start();

                //logger.Info("Getting signal streams completed  and Timers Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
                //logger.Info("");
            }
        }

        //private void EnsureAllSocketsRunning()
        //{
        //    Parallel.ForEach(MySignals.Where(x => x.IsSymbolTickerSocketRunning == false
        //    || x.IsDailyKlineSocketRunning == false), sig =>
        //    {
        //        if (sig.IsSymbolTickerSocketRunning == false)
        //        {
        //            try
        //            {
        //                try
        //                {
        //                    if (sig.TickerSocketGuid != Guid.Empty) socket.CloseWebSocketInstance(sig.TickerSocketGuid);
        //                }
        //                catch (Exception ex1)
        //                {
        //                    logger.Info("exception at Ticker socket for " + sig.Symbol + "  " + ex1.Message);
        //                }

        //                sig.TickerSocketGuid = socket.ConnectToIndividualSymbolTickerWebSocket(sig.Symbol, b =>
        //                {
        //                    sig.CurrPr = b.LastPrice; sig.IsSymbolTickerSocketRunning = true;
        //                });

        //            }
        //            catch (Exception ex)
        //            {
        //                sig.IsSymbolTickerSocketRunning = false;
        //                logger.Info("exception at Ticker socket for " + sig.Symbol + "  " + ex.Message);
        //            }

        //        }

        //        if (sig.IsDailyKlineSocketRunning == false)
        //        {

        //            try
        //            {
        //                try
        //                {
        //                    if (sig.KlineSocketGuid != Guid.Empty) socket.CloseWebSocketInstance(sig.KlineSocketGuid);
        //                }
        //                catch (Exception ex1)
        //                {
        //                    logger.Info("exception at Kline socket for " + sig.Symbol + "  " + ex1.Message);
        //                }

        //                sig.KlineSocketGuid = socket.ConnectToKlineWebSocket(sig.Symbol, KlineInterval.OneDay, b =>
        //                {
        //                    sig.OpenTime = b.Kline.StartTime;
        //                    sig.CloseTime = b.Kline.EndTime;
        //                    sig.Symbol = b.Symbol;
        //                    sig.DayVol = b.Kline.Volume;
        //                    sig.DayTradeCount = b.Kline.NumberOfTrades;
        //                    sig.IsDailyKlineSocketRunning = true;
        //                });


        //            }
        //            catch (Exception ex)
        //            {
        //                sig.IsDailyKlineSocketRunning = false;
        //                logger.Info("exception at Kline socket for " + sig.Symbol + "  " + ex.Message);
        //            }

        //        }
        //    });

        //}

        private void CreateSignals()
        {
            using (var db = new DB())
            {
                foreach (var coin in myCoins)
                {

                    if (!MySignals.Any(x => x.Symbol == coin.Pair))
                    {
                        Signal sig = new Signal();
                        //sig.IsSymbolTickerSocketRunning = false;
                        //sig.IsDailyKlineSocketRunning = false;
                        sig.Symbol = coin.Pair;
                        sig.Ref1MinCandles = db.SignalCandle.AsNoTracking().Where(x => x.Pair == sig.Symbol && x.CandleType == "1min").ToList();
                        sig.Ref5MinCandles = db.SignalCandle.AsNoTracking().Where(x => x.Pair == sig.Symbol && x.CandleType == "5min").ToList();
                        sig.Ref15MinCandles = db.SignalCandle.AsNoTracking().Where(x => x.Pair == sig.Symbol && x.CandleType == "15min").ToList();
                        sig.Ref30MinCandles = db.SignalCandle.AsNoTracking().Where(x => x.Pair == sig.Symbol && x.CandleType == "30min").ToList();
                        sig.Ref1HourCandles = db.SignalCandle.AsNoTracking().Where(x => x.Pair == sig.Symbol && x.CandleType == "1hour").ToList();
                        sig.Ref4HourCandles = db.SignalCandle.AsNoTracking().Where(x => x.Pair == sig.Symbol && x.CandleType == "4hour").ToList();
                        sig.Ref1DayCandles = db.SignalCandle.AsNoTracking().Where(x => x.Pair == sig.Symbol && x.CandleType == "day").ToList();
                        MySignals.Add(sig);
                    }
                }
            }

            

          
        }

        private void ResetSignalsWithSelectedValues()
        {
            foreach (var coin in myCoins)
            {
                Signal sig = MySignals.Where(x => x.Symbol == coin.Pair).FirstOrDefault();
                if (sig != null)
                {
                    sig.IsIgnored = false;
                    sig.IsPicked = false;
                    sig.CoinId = coin.Id;
                    sig.PercBelowDayHighToBuy = coin.PercBelowDayHighToBuy;
                    sig.PercAboveDayLowToSell = coin.PercAboveDayLowToSell;
                    sig.IsBestTimeToBuyAtDayLowest = false;
                    sig.IsBestTimeToScalpBuy = false;
                }
            }
        }

        private int GetConsecutiveUpDowns(List<SignalCandle> candleList, string direction)
        {
            if (candleList == null || candleList.Count == 0) return 0;

            int TotalConsecutiveChanges = 0;

            candleList = candleList.OrderByDescending(x => x.CloseTime).ToList();

            bool directionCondition = false;

            for (int i = 0; i < candleList.Count - 1; i++)
            {
                directionCondition = direction == "up" ?
                    candleList[i].ClosePrice >= candleList[i + 1].ClosePrice :
                    candleList[i].ClosePrice < candleList[i + 1].ClosePrice;

                if (directionCondition)
                    TotalConsecutiveChanges++;
                else
                    break;
            }

            return TotalConsecutiveChanges;
        }

        private List<SignalCandle> FillSignalCandles(Signal sig, List<SignalCandle> candleList, string candleType, int count, int hour, int minute)
        {

            List<SignalCandle> signalCandles = new List<SignalCandle>();


            using (var db = new DB())
            {
                signalCandles = db.SignalCandle.AsNoTracking().Where(x => x.Pair == sig.Symbol && x.CandleType == candleType).ToList();



                if (candleList.Count < count)
                {
                    foreach (var refcndl in signalCandles)
                    {
                        if (!candleList.Any(x => x.CloseTime == refcndl.CloseTime))
                        {
                            candleList.Add(refcndl);
                        }
                    }
                }



                var time = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minute, 0);

                if (signalCandles == null || signalCandles.Count == 0)
                {
                    candleList.Add(new SignalCandle { AddedTime = DateTime.Now, CandleType = candleType, Pair = sig.Symbol, ClosePrice = sig.CurrPr, CloseTime = time, UpOrDown = "down" }
                    );
                }

                candleList = candleList.OrderByDescending(x => x.CloseTime).ToList();

                if (candleList.Count > count)
                {
                    candleList.RemoveRange(count, candleList.Count - count);
                }

                if (time > DateTime.Now) return candleList;

                var candleQuery = "delete from SignalCandle where CandleType='" + candleType + "' and Pair='" + sig.Symbol + "'";

                db.Database.ExecuteSqlRaw(candleQuery);

                candleList = candleList.OrderBy(x => x.CloseTime).ToList();

                if (candleList.Any(x => x.CloseTime == time))
                {
                    List<SignalCandle> canlis = candleList.Where(x => x.CloseTime == time).ToList();

                    foreach (var candle in canlis)
                    {
                        candleList.Remove(candle);
                    }
                }

                candleList.Add(new SignalCandle { Pair = sig.Symbol, CloseTime = time, ClosePrice = sig.CurrPr, CandleType = candleType, AddedTime = DateTime.Now });

                decimal PreviousClosePrice = 0M;

                foreach (SignalCandle cndl in candleList)
                {
                    cndl.Id = Guid.Empty;

                    if (cndl.ClosePrice >= PreviousClosePrice)
                        cndl.UpOrDown = "up";
                    else
                        cndl.UpOrDown = "down";

                    db.SignalCandle.Add(cndl);

                    PreviousClosePrice = cndl.ClosePrice;
                }

                db.SaveChanges();
            }

            return candleList;
        }

        private decimal GetPrChgBetnCrAndRefStart(decimal currentPrice, List<SignalCandle> candleList)
        {
            if (candleList == null || candleList.Count == 0) return 0M;
            var firstpriceOfCandles = candleList.First().ClosePrice;
            return currentPrice.GetDiffPercBetnNewAndOld(firstpriceOfCandles);
        }


        private void CollectReferenceCandlesNew()
        {

            bool fiveminsdeleted = false;

            for (int i = 0; i < MySignals.Count; i++)
            {
                try
                {
                    #region day

                    if (DateTime.Now.Hour % 23 == 0 && DateTime.Now.Minute % 57 == 0)
                    {

                        #region delete All 5 min candles to start from Scratch

                        if (fiveminsdeleted == false)
                        {
                            using (var db = new DB())
                            {
                                var candleQuery = "delete from SignalCandle where CandleType='5min'";
                                db.Database.ExecuteSqlRaw(candleQuery);

                                fiveminsdeleted = true;
                            }
                        }

                        #endregion

                        MySignals[i].Ref5MinCandles = new List<SignalCandle>(); // Reset 5 Mins Candle from Memory
                        MySignals[i].Ref1DayCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1DayCandles, "day", CandleLimit.OneDayLimit, 23, 57);
                    
                    }

                  

                    #endregion day

                    #region 4 hour

                    if ((DateTime.Now.Hour == 3 || DateTime.Now.Hour == 7 || DateTime.Now.Hour == 11 || DateTime.Now.Hour == 15 ||
                        DateTime.Now.Hour == 19 || DateTime.Now.Hour == 23)
                        && DateTime.Now.Minute % 57 == 0)
                    {
                        MySignals[i].Ref4HourCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref4HourCandles, "4hour", CandleLimit.FourHourLimit, DateTime.Now.Hour, 57);
                       
                    }
             
                  
                    #endregion 4hour

                    #region 1 hour

                    if (DateTime.Now.Minute % 57 == 0) //Collected for last 24 hours
                    {
                        MySignals[i].Ref1HourCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1HourCandles, "1hour", CandleLimit.OneHourLimit, DateTime.Now.Hour, 57); // 24 hours
                      
                    }


                    #endregion 1hour

                    #region 30 minute

                    if (DateTime.Now.Minute % 30 == 0) //Collected for last 6 hours
                    {
                        MySignals[i].Ref30MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref30MinCandles, "30min", CandleLimit.ThirtyMinLimit, DateTime.Now.Hour, DateTime.Now.Minute);
                       


                    }

                    #endregion 30minute

                    #region 15 minute

                    if (DateTime.Now.Minute % 15 == 0) // collected for last 3 hours
                    {
                        MySignals[i].Ref15MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref15MinCandles, "15min", CandleLimit.FifteenMinLimit, DateTime.Now.Hour, DateTime.Now.Minute);
                       

                    }

                    #endregion 15minute

                    #region 5 minute

                    if (DateTime.Now.Minute % 5 == 0) // collected for last day
                    {
                        MySignals[i].Ref5MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref5MinCandles, "5min", CandleLimit.FiveMinLimit, DateTime.Now.Hour, DateTime.Now.Minute);
                      


                    }

                    #endregion 5minute

                    #region 1 minute

                    if (DateTime.Now.Minute % 1 == 0) // collected for last hour
                    {
                        MySignals[i].Ref1MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1MinCandles, "1min", CandleLimit.OneMinLimit, DateTime.Now.Hour, DateTime.Now.Minute);
                       

                    }
                   

                    #endregion 1minute


                }
                catch (Exception ex)
                {
                    logger.Info("Exception at CollectReferenceCandles " + MySignals[i].Symbol + " " + ex.Message);
                }
            }

            using (var db = new DB())
            {
                foreach (var coin in myCoins)
                {
                    try
                    {
                        var sig = MySignals.Where(x => x.Symbol == coin.Pair).FirstOrDefault();
                        var AllOneMin = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime);
                        var AllFiveMin = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime);
                        var FifteenMin = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(3);
                        var ThirtyMin = sig.Ref30MinCandles.OrderByDescending(x => x.CloseTime).Take(5);
                        var OneHour = sig.Ref1HourCandles.OrderByDescending(x => x.CloseTime);
                        var FourHour = sig.Ref4HourCandles.OrderByDescending(x => x.CloseTime);

                        var Fifteen_OneMin = AllOneMin.Take(15);
                        var Ten_OneMin = AllOneMin.Take(10);
                        var Five_OneMin = AllOneMin.Take(5);

                        var isOneOnUpTrend = false;

                        if (AllOneMin.Any())
                        {
                            isOneOnUpTrend = Ten_OneMin.First().ClosePrice >= Ten_OneMin.Max(x => x.ClosePrice);

                            coin.FiveMinChange = ((coin.CurrentPrice - Five_OneMin.Last().ClosePrice) / Five_OneMin.Last().ClosePrice) * 100;
                            coin.TenMinChange = ((coin.CurrentPrice - Ten_OneMin.Last().ClosePrice) / Ten_OneMin.Last().ClosePrice) * 100;
                            coin.FifteenMinChange = ((coin.CurrentPrice - Fifteen_OneMin.Last().ClosePrice) / Fifteen_OneMin.Last().ClosePrice) * 100;
                            coin.ThirtyMinChange = ((coin.CurrentPrice - AllOneMin.Last().ClosePrice) / AllOneMin.Last().ClosePrice) * 100;
                        }
                        else
                        {
                            isOneOnUpTrend = false;
                            coin.ThirtyMinChange = 0;
                            coin.FiveMinChange = 0;
                            coin.TenMinChange = 0;
                            coin.FifteenMinChange = 0;
                        }


                        var Five_FiveMin = AllFiveMin.Take(5).ToList();
                        var Nine_FiveMin = AllFiveMin.Take(9).ToList();
                        var Twelve_FiveMin = AllFiveMin.Take(12).ToList();

                        var isFiveOnUpTrend = false;
                        //   coin.CurrentPrice = sig.CurrPr;

                        if (AllFiveMin.Any())
                        {
                            isFiveOnUpTrend = coin.CurrentPrice >= Five_FiveMin.Max(x => x.ClosePrice);
                            coin.FourtyFiveMinChange = ((coin.CurrentPrice - Nine_FiveMin.Last().ClosePrice) / Nine_FiveMin.Last().ClosePrice) * 100;
                            coin.OneHourChange = ((coin.CurrentPrice - Twelve_FiveMin.Last().ClosePrice) / Twelve_FiveMin.Last().ClosePrice) * 100;
                            coin.TwentyFourHourChange = ((coin.CurrentPrice - AllFiveMin.Last().ClosePrice) / AllFiveMin.Last().ClosePrice) * 100;
                            //coin.DayLowPrice = AllFiveMin.Min(x => x.ClosePrice);
                            //coin.DayHighPrice = AllFiveMin.Max(x => x.ClosePrice);
                            //coin.DayOpenPrice = AllFiveMin.First().ClosePrice;
                        }
                        else
                        {
                            //coin.DayLowPrice = coin.CurrentPrice;
                            //coin.DayHighPrice = coin.CurrentPrice;
                            //coin.DayOpenPrice = coin.CurrentPrice;
                            coin.OneHourChange = 0;
                            coin.FourtyFiveMinChange = 0;
                            coin.TwentyFourHourChange = 0;
                        }


                        var isFifteenOnUpTrend = false;

                        if (FifteenMin.Any())
                        {
                            isFifteenOnUpTrend = coin.CurrentPrice >= FifteenMin.Max(x => x.ClosePrice);
                        }


                        var isThirtyOnUpTrend = false;

                        if (ThirtyMin.Any())
                        {
                            isThirtyOnUpTrend = coin.CurrentPrice >= ThirtyMin.Max(x => x.ClosePrice);
                        }


                        var FourOneHour = OneHour.Take(4);
                        bool isHourOnUpTrend = false;

                        if (OneHour.Any())
                        {
                            coin.FourHourChange = ((coin.CurrentPrice - FourOneHour.Last().ClosePrice) / FourOneHour.Last().ClosePrice) * 100;
                            isHourOnUpTrend = coin.CurrentPrice >= FourOneHour.Max(x => x.ClosePrice);
                        }
                        else
                        {
                            coin.FourHourChange = 0;
                        }


                        bool isFourHourOnUpTrend = false;
                        if (FourHour.Any())
                        {
                            coin.FortyEightHourChange = ((coin.CurrentPrice - FourHour.Last().ClosePrice) / FourHour.Last().ClosePrice) * 100;
                            isFourHourOnUpTrend = coin.CurrentPrice >= FourHour.Max(x => x.ClosePrice);
                        }
                        else
                        {
                            coin.FortyEightHourChange = 0;
                        }

                        var OneDay = sig.Ref1DayCandles.Take(7);
                        bool isOneDayOnUpTrend = false;

                        if (OneDay.Any())
                        {
                            coin.OneWeekChange = ((OneDay.First().ClosePrice - OneDay.Last().ClosePrice) / OneDay.Last().ClosePrice) * 100;
                            isOneDayOnUpTrend = coin.CurrentPrice >= OneDay.Max(x => x.ClosePrice);
                        }
                        else
                        {
                            coin.OneWeekChange = 0;
                        }

                        coin.ClimbingFast = isOneOnUpTrend && isFiveOnUpTrend && isFifteenOnUpTrend; //&& isThirtyOnUpTrend
                        coin.ClimbedHigh = isOneOnUpTrend && isFiveOnUpTrend && isFifteenOnUpTrend && isThirtyOnUpTrend;
                        coin.SuperHigh = isOneOnUpTrend && isFiveOnUpTrend && isFifteenOnUpTrend && isThirtyOnUpTrend && isHourOnUpTrend;

                        coin.DayTradeCount = sig.DayTradeCount;
                        //coin.DayVolume = coinSignal.DayVol;

                        if (coin.ClimbingFast)
                        {
                            coin.TradeSuggestion = "L1: up in five 1,5 and 15 min candles<br>Good to buy";
                        }
                        else if (coin.ClimbedHigh)
                        {
                            coin.TradeSuggestion = "L2: up in five 1,5,15,30 min candles<br>Mostly will go high";
                        }
                        else if (coin.SuperHigh)
                        {
                            coin.TradeSuggestion = "L3: up in five 1,5,15,30,60 min candles<br>Can go higher or will start to go down?";
                        }
                        else
                        {
                            coin.TradeSuggestion = String.Empty;
                        }
                        db.Update(coin);
                    }
                    catch
                    {

                    }
                }
                db.SaveChanges();
            }
        }

        private void CollectReferenceCandles()
        {

            bool fiveminsdeleted = false;
            for (int i = 0; i < MySignals.Count; i++)
            {
                try
                {
                    #region day

                    if (DateTime.Now.Hour % 23 == 0 && DateTime.Now.Minute % 57 == 0)
                    {

                        #region delete All 5 min candles to start from Scratch

                        if (fiveminsdeleted == false)
                        {
                            using (var db = new DB())
                            {
                                var candleQuery = "delete from SignalCandle where CandleType='5min'";
                                db.Database.ExecuteSqlRaw(candleQuery);

                                fiveminsdeleted = true;
                            }
                        }

                        #endregion

                        MySignals[i].Ref5MinCandles = new List<SignalCandle>(); // Reset 5 Mins Candle from Memory

                        MySignals[i].Ref1DayCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1DayCandles, "day", CandleLimit.OneDayLimit, 23, 57);
                        MySignals[i].TotalConsecutive1DayDowns = GetConsecutiveUpDowns(MySignals[i].Ref1DayCandles, "down");
                        MySignals[i].TotalConsecutive1DayUps = GetConsecutiveUpDowns(MySignals[i].Ref1DayCandles, "up");

                        if (MySignals[i].Ref1DayCandles != null && MySignals[i].Ref1DayCandles.Count > 0)
                        {
                            if (MySignals[i].CurrPr <= MySignals[i].Ref1DayCandles.Min(x => x.ClosePrice))
                            {
                                MySignals[i].TotalConsecutive1DayDowns++;
                                if (MySignals[i].TotalConsecutive1DayUps > 0) MySignals[i].TotalConsecutive1DayUps--;
                            }
                            else
                            {
                                MySignals[i].TotalConsecutive1DayUps++;
                                if (MySignals[i].TotalConsecutive1DayDowns > 0) MySignals[i].TotalConsecutive1DayDowns--;
                            }
                        }
                    }

                    MySignals[i].PrChPercCurrAndRef1Day = GetPrChgBetnCrAndRefStart(MySignals[i].CurrPr, MySignals[i].Ref1DayCandles);

                    #endregion day

                    #region 4 hour

                    if ((DateTime.Now.Hour == 3 || DateTime.Now.Hour == 7 || DateTime.Now.Hour == 11 || DateTime.Now.Hour == 15 ||
                        DateTime.Now.Hour == 19 || DateTime.Now.Hour == 23)
                        && DateTime.Now.Minute % 57 == 0)
                    {
                        MySignals[i].Ref4HourCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref4HourCandles, "4hour", CandleLimit.FourHourLimit, DateTime.Now.Hour, 57);
                        MySignals[i].TotalConsecutive4HourDowns = GetConsecutiveUpDowns(MySignals[i].Ref4HourCandles, "down");
                        MySignals[i].TotalConsecutive4HourUps = GetConsecutiveUpDowns(MySignals[i].Ref4HourCandles, "up");

                        if (MySignals[i].Ref4HourCandles != null && MySignals[i].Ref4HourCandles.Count > 0)
                        {
                            if (MySignals[i].CurrPr <= MySignals[i].Ref4HourCandles.Min(x => x.ClosePrice))
                            {
                                MySignals[i].TotalConsecutive4HourDowns++;
                                if (MySignals[i].TotalConsecutive4HourUps > 0) MySignals[i].TotalConsecutive4HourUps--;
                            }
                            else
                            {
                                MySignals[i].TotalConsecutive4HourUps++;
                                if (MySignals[i].TotalConsecutive4HourDowns > 0) MySignals[i].TotalConsecutive4HourDowns--;
                            }
                        }
                    }
                    MySignals[i].PrChPercCurrAndRef4Hour = GetPrChgBetnCrAndRefStart(MySignals[i].CurrPr, MySignals[i].Ref4HourCandles);

                    //if (MySignals[i].Ref4HourCandles.Count > 0)
                    //{
                    //    MySignals[i].MinRef4Hour = MySignals[i].Ref4HourCandles.Min(x => x.ClosePrice);
                    //    MySignals[i].MaxRef4Hour = MySignals[i].Ref4HourCandles.Max(x => x.ClosePrice);
                    //}
                    #endregion 4hour

                    #region 1 hour

                    if (DateTime.Now.Minute % 57 == 0) //Collected for last 24 hours
                    {
                        MySignals[i].Ref1HourCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1HourCandles, "1hour", CandleLimit.OneHourLimit, DateTime.Now.Hour, 57); // 24 hours
                        MySignals[i].TotalConsecutive1HourDowns = GetConsecutiveUpDowns(MySignals[i].Ref1HourCandles, "down");
                        MySignals[i].TotalConsecutive1HourUps = GetConsecutiveUpDowns(MySignals[i].Ref1HourCandles, "up");

                        if (MySignals[i].Ref1HourCandles != null && MySignals[i].Ref1HourCandles.Count > 0)
                        {
                            if (MySignals[i].CurrPr <= MySignals[i].Ref1HourCandles.Min(x => x.ClosePrice))
                            {
                                MySignals[i].TotalConsecutive1HourDowns++;
                                if (MySignals[i].TotalConsecutive1HourUps > 0) MySignals[i].TotalConsecutive1HourUps--;
                            }
                            else
                            {
                                MySignals[i].TotalConsecutive1HourUps++;
                                if (MySignals[i].TotalConsecutive1HourDowns > 0) MySignals[i].TotalConsecutive1HourDowns--;
                            }
                        }
                    }

                    MySignals[i].PrChPercCurrAndRef1Hour = GetPrChgBetnCrAndRefStart(MySignals[i].CurrPr, MySignals[i].Ref1HourCandles);

                    #endregion 1hour

                    #region 30 minute

                    if (DateTime.Now.Minute % 30 == 0) //Collected for last 6 hours
                    {
                        MySignals[i].Ref30MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref30MinCandles, "30min", CandleLimit.ThirtyMinLimit, DateTime.Now.Hour, DateTime.Now.Minute);
                        MySignals[i].TotalConsecutive30MinDowns = GetConsecutiveUpDowns(MySignals[i].Ref30MinCandles, "down");
                        MySignals[i].TotalConsecutive30MinUps = GetConsecutiveUpDowns(MySignals[i].Ref30MinCandles, "up");

                        if (MySignals[i].Ref30MinCandles != null && MySignals[i].Ref30MinCandles.Count > 0)
                        {
                            if (MySignals[i].CurrPr <= MySignals[i].Ref30MinCandles.Min(x => x.ClosePrice))
                            {
                                MySignals[i].TotalConsecutive30MinDowns++;
                                if (MySignals[i].TotalConsecutive30MinUps > 0) MySignals[i].TotalConsecutive30MinUps--;
                            }
                            else
                            {
                                MySignals[i].TotalConsecutive30MinUps++;
                                if (MySignals[i].TotalConsecutive30MinDowns > 0) MySignals[i].TotalConsecutive30MinDowns--;
                            }
                        }



                    }
                    MySignals[i].PrChPercCurrAndRef30Min = GetPrChgBetnCrAndRefStart(MySignals[i].CurrPr, MySignals[i].Ref30MinCandles);

                    #endregion 30minute

                    #region 15 minute

                    if (DateTime.Now.Minute % 15 == 0) // collected for last 3 hours
                    {
                        MySignals[i].Ref15MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref15MinCandles, "15min", CandleLimit.FifteenMinLimit, DateTime.Now.Hour, DateTime.Now.Minute);
                        MySignals[i].TotalConsecutive15MinDowns = GetConsecutiveUpDowns(MySignals[i].Ref15MinCandles, "down");
                        MySignals[i].TotalConsecutive15MinUps = GetConsecutiveUpDowns(MySignals[i].Ref15MinCandles, "up");

                        if (MySignals[i].Ref15MinCandles != null && MySignals[i].Ref15MinCandles.Count > 0)
                        {
                            if (MySignals[i].CurrPr <= MySignals[i].Ref15MinCandles.Min(x => x.ClosePrice))
                            {
                                MySignals[i].TotalConsecutive15MinDowns++;
                                if (MySignals[i].TotalConsecutive15MinUps > 0) MySignals[i].TotalConsecutive15MinUps--;
                            }
                            else
                            {
                                MySignals[i].TotalConsecutive15MinUps++;
                                if (MySignals[i].TotalConsecutive15MinDowns > 0) MySignals[i].TotalConsecutive15MinDowns--;
                            }
                        }

                    }
                    MySignals[i].PrChPercCurrAndRef15Min = GetPrChgBetnCrAndRefStart(MySignals[i].CurrPr, MySignals[i].Ref15MinCandles);

                    #endregion 15minute

                    #region 5 minute

                    if (DateTime.Now.Minute % 5 == 0) // collected for last day
                    {
                        MySignals[i].Ref5MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref5MinCandles, "5min", CandleLimit.FiveMinLimit, DateTime.Now.Hour, DateTime.Now.Minute);
                        MySignals[i].TotalConsecutive5MinDowns = GetConsecutiveUpDowns(MySignals[i].Ref5MinCandles, "down");
                        MySignals[i].TotalConsecutive5MinUps = GetConsecutiveUpDowns(MySignals[i].Ref5MinCandles, "up");

                        if (MySignals[i].Ref5MinCandles != null && MySignals[i].Ref5MinCandles.Count > 0)
                        {
                            if (MySignals[i].CurrPr <= MySignals[i].Ref5MinCandles.Min(x => x.ClosePrice))
                            {
                                MySignals[i].TotalConsecutive5MinDowns++;
                                if (MySignals[i].TotalConsecutive5MinUps > 0) MySignals[i].TotalConsecutive5MinUps--;
                            }
                            else
                            {
                                MySignals[i].TotalConsecutive5MinUps++;
                                if (MySignals[i].TotalConsecutive5MinDowns > 0) MySignals[i].TotalConsecutive5MinDowns--;
                            }
                        }


                    }
                    MySignals[i].PrChPercCurrAndRef5Min = GetPrChgBetnCrAndRefStart(MySignals[i].CurrPr, MySignals[i].Ref5MinCandles);


                    #endregion 5minute

                    #region 1 minute

                    if (DateTime.Now.Minute % 1 == 0) // collected for last hour
                    {
                        MySignals[i].Ref1MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1MinCandles, "1min", CandleLimit.OneMinLimit, DateTime.Now.Hour, DateTime.Now.Minute);
                        MySignals[i].TotalConsecutive1MinDowns = GetConsecutiveUpDowns(MySignals[i].Ref1MinCandles, "down");
                        MySignals[i].TotalConsecutive1MinUps = GetConsecutiveUpDowns(MySignals[i].Ref1MinCandles, "up");

                        if (MySignals[i].Ref1MinCandles != null && MySignals[i].Ref1MinCandles.Count > 0)
                        {
                            if (MySignals[i].CurrPr <= MySignals[i].Ref1MinCandles.Min(x => x.ClosePrice))
                            {
                                MySignals[i].TotalConsecutive1MinDowns++;
                                if (MySignals[i].TotalConsecutive1MinUps > 0) MySignals[i].TotalConsecutive1MinUps--;
                            }
                            else
                            {
                                MySignals[i].TotalConsecutive1MinUps++;
                                if (MySignals[i].TotalConsecutive1MinDowns > 0) MySignals[i].TotalConsecutive1MinDowns--;
                            }
                        }

                    }
                    MySignals[i].PrChPercCurrAndRef1Min = GetPrChgBetnCrAndRefStart(MySignals[i].CurrPr, MySignals[i].Ref1MinCandles);

                    #endregion 1minute

                    #region calculations

                    MySignals[i].PrDiffHighAndLowPerc = MySignals[i].DayHighPr.GetDiffPercBetnNewAndOld(MySignals[i].DayLowPr);
                    MySignals[i].PrDiffCurrAndLowPerc = MySignals[i].CurrPr.GetDiffPercBetnNewAndOld(MySignals[i].DayLowPr);
                    MySignals[i].PrDiffCurrAndHighPerc = MySignals[i].CurrPr.GetDiffPercBetnNewAndOld(MySignals[i].DayHighPr);
                    MySignals[i].JustRecoveredFromDayLow = MySignals[i].PrDiffCurrAndLowPerc >= configr.DayLowGreaterthanTobuy && MySignals[i].PrDiffCurrAndLowPerc <= configr.DayLowLessthanTobuy;

                    MySignals[i].IsAtDayLow = MySignals[i].CurrPr < MySignals[i].DayAveragePr;
                    MySignals[i].IsAtDayHigh = MySignals[i].CurrPr > MySignals[i].DayAveragePr;

                    var dayAveragePrice = (MySignals[i].DayHighPr + MySignals[i].DayLowPr) / 2;
                    dayAveragePrice = dayAveragePrice - (dayAveragePrice * 2M / 100);
                    MySignals[i].IsCloseToDayLow = MySignals[i].CurrPr < dayAveragePrice;
                    MySignals[i].DayAveragePr = (MySignals[i].DayHighPr + MySignals[i].DayLowPr) / 2;

                    if (MySignals[i].Ref1HourCandles.Count > 0)
                    {
                        MySignals[i].DayHighPr = MySignals[i].Ref1HourCandles.Max(x => x.ClosePrice);
                        MySignals[i].DayLowPr = MySignals[i].Ref1HourCandles.Min(x => x.ClosePrice);
                    }
                    else
                    {
                        MySignals[i].DayHighPr = MySignals[i].Ref1MinCandles.Max(x => x.ClosePrice);
                        MySignals[i].DayLowPr = MySignals[i].Ref1MinCandles.Min(x => x.ClosePrice);
                    }

                    var coin = myCoins.Where(x => x.Pair == MySignals[i].Symbol).FirstOrDefault();



                    if (coin != null)
                    {
                        MySignals[i].IsIncludedForTrading = coin.IsIncludedForTrading;
                    }
                    else
                    {
                        MySignals[i].IsIncludedForTrading = false;
                    }

                    MySignals[i].IsBestTimeToBuyAtDayLowest = MySignals[i].CurrPr > 0 &&
                                                              MySignals[i].PrDiffCurrAndHighPerc < MySignals[i].PercBelowDayHighToBuy &&
                                                              MySignals[i].PrDiffHighAndLowPerc > MySignals[i].PercAboveDayLowToSell &&
                                                              MySignals[i].IsCloseToDayLow;

                    MySignals[i].IsBestTimeToSellAtDayHighest = MySignals[i].CurrPr > 0 &&
                                                                MySignals[i].PrDiffHighAndLowPerc > MySignals[i].PercAboveDayLowToSell &&
                                                                MySignals[i].IsAtDayHigh;

                    #endregion calculations

                }
                catch (Exception ex)
                {
                    logger.Info("Exception at CollectReferenceCandles " + MySignals[i].Symbol + " " + ex.Message);
                }
            }

            using (var db = new DB())
            {
                foreach (var coin in myCoins)
                {
                    try
                    {
                        var sig = MySignals.Where(x => x.Symbol == coin.Pair).FirstOrDefault();
                        var AllOneMin = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime);
                        var AllFiveMin = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime);
                        var FifteenMin = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(3);
                        var ThirtyMin = sig.Ref30MinCandles.OrderByDescending(x => x.CloseTime).Take(5);
                        var OneHour = sig.Ref1HourCandles.OrderByDescending(x => x.CloseTime);
                        var FourHour = sig.Ref4HourCandles.OrderByDescending(x => x.CloseTime);

                        var Fifteen_OneMin = AllOneMin.Take(15);
                        var Ten_OneMin = AllOneMin.Take(10);
                        var Five_OneMin = AllOneMin.Take(5);

                        var isOneOnUpTrend = false;

                        if (AllOneMin.Any())
                        {
                            isOneOnUpTrend = Ten_OneMin.First().ClosePrice >= Ten_OneMin.Max(x => x.ClosePrice);

                            coin.FiveMinChange = ((coin.CurrentPrice - Five_OneMin.Last().ClosePrice) / Five_OneMin.Last().ClosePrice) * 100;
                            coin.TenMinChange = ((coin.CurrentPrice - Ten_OneMin.Last().ClosePrice) / Ten_OneMin.Last().ClosePrice) * 100;
                            coin.FifteenMinChange = ((coin.CurrentPrice - Fifteen_OneMin.Last().ClosePrice) / Fifteen_OneMin.Last().ClosePrice) * 100;
                            coin.ThirtyMinChange = ((coin.CurrentPrice - AllOneMin.Last().ClosePrice) / AllOneMin.Last().ClosePrice) * 100;
                        }
                        else
                        {
                            isOneOnUpTrend = false;
                            coin.ThirtyMinChange = 0;
                            coin.FiveMinChange = 0;
                            coin.TenMinChange = 0;
                            coin.FifteenMinChange = 0;
                        }


                        var Five_FiveMin = AllFiveMin.Take(5).ToList();
                        var Nine_FiveMin = AllFiveMin.Take(9).ToList();
                        var Twelve_FiveMin = AllFiveMin.Take(12).ToList();

                        var isFiveOnUpTrend = false;
                        //   coin.CurrentPrice = sig.CurrPr;

                        if (AllFiveMin.Any())
                        {
                            isFiveOnUpTrend = coin.CurrentPrice >= Five_FiveMin.Max(x => x.ClosePrice);
                            coin.FourtyFiveMinChange = ((coin.CurrentPrice - Nine_FiveMin.Last().ClosePrice) / Nine_FiveMin.Last().ClosePrice) * 100;
                            coin.OneHourChange = ((coin.CurrentPrice - Twelve_FiveMin.Last().ClosePrice) / Twelve_FiveMin.Last().ClosePrice) * 100;
                            coin.TwentyFourHourChange = ((coin.CurrentPrice - AllFiveMin.Last().ClosePrice) / AllFiveMin.Last().ClosePrice) * 100;
                            //coin.DayLowPrice = AllFiveMin.Min(x => x.ClosePrice);
                            //coin.DayHighPrice = AllFiveMin.Max(x => x.ClosePrice);
                            //coin.DayOpenPrice = AllFiveMin.First().ClosePrice;
                        }
                        else
                        {
                            //coin.DayLowPrice = coin.CurrentPrice;
                            //coin.DayHighPrice = coin.CurrentPrice;
                            //coin.DayOpenPrice = coin.CurrentPrice;
                            coin.OneHourChange = 0;
                            coin.FourtyFiveMinChange = 0;
                            coin.TwentyFourHourChange = 0;
                        }


                        var isFifteenOnUpTrend = false;

                        if (FifteenMin.Any())
                        {
                            isFifteenOnUpTrend = coin.CurrentPrice >= FifteenMin.Max(x => x.ClosePrice);
                        }


                        var isThirtyOnUpTrend = false;

                        if (ThirtyMin.Any())
                        {
                            isThirtyOnUpTrend = coin.CurrentPrice >= ThirtyMin.Max(x => x.ClosePrice);
                        }


                        var FourOneHour = OneHour.Take(4);
                        bool isHourOnUpTrend = false;

                        if (OneHour.Any())
                        {
                            coin.FourHourChange = ((coin.CurrentPrice - FourOneHour.Last().ClosePrice) / FourOneHour.Last().ClosePrice) * 100;
                            isHourOnUpTrend = coin.CurrentPrice >= FourOneHour.Max(x => x.ClosePrice);
                        }
                        else
                        {
                            coin.FourHourChange = 0;
                        }


                        bool isFourHourOnUpTrend = false;
                        if (FourHour.Any())
                        {
                            coin.FortyEightHourChange = ((coin.CurrentPrice - FourHour.Last().ClosePrice) / FourHour.Last().ClosePrice) * 100;
                            isFourHourOnUpTrend = coin.CurrentPrice >= FourHour.Max(x => x.ClosePrice);
                        }
                        else
                        {
                            coin.FortyEightHourChange = 0;
                        }

                        var OneDay = sig.Ref1DayCandles.Take(7);
                        bool isOneDayOnUpTrend = false;

                        if (OneDay.Any())
                        {
                            coin.OneWeekChange = ((OneDay.First().ClosePrice - OneDay.Last().ClosePrice) / OneDay.Last().ClosePrice) * 100;
                            isOneDayOnUpTrend = coin.CurrentPrice >= OneDay.Max(x => x.ClosePrice);
                        }
                        else
                        {
                            coin.OneWeekChange = 0;
                        }

                        coin.ClimbingFast = isOneOnUpTrend && isFiveOnUpTrend && isFifteenOnUpTrend; //&& isThirtyOnUpTrend
                        coin.ClimbedHigh = isOneOnUpTrend && isFiveOnUpTrend && isFifteenOnUpTrend && isThirtyOnUpTrend;
                        coin.SuperHigh = isOneOnUpTrend && isFiveOnUpTrend && isFifteenOnUpTrend && isThirtyOnUpTrend && isHourOnUpTrend;

                        coin.DayTradeCount = sig.DayTradeCount;
                        //coin.DayVolume = coinSignal.DayVol;

                        if (coin.ClimbingFast)
                        {
                            coin.TradeSuggestion = "L1: up in five 1,5 and 15 min candles<br>Good to buy";
                        }
                        else if (coin.ClimbedHigh)
                        {
                            coin.TradeSuggestion = "L2: up in five 1,5,15,30 min candles<br>Mostly will go high";
                        }
                        else if (coin.SuperHigh)
                        {
                            coin.TradeSuggestion = "L3: up in five 1,5,15,30,60 min candles<br>Can go higher or will start to go down?";
                        }
                        else
                        {
                            coin.TradeSuggestion = String.Empty;
                        }
                        db.Update(coin);
                    }
                    catch
                    {

                    }
                }
                db.SaveChanges();
            }
        }

        private void CreateBuyLowestSellHighestSignals()
        {
            //Upgrades

            //1. If the current trend is downwards ( You can consider how coins are doing on daily, 2 days and weekly, just dont buy
            //BTC,ETH,ADA,BNB,SOL,AVAX,ALGO,LTC,ATOM,DOT ( The indicator coins must be gaining today,two days. Otherwise dont enter trade - They are on downtrend)
            //2. Only if the current trend is upwards you must start buying
            //3. Buy only when you see the coins are going upwards ( At least recovered 50% from their lowest recent point ( Weekly or biweekly)
            //4. If a coin has gained so much in the last day or week, buy only if its current price is below average of the last week.
            //if the price is not less than the day average, the two day average do not buy that coin (There is a huge chance of them coing down).
            // if the weekly gain is more than 25% and current price is not less than half of the maximum price, dont buy. Then can crash so bad.
            //5.Even after coins feel a lot, wait till you see clear up trend in the market before buying
            //6. When your loss is worse than 5% sell all, and wait for clear indication manually to restart trading.
            //7. If the day or week is upbeat and on an upward trend, do not sell prematurely. Wait till the day clearly shows a downtrend.
            //8. Calculations are db intensive. Give sufficent time to calculate and complete the trade
        }

        private void LogInfo()
        {
            if (configr.ShowBuyLogs)
            {
                logger.Info("");
                logger.Info("Buyables");
                logger.Info("--------");

                foreach (var sig in MySignals.OrderBy(x => x.PrDiffCurrAndHighPerc))
                {
                    var check1 = (sig.PrDiffCurrAndLowPerc >= configr.DayLowGreaterthanTobuy && sig.PrDiffCurrAndLowPerc <= configr.DayLowLessthanTobuy) ? "Y" : "N";
                    var check2 = (sig.PrDiffCurrAndHighPerc < sig.PercBelowDayHighToBuy) ? "Y" : "N";
                    var check3 = (sig.PrDiffHighAndLowPerc > sig.PercAboveDayLowToSell) ? "Y" : "N";

                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(6, ' ') +
                             " " + sig.CoinId.ToString().PadRight(5, ' ') +
                              " Pr " + sig.CurrPr.Rnd(3).ToString().PadRight(11, ' ') +
                              " Lo " + sig.DayLowPr.Rnd(3).ToString().PadRight(11, ' ') +
                              " Hi " + sig.DayHighPr.Rnd(3).ToString().PadRight(11, ' ') +
                              " Cr&Lw " + sig.PrDiffCurrAndLowPerc.Rnd(2).ToString().PadRight(5, ' ')
                              + " > " + configr.DayLowGreaterthanTobuy.Rnd(1) +
                              " & < " + configr.DayLowLessthanTobuy.Rnd(1) + " : "
                              + check1.PadRight(3, ' ') +
                               " Cr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(1).ToString().PadRight(6, ' ') +
                               " < " + sig.PercBelowDayHighToBuy.Rnd(1).ToString().PadRight(6, ' ') +
                               " : " + check2.PadRight(3, ' ') +
                               " Hi&Lw " + sig.PrDiffHighAndLowPerc.Rnd(1).ToString().PadRight(5, ' ') +
                               " > " + sig.PercAboveDayLowToSell.Rnd(1).ToString().PadRight(6, ' ') + " : "
                               + check3.PadRight(3, ' ') +
                               " TrCnt " + sig.DayTradeCount.Rnd(0);

                    if (sig.IsBestTimeToBuyAtDayLowest)
                        logger.Info(log);
                }
            }

            if (configr.ShowNoBuyLogs)
            {
                logger.Info("");
                logger.Info("Not Buyables");
                logger.Info("------------");
                foreach (var sig in MySignals.OrderBy(x => x.PrDiffCurrAndLowPerc))
                {
                    var check1 = (sig.PrDiffCurrAndLowPerc >= configr.DayLowGreaterthanTobuy && sig.PrDiffCurrAndLowPerc <= configr.DayLowLessthanTobuy) ? "Y" : "N";
                    var check2 = (sig.PrDiffCurrAndHighPerc < sig.PercBelowDayHighToBuy) ? "Y" : "N";
                    var check3 = (sig.PrDiffHighAndLowPerc > sig.PercAboveDayLowToSell) ? "Y" : "N";

                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(6, ' ') +
                             " " + sig.CoinId.ToString().PadRight(5, ' ') +
                              " Pr " + sig.CurrPr.Rnd(3).ToString().PadRight(11, ' ') +
                              " Lo " + sig.DayLowPr.Rnd(3).ToString().PadRight(11, ' ') +
                              " Hi " + sig.DayHighPr.Rnd(3).ToString().PadRight(11, ' ') +
                              " Cr&Lw " + sig.PrDiffCurrAndLowPerc.Rnd(2).ToString().PadRight(5, ' ')
                              + " > " + configr.DayLowGreaterthanTobuy.Rnd(1) +
                              " & < " + configr.DayLowLessthanTobuy.Rnd(1) + " : "
                              + check1.PadRight(3, ' ') +
                               " Cr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(1).ToString().PadRight(6, ' ') +
                               " < " + sig.PercBelowDayHighToBuy.Rnd(1).ToString().PadRight(6, ' ') +
                               " : " + check2.PadRight(3, ' ') +
                               " Hi&Lw " + sig.PrDiffHighAndLowPerc.Rnd(1).ToString().PadRight(5, ' ') +
                               " > " + sig.PercAboveDayLowToSell.Rnd(1).ToString().PadRight(6, ' ') + " : "
                               + check3.PadRight(3, ' ') +
                               " TrCnt " + sig.DayTradeCount.Rnd(0);

                    if (!sig.IsBestTimeToBuyAtDayLowest)
                        logger.Info(log);
                }
            }

            if (configr.ShowScalpBuyLogs)
            {
                logger.Info("");
                logger.Info("Scalp Buyables");

                logger.Info("----------------");

                foreach (var sig in MySignals.OrderBy(x => x.PrChPercCurrAndRef30Min))
                {

                    //var OneMinCandles = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime).Take(5);

                    //var IsOneMinOnUpTrend = OneMinCandles.First().ClosePrice >= OneMinCandles.Max(x => x.ClosePrice);

                    //var FiveMinCandles = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime).Take(4);

                    //var IsFiveMinOnUpTrend = FiveMinCandles.First().ClosePrice >= FiveMinCandles.Max(x => x.ClosePrice);

                    //var FifteenMinCandles = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

                    //var IsFifteenMinOnUpTrend = FifteenMinCandles.First().ClosePrice >= FifteenMinCandles.Max(x => x.ClosePrice);

                    //// prices are going down. Dont buy till you see recovery
                    //if (IsOneMinOnUpTrend && IsFiveMinOnUpTrend && IsFifteenMinOnUpTrend)
                    //{
                    //    logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
                    //      " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
                    //      "  is on uptrend "
                    //      );


                    //}

                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(8, ' ')
                                    + " " + sig.CoinId.ToString().PadRight(8, ' ') +
                                 " 4HD>3 : " + sig.TotalConsecutive4HourDowns.ToString().PadRight(6, ' ')
                                 + " " + sig.PrChPercCurrAndRef4Hour.Rnd(3).ToString().PadRight(7, ' ') +
                                 " HrD>4 : " + sig.TotalConsecutive1HourDowns.ToString().PadRight(6, ' ')
                                 + " " + sig.PrChPercCurrAndRef1Hour.Rnd(3).ToString().PadRight(7, ' ') +
                                 " 30D>4 : " + sig.TotalConsecutive30MinDowns.ToString().PadRight(6, ' ')
                                 + " " + sig.PrChPercCurrAndRef30Min.Rnd(3).ToString().PadRight(7, ' ') +
                                 " 15D>5 : " + sig.TotalConsecutive15MinDowns.ToString().PadRight(6, ' ')
                                 + " " + sig.PrChPercCurrAndRef15Min.Rnd(3).ToString().PadRight(7, ' ') +
                                 " 5D>5 : " + sig.TotalConsecutive5MinDowns.ToString().PadRight(6, ' ')
                                 + " " + sig.PrChPercCurrAndRef5Min.Rnd(3).ToString().PadRight(7, ' ') +
                                 " Lowest :" + sig.IsBestTimeToBuyAtDayLowest.ToString().PadRight(6, ' ') +
                                 " CrHiDf<-2 :" + (sig.PrDiffCurrAndHighPerc < -2M).ToString().PadRight(6, ' ') +
                                 " HiLwDf>3 :" + (sig.PrDiffHighAndLowPerc > 3M).ToString().PadRight(6, ' ') +
                                 " DHi: " + (sig.CurrPr < ((sig.DayHighPr + sig.DayAveragePr) / configr.DivideHighAndAverageBy)).ToString();


                    if (sig.IsBestTimeToScalpBuy)
                        logger.Info(log);
                }
            }

            if (configr.ShowNoScalpBuyLogs)
            {
                logger.Info("");
                logger.Info("Not Scalp Buyables");
                logger.Info("----------------");
                foreach (var sig in MySignals.OrderBy(x => x.PrChPercCurrAndRef30Min))
                {
                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(8, ' ')
                                    + " " + sig.CoinId.ToString().PadRight(8, ' ') +
                                    " 4HD<3 : " + sig.TotalConsecutive4HourDowns.ToString().PadRight(6, ' ')
                                     + " " + sig.PrChPercCurrAndRef4Hour.Rnd(3).ToString().PadRight(7, ' ') +
                                    " HrD<4 : " + sig.TotalConsecutive1HourDowns.ToString().PadRight(6, ' ')
                                     + " " + sig.PrChPercCurrAndRef1Hour.Rnd(3).ToString().PadRight(7, ' ') +
                                    " 30D<4 : " + sig.TotalConsecutive30MinDowns.ToString().PadRight(6, ' ') +
                                      " " + sig.PrChPercCurrAndRef30Min.Rnd(3).ToString().PadRight(7, ' ') +
                                    " 15D<5 : " + sig.TotalConsecutive15MinDowns.ToString().PadRight(6, ' ')
                                       + " " + sig.PrChPercCurrAndRef15Min.Rnd(3).ToString().PadRight(7, ' ') +
                                    " 5D<5 : " + sig.TotalConsecutive5MinDowns.ToString().PadRight(6, ' ') +
                                      " " + sig.PrChPercCurrAndRef5Min.Rnd(3).ToString().PadRight(7, ' ') +
                                    " Lowest : " + sig.IsBestTimeToBuyAtDayLowest.ToString().PadRight(6, ' ') +
                                    " CrHiDf>-2 : " + (sig.PrDiffCurrAndHighPerc < -2M).ToString().PadRight(6, ' ') +
                                    " HiLwDf<3 : " + (sig.PrDiffHighAndLowPerc > 3M).ToString().PadRight(6, ' ') +
                                    " DHi : " + (sig.CurrPr < ((sig.DayHighPr + sig.DayAveragePr) / configr.DivideHighAndAverageBy)).ToString();

                    if (!sig.IsBestTimeToScalpBuy)
                        logger.Info(log);
                }
            }

            if (configr.ShowSellLogs)
            {
                logger.Info("");
                logger.Info("Sellables");
                logger.Info("---------");


                foreach (var sig in MySignals.OrderBy(x => x.PrChPercCurrAndRef30Min))
                {
                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(6, ' ') +
                                 " " + sig.CoinId.ToString().PadRight(3, ' ') +
                                 " Pr " + sig.CurrPr.Rnd(4).ToString().PadRight(11, ' ') +
                                  " DiCr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(3).ToString().PadRight(8, ' ') + " < " +
                                  configr.DayHighLessthanToSell.Rnd(3) + " & > " + configr.DayHighGreaterthanToSell.Rnd(3) + " : " +
                                  (sig.PrDiffCurrAndHighPerc <= configr.DayHighLessthanToSell && sig.PrDiffCurrAndHighPerc >= configr.DayHighGreaterthanToSell).ToString().PadRight(6, ' ') +
                                  " Hi&Lw " + sig.PrDiffHighAndLowPerc.Rnd(2).ToString().PadRight(6, ' ') + " > " + sig.PercAboveDayLowToSell.Rnd(3) + " : " + (sig.PrDiffHighAndLowPerc > sig.PercAboveDayLowToSell).ToString().PadRight(6, ' ') +
                                   " At DHi? " + (sig.PrDiffCurrAndHighPerc <= configr.DayHighLessthanToSell && sig.PrDiffCurrAndHighPerc >= configr.DayHighGreaterthanToSell).ToString().PadRight(6, ' ');

                    if (sig.IsBestTimeToSellAtDayHighest)
                    {
                        logger.Info(log + " Day Sell: Yes");
                    }
                }
            }

            if (configr.ShowNoSellLogs)
            {
                logger.Info("");
                logger.Info("Not Sellables");
                logger.Info("------------");

                foreach (var sig in MySignals.OrderBy(x => x.PrDiffCurrAndHighPerc))
                {
                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(6, ' ') +
                                             " " + sig.CoinId.ToString().PadRight(3, ' ') +
                                             " Pr " + sig.CurrPr.Rnd(4).ToString().PadRight(11, ' ') +
                                              " DiCr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(3).ToString().PadRight(8, ' ') + " < " +
                                              configr.DayHighLessthanToSell.Rnd(1) + " & > " + configr.DayHighGreaterthanToSell.Rnd(1) + " : " +
                                              (sig.PrDiffCurrAndHighPerc <= configr.DayHighLessthanToSell && sig.PrDiffCurrAndHighPerc >= configr.DayHighGreaterthanToSell).ToString().PadRight(6, ' ') +
                                              " Hi&Lw " + sig.PrDiffHighAndLowPerc.Rnd(2).ToString().PadRight(6, ' ') + " > " + sig.PercAboveDayLowToSell.Rnd(0) + " : " + (sig.PrDiffHighAndLowPerc > sig.PercAboveDayLowToSell).ToString().PadRight(6, ' ') +
                                               " At DHi? " + (sig.PrDiffCurrAndHighPerc <= configr.DayHighLessthanToSell && sig.PrDiffCurrAndHighPerc >= configr.DayHighGreaterthanToSell).ToString().PadRight(6, ' ');

                    if (!sig.IsBestTimeToSellAtDayHighest)
                    {
                        logger.Info(log + " Day Sell: No");
                    }
                }
                logger.Info("");
            }

            //logger.Info("Coins who gave exception while Creating (but probably are running without issue)");
            //logger.Info("");

            //foreach (var currentsocket in socket.AllSockets)
            //{
            //    logger.Info(currentsocket.Url.ToString());

            //}


            logger.Info("");

            foreach (var sig in MySignals)
            {
                if (sig.IsSymbolTickerSocketRunning == false)
                {
                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " Symbol Ticker Socket not running";
                    logger.Info(log);
                }

                if (sig.IsDailyKlineSocketRunning == false)
                {
                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " Kline Socket not running";
                    logger.Info(log);
                }
            }
            logger.Info("");
            //logger.Info("");
            //logger.Info("Coins whose Prices are marked as zero");
            //foreach (var sig in CurrentSignals)
            //{
            //    if (sig.CurrPr == 0)
            //    {
            //        string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +" Price is zero. Some issue";
            //        logger.Info(log);
            //    }
            //}
            //logger.Info("");

        }

        private async Task<bool> ShouldSkipPlayerFromBuying(Player player)
        {
            DB db = new DB();

            if (MySignals == null || MySignals.Count() == 0)
            {
                logger.Info("  " + StrTradeTime + " no signals found. So cannot proceed for buy process ");
                return true;
            }

            if (player.IsTrading)
            {
                await UpdateActivePlayerStats(player);
                return true;
            }

            if (player.isBuyOrderCompleted) // before buying the buyordercompleted should be reset to false, so dont buy if its true
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " isBuyOrderCompleted is true. Cant use it to buy");
                return true;
            }

            if (!myCoins.Any(x => x.ForceBuy == true))
            {
                if (player.isBuyAllowed == false)
                {
                    if (configr.ShowBuyingFlowLogs)
                        logger.Info("  " + StrTradeTime + " " + player.Name + "  Not  Allowed for buying");
                    return true;
                }
                if (configr.IsBuyingAllowed == false)
                {
                    if (configr.ShowBuyingFlowLogs)
                        logger.Info("  " + StrTradeTime + " " + player.Name + "  overall system not  Allowed for buying");
                    return true;
                }
            }

            return false;
        }

        private bool IsBitCoinGoingDown()
        {
            var sig = MySignals.Where(x => x.Symbol == "BTCUSDT").FirstOrDefault();

            var LastFive_OneMinCandles = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime).Take(5);

            var IsOneMinOnDownTrend = LastFive_OneMinCandles.First().ClosePrice <= LastFive_OneMinCandles.Min(x => x.ClosePrice);

            var LastThree_FiveMinCandles = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime).Take(3);

            var IsFiveMinOnDownTrend = LastThree_FiveMinCandles.First().ClosePrice <= LastThree_FiveMinCandles.Min(x => x.ClosePrice);

            //var LastTwo_FifteenMinCandles = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

            //var IsFifteenMinOnDownTrend = LastTwo_FifteenMinCandles.First().ClosePrice <= LastTwo_FifteenMinCandles.Min(x => x.ClosePrice);

            // prices are going down. Dont buy till you see recovery
            if (IsOneMinOnDownTrend || IsFiveMinOnDownTrend) //|| IsFifteenMinOnDownTrend
            {
                if (bitCoinStatuslogged == false)
                {
                    logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
                      " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
                      "  Bitcoin Prices going down.Wait till confirmation"
                      );
                    bitCoinStatuslogged = true;
                }

                return true;
            }
            return false;
        }

        private bool IsCoinPriceGoingDown(Signal sig)
        {

            //var OneMins = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime).Take(5);

            //var IsOneMinOnDownTrend = OneMins.First().ClosePrice <= OneMins.Min(x => x.ClosePrice);

            //var FiveMins = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

            //var IsFiveMinOnDownTrend = FiveMins.First().ClosePrice <= FiveMins.Min(x => x.ClosePrice);

            //var FifteenMins = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

            //var IsFifteenMinOnDownTrend = FifteenMins.First().ClosePrice <= FifteenMins.Min(x => x.ClosePrice);

            // prices are going down. Dont buy till you see recovery
            //if (IsOneMinOnDownTrend) //|| IsFiveMinOnDownTrend
            //{
            //    logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
            //      " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
            //      "  Prices going down.Wait till confirmation"
            //      );

            //    return true;
            //}
            return false;
        }

        public bool PricesGoingUp(Signal sig, Player player)
        {
            if (sig == null) return false;

            DB db = new DB();

            decimal maxofLastfew = 0;
            var referencecandletimes = sig.OpenTime.AddMinutes(-20);

            var lastfewsignals = db.Candle.AsNoTracking().Where(x => x.Symbol == sig.Symbol && x.OpenTime < sig.OpenTime
              && x.OpenTime >= referencecandletimes).ToList();

            if (lastfewsignals != null && lastfewsignals.Count > 0)
            {
                maxofLastfew = lastfewsignals.Max(x => x.CurrentPrice);
            }
            else
            {
                return false;
            }

            if (sig.CurrPr >= maxofLastfew)
            {
                logger.Info("  " +
                   sig.CloseTime.ToString("dd-MMM HH:mm") +
                 " " + player.Name +
                " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                 " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(11, ' ') +
                 " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(11, ' ') +
                 " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(11, ' ') +
                 " > max of Lst few rnds " + maxofLastfew.Rnd(5).ToString().PadRight(11, ' ') +
                 " PrDiff " + player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost).Deci().Rnd(5).ToString().PadRight(11, ' ') +
                 " going up. No Sell ");
                return true;
            }

            return false;
        }

        private bool IsCoinTradeCountTooLow(Signal sig)
        {

            // Day Trade Count too low
            if (sig.DayTradeCount < configr.MinAllowedTradeCount)
            {
                logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
                  " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
                  "  Day Trade Count too low. Dont buy " +
                  " Day Trade Count " + sig.DayTradeCount.Rnd(1)
                  );


                return true;
            }

            return false;
        }

        private async Task<bool> IsRecentlySold(Signal sig)
        {
            using (var db = new DB())
            {
                var recentSells = await db.PlayerTrades.Where(x => x.BuyOrSell != "Buy").OrderByDescending(x => x.Id).Take(15).ToListAsync();

                foreach (var recentSell in recentSells)
                {
                    if (recentSell.Pair == sig.Symbol && sig.CurrPr > (recentSell.BuyCoinPrice + recentSell.SellCoinPrice) / 2)
                    {
                        logger.Info(
                            sig.OpenTime.ToString("dd-MMM HH:mm") +
                            " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
                            "  Recently Sold. " +
                            " CrPr > last bought + sold / 2 " +
                 ((recentSell.BuyCoinPrice + recentSell.SellCoinPrice) / 2).Deci().Rnd(3).ToString().PadRight(7, ' ') + " ");
                        return true;
                    }
                }

            }

            return false;
        }

        private async Task BuyTheCoin(Player playertobuy, Signal sig, bool marketbuy)
        {

            await RedistributeBalances();


            DB db = new DB();

            var player = await db.Player.Where(x => x.Name == playertobuy.Name).FirstOrDefaultAsync();


            if (player.AvailableAmountToBuy < configr.MinimumAmountToTradeWith)
            {
                logger.Info(sig.Symbol + "  " + StrTradeTime + " " + player.Name + " Avl Amt " + player.AvailableAmountToBuy + " Not enough for buying ");
                return;
            }

            var PriceResponse = await client.GetPrice(sig.Symbol);


            decimal mybuyPrice = PriceResponse.Price;

            //var orderbook = await client.GetOrderBook(sig.Symbol, false, 8);
            // decimal mybuyPrice = orderbook.Asks.Min(x => x.Price);

            //foreach (var bid in orderbook.Bids)
            //{
            //    logger.Info(player.Pair + " Price " + bid.Price + " Qty " + bid.Quantity);
            //}
            //logger.Info(player.Pair + " Maximum bid is " + orderbook.Bids.Max(x => x.Price));

            LogBuy(player, sig);

            player.Pair = sig.Symbol;

            var coin = myCoins.Where(x => x.Pair == sig.Symbol).FirstOrDefault();

            var coinprecison = coin.TradePrecision;

            var quantity = (player.AvailableAmountToBuy.Deci() / mybuyPrice).Rnd(coinprecison);

            BaseCreateOrderResponse buyOrder = null;
            if (marketbuy)
            {
                buyOrder = await client.CreateOrder(new CreateOrderRequest()
                {
                    //Quantity = quantity,
                    //Side = OrderSide.Buy,
                    //Symbol = player.Pair,
                    //Type = OrderType.Market

                    Price = mybuyPrice,
                    Quantity = quantity,
                    Side = OrderSide.Buy,
                    Symbol = player.Pair,
                    Type = OrderType.Limit,
                    TimeInForce = TimeInForce.GTC
                });

                MyCoins buycoin = myCoins.Where(x => x.Pair == player.Pair).FirstOrDefault();

                if (buycoin.ForceBuy == true)
                {
                    buycoin.ForceBuy = false;
                    db.MyCoins.Update(buycoin);
                    logger.Info(player.Pair + " Marked forcebuy to false");
                }
                player.IsTrading = true;
                player.DayHigh = sig.DayHighPr;
                player.DayLow = sig.DayLowPr;
                player.BuyCoinPrice = mybuyPrice;
                player.Quantity = quantity;
                player.BuyCommision = player.AvailableAmountToBuy * configr.CommisionAmount / 100;
                player.TotalBuyCost = player.AvailableAmountToBuy + player.BuyCommision;
                player.CurrentCoinPrice = mybuyPrice;
                player.TotalCurrentValue = player.AvailableAmountToBuy; //exclude commision in the current value.
                player.BuyTime = DateTime.Now;
                player.SellBelowPerc = player.SellAbovePerc;
                player.BuyOrderId = buyOrder.OrderId;
                player.SellOrderId = 0;
                player.UpdatedTime = DateTime.Now;
                player.BuyOrSell = "Buy";
                player.SellTime = null;
                player.isSellAllowed = false;
                player.SellAtPrice = null;
                player.BuyAtPrice = null;

                player.SellCommision = player.BuyCommision;
                player.SellCoinPrice = mybuyPrice;
                player.ProfitLossAmt = (player.TotalCurrentValue - player.TotalBuyCost).Deci();
                player.TotalSellAmount = player.TotalBuyCost; // resetting available amount for trading
                player.AvailableAmountToBuy = 0; // bought, so no amount available to buy
                player.isBuyOrderCompleted = false;
                player.RepsTillCancelOrder = 0;
                player.SellAbovePerc = configr.DefaultSellAbovePerc;
                player.SellBelowPerc = configr.DefaultSellAbovePerc;
                db.Player.Update(player);
                PlayerTrades playerHistory = iPlayerMapper.Map<Player, PlayerTrades>(player);
                playerHistory.Id = 0;
                await db.PlayerTrades.AddAsync(playerHistory);

                await db.SaveChangesAsync();
            }

            //Send Buy Order


        }

        private async Task Buy()
        {
            DB db = new DB();

            await GetMyCoins();
            var Allcoins = await db.MyCoins.ToListAsync();
            var players = await db.Player.OrderBy(x => x.Id).ToListAsync();

            boughtCoins = await db.Player.Where(x => x.Pair != null).Select(x => x.Pair).ToListAsync();

            foreach (var player in players)
            {
                if (await ShouldSkipPlayerFromBuying(player) == true)
                    continue;

                var forcebuyCoin = Allcoins.Where(x => x.ForceBuy == true).FirstOrDefault();


                // foreach (Signal sig in MySignals.Where(x => x.ForceBuy == true))

                if (forcebuyCoin != null)
                {
                    var sig = MySignals.Where(x => x.Symbol == forcebuyCoin.Pair).FirstOrDefault();

                    //if (await IsRecentlySold(sig) || IsCoinTradeCountTooLow(sig))
                    //{
                    //    forcebuyCoin.ForceBuy=false;
                    //    db.Update(forcebuyCoin);
                    //    await db.SaveChangesAsync();
                    //    sig.IsIgnored = true;
                    //    continue;
                    //}

                    if (!IsCoinPriceGoingDown(sig))
                    {
                        await BuyTheCoin(player, sig, true);
                        sig.IsPicked = true;
                        boughtCoins.Add(sig.Symbol);
                        return;
                    }
                }
                else if (player.IsTrading == false && player.Pair != null && player.Pair.Length > 0 && player.BuyAtPrice != null && player.BuyAtPrice > 0)
                {
                    var sig = MySignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();

                    if (sig != null)
                    {
                        if (sig.CurrPr < player.BuyAtPrice)
                        {
                            logger.Info(player.Name + " Marked buying price " + player.BuyAtPrice.Deci().Rnd(7) + " reached for " + player.Pair + " .Buying");
                            await BuyTheCoin(player, sig, true);
                            sig.IsPicked = true;
                            boughtCoins.Add(sig.Symbol);
                            return;
                        }
                        else
                        {
                            logger.Info(player.Name + " Marked buying price " + player.BuyAtPrice.Deci().Rnd(7) + " not reached for " + player.Pair + " . Not Buying. Current Price is " + sig.CurrPr.Rnd(7));
                        }
                    }
                }

                continue;

                //if (MySignals.Any(x => x.ForceBuy == true))
                //{
                //    logger.Info("Coins marked for buying manually, so not going to buy automatically for " + player.Name);
                //    continue;
                //}
                //foreach (Signal sig in MySignals.OrderBy(x => x.PrDiffCurrAndHighPerc).ToList())
                //{

                //    if ((sig.IsIgnored || sig.IsPicked || !sig.IsIncludedForTrading) && sig.ForceBuy == false)
                //        continue;




                //    if (boughtCoins.Contains(sig.Symbol))
                //    {
                //        sig.IsPicked = true;
                //        continue;
                //    }
                //    else
                //    {
                //        sig.IsPicked = false;
                //    }

                //    if (sig.IsBestTimeToBuyAtDayLowest || sig.ForceBuy) // () sig.IsBestTimeToBuyAtDayLowest
                //    {
                //        //if (IsBitCoinGoingDown())
                //        //    continue;

                //        if (IsCoinPriceGoingDown(sig))
                //        {
                //            sig.IsIgnored = true;
                //            continue;
                //        }
                //        else
                //        {
                //            sig.IsIgnored = false;
                //        }


                //        if (IsCoinTradeCountTooLow(sig))
                //        {
                //            sig.IsIgnored = true;
                //            continue;
                //        }
                //        else
                //        {
                //            sig.IsIgnored = false;
                //        }
                //        try
                //        {
                //            await BuyTheCoin(player, sig, false);
                //            sig.IsPicked = true;
                //            boughtCoins.Add(sig.Symbol);
                //            break;
                //        }
                //        catch (Exception ex)
                //        {
                //            logger.Error(" " + player.Name + " " + sig.Symbol + " Exception: " + ex.ToString());
                //        }
                //    }
                //    else
                //    {
                //        LogNoBuy(player, sig);
                //        sig.IsPicked = false;
                //    }
                //}
            }
        }

        private async Task Sell(Player player)
        {
            #region initial Selling Set up

            string Quantityvalue = string.Empty;
            string predecimal = string.Empty;
            string decimals = string.Empty;
            string subdecimals = string.Empty;

            DB db = new DB();

            Signal sig = MySignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();
            if (ShouldReturnFromSelling(sig, player) == true) return;

            var pair = player.Pair;
            var mysellPrice = sig.CurrPr;
            player.Quantity = GetAvailQty(player, pair);
            player.DayHigh = sig.DayHighPr;
            player.DayLow = sig.DayLowPr;
            player.UpdatedTime = DateTime.Now;
            player.SellCoinPrice = mysellPrice;
            player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
            player.TotalSellAmount = mysellPrice * player.Quantity - player.SellCommision;
            player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
            player.CurrentCoinPrice = mysellPrice;
            player.TotalCurrentValue = player.TotalSellAmount;
            player.SellOrderId = 0;



            var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
            var NextSellbelow = prDiffPerc * configr.ReducePriceDiffPercBy / 100;

            player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";

            if (player.ProfitLossChanges.Length > 200)
                player.ProfitLossChanges = player.ProfitLossChanges.GetLast(200);

            #endregion  initial Selling Set up

            #region Sell Price is Set. Sell Now if met

            if (player.SellAtPrice != null && player.IsTrading && player.CurrentCoinPrice > player.SellAtPrice && player.isSellAllowed)
            {
                logger.Info(player.Name + " Marked price to sell " + player.SellAtPrice.Deci().Rnd(7) + " reached for " + player.Pair + " .Selling");
                player.ForceSell = true;
            }

            if (player.SellAtPrice != null && player.IsTrading && player.CurrentCoinPrice < player.SellAtPrice && player.isSellAllowed)
            {
                logger.Info(player.Name + " Marked price to sell " + player.SellAtPrice.Deci().Rnd(7) + " not yet reached for " + player.Pair + " Cant sell now. Current Price is " + player.CurrentCoinPrice.Rnd(7));
                player.ForceSell = false;
            }


            #endregion

            #region Price Difference Less Than Sell Above

            if (prDiffPerc <= player.SellAbovePerc && player.ForceSell == false)
            {
                if (DateTime.Now.Minute == configr.ReduceSellAboveAtMinute &&
                   (DateTime.Now.Second >= configr.ReduceSellAboveFromSecond &&
                    DateTime.Now.Second <= configr.ReduceSellAboveToSecond))
                {
                    if (player.SellAbovePerc >= configr.MinSellAbovePerc)
                    {
                        if (configr.IsReducingSellAbvAllowed)
                            player.SellAbovePerc = player.SellAbovePerc - configr.ReduceSellAboveBy;
                    }
                }

                player.SellBelowPerc = player.SellAbovePerc;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            #endregion Price Difference Less Than Sell Above

            #region Price Difference Greater Than Sell Above And Greater Than Sell below

            if (prDiffPerc >= player.SellBelowPerc && player.ForceSell == false)
            {
                if (prDiffPerc > player.LastRoundProfitPerc && NextSellbelow > player.SellBelowPerc)
                    player.SellBelowPerc = NextSellbelow;
                player.LastRoundProfitPerc = prDiffPerc;
                player.AvailableAmountToBuy = 0;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            #endregion Price Difference Greater Than Sell Above And Greater Than Sell below

            #region Sell Not allowed

            if ((player.isSellAllowed == false || configr.IsSellingAllowed == false) && player.ForceSell == false)
            {
                player.AvailableAmountToBuy = 0;
                player.LastRoundProfitPerc = prDiffPerc;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            #endregion Sell Not allowed 

            #region Price Went Below Last Profitable Round - Sell

            if (prDiffPerc < player.LastRoundProfitPerc || player.ForceSell == true)
            {
                if (sig != null)
                {
                    logger.Info("  " +
                              sig.OpenTime.ToString("dd-MMM HH:mm") +
                            " " + player.Name +
                           " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                            " < LastRoundProfitPerc  " + player.LastRoundProfitPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                            " selling ");
                }

                var PriceChangeResponse = await client.GetDailyTicker(pair);

                var orderbook = await client.GetOrderBook(player.Pair, false, 8);

                ////foreach (var bid in orderbook.Bids)
                ////{
                ////    logger.Info(player.Pair + " Price " + bid.Price + " Qty " + bid.Quantity);
                ////}
                ////logger.Info(player.Pair + " Maximum bid is " + orderbook.Bids.Max(x => x.Price));
                mysellPrice = orderbook.Bids.Max(x => x.Price);

                //  mysellPrice = PriceChangeResponse.LastPrice;

                player.DayHigh = PriceChangeResponse.HighPrice;
                player.DayLow = PriceChangeResponse.LowPrice;
                player.UpdatedTime = DateTime.Now;
                player.SellCoinPrice = mysellPrice;
                player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
                player.TotalSellAmount = mysellPrice * player.Quantity - player.SellCommision;
                player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
                player.CurrentCoinPrice = mysellPrice;
                player.TotalCurrentValue = player.TotalSellAmount;

                var coinprecison = myCoins.Where(x => x.Pair == pair).FirstOrDefault().TradePrecision;

                BaseCreateOrderResponse sellOrder = null;

                if (configr.CrashSell == true || player.ForceSell == true)
                {


                    Quantityvalue = player.Quantity.Deci().ToString();

                    if (Quantityvalue.Contains("."))
                    {
                        predecimal = Quantityvalue.Substring(0, Quantityvalue.IndexOf('.'));
                        decimals = Quantityvalue.Substring(Quantityvalue.IndexOf('.'));
                        subdecimals = decimals.Substring(0, coinprecison + 1);
                        Quantityvalue = predecimal + subdecimals;
                    }


                    logger.Info("Selling " + player.Pair + " Qty " + player.Quantity.Deci().Rnd(coinprecison));

                    //sellOrder = await client.CreateOrder(new CreateOrderRequest()
                    //{
                    //    // Price = mysellPrice,
                    //    Quantity = Convert.ToDecimal(Quantityvalue),
                    //    //  Quantity = player.Quantity.Deci().Rnd(coinprecison),
                    //    Side = OrderSide.Sell,
                    //    Symbol = player.Pair,
                    //    Type = OrderType.Market
                    //});

                    sellOrder = await client.CreateOrder(new CreateOrderRequest()
                    {
                        Price = mysellPrice,
                        Quantity = player.Quantity.Deci().Rnd(coinprecison),
                        Side = OrderSide.Sell,
                        Symbol = player.Pair,
                        Type = OrderType.Limit,
                        TimeInForce = TimeInForce.GTC
                    });

                }
                else
                {
                    sellOrder = await client.CreateOrder(new CreateOrderRequest()
                    {
                        Price = mysellPrice,
                        Quantity = player.Quantity.Deci().Rnd(coinprecison),
                        Side = OrderSide.Sell,
                        Symbol = player.Pair,
                        Type = OrderType.Limit,
                        TimeInForce = TimeInForce.GTC
                    });
                }
                player.SellOrderId = sellOrder.OrderId;
                player.SellTime = DateTime.Now;
                player.AvailableAmountToBuy = player.TotalSellAmount;
            }

            #endregion Price Went Below Last Profitable Round - Sell

            #region Price going Above Last Profitable Round - Dont Sell

            else
            {
                player.AvailableAmountToBuy = 0;
            }

            #endregion  Price going Above Last Profitable Round - Dont Sell

            #region final selling Set up

            player.LastRoundProfitPerc = prDiffPerc;
            player.ForceSell = false;
            db.Player.Update(player);
            await db.SaveChangesAsync();

            #endregion final selling Set up
        }

        private async Task CheckCrashToSellAll()
        {

            decimal buycost = 0;
            decimal currentvalue = 0;
            using (var db = new DB())
            {
                var players = await db.Player.Where(x => x.IsTrading == true && x.SellOrderId <= 0).ToListAsync();

                foreach (var player in players)
                {
                    buycost = player.TotalBuyCost.Deci();
                    currentvalue = player.TotalSellAmount.Deci();

                    var prDi = currentvalue.GetDiffPercBetnNewAndOld(buycost);

                    if (prDi <= configr.SellWhenAllBotsAtLossBelow && player.isSellAllowed)
                    {
                        player.ForceSell = true;
                        db.Player.Update(player);
                    }

                }

                await db.SaveChangesAsync();
            }

            //if (configr.ShouldSellWhenAllBotsAtLoss == true)
            //{
            //    bool allPlayersAtLoss = true;

            //    using (var db = new DB())
            //    {
            //        var players = await db.Player.Where(x => x.IsTrading == true && x.SellOrderId <= 0).ToListAsync();

            //        foreach (var player in players)
            //        {
            //            var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
            //            if (prDiffPerc > configr.SellWhenAllBotsAtLossBelow)
            //            {
            //                allPlayersAtLoss = false;
            //                break;
            //            }
            //        }

            //        if (allPlayersAtLoss == false)
            //        {
            //            decimal totalbuycost = 0;
            //            decimal totalcurrentvalue = 0;
            //            foreach (var player in players)
            //            {
            //                totalbuycost += player.TotalBuyCost.Deci();
            //                totalcurrentvalue += player.TotalSellAmount.Deci();

            //            }

            //            var prDiff = totalcurrentvalue.GetDiffPercBetnNewAndOld(totalbuycost);
            //            if (prDiff <= configr.SellWhenAllBotsAtLossBelow)
            //            {
            //                allPlayersAtLoss = true;
            //            }
            //        }

            //        if (allPlayersAtLoss == true)
            //        {
            //            foreach (var player in players)
            //            {
            //                player.ForceSell = true;
            //                db.Player.Update(player);
            //            }
            //            configr.CrashSell = true;
            //            configr.IsBuyingAllowed = false;
            //            db.Config.Update(configr);
            //            await db.SaveChangesAsync();
            //        }



            //        await db.SaveChangesAsync();

            //    }
            //}
        }

        private bool ShouldReturnFromSelling(Signal sig, Player player)
        {
            DB db = new DB();

            if (sig == null) return true;

            if (player == null)
            {
                logger.Info("Player returned as null. Some issue. Returning from Sell");
                return true;
            }

            var pair = player.Pair;

            if (pair == null)
            {
                logger.Info("Player's Pair to sell returned as null. Some issue. Returning from Sell");
                return true;
            }

            var newPlayer = db.Player.AsNoTracking().Where(x => x.Name == player.Name).FirstOrDefault();

            if (newPlayer.IsTrading == false) return true;

            if (newPlayer.SellOrderId > 0)
            {
                logger.Info("Sell order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
                return true;
            }
            if (newPlayer.isBuyOrderCompleted == false)
            {
                logger.Info("Buy order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
                return true;
            }
            var availableQty = GetAvailQty(player, pair);

            if (availableQty <= 0)
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ') + " Available Quantity 0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return true;
            }

            if (player.Quantity == null || player.Quantity.Deci() == 0)
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ')
                + " player.Quantity  0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return true;
            }

            return false;
        }

        private async void SellThisBot(object sender, RoutedEventArgs e)
        {
            DB db = new DB();
            PlayerViewModel model = (sender as Button).DataContext as PlayerViewModel;
            Player player = await db.Player.Where(x => x.Name == model.Name).FirstOrDefaultAsync();
            player.ForceSell = true;
            await Sell(player);
            // await SetGrid();
        }

        public async Task RedistributeBalances()
        {
            DB db = new DB();
            var players = await db.Player.AsNoTracking().ToListAsync();

            //decimal TotalAmount=0;

            //foreach (var player in players)
            //{
            //    TotalAmount = TotalAmount + player.TotalCurrentValue.Deci() + player.AvailableAmountToBuy.Deci();
            //}

            //decimal OvearallAverageAmount = TotalAmount / players.Count();

            var availplayers = await db.Player.Where(x => x.IsTrading == false).OrderBy(x => x.Id).ToListAsync();

            AccountInformationResponse accinfo = await client.GetAccountInformation();

            decimal TotalAvalUSDT = 0;

            var USDT = accinfo.Balances.Where(x => x.Asset == "USDT").FirstOrDefault();

            if (USDT != null)
            {
                TotalAvalUSDT = USDT.Free - (USDT.Free * 0.5M / 100); //Take only 98 % to cater for small differences
            }
            if (availplayers.Count() > 0)
            {
                var avgAvailAmountForTrading = TotalAvalUSDT / availplayers.Count();

                if (avgAvailAmountForTrading > configr.MaximumAmountForaBot)
                {
                    avgAvailAmountForTrading = configr.MaximumAmountForaBot;
                }

                foreach (var player in availplayers)
                {
                    player.AvailableAmountToBuy = avgAvailAmountForTrading;
                    player.TotalCurrentValue = 0;
                    db.Player.Update(player);
                }
                await db.SaveChangesAsync();
            }
        }

        public async Task UpdateActivePlayerStats(Player player)
        {
            DB db = new DB();

            var playerSignal = MySignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();

            if (playerSignal != null)
            {
                player.DayHigh = playerSignal.DayHighPr;
                player.DayLow = playerSignal.DayLowPr;
                player.CurrentCoinPrice = playerSignal.CurrPr;
                player.TotalCurrentValue = player.CurrentCoinPrice * player.Quantity;
                player.TotalSellAmount = player.TotalCurrentValue;
                player.ProfitLossAmt = (player.TotalCurrentValue - player.TotalBuyCost).Deci();
                var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
                player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";
                if (player.ProfitLossChanges.Length > 200)
                    player.ProfitLossChanges = player.ProfitLossChanges.GetLast(200);

                player.AvailableAmountToBuy = 0;
                player.UpdatedTime = DateTime.Now;
                db.Player.Update(player);
                await db.SaveChangesAsync();
            }
        }

        private async Task UpdateTradeBuyDetails()
        {
            DB db = new DB();

            var players = await db.Player.Where(x => x.IsTrading == true).ToListAsync();
            var bnbprice = await client.GetPrice("BNBUSDT");

            foreach (var player in players)
            {
                if (!player.isBuyOrderCompleted)
                {
                    QueryOrderRequest request = new QueryOrderRequest();
                    request.Symbol = player.Pair;
                    request.OrderId = player.BuyOrderId;

                    var orderResponse = await client.QueryOrder(request);

                    if (orderResponse != null)
                    {

                        if (orderResponse.Status == OrderStatus.PartiallyFilled)
                        {
                            //dont cancel.Wait indefinitely till filled
                        }

                        else if (orderResponse.Status != OrderStatus.Filled)
                        {
                            if (player.RepsTillCancelOrder > configr.MaxRepsBeforeCancelOrder)
                            {
                                CancelOrderRequest cancelrequest = new CancelOrderRequest();
                                cancelrequest.OrderId = orderResponse.OrderId;
                                cancelrequest.Symbol = orderResponse.Symbol;
                                await client.CancelOrder(cancelrequest);
                                await ClearPlayer(player);
                            }
                            else
                            {
                                player.RepsTillCancelOrder = player.RepsTillCancelOrder + 1;
                                //player.Quantity=await GetAvailQty(player,player.Pair);
                                //player.TotalBuyCost=player.Quantity*player.BuyCoinPrice+player.BuyCommision;
                                db.Player.Update(player);
                                await db.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            player.isBuyOrderCompleted = true;
                            player.ForceSell = false;
                            db.Player.Update(player);
                            await db.SaveChangesAsync();
                        }
                    }

                }
            }


        }

        private async Task UpdateTradeSellDetails()
        {

            DB db = new DB();

            var players = await db.Player.Where(x => x.IsTrading == true).ToListAsync();
            var bnbprice = await client.GetPrice("BNBUSDT");

            foreach (var player in players)
            {
                if (player.SellOrderId > 0)
                {
                    QueryOrderRequest request = new QueryOrderRequest();
                    request.Symbol = player.Pair;
                    request.OrderId = player.SellOrderId;

                    var orderResponse = await client.QueryOrder(request);

                    if (orderResponse != null)
                    {
                        if (orderResponse.Status == OrderStatus.PartiallyFilled)
                        {
                            //dont cancel.Wait indefinitely till filled
                        }
                        else if (orderResponse.Status != OrderStatus.Filled)
                        {
                            if (player.RepsTillCancelOrder > configr.MaxRepsBeforeCancelOrder)
                            {
                                CancelOrderRequest cancelrequest = new CancelOrderRequest();
                                cancelrequest.OrderId = orderResponse.OrderId;
                                cancelrequest.Symbol = orderResponse.Symbol;
                                await client.CancelOrder(cancelrequest);
                                player.SellOrderId = 0;
                                player.RepsTillCancelOrder = 0;
                                db.Player.Update(player);
                                await db.SaveChangesAsync();
                            }
                            else
                            {
                                player.RepsTillCancelOrder = player.RepsTillCancelOrder + 1;
                                db.Player.Update(player);
                                await db.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            await UpdatePlayerAfterSellConfirmed(player);
                        }
                    }

                }
            }

        }

        private async Task UpdatePlayerAfterSellConfirmed(Player player)
        {
            DB db = new DB();

            player.AvailableAmountToBuy = player.TotalSellAmount;

            var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);

            if (prDiffPerc <= 0)
            {
                player.BuyOrSell = "Loss";
            }
            else
            {
                player.BuyOrSell = "Profit";
            }

            PlayerTrades PlayerTrades = iPlayerMapper.Map<Player, PlayerTrades>(player);
            PlayerTrades.Id = 0;
            await db.PlayerTrades.AddAsync(PlayerTrades);
            player.ForceSell = false;
            player.LastRoundProfitPerc = 0;
            player.DayHigh = 0.0M;
            player.DayLow = 0.0M;
            player.Pair = null;
            player.BuyCoinPrice = 0.0M;
            player.CurrentCoinPrice = 0.0M;
            player.Quantity = 0.0M;
            player.TotalBuyCost = 0.0M;
            player.TotalCurrentValue = 0.0M;
            player.TotalSellAmount = 0.0M;
            player.BuyTime = null;
            player.SellTime = null;
            player.BuyCommision = 0.0M;
            player.SellCoinPrice = 0.0M;
            player.SellCommision = 0.0M;
            player.SellBelowPerc = player.SellAbovePerc;
            player.IsTrading = false;
            player.BuyOrSell = string.Empty;
            player.ProfitLossAmt = 0;
            player.ProfitLossChanges = string.Empty;
            player.BuyOrderId = 0;
            player.SellOrderId = 0;
            player.HardSellPerc = 0;
            player.isBuyOrderCompleted = false;
            player.RepsTillCancelOrder = 0;
            player.SellAbovePerc = configr.DefaultSellAbovePerc;
            player.SellBelowPerc = configr.DefaultSellAbovePerc;
            player.isSellAllowed = false;
            player.SellAtPrice = null;
            player.BuyAtPrice = null;

            db.Player.Update(player);
            await db.SaveChangesAsync();

            await RedistributeBalances();


        }

        public decimal? GetAvailQty(Player player, string pair)
        {
            decimal? availableQty = player.Quantity.Deci();

            //var coin = pair.Replace("USDT", "");

            //AccountInformationResponse accinfo = await client.GetAccountInformation();

            //var coinAvailable = accinfo.Balances.Where(x => x.Asset == coin).FirstOrDefault();


            //if (coinAvailable != null)
            //{
            //    availableQty = coinAvailable.Free;
            //}
            //else
            //{
            //    logger.Info("  " +
            //    StrTradeTime +
            //    " " + player.Name +
            //         " " + pair.Replace("USDT", "").PadRight(7, ' ') +
            //    " not available in Binance. its unusal, so wont execute sell order. Check it out");
            //    availableQty = 0;
            //}

            return availableQty;
        }

        private async Task GetMyCoins()
        {
            using (var db = new DB())
            {
                myCoins = await db.MyCoins.AsNoTracking().Where(x => x.IsIncludedForTrading == true).ToListAsync();

                //  myCoins = await db.MyCoins.AsNoTracking().Where(x => x.Pair == "REPUSDT").ToListAsync();
            }
        }

        private async Task UpdateCoins()
        {

            var binanceCoinData = await client.GetProducts();

            List<CoinData> coinDataList = new List<CoinData>();

            foreach (var data in binanceCoinData.Data)
            {
                if (data.s.EndsWith("USDT"))
                {
                    if (data.s.EndsWith("UPUSDT") || data.s.EndsWith("DOWNUSDT") ||
                        data.s.EndsWith("BULLUSDT") || data.s.EndsWith("BEARUSDT") ||
                        data.s == "BUSDUSDT" || data.s == "USDCUSDT" ||
                        data.s == "EURUSDT" || data.s == "DAIUSDT"
                        )
                    {
                        continue;
                    }

                    CoinData coinData = new CoinData();
                    coinData.pair = data.s;
                    coinData.coinSymbol = data.b;
                    coinData.precision = data.i.Deci();
                    coinData.coinName = data.an;
                    coinData.openprice = data.o.Deci();
                    coinData.dayhigh = data.h.Deci();
                    coinData.daylow = data.l.Deci();
                    coinData.currentprice = data.c.Deci();
                    coinData.volume = data.v.Deci();
                    coinData.USDTVolume = data.qv.Deci();
                    coinData.totalCoinsInStorage = data.cs.Deci();
                    coinData.MarketCap = coinData.totalCoinsInStorage * coinData.currentprice;
                    coinDataList.Add(coinData);
                }
            }


            using (var db = new DB())
            {
                List<string> coins = db.MyCoins.Select(x => x.Pair).ToList();
                int i = 0;
                foreach (var coindata in coinDataList.OrderByDescending(x => x.USDTVolume))
                {
                    try
                    {
                        i++;
                        if (!coins.Contains(coindata.pair))
                        {
                            MyCoins coin = new MyCoins();
                            coin.Pair = coindata.pair;
                            coin.IsIncludedForTrading = false;
                            coin.TradePrecision = coindata.precision.GetAllowedPrecision();
                            coin.PercAboveDayLowToSell = 13;
                            coin.PercBelowDayHighToBuy = -13;
                            coin.CoinName = coindata.coinName;
                            coin.CoinSymbol = coindata.coinSymbol;
                            coin.Rank = i;
                            coin.DayTradeCount = coindata.volume;
                            coin.DayVolume = coindata.volume;
                            coin.DayVolumeUSDT = coindata.USDTVolume;
                            coin.DayOpenPrice = coindata.openprice;
                            coin.DayHighPrice = coindata.dayhigh;
                            coin.DayLowPrice = coindata.daylow;
                            coin.CurrentPrice = coindata.currentprice;
                            coin.DayPriceDiff = coindata.currentprice.GetDiffPercBetnNewAndOld(coindata.openprice);
                            coin.FiveMinChange = 0M;
                            coin.TenMinChange = 0M;
                            coin.FifteenMinChange = 0M;
                            coin.ThirtyMinChange = 0M;
                            coin.OneHourChange = 0M;
                            coin.FourHourChange = 0M;
                            coin.TwentyFourHourChange = 0M;
                            coin.FortyEightHourChange = 0M;
                            coin.OneWeekChange = 0M;
                            coin.PrecisionDecimals = coindata.precision;
                            coin.MarketCap = coindata.MarketCap;
                            coin.TradeSuggestion = String.Empty;
                            
                            CreateSignals();

                            if (MySignals.Any(x => x.Symbol == coin.Pair))
                            {
                                for (int j = 0; j < MySignals.Count; j++)
                                {
                                    if (MySignals[j].Symbol == coin.Pair)
                                    {

                                        MySignals[j].CurrPr = coin.CurrentPrice;
                                        MySignals[j].OpenTime = DateTime.Now;
                                        MySignals[j].CloseTime = DateTime.Now;
                                        MySignals[j].DayVol = coin.DayVolume;
                                        MySignals[j].DayTradeCount = coin.DayVolume;

                                    }
                                }
                            }

                            await db.MyCoins.AddAsync(coin);
                        }
                        else
                        {
                            var coin = db.MyCoins.Where(x => x.Pair == coindata.pair).FirstOrDefault();
                            coin.TradePrecision = coindata.precision.GetAllowedPrecision();
                            coin.Rank = i;
                            coin.DayTradeCount = coindata.volume;
                            coin.DayVolume = coindata.volume;
                            coin.DayVolumeUSDT = coindata.USDTVolume;
                            coin.DayOpenPrice = coindata.openprice;
                            coin.DayHighPrice = coindata.dayhigh;
                            coin.DayLowPrice = coindata.daylow;
                            coin.CurrentPrice = coindata.currentprice;
                            coin.DayPriceDiff = coindata.currentprice.GetDiffPercBetnNewAndOld(coindata.openprice);
                            coin.PrecisionDecimals = coindata.precision;
                            coin.MarketCap = coindata.MarketCap;

                            db.MyCoins.Update(coin);

                            if (MySignals.Any(x => x.Symbol == coin.Pair))
                            {
                                for (int j = 0; j < MySignals.Count; j++)
                                {
                                    if (MySignals[j].Symbol == coin.Pair)
                                    {

                                        MySignals[j].CurrPr = coin.CurrentPrice;
                                        MySignals[j].OpenTime = DateTime.Now;
                                        MySignals[j].CloseTime = DateTime.Now;
                                        MySignals[j].DayVol = coin.DayVolume;
                                    }
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Info("Exception while updating coins " + ex.Message);
                    }
                }

                await db.SaveChangesAsync();
            }
        }

        public bool bitCoinStatuslogged = true;

        private void UpdateConfigAfterCrashSell()
        {
            using (var db = new DB())
            {
                if (configr.CrashSell == true)
                {
                    configr.CrashSell = false;
                    db.Config.Update(configr);
                    db.SaveChanges();
                }
            }
        }

        private async Task Trade()
        {
            TradeTime = DateTime.Now;
            StrTradeTime = TradeTime.ToString("dd-MMM HH:mm:ss");
            logger.Info("");
            logger.Info("Trading Started at " + StrTradeTime);
            bitCoinStatuslogged = false;

            if (isControlCurrentlyInTradeMethod) return;
            isControlCurrentlyInTradeMethod = true;

            DB db = new DB();
            await UpdateCoins();

            configr = await db.Config.FirstOrDefaultAsync();
            NextTradeTime = TradeTime.AddSeconds(configr.IntervalMinutes).ToString("dd-MMM HH:mm:ss");
            logger.Info("");
            logger.Info("Collecting signals completed");
            await GetMyCoins();
            CreateSignals();
            ResetSignalsWithSelectedValues();
            CollectReferenceCandlesNew();
            logger.Info("");
            logger.Info("Collecting signals completed");
            logger.Info("");
            foreach (var coin in myCoins.Where(x => x.ForceBuy == true))
            {
                logger.Info(coin.Pair + " Marked to buy in this round");
            }


            await UpdateTradeBuyDetails();
            await UpdateTradeSellDetails();


            #region Buy
            try
            {
                await Buy();
            }
            catch (Exception ex)
            {
                logger.Error("Exception at buy " + ex.Message);
            }
            #endregion Buys

            #region Sell

            try
            {
                var activePlayers = await db.Player.AsNoTracking().OrderBy(x => x.Id).Where(x => x.IsTrading == true).ToListAsync();
                foreach (var player in activePlayers)
                {
                    await Sell(player);
                }

                await CheckCrashToSellAll();
            }
            catch (Exception ex)
            {
                logger.Error("Exception in sell  " + ex.Message);
            }
            finally
            {
                UpdateConfigAfterCrashSell();
            }
            #endregion  Sell

            logger.Info("Trading Completed at  " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));

            isControlCurrentlyInTradeMethod = false;
        }

        #region low priority methods

        private async void TraderTimer_Tick(object sender, EventArgs e)
        {


            try
            {
                await Trade();
            }
            catch (Exception ex)
            {
                isControlCurrentlyInTradeMethod = false;
                logger.Error("Exception at trade " + ex.Message);
            }

        }

        private async void CollectTimer_Tick(object sender, EventArgs e)
        {
            await CollectData();
        }

        private async Task<bool> CollectData()
        {

            await GetMyCoins();

            if (myCoins.Any(x => x.ForceBuy == true)) return false;

            logger.Info("Collecting Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));

            try
            {

                //  CreateBuyLowestSellHighestSignals();
                //  LogInfo();
            }
            catch (Exception ex)
            {
                logger.Error("Exception at CollectTimer_Tick " + ex.Message);
            }

            logger.Info("Collecting Completed at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            return true;
        }

        //private async void CheckSocketsTimer_Tick(object sender, EventArgs e)
        //{
        //    await GetMyCoins();
        //    if (myCoins.Any(x => x.ForceBuy == true)) return;

        //    logger.Info("Checking Sockets and updating Coins Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));

        //    //  await UpdateCoins();

        //    foreach (var sig in MySignals)
        //    {
        //        try
        //        {
        //            if (!socket.IsAlive(sig.TickerSocketGuid))
        //            {
        //                sig.IsSymbolTickerSocketRunning = false;
        //                sig.IsDailyKlineSocketRunning = false;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.Error("Exception at Checking Sockets " + ex.Message);
        //        }
        //    }
        //    logger.Info("Checking Sockets and updating Coins Completed at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
        //}

        private async void btnTrade_Click(object sender, RoutedEventArgs e)
        {
            await Trade();
        }

        private async void btnCollect_Click(object sender, RoutedEventArgs e)
        {
            await CollectData();
        }

        private async Task ClearPlayer(Player player)
        {
            DB db = new DB();
            player.LastRoundProfitPerc = 0;
            player.DayHigh = 0.0M;
            player.DayLow = 0.0M;
            player.Pair = null;
            player.BuyCoinPrice = 0.0M;
            player.CurrentCoinPrice = 0.0M;
            player.Quantity = 0.0M;
            player.TotalBuyCost = 0.0M;
            player.TotalCurrentValue = 0.0M;
            player.TotalSellAmount = 0.0M;
            player.BuyTime = null;
            player.SellTime = null;
            player.BuyCommision = 0.0M;
            player.SellCoinPrice = 0.0M;
            player.SellCommision = 0.0M;
            player.SellBelowPerc = player.SellAbovePerc;
            player.IsTrading = false;
            player.BuyOrSell = string.Empty;
            player.ProfitLossAmt = 0;
            player.ProfitLossChanges = string.Empty;
            player.BuyOrderId = 0;
            player.SellOrderId = 0;
            player.HardSellPerc = 0;
            player.isBuyOrderCompleted = false;
            player.RepsTillCancelOrder = 0;
            db.Player.Update(player);
            await db.SaveChangesAsync();
        }

        public async Task GetAccountInfo()
        {
            AccountInformationResponse accinfo = await client.GetAccountInformation();

            foreach (var balance in accinfo.Balances)
            {
                if (balance.Free > 0)
                    logger.Info(balance.Asset + "," + balance.Free);
            }
        }

        private void ViewCoin(object sender, RoutedEventArgs e)
        {
            try
            {
                PlayerViewModel model = (sender as Button).DataContext as PlayerViewModel;
                var url = model.Pair.GetURL();
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {


            }
        }

        private async void btnClearPlayer_Click(object sender, RoutedEventArgs e)
        {
            await ClearData();

        }

        private async Task ClearData()
        {
            DB TradeDB = new DB();

            await TradeDB.Database.ExecuteSqlRawAsync("TRUNCATE TABLE PlayerTrades");

            var players = await TradeDB.Player.ToListAsync();
            int i = 1;

            foreach (var player in players)
            {
                player.Name = "DIA" + i;
                player.Pair = null;
                player.DayHigh = 0.0M;
                player.DayLow = 0.0M;
                player.BuyCoinPrice = 0.0M;
                player.CurrentCoinPrice = 0.0M;
                player.Quantity = 0.0M;
                player.TotalBuyCost = 0.0M;
                player.TotalCurrentValue = 0.0M;
                player.TotalSellAmount = 0.0M;
                player.BuyTime = null;
                player.SellTime = null;
                player.UpdatedTime = null;
                player.IsTrading = false;
                player.AvailableAmountToBuy = 0.0M;
                player.BuyCommision = 0.0M;
                player.SellCommision = 0.0M;
                player.SellCoinPrice = 0.0M;
                player.BuyOrSell = string.Empty;
                player.BuyOrderId = 0;
                player.SellOrderId = 0;
                player.ProfitLossAmt = 0;
                player.LastRoundProfitPerc = 0;
                player.ProfitLossChanges = string.Empty;
                player.isSellAllowed = false;

                TradeDB.Update(player);
                i++;
            }
            await TradeDB.SaveChangesAsync();
        }

        public void LogNoBuy(Player player, Signal sig)
        {
            //if (configr.ShowDetailedBuyingFlowLogs)
            //{
            //    logger.Info("  " +
            //                sig.OpenTime.ToString("dd-MMM HH:mm") +
            //              " " + player.Name +
            //              " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
            //               " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(11, ' ') +
            //               " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(11, ' ') +
            //              " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(11, ' ') +
            //               " IsDayLow " + sig.IsAtDayLow.ToString().PadRight(11, ' ') +
            //              " IsBestTimeToBuy " + sig.IsBestTimeToBuyAtDayLowest.ToString().PadRight(11, ' ') +
            //              " Not best time to buy ");
            //}
        }

        public void LogBuy(Player player, Signal sig)
        {

            logger.Info("  " +
               sig.OpenTime.ToString("dd-MMM HH:mm") +
             " " + player.Name +
            " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
              " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(11, ' ') +
              " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(11, ' ') +
             " CurCnPr " + sig.CurrPr.Rnd().ToString().PadRight(11, ' ') +
             " Close to day low than high. Buy Now ");

        }

        public void LogSignal(Signal sig)
        {
            string daylow = "";

            if (sig.IsCloseToDayLow)
                daylow = " DayLow : " + sig.IsCloseToDayLow + "".PadRight(5, ' ');
            else
                daylow = " DayLow : " + sig.IsCloseToDayLow + "".PadRight(4, ' ');

            if (sig.IsBestTimeToScalpBuy)
            {
                logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
                   " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                   " CurCnPr " + sig.CurrPr.Rnd().ToString().PadRight(11, ' ') +
                    daylow + " PrDi Cr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(6).ToString().PadRight(11, ' ') +
                    " Pr Ch in Lst Hr " + sig.PrChPercCurrAndRef5Min.Rnd(5).ToString().PadRight(11, ' ') +
                    " buy? : " + sig.IsBestTimeToScalpBuy +
                     " Close to Low? : " + sig.IsCloseToDayLow
                 );
            }

        }

        public void LogDontSellBelowPercReason(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {
            #region log not selling reason

            //logger.Info("  " +
            //ProcessingTimeString +
            //" " + player.Name +
            //" " + pair.Replace("USDT", "").PadRight(7, ' ') +
            //" BuyCnPr " + player.BuyCoinPrice.Deci().Rnd().ToString().PadRight(10, ' ') +
            //" CurCnPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
            //" TotBuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
            //" TotSlPr  " + player.TotalSellAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            //" PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(5) +
            //" > " + player.DontSellBelowPerc.Deci().Rnd(2) +
            //" Not selling ");


            #endregion log not selling reason
        }

        public void LogLossSell(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {
            #region log loss sell

            logger.Info("  " +
            StrTradeTime +
            " " + player.Name +
             " " + pair.Replace("USDT", "").PadRight(7, ' ') +
               " BuyCnPr " + player.BuyCoinPrice.Deci().Rnd().ToString().PadRight(10, ' ') +
               " CurCnPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
               " TotBuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
               " TotSlPr  " + player.TotalSellAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(4).ToString().PadRight(5, ' ') +
            " < " + player.SellBelowPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
            " Loss Sell ");

            // logger.Info("  " + StrTradeTime + " Total Consecutive Loss " + configr.TotalConsecutiveLosses);
            #endregion log loss sell
        }

        public void LogProfitSell(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {

            #region log profit sell

            logger.Info("  " +
            StrTradeTime +
             " " + player.Name +
               " " + pair.Replace("USDT", "").PadRight(7, ' ') +
            " BuyCnPr " + player.BuyCoinPrice.Deci().Rnd().ToString().PadRight(10, ' ') +
            " CurCnPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
            " TotBuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
            " TotSlPr  " + player.TotalSellAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
             " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(4).ToString().PadRight(5, ' ') +
             " > " + player.SellAbovePerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
             " Profit Sell ");
            #endregion log profit sell

        }

        public void LogNoSellReason(Player player, Signal sig, decimal mysellPrice, decimal? prDiffPerc)
        {

            if (configr.ShowSellingFlowLogs)
            {
                logger.Info("  " +
                         StrTradeTime +
                         " " + player.Name +
                           " " + player.Pair.Replace("USDT", "").PadRight(7, ' ') +
                            " BuyCnPr " + player.BuyCoinPrice.Deci().Rnd().ToString().PadRight(10, ' ') +
                            " CurCnPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
                            " TotBuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
                            " TotSlPr  " + player.TotalSellAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
                            " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                            "  > Sell below % " + player.SellBelowPerc.Deci().Rnd(2) +
                            "  At best sell Time? " + sig.IsBestTimeToSellAtDayHighest.ToString() + " Dont Sell ");
            }

        }



        #endregion


    }
}

//private async void ForceBuySellTimer_Tick(object sender, EventArgs e)
//{

//    if (isControlCurrentlyInTradeMethod) return;

//    try
//    {
//        isControlCurrentlyInTradeMethod = true;

//        await GetMyCoins();

//        if (myCoins.Any(x => x.ForceBuy == true))
//        {
//            logger.Info("coins with forcebuy set to true exist. Buying...");
//            await Buy();
//        }
//        else
//        {
//            logger.Info("No coins with forcebuy set to true");
//        }

//        using (var db = new DB())
//        {
//            var players = await db.Player.AsNoTracking().Where(x => x.ForceSell == true).ToListAsync();
//            if (players.Any())
//            {
//                logger.Info("coins with forcesell set to true exist. Selling...");
//                foreach (var player in players)
//                {
//                    await Sell(player);
//                }
//            }
//            else
//            {
//                logger.Info("No players with forcesell set to true");
//            }
//        }
//    }
//    catch (Exception ex)
//    {

//        logger.Error("Exception at force buy and sell " + ex.Message);
//    }
//    finally
//    {
//        isControlCurrentlyInTradeMethod = false;
//    }

//}
