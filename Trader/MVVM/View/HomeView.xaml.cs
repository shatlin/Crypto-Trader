
using AutoMapper;
using BinanceExchange.API.Client;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using log4net;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Trader.Models;

namespace Trader.MVVM.View
{
    public static class Helpers
    {
        public static decimal Rnd(this decimal value, int places = 5)
        {
            return Math.Round(value, places);
        }

        public static decimal Deci(this decimal? value)
        {
            return Convert.ToDecimal(value);
        }

        public static decimal GetDiffPerc(this decimal oldValue, decimal NewValue)
        {

            return ((oldValue - NewValue) / ((NewValue + NewValue) / 2)) * 100;
        }

        public static decimal? GetDiffPerc(this decimal? oldValue, decimal? NewValue)
        {

            return ((oldValue - NewValue) / ((NewValue + NewValue) / 2)) * 100;
        }


    }

    public partial class HomeView : UserControl
    {
        public List<MyTradeFavouredCoins> MyTradeFavouredCoins { get; set; }
        public List<string> MyTradedCoinList { get; set; }
        public DispatcherTimer CandleDataRetrieverTimer;
        public DispatcherTimer TraderTimer;
        public DispatcherTimer CandleDailyDataRetrieverTimer;
        BinanceClient client;
        ILog logger;
        DB db;
        int intervalminutes = 15;
        double hourDifference = 2;
        IMapper iMapr;
        DateTime referenceStartTime = DateTime.Today;
        List<MyTradeFavouredCoins> myCoins = new List<MyTradeFavouredCoins>();
        List<string> boughtCoins = new List<string>();

        public HomeView()
        {
            InitializeComponent();
            referenceStartTime = referenceStartTime.Add(new TimeSpan(6, 0, 0));
            Startup();
        }

        private void Startup()
        {



            TraderTimer = new DispatcherTimer();
            TraderTimer.Tick += new EventHandler(TraderTimer_Tick);
            TraderTimer.Interval = new TimeSpan(0, intervalminutes, 0);


            logger = LogManager.GetLogger(typeof(MainWindow));
            db = new DB();
            var api = db.API.FirstOrDefault();
            client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = api.key,
                SecretKey = api.secret,
                Logger = logger,
            });


            SetGrid();
            CalculateBalanceSummary();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<TradeBot, TradeBotHistory>();
            });

            iMapr = config.CreateMapper();

#if !DEBUG

            CandleDailyDataRetrieverTimer = new DispatcherTimer();
            CandleDailyDataRetrieverTimer.Tick += new EventHandler(CandleDailyDataRetrieverTimer_Tick);
            CandleDailyDataRetrieverTimer.Interval = new TimeSpan(24, 0, 0);
            CandleDailyDataRetrieverTimer.Start();

            CandleDataRetrieverTimer = new DispatcherTimer();
            CandleDataRetrieverTimer.Tick += new EventHandler(CandleDataRetrieverTimer_Tick);
            CandleDataRetrieverTimer.Interval = new TimeSpan(0, intervalminutes, 0);
            CandleDataRetrieverTimer.Start();
            GetCandles();
