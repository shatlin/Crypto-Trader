
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

    public partial class HomeView : UserControl
    {
        public List<MyCoins> myCoins { get; set; }
        public DispatcherTimer TradeTimer;
        public DispatcherTimer ForceBuySellTimer;
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
        List<Task> tasks = new List<Task>();

        public HomeView()
        {
            InitializeComponent();
            Startup();
        }

        private async void Startup()
        {
            logger = LogManager.GetLogger(typeof(MainWindow));

            logger.Info("App Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            logger.Info("");

            DB db = new DB();
            configr = await db.Config.FirstOrDefaultAsync();

            TradeTimer = new DispatcherTimer();
            TradeTimer.Tick += new EventHandler(TraderTimer_Tick);
            TradeTimer.Interval = new TimeSpan(0, 0, configr.IntervalMinutes);

            //ForceBuySellTimer = new DispatcherTimer();
            //ForceBuySellTimer.Tick += new EventHandler(ForceBuySellTimer_Tick);
            //ForceBuySellTimer.Interval = new TimeSpan(0, 0, 6);

            lblBotName.Text = configr.Botname;


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
            //  await SetGrid();

            if (configr.UpdateCoins)
            {
                //await UpdateCoinsForTrading();
                //Thread.Sleep(1000);
                await UpdateAllowedPrecisionsForPairs();
            }

            await RedistributeBalances();

            logger.Info("Getting signal streams Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            logger.Info("");
            CreateSignals();



            //while(MySignals.Any(sig=>sig.IsDailyKlineSocketRunning == false || sig.IsSymbolTickerSocketRunning == false))
            //{
            //    EnsureAllSocketsRunning();
            //    Thread.Sleep(200);
            //}

            TradeTimer.Start();
            // ForceBuySellTimer.Start();
            logger.Info("Getting signal streams completed  and Timer Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            logger.Info("");
        }

        private void EnsureAllSocketsRunning()
        {
                Parallel.ForEach(MySignals.Where(x => x.IsSymbolTickerSocketRunning == false 
                || x.IsDailyKlineSocketRunning == false), sig =>
                {
                    if (sig.IsSymbolTickerSocketRunning == false)
                    {
                        try
                        {
                            try
                            {
                                if (sig.TickerSocketGuid != Guid.Empty) socket.CloseWebSocketInstance(sig.TickerSocketGuid);
                            }
                            catch (Exception ex1)
                            {
                                logger.Info("exception at Ticker socket for " + sig.Symbol + "  " + ex1.Message);
                            }

                            sig.TickerSocketGuid = socket.ConnectToIndividualSymbolTickerWebSocket(sig.Symbol, b =>
                            {
                                sig.CurrPr = b.LastPrice; sig.IsSymbolTickerSocketRunning = true;
                            });

                            //    logger.Info("Ticker socket started for " + coin.Coin);
                        }
                        catch (Exception ex)
                        {
                            sig.IsSymbolTickerSocketRunning = false;
                            logger.Info("exception at Ticker socket for " + sig.Symbol + "  " + ex.Message);
                        }

                    }

                    if (sig.IsDailyKlineSocketRunning == false)
                    {

                        try
                        {
                            try
                            {
                                if (sig.KlineSocketGuid != Guid.Empty) socket.CloseWebSocketInstance(sig.KlineSocketGuid);
                            }
                            catch (Exception ex1)
                            {
                                logger.Info("exception at Kline socket for " + sig.Symbol + "  " + ex1.Message);
                            }

                            sig.KlineSocketGuid = socket.ConnectToKlineWebSocket(sig.Symbol, KlineInterval.OneDay, b =>
                            {
                                sig.OpenTime = b.Kline.StartTime;
                                sig.CloseTime = b.Kline.EndTime;
                                sig.Symbol = b.Symbol;
                                sig.DayVol = b.Kline.Volume;
                                sig.DayTradeCount = b.Kline.NumberOfTrades;
                                sig.IsDailyKlineSocketRunning = true;
                            });

                            //logger.Info("Kline  socket started for " + coin.Coin);

                        }
                        catch (Exception ex)
                        {
                            sig.IsDailyKlineSocketRunning = false;
                            logger.Info("exception at Kline socket for " + sig.Symbol + "  " + ex.Message);
                            // Thread.Sleep(100);
                        }

                    }

                    //try
                    //{
                    //    if (!socket.IsAlive(sig.TickerSocketGuid))
                    //    {
                    //        sig.IsSymbolTickerSocketRunning = false;
                    //        sig.IsDailyKlineSocketRunning = false;
                    //    }
                    //}
                    //catch
                    //{
                    //}

                    //}
                });
            
        }

        private void CreateSignals()
        {
            using (var db = new DB())
            {
                foreach (var coin in myCoins)
                {
                    Signal sig = new Signal();
                    sig.IsSymbolTickerSocketRunning = false;
                    sig.IsDailyKlineSocketRunning = false;
                    sig.Symbol = coin.Coin;
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
                    sig.IsBestTimeToBuyAtDayLowest = false;
                    sig.IsBestTimeToScalpBuy = false;
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

        private decimal GetPriceChangeBetweenCurrentAndReferenceStart(decimal currentPrice, List<SignalCandle> candleList)
        {

            if (candleList == null || candleList.Count == 0)
                return 0M;
            //    candleList = candleList.OrderBy(x => x.CloseTime).ToList();
            var firstpriceOfCandles = candleList.First().ClosePrice;
            return currentPrice.GetDiffPercBetnNewAndOld(firstpriceOfCandles);
        }

        private void CollectReferenceCandles()
        {

            for (int i = 0; i < MySignals.Count; i++)
            {
                try
                {

                    #region day

                    if (DateTime.Now.Hour % 23 == 0 && DateTime.Now.Minute % 57 == 0)
                    {


                        MySignals[i].Ref1DayCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1DayCandles, "day", 30, 23, 57);
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

                    }

                    MySignals[i].PrChPercCurrAndRef1Day = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref1DayCandles);

                    //if (MySignals[i].Ref1DayCandles.Count > 0)
                    //{
                    //    MySignals[i].MinRef1Day = MySignals[i].Ref1DayCandles.Min(x => x.ClosePrice);
                    //    MySignals[i].MaxRef1Day = MySignals[i].Ref1DayCandles.Max(x => x.ClosePrice);
                    //}

                    #endregion day

                    #region 4hour

                    if ((DateTime.Now.Hour == 3 || DateTime.Now.Hour == 7 || DateTime.Now.Hour == 11 || DateTime.Now.Hour == 15 ||
                        DateTime.Now.Hour == 19 || DateTime.Now.Hour == 23)
                        && DateTime.Now.Minute % 57 == 0)
                    {
                        MySignals[i].Ref4HourCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref4HourCandles, "4hour", 12, DateTime.Now.Hour, 57);
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

                    }
                    MySignals[i].PrChPercCurrAndRef4Hour = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref4HourCandles);

                    if (MySignals[i].Ref4HourCandles.Count > 0)
                    {
                        MySignals[i].MinRef4Hour = MySignals[i].Ref4HourCandles.Min(x => x.ClosePrice);
                        MySignals[i].MaxRef4Hour = MySignals[i].Ref4HourCandles.Max(x => x.ClosePrice);
                    }
                    #endregion 4hour

                    #region 1hour

                    if (DateTime.Now.Minute % 57 == 0) //Collected for last 24 hours
                    {
                        MySignals[i].Ref1HourCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1HourCandles, "1hour", 24, DateTime.Now.Hour, 57); // 24 hours
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

                    }
                    MySignals[i].PrChPercCurrAndRef1Hour = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref1HourCandles);

                    //if (MySignals[i].Ref1HourCandles.Count > 0)
                    //{
                    //    MySignals[i].MinRef1Hour = MySignals[i].Ref1HourCandles.Min(x => x.ClosePrice);
                    //    MySignals[i].MaxRef1Hour = MySignals[i].Ref1HourCandles.Max(x => x.ClosePrice);
                    //}
                    #endregion 1hour

                    #region 30minute

                    if (DateTime.Now.Minute % 30 == 0) //Collected for last 6 hours
                    {
                        MySignals[i].Ref30MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref30MinCandles, "30min", 24, DateTime.Now.Hour, DateTime.Now.Minute);
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

                    }
                    MySignals[i].PrChPercCurrAndRef30Min = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref30MinCandles);
                    //if (MySignals[i].Ref30MinCandles.Count > 0)
                    //{
                    //    MySignals[i].MinRef30Min = MySignals[i].Ref30MinCandles.Min(x => x.ClosePrice);
                    //    MySignals[i].MaxRef30Min = MySignals[i].Ref30MinCandles.Max(x => x.ClosePrice);
                    //}
                    #endregion 30minute

                    #region 15minute

                    if (DateTime.Now.Minute % 15 == 0) // collected for last 3 hours
                    {
                        MySignals[i].Ref15MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref15MinCandles, "15min", 16, DateTime.Now.Hour, DateTime.Now.Minute);
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

                    }
                    MySignals[i].PrChPercCurrAndRef15Min = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref15MinCandles);

                    //if (MySignals[i].Ref15MinCandles.Count > 0)
                    //{
                    //    MySignals[i].MinRef15Min = MySignals[i].Ref15MinCandles.Min(x => x.ClosePrice);
                    //    MySignals[i].MaxRef15Min = MySignals[i].Ref15MinCandles.Max(x => x.ClosePrice);
                    //}
                    #endregion 15minute

                    #region 5minute
                    if (DateTime.Now.Minute % 5 == 0) // collected for last day
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


                    }
                    MySignals[i].PrChPercCurrAndRef5Min = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref5MinCandles);

                    //if (MySignals[i].Ref5MinCandles.Count > 0)
                    //{
                    //    MySignals[i].MinRef5Min = MySignals[i].Ref5MinCandles.Min(x => x.ClosePrice);
                    //    MySignals[i].MaxRef5Min = MySignals[i].Ref5MinCandles.Max(x => x.ClosePrice);
                    //}

                    #endregion 5minute

                    #region 1minute

                    if (DateTime.Now.Minute % 1 == 0) // collected for last hour
                    {
                        MySignals[i].Ref1MinCandles = FillSignalCandles(MySignals[i], MySignals[i].Ref1MinCandles, "1min", 30, DateTime.Now.Hour, DateTime.Now.Minute);
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

                    }
                    MySignals[i].PrChPercCurrAndRef1Min = GetPriceChangeBetweenCurrentAndReferenceStart(MySignals[i].CurrPr, MySignals[i].Ref1MinCandles);
                    //if (MySignals[i].Ref1MinCandles.Count > 0)
                    //{
                    //    MySignals[i].MinRef1Min = MySignals[i].Ref1MinCandles.Min(x => x.ClosePrice);
                    //    MySignals[i].MaxRef1Min = MySignals[i].Ref1MinCandles.Max(x => x.ClosePrice);
                    //}
                    #endregion 1minute

                    MySignals[i].PrDiffHighAndLowPerc = MySignals[i].DayHighPr.GetDiffPercBetnNewAndOld(MySignals[i].DayLowPr);
                    MySignals[i].PrDiffCurrAndLowPerc = MySignals[i].CurrPr.GetDiffPercBetnNewAndOld(MySignals[i].DayLowPr);
                    MySignals[i].PrDiffCurrAndHighPerc = MySignals[i].CurrPr.GetDiffPercBetnNewAndOld(MySignals[i].DayHighPr);
                    MySignals[i].JustRecoveredFromDayLow = MySignals[i].PrDiffCurrAndLowPerc >= configr.DayLowGreaterthanTobuy && MySignals[i].PrDiffCurrAndLowPerc <= configr.DayLowLessthanTobuy;

                    //MySignals[i].IsAtDayLow = MySignals[i].PrDiffCurrAndLowPerc >= configr.DayLowGreaterthanTobuy && MySignals[i].PrDiffCurrAndLowPerc <= configr.DayLowLessthanTobuy;

                    //MySignals[i].IsAtDayHigh = MySignals[i].PrDiffCurrAndHighPerc <= configr.DayHighLessthanToSell && MySignals[i].PrDiffCurrAndHighPerc >= configr.DayHighGreaterthanToSell;

                    MySignals[i].IsAtDayLow = MySignals[i].CurrPr < MySignals[i].DayAveragePr;
                    MySignals[i].IsAtDayHigh = MySignals[i].CurrPr > MySignals[i].DayAveragePr;

                    var dayAveragePrice = (MySignals[i].DayHighPr + MySignals[i].DayLowPr) / 2;
                    dayAveragePrice = dayAveragePrice - (dayAveragePrice * 2M / 100);
                    MySignals[i].IsCloseToDayLow = MySignals[i].CurrPr < dayAveragePrice;
                    MySignals[i].DayAveragePr = (MySignals[i].DayHighPr + MySignals[i].DayLowPr) / 2;

                    MySignals[i].DayHighPr = MySignals[i].Ref1HourCandles.Max(x => x.ClosePrice);
                    MySignals[i].DayLowPr = MySignals[i].Ref1HourCandles.Min(x => x.ClosePrice);

                    var coin = myCoins.Where(x => x.Coin == MySignals[i].Symbol).FirstOrDefault();

                    MySignals[i].ForceBuy = coin.ForceBuy;

                    if (coin != null)
                    {
                        MySignals[i].IsIncludedForTrading = coin.IsIncludedForTrading;
                    }
                    else
                    {
                        MySignals[i].IsIncludedForTrading = false;
                    }

                    MySignals[i].IsBestTimeToBuyAtDayLowest = MySignals[i].CurrPr > 0 && MySignals[i].PrDiffCurrAndHighPerc < MySignals[i].PercBelowDayHighToBuy
                                                                     && MySignals[i].PrDiffHighAndLowPerc > MySignals[i].PercAboveDayLowToSell && MySignals[i].IsCloseToDayLow;

                    MySignals[i].IsBestTimeToSellAtDayHighest = MySignals[i].CurrPr > 0 && MySignals[i].PrDiffHighAndLowPerc > MySignals[i].PercAboveDayLowToSell && MySignals[i].IsAtDayHigh;
                }
                catch (Exception ex)
                {
                    logger.Info("Exception at CollectReferenceCandles " + MySignals[i].Symbol + " " + ex.Message);
                    throw;
                }

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

            var OneMins = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime).Take(5);

            var IsOneMinOnDownTrend = OneMins.First().ClosePrice <= OneMins.Min(x => x.ClosePrice);

            //var FiveMins = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

            //var IsFiveMinOnDownTrend = FiveMins.First().ClosePrice <= FiveMins.Min(x => x.ClosePrice);

            //var FifteenMins = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

            //var IsFifteenMinOnDownTrend = FifteenMins.First().ClosePrice <= FifteenMins.Min(x => x.ClosePrice);

            // prices are going down. Dont buy till you see recovery
            if (IsOneMinOnDownTrend) //|| IsFiveMinOnDownTrend
            {
                logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
                  " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
                  "  Prices going down.Wait till confirmation"
                  );

                return true;
            }
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
                var recentSells = await db.PlayerTrades.Where(x => x.BuyOrSell != "Buy").OrderByDescending(x => x.Id).Take(10).ToListAsync();

                foreach (var recentSell in recentSells)
                {
                    if (recentSell.Pair == sig.Symbol && sig.CurrPr > (recentSell.BuyCoinPrice + recentSell.SellCoinPrice) / 2)
                    {
                        //       logger.Info(
                        //           sig.OpenTime.ToString("dd-MMM HH:mm") +
                        //           " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                        //           " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
                        //           "  Recently Sold. " +
                        //           " CrPr > last bought + sold / 2 " +
                        //((recentSell.BuyCoinPrice + recentSell.SellCoinPrice) / 2).Deci().Rnd(3).ToString().PadRight(7, ' ') + " ");
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

            LogBuy(player, sig);

            player.Pair = sig.Symbol;

            var coin = myCoins.Where(x => x.Coin == sig.Symbol).FirstOrDefault();

            var coinprecison = coin.TradePrecision;

            var quantity = (player.AvailableAmountToBuy.Deci() / mybuyPrice).Rnd(coinprecison);

            BaseCreateOrderResponse buyOrder = null;
            if (marketbuy == false)
            {
                buyOrder = await client.CreateOrder(new CreateOrderRequest()
                {
                    Price = mybuyPrice,
                    Quantity = quantity,
                    Side = OrderSide.Buy,
                    Symbol = player.Pair,
                    Type = OrderType.Limit,
                    TimeInForce = TimeInForce.GTC

                    //Quantity = quantity,
                    //Side = OrderSide.Buy,
                    //Symbol = player.Pair,
                    //Type = OrderType.Market

                });
            }
            else
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

            MyCoins buycoin = myCoins.Where(x => x.Coin == player.Pair).FirstOrDefault();

            if (buycoin.ForceBuy == true)
            {
                buycoin.ForceBuy = false;
                db.MyCoins.Update(buycoin);
            }

            await db.SaveChangesAsync();
        }

        private async Task Buy()
        {
            DB db = new DB();

            var players = await db.Player.OrderBy(x => x.Id).ToListAsync();
            boughtCoins = await db.Player.Where(x => x.Pair != null).Select(x => x.Pair).ToListAsync();

            foreach (var player in players)
            {
                if (await ShouldSkipPlayerFromBuying(player) == true)
                    continue;

                var forcebuyCoin = myCoins.Where(x => x.ForceBuy == true).FirstOrDefault();


                // foreach (Signal sig in MySignals.Where(x => x.ForceBuy == true))

                if (forcebuyCoin != null)

                {
                    var sig = MySignals.Where(x => x.Symbol == forcebuyCoin.Coin).FirstOrDefault();

                    if (!IsCoinTradeCountTooLow(sig) && !IsCoinPriceGoingDown(sig))
                    {
                        await BuyTheCoin(player, sig, true);
                        sig.IsPicked = true;
                        boughtCoins.Add(sig.Symbol);
                        return;
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


                //    if (await IsRecentlySold(sig) && sig.ForceBuy == false)
                //    {
                //        sig.IsIgnored = true;
                //        continue;
                //    }

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
            if (await ShouldReturnFromSelling(sig, player) == true) return;

            var pair = player.Pair;
            var mysellPrice = sig.CurrPr;
            player.Quantity = await GetAvailQty(player, pair);
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

                mysellPrice = PriceChangeResponse.LastPrice;
                player.DayHigh = PriceChangeResponse.HighPrice;
                player.DayLow = PriceChangeResponse.LowPrice;
                player.UpdatedTime = DateTime.Now;
                player.SellCoinPrice = mysellPrice;
                player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
                player.TotalSellAmount = mysellPrice * player.Quantity - player.SellCommision;
                player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
                player.CurrentCoinPrice = mysellPrice;
                player.TotalCurrentValue = player.TotalSellAmount;

                var coinprecison = myCoins.Where(x => x.Coin == pair).FirstOrDefault().TradePrecision;

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
            if (configr.ShouldSellWhenAllBotsAtLoss == true)
            {
                bool allPlayersAtLoss = true;

                using (var db = new DB())
                {
                    var players = await db.Player.Where(x => x.IsTrading == true && x.SellOrderId <= 0).ToListAsync();

                    foreach (var player in players)
                    {
                        var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
                        if (prDiffPerc > configr.SellWhenAllBotsAtLossBelow)
                        {
                            allPlayersAtLoss = false;
                            break;
                        }
                    }

                    if (allPlayersAtLoss == false)
                    {
                        decimal totalbuycost = 0;
                        decimal totalcurrentvalue = 0;
                        foreach (var player in players)
                        {
                            totalbuycost += player.TotalBuyCost.Deci();
                            totalcurrentvalue += player.TotalSellAmount.Deci();

                        }

                        var prDiff = totalcurrentvalue.GetDiffPercBetnNewAndOld(totalbuycost);
                        if (prDiff <= configr.SellWhenAllBotsAtLossBelow)
                        {
                            allPlayersAtLoss = true;
                        }
                    }

                    if (allPlayersAtLoss == true)
                    {
                        foreach (var player in players)
                        {
                            player.ForceSell = true;
                            db.Player.Update(player);
                        }
                        configr.CrashSell = true;
                        configr.IsBuyingAllowed = false;
                        db.Config.Update(configr);
                        await db.SaveChangesAsync();
                    }

                    decimal buycost = 0;
                    decimal currentvalue = 0;

                    foreach (var player in players)
                    {
                         buycost = player.TotalBuyCost.Deci();
                         currentvalue = player.TotalSellAmount.Deci();

                        var prDi = currentvalue.GetDiffPercBetnNewAndOld(buycost);

                        if (prDi <= configr.SellWhenAllBotsAtLossBelow)
                        {
                            player.ForceSell = true;
                            db.Player.Update(player);
                        }

                    }

                    await db.SaveChangesAsync();

                }
            }
        }

        private async Task<bool> ShouldReturnFromSelling(Signal sig, Player player)
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
            var availableQty = await GetAvailQty(player, pair);

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
                TotalAvalUSDT = USDT.Free - (USDT.Free * 1.5M / 100); //Take only 98 % to cater for small differences
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
            db.Player.Update(player);
            await db.SaveChangesAsync();

            await RedistributeBalances();


        }

        public async Task<decimal?> GetAvailQty(Player player, string pair)
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

        private async Task GetMyCoins()
        {
            using (var db = new DB())
            {
                myCoins = await db.MyCoins.AsNoTracking().Where(x => x.IsIncludedForTrading == true).ToListAsync();
            }
        }

        private async Task UpdateCoins()
        {

            await GetMyCoins();
            using (var db = new DB())
            {
                foreach (var coin in myCoins.Where(x => x.IsIncludedForTrading == true))
                {
                    var sig = MySignals.Where(x => x.Symbol == coin.Coin).FirstOrDefault();
                    coin.DayTradeCount = sig.DayTradeCount;
                    coin.DayVolume = sig.DayVol;
                    db.Update(coin);
                }
                await db.SaveChangesAsync();
            }
            await GetMyCoins();
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
                        symbol.Symbol == "USDCUSDT" || symbol.Symbol == "EURUSDT" || symbol.Symbol == "DAIUSDT"
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
                            coin.IsIncludedForTrading = false;
                            coin.TradePrecision = 0;
                            coin.PercAboveDayLowToSell = 13;
                            coin.PercBelowDayHighToBuy = -13;
                            await db.MyCoins.AddAsync(coin);

                            logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin Added");
                        }
                        else
                        {
                            //logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin existing");
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

            //await GetMyCoins();

            //if (myCoins.Any(x => x.ForceBuy == true))
            //{
            //    return; // coins are marked to buy. Dont use trading code at this time.
            //}

            //using (var db2 = new DB())
            //{
            //    var players = await db2.Player.AsNoTracking().Where(x => x.ForceSell == true).ToListAsync();
            //    if (players.Any())
            //    {
            //        return;
            //    }
            //}

            if (isControlCurrentlyInTradeMethod) return;

            isControlCurrentlyInTradeMethod = true;

            DB db = new DB();
            configr = await db.Config.FirstOrDefaultAsync();



            NextTradeTime = TradeTime.AddSeconds(configr.IntervalMinutes).ToString("dd-MMM HH:mm:ss");

            var watch = new Stopwatch();
            watch.Start();

            EnsureAllSocketsRunning();
            ResetSignalsWithSelectedValues();

            watch.Stop();

            logger.Info("Total EnsureAllSocketsRunning calculation time " + watch.ElapsedMilliseconds);

            watch.Restart();


            CollectReferenceCandles();

            watch.Stop();

            logger.Info("Total CollectReferenceCandles calculation time " + watch.ElapsedMilliseconds);

            watch.Restart();

            //  CreateScalpBuySignals();
            CreateBuyLowestSellHighestSignals();

            watch.Stop();

            logger.Info("Total CreateBuyLowestSellHighestSignals calculation time " + watch.ElapsedMilliseconds);

            watch.Restart();

            LogInfo();




            await UpdateTradeBuyDetails();
            await UpdateTradeSellDetails();
            await UpdateCoins();

            watch.Stop();

            logger.Info("Total UpdateTradeBuyDetails UpdateTradeSellDetails UpdateCoins calculation time " + watch.ElapsedMilliseconds);

            watch.Restart();

            #region Buy

            try
            {
                //logger.Info("");
                //logger.Info("Buying Started for " + StrTradeTime);
                //logger.Info("");
                await Buy();
                //logger.Info("");
                //logger.Info("Buying Completed for " + StrTradeTime);
                //logger.Info("");

            }
            catch (Exception ex)
            {
                logger.Error("Exception at buy " + ex.Message);
            }

            logger.Info("");


            watch.Stop();

            logger.Info("Total Buy time " + watch.ElapsedMilliseconds);

            #endregion Buys

            #region Sell

            watch.Restart();

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

            watch.Stop();

            logger.Info("Total Sell time " + watch.ElapsedMilliseconds);

            #endregion  Sell

            #region Check Crash Sell

            watch.Restart();

            try
            {
                await CheckCrashToSellAll();
            }
            catch (Exception ex)
            {
                logger.Error("Exception in Crash sell set up " + ex.Message);
            }
            finally
            {
                UpdateConfigAfterCrashSell();
            }

            watch.Stop();

            logger.Info("Total CheckCrashToSellAll time " + watch.ElapsedMilliseconds);

            logger.Info("");
            logger.Info("Trading Completed at  " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            #endregion



            logger.Info("----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");

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

        private async void ForceBuySellTimer_Tick(object sender, EventArgs e)
        {

            if (isControlCurrentlyInTradeMethod) return;

            try
            {
                isControlCurrentlyInTradeMethod = true;

                await GetMyCoins();

                if (myCoins.Any(x => x.ForceBuy == true))
                {
                    logger.Info("coins with forcebuy set to true exist. Buying...");
                    await Buy();
                }
                else
                {
                    logger.Info("No coins with forcebuy set to true");
                }

                using (var db = new DB())
                {
                    var players = await db.Player.AsNoTracking().Where(x => x.ForceSell == true).ToListAsync();
                    if (players.Any())
                    {
                        logger.Info("coins with forcesell set to true exist. Selling...");
                        foreach (var player in players)
                        {
                            await Sell(player);
                        }
                    }
                    else
                    {
                        logger.Info("No players with forcesell set to true");
                    }
                }
            }
            catch (Exception ex)
            {

                logger.Error("Exception at force buy and sell " + ex.Message);
            }
            finally
            {
                isControlCurrentlyInTradeMethod = false;
            }

        }

        private async void btnTrade_Click(object sender, RoutedEventArgs e)
        {
            await Trade();
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
                playerViewModel.ProfitLossChanges = player.ProfitLossChanges.GetLast(95);
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

        private void oldbuylowlogic()
        {
            ////if current price is less than the lowest price of last two weeks
            //// if current price is close to days low
            ////if current price like -9% less than minimum price of last two weeks ( too much)

            //if (sig.CurrPr < sig.MinRef1Day && sig.IsCloseToDayLow && sig.PrDiffCurrAndHighPerc < sig.PercBelowDayHighToBuy)
            //{
            //    sig.IsBestTimeToBuyAtDayLowest = true;
            //    logger.Info(sig.Symbol + "  " + StrTradeTime + " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(5, ' ')
            //        + " < Ref 1day candle mins " + sig.MinRef1Day.Rnd(5).ToString().PadRight(5, ' '));
            //    continue;
            //}
            //else
            //{
            //    sig.IsBestTimeToBuyAtDayLowest = false;
            //}

            //if (sig.IsBestTimeToBuyAtDayLowest == false)
            //{
            //    if (sig.CurrPr < sig.MinRef4Hour && sig.IsCloseToDayLow && sig.CurrPr.GetDiffPercBetnNewAndOld(sig.MinRef4Hour) < sig.PercBelowDayHighToBuy)
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = true;
            //        logger.Info(sig.Symbol + "  " + StrTradeTime + " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(5, ' ')
            //            + " < Ref 4hr candle mins " + sig.MinRef4Hour.Rnd(5).ToString().PadRight(5, ' '));
            //        continue;
            //    }
            //    else
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = false;
            //    }
            //}
            //if (sig.IsBestTimeToBuyAtDayLowest == false)
            //{
            //    if (sig.CurrPr < sig.MinRef1Hour && sig.IsCloseToDayLow && sig.CurrPr.GetDiffPercBetnNewAndOld(sig.MinRef1Hour) < sig.PercBelowDayHighToBuy)
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = true;
            //        logger.Info(sig.Symbol + "  " + StrTradeTime + " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(5, ' ')
            //            + " < Ref 1hr candle mins " + sig.MinRef1Hour.Rnd(5).ToString().PadRight(5, ' '));
            //        continue;
            //    }
            //    else
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = false;
            //    }
            //}
            //if (sig.IsBestTimeToBuyAtDayLowest == false)
            //{
            //    if (sig.CurrPr < sig.MinRef30Min && sig.IsCloseToDayLow && sig.CurrPr.GetDiffPercBetnNewAndOld(sig.MinRef30Min) < sig.PercBelowDayHighToBuy)
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = true;
            //        logger.Info(sig.Symbol + "  " + StrTradeTime + " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(5, ' ')
            //            + " < Ref 30  candle mins " + sig.MinRef30Min.Rnd(5).ToString().PadRight(5, ' '));

            //        continue;

            //    }
            //    else
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = false;
            //    }
            //}

            //if (sig.IsBestTimeToBuyAtDayLowest == false)
            //{
            //    if (sig.PrChPercCurrAndRef1Day < -20M && sig.IsCloseToDayLow)
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = true;
            //        logger.Info(sig.Symbol + "  " + StrTradeTime + " PrChPercCurrAndRef1Day " + sig.PrChPercCurrAndRef1Day.Rnd(5).ToString().PadRight(5, ' ')
            //            + " < 20 ");
            //        continue;
            //    }
            //    else
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = false;
            //    }
            //}
            //if (sig.IsBestTimeToBuyAtDayLowest == false)
            //{

            //    if (sig.PrChPercCurrAndRef4Hour < -18M && sig.IsCloseToDayLow)
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = true;
            //        logger.Info(sig.Symbol + "  " + StrTradeTime + " PrChPercCurrAndRef4Hour " + sig.PrChPercCurrAndRef4Hour.Rnd(5).ToString().PadRight(5, ' ')
            //            + " < 18 ");
            //        continue;
            //    }
            //    else
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = false;
            //    }
            //}
            //if (sig.IsBestTimeToBuyAtDayLowest == false)
            //{

            //    if (sig.PrChPercCurrAndRef1Hour < -16M && sig.IsCloseToDayLow)
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = true;
            //        logger.Info(sig.Symbol + "  " + StrTradeTime + " PrChPercCurrAndRef1Hour " + sig.PrChPercCurrAndRef1Hour.Rnd(5).ToString().PadRight(5, ' ')
            //            + " < 16 ");
            //        continue;
            //    }
            //    else
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = false;
            //    }
            //}
            //if (sig.IsBestTimeToBuyAtDayLowest == false)
            //{

            //    if (sig.PrChPercCurrAndRef30Min < -14M && sig.IsCloseToDayLow)
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = true;
            //        logger.Info(sig.Symbol + "  " + StrTradeTime + " PrChPercCurrAndRef30Min " + sig.PrChPercCurrAndRef30Min.Rnd(5).ToString().PadRight(5, ' ')
            //            + " < 14 ");
            //        continue;
            //    }
            //    else
            //    {
            //        sig.IsBestTimeToBuyAtDayLowest = false;
            //    }

            //}

            //if (sig.CurrPr > sig.DayAveragePr)
            //{
            //    sig.IsBestTimeToBuyAtDayLowest = false;
            //}

            //private int GetTotalConsecutiveUpOrDown(List<SignalCandle> candleList, string direction)
            //{
            //    // var mintime = candleList.Min(x => x.CloseTime);
            //    // var avgPriceOfCandles = candleList.Average(x => x.ClosePrice);

            //    if (candleList == null || candleList.Count == 0) return 0;

            //    int TotalConsecutiveChanges = 0;

            //    candleList = candleList.OrderByDescending(x => x.CloseTime).ToList();

            //    bool directionCondition = false;

            //    for (int i = 0; i < candleList.Count - 1; i++)
            //    {
            //        if (direction == "up")
            //        {
            //            if (candleList[i].ClosePrice > candleList[i + 1].ClosePrice)
            //            {
            //                directionCondition = true;
            //            }
            //            else if (i + 2 < candleList.Count)
            //            {

            //                if (candleList[i].ClosePrice > candleList[i + 2].ClosePrice)
            //                {
            //                    directionCondition = true;
            //                }
            //                else if (i + 3 < candleList.Count)
            //                {

            //                    if (candleList[i].ClosePrice > candleList[i + 3].ClosePrice)
            //                    {
            //                        directionCondition = true;
            //                    }
            //                    else
            //                    {
            //                        directionCondition = false;
            //                    }
            //                }
            //                else
            //                {
            //                    directionCondition = false;
            //                }
            //            }
            //            else
            //            {
            //                directionCondition = false;
            //            }
            //        }
            //        else
            //        {
            //            if (candleList[i].ClosePrice <= candleList[i + 1].ClosePrice)
            //            {
            //                directionCondition = true;
            //            }
            //            else if (i + 2 < candleList.Count)
            //            {

            //                if (candleList[i].ClosePrice <= candleList[i + 2].ClosePrice)
            //                {
            //                    directionCondition = true;
            //                }
            //                else if (i + 3 < candleList.Count)
            //                {

            //                    if (candleList[i].ClosePrice <= candleList[i + 3].ClosePrice)
            //                    {
            //                        directionCondition = true;
            //                    }
            //                    else
            //                    {
            //                        directionCondition = false;
            //                    }
            //                }
            //                else
            //                {
            //                    directionCondition = false;
            //                }
            //            }
            //            else
            //            {
            //                directionCondition = false;
            //            }
            //        }

            //        if (directionCondition)
            //            TotalConsecutiveChanges++;
            //        else
            //            break;
            //    }

            //    return TotalConsecutiveChanges;
            //}
        }

        //private bool IsCoinPriceNotGoingUp(Signal sig)
        //{

        //    var LastFive_OneMinCandles = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime).Take(5);

        //    var IsOneMinOnDownTrend = LastFive_OneMinCandles.First().ClosePrice < LastFive_OneMinCandles.Last().ClosePrice;

        //    var LastThree_FiveMinCandles = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime).Take(3);

        //    var IsFiveMinOnDownTrend = LastThree_FiveMinCandles.First().ClosePrice < LastThree_FiveMinCandles.Last().ClosePrice;

        //    var LastTwo_FifteenMinCandles = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

        //    var IsFifteenMinOnDownTrend = LastTwo_FifteenMinCandles.First().ClosePrice < LastTwo_FifteenMinCandles.Last().ClosePrice;

        //    // prices are going down. Dont buy till you see recovery
        //    if (IsOneMinOnDownTrend|| IsFiveMinOnDownTrend|| IsFifteenMinOnDownTrend)
        //    {
        //        logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
        //          " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
        //          "  Prices not going up. Wait till you see uptrend" +
        //          " 1up < 2  ? " + sig.TotalConsecutive1MinUps.ToString().PadRight(7, ' ') + " " 
        //          );


        //        return true;
        //    }
        //    return false;
        //}

        private void CreateScalpBuySignals()
        {
            //foreach (var sig in MySignals)
            //{
            //    try
            //    {

            //        //if (sig.CurrPr <= 0)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = false;
            //        //    continue;
            //        //}

            //        //var OneMinCandles = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime).Take(5);

            //        //var IsOneMinOnUpTrend = OneMinCandles.First().ClosePrice >= OneMinCandles.Max(x => x.ClosePrice);

            //        //var FiveMinCandles = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime).Take(4);

            //        //var IsFiveMinOnUpTrend = FiveMinCandles.First().ClosePrice >= FiveMinCandles.Max(x => x.ClosePrice);

            //        //var FifteenMinCandles = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

            //        //var IsFifteenMinOnUpTrend = FifteenMinCandles.First().ClosePrice >= FifteenMinCandles.Max(x => x.ClosePrice);

            //        //// prices are going down. Dont buy till you see recovery
            //        //if (IsOneMinOnUpTrend && IsFiveMinOnUpTrend && IsFifteenMinOnUpTrend)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;


            //        //}
            //        //if (sig.IsBestTimeToBuyAtDayLowest)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    sig.PrChPercCurrAndRef30Min=-70M; // setting is so low so that this gets preference to buy
            //        //    sig.PrChPercCurrAndRef4Hour = -70M;
            //        //    sig.PrChPercCurrAndRef1Hour = -70M;
            //        //    sig.PrChPercCurrAndRef15Min = -70M;
            //        //    continue;
            //        //}

            //        //if (sig.Ref4HourCandles == null || sig.Ref4HourCandles.Count < 6)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = false;
            //        //    continue;
            //        //}
            //        //if (sig.Ref1HourCandles == null || sig.Ref1HourCandles.Count < 23)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = false;
            //        //    continue;
            //        //}
            //        //if (sig.Ref30MinCandles == null || sig.Ref30MinCandles.Count < 17)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = false;
            //        //    continue;
            //        //}
            //        //if (sig.Ref15MinCandles == null || sig.Ref15MinCandles.Count < 15)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = false;
            //        //    continue;
            //        //}
            //        //if (sig.Ref5MinCandles == null || sig.Ref5MinCandles.Count < 11)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = false;
            //        //    continue;
            //        //}



            //        //if (sig.PrChPercCurrAndRef4Hour < configr.ScalpFourHourDiffLessThan) //-4M
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}
            //        //if (sig.PrChPercCurrAndRef1Hour < configr.ScalpOneHourDiffLessThan)//-4M
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}
            //        //if (sig.PrChPercCurrAndRef30Min < configr.ScalpThirtyMinDiffLessThan)//-4M
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}
            //        //if (sig.PrChPercCurrAndRef15Min < configr.ScalpFifteenMinDiffLessThan)//-3M
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}
            //        //if (sig.PrChPercCurrAndRef5Min < configr.ScalpFiveMinDiffLessThan) //-3M
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}

            //        //if (sig.PrDiffCurrAndHighPerc >= -2M)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = false;
            //        //    continue;
            //        //}

            //        //if (sig.PrDiffHighAndLowPerc <= 3M)
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = false;
            //        //    continue;
            //        //}

            //        ////if (sig.CurrPr >= ((sig.DayHighPr + sig.DayAveragePr) / configr.DivideHighAndAverageBy))
            //        ////{
            //        ////    sig.IsBestTimeToScalpBuy = false;
            //        ////    continue;
            //        ////}

            //        //if (sig.TotalConsecutive4HourDowns >= configr.ScalpFourHourDownMoreThan) //3
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}

            //        //if (sig.TotalConsecutive1HourDowns >= configr.ScalpOneHourDownMoreThan) //4
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}

            //        //if (sig.TotalConsecutive30MinDowns >= configr.ScalpThirtyMinDownMoreThan) //4
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}

            //        //if (sig.TotalConsecutive15MinDowns >= configr.ScalpFifteenMinDownMoreThan) //4
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}

            //        //if (sig.TotalConsecutive5MinDowns >= configr.ScalpFiveMinDownMoreThan) //5
            //        //{
            //        //    sig.IsBestTimeToScalpBuy = true;
            //        //    continue;
            //        //}

            //        sig.IsBestTimeToScalpBuy = false;
            //    }
            //    catch (Exception ex)
            //    {

            //        logger.Info("Exception at scalp buy signal generators " + sig.Symbol + " " + ex.Message);
            //        throw;
            //    }
            //}
        }

        #endregion




    }
}

