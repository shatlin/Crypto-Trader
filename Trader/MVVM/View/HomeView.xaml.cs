
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
        public List<MyCoins> MyCoins { get; set; }
        public DispatcherTimer TradeTimer;
        public DateTime TradeTime { get; set; }
        public string StrTradeTime { get; set; }
        public string NextTradeTime { get; set; }
        BinanceClient client;
        public List<PlayerViewModel> PlayerViewModels;
        public int UpdatePrecisionCounter = 0;
        ILog logger;
        IMapper iPlaymerMapper;
        List<string> boughtCoins = new List<string>();
        List<Signal> CurrentSignals = new List<Signal>();
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

            iPlaymerMapper = playerMapConfig.CreateMapper();
            socket = new InstanceBinanceWebSocketClient(client);
            CurrentSignals = new List<Signal>();

            // MyCoins = await db.MyCoins.AsNoTracking().Take(30).ToListAsync();
            await GetMyCoins();
            await SetGrid();

            // await GetAllUSDTPairs();
            await UpdateAllowedPrecisionsForPairs();
            // await RemoveOldCandles();
            // await GetAccountInfo();
            GetSignals();
            TradeTimer.Start();
            logger.Info("Application Ready and Timer Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            logger.Info("");
        }

        private void EnsureSocketRunning()
        {
            Parallel.ForEach(MyCoins, coin =>
            {
                foreach (var sig in CurrentSignals)
                {
                    if (sig.Symbol == coin.Coin)
                    {
                        if (sig.IsSymbolTickerSocketRunning == false)
                        {
                            // tasks.Add(Task.Factory.StartNew(() =>
                            // {
                            try
                            {
                                if (sig.TickerSocketGuid != Guid.Empty) socket.CloseWebSocketInstance(sig.TickerSocketGuid);


                                sig.TickerSocketGuid = socket.ConnectToIndividualSymbolTickerWebSocket(coin.Coin, b => { sig.CurrPr = b.LastPrice; sig.IsSymbolTickerSocketRunning = true; });

                                logger.Info("Ticker socket started for " + coin.Coin);
                            }
                            catch
                            {
                                sig.IsSymbolTickerSocketRunning = false;
                                logger.Info("exception at Ticker socket for " + coin.Coin);
                                Thread.Sleep(100);
                            }
                            //  }));
                        }

                        if (sig.IsDailyKlineSocketRunning == false)
                        {
                            // tasks.Add(Task.Factory.StartNew(() =>
                            //{
                            try
                            {
                                if (sig.KlineSocketGuid != Guid.Empty) socket.CloseWebSocketInstance(sig.KlineSocketGuid);


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
                                sig.JustRecoveredFromDayLow = sig.PrDiffCurrAndLowPerc >= 1M && sig.PrDiffCurrAndLowPerc <= 2M;
                                sig.IsAtDayLow = sig.PrDiffCurrAndLowPerc >= 0.5M && sig.PrDiffCurrAndLowPerc <= 1.5M;
                                sig.PercBelowDayHighToBuy = coin.PercBelowDayHighToBuy;
                                sig.PercAboveDayLowToSell = coin.PercAboveDayLowToSell;
                                // has gone to the bottom and gone up a bit. Then we know the lowest is reached
                                sig.IsAtDayHigh = sig.PrDiffCurrAndHighPerc <= -0.5M && sig.PrDiffCurrAndHighPerc >= -1.5M;
                                    // has gone to the top and gone down a little bit. Then we know the higest is reached
                                    sig.IsDailyKlineSocketRunning = true;
                            });

                                logger.Info("Kline  socket started for " + coin.Coin);

                            }
                            catch
                            {
                                sig.IsDailyKlineSocketRunning = false;
                                logger.Info("exception at Kline socket for " + coin.Coin);
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

            foreach (var coin in MyCoins)
            {
                Signal sig = new Signal();
                sig.IsSymbolTickerSocketRunning = false;
                sig.IsDailyKlineSocketRunning = false;
                sig.Symbol = coin.Coin;
                sig.RefFiveMinCandles = new List<SignalCandle>();
                sig.RefHourCandles = new List<SignalCandle>();
                sig.PercBelowDayHighToBuy = coin.PercBelowDayHighToBuy;
                sig.PercAboveDayLowToSell = coin.PercAboveDayLowToSell;
                CurrentSignals.Add(sig);
            }

            EnsureSocketRunning();

            logger.Info("Get signals completed");
            logger.Info("");
        }

        private void CollectReferenceCandles()
        {
            foreach (var sig in CurrentSignals)
            {
                using (var db = new DB())
                {
                    List<SignalCandle> signalCandles = db.SignalCandle.Where(x => x.Pair == sig.Symbol).ToList();
                    if (sig.RefFiveMinCandles.Count < 6)
                    {
                        foreach (var refcndl in signalCandles)
                        {
                            sig.RefFiveMinCandles.Add(refcndl);
                        }
                    }
                }

                if (DateTime.Now.Minute % 5 == 0) //[PROD] Collect 5 minute Candles DateTime.Now.Minute % 5 == 0
                {
                    var currentDate = DateTime.Now;
                    DateTime time = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, currentDate.Minute, 0);

                    if (sig.RefFiveMinCandles.Any(x => x.CloseTime == time)) sig.RefFiveMinCandles.Where(x => x.CloseTime == time).First().ClosePrice = sig.CurrPr;

                    else
                    {
                        int lastid = 1;
                        if (sig.RefFiveMinCandles.Any()) lastid = sig.RefFiveMinCandles.Max(x => x.Id) + 1;
                        sig.RefFiveMinCandles.Add(new SignalCandle { SeqNo = lastid, Pair = sig.Symbol, CloseTime = time, ClosePrice = sig.CurrPr });
                        if (sig.RefFiveMinCandles.Count > 12)
                        {
                            sig.RefFiveMinCandles.RemoveAt(0);
                        }
                        using (var db = new DB())
                        {
                            db.Database.ExecuteSqlRaw("delete from SignalCandle where Pair='" + sig.Symbol + "'");
                            foreach (SignalCandle cndl in sig.RefFiveMinCandles)
                            {
                                cndl.Id = 0;
                                db.SignalCandle.Add(cndl);
                            }
                            db.SaveChanges();
                        }

                    }
                }

                if (DateTime.Now.Minute == 1) // Collect 1 hour candles
                {
                    var currentDate = DateTime.Now;
                    DateTime time = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, currentDate.Hour, 0, 0);

                    if (sig.RefHourCandles.Any(x => x.CloseTime == time)) sig.RefHourCandles.Where(x => x.CloseTime == time).First().ClosePrice = sig.CurrPr;
                    else
                    {
                        int nextid = 1;
                        if (sig.RefHourCandles.Any()) nextid = sig.RefHourCandles.Max(x => x.Id) + 1;
                        sig.RefHourCandles.Add(new SignalCandle { SeqNo = nextid, Pair = sig.Symbol, CloseTime = time, ClosePrice = sig.CurrPr });
                        if (sig.RefHourCandles.Count > 12) sig.RefHourCandles.RemoveAt(0);
                    }
                }

                List<SignalCandle> fivemincandles = sig.RefFiveMinCandles;

                if (fivemincandles.Count <= 6)
                {
                    sig.IsBestTimeToScalpBuy = false; // havent collected enough candles to assess scalp buys. Wait till 30 minutes to see how market is going before buying
                }
                else
                {
                    var mintime = fivemincandles.Min(x => x.CloseTime);
                    sig.PriceChangeInLastHour = sig.CurrPr.GetDiffPercBetnNewAndOld(fivemincandles.Where(x => x.CloseTime == mintime).First().ClosePrice);

                    sig.TotalPreviousDowns = 0;
                    sig.TotalPreviousUps = 0;

                    fivemincandles = sig.RefFiveMinCandles.OrderByDescending(x => x.CloseTime).Take(6).ToList();

                    for (int i = 0; i < fivemincandles.Count - 1; i++)
                    {
                        if (fivemincandles[i].ClosePrice <= fivemincandles[i + 1].ClosePrice)
                        {
                            sig.TotalPreviousDowns++;
                        }
                        else
                        {
                            sig.TotalPreviousUps++;
                        }
                    }

                    if (
                        fivemincandles[0].ClosePrice <= fivemincandles[1].ClosePrice &&
                        fivemincandles[1].ClosePrice <= fivemincandles[2].ClosePrice &&
                        fivemincandles[2].ClosePrice <= fivemincandles[3].ClosePrice
                        )
                    {
                        sig.isLastThreeFiveMinsGoingDown = true;
                    }
                    else
                    {
                        sig.isLastThreeFiveMinsGoingDown = false;
                    }

                    if (
                        fivemincandles[0].ClosePrice <= fivemincandles[1].ClosePrice &&
                        fivemincandles[1].ClosePrice <= fivemincandles[2].ClosePrice

                        )
                    {
                        sig.isLastTwoFiveMinsGoingDown = true;
                    }
                    else
                    {
                        sig.isLastTwoFiveMinsGoingDown = false;
                    }

                    if (
                        fivemincandles[0].ClosePrice >= fivemincandles[1].ClosePrice &&
                        fivemincandles[1].ClosePrice >= fivemincandles[2].ClosePrice &&
                        fivemincandles[2].ClosePrice >= fivemincandles[3].ClosePrice
                        )
                    {
                        sig.isLastThreeFiveMinsGoingUp = true;
                    }
                    else
                    {
                        sig.isLastThreeFiveMinsGoingUp = false;
                    }


                }

            }

        }

        private void CreateBuyLowestSellHighestSignals()
        {

            //[QA]
            //foreach (var sig in CurrentSignals)
            //{
            //    sig.IsBestTimeToBuyAtDayLowest = sig.CurrPr > 0 && sig.PrDiffCurrAndHighPerc < -8M && sig.JustRecoveredFromDayLow;
            //    sig.IsBestTimeToSellAtDayHighest = sig.CurrPr > 0 && sig.PrDiffHighAndLowPerc > 8M && sig.IsAtDayHigh;
            //}

            //[PROD]
            foreach (var sig in CurrentSignals)
            {
                //To buy at lowest
                //1. There should be at least 6% difference between high and low
                //2. Current price should be at least 6% lower than the high price
                //3. High Percentage should be atleast 6% bigger than low percentage
                sig.IsBestTimeToBuyAtDayLowest = sig.CurrPr > 0 && sig.PrDiffCurrAndHighPerc < sig.PercBelowDayHighToBuy && sig.PrDiffHighAndLowPerc > sig.PercAboveDayLowToSell && sig.IsAtDayLow;

                //To sell at highest

                //1. There should be at least 6% difference between high and low
                sig.IsBestTimeToSellAtDayHighest = sig.CurrPr > 0 && sig.PrDiffHighAndLowPerc > sig.PercAboveDayLowToSell && sig.IsAtDayHigh;
            }
        }

        private void CreateScalpBuySignals()
        {

            foreach (var sig in CurrentSignals)
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

                if (sig.PriceChangeInLastHour > -2M)
                {
                    sig.IsBestTimeToScalpBuy = false;
                    continue;
                }

                if (sig.PrDiffCurrAndHighPerc >= -3M)
                {
                    sig.IsBestTimeToScalpBuy = false;
                    continue;
                }

                if (sig.PrDiffHighAndLowPerc <= 3M)
                {
                    sig.IsBestTimeToScalpBuy = false;
                    continue;
                }

                if (!sig.isLastTwoFiveMinsGoingDown)
                {
                    sig.IsBestTimeToScalpBuy = false;
                    continue;
                }

                //[QA]

                //sig.IsBestTimeToScalpBuy = (sig.CurrPr > 0 && sig.CurrPr <= (sig.DayHighPr + sig.DayAveragePr / 2.8M) && sig.PrDiffCurrAndHighPerc < -5M) ||
                //                            (sig.CurrPr > 0 && sig.IsCloseToDayLow && sig.PrDiffCurrAndHighPerc < -4M);

                //[PROD]

                // it would be best time to scalp buy when
                //1. The price is at its lowest for the day
                //2 . (sig.CurrPr <= (sig.DayHighPr + sig.DayAveragePr) / 2.1M && sig.PriceChangeInLastHour < -1.2M)
                //3. (sig.IsCloseToDayLow && sig.PriceChangeInLastHour < -1.2M)
                //4. Last 3 candles are down and price went down less than -1.2M
                //5. Current price should be at least 3% lower than the highest price of the day
                //6. High percent should be atleast 3% higher than low percent
                if ((sig.CurrPr <= (sig.DayHighPr + sig.DayAveragePr) / configr.DivideHighAndAverageBy) || sig.IsCloseToDayLow)
                    sig.IsBestTimeToScalpBuy = true;
                else
                    sig.IsBestTimeToScalpBuy = false;
            }
        }

        private void LogDecisions()
        {
            logger.Info("");


            logger.Info("");
            logger.Info("Buyables");
            logger.Info("--------");

            foreach (var sig in CurrentSignals.OrderBy(x => x.PrDiffCurrAndHighPerc))
            {
                string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(6, ' ') +
                                 " DHi " + sig.DayHighPr.Rnd(5).ToString().PadRight(11, ' ') +
                                 " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(11, ' ') +
                                 " DLo " + sig.DayLowPr.Rnd(5).ToString().PadRight(11, ' ') +
                                 " DiCr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(5).ToString().PadRight(11, ' ') +
                                 " DiCr&Lw " + sig.PrDiffCurrAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') +
                                 " Dihi&Lw " + sig.PrDiffHighAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') +
                                 " hi+avg/2.1 : " + ((sig.DayHighPr + sig.DayAveragePr) / configr.DivideHighAndAverageBy).Rnd(5).ToString().PadRight(11, ' ') +
                                 " ClsToLow : " + sig.IsCloseToDayLow.ToString().PadRight(8, ' ') +
                                 " PrCh " + sig.PriceChangeInLastHour.Rnd(5).ToString().PadRight(8, ' ');

                if (sig.IsBestTimeToBuyAtDayLowest)
                    log += " Day Buy : Yes ";
                //if (sig.IsBestTimeToScalpBuy)
                //    log += " Scalp Buy : Yes ";
                if (sig.IsBestTimeToBuyAtDayLowest) //|| sig.IsBestTimeToScalpBuy
                    logger.Info(log);
            }

            logger.Info("");
            logger.Info("Not Buyables");
            logger.Info("--------");
            foreach (var sig in CurrentSignals.OrderBy(x => x.PrDiffCurrAndLowPerc))
            {
                string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(6, ' ') +
                               " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(11, ' ') +
                               " DLo " + sig.DayLowPr.Rnd(5).ToString().PadRight(11, ' ') +
                               " DHi " + sig.DayHighPr.Rnd(5).ToString().PadRight(11, ' ') +
                               " DiCr&LwPrc " + sig.PrDiffCurrAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') +
                               " PrDiCr&LwPrc " + sig.PrDiffCurrAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') + " > 0.5 & < 1.5 ? : " + (sig.PrDiffCurrAndLowPerc >= 0.5M && sig.PrDiffCurrAndLowPerc <= 1.5M).ToString().PadRight(6, ' ') +
                               " DiCr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(5).ToString().PadRight(9, ' ') + " < "+sig.PercBelowDayHighToBuy.Rnd(0)+" ? : " + (sig.PrDiffCurrAndHighPerc < sig.PercBelowDayHighToBuy).ToString().PadRight(6, ' ') +
                               " DiHi&Lw " + sig.PrDiffHighAndLowPerc.Rnd(5).ToString().PadRight(9, ' ') + " > "+sig.PercAboveDayLowToSell.Rnd(0) +" ? : " + (sig.PrDiffHighAndLowPerc > sig.PercAboveDayLowToSell).ToString();
                if (!sig.IsBestTimeToBuyAtDayLowest)
                    logger.Info(log);
            }
            //logger.Info("");
            //logger.Info("Scalp Buyables");

            //logger.Info("");
            //foreach (var sig in CurrentSignals.OrderBy(x => x.PrDiffCurrAndHighPerc))
            //{
            //    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(6, ' ') +
            //                     " DHi " + sig.DayHighPr.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " DLo " + sig.DayLowPr.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " DiCr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " DiCr&Lw " + sig.PrDiffCurrAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " Dihi&Lw " + sig.PrDiffHighAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " hi+avg/2.1 : " + ((sig.DayHighPr + sig.DayAveragePr) / 2.1M).Rnd(5).ToString().PadRight(11, ' ') +
            //                     " ClsToLow : " + sig.IsCloseToDayLow.ToString().PadRight(8, ' ') +
            //                     " PrCh " + sig.PriceChangeInLastHour.Rnd(5).ToString().PadRight(8, ' ');
            //    if (sig.IsBestTimeToScalpBuy)
            //        logger.Info(log + " Scalp Buy : Yes ");
            //}

            //logger.Info("");
            //logger.Info("Not Scalp Buyables");
            //logger.Info("");
            //foreach (var sig in CurrentSignals)
            //{
            //    string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(6, ' ') +
            //                     " DHi " + sig.DayHighPr.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " DLo " + sig.DayLowPr.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " DiCr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " DiCr&Lw " + sig.PrDiffCurrAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " Dihi&Lw " + sig.PrDiffHighAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') +
            //                     " hi+avg/2.1 : " + ((sig.DayHighPr + sig.DayAveragePr) / 2.1M).Rnd(5).ToString().PadRight(11, ' ') +
            //                     " ClsToLow : " + sig.IsCloseToDayLow.ToString().PadRight(8, ' ') +
            //                     " 2downs : " + sig.isLastTwoFiveMinsGoingDown.ToString().PadRight(6, ' ') +
            //                     " PrCh " + sig.PriceChangeInLastHour.Rnd(5).ToString().PadRight(8, ' ');

            //    if (!sig.IsBestTimeToScalpBuy)
            //        logger.Info(log + " Scalp Buy : No ");
            //}



            logger.Info("");
            logger.Info("Sellables");
            logger.Info("---------");

            foreach (var sig in CurrentSignals.OrderBy(x => x.PrDiffCurrAndHighPerc))
            {
                string log = sig.Symbol.Replace("USDT", "").ToString().PadRight(6, ' ') +
                                 " DHi " + sig.DayHighPr.Rnd(5).ToString().PadRight(11, ' ') +
                                 " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(11, ' ') +
                                 " DLo " + sig.DayLowPr.Rnd(5).ToString().PadRight(11, ' ') +
                                 " DiCr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(5).ToString().PadRight(11, ' ') +
                                 " DiCr&Lw " + sig.PrDiffCurrAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') +
                                 " Dihi&Lw " + sig.PrDiffHighAndLowPerc.Rnd(5).ToString().PadRight(11, ' ') +
                                 " hi+avg/2.1 : " + ((sig.DayHighPr + sig.DayAveragePr) / configr.DivideHighAndAverageBy).Rnd(5).ToString().PadRight(11, ' ') +
                                 " ClsToLow : " + sig.IsCloseToDayLow.ToString().PadRight(8, ' ') +
                                 " PrCh " + sig.PriceChangeInLastHour.Rnd(5).ToString().PadRight(8, ' ');

                if (sig.IsBestTimeToSellAtDayHighest)
                {
                    logger.Info(log + " Day Sell: Yes");
                }
            }
            logger.Info("");

            //logger.Info("Coins who gave exception while Creating (but probably are running without issue)");
            //logger.Info("");

            //foreach (var currentsocket in socket.AllSockets)
            //{
            //    logger.Info(currentsocket.Url.ToString());

            //}

            foreach (var sig in CurrentSignals)
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

        private async Task BuyTheCoin(Player player, Signal sig)
        {
            DB db = new DB();

            var PriceResponse = await client.GetPrice(sig.Symbol);

            decimal mybuyPrice = PriceResponse.Price;

            LogBuy(player, sig);

            player.Pair = sig.Symbol;

            var coin = MyCoins.Where(x => x.Coin == sig.Symbol).FirstOrDefault();

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

            db.Player.Update(player);

            //Send Buy Order

            PlayerTrades playerHistory = iPlaymerMapper.Map<Player, PlayerTrades>(player);
            playerHistory.Id = 0;
            await db.PlayerTrades.AddAsync(playerHistory);
            await db.SaveChangesAsync();
        }

        private async Task Buy()
        {
            if (CurrentSignals == null || CurrentSignals.Count() == 0)
            {
                logger.Info("  " + StrTradeTime + " no signals found. So cannot proceed for buy process ");
                return;
            }

            await RedistributeBalances();

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
                if (player.AvailableAmountToBuy < configr.MinimumAmountToTradeWith)
                {
                    //logger.Info("  " + StrTradeTime + " " + player.Name + " Avl Amt " + player.AvailableAmountToBuy + " Not enough for buying ");
                    continue;
                }
                if (player.isBuyAllowed == false)
                {
                    continue;
                }
                if (configr.IsBuyingAllowed == false)
                {
                    continue;
                }
                foreach (Signal sig in CurrentSignals.OrderBy(x => x.PrDiffCurrAndHighPerc).ToList())
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

                    if (sig.isLastTwoFiveMinsGoingDown) // prices are going down continuously. Dont buy till you see recovery
                    {
                        sig.IsIgnored = true;
                        continue;
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
                        sig.IsIgnored = true;
                    }
                }
            }
        }

        private async Task Sell(Player player)
        {

            Signal sig = CurrentSignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();
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

            decimal availableQty = await GetAvailQty(player, pair);

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

            if (player.ProfitLossChanges.Length > 160)
            {
                player.ProfitLossChanges = player.ProfitLossChanges.GetLast(160);
            }

            var NextSellbelow = prDiffPerc * 93 / 100;

            // less than sellable percentage. Return
            if (prDiffPerc <= player.SellAbovePerc && ForceSell == false)
            {
                player.SellBelowPerc = player.SellAbovePerc;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            if (prDiffPerc >= player.SellBelowPerc && ForceSell == false)
            {
                player.SellBelowPerc = NextSellbelow;
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
            if ((prDiffPerc < player.LastRoundProfitPerc && sig.IsBestTimeToSellAtDayHighest) || ForceSell == true) // Scalp: (prDiffPerc < player.LastRoundProfitPerc ) || ForceSell == true)
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

                var coinprecison = MyCoins.Where(x => x.Coin == pair).FirstOrDefault().TradePrecision;

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
                // LogNoSellReason(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                player.AvailableAmountToBuy = 0;
                player.LastRoundProfitPerc = prDiffPerc;
                db.Player.Update(player);
            }
            await db.SaveChangesAsync();
            ForceSell = false;
        }

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

                //if (avgAvailAmountForTrading > configr.MaximumAmountForaBot)
                //{
                //    avgAvailAmountForTrading = configr.MaximumAmountForaBot;
                //}
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

            var playerSignal = CurrentSignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();

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

                        if (orderResponse.Status != OrderStatus.Filled)
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

            PlayerTrades PlayerTrades = iPlaymerMapper.Map<Player, PlayerTrades>(player);
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

            db.Player.Update(player);
            await db.SaveChangesAsync();
        }

        public async Task<decimal> GetAvailQty(Player player, string pair)
        {
            decimal availableQty = 0;

            var coin = pair.Replace("USDT", "");

            AccountInformationResponse accinfo = await client.GetAccountInformation();

            var coinAvailable = accinfo.Balances.Where(x => x.Asset == coin).FirstOrDefault();


            if (coinAvailable != null)
            {
                availableQty = coinAvailable.Free;
            }
            else
            {
                logger.Info("  " +
                StrTradeTime +
                " " + player.Name +
                     " " + pair.Replace("USDT", "").PadRight(7, ' ') +
                " not available in Binance. its unusal, so wont execute sell order. Check it out");
                availableQty = 0;
            }

            return availableQty;
        }

        public async Task UpdateAllowedPrecisionsForPairs()
        {
            if (UpdatePrecisionCounter % 100 == 0)
            {
                UpdatePrecisionCounter = 1;

                DB db = new DB();

                exchangeInfo = await client.GetExchangeInfo();

                foreach (var coin in MyCoins)
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
            else
            {
                UpdatePrecisionCounter++;
            }

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
                MyCoins = await db.MyCoins.AsNoTracking().Where(x => x.IsIncludedForTrading == true).ToListAsync();
            }
        }

        private async Task Trade()
        {

            if (isControlCurrentlyInTradeMethod) return;

            isControlCurrentlyInTradeMethod = true;

            DB db = new DB();
            configr = await db.Config.FirstOrDefaultAsync();

            await GetMyCoins();

            TradeTime = DateTime.Now;
            StrTradeTime = TradeTime.ToString("dd-MMM HH:mm:ss");
            // NextTradeTime = TradeTime.AddMinutes(configr.IntervalMinutes).ToString("dd-MMM HH:mm:ss");
            NextTradeTime = TradeTime.AddSeconds(configr.IntervalMinutes).ToString("dd-MMM HH:mm:ss");

            EnsureSocketRunning();
            CollectReferenceCandles();
            CreateBuyLowestSellHighestSignals();
            CreateScalpBuySignals();
            LogDecisions();

            await UpdateTradeBuyDetails();
            await UpdateTradeSellDetails();
            Thread.Sleep(400);

            #region Buy

            try
            {

                await Buy();
                logger.Info("Buying Completed for " + StrTradeTime);

            }
            catch (Exception ex)
            {
                logger.Error("Exception at buy " + ex.Message);
            }

            logger.Info("");
            #endregion Buys

            #region Sell


            try
            {
                Thread.Sleep(400);

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

            await SetGrid();
            //  logger.Info("Next run at " + NextTradeTime);
            logger.Info("----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------");
            isControlCurrentlyInTradeMethod = false;

        }

        private async void TraderTimer_Tick(object sender, EventArgs e)
        {
            await Trade();

        }

        private void btnTrade_Click(object sender, RoutedEventArgs e)
        {
            // await Trade();
            EnsureSocketRunning();
            CollectReferenceCandles();
            CreateBuyLowestSellHighestSignals();
            CreateScalpBuySignals();
            LogDecisions();
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

        private async Task RemoveOldCandles()
        {

            using (var db = new DB())
            {
                await db.Database.ExecuteSqlRawAsync("delete from Candle where CAST(RecordedTime AS DATE)  <= GETDATE()-1");
            }

        }

        public async Task GetAllUSDTPairs()
        {
            List<Signal> signals = new List<Signal>();
            exchangeInfo = await client.GetExchangeInfo();

            foreach (var symbole in exchangeInfo.Symbols)
            {
                if (symbole.Symbol.EndsWith("USDT"))
                {
                    if (symbole.Symbol.EndsWith("UPUSDT") || symbole.Symbol.EndsWith("DOWNUSDT") ||
                        symbole.Symbol.EndsWith("BULLUSDT") || symbole.Symbol.EndsWith("BEARUSDT") || symbole.Symbol == "BUSDUSDT" || symbole.Symbol == "USDCUSDT" || symbole.Symbol == "EURUSDT")
                    {
                        continue;
                    }
                    var pricechangeresponse = await client.GetDailyTicker(symbole.Symbol);
                    Signal signal = new Signal();
                    signal.Symbol = symbole.Symbol;
                    signal.DayTradeCount = pricechangeresponse.TradeCount;
                    signals.Add(signal);
                }
            }

            signals = signals.OrderByDescending(x => x.DayTradeCount).ToList();

            foreach (var sig in signals)
            {
                logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' '));
            }
        }

        private void ViewCoin(object sender, RoutedEventArgs e)
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
                playerViewModel.LastRoundProfitPerc = player.LastRoundProfitPerc;
                playerViewModel.ProfitLossChanges = player.ProfitLossChanges.GetLast(140);
                PlayerViewModels.Add(playerViewModel);
            }

            PlayerGrid.ItemsSource = PlayerViewModels.OrderByDescending(x => x.CurrentRoundProfitPerc);

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
            //logger.Info("  " +
            //            sig.CandleOpenTime.ToString("dd-MMM HH:mm") +
            //          " " + player.Name +
            //         " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
            //           " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(11, ' ') +
            //           " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(11, ' ') +
            //          " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(11, ' ') +
            //          " Not Close to day low than high. Dont buy ");
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
                    " Pr Ch in Lst Hr " + sig.PriceChangeInLastHour.Rnd(5).ToString().PadRight(11, ' ') +
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

        public void LogNoSellReason(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {
            //if(prDiffPerc< player.SellAbovePerc.Deci())
            //{ 
            //logger.Info("  " +
            //ProcessingTimeString +
            //" " + player.Name +
            //  " " + pair.Replace("USDT", "").PadRight(7, ' ') +

            //   " BuyCnPr " + player.BuyCoinPrice.Deci().Rnd().ToString().PadRight(10, ' ') +
            //   " CurCnPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
            //   " TotBuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
            //   " TotSlPr  " + player.TotalSellAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            //" PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
            //" < Sell above % " + player.SellAbovePerc.Deci().Rnd(2).ToString().PadRight(3, ' ') + " Dont Sell ");
            //}
            //else
            //{
            //    logger.Info("  " +
            //             ProcessingTimeString +
            //             " " + player.Name +
            //               " " + pair.Replace("USDT", "").PadRight(7, ' ') +

            //                " BuyCnPr " + player.BuyCoinPrice.Deci().Rnd().ToString().PadRight(10, ' ') +
            //                " CurCnPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
            //                " TotBuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
            //                " TotSlPr  " + player.TotalSellAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            //             " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
            //             "  > Sell below % " + player.SellBelowPerc.Deci().Rnd(2) + " Dont Sell ");
            //}


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
//                         " DiCr&Lw " + sig.PrDiffCurrAndLowPerc.Rnd(6).ToString().PadRight(11, ' ') +
//                         " Trds " + sig.DayTradeCount.Rnd(6).ToString().PadRight(11, ' ') +
//                         " Vols " + sig.DayVol.Rnd(2).ToString().PadRight(20, ' ');
//log += " is At Day High. Best time to Sell";
//logger.Info(log);