#endif

        }

        //private async void BalanceTimer_Tick(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        await UpdateBalance();
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error("Exception at Updating Balance at timed intervals " + ex.Message);
        //    }
        //}

        private void SetGrid()
        {
            DB GridDB = new DB();
            BalanceDG.ItemsSource = GridDB.Balance.AsNoTracking().OrderByDescending(x => x.DifferencePercentage).ToList();
        }

        private async void btnUpdateBalance_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var prices = await client.GetAllPrices();
                await UpdateBalance(prices);

            }
            catch (Exception ex)
            {

                logger.Error("Exception at Updating Balance at timed intervals " + ex.Message);
            }
        }

        private async Task<List<Balance>> UpdateBalance(List<SymbolPriceResponse> prices)
        {
            DB BalanceDB = new DB();
            try
            {
                await BalanceDB.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Balance");
                AccountInformationResponse accinfo = await client.GetAccountInformation();
                var trades = await BalanceDB.MyTrade.ToListAsync();

                foreach (var asset in accinfo.Balances)
                {

                    try
                    {
                        if (asset.Free > 0)
                        {
                            var bal = new Balance
                            {
                                Asset = asset.Asset,
                                Free = asset.Free,
                                Locked = asset.Locked
                            };

                            if (asset.Asset.ToUpper() == "BUSD" || asset.Asset.ToUpper() == "USDT")
                            {
                                bal.CurrentPrice = asset.Free + asset.Locked;
                                bal.BoughtPrice = asset.Free + asset.Locked;
                                await BalanceDB.Balance.AddAsync(bal);
                                continue;
                            }

                            foreach (var price in prices)
                            {
                                try
                                {
                                    if (price.Symbol.ToUpper() == asset.Asset.ToUpper() + "USDT")
                                    {
                                        bal.CurrentPrice = bal.Free * price.Price;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger.Error("Exception while retrieving Price for Asset " + asset.Asset + " " + ex.Message);
                                }
                            }

                            decimal spentprice = 0;

                            foreach (var trade in trades)
                            {
                                if (trade.Pair.ToUpper().Contains(asset.Asset.ToUpper()))
                                {
                                    if (trade.IsBuyer)
                                    {
                                        spentprice += (trade.Price * trade.Quantity) + (trade.Price * trade.Quantity) * 0.075M / 100;
                                    }
                                    else
                                    {
                                        spentprice -= (trade.Price * trade.Quantity) + (trade.Price * trade.Quantity) * 0.075M / 100;
                                    }
                                }
                            }
                            bal.BoughtPrice = spentprice;
                            bal.AverageBuyingCoinPrice = (bal.BoughtPrice / bal.Free) + (bal.BoughtPrice / bal.Free) * 1 / 100;

                            try
                            {
                                bal.CurrentCoinPrice = prices.Where(x => x.Symbol == asset.Asset + "USDT").FirstOrDefault().Price;
                            }
                            catch
                            {
                                bal.CurrentCoinPrice = (bal.CurrentPrice / bal.Free);
                            }
                            bal.Difference = bal.CurrentPrice - bal.BoughtPrice;
                            bal.DifferencePercentage = (bal.CurrentPrice - bal.BoughtPrice) / ((bal.CurrentPrice + bal.BoughtPrice) / 2) * 100;

                            await BalanceDB.Balance.AddAsync(bal);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Exception while retrieving balances " + ex.Message);
                    }
                }

                await BalanceDB.SaveChangesAsync();

                SetGrid();
                CalculateBalanceSummary();


            }
            catch (Exception ex)
            {
                logger.Error($"Exception at Updating Balance {ex.Message}");
            }
            var currentBalance = await BalanceDB.Balance.ToListAsync();
            return currentBalance;
        }

        private void CalculateBalanceSummary()
        {
            try
            {
                DB BalanceDB = new DB();
                decimal totalinvested = 0;
                decimal totalcurrent = 0;
                decimal totaldifference = 0;
                decimal totaldifferenceinpercentage = 0;

                var balances = BalanceDB.Balance.AsNoTracking().ToList();
                if (balances != null && balances.Count > 0)
                {
                    foreach (var balance in balances)
                    {
                        totalinvested += balance.BoughtPrice;
                        totalcurrent += balance.CurrentPrice;
                    }
                    totaldifference = totalcurrent - totalinvested;
                    totaldifferenceinpercentage = (totaldifference / ((totalinvested + totalcurrent) / 2)) * 100;
                }
                lblInvested.Text = "Invested:   " + String.Format("{0:0.00}", totalinvested);
                lblCurrentValue.Text = "Current Value:   " + String.Format("{0:0.00}", totalcurrent);
                lblDifference.Text = "Difference:   " + String.Format("{0:0.00}", totaldifference);
                lblDifferencePercentage.Text = "Difference %:   " + String.Format("{0:0.00}", totaldifferenceinpercentage);
            }
            catch (Exception ex)
            {
                logger.Error("Exception at setting Summary " + ex.Message);
            }

        }

        //private async Task GetPrices()
        //{
        //    var prices = await client.GetAllPrices();
        //    foreach (var price in prices)
        //    {

        //        if ((price.Symbol.Contains("BUSD") && !price.Symbol.Contains("USDT")) || (price.Symbol.Contains("USDT") && !price.Symbol.Contains("BUSD")))
        //        {
        //            try
        //            {
        //                var pr = new Price
        //                {
        //                    date = DateTime.Now,
        //                    pair = price.Symbol,
        //                    price = price.Price
        //                };
        //                await db.Price.AddAsync(pr);
        //            }
        //            catch (Exception ex)
        //            {
        //                logger.Error("Exception at Get Prices  " + ex.Message);
        //            }
        //        }
        //    }
        //    await db.SaveChangesAsync();
        //}

        private async Task GetMyTrades()
        {
            DB TradeDB = new DB();
            foreach (var coin in MyTradedCoinList)
            {
                try
                {
                    List<AccountTradeReponse> accountTrades = await client.GetAccountTrades(new AllTradesRequest()
                    {
                        Limit = 50,
                        Symbol = coin
                    });

                    foreach (var trade in accountTrades)
                    {
                        var mytrade = new MyTrade
                        {
                            Price = trade.Price,
                            Pair = coin,
                            Quantity = trade.Quantity,
                            Commission = trade.Commission,
                            CommissionAsset = trade.CommissionAsset,
                            Time = trade.Time,
                            IsBuyer = trade.IsBuyer,
                            IsMaker = trade.IsMaker,
                            IsBestMatch = trade.IsBestMatch,
                            OrderId = trade.OrderId,
                            Amount = trade.Quantity * trade.Price + (trade.Commission * Rates.BNB)
                        };
                        await TradeDB.MyTrade.AddAsync(mytrade);
                    }
                }
                catch (Exception ex)
                {

                    logger.Error("Error while retrieving price ticker for " + coin + " " + ex.Message);
                }
            }
            await TradeDB.SaveChangesAsync();
        }

        private async void CandleDataRetrieverTimer_Tick(object sender, EventArgs e)
        {
            await GetCandles();
        }

        private async void CandleDailyDataRetrieverTimer_Tick(object sender, EventArgs e)
        {
            await GetCandlesOnceaDay();
        }

        private async void TraderTimer_Tick(object sender, EventArgs e)
        {
            await Trade();
        }

        private async Task<List<Candle>> GetCandles()
        {

            List<Candle> candles = new List<Candle>();

            logger.Info("Getting Candle Started at " + DateTime.Now);

            DB candledb = new DB();

            var counter = await candledb.Counter.FirstOrDefaultAsync();

            if (counter.IsCandleCurrentlyBeingUpdated)
            {
                return candles;
            }

            var minutedifference = (DateTime.Now - counter.CandleLastUpdatedTime).TotalMinutes;

            if (minutedifference < (intervalminutes - 5))
            {
                logger.Info(" Candle retrieved only " + minutedifference + " minutes back. Dont need to get again");
                return candles;
            }

            counter.IsCandleCurrentlyBeingUpdated = true;
            candledb.Update(counter);
            await candledb.SaveChangesAsync();

            MyTradeFavouredCoins = await candledb.MyTradeFavouredCoins.ToListAsync();

            var prices = await client.GetAllPrices();
            await UpdateBalance(prices);

            int candlecurrentSet = counter.CandleCurrentSet;

            foreach (var coin in MyTradeFavouredCoins)
            {
                var pricesofcoin = prices.Where(x => x.Symbol.Contains(coin.Pair));
                if (pricesofcoin == null || pricesofcoin.Count() == 0)
                {
                    continue;
                }
                foreach (var price in pricesofcoin)
                {
                    if (price.Symbol != coin.Pair + "BUSD" && price.Symbol != coin.Pair + "USDC" && price.Symbol != coin.Pair + "USDT") // if the price symbol doesnt contain usdt and busd ignore those coins
                    {
                        continue;
                    }
                    Candle candle = new Candle();
                    var pricechangeresponse = await client.GetDailyTicker(price.Symbol);
                    GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
                    cr.Limit = 1;
                    cr.Symbol = price.Symbol;
                    cr.Interval = KlineInterval.FifteenMinutes;

                    var candleresponse = await client.GetKlinesCandlesticks(cr);

                    candle.Symbol = price.Symbol;
                    candle.Open = candleresponse[0].Open;
                    candle.OpenTime = candleresponse[0].OpenTime.AddHours(hourDifference);
                    candle.High = candleresponse[0].High;
                    candle.Low = candleresponse[0].Low;
                    candle.Close = candleresponse[0].Close;
                    candle.Volume = candleresponse[0].Volume;
                    candle.CloseTime = candleresponse[0].CloseTime.AddHours(hourDifference);
                    candle.QuoteAssetVolume = candleresponse[0].QuoteAssetVolume;
                    candle.NumberOfTrades = candleresponse[0].NumberOfTrades;
                    candle.TakerBuyBaseAssetVolume = candleresponse[0].TakerBuyBaseAssetVolume;
                    candle.TakerBuyQuoteAssetVolume = candleresponse[0].TakerBuyQuoteAssetVolume;
                    candle.Change = pricechangeresponse.PriceChange;
                    candle.PriceChangePercent = pricechangeresponse.PriceChangePercent;
                    candle.WeightedAveragePercent = pricechangeresponse.PriceChangePercent;
                    candle.PreviousClosePrice = pricechangeresponse.PreviousClosePrice;
                    candle.CurrentPrice = pricechangeresponse.LastPrice;
                    candle.OpenPrice = pricechangeresponse.OpenPrice;
                    candle.DayHighPrice = pricechangeresponse.HighPrice;
                    candle.DayLowPrice = pricechangeresponse.LowPrice;
                    candle.DayVolume = pricechangeresponse.Volume;
                    candle.DayTradeCount = pricechangeresponse.TradeCount;
                    candle.DataSet = candlecurrentSet;

                    var isCandleExisting = await candledb.Candle.Where(x => x.DataSet == candle.DataSet && x.OpenTime == candle.OpenTime && x.Symbol == candle.Symbol).FirstOrDefaultAsync();

                    if (isCandleExisting == null)
                    {
                        candles.Add(candle);
                        await candledb.Candle.AddAsync(candle);
                        await candledb.SaveChangesAsync();
                    }
                }

            }

            counter.CandleCurrentSet++;
            counter.IsCandleCurrentlyBeingUpdated = false;
            counter.CandleLastUpdatedTime = DateTime.Now;
            candledb.Counter.Update(counter);
            await candledb.SaveChangesAsync();
            logger.Info("Getting Candle Completed at " + DateTime.Now);
            return candles;
        }

        private async Task GetCandlesOnceaDay()
        {
            logger.Info("Getting Daily Candle Started at " + DateTime.Now);
            DB candledb = new DB();
            var prices = await client.GetAllPrices();
            await UpdateBalance(prices);

            int dailycandlecurrentSet = candledb.Counter.FirstOrDefault().DailyCandleCurrentSet;

            foreach (var price in prices)
            {

                if (
                     price.Symbol.Contains("UPUSDT") || price.Symbol.Contains("DOWNUSDT") ||
                     price.Symbol.Contains("UPBUSD") || price.Symbol.Contains("DOWNBUSD") ||
                      price.Symbol.Contains("UPUSDC") || price.Symbol.Contains("DOWNUSDC") ||
                     price.Symbol.Contains("BEARBUSD") || price.Symbol.Contains("BULLBUSD") ||
                     price.Symbol.Contains("BEARUSDT") || price.Symbol.Contains("BULLUSDT") ||
                     price.Symbol.Contains("BEARUSDC") || price.Symbol.Contains("BULLUSDC")
                    )
                {
                    continue;
                }

                if (!price.Symbol.Contains("BUSD") && !price.Symbol.Contains("USDT") && !price.Symbol.Contains("USDC")) // if the price symbol doesnt contain usdt and busd ignore those coins

                {
                    continue;
                }

                DailyCandle dailycandle = new DailyCandle();
                var pricechangeresponse = await client.GetDailyTicker(price.Symbol);
                GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
                cr.Limit = 1;
                cr.Symbol = price.Symbol;
                cr.Interval = KlineInterval.OneDay;
                var candleresponse = await client.GetKlinesCandlesticks(cr);

                dailycandle.Symbol = price.Symbol;
                dailycandle.Open = candleresponse[0].Open;
                dailycandle.OpenTime = candleresponse[0].OpenTime.AddHours(hourDifference);
                dailycandle.High = candleresponse[0].High;
                dailycandle.Low = candleresponse[0].Low;
                dailycandle.Close = candleresponse[0].Close;
                dailycandle.Volume = candleresponse[0].Volume;
                dailycandle.CloseTime = candleresponse[0].CloseTime.AddHours(hourDifference);
                dailycandle.QuoteAssetVolume = candleresponse[0].QuoteAssetVolume;
                dailycandle.NumberOfTrades = candleresponse[0].NumberOfTrades;
                dailycandle.TakerBuyBaseAssetVolume = candleresponse[0].TakerBuyBaseAssetVolume;
                dailycandle.TakerBuyQuoteAssetVolume = candleresponse[0].TakerBuyQuoteAssetVolume;
                dailycandle.Change = pricechangeresponse.PriceChange;
                dailycandle.PriceChangePercent = pricechangeresponse.PriceChangePercent;
                dailycandle.WeightedAveragePercent = pricechangeresponse.PriceChangePercent;
                dailycandle.PreviousClosePrice = pricechangeresponse.PreviousClosePrice;
                dailycandle.CurrentPrice = pricechangeresponse.LastPrice;
                dailycandle.OpenPrice = pricechangeresponse.OpenPrice;
                dailycandle.DayHighPrice = pricechangeresponse.HighPrice;
                dailycandle.DayLowPrice = pricechangeresponse.LowPrice;
                dailycandle.DayVolume = pricechangeresponse.Volume;
                dailycandle.DayTradeCount = pricechangeresponse.TradeCount;
                dailycandle.DataSet = dailycandlecurrentSet;

                var isCandleExisting = await candledb.DailyCandle.Where(x => x.DataSet == dailycandle.DataSet && x.OpenTime == dailycandle.OpenTime && x.Symbol == dailycandle.Symbol).FirstOrDefaultAsync();

                if (isCandleExisting == null)
                {
                    await candledb.DailyCandle.AddAsync(dailycandle);
                    await candledb.SaveChangesAsync();
                }


            }


            var counters = await candledb.Counter.FirstOrDefaultAsync();
            counters.DailyCandleCurrentSet++;
            candledb.Counter.Update(counters);
            await candledb.SaveChangesAsync();
            logger.Info("Getting Daily Candle Completed at " + DateTime.Now);
        }

        private async void btnTrade_Click(object sender, RoutedEventArgs e)
        {
            // TraderTimer.Start();
            await Trade();
        }

        private async Task ClearData()
        {
            DB TradeDB = new DB();

            await TradeDB.Database.ExecuteSqlRawAsync("TRUNCATE TABLE TradeBotHistory");

            var bots = await TradeDB.TradeBot.ToListAsync();

            foreach (var bot in bots)
            {
                bot.Pair = null;
                bot.DayHigh = 0.0M;
                bot.DayLow = 0.0M;
                bot.BuyPricePerCoin = 0.0M;
                bot.CurrentPricePerCoin = 0.0M;
                bot.QuantityBought = 0.0M;
                bot.TotalBuyCost = 0.0M;
                bot.TotalCurrentValue = 0.0M;
                bot.TotalSoldAmount = 0.0M;
                bot.BuyTime = null;
                bot.SellTime = null;
                bot.CreatedDate = new DateTime(2021, 3, 1, 23, 0, 0);
                bot.UpdatedTime = null;
                bot.IsActivelyTrading = false;
                bot.AvailableAmountForTrading = 400;
                bot.OriginalAllocatedValue = 400;
                bot.BuyingCommision = 0.0M;
                bot.QuantitySold = 0.0M;
                bot.SoldCommision = 0.0M;
                bot.SoldPricePricePerCoin = 0.0M;
                bot.TotalCurrentProfit = 0.0M;
                bot.CandleOpenTimeAtBuy = null;
                bot.BuyOrSell = string.Empty;
                bot.CandleOpenTimeAtSell = null;
                bot.TotalExpectedProfit = 0.0M;
                TradeDB.Update(bot);
            }
            await TradeDB.SaveChangesAsync();
        }

        private async void btnClearRobot_Click(object sender, RoutedEventArgs e)
        {
            await ClearData();

        }

        private async void btnCollectData_Click(object sender, RoutedEventArgs e)
        {
            List<Candle> candles = new List<Candle>();

            logger.Info("Getting Candle Started at " + DateTime.Now);

            DB candledb = new DB();

            var files = Directory.EnumerateFiles(@"C:\Shatlin\klines\csv", "*.csv");
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

            foreach (string file in files)
            {

                string filename = file.Split('-')[0];
                filename = filename.Substring(filename.LastIndexOf("\\") + 1);
                using (var reader = new StreamReader(file))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(",");
                        Candle candle = new Candle();
                        candle.Symbol = filename;
                        double d = Convert.ToDouble(values[0].ToString());

                        candle.OpenTime = Convert.ToDateTime(epoch.AddMilliseconds(Convert.ToDouble(values[0])));
                        candle.Open = Convert.ToDecimal(values[1]);
                        candle.High = Convert.ToDecimal(values[2]);
                        candle.Low = Convert.ToDecimal(values[3]);
                        candle.Close = Convert.ToDecimal(values[4]);
                        candle.Volume = Convert.ToDecimal(values[5]);
                        candle.CloseTime = Convert.ToDateTime(epoch.AddMilliseconds(Convert.ToDouble(values[6])));
                        candle.QuoteAssetVolume = Convert.ToDecimal(values[7]);
                        candle.NumberOfTrades = Convert.ToInt32(values[8]);
                        candle.TakerBuyBaseAssetVolume = Convert.ToDecimal(values[9]);
                        candle.TakerBuyQuoteAssetVolume = Convert.ToDecimal(values[10]);
                        candle.Change = 0.0M;
                        candle.PriceChangePercent = 0.0M;
                        candle.WeightedAveragePercent = 0.0M;
                        candle.PreviousClosePrice = 0.0M;
                        candle.CurrentPrice = candle.CurrentPrice;
                        candle.OpenPrice = 0.0M;
                        candle.DayHighPrice = 0.0M;
                        candle.DayLowPrice = 0.0M;
                        candle.DayVolume = 0.0M;
                        candle.DayTradeCount = 0;
                        candle.DataSet = 0;
                        await candledb.AddAsync(candle);
                    }
                }
                await candledb.SaveChangesAsync();

            }

            await UpdateData();
        }

        private async Task UpdateData()
        {
            DateTime currentdate = new DateTime(2021, 6, 10);
            DateTime lastdate = new DateTime(2021, 6, 13);
            List<Candle> selectedCandles;
            DB TradeDB = new DB();
            List<MyTradeFavouredCoins> myTradeFavouredCoins = await TradeDB.MyTradeFavouredCoins.AsNoTracking().ToListAsync();

            while (currentdate <= lastdate)
            {
                try
                {
                    foreach (var favtrade in myTradeFavouredCoins)
                    {
                        selectedCandles = await TradeDB.Candle.Where(x => x.Symbol.Contains(favtrade.Pair) && x.OpenTime.Date == currentdate
                        && x.CurrentPrice <= 0).ToListAsync();

                        if (selectedCandles == null || selectedCandles.Count == 0) continue;

                        foreach (var candle in selectedCandles)
                        {
                            candle.CurrentPrice = candle.Close;
                            candle.DayHighPrice = selectedCandles.Max(x => x.High);
                            candle.DayLowPrice = selectedCandles.Min(x => x.Low);
                            candle.DayVolume = selectedCandles.Sum(x => x.Volume);
                            candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
                            TradeDB.Candle.Update(candle);
                        }
                        await TradeDB.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(" Updating candle error " + ex.Message);
                }
                currentdate = currentdate.AddDays(1);
            }
        }

        private async Task<List<Signal>> GetSignals(DateTime cndlHr)
        {

            DB TradeDB = new DB();

            List<Signal> signals = new List<Signal>();

            List<Candle> latestCndls = await TradeDB.Candle.AsNoTracking().Where(x => x.OpenTime == cndlHr).ToListAsync();

            // DateTime refCandlMinTime = currentCandleSetDate.AddHours(-23);

            List<Candle> refCndls = await TradeDB.Candle.AsNoTracking()
                .Where(x => x.OpenTime >= cndlHr.AddHours(-23) && x.OpenTime < cndlHr).ToListAsync();

            foreach (var myfavcoin in myCoins)
            {

                try
                {
                    #region Prefer BUSD if not available go for USDT

                    List<string> usdStrings = new List<string>()
                { myfavcoin.Pair + "USDT", myfavcoin.Pair + "BUSD", myfavcoin.Pair + "USDC" };

                    var usdCndlList = latestCndls.Where(x => usdStrings.Contains(x.Symbol));

                    if (usdCndlList == null) continue;

                    var busdcandle = usdCndlList.Where(x => x.Symbol == myfavcoin.Pair + "BUSD").FirstOrDefault();

                    Signal sig = new Signal();

                    if (busdcandle != null) sig.Symbol = busdcandle.Symbol;
                    else sig.Symbol = myfavcoin.Pair + "USDT";

                    var selCndl = usdCndlList.Where(x => x.Symbol == sig.Symbol).FirstOrDefault();

                    if (selCndl == null) continue;

                    var usdRefCndls = refCndls.Where(x => usdStrings.Contains(x.Symbol));

                    if (usdRefCndls == null || usdRefCndls.Count() == 0) continue;

                    #endregion

                    sig.CurrPr = selCndl.CurrentPrice;
                    sig.DayHighPr = selCndl.DayHighPrice;
                    sig.DayLowPr = selCndl.DayLowPrice;
                    sig.CandleOpenTime = selCndl.OpenTime;
                    sig.DayVol = usdCndlList.Sum(x => x.DayVolume);
                    sig.DayTradeCount = usdCndlList.Sum(x => x.DayTradeCount);
                    sig.RefHighPr = usdRefCndls.Max(x => x.CurrentPrice);
                    sig.RefLowPr = usdRefCndls.Min(x => x.CurrentPrice);
                    sig.RefAvgCurrPr = usdRefCndls.Average(x => x.CurrentPrice);
                    sig.RefDayVol = usdRefCndls.Average(x => x.DayVolume);
                    sig.RefDayTradeCount = (int)usdRefCndls.Average(x => x.DayTradeCount);

                    sig.DayPrDiffPercentage = sig.DayHighPr.GetDiffPerc(sig.DayLowPr);
                    sig.PrDiffCurrAndHighPerc = Math.Abs(sig.DayHighPr.GetDiffPerc(sig.CurrPr));
                    sig.PrDiffCurrAndLowPerc = Math.Abs(sig.DayLowPr.GetDiffPerc(sig.CurrPr));
                    // this will always be positive. You need to first target those coins which are 
                    //closest to low price. Dont worry about trade count for now
                    sig.CurrPrDiffSigAndRef = sig.CurrPr.GetDiffPerc(sig.RefAvgCurrPr);
                    //Difference between current price and the average current prices of last 24 hours

                    var dayAveragePrice = (sig.DayHighPr + sig.DayLowPr) / 2;

                    if (sig.CurrPr < dayAveragePrice) sig.IsCloseToDayLow = true;

                    else sig.IsCloseToDayHigh = true;

                    signals.Add(sig);
                }
                catch (Exception ex)
                {
                    logger.Error("Error in signal Generator " + ex.Message);
                }
            }

            return signals.OrderBy(x => x.PrDiffCurrAndLowPerc).ToList();
        }

        private async Task Buy(List<Signal> Signals)
        {

            if (Signals == null || Signals.Count() == 0)
            {
                logger.Info("No signals found. returning from buying");
                return;
            }

            #region definitions

            var candleopentime = Signals.FirstOrDefault().CandleOpenTime;

            DB db = new DB();
            var bots = await db.TradeBot.OrderBy(x => x.Id).ToListAsync();

            boughtCoins = await db.TradeBot.Where(x => x.Pair != null).
                          Select(x => x.Pair).ToListAsync();

            bool isdbUpdateRequired = false;

            #endregion definitions

            bool isanybuyingdone = false;

            foreach (var bot in bots)
            {
                #region if bot is currently trading, just update stats and go to next bot

                if (bot.IsActivelyTrading)
                {
                    var botsSignal = Signals.Where(x => x.Symbol == bot.Pair).FirstOrDefault();

                    if (botsSignal != null)
                    {
                        bot.DayHigh = botsSignal.DayHighPr;
                        bot.DayLow = botsSignal.DayLowPr;
                        bot.CurrentPricePerCoin = botsSignal.CurrPr;
                        bot.TotalCurrentValue = bot.CurrentPricePerCoin * bot.QuantityBought;
                        bot.TotalCurrentProfit = bot.TotalCurrentValue - bot.OriginalAllocatedValue;
                        bot.UpdatedTime = DateTime.Now;
                        isdbUpdateRequired = true;
                    }
                    continue;
                }

                #endregion if bot is currently trading, just update stats and go to next bot

                foreach (var sig in Signals)
                {
                    if (sig.IsPicked) { continue; }
                    if (boughtCoins.Contains(sig.Symbol)) continue;

                    //buying criteria
                    //1. signals are ordered by the coins whose current price are at their lowest at the moment
                    //2. See if this price is the lowest in the last 24 hours
                    //3. See if the price difference is lower than what the bot is expecting to buy at. If yes, buy.

                    //Later see if you are on a downtrend and keep waiting till it reaches its low and then buy

                    if (sig.IsCloseToDayLow && (sig.PrDiffCurrAndHighPerc > bot.BuyWhenValuePercentageIsBelow))
                    {
                        logger.Info(bot.Name + " " + bot.Avatar + " " + sig.Symbol + " Cndl Hr " + sig.CandleOpenTime +
                        " DayHigh " + sig.DayHighPr.Rnd() +
                        " DayLow " + sig.DayLowPr.Rnd() +
                        " CurrPr" + sig.CurrPr.Rnd() +
                        " PrDiff% " + sig.PrDiffCurrAndHighPerc.Rnd() +
                        " > " + bot.BuyWhenValuePercentageIsBelow.Deci().Rnd() + " - Buy");

                        bot.IsActivelyTrading = true;
                        bot.Pair = sig.Symbol;
                        bot.DayHigh = sig.DayHighPr;
                        bot.DayLow = sig.DayLowPr;
                        bot.BuyPricePerCoin = sig.CurrPr;
                        bot.QuantityBought = bot.AvailableAmountForTrading / sig.CurrPr;
                        bot.BuyingCommision = bot.AvailableAmountForTrading * 0.075M / 100;
                        bot.TotalBuyCost = bot.AvailableAmountForTrading + bot.BuyingCommision;
                        bot.CurrentPricePerCoin = sig.CurrPr;
                        bot.TotalCurrentValue = bot.AvailableAmountForTrading;
                        bot.TotalCurrentProfit = bot.AvailableAmountForTrading - bot.OriginalAllocatedValue;
                        bot.BuyTime = DateTime.Now;
                        bot.AvailableAmountForTrading = 0;
                        bot.CandleOpenTimeAtBuy = sig.CandleOpenTime;
                        bot.CandleOpenTimeAtSell = null;
                        bot.UpdatedTime = DateTime.Now;
                        bot.BuyOrSell = "BUY";
                        bot.SellTime = null;
                        bot.QuantitySold = 0.0M;
                        bot.SoldCommision = 0.0M;
                        bot.SoldPricePricePerCoin = 0.0M;
                        sig.IsPicked = true;
                        db.TradeBot.Update(bot);
                        TradeBotHistory BotHistory = iMapr.Map<TradeBot, TradeBotHistory>(bot);
                        BotHistory.Id = 0;
                        await db.TradeBotHistory.AddAsync(BotHistory);
                        isdbUpdateRequired = true; //flag that db needs to be updated, and update it at the end
                        boughtCoins.Add(sig.Symbol);
                        isanybuyingdone = true;
                        break;
                    }
                }
            }

            if (isdbUpdateRequired) await db.SaveChangesAsync();

            if (isanybuyingdone == false)
            {
                logger.Info("No criteria met to make any buys this hour " + candleopentime);
            }

        }

        private async Task Sell(List<Signal> Signals)
        {
            bool isSaleHappen = false;

            if (Signals == null || Signals.Count() == 0)
            {
                logger.Info("No signals found. returning from selling");
                return;
            }

            var candleopentime = Signals.FirstOrDefault().CandleOpenTime;

            DB TradeDB = new DB();
            var bots = await TradeDB.TradeBot.OrderBy(x => x.Id).ToListAsync();

            foreach (var bot in bots)
            {
                if (!bot.IsActivelyTrading) continue; // empty bot, nothing to update

                var CoinPair = bot.Pair;
                var signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();

                if (signal == null) //for some weird reason if symbol comes null.
                {
                    if (CoinPair.Contains("BUSD")) CoinPair = CoinPair.Replace("BUSD", "USDT");
                    else if (CoinPair.Contains("USDT")) CoinPair = CoinPair.Replace("USDT", "BUSD");
                    signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();
                }

                if (signal == null) continue;

                //update to set all these values in tradebothistory
                bot.TotalBuyCost = bot.BuyPricePerCoin * bot.QuantityBought + bot.BuyingCommision;
                bot.SoldCommision = bot.CurrentPricePerCoin * bot.QuantityBought * 0.075M / 100;
                bot.TotalSoldAmount = bot.TotalCurrentValue - bot.SoldCommision;


                var prDiffPerc = bot.TotalSoldAmount.GetDiffPerc(bot.TotalBuyCost);

                if ((prDiffPerc > 5) || (prDiffPerc < -3)) //(prDiffPerc > 5) || (prDiffPerc < -3 && prDiffPerc > -25)
                {

                    bot.DayHigh = signal.DayHighPr;
                    bot.DayLow = signal.DayLowPr;
                    bot.CurrentPricePerCoin = signal.CurrPr;
                    bot.TotalCurrentValue = bot.CurrentPricePerCoin * bot.QuantityBought;
                    bot.QuantitySold = Convert.ToDecimal(bot.QuantityBought);
                    bot.AvailableAmountForTrading = bot.TotalSoldAmount;
                    bot.SellTime = DateTime.Now;
                    bot.UpdatedTime = DateTime.Now;
                    bot.SoldPricePricePerCoin = signal.CurrPr;
                    bot.CandleOpenTimeAtSell = signal.CandleOpenTime;
                    bot.BuyOrSell = "SELL";
                    bot.TotalCurrentProfit = bot.AvailableAmountForTrading - bot.OriginalAllocatedValue;

                    logger.Info("CndlHr " + candleopentime + " buy Pr " + bot.TotalBuyCost.Deci().Rnd() +
                                  " sell pr " + bot.TotalSoldAmount.Deci().Rnd() +
                                  " diff % " + prDiffPerc.Deci().Rnd() + " > " + " 5 % or < - 3 % - Sell");

                    // create sell order (in live system)
                    TradeBotHistory tradeBotHistory = iMapr.Map<TradeBot, TradeBotHistory>(bot);
                    tradeBotHistory.Id = 0;
                    await TradeDB.TradeBotHistory.AddAsync(tradeBotHistory);

                    // reset records to buy again
                    bot.DayHigh = 0.0M;
                    bot.DayLow = 0.0M;
                    bot.Pair = null;
                    bot.BuyPricePerCoin = 0.0M;
                    bot.CurrentPricePerCoin = 0.0M;
                    bot.QuantityBought = 0.0M;
                    bot.TotalBuyCost = 0.0M;
                    bot.TotalCurrentValue = 0.0M;
                    bot.TotalSoldAmount = 0.0M;
                    bot.BuyTime = null;
                    bot.SellTime = null;
                    bot.BuyingCommision = 0.0M;
                    bot.SoldPricePricePerCoin = 0.0M;
                    bot.QuantitySold = 0.0M;
                    bot.SoldCommision = 0.0M;
                    bot.IsActivelyTrading = false;
                    bot.CandleOpenTimeAtBuy = null;
                    bot.CandleOpenTimeAtSell = null;
                    bot.BuyOrSell = string.Empty;
                    TradeDB.TradeBot.Update(bot);
                    isSaleHappen = true;
                }

            }

            await TradeDB.SaveChangesAsync();

            if (isSaleHappen)
            {
                var availBots = await TradeDB.TradeBot.Where(x => x.IsActivelyTrading == false).ToListAsync();
                var avgAvailAmountForTrading = availBots.Average(x => x.AvailableAmountForTrading);

                foreach (var bot in availBots)
                {
                    bot.AvailableAmountForTrading = avgAvailAmountForTrading;
                    TradeDB.TradeBot.Update(bot);
                }

                await TradeDB.SaveChangesAsync();
            }

            //decimal? totBuyingPr = 0;
            //decimal? totCurrPr = 0;
            //decimal? avgProfitPerc = bots.Average(x => x.SellWhenProfitPercentageIsAbove);


            //foreach (var bot in bots) // this loop is used to calculate the total current price totCurrPr of the bought group.
            //{
            //    if (!bot.IsActivelyTrading) continue; // empty bot, nothing to update


            //    decimal qtyBght = Convert.ToDecimal(bot.QuantityBought);

            //    totBuyingPr += bot.TotalBuyCost;

            //    foreach (var signal in Signals)
            //    {
            //        if (signal.Symbol == bot.Pair)
            //        {
            //            var currentPrice = signal.CurrPr;
            //            totCurrPr += (signal.CurrPr * qtyBght) + ((signal.CurrPr * qtyBght) * (0.075M / 100));
            //            break;
            //        }
            //    }
            //}

            //if (totBuyingPr > 0)
            //{
            //    var prDiffPerc = totCurrPr.GetDiffPerc(totBuyingPr);
            //    bool isBotGrSold = false;

            //    if ((prDiffPerc > 5) || (prDiffPerc < -3)) //(prDiffPerc > 5) || (prDiffPerc < -3 && prDiffPerc > -25)
            //    {
            //        decimal botGrAvlAmt = 0.0M;

            //        foreach (var bot in bots)
            //        {
            //            try
            //            {

            //                if (!bot.IsActivelyTrading) // bot  not trading, but take the avail amt to calc how to distribue after sold
            //                {
            //                    botGrAvlAmt += Convert.ToDecimal(bot.AvailableAmountForTrading);
            //                    continue; // no data to update, its not actively trading, empty robo
            //                }

            //                var CoinPair = bot.Pair;
            //                var signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();

            //                if (signal == null) //for some weird reason if symbol comes null.
            //                {
            //                    if (CoinPair.Contains("BUSD")) CoinPair = CoinPair.Replace("BUSD", "USDT");
            //                    else if (CoinPair.Contains("USDT")) CoinPair = CoinPair.Replace("USDT", "BUSD");
            //                    signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();
            //                }

            //                if (signal == null) continue;

            //                //update to set all these values in tradebothistory
            //                bot.DayHigh = signal.DayHighPr;
            //                bot.DayLow = signal.DayLowPr;
            //                bot.CurrentPricePerCoin = signal.CurrPr;
            //                bot.TotalCurrentValue = bot.CurrentPricePerCoin * bot.QuantityBought;
            //                bot.QuantitySold = Convert.ToDecimal(bot.QuantityBought);
            //                bot.SoldCommision = bot.CurrentPricePerCoin * bot.QuantityBought * 0.075M / 100;
            //                bot.TotalSoldAmount = bot.TotalCurrentValue - bot.SoldCommision;
            //                bot.AvailableAmountForTrading = bot.TotalSoldAmount;
            //                bot.SellTime = DateTime.Now;
            //                bot.UpdatedTime = DateTime.Now;
            //                bot.SoldPricePricePerCoin = signal.CurrPr;
            //                bot.CandleOpenTimeAtSell = signal.CandleOpenTime;
            //                bot.BuyOrSell = "SELL";
            //                botGrAvlAmt += Convert.ToDecimal(bot.TotalSoldAmount);

            //                // create sell order (in live system)
            //                TradeBotHistory tradeBotHistory = iMapr.Map<TradeBot, TradeBotHistory>(bot);
            //                tradeBotHistory.Id = 0;
            //                await TradeDB.TradeBotHistory.AddAsync(tradeBotHistory);

            //                // reset records to buy again
            //                bot.DayHigh = 0.0M;
            //                bot.DayLow = 0.0M;
            //                bot.Pair = null;
            //                bot.BuyPricePerCoin = 0.0M;
            //                bot.CurrentPricePerCoin = 0.0M;
            //                bot.QuantityBought = 0.0M;
            //                bot.TotalBuyCost = 0.0M;
            //                bot.TotalCurrentValue = 0.0M;
            //                bot.TotalSoldAmount = 0.0M;
            //                bot.BuyTime = null;
            //                bot.SellTime = null;
            //                bot.BuyingCommision = 0.0M;
            //                bot.SoldPricePricePerCoin = 0.0M;
            //                bot.QuantitySold = 0.0M;
            //                bot.SoldCommision = 0.0M;
            //                bot.IsActivelyTrading = false;
            //                bot.CandleOpenTimeAtBuy = null;
            //                bot.CandleOpenTimeAtSell = null;
            //                bot.BuyOrSell = string.Empty;
            //                TradeDB.TradeBot.Update(bot);
            //                await TradeDB.SaveChangesAsync();
            //                isBotGrSold = true;
            //            }
            //            catch (Exception ex)
            //            {

            //                logger.Error("Exception in the loop in selling " + ex.Message);
            //            }
            //            // In the future write code to wait and see if the prices keep going up before selling abruptly.
            //            //Only when you have made sufficiently sure that prices will not go higher, then sell them.
            //        }

            //        if (isBotGrSold)
            //        {
            //            try
            //            {
            //                logger.Info("CndlHr " + candleopentime + " Grp buy Pr " + totBuyingPr.Deci().Rnd() +
            //                             " sell pr " + totCurrPr.Deci().Rnd() +
            //                             " diff % " + prDiffPerc.Deci().Rnd() + " > " + " 5 % or < - 3 % - Sell");

            //                foreach (var bot in bots)
            //                {
            //                    bot.AvailableAmountForTrading = botGrAvlAmt / 25;
            //                    bot.TotalCurrentProfit = bot.AvailableAmountForTrading - bot.OriginalAllocatedValue;
            //                    TradeDB.TradeBot.Update(bot);
            //                }
            //                await TradeDB.SaveChangesAsync();
            //            }
            //            catch (Exception ex)
            //            {

            //                logger.Error("Exception in redistributing bot values in selling " + ex.Message);
            //            }
            //        }

            //        // return;
            //    }
            //    else
            //    {
            //        logger.Info("No criteria met to make any sell this hour " + candleopentime);
            //    }


            //    #region logic to see at loss if overall you are maing more profit than planned to ensure you can continue to trade

            //    //price difference less than average expected profit,
            //    //but see if the bots are stuck and can be sold at loss to resume trading.

            //    //double totaldaysnotsold = 0;
            //    //DateTime? maxbuyDate = bots.Max(x => x.CandleOpenTimeAtBuy);

            //    //botname = bots.FirstOrDefault().Name;

            //    //if (maxbuyDate != null)
            //    //{
            //    //    totaldaysnotsold = (Signals.Max(x => x.CandleOpenTime) - Convert.ToDateTime(maxbuyDate)).TotalDays;
            //    //}

            //    //double sellAfternotsoldfordays = bots.Average(x => x.SellWhenNotSoldForDays);

            //    //if (totaldaysnotsold > sellAfternotsoldfordays) //You are stuck for a few days. See if possible.
            //    //{


            //    //    var OriginalAllocatedValue = Convert.ToDecimal(bots.Sum(x => x.OriginalAllocatedValue));

            //    //    //var starteddate = Convert.ToDateTime(bots.Max(x => x.CreatedDate));

            //    //    //int totaldayssinceStarted = (Signals.Max(x => x.CandleOpenTime) - starteddate).Days;

            //    //    decimal expectedProfit = Convert.ToDecimal(bots.Sum(x => x.TotalExpectedProfit));

            //    //    var totalavailableamount = bots.Sum(x => x.AvailableAmountForTrading);

            //    //    var totalcurrentvalueoftradingbots = bots.Sum(x => x.TotalCurrentValue);

            //    //    var totalbuyCostoftradingbots = bots.Sum(x => x.TotalBuyCost);


            //    //    decimal totalcurrentamount = 0;

            //    //    if (totalavailableamount != null)
            //    //    {
            //    //        totalcurrentamount += Convert.ToDecimal(totalavailableamount);
            //    //    }
            //    //    if (totalcurrentvalueoftradingbots != null)
            //    //    {
            //    //        totalcurrentamount += Convert.ToDecimal(totalcurrentvalueoftradingbots);
            //    //    }


            //    //    // calculate overall profit achieved so far (Sum of totalcurrentvalue+totalavailable amount - (Sum of initial investment). Ensure total current value is calculated before doing this.


            //    //    // calculate expected profit so far. compound interest of 2% from created date. 

            //    //    // calculate loss when you sell. Total Buy price - Total Current Value.

            //    //    var totalcurrentloss = totalbuyCostoftradingbots - totalcurrentvalueoftradingbots;

            //    //    // if your total profit after sold is still bigger than expected profit so far, sell.

            //    //    //if (prDiffPerc > - 19 &&
            //    //    //    (totalcurrentamount - totalcurrentloss) > expectedProfit)
            //    //    //{
            //    //    //    decimal botGrAvlAmt = 0.0M;
            //    //    //    foreach (var bot in bots)
            //    //    //    {
            //    //    //        batgroupname = bot.Name;

            //    //    //        if (!bot.IsActivelyTrading) // bought is not actively trading, but take the avail amt to calc how to distribue after sold
            //    //    //        {
            //    //    //            botGrAvlAmt += Convert.ToDecimal(bot.AvailableAmountForTrading);

            //    //    //            continue; // no data to update, its not actively trading, empty robo
            //    //    //        }

            //    //    //        var CoinPair = bot.Pair;
            //    //    //        var signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();

            //    //    //        if (signal == null)
            //    //    //        {
            //    //    //            if (CoinPair.Contains("BUSD")) CoinPair = CoinPair.Replace("BUSD", "USDT");
            //    //    //            else if (CoinPair.Contains("USDT")) CoinPair = CoinPair.Replace("USDT", "BUSD");
            //    //    //            signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();
            //    //    //        }

            //    //    //        if (signal == null)
            //    //    //        {
            //    //    //            logger.Error("Unusual. Signal came out null for pair " + CoinPair + " while trying to force sell");
            //    //    //            logger.Error("Going to next bot");
            //    //    //            continue;
            //    //    //        }

            //    //    //        bot.DayHigh = signal.DayHighPrice;
            //    //    //        bot.DayLow = signal.DayLowPrice;
            //    //    //        bot.CurrentPricePerCoin = signal.CurrentPrice;
            //    //    //        bot.TotalCurrentValue = bot.CurrentPricePerCoin * bot.QuantityBought;
            //    //    //        bot.QuantitySold = Convert.ToDecimal(bot.QuantityBought);
            //    //    //        bot.SoldCommision = bot.CurrentPricePerCoin * bot.QuantityBought * 0.075M / 100;
            //    //    //        bot.TotalSoldAmount = bot.TotalCurrentValue - bot.SoldCommision;
            //    //    //        bot.AvailableAmountForTrading = bot.TotalSoldAmount;
            //    //    //        bot.TotalCurrentProfit = bot.AvailableAmountForTrading - bot.OriginalAllocatedValue;
            //    //    //        bot.SellTime = DateTime.Now;
            //    //    //        bot.UpdatedTime = DateTime.Now;
            //    //    //        bot.SoldPricePricePerCoin = signal.CurrentPrice;
            //    //    //        bot.CandleOpenTimeAtSell = signal.CandleOpenTime;
            //    //    //        bot.BuyOrSell = "SELL";


            //    //    //        botGrAvlAmt += Convert.ToDecimal(bot.TotalSoldAmount);
            //    //    //        // create sell order (in live system)

            //    //    //        TradeBotHistory tradeBotHistory = iMapper.Map<TradeBot, TradeBotHistory>(bot);
            //    //    //        tradeBotHistory.Id = 0;
            //    //    //        await TradeDB.TradeBotHistory.AddAsync(tradeBotHistory);

            //    //    //        // reset records to buy again
            //    //    //        bot.DayHigh = 0.0M;
            //    //    //        bot.DayLow = 0.0M;
            //    //    //        bot.Pair = null;
            //    //    //        bot.BuyPricePerCoin = 0.0M;
            //    //    //        bot.CurrentPricePerCoin = 0.0M;
            //    //    //        bot.QuantityBought = 0.0M;
            //    //    //        bot.TotalBuyCost = 0.0M;
            //    //    //        bot.TotalCurrentValue = 0.0M;
            //    //    //        bot.TotalSoldAmount = 0.0M;
            //    //    //        bot.BuyTime = null;
            //    //    //        bot.SellTime = null;
            //    //    //        bot.BuyingCommision = 0.0M;
            //    //    //        bot.SoldPricePricePerCoin = 0.0M;
            //    //    //        bot.QuantitySold = 0.0M;
            //    //    //        bot.SoldCommision = 0.0M;
            //    //    //        bot.IsActivelyTrading = false;
            //    //    //        bot.CandleOpenTimeAtBuy = null;
            //    //    //        bot.CandleOpenTimeAtSell = null;
            //    //    //        bot.BuyOrSell = string.Empty;
            //    //    //        TradeDB.TradeBot.Update(bot);
            //    //    //        await TradeDB.SaveChangesAsync();
            //    //    //        isBotGrSold = true;
            //    //    //        // In the future write code to wait and see if the prices keep going up before selling abruptly.
            //    //    //        //Only when you have made sufficiently sure that prices will not go higher, then sell them.
            //    //    //    }


            //    //    //    if (isBotGrSold)
            //    //    //    {
            //    //    //        logger.Info(

            //    //    //        batgroupname + "-" + " group buy price " + Math.Round(Convert.ToDecimal(totBuyPr), 6) +
            //    //    //        " sell price " + Math.Round(Convert.ToDecimal(totCurrPr), 6) +
            //    //    //        " diff % " + Math.Round(Convert.ToDecimal(prDiffPerc), 6) +
            //    //    //        " Days not sold: " + Math.Round(totaldaysnotsold, 1) +
            //    //    //         " > allowed non traded days " + Math.Round(sellAfternotsoldfordays, 0) + ". Force Selling");



            //    //    //        foreach (var bot in bots)
            //    //    //        {
            //    //    //            bot.AvailableAmountForTrading = botGrAvlAmt / 5;
            //    //    //            TradeDB.TradeBot.Update(bot);
            //    //    //        }
            //    //    //        await TradeDB.SaveChangesAsync();
            //    //    //    }
            //    //    //}

            //    //    //else
            //    //    //{
            //    //    //    logger.Info(batgroupname + "-" + " total Amt planned to sell " + Math.Round(Convert.ToDecimal(totalcurrentamount + totalcurrentloss), 6) +
            //    //    //     " < profit expected " + Math.Round(Convert.ToDecimal(expectedProfit), 6) + " .  HODLing" +
            //    //    //       " Days not sold: " + Math.Round(totaldaysnotsold, 1) +
            //    //    //       " allowed non traded days " + Math.Round(sellAfternotsoldfordays, 0)
            //    //    //       );

            //    //    //}


            //    //    // else HODL.
            //    //}

            //    #endregion  logic to see at loss if overall you are maing more profit than planned to ensure you can continue to trade

            //}

            //await TradeDB.SaveChangesAsync();

        }

        private async Task Trade()
        {
            await ClearData();
            DB db = new DB();
            var allbots = await db.TradeBot.ToListAsync();
            List<Signal> signals = new List<Signal>();
            myCoins = await db.MyTradeFavouredCoins.AsNoTracking().ToListAsync();


            DateTime startHr = new DateTime(2021, 3, 2, 0, 0, 0);
            //DateTime startHr = new DateTime(2021, 6, 1, 0, 0, 0);

            DateTime endHr = db.Candle.AsNoTracking().Max(x => x.OpenTime); // for prod
                                                                            //  List<string> bots = new List<string>() { "Diana", "Damien", "Shatlin", "Pepper", "Eevee" };

            int i = 0;
            while (startHr < endHr)
            {
                if (i % 24 == 0) // do it only once a day
                {
                    foreach (var b in allbots)
                    {
                        var starteddate = Convert.ToDateTime(b.CreatedDate);
                        var totaldayssinceStarted = (startHr - starteddate).Days;
                        var expectedProfit = Convert.ToDecimal(b.OriginalAllocatedValue);

                        for (int j = 0; j < totaldayssinceStarted; j++)
                        {
                            expectedProfit = (expectedProfit + (expectedProfit * 0.6M / 100)); //expecting 0.4% profit daily
                        }
                        b.TotalExpectedProfit = expectedProfit;
                    }
                    await db.SaveChangesAsync();
                }


                try
                {
                    signals = await GetSignals(startHr);
                }
                catch (Exception ex)
                {
                    logger.Error("Exception in signal Generator.Returning.. " + ex.Message);
                    return;
                }

                try
                {
                    await Buy(signals);
                }
                catch (Exception ex)
                {
                    logger.Error("Exception in Buy  " + ex.Message);
                }

                try
                {
                    await Sell(signals);
                }
                catch (Exception ex)
                {

                    logger.Error("Exception in sell  " + ex.Message);
                }


                startHr = startHr.AddHours(1);
                i++;


            }

            logger.Info("---------------Trading Completed for candle hour--------------" + endHr);
        }

    }
}