/*
 * 
 * 
 *  private async Task PerformBuys(IOrderedEnumerable<SignalIndicator> SignalGeneratorList)
        {
            DB TradeDB = new DB();

            var tradebots = await TradeDB.TradeBot.OrderBy(x => x.Id).ToListAsync();

            #region buying scan

            var alreadyboughtCoins = tradebots.Where(x => x.Pair != null).Select(x => x.Pair);

            for (int i = 0; i < tradebots.Count(); i++)
            {
                if (tradebots[i].IsActivelyTrading) //trading, go to the next one
                {
                    continue;
                }
                if (tradebots[i].Order == 1)
                {
                    // first bot in the group and not actively trading, so no refence amounts to trade with,
                    //this bot will scan the market condition for a favorable buy.
                    //In the future, it should actively try to buy when the price of the coin is at its lowest.

                    foreach (var indicator in SignalGeneratorList)
                    {
                        if (indicator.IsPicked) continue;
                        if (alreadyboughtCoins.Contains(indicator.Symbol))
                        {
                            continue;
                        }
                        var indicatorcurrentprice = indicator.CurrentPrice;
                        var indicatorSymbol = indicator.Symbol;
                        var indicatoroldprice = indicator.ReferenceSetAverageCurrentPrice;

                        var pricedifferencepercentage = (indicatorcurrentprice - indicatoroldprice) /
                        ((indicatorcurrentprice + indicatoroldprice / 2)) * 100;

                        if (
                            pricedifferencepercentage < 0 &&
                            Math.Abs(pricedifferencepercentage) > tradebots[i].BuyWhenValuePercentageIsBelow
                            )
                        {
                            tradebots[i].IsActivelyTrading = true;
                            tradebots[i].Pair = indicator.Symbol;
                            tradebots[i].DayHigh = indicator.DayHighPrice;
                            tradebots[i].DayLow = indicator.DayLowPrice;
                            tradebots[i].CreatedDate = DateTime.Now;
                            tradebots[i].BuyPricePerCoin = indicator.CurrentPrice;
                            tradebots[i].QuantityBought = tradebots[i].AvailableAmountForTrading / indicator.CurrentPrice;
                            tradebots[i].BuyingCommision = tradebots[i].AvailableAmountForTrading * 0.075M / 100;
                            tradebots[i].TotalBuyCost = tradebots[i].AvailableAmountForTrading + tradebots[i].BuyingCommision;
                            tradebots[i].CurrentPricePerCoin = indicator.CurrentPrice;
                            tradebots[i].TotalCurrentValue = tradebots[i].AvailableAmountForTrading;
                            tradebots[i].TotalCurrentProfit = 0;
                            tradebots[i].BuyTime = DateTime.Now;
                            tradebots[i].AvailableAmountForTrading = 0;
                            TradeDB.TradeBot.Update(tradebots[i]);

                            await TradeDB.SaveChangesAsync();
                            indicator.IsPicked = true;
                            // Update buy record, set it active, in live system, you will be issuing a buy order
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    //not the first bot, so the previous bot will be actively trading. The lower ones are support bots, buy only when the current prices are so much lower than the first bot

                    var previousCoinPrice = tradebots[i - 1].BuyPricePerCoin;
                    var previousCoinPair = tradebots[i - 1].Pair;
                    DateTime PreviousCoinBuyTime = Convert.ToDateTime(tradebots[i - 1].BuyTime);

                    foreach (var indicator in SignalGeneratorList)
                    {
                        if (indicator.IsPicked) continue;
                        var indicatorcurrentprice = indicator.CurrentPrice;
                        var indicatorSymbol = indicator.Symbol;

                        var closestcandles = await TradeDB.Candle.Where(
                            x => x.Symbol == indicator.Symbol &&
                            x.RecordedTime.Date == PreviousCoinBuyTime.Date &&
                            x.RecordedTime.Hour == PreviousCoinBuyTime.Hour
                            ).ToListAsync();

                        long min = long.MaxValue;

                        Candle selectedCandle = new Candle();

                        foreach (var candidatecandle in closestcandles)
                        {
                            if (Math.Abs(PreviousCoinBuyTime.Ticks - candidatecandle.RecordedTime.Ticks) < min)
                            {
                                min = Math.Abs(PreviousCoinBuyTime.Ticks - candidatecandle.RecordedTime.Ticks);
                                selectedCandle = candidatecandle;
                            }
                        }
                        var indicatoroldprice = selectedCandle.CurrentPrice; //[TO DO] - Relook at this line

                        var pricedifference = (indicatorcurrentprice - indicatoroldprice) / ((indicatorcurrentprice + indicatoroldprice / 2)) * 100;

                        if (pricedifference < 0 && Math.Abs(pricedifference) > tradebots[i].BuyWhenValuePercentageIsBelow)
                        {
                            //buy
                            tradebots[i].IsActivelyTrading = true;
                            tradebots[i].Pair = indicator.Symbol;
                            tradebots[i].DayHigh = indicator.DayHighPrice;
                            tradebots[i].DayLow = indicator.DayLowPrice;
                            tradebots[i].CreatedDate = DateTime.Now;
                            tradebots[i].BuyPricePerCoin = indicator.CurrentPrice;
                            tradebots[i].QuantityBought = tradebots[i].AvailableAmountForTrading / indicator.CurrentPrice;
                            tradebots[i].BuyingCommision = tradebots[i].AvailableAmountForTrading * 0.075M / 100;
                            tradebots[i].TotalBuyCost = tradebots[i].AvailableAmountForTrading + tradebots[i].BuyingCommision;

                            tradebots[i].CurrentPricePerCoin = indicator.CurrentPrice;
                            tradebots[i].TotalCurrentValue = tradebots[i].AvailableAmountForTrading;
                            tradebots[i].TotalCurrentProfit = 0;
                            tradebots[i].BuyTime = DateTime.Now;
                            tradebots[i].AvailableAmountForTrading = 0;
                            indicator.IsPicked = true;
                            // Update buy record, set it active, in live system, you will be issuing a buy order
                            // Update buy record, set it active, in live system, you will be issuing a buy order
                        }
                        else
                        {
                            continue;
                        }
                    }
                }

            }

            #endregion buying scan
        }

        private async Task PerformSells(IOrderedEnumerable<SignalIndicator> SignalGeneratorList)
        {
            DB TradeDB = new DB();

            #region selling scan
            var tradebots = await TradeDB.TradeBot.OrderBy(x => x.Id).ToListAsync();

            var botgroups = tradebots.OrderByDescending(x => x.Order).GroupBy(x => x.Name).ToList();

            // see when a bot batch made more than 5 % profit. Later you can change these to be configurable.
            foreach (var botgroup in botgroups)
            {
                decimal? totalbuyingprice = 0;
                decimal? totalcurrentprice = 0;

                foreach (var bot in botgroup)
                {
                    if (!bot.IsActivelyTrading) // not in trading, so cannot sell
                    {
                        continue;
                    }

                    // collect buying price of each coin in the group and collect quantity bought
                    // collect current price
                    // if the total current price gives you more than 4% profit sell it.

                    decimal? BuyingCoinPrice = bot.BuyPricePerCoin;
                    var CoinPair = bot.Pair;
                    decimal quanitybought = Convert.ToDecimal(bot.QuantityBought);
                    decimal? buyingcommision = bot.BuyingCommision;

                    totalbuyingprice += (BuyingCoinPrice * quanitybought + buyingcommision);

                    foreach (var indicator in SignalGeneratorList)
                    {
                        if (indicator.Symbol == CoinPair)
                        {
                            var currentPrice = indicator.CurrentPrice;
                            totalcurrentprice += (indicator.CurrentPrice * quanitybought) + ((indicator.CurrentPrice * quanitybought) * 0.075M / 100);
                            break;
                        }
                    }
                }

                if (totalbuyingprice == 0) // no trades happening in the group, go the next bot group.
                {
                    continue;
                }

                var pricedifference = (totalcurrentprice - totalbuyingprice) / ((totalcurrentprice + totalbuyingprice) / 2) * 100;

                //Your total profit is more than 5%. Sell it and get ready to buy again.
                if (pricedifference > 5)
                {
                    foreach (var bot in botgroup)
                    {
                        var CoinPair = bot.Pair;
                        var CoinIndicator = SignalGeneratorList.Where(x => x.Symbol == CoinPair).FirstOrDefault();
                        bot.DayHigh = CoinIndicator.DayHighPrice;
                        bot.DayLow = CoinIndicator.DayLowPrice;
                        bot.CurrentPricePerCoin = CoinIndicator.CurrentPrice;
                        bot.TotalCurrentValue = bot.CurrentPricePerCoin * bot.QuantityBought;
                        bot.QuantitySold = Convert.ToDecimal(bot.QuantityBought);
                        bot.SoldCommision = bot.CurrentPricePerCoin * bot.QuantityBought * 0.075M / 100;
                        bot.TotalSoldAmount = bot.TotalCurrentValue - bot.SoldCommision;
                        bot.AvailableAmountForTrading = bot.TotalSoldAmount;
                        bot.TotalCurrentProfit = bot.TotalSoldAmount - bot.TotalBuyCost;
                        bot.SellTime = DateTime.Now;
                        bot.UpdatedTime = DateTime.Now;

                        // create sell order (in live system)
                        // copy the record to history

                        TradeBotHistory tradeBotHistory = iMapper.Map<TradeBot, TradeBotHistory>(bot);

                        await TradeDB.TradeBotHistory.AddAsync(tradeBotHistory);


                        // reset records to buy again

                        bot.DayHigh = 0.0M;
                        bot.DayLow = 0.0M;
                        bot.Pair = string.Empty;
                        bot.BuyPricePerCoin = 0.0M;
                        bot.CurrentPricePerCoin = 0.0M;
                        bot.QuantityBought = 0.0M;
                        bot.TotalBuyCost = 0.0M;
                        bot.TotalCurrentValue = 0.0M;
                        bot.TotalSoldAmount = 0.0M;
                        bot.BuyTime = null;
                        bot.CreatedDate = null;
                        bot.SellTime = null;
                        bot.BuyingCommision = 0.0M;
                        bot.SoldPricePricePerCoin = 0.0M;
                        bot.TotalCurrentProfit = 0.0M;
                        bot.QuantitySold = 0.0M;
                        bot.SoldCommision = 0.0M;
                        bot.TotalCurrentProfit = 0.0M;
                        bot.IsActivelyTrading = false;

                        TradeDB.TradeBot.Update(bot);
                        await TradeDB.SaveChangesAsync();


                        // update record fully.

                        // In the future write code to wait and see if the prices keep going up before selling abruptly.
                        //Only when you have made sufficiently sure that prices will not go higher, then sell them.
                    }

                }

            }



            #endregion selling scan
        }

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


