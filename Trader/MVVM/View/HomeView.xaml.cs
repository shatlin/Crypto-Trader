
using AutoMapper;
using BinanceExchange.API.Client;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
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

    public partial class HomeView : UserControl
    {
        public List<MyCoins> myCoins { get; set; }
        public DispatcherTimer TradeTimer;
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
        public bool ForceSell = false;
        InstanceBinanceWebSocketClient socket;
        public ExchangeInfoResponse exchangeInfo = new ExchangeInfoResponse();
        public bool isControlCurrentlyInTradeMethod = false;
        List<Task> tasks = new List<Task>();
        public HomeView()
        {
            InitializeComponent();
            Startup();
        }

        private async void Startup()
        {
            DB db = new DB();
            configr = await db.Config.FirstOrDefaultAsync();
            TradeTimer = new DispatcherTimer();
            TradeTimer.Tick += new EventHandler(TraderTimer_Tick);
            TradeTimer.Interval = new TimeSpan(0, 0, configr.IntervalMinutes);

            lblBotName.Text = configr.Botname;
            logger = LogManager.GetLogger(typeof(MainWindow));

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


            socket = new InstanceBinanceWebSocketClient(client);
            MySignals = new List<Signal>();

            await GetMyCoins();
            await SetGrid();

            if (configr.UpdateCoins)
            {
                await UpdateCoinsForTrading();
                Thread.Sleep(1000);
            }

            await UpdateAllowedPrecisionsForPairs();

            // await RemoveOldCandles();
            // await GetAccountInfo();
            GetSignals();
            TradeTimer.Start();
            logger.Info("Application Ready and Timer Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            logger.Info("");
        }

        private void EnsureAllSocketsRunning()
        {
            Parallel.ForEach(myCoins, coin =>
            {



                foreach (var sig in MySignals)
                {
                    if (sig.Symbol == coin.Coin)
                    {
                        if (sig.IsSymbolTickerSocketRunning == false)
                        {
                            // tasks.Add(Task.Factory.StartNew(() =>
                            // {
                            try
                            {
                                try
                                {
                                    if (sig.TickerSocketGuid != Guid.Empty) socket.CloseWebSocketInstance(sig.TickerSocketGuid);
                                }
                                catch (Exception ex1)
                                {
                                    logger.Info("exception at Ticker socket for " + coin.Coin + "  " + ex1.Message);
                                }


                                sig.TickerSocketGuid = socket.ConnectToIndividualSymbolTickerWebSocket(coin.Coin, b => { sig.CurrPr = b.LastPrice; sig.IsSymbolTickerSocketRunning = true; });

                                // logger.Info("Ticker socket started for " + coin.Coin);
                            }
                            catch (Exception ex)
                            {
                                sig.IsSymbolTickerSocketRunning = false;
                                logger.Info("exception at Ticker socket for " + coin.Coin + "  " + ex.Message);
                            }
                            //  }));
                        }

                        if (sig.IsDailyKlineSocketRunning == false)
                        {
                            // tasks.Add(Task.Factory.StartNew(() =>
                            //{
                            try
                            {
                                try
                                {
                                    if (sig.KlineSocketGuid != Guid.Empty) socket.CloseWebSocketInstance(sig.KlineSocketGuid);
                                }
                                catch (Exception ex1)
                                {

                                    logger.Info("exception at Kline socket for " + coin.Coin + "  " + ex1.Message);
                                }


                                sig.KlineSocketGuid = socket.ConnectToKlineWebSocket(coin.Coin, KlineInterval.OneDay, b =>
                            {
                                sig.OpenTime = b.Kline.StartTime;
                                sig.CloseTime = b.Kline.EndTime;
                                sig.Symbol = b.Symbol;
                                sig.DayHighPr = b.Kline.High;
                                sig.DayLowPr = b.Kline.Low;
                                sig.DayVol = b.Kline.Volume;
                                sig.DayTradeCount = b.Kline.NumberOfTrades;
                                sig.PrDiffHighAndLowPerc = sig.DayHighPr.GetDiffPercBetnNewAndOld(sig.DayLowPr);
                                sig.PrDiffCurrAndLowPerc = sig.CurrPr.GetDiffPercBetnNewAndOld(sig.DayLowPr);
                                sig.PrDiffCurrAndHighPerc = sig.CurrPr.GetDiffPercBetnNewAndOld(sig.DayHighPr);
                                sig.JustRecoveredFromDayLow = sig.PrDiffCurrAndLowPerc >= configr.DayLowGreaterthanTobuy && sig.PrDiffCurrAndLowPerc <= configr.DayLowLessthanTobuy;
                                sig.IsAtDayLow = sig.PrDiffCurrAndLowPerc >= configr.DayLowGreaterthanTobuy && sig.PrDiffCurrAndLowPerc <= configr.DayLowLessthanTobuy;
                                // has gone to the bottom and gone up a bit. Then we know the lowest is reached
                                sig.IsAtDayHigh = sig.PrDiffCurrAndHighPerc <= configr.DayHighLessthanToSell && sig.PrDiffCurrAndHighPerc >= configr.DayHighGreaterthanToSell;
                                // has gone to the top and gone down a little bit. Then we know the higest is reached
                                sig.IsDailyKlineSocketRunning = true;

                                //                        public decimal DayLowGreaterthanTobuy { get; set; }
                                //public decimal DayLowLessthanTobuy { get; set; }
                                //public decimal DayHighLessthanToSell { get; set; }
                                //public decimal DayHighGreaterthanToSell { get; set; }
                            });

                                //  logger.Info("Kline  socket started for " + coin.Coin);

                            }
                            catch (Exception ex)
                            {
                                sig.IsDailyKlineSocketRunning = false;
                                logger.Info("exception at Kline socket for " + coin.Coin + "  " + ex.Message);
                                Thread.Sleep(100);
                            }
                            // }));
                        }
                        break;
                    }
                }
                //  Task.WaitAll(tasks.ToArray());



            });

        }

        private void GetSignals()
        {

            foreach (var coin in myCoins)
            {
                Signal sig = new Signal();
                sig.IsSymbolTickerSocketRunning = false;
                sig.IsDailyKlineSocketRunning = false;
                sig.Symbol = coin.Coin;
                sig.Ref1MinCandles = new List<SignalCandle>();
                sig.Ref5MinCandles = new List<SignalCandle>();
                sig.Ref15MinCandles = new List<SignalCandle>();
                sig.Ref30MinCandles = new List<SignalCandle>();
                sig.Ref1HourCandles = new List<SignalCandle>();
                sig.Ref4HourCandles = new List<SignalCandle>();
                sig.Ref1DayCandles = new List<SignalCandle>();
                MySignals.Add(sig);
            }

            EnsureAllSocketsRunning();

            logger.Info("Get signals completed");
            logger.Info("");
        }

        private void ResetSignalsWithSelectedValues()
        {
            foreach (var coin in myCoins)
            {
                Signal sig = MySignals.Where(x => x.Symbol == coin.Coin).FirstOrDefault();
                if (sig != null)
                {
                    sig.IsIgnored = false;
                    sig.IsPicked = false;
                    sig.CoinId = coin.Id;
                    sig.PercBelowDayHighToBuy = coin.PercBelowDayHighToBuy;
                    sig.PercAboveDayLowToSell = coin.PercAboveDayLowToSell;
                }
            }


        }


        // will have 7 candles. Last week
        // will have 24 candles. Last 24 hours
        // will have 24 candles. Last 2 hours
        // will have 24 candles. Last 6 hours
        // will have 24 candles. Last 12 hours


        private int GetTotalConsecutiveUpOrDown(List<SignalCandle> candleList, string direction)
        {
            // var mintime = candleList.Min(x => x.CloseTime);
            // var avgPriceOfCandles = candleList.Average(x => x.ClosePrice);

            if (candleList == null || candleList.Count == 0) return 0;

            int TotalConsecutiveChanges = 0;

            candleList = candleList.OrderByDescending(x => x.CloseTime).ToList();

            bool directionCondition = false;

            for (int i = 0; i < candleList.Count - 1; i++)
            {
                directionCondition = direction == "up" ?
                    candleList[i].ClosePrice > candleList[i + 1].ClosePrice :
                    candleList[i].ClosePrice <= candleList[i + 1].ClosePrice;

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

                candleList = candleList.OrderByDescending(x => x.CloseTime).ToList();

                if (candleList.Count > count)
                {
                    candleList.RemoveRange(count, candleList.Count - count);
                }

                if (time > DateTime.Now) return candleList;

                var candleQuery= "delete from SignalCandle where CandleType='" + candleType + "' and Pair='" + sig.Symbol + "'";

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
                 
                candleList.Add(new SignalCandle { Pair = sig.Symbol, CloseTime = time, ClosePrice = sig.CurrPr, CandleType = candleType, AddedTime=DateTime.Now });
                
                foreach (SignalCandle cndl in candleList)
                {
                    cndl.Id = Guid.Empty;
                    db.SignalCandle.Add(cndl);
                }
                db.SaveChanges();
            }

            return candleList;
        }

        private decimal GetPriceChangeBetweenCurrentAndReferenceStart(decimal currentPrice, List<SignalCandle> candleList)
        {

            if (candleList == null || candleList.Count == 0)
                return 0M;
            candleList = candleList.OrderBy(x => x.CloseTime).ToList();
            var firstpriceOfCandles = candleList.First().ClosePrice;
            return currentPrice.GetDiffPercBetnNewAndOld(firstpriceOfCandles);
        }

        private void CollectReferenceCandles()
        {

            for (int i = 0; i < MySignals.Count; i++)
            {

                #region day

                if (DateTime.Now.Hour % 23 == 0 && DateTime.Now.Minute % 57 == 0) 
                {
                    UpdateCoinsForTrading();

                    MySignals[i].Ref1DayCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1DayCandles, "day", 7, 23, 57);
                    MySignals[i].TotalConsecutive1DayDowns = GetTotalConsecutiveUpOrDown(MySignals[i].Ref1DayCandles, "down");
                    MySignals[i].TotalConsecutive1DayUps = GetTotalConsecutiveUpOrDown(MySignals[i].Ref1DayCandles, "up");

                    if (MySignals[i].Ref1DayCandles != null && MySignals[i].Ref1DayCandles.Count > 0)
                    {
                        if (MySignals[i].CurrPr <= MySignals[i].Ref1DayCandles.Min(x => x.ClosePrice))
                        {
                            MySignals[i].TotalConsecutive1DayDowns++;
                            if (MySignals[i].TotalConsecutive1DayUps > 0)
                                MySignals[i].TotalConsecutive1DayUps--;
                        }
                        else
                        {

                            MySignals[i].TotalConsecutive1DayUps++;
                            if (MySignals[i].TotalConsecutive1DayDowns > 0)
                                MySignals[i].TotalConsecutive1DayDowns--;
                        }
                    }
                    //   MySignals[i].IsBestTimeToScalpBuy = MySignals[i].RefDayCandles.Count >= 7;
                    MySignals[i].PrChPercCurrAndRef1Day = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref1DayCandles);
                }
                #endregion day


                #region 4hour
                if ((DateTime.Now.Hour == 3 || DateTime.Now.Hour == 7 || DateTime.Now.Hour == 11 || DateTime.Now.Hour == 15 ||
                    DateTime.Now.Hour == 19 || DateTime.Now.Hour == 23)
                    && DateTime.Now.Minute % 57 == 0) 
                {
                    MySignals[i].Ref4HourCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref4HourCandles, "4hour", 18, DateTime.Now.Hour, 57);
                    MySignals[i].TotalConsecutive4HourDowns = GetTotalConsecutiveUpOrDown(MySignals[i].Ref4HourCandles, "down");
                    MySignals[i].TotalConsecutive4HourUps = GetTotalConsecutiveUpOrDown(MySignals[i].Ref4HourCandles, "up");

                    if (MySignals[i].Ref4HourCandles != null && MySignals[i].Ref4HourCandles.Count > 0)
                    {
                        if (MySignals[i].CurrPr <= MySignals[i].Ref4HourCandles.Min(x => x.ClosePrice))
                        {

                            MySignals[i].TotalConsecutive4HourDowns++;

                            if (MySignals[i].TotalConsecutive4HourUps > 0)
                                MySignals[i].TotalConsecutive4HourUps--;
                        }
                        else
                        {
                            MySignals[i].TotalConsecutive4HourUps++;

                            if (MySignals[i].TotalConsecutive4HourDowns > 0)
                                MySignals[i].TotalConsecutive4HourDowns--;
                        }
                    }

                    //  MySignals[i].IsBestTimeToScalpBuy = MySignals[i].Ref4HourCandles.Count >= 6;
                    MySignals[i].PrChPercCurrAndRef4Hour = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref4HourCandles);
                }
                #endregion 4hour


                #region 1hour

                if (DateTime.Now.Minute % 57 == 0) 
                {
                    MySignals[i].Ref1HourCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1HourCandles, "1hour", 24, DateTime.Now.Hour, 57);
                    MySignals[i].TotalConsecutive1HourDowns = GetTotalConsecutiveUpOrDown(MySignals[i].Ref1HourCandles, "down");
                    MySignals[i].TotalConsecutive1HourUps = GetTotalConsecutiveUpOrDown(MySignals[i].Ref1HourCandles, "up");

                    if (MySignals[i].Ref1HourCandles != null && MySignals[i].Ref1HourCandles.Count > 0)
                    {
                        if (MySignals[i].CurrPr <= MySignals[i].Ref1HourCandles.Min(x => x.ClosePrice))
                        {

                            MySignals[i].TotalConsecutive1HourDowns++;

                            if (MySignals[i].TotalConsecutive1HourUps > 0)
                                MySignals[i].TotalConsecutive1HourUps--;
                        }
                        else
                        {
                            MySignals[i].TotalConsecutive1HourUps++;

                            if (MySignals[i].TotalConsecutive1HourDowns > 0)
                                MySignals[i].TotalConsecutive1HourDowns--;
                        }
                    }

                  //  MySignals[i].IsBestTimeToScalpBuy = MySignals[i].Ref1HourCandles.Count >= 12;
                    MySignals[i].PrChPercCurrAndRef1Hour = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref1HourCandles);
                }

                #endregion 1hour


                #region 30minute

                if (DateTime.Now.Minute % 30 == 0)
                {
                    MySignals[i].Ref30MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref30MinCandles, "30min", 48,DateTime.Now.Hour, DateTime.Now.Minute);
                    MySignals[i].TotalConsecutive30MinDowns = GetTotalConsecutiveUpOrDown(MySignals[i].Ref30MinCandles, "down");
                    MySignals[i].TotalConsecutive30MinUps = GetTotalConsecutiveUpOrDown(MySignals[i].Ref30MinCandles, "up");

                    if (MySignals[i].Ref30MinCandles != null && MySignals[i].Ref30MinCandles.Count > 0)
                    {
                        if (MySignals[i].CurrPr <= MySignals[i].Ref30MinCandles.Min(x => x.ClosePrice))
                        {
                            MySignals[i].TotalConsecutive30MinDowns++;
                            if (MySignals[i].TotalConsecutive30MinUps > 0)
                                MySignals[i].TotalConsecutive30MinUps--;
                        }
                        else
                        {
                            MySignals[i].TotalConsecutive30MinUps++;
                            if (MySignals[i].TotalConsecutive30MinDowns > 0)
                                MySignals[i].TotalConsecutive30MinDowns--;
                        }
                    }

                   // MySignals[i].IsBestTimeToScalpBuy = MySignals[i].Ref30MinCandles.Count >= 12;
                    MySignals[i].PrChPercCurrAndRef30Min = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref30MinCandles);
                }

                #endregion 30minute

                #region 15minute

                if (DateTime.Now.Minute % 15 == 0)
                {
                    MySignals[i].Ref15MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref15MinCandles, "15min", 96, DateTime.Now.Hour, DateTime.Now.Minute);
                    MySignals[i].TotalConsecutive15MinDowns = GetTotalConsecutiveUpOrDown(MySignals[i].Ref15MinCandles, "down");
                    MySignals[i].TotalConsecutive15MinUps = GetTotalConsecutiveUpOrDown(MySignals[i].Ref15MinCandles, "up");

                    if (MySignals[i].Ref15MinCandles != null && MySignals[i].Ref15MinCandles.Count > 0)
                    {
                        if (MySignals[i].CurrPr <= MySignals[i].Ref15MinCandles.Min(x => x.ClosePrice))
                        {
                            MySignals[i].TotalConsecutive15MinDowns++;
                            if (MySignals[i].TotalConsecutive15MinUps > 0)
                                MySignals[i].TotalConsecutive15MinUps--;
                        }
                        else
                        {
                            MySignals[i].TotalConsecutive15MinUps++;
                            if (MySignals[i].TotalConsecutive15MinDowns > 0)
                                MySignals[i].TotalConsecutive15MinDowns--;
                        }
                    }
                  //  MySignals[i].IsBestTimeToScalpBuy = MySignals[i].Ref15MinCandles.Count >= 12;
                    MySignals[i].PrChPercCurrAndRef15Min = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref15MinCandles);
                }
                #endregion 15minute

                #region 5minute
                if (DateTime.Now.Minute % 5 == 0)
                {
                    MySignals[i].Ref5MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref5MinCandles, "5min", 288, DateTime.Now.Hour, DateTime.Now.Minute);
                    MySignals[i].TotalConsecutive5MinDowns = GetTotalConsecutiveUpOrDown(MySignals[i].Ref5MinCandles, "down");
                    MySignals[i].TotalConsecutive5MinUps = GetTotalConsecutiveUpOrDown(MySignals[i].Ref5MinCandles, "up");

                    if (MySignals[i].Ref5MinCandles != null && MySignals[i].Ref5MinCandles.Count > 0)
                    {
                        if (MySignals[i].CurrPr <= MySignals[i].Ref5MinCandles.Min(x => x.ClosePrice))
                        {
                            MySignals[i].TotalConsecutive5MinDowns++;
                            if (MySignals[i].TotalConsecutive5MinUps > 0)
                                MySignals[i].TotalConsecutive5MinUps--;
                        }
                        else
                        {
                            MySignals[i].TotalConsecutive5MinUps++;
                            if (MySignals[i].TotalConsecutive5MinDowns > 0)
                                MySignals[i].TotalConsecutive5MinDowns--;
                        }
                    }

                   
                    MySignals[i].PrChPercCurrAndRef5Min = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref5MinCandles);

                }
                #endregion 5minute

                #region 1minute

                if (DateTime.Now.Minute % 1 == 0)
                {
                    MySignals[i].Ref1MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1MinCandles, "1min", 60, DateTime.Now.Hour, DateTime.Now.Minute);
                    MySignals[i].TotalConsecutive1MinDowns = GetTotalConsecutiveUpOrDown(MySignals[i].Ref1MinCandles, "down");
                    MySignals[i].TotalConsecutive1MinUps = GetTotalConsecutiveUpOrDown(MySignals[i].Ref1MinCandles, "up");

                    if (MySignals[i].Ref1MinCandles != null && MySignals[i].Ref1MinCandles.Count > 0)
                    {
                        if (MySignals[i].CurrPr <= MySignals[i].Ref1MinCandles.Min(x => x.ClosePrice))
                        {
                            MySignals[i].TotalConsecutive1MinDowns++;
                            if (MySignals[i].TotalConsecutive1MinUps > 0)
                                MySignals[i].TotalConsecutive1MinUps--;
                        }
                        else
                        {
                            MySignals[i].TotalConsecutive1MinUps++;
                            if (MySignals[i].TotalConsecutive1MinDowns > 0)
                                MySignals[i].TotalConsecutive1MinDowns--;
                        }
                    }

                  // MySignals[i].IsBestTimeToScalpBuy = MySignals[i].Ref1MinCandles.Count >= 30;
                    MySignals[i].PrChPercCurrAndRef1Min = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref1MinCandles);
                }
                #endregion 1minute

            }
        }

        private void CreateBuyLowestSellHighestSignals()
        {
            foreach (var sig in MySignals)
            {
                sig.IsBestTimeToBuyAtDayLowest = sig.CurrPr > 0 && sig.PrDiffCurrAndHighPerc < sig.PercBelowDayHighToBuy && sig.PrDiffHighAndLowPerc > sig.PercAboveDayLowToSell && sig.IsAtDayLow;

                sig.IsBestTimeToSellAtDayHighest = sig.CurrPr > 0 && sig.PrDiffHighAndLowPerc > sig.PercAboveDayLowToSell && sig.IsAtDayHigh;
            }
        }

        private void CreateScalpBuySignals()
        {

            foreach (var sig in MySignals)
            {
                var dayAveragePrice = (sig.DayHighPr + sig.DayLowPr) / 2;
                dayAveragePrice = dayAveragePrice - (dayAveragePrice * 2M / 100);
                sig.IsCloseToDayLow = sig.CurrPr < dayAveragePrice;
                sig.DayAveragePr = (sig.DayHighPr + sig.DayLowPr) / 2;

                if (sig.IsBestTimeToBuyAtDayLowest)
                {
                    sig.IsBestTimeToScalpBuy = true;
                    continue;
                }

                if (sig.CurrPr <= 0)
                {
                    sig.IsBestTimeToScalpBuy = false;
                    continue;
                }

                if (sig.PrDiffCurrAndHighPerc >= -2M)
                {
                    sig.IsBestTimeToScalpBuy = false;
                    continue;
                }

                if (sig.PrDiffHighAndLowPerc <= 3M)
                {
                    sig.IsBestTimeToScalpBuy = false;
                    continue;
                }

                if (sig.CurrPr >= ((sig.DayHighPr + sig.DayAveragePr) / configr.DivideHighAndAverageBy))
                {
                    sig.IsBestTimeToScalpBuy = false;
                    continue;
                }

                if (sig.TotalConsecutive4HourDowns >= 3)
                {
                    sig.IsBestTimeToScalpBuy = true;
                    continue;
                }

                if (sig.TotalConsecutive1HourDowns >= 3)
                {
                    sig.IsBestTimeToScalpBuy = true;
                    continue;
                }

                if (sig.TotalConsecutive30MinDowns >= 4)
                {
                    sig.IsBestTimeToScalpBuy = true;
                    continue;
                }

                if (sig.TotalConsecutive15MinDowns >= 5)
                {
                    sig.IsBestTimeToScalpBuy = true;
                    continue;
                }

                if (sig.TotalConsecutive5MinDowns >= 6)
                {
                    sig.IsBestTimeToScalpBuy = true;
                    continue;
                }

                sig.IsBestTimeToScalpBuy = false;
            }
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
                             " " + sig.CoinId.ToString().PadRight(3, ' ') +
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
                             " " + sig.CoinId.ToString().PadRight(3, ' ') +
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

                foreach (var sig in MySignals.OrderByDescending(x => x.TotalConsecutive4HourDowns))
                {
                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(8, ' ') +
                                 " 4Hr Dns " + sig.TotalConsecutive4HourDowns.ToString().PadRight(5, ' ') +
                                 " Hr Dns " + sig.TotalConsecutive1HourDowns.ToString().PadRight(5, ' ') +
                                 " 30 Dns " + sig.TotalConsecutive30MinDowns.ToString().PadRight(5, ' ') +
                                 " 15 Dns " + sig.TotalConsecutive15MinDowns.ToString().PadRight(5, ' ') +
                                 " 5 Dns " + sig.TotalConsecutive5MinDowns.ToString().PadRight(5, ' ') +
                                 " 1 Dns " + sig.TotalConsecutive1MinDowns.ToString().PadRight(5, ' ');

                    if (sig.IsBestTimeToScalpBuy)
                        logger.Info(log);
                }
            }

            if (configr.ShowNoScalpBuyLogs)
            {
                logger.Info("");
                logger.Info("Not Scalp Buyables");
                logger.Info("----------------");
                foreach (var sig in MySignals)
                {
                    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(8, ' ') +
                                         " 5dn " + sig.TotalConsecutive5MinDowns.ToString().PadRight(3, ' ') +
                                         " 5up " + sig.TotalConsecutive5MinUps.ToString().PadRight(3, ' ') +
                                         " Ch " + sig.PrChPercCurrAndRef5Min.Rnd(3).ToString().PadRight(12, ' ') +
                                         " 15dn " + sig.TotalConsecutive15MinDowns.ToString().PadRight(3, ' ') +
                                         " 15up " + sig.TotalConsecutive15MinUps.ToString().PadRight(3, ' ') +
                                         " Ch " + sig.PrChPercCurrAndRef15Min.Rnd(3).ToString().PadRight(12, ' ') +
                                         " 30dn " + sig.TotalConsecutive30MinDowns.ToString().PadRight(3, ' ') +
                                         " 30up " + sig.TotalConsecutive30MinUps.ToString().PadRight(3, ' ') +
                                         " Ch " + sig.PrChPercCurrAndRef30Min.Rnd(3).ToString().PadRight(12, ' ') +
                                         " Hrdn " + sig.TotalConsecutive1HourDowns.ToString().PadRight(3, ' ') +
                                         " Hrup " + sig.TotalConsecutive1HourUps.ToString().PadRight(3, ' ') +
                                         " Ch " + sig.PrChPercCurrAndRef1Hour.Rnd(3).ToString().PadRight(12, ' ') +
                                         " Ddn " + sig.TotalConsecutive1DayDowns.ToString().PadRight(3, ' ') +
                                         " Dup " + sig.TotalConsecutive1DayUps.ToString().PadRight(3, ' ') +
                                         " Ch " + sig.PrChPercCurrAndRef1Day.Rnd(3).ToString();
                    if (!sig.IsBestTimeToScalpBuy)
                        logger.Info(log);
                }
            }

            if (configr.ShowSellLogs)
            {
                logger.Info("");
                logger.Info("Sellables");
                logger.Info("---------");


                foreach (var sig in MySignals.OrderBy(x => x.PrDiffCurrAndHighPerc))
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

        private async Task BuyTheCoin(Player playertobuy, Signal sig)
        {

            await RedistributeBalances();


            DB db = new DB();

            var player = await db.Player.Where(x => x.Name == playertobuy.Name).FirstOrDefaultAsync();


            if (player.AvailableAmountToBuy < configr.MinimumAmountToTradeWith)
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " Avl Amt " + player.AvailableAmountToBuy + " Not enough for buying ");
                return;
            }

            var PriceResponse = await client.GetPrice(sig.Symbol);

            decimal mybuyPrice = PriceResponse.Price;

            LogBuy(player, sig);

            player.Pair = sig.Symbol;

            var coin = myCoins.Where(x => x.Coin == sig.Symbol).FirstOrDefault();

            var coinprecison = coin.TradePrecision;

            var quantity = (player.AvailableAmountToBuy.Deci() / mybuyPrice).Rnd(coinprecison);

            var buyOrder = await client.CreateOrder(new CreateOrderRequest()
            {
                Price = mybuyPrice,
                Quantity = quantity,
                Side = OrderSide.Buy,
                Symbol = player.Pair,
                Type = OrderType.Limit,
                TimeInForce = TimeInForce.GTC
            });


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

            //Send Buy Order

            PlayerTrades playerHistory = iPlayerMapper.Map<Player, PlayerTrades>(player);
            playerHistory.Id = 0;
            await db.PlayerTrades.AddAsync(playerHistory);
            await db.SaveChangesAsync();
        }

        private async Task Buy()
        {
            if (MySignals == null || MySignals.Count() == 0)
            {
                logger.Info("  " + StrTradeTime + " no signals found. So cannot proceed for buy process ");
                return;
            }



            DB db = new DB();

            var players = await db.Player.OrderBy(x => x.Id).ToListAsync();
            boughtCoins = await db.Player.Where(x => x.Pair != null).Select(x => x.Pair).ToListAsync();

            foreach (var player in players)
            {
                if (player.IsTrading)
                {
                    await UpdateActivePlayerStats(player);
                    continue;
                }

                if (player.isBuyOrderCompleted) // before buying the buyordercompleted should be reset to false, so dont buy if its true
                {
                    logger.Info("  " + StrTradeTime + " " + player.Name + " isBuyOrderCompleted is true. Cant use it to buy");
                    continue;
                }

                if (player.isBuyAllowed == false)
                {
                    if (configr.ShowBuyingFlowLogs)
                        logger.Info("  " + StrTradeTime + " " + player.Name + "  Not  Allowed for buying");
                    continue;
                }
                if (configr.IsBuyingAllowed == false)
                {
                    if (configr.ShowBuyingFlowLogs)
                        logger.Info("  " + StrTradeTime + " " + player.Name + "  overall system not  Allowed for buying");
                    continue;
                }
                foreach (Signal sig in MySignals.OrderBy(x => x.PrDiffCurrAndHighPerc).ToList())
                {
                    if (sig.IsIgnored)
                        continue;

                    if (sig.IsPicked)
                        continue;

                    if (boughtCoins.Contains(sig.Symbol))
                    {
                        sig.IsPicked = true;
                        continue;
                    }
                    else
                    {
                        sig.IsPicked = false;
                    }


                    var bitcoinSignal = MySignals.Where(x => x.Symbol == "BTCUSDT").FirstOrDefault();

                    if (bitcoinSignal != null)
                    {
                        // prices are going down. Dont buy till you see recovery
                        if (bitcoinSignal.TotalConsecutive1MinDowns >= 3 ||
                            bitcoinSignal.TotalConsecutive5MinDowns >= 2 ||
                            bitcoinSignal.TotalConsecutive15MinDowns >= 1)
                        {
                            if (bitCoinStatuslogged == false)
                            {
                                logger.Info(" " +
                                    bitcoinSignal.OpenTime.ToString("dd-MMM HH:mm") +
                                    " " + bitcoinSignal.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " +
                                    bitcoinSignal.CurrPr.Rnd(3).ToString().PadRight(10, ' ') + "  Bicoin going down." +
                                    " 1d >=3  ? " + bitcoinSignal.TotalConsecutive1MinDowns.ToString().PadRight(7, ' ') + " " +
                                    " 5d >=2  ? " + bitcoinSignal.TotalConsecutive5MinDowns.ToString().PadRight(7, ' ') +
                                    " 15d >=1  ? " + bitcoinSignal.TotalConsecutive15MinDowns.ToString().PadRight(7, ' ')
                                );

                                bitCoinStatuslogged = true;
                            }

                            continue;
                        }
                    }

                    // prices are going down. Dont buy till you see recovery
                    if (sig.TotalConsecutive1MinDowns >= 3 || sig.TotalConsecutive5MinDowns >= 2)
                    {
                        //logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
                        //  " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
                        //  "  Prices going down." +
                        //  " 1d >=3  ? " + sig.TotalConsecutive1MinDowns.ToString().PadRight(7, ' ') + " " +
                        //  " 5d >=2  ? " + sig.TotalConsecutive5MinDowns.ToString().PadRight(7, ' ')
                        //  );

                        sig.IsIgnored = true;
                        continue;
                    }
                    else
                    {
                        sig.IsIgnored = false;
                    }

                    // Day Trade Count too low
                    if (sig.DayTradeCount < configr.MinAllowedTradeCount)
                    {
                        //logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
                        //  " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
                        //  "  Day Trade Count too low. Dont buy " +
                        //  " Day Trade Count " + sig.DayTradeCount.Rnd(1)
                        //  );

                        sig.IsIgnored = true;
                        continue;
                    }
                    else
                    {
                        sig.IsIgnored = false;
                    }

                    if (sig.IsBestTimeToBuyAtDayLowest) // (sig.IsBestTimeToScalpBuy)
                    {
                        try
                        {
                            await BuyTheCoin(player, sig);
                            sig.IsPicked = true;
                            boughtCoins.Add(sig.Symbol);
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.Error(" " + player.Name + " " + sig.Symbol + " Exception: " + ex.ToString());
                        }
                    }
                    else
                    {
                        LogNoBuy(player, sig);
                        sig.IsPicked = false;
                    }
                }
            }
        }

        private async Task Sell(Player player)
        {
            Signal sig = MySignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();
            if (sig == null) return;

            DB db = new DB();
            decimal mysellPrice = sig.CurrPr;

            if (player == null)
            {
                logger.Info("Player returned as null. Some issue. Returning from Sell");
                return;
            }

            var pair = player.Pair;

            if (pair == null)
            {
                logger.Info("Player's Pair to sell returned as null. Some issue. Returning from Sell");
                return;
            }
            var newPlayer = db.Player.AsNoTracking().Where(x => x.Name == player.Name).FirstOrDefault();
            if (newPlayer.IsTrading == false) return;

            if (newPlayer.SellOrderId > 0)
            {
                logger.Info("Sell order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
                return;
            }
            if (newPlayer.isBuyOrderCompleted == false)
            {
                logger.Info("Buy order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
                return;
            }

            player.DayHigh = sig.DayHighPr;
            player.DayLow = sig.DayLowPr;
            player.UpdatedTime = DateTime.Now;
            player.SellCoinPrice = mysellPrice;

            decimal availableQty = GetAvailQty(player, pair);

            if (availableQty <= 0)
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ') + " Available Quantity 0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return;
            }

            if (player.Quantity == null || player.Quantity.Deci() == 0)
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ')
                + " player.Quantity  0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return;
            }

            player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
            player.TotalSellAmount = mysellPrice * player.Quantity + player.SellCommision;
            player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
            player.CurrentCoinPrice = mysellPrice;
            player.TotalCurrentValue = player.TotalSellAmount;
            player.SellOrderId = 0;
            var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
            player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";

            if (player.ProfitLossChanges.Length > 200)
            {
                player.ProfitLossChanges = player.ProfitLossChanges.GetLast(200);
            }

            var NextSellbelow = prDiffPerc * configr.ReducePriceDiffPercBy / 100;

            // less than sellable percentage. Return
            if (prDiffPerc <= player.SellAbovePerc && ForceSell == false)
            {

                // Reducing Profit Perecetages every  hour if the coin is not able to make a sell due to high profit % set. Do it till you reach 1%

                if (DateTime.Now.Minute == configr.ReduceSellAboveAtMinute &&
                    (DateTime.Now.Second >= configr.ReduceSellAboveFromSecond && DateTime.Now.Second <= configr.ReduceSellAboveToSecond))
                {

                    if (player.SellAbovePerc >= configr.MinSellAbovePerc)
                    {
                        if (configr.IsReducingSellAbvAllowed)
                        {
                            player.SellAbovePerc = player.SellAbovePerc - configr.ReduceSellAboveBy;

                            logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
                                " " + player.Name +
                                " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                                " Reduced player's SellAbovePerc to " + player.SellAbovePerc);
                        }
                    }
                }

                player.SellBelowPerc = player.SellAbovePerc;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            if (prDiffPerc >= player.SellBelowPerc && ForceSell == false)
            {
                if (prDiffPerc > player.LastRoundProfitPerc && NextSellbelow > player.SellBelowPerc)
                {
                    player.SellBelowPerc = NextSellbelow;
                }
                player.LastRoundProfitPerc = prDiffPerc;
                player.AvailableAmountToBuy = 0;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                if (sig != null)
                {
                    logger.Info("  " +
                              sig.OpenTime.ToString("dd-MMM HH:mm") +
                            " " + player.Name +
                           " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                            " > NextSellbelow  " + NextSellbelow.Deci().Rnd().ToString().PadRight(11, ' ') +
                            " Not selling ");
                }
                return;
            }

            if (sig != null)
            {
                logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
                            " " + player.Name +
                            " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                            "  LastRoundProfitPerc  " + player.LastRoundProfitPerc.Deci().Rnd().ToString().PadRight(11, ' ')
                        );
            }
            if ((newPlayer.isSellAllowed == false || configr.IsSellingAllowed == false) && ForceSell == false)
            {
                logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
                            " " + player.Name +
                            " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " Selling not allowed "
                        );
                player.AvailableAmountToBuy = 0;
                player.LastRoundProfitPerc = prDiffPerc;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }
            //  if ((prDiffPerc < player.LastRoundProfitPerc && sig.IsBestTimeToSellAtDayHighest) || ForceSell == true)
            if (((prDiffPerc < player.LastRoundProfitPerc)) || ForceSell == true) // Scalp: (prDiffPerc < player.LastRoundProfitPerc ) || ForceSell == true)
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

                ForceSell = false;

                var PriceChangeResponse = await client.GetDailyTicker(pair);

                logger.Info("  " +
                              sig.OpenTime.ToString("dd-MMM HH:mm") +
                            " " + player.Name +
                           " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " Contacting selling price ticker ");

                mysellPrice = PriceChangeResponse.LastPrice;

                player.DayHigh = PriceChangeResponse.HighPrice;
                player.DayLow = PriceChangeResponse.LowPrice;
                player.UpdatedTime = DateTime.Now;
                player.SellCoinPrice = mysellPrice;
                player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
                player.TotalSellAmount = mysellPrice * player.Quantity + player.SellCommision;
                player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
                player.CurrentCoinPrice = mysellPrice;
                player.TotalCurrentValue = player.TotalSellAmount;

                var coinprecison = myCoins.Where(x => x.Coin == pair).FirstOrDefault().TradePrecision;

                var sellOrder = await client.CreateOrder(new CreateOrderRequest()
                {
                    Price = mysellPrice,
                    Quantity = player.Quantity.Deci().Rnd(coinprecison),
                    Side = OrderSide.Sell,
                    Symbol = player.Pair,
                    Type = OrderType.Limit,
                    TimeInForce = TimeInForce.GTC
                });

                player.SellOrderId = sellOrder.OrderId;
                player.SellTime = DateTime.Now;
                player.AvailableAmountToBuy = player.TotalSellAmount;
                db.Player.Update(player);
            }
            else
            {
                LogNoSellReason(player, sig, mysellPrice, prDiffPerc);
                player.AvailableAmountToBuy = 0;
                player.LastRoundProfitPerc = prDiffPerc;
                db.Player.Update(player);
            }
            await db.SaveChangesAsync();
            ForceSell = false;
        }

        #region QA

        private async Task BuyTheCoinQA(PlayerQA player, Signal sig)
        {
            DB db = new DB();

            var PriceResponse = await client.GetPrice(sig.Symbol);

            decimal mybuyPrice = PriceResponse.Price;

            player.Pair = sig.Symbol;

            var coin = myCoins.Where(x => x.Coin == sig.Symbol).FirstOrDefault();

            var coinprecison = coin.TradePrecision;

            var quantity = (player.AvailableAmountToBuy.Deci() / mybuyPrice).Rnd(coinprecison);

            //var buyOrder = await client.CreateOrder(new CreateOrderRequest()
            //{
            //    Price = mybuyPrice,
            //    Quantity = quantity,
            //    Side = OrderSide.Buy,
            //    Symbol = player.Pair,
            //    Type = OrderType.Limit,
            //    TimeInForce = TimeInForce.GTC
            //});


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
            player.BuyOrderId = 10; //[TODO] hardcoded for QA
            player.SellOrderId = 0;
            player.UpdatedTime = DateTime.Now;
            player.BuyOrSell = "Buy";
            player.SellTime = null;
            player.SellCommision = player.BuyCommision;
            player.SellCoinPrice = mybuyPrice;
            player.ProfitLossAmt = (player.TotalCurrentValue - player.TotalBuyCost).Deci();
            player.TotalSellAmount = player.TotalBuyCost; // resetting available amount for trading
            player.AvailableAmountToBuy = 0; // bought, so no amount available to buy
            player.isBuyOrderCompleted = false;
            player.RepsTillCancelOrder = 0;
            player.SellAbovePerc = 0.6M;
            player.SellBelowPerc = player.SellAbovePerc;
            db.PlayerQA.Update(player);

            //Send Buy Order

            PlayerTradesQA playerHistory = iPlayerQAMapper.Map<PlayerQA, PlayerTradesQA>(player);
            playerHistory.Id = 0;
            await db.PlayerTradesQA.AddAsync(playerHistory);
            await db.SaveChangesAsync();
        }

        private async Task BuyQA()
        {
            if (MySignals == null || MySignals.Count() == 0)
            {
                //   logger.Info("  " + StrTradeTime + " no signals found. So cannot proceed for buy process ");
                return;
            }

            DB db = new DB();

            var players = await db.PlayerQA.OrderBy(x => x.Id).ToListAsync();
            boughtCoins = await db.PlayerQA.Where(x => x.Pair != null).Select(x => x.Pair).ToListAsync();

            foreach (var player in players)
            {
                if (player.IsTrading)
                {
                    await UpdateActivePlayerStatsQA(player);
                    continue;
                }

                if (player.isBuyOrderCompleted) // before buying the buyordercompleted should be reset to false, so dont buy if its true
                {
                    //  logger.Info("  " + StrTradeTime + " " + player.Name + " isBuyOrderCompleted is true. Cant use it to buy");
                    continue;
                }

                if (player.AvailableAmountToBuy < configr.MinimumAmountToTradeWith)
                {
                    //if (configr.ShowBuyingFlowLogs)
                    //    logger.Info("  " + StrTradeTime + " " + player.Name + " Avl Amt " + player.AvailableAmountToBuy + " Not enough for buying ");
                    continue;
                }
                //if (player.isBuyAllowed == false)
                //{
                //    //if (configr.ShowBuyingFlowLogs)
                //    //    logger.Info("  " + StrTradeTime + " " + player.Name + "  Not  Allowed for buying");
                //    continue;
                //}
                //if (configr.IsBuyingAllowed == false)
                //{
                //    //if (configr.ShowBuyingFlowLogs)
                //    //    logger.Info("  " + StrTradeTime + " " + player.Name + "  overall system not  Allowed for buying");
                //    continue;
                //}

                foreach (Signal sig in MySignals.OrderBy(x => x.PrDiffCurrAndHighPerc).ToList())
                {
                    if (sig.IsIgnored)
                        continue;

                    if (sig.IsPicked)
                        continue;

                    if (boughtCoins.Contains(sig.Symbol))
                    {
                        sig.IsPicked = true;
                        continue;
                    }
                    else
                    {
                        sig.IsPicked = false;
                    }

                    var bitcoinSignal = MySignals.Where(x => x.Symbol == "BTCUSDT").FirstOrDefault();

                    if (bitcoinSignal != null)
                    {
                        // prices are going down. Dont buy till you see recovery
                        if (
                            bitcoinSignal.TotalConsecutive15MinDowns >= 2 ||
                            bitcoinSignal.TotalConsecutive5MinDowns >= 2)
                        {
                            sig.IsIgnored = true;
                            continue;
                        }
                    }

                    // prices are going down. Dont buy till you see recovery
                    if (sig.TotalConsecutive1MinDowns >= 3)
                    {
                        sig.IsIgnored = true;
                        continue;
                    }
                    else
                    {
                        sig.IsIgnored = false;
                    }

                    // Day Trade Count too low
                    if (sig.DayTradeCount < configr.MinAllowedTradeCount)
                    {
                        //logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
                        //  " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
                        //  "  Day Trade Count too low. Dont buy " +
                        //  " Day Trade Count " + sig.DayTradeCount.Rnd(1)
                        //  );

                        sig.IsIgnored = true;
                        continue;
                    }
                    else
                    {
                        sig.IsIgnored = false;
                    }

                    if (sig.IsBestTimeToScalpBuy)
                    {
                        try
                        {
                            await BuyTheCoinQA(player, sig);
                            sig.IsPicked = true;
                            boughtCoins.Add(sig.Symbol);
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.Error(" " + player.Name + " " + sig.Symbol + " Exception: " + ex.ToString());
                        }
                    }
                    else
                    {
                        sig.IsPicked = false;
                        sig.IsIgnored = false;
                    }
                }
            }
        }

        private async Task SellQA(PlayerQA player)
        {
            Signal sig = MySignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();
            if (sig == null) return;

            DB db = new DB();
            decimal mysellPrice = sig.CurrPr;

            if (player == null)
            {
                //logger.Info("Player returned as null. Some issue. Returning from Sell");
                return;
            }

            var pair = player.Pair;

            if (pair == null)
            {
                //logger.Info("Player's Pair to sell returned as null. Some issue. Returning from Sell");
                return;
            }

            var newPlayer = db.PlayerQA.AsNoTracking().Where(x => x.Name == player.Name).FirstOrDefault();
            if (newPlayer.IsTrading == false) return;

            //[TODO] Update if productionalized

            //if (newPlayer.SellOrderId > 0)
            //{
            //    //logger.Info("Sell order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
            //    return;
            //}
            //if (newPlayer.isBuyOrderCompleted == false)
            //{
            //    //logger.Info("Buy order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
            //    return;
            //}

            player.DayHigh = sig.DayHighPr;
            player.DayLow = sig.DayLowPr;
            player.UpdatedTime = DateTime.Now;
            player.SellCoinPrice = mysellPrice;

            decimal availableQty = GetAvailQtyQA(player, pair);

            if (availableQty <= 0)
            {
                //logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ') + " Available Quantity 0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return;
            }

            if (player.Quantity == null || player.Quantity.Deci() == 0)
            {
                //logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ')
                //  + " player.Quantity  0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return;
            }

            player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
            player.TotalSellAmount = mysellPrice * player.Quantity + player.SellCommision;
            player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
            player.CurrentCoinPrice = mysellPrice;
            player.TotalCurrentValue = player.TotalSellAmount;
            player.SellOrderId = 0;
            var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
            player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";

            if (player.ProfitLossChanges.Length > 200)
            {
                player.ProfitLossChanges = player.ProfitLossChanges.GetLast(200);
            }

            var NextSellbelow = prDiffPerc * configr.ReducePriceDiffPercBy / 100;

            // less than sellable percentage. Return
            if (prDiffPerc <= player.SellAbovePerc && ForceSell == false)
            {

                // Reducing Profit Perecetages every  hour if the coin is not able to make a sell due to high profit % set. Do it till you reach 1%

                if (DateTime.Now.Minute == configr.ReduceSellAboveAtMinute &&
                    (DateTime.Now.Second >= configr.ReduceSellAboveFromSecond && DateTime.Now.Second <= configr.ReduceSellAboveToSecond))
                {

                    if (player.SellAbovePerc >= configr.MinSellAbovePerc)
                    {
                        if (configr.IsReducingSellAbvAllowed)
                        {
                            player.SellAbovePerc = player.SellAbovePerc - configr.ReduceSellAboveBy;

                            //logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
                            //    " " + player.Name +
                            //   " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            //   " Reduced player's SellAbovePerc to " + player.SellAbovePerc);
                        }
                    }
                }

                player.SellBelowPerc = player.SellAbovePerc;
                db.PlayerQA.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            if (prDiffPerc >= player.SellBelowPerc && ForceSell == false)
            {
                if (prDiffPerc > player.LastRoundProfitPerc && NextSellbelow > player.SellBelowPerc)
                {
                    player.SellBelowPerc = NextSellbelow;
                }
                player.LastRoundProfitPerc = prDiffPerc;
                player.AvailableAmountToBuy = 0;
                db.PlayerQA.Update(player);
                await db.SaveChangesAsync();

                if (sig != null)
                {
                    //logger.Info("  " +
                    //        sig.OpenTime.ToString("dd-MMM HH:mm") +
                    //      " " + player.Name +
                    //     " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                    //      " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                    //      " > NextSellbelow  " + NextSellbelow.Deci().Rnd().ToString().PadRight(11, ' ') +
                    //      " Not selling ");
                }
                return;
            }

            if (sig != null)
            {
                //logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
                //         " " + player.Name +
                //         " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                //         " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                //         "  LastRoundProfitPerc  " + player.LastRoundProfitPerc.Deci().Rnd().ToString().PadRight(11, ' ')
                //        );
            }
            if ((newPlayer.isSellAllowed == false || configr.IsSellingAllowed == false) && ForceSell == false)
            {
                //logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
                //          " " + player.Name +
                //          " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                //          " Selling not allowed "
                //       );
                player.AvailableAmountToBuy = 0;
                player.LastRoundProfitPerc = prDiffPerc;
                db.PlayerQA.Update(player);
                await db.SaveChangesAsync();
                return;
            }
            //  if ((prDiffPerc < player.LastRoundProfitPerc && sig.IsBestTimeToSellAtDayHighest) || ForceSell == true)
            if (((prDiffPerc < player.LastRoundProfitPerc)) || ForceSell == true) // Scalp: (prDiffPerc < player.LastRoundProfitPerc ) || ForceSell == true)
            {
                if (sig != null)
                {
                    //logger.Info("  " +
                    //       sig.OpenTime.ToString("dd-MMM HH:mm") +
                    //     " " + player.Name +
                    //    " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                    //     " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                    //     " < LastRoundProfitPerc  " + player.LastRoundProfitPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                    //     " selling ");
                }

                ForceSell = false;

                var PriceChangeResponse = await client.GetDailyTicker(pair);

                //logger.Info("  " +
                //            sig.OpenTime.ToString("dd-MMM HH:mm") +
                //          " " + player.Name +
                //         " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                //          " Contacting selling price ticker ");

                mysellPrice = PriceChangeResponse.LastPrice;

                player.DayHigh = PriceChangeResponse.HighPrice;
                player.DayLow = PriceChangeResponse.LowPrice;
                player.UpdatedTime = DateTime.Now;
                player.SellCoinPrice = mysellPrice;
                player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
                player.TotalSellAmount = mysellPrice * player.Quantity + player.SellCommision;
                player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
                player.CurrentCoinPrice = mysellPrice;
                player.TotalCurrentValue = player.TotalSellAmount;

                var coinprecison = myCoins.Where(x => x.Coin == pair).FirstOrDefault().TradePrecision;

                //var sellOrder = await client.CreateOrder(new CreateOrderRequest()
                //{
                //    Price = mysellPrice,
                //    Quantity = player.Quantity.Deci().Rnd(coinprecison),
                //    Side = OrderSide.Sell,
                //    Symbol = player.Pair,
                //    Type = OrderType.Limit,
                //    TimeInForce = TimeInForce.GTC
                //});

                player.SellOrderId = 10; //[TODO] sellOrder.OrderId;
                player.SellTime = DateTime.Now;
                player.AvailableAmountToBuy = player.TotalSellAmount;
                db.PlayerQA.Update(player);
                await db.SaveChangesAsync();

                await UpdatePlayerAfterSellConfirmedQA(player);
            }
            else
            {
                player.AvailableAmountToBuy = 0;
                player.LastRoundProfitPerc = prDiffPerc;
                db.PlayerQA.Update(player);
                await db.SaveChangesAsync();
            }

            ForceSell = false;
        }

        public async Task UpdateActivePlayerStatsQA(PlayerQA player)
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
                var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
                player.AvailableAmountToBuy = 0;
                player.UpdatedTime = DateTime.Now;
                db.PlayerQA.Update(player);
                await db.SaveChangesAsync();
            }
        }

        public decimal GetAvailQtyQA(PlayerQA player, string pair)
        {
            decimal availableQty = player.Quantity.Deci();
            return availableQty;
        }

        private async Task UpdatePlayerAfterSellConfirmedQA(PlayerQA player)
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

            PlayerTradesQA PlayerTrades = iPlayerQAMapper.Map<PlayerQA, PlayerTradesQA>(player);

            PlayerTrades.Id = 0;
            await db.PlayerTradesQA.AddAsync(PlayerTrades);
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
            player.IsTrading = false;
            player.BuyOrSell = string.Empty;
            player.ProfitLossAmt = 0;
            player.ProfitLossChanges = string.Empty;
            player.BuyOrderId = 0;
            player.SellOrderId = 0;
            player.HardSellPerc = 0;
            player.isBuyOrderCompleted = false;
            player.RepsTillCancelOrder = 0;
            player.SellAbovePerc = 0.6M;
            player.SellBelowPerc = player.SellAbovePerc;
            db.PlayerQA.Update(player);
            await db.SaveChangesAsync();
            await RedistributeBalancesQA();

        }

        public async Task RedistributeBalancesQA()
        {
            DB db = new DB();

            decimal TotalAmount = 0;

            var availplayers = await db.PlayerQA.Where(x => x.IsTrading == false).OrderBy(x => x.Id).ToListAsync();

            foreach (var player in availplayers)
            {
                TotalAmount = TotalAmount + player.AvailableAmountToBuy.Deci();
            }


            if (availplayers.Count() > 0)
            {
                var avgAvailAmountForTrading = TotalAmount / availplayers.Count();

                foreach (var player in availplayers)
                {
                    player.AvailableAmountToBuy = avgAvailAmountForTrading;
                    player.TotalCurrentValue = 0;
                    db.PlayerQA.Update(player);
                }
                await db.SaveChangesAsync();
            }
        }

        #endregion QA

        private async void SellThisBot(object sender, RoutedEventArgs e)
        {
            DB db = new DB();
            PlayerViewModel model = (sender as Button).DataContext as PlayerViewModel;
            Player player = await db.Player.Where(x => x.Name == model.Name).FirstOrDefaultAsync();
            ForceSell = true;
            await Sell(player);
            ForceSell = false;
            await SetGrid();
        }

        public async Task RedistributeBalances()
        {
            DB db = new DB();
            var players = await db.Player.AsNoTracking().ToListAsync();
            //  decimal TotalAmount=0;

            //foreach(var player in players)
            //{
            //    TotalAmount=TotalAmount+player.TotalCurrentValue.Deci()+ player.AvailableAmountToBuy.Deci();
            //}
            //decimal OvearallAverageAmount = TotalAmount/players.Count();

            var availplayers = await db.Player.Where(x => x.IsTrading == false).OrderBy(x => x.Id).ToListAsync();

            AccountInformationResponse accinfo = await client.GetAccountInformation();

            decimal TotalAvalUSDT = 0;

            var USDT = accinfo.Balances.Where(x => x.Asset == "USDT").FirstOrDefault();

            if (USDT != null)
            {
                TotalAvalUSDT = USDT.Free - (USDT.Free * 2 / 100); //Take only 98 % to cater for small differences
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
                var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
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
                                db.Player.Update(player);
                                await db.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            player.isBuyOrderCompleted = true;
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
            db.Player.Update(player);
            await db.SaveChangesAsync();

            await RedistributeBalances();


        }

        public decimal GetAvailQty(Player player, string pair)
        {
            decimal availableQty = player.Quantity.Deci();

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

        public async Task UpdateAllowedPrecisionsForPairs()
        {

            DB db = new DB();

            exchangeInfo = await client.GetExchangeInfo();
            await GetMyCoins();

            foreach (var coin in myCoins)
            {
                var symbol = exchangeInfo.Symbols.Where(x => x.Symbol == coin.Coin).FirstOrDefault();
                if (symbol != null)
                {
                    ExchangeInfoSymbolFilterLotSize lotsize = symbol.Filters[2] as ExchangeInfoSymbolFilterLotSize;
                    var precision = lotsize.StepSize.GetAllowedPrecision();
                    coin.TradePrecision = precision;
                    db.MyCoins.Update(coin);
                }
                //    logger.Info("Precision for coin " + coin.Coin + " is set as " + precision + " Original step size from exchange info is " + lotsize.StepSize);
            }

            await db.SaveChangesAsync();


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

        private async Task GetMyCoins()
        {
            using (var db = new DB())
            {
                myCoins = await db.MyCoins.AsNoTracking().Where(x => x.IsIncludedForTrading == true).ToListAsync();
                // MyCoins = await db.MyCoins.AsNoTracking().Where(x => x.IsIncludedForTrading == true).Take(2).ToListAsync();
            }
        }

        public async Task UpdateCoinsForTrading()
        {
            List<Signal> signals = new List<Signal>();
            exchangeInfo = await client.GetExchangeInfo();

            foreach (var symbol in exchangeInfo.Symbols)
            {
                if (symbol.Symbol.EndsWith("USDT"))
                {
                    if (symbol.Symbol.EndsWith("UPUSDT") || symbol.Symbol.EndsWith("DOWNUSDT") ||
                        symbol.Symbol.EndsWith("BULLUSDT") || symbol.Symbol.EndsWith("BEARUSDT") || symbol.Symbol == "BUSDUSDT" ||
                        symbol.Symbol == "USDCUSDT" || symbol.Symbol == "EURUSDT" || symbol.Symbol == "DAIUSDT" ||
                        symbol.Symbol == "ATAUSDT" || symbol.Symbol == "MBOXUSDT" || symbol.Symbol == "C98USDT"
                        )
                    {
                        continue;
                    }
                    var pricechangeresponse = await client.GetDailyTicker(symbol.Symbol);
                    Signal signal = new Signal();
                    signal.Symbol = symbol.Symbol;
                    signal.DayTradeCount = pricechangeresponse.TradeCount;
                    signals.Add(signal);
                }
            }

            signals = signals.OrderByDescending(x => x.DayTradeCount).ToList();

            using (var db = new DB())
            {
                List<string> coins = db.MyCoins.Select(x => x.Coin).ToList();
                List<string> playercoins = db.Player.Select(x => x.Pair).ToList();
                //  List<string> playerQAcoins = db.PlayerQA.Select(x => x.Pair).ToList();

                foreach (var sig in signals)
                {
                    // heavily traded but not in coin list. Add

                    if (sig.DayTradeCount > configr.MinAllowedTradeCount)
                    {
                        if (!coins.Contains(sig.Symbol))
                        {
                            MyCoins coin = new MyCoins();
                            coin.Coin = sig.Symbol;
                            coin.IsIncludedForTrading = true;
                            coin.TradePrecision = 0;
                            coin.PercAboveDayLowToSell = 12;
                            coin.PercBelowDayHighToBuy = -13;
                            await db.MyCoins.AddAsync(coin);

                            logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin Added");
                        }
                        else
                        {
                            logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin existing");
                        }
                    }
                    //else
                    //{
                    //    if (!playercoins.Contains(sig.Symbol)) // && !playerQAcoins.Contains(sig.Symbol)
                    //    {
                    //        var coin = await db.MyCoins.Where(x => x.Coin == sig.Symbol).FirstOrDefaultAsync();

                    //        if (coin != null)
                    //        {
                    //            db.MyCoins.Remove(coin);

                    //            logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin Removed");
                    //        }
                    //    }
                    //    else
                    //    {
                    //        logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin Being Traded. Cant remove");
                    //    }
                    //}

                    // low traded, but in playerlist. Dont delete for now
                    //low traded and not in playerlist. Delete

                    // logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' '));
                }
                await db.SaveChangesAsync();
            }
        }

        public bool bitCoinStatuslogged = true;

        private async Task Trade()
        {

            bitCoinStatuslogged = false;

            if (isControlCurrentlyInTradeMethod) return;

            isControlCurrentlyInTradeMethod = true;

            DB db = new DB();
            configr = await db.Config.FirstOrDefaultAsync();

            await GetMyCoins();

            TradeTime = DateTime.Now;
            StrTradeTime = TradeTime.ToString("dd-MMM HH:mm:ss");

            NextTradeTime = TradeTime.AddSeconds(configr.IntervalMinutes).ToString("dd-MMM HH:mm:ss");

            EnsureAllSocketsRunning();
            ResetSignalsWithSelectedValues();

            CollectReferenceCandles();
            CreateScalpBuySignals();
            CreateBuyLowestSellHighestSignals();
            LogInfo();
            await UpdateTradeBuyDetails();
            await UpdateTradeSellDetails();

            #region Buy

            try
            {
                logger.Info("");
                logger.Info("Buying Started for " + StrTradeTime);
                logger.Info("");
                await Buy();
                logger.Info("");
                logger.Info("Buying Completed for " + StrTradeTime);
                logger.Info("");

            }
            catch (Exception ex)
            {
                logger.Error("Exception at buy " + ex.Message);
            }

            logger.Info("");
            #endregion Buys

            #region Sell

            logger.Info("Selling Started for " + StrTradeTime);
            logger.Info("");
            try
            {

                var activePlayers = await db.Player.AsNoTracking().OrderBy(x => x.Id).Where(x => x.IsTrading == true).ToListAsync();

                foreach (var player in activePlayers)
                {
                    await Sell(player);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Exception in sell  " + ex.Message);
            }

            logger.Info("Selling Completed for " + StrTradeTime);
            logger.Info("");
            #endregion  Sell



            //#region BuyQA

            //try
            //{

            //    await BuyQA();

            //}
            //catch (Exception ex)
            //{
            //    logger.Error("Exception at buy QA " + ex.Message);
            //}


            //#endregion BuyQA

            //#region SellQA
            //try
            //{

            //    var activePlayers = await db.PlayerQA.AsNoTracking().OrderBy(x => x.Id).Where(x => x.IsTrading == true).ToListAsync();

            //    foreach (var player in activePlayers)
            //    {
            //        await SellQA(player);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    logger.Error("Exception in sell QA " + ex.Message);
            //}


            //#endregion  SellQA


            await SetGrid();
            //  logger.Info("Next run at " + NextTradeTime);

            logger.Info("----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");

            isControlCurrentlyInTradeMethod = false;

        }

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

        private async void btnTrade_Click(object sender, RoutedEventArgs e)
        {
            await Trade();
        }

        #region low priority methods

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

        private async Task SetGrid()
        {
            DB db = new DB();
            decimal? totProfitPerc = 0;
            decimal? totProfit = 0;
            decimal? totalbuys = 0;
            decimal? totalcurrent = 0;
            PlayerViewModels = new List<PlayerViewModel>();
            var players = await db.Player.Where(x => x.IsTrading == true).ToListAsync();

            foreach (var player in players)
            {
                PlayerViewModel playerViewModel = new PlayerViewModel();
                var pair = player.Pair;
                playerViewModel.Name = player.Name;
                playerViewModel.Pair = pair;
                playerViewModel.BuyPricePerCoin = player.BuyCoinPrice;
                playerViewModel.CurrentPricePerCoin = player.CurrentCoinPrice;
                playerViewModel.QuantityBought = player.Quantity;
                playerViewModel.BuyTime = Convert.ToDateTime(player.BuyTime).ToString("dd-MMM HH:mm");
                playerViewModel.SellBelowPerc = player.SellBelowPerc;
                playerViewModel.SellAbovePerc = player.SellAbovePerc;
                playerViewModel.TotalBuyCost = player.TotalBuyCost;
                playerViewModel.TotalSoldAmount = player.TotalSellAmount;
                playerViewModel.TotalCurrentValue = player.TotalCurrentValue;
                totalcurrent += playerViewModel.TotalCurrentValue;
                totalbuys += playerViewModel.TotalBuyCost;
                var prDiffPerc = player.TotalCurrentValue.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
                totProfitPerc += prDiffPerc;
                totProfit += (player.TotalCurrentValue - player.TotalBuyCost);
                playerViewModel.CurrentRoundProfitPerc = prDiffPerc;
                playerViewModel.CurrentRoundProfitAmt = player.TotalCurrentValue - player.TotalBuyCost;
                playerViewModel.LastRoundProfitPerc = player.LastRoundProfitPerc;
                playerViewModel.ProfitLossChanges = player.ProfitLossChanges.GetLast(170);
                PlayerViewModels.Add(playerViewModel);
            }

            PlayerGrid.ItemsSource = PlayerViewModels.OrderByDescending(x => x.CurrentRoundProfitAmt);

            var inactiveplayers = await db.Player.Where(x => x.IsTrading == false).ToListAsync();

            foreach (var inactiveplayer in inactiveplayers)
            {
                totalbuys += inactiveplayer.AvailableAmountToBuy;
                totalcurrent += inactiveplayer.AvailableAmountToBuy;
            }

            lblAvgProfLoss.Text = "Profit: " + totProfit.Deci().Rnd(2) + " Invested: " + totalbuys.Deci().Rnd(0) + " Current: " + totalcurrent.Deci().Rnd(0);

            lblLastRun.Text = "Last Run : " + StrTradeTime;
            lblNextRun.Text = "Next Run: " + NextTradeTime;
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
                player.isSellAllowed = true;

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

        #region oldcode

      

        private async Task RemoveOldCandles()
        {

            using (var db = new DB())
            {
                await db.Database.ExecuteSqlRawAsync("delete from Candle where CAST(RecordedTime AS DATE)  <= GETDATE()-1");
            }

        }

        #endregion
    }
}

/*
 //Old Code

  private async Task<bool> IsReadyForSell(Player player)
        {
            DB db = new DB();
            ForceSell = false;
            var newPlayer = db.Player.AsNoTracking().Where(x => x.Name == player.Name).FirstOrDefault();

            if (newPlayer.IsTrading == false)
            {
                return false;
            }

            Signal sig = CurrentSignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();

            decimal mysellPrice = 0;
            var pair = player.Pair;

            if (player == null)
            {
                logger.Info("Sellable: Player returned as null. Some issue. Returning from Sell");
                return false;
            }
            if (pair == null)
            {
                logger.Info("Sellable: Player's Pair to sell returned as null. Some issue. Returning from Sell");
                return false;
            }

            mysellPrice = sig.CurrPr;
            player.DayHigh = sig.DayHighPr;
            player.DayLow = sig.DayLowPr;
            player.UpdatedTime = DateTime.Now;
            player.SellCoinPrice = mysellPrice;
            decimal availableQty = player.Quantity.Deci();

            if (availableQty <= 0)
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ') +
                  " Available Quantity 0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return false;
            }
            if (player.Quantity == null || player.Quantity.Deci() == 0)
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ') +
               " player.Quantity  0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return false;
            }

            player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
            player.TotalSellAmount = mysellPrice * player.Quantity + player.SellCommision;
            player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
            player.CurrentCoinPrice = mysellPrice;
            player.TotalCurrentValue = player.TotalSellAmount;
            player.SellOrderId = 0;
            var prDiffPerc = player.TotalSellAmount.GetDiffPerc(player.TotalBuyCost);


            if (prDiffPerc <= player.SellAbovePerc)
            {
                player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";
                player.SellBelowPerc = player.SellAbovePerc;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return false;
            }
            if (prDiffPerc > player.SellAbovePerc && PricesGoingUp(sig, player))
            {
                if (player.SellBelowPerc < prDiffPerc * 90 / 100)
                    player.SellBelowPerc = prDiffPerc * 90 / 100;
                player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";
                player.LastRoundProfitPerc = prDiffPerc;
                player.AvailableAmountToBuy = 0;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return false;
            }
            else if (prDiffPerc > player.SellAbovePerc && prDiffPerc > player.SellBelowPerc)
            {
                player.SellBelowPerc = prDiffPerc * 90 / 100;
                player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";
                player.LastRoundProfitPerc = prDiffPerc;
                player.AvailableAmountToBuy = 0;
                db.Player.Update(player);
            }
            else if (prDiffPerc > player.SellAbovePerc)
            {
                player.LastRoundProfitPerc = prDiffPerc;
                player.AvailableAmountToBuy = 0;
                db.Player.Update(player);
            }

            await db.SaveChangesAsync();
            return true;
        }
 */
















//string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
//                         " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(11, ' ') +
//                         " DHi " + sig.DayHighPr.Rnd(6).ToString().PadRight(11, ' ') +
//                         " DLo " + sig.DayLowPr.Rnd(6).ToString().PadRight(11, ' ') +
//                          " DiCr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(6).ToString().PadRight(11, ' ') +
//                         " Cr&Lw " + sig.PrDiffCurrAndLowPerc.Rnd(6).ToString().PadRight(11, ' ') +
//                         " Trds " + sig.DayTradeCount.Rnd(6).ToString().PadRight(11, ' ') +
//                         " Vols " + sig.DayVol.Rnd(2).ToString().PadRight(20, ' ');
//log += " is At Day High. Best time to Sell";
//logger.Info(log);