/*
 * 
 * 
 * 
 * 
 * private TradeBot UpdateBotToBuy(TradeBot bot, Signal signal)
    {
        bot.IsActivelyTrading = true;
        bot.Pair = signal.Symbol;
        bot.DayHigh = signal.DayHighPr;
        bot.DayLow = signal.DayLowPr;
        bot.BuyPricePerCoin = signal.CurrPr;
        bot.QuantityBought = bot.AvailableAmountForTrading / signal.CurrPr;
        bot.BuyingCommision = bot.AvailableAmountForTrading * 0.075M / 100;
        bot.TotalBuyCost = bot.AvailableAmountForTrading + bot.BuyingCommision;
        bot.CurrentPricePerCoin = signal.CurrPr;
        bot.TotalCurrentValue = bot.AvailableAmountForTrading;
        bot.TotalCurrentProfit = bot.AvailableAmountForTrading - bot.OriginalAllocatedValue;
        bot.BuyTime = DateTime.Now;
        bot.AvailableAmountForTrading = 0;
        bot.CandleOpenTimeAtBuy = signal.CandleOpenTime;
        bot.CandleOpenTimeAtSell = null;
        bot.UpdatedTime = DateTime.Now;
        bot.BuyOrSell = "BUY";
        bot.SellTime = null;
        bot.QuantitySold = 0.0M;
        bot.SoldCommision = 0.0M;
        bot.SoldPricePricePerCoin = 0.0M;

        return bot;
    }
 //old code of buy

 if (bots[i].Order == 1)
                {
                    foreach (var sig in Signals)
                    {
                        if (sig.IsPicked) continue;
                        if (boughtCoins.Contains(sig.Symbol)) continue;

                        //buying criteria
                        //1. signals are ordered by the coins whose current price are at their lowest at the moment
                        //2. See if this price is the lowest in the last 24 hours
                        //3. See if the price difference is lower than what the bot is expecting to buy at. If yes, buy.

                        if (sig.CurrPrDiffSigAndRef < 0 &&
                            Math.Abs(sig.CurrPrDiffSigAndRef) > bots[i].BuyWhenValuePercentageIsBelow)
                        {
                            logger.Info(bots[i].Name + "-" + bots[i].Avatar + "-" + sig.Symbol + "-" +
                            "Curr Pr: " + sig.CurrPr.Rnd() +
                            " Ref Pr: " + sig.RefAvgCurrPr.Rnd() +
                            " price diff % " + sig.CurrPrDiffSigAndRef.Rnd() +
                            " > " + bots[i].BuyWhenValuePercentageIsBelow.Deci().Rnd() + " Buying ");

                            bots[i] = UpdateBotToBuy(bots[i], sig);
                            sig.IsPicked = true;
                            db.TradeBot.Update(bots[i]);

                            TradeBotHistory BotHistory = iMapr.Map<TradeBot, TradeBotHistory>(bots[i]);
                            BotHistory.Id = 0;
                            await db.TradeBotHistory.AddAsync(BotHistory);
                            isdbUpdateRequired = true; //flag that db needs to be updated, and update it at the end

                            // while ((i + 2) < bots.Count() && bots[i + 1].Order != 1) i++; 
                            // once the first bot bought, there is no point to loop through 2,3,4,5 trying to buy. They will not buy. ( What if there is a second coin whose price is 6% lower than the current buy? so dont skip. Let them try
                            boughtCoins.Add(sig.Symbol);
                            break; // coin is bought, dont need to loop through signals.
                        }
                    }
                }
                else
                {
                    foreach (var sig in Signals)
                    {
                        if (sig.IsPicked) { continue; }
                        if (boughtCoins.Contains(sig.Symbol)) continue;

                        //look for the last bots candlebuytime and get that candle.
                        var prevBotCandle = await db.Candle.Where
                            (x => x.Symbol == sig.Symbol && 
                            x.OpenTime == Convert.ToDateTime(bots[i - 1].CandleOpenTimeAtBuy)).FirstOrDefaultAsync();

                        if (prevBotCandle == null) continue;

                        var prDiffPerc = sig.CurrPr.GetDiffPerc(prevBotCandle.CurrentPrice);

                        if (prDiffPerc < 0 && Math.Abs(prDiffPerc) > bots[i].BuyWhenValuePercentageIsBelow)
                        {
                            logger.Info(bots[i].Name + "-" + bots[i].Avatar + "-" + sig.Symbol + "-" +
                            "Curr Pr: " + Math.Round(sig.CurrPr, 6) +
                            " Ref Pr: " + Math.Round(prevBotCandle.CurrentPrice, 6) +
                            " price diff % " + Math.Abs(Math.Round(prDiffPerc, 6)) +
                            " > " + Math.Round(Convert.ToDecimal(bots[i].BuyWhenValuePercentageIsBelow), 2) + " Buying ");

                            bots[i] = UpdateBotToBuy(bots[i], sig);
                            sig.IsPicked = true;
                            db.TradeBot.Update(bots[i]);
                            TradeBotHistory tradeBotHistory = iMapr.Map<TradeBot, TradeBotHistory>(bots[i]);
                            tradeBotHistory.Id = 0;
                            await db.TradeBotHistory.AddAsync(tradeBotHistory);
                            await db.SaveChangesAsync();
                            //while ((i + 2) < bots.Count() && bots[i + 1].Order != 1) i++;
                            boughtCoins.Add(sig.Symbol);
                            break;
                        }
                    }
                }
 */