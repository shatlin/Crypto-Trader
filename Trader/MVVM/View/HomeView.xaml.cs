
using AutoMapper;
using BinanceExchange.API;
using BinanceExchange.API.Client;
using BinanceExchange.API.Client.Interfaces;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Market;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Models.WebSocket;
using BinanceExchange.API.Utility;
using BinanceExchange.API.Websockets;
using log4net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Trader.Models;
using WebSocketSharp;

namespace Trader.MVVM.View
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
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
        IMapper iMapper;
        DateTime referenceStartTime = DateTime.Today;

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

            iMapper = config.CreateMapper();

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

            MyTradeFavouredCoins = await candledb.MyTradeFavouredCoins.OrderBy(x => x.PreferenceOrder).ToListAsync();

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

                    candle.RecordedTime = candleresponse[0].OpenTime.AddHours(hourDifference);
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

                    var isCandleExisting = await candledb.Candle.Where(x => x.DataSet == candle.DataSet && x.RecordedTime == candle.RecordedTime && x.Symbol == candle.Symbol).FirstOrDefaultAsync();

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
                dailycandle.RecordedTime = candleresponse[0].OpenTime.AddHours(hourDifference);
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

                var isCandleExisting = await candledb.DailyCandle.Where(x => x.DataSet == dailycandle.DataSet && x.RecordedTime == dailycandle.RecordedTime && x.Symbol == dailycandle.Symbol).FirstOrDefaultAsync();

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
                bot.CreatedDate = null;
                bot.UpdatedTime = null;
                bot.IsActivelyTrading = false;
                bot.AvailableAmountForTrading = 200;
                bot.OriginalAllocatedValue = 200;
                bot.BuyingCommision = 0.0M;
                bot.QuantitySold = 0.0M;
                bot.SoldCommision = 0.0M;
                bot.SoldPricePricePerCoin = 0.0M;
                bot.TotalCurrentProfit = 0.0M;
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
            //List<Candle> candles = new List<Candle>();

            //logger.Info("Getting Candle Started at " + DateTime.Now);

            //DB candledb = new DB();

            //var files = Directory.EnumerateFiles(@"C:\Shatlin\klines\csv", "*.csv");
            //var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

            //foreach (string file in files)
            //{

            //    string filename=file.Split('-')[0];
            //    filename=filename.Substring(filename.LastIndexOf("\\")+1);
            //    using (var reader = new StreamReader(file))
            //    {
            //        while (!reader.EndOfStream)
            //        {
            //            string line = reader.ReadLine();
            //            string[] values = line.Split(",");
            //            Candle candle = new Candle();
            //            candle.Symbol = filename;
            //            double d = Convert.ToDouble(values[0].ToString());

            //            candle.RecordedTime = Convert.ToDateTime(epoch.AddMilliseconds(d)); 
            //            candle.OpenTime = Convert.ToDateTime(epoch.AddMilliseconds(Convert.ToDouble(values[0])));
            //            candle.Open = Convert.ToDecimal(values[1]);
            //            candle.High = Convert.ToDecimal(values[2]);
            //            candle.Low = Convert.ToDecimal(values[3]);
            //            candle.Close = Convert.ToDecimal(values[4]);
            //            candle.Volume = Convert.ToDecimal(values[5]);
            //            candle.CloseTime = Convert.ToDateTime(epoch.AddMilliseconds(Convert.ToDouble(values[6])));
            //            candle.QuoteAssetVolume = Convert.ToDecimal(values[7]);
            //            candle.NumberOfTrades = Convert.ToInt32(values[8]);
            //            candle.TakerBuyBaseAssetVolume = Convert.ToDecimal(values[9]);
            //            candle.TakerBuyQuoteAssetVolume = Convert.ToDecimal(values[10]);
            //            candle.Change = 0.0M;
            //            candle.PriceChangePercent = 0.0M;
            //            candle.WeightedAveragePercent = 0.0M;
            //            candle.PreviousClosePrice = 0.0M;
            //            candle.CurrentPrice = 0.0M;
            //            candle.OpenPrice = 0.0M;
            //            candle.DayHighPrice = 0.0M;
            //            candle.DayLowPrice = 0.0M;
            //            candle.DayVolume = 0.0M;
            //            candle.DayTradeCount = 0;
            //            candle.DataSet = 0;
            //            await candledb.AddAsync(candle);
            //        }
            //    }
            //    await candledb.SaveChangesAsync();

            //}

            await UpdateData();
        }


        private async Task UpdateData()
        {
            DateTime currentdate = new DateTime(2021, 3, 1);
            DateTime lastdate = new DateTime(2021, 6, 9);
            List<Candle> selectedCandles;

            DB TradeDB = new DB();

            List<MyTradeFavouredCoins> myTradeFavouredCoins = await TradeDB.MyTradeFavouredCoins.OrderBy(x => x.PreferenceOrder).AsNoTracking().ToListAsync();

            while (currentdate < lastdate)
            {

                foreach (var favtrade in myTradeFavouredCoins)
                {

                    try
                    {
                        selectedCandles = await TradeDB.Candle.Where(x => x.Symbol.Contains(favtrade.Pair) && x.OpenTime.Date == currentdate).ToListAsync();

                        if (selectedCandles == null || selectedCandles.Count == 0)
                        {
                            continue;
                        }
                        var dayhigh = selectedCandles.Max(x => x.High);
                        var daylow = selectedCandles.Min(x => x.Low);

                        List<decimal> randomcurrentPrice = new List<decimal>();

                        randomcurrentPrice.Add(daylow);
                        randomcurrentPrice.Add(dayhigh);

                        Random rand = new Random();
                        int randomNum = 0;
                        int randomNum2 = 0;

                        for (int i = 0; i < selectedCandles.Count - 2; i++)
                        {
                            randomNum = rand.Next(0, randomcurrentPrice.Count());
                            randomNum2 = rand.Next(0, randomcurrentPrice.Count());
                            decimal random = (randomcurrentPrice[randomNum] + randomcurrentPrice[randomNum2]) / 2;
                            randomcurrentPrice.Add(random);
                        }

                        foreach (var candle in selectedCandles)
                        {
                            randomNum = rand.Next(0, randomcurrentPrice.Count());

                            candle.CurrentPrice = randomcurrentPrice[randomNum];
                            candle.DayHighPrice = selectedCandles.Max(x => x.High);
                            candle.DayLowPrice = selectedCandles.Min(x => x.Low);
                            candle.DayVolume = selectedCandles.Sum(x => x.Volume);
                            candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
                            TradeDB.Candle.Update(candle);
                        }

                        await TradeDB.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                }

                currentdate = currentdate.AddDays(1);
            }


        }


        private async Task<IOrderedEnumerable<SignalIndicator>> GetSignalIndicators(DateTime currentCandleSetDate)
        {

            //logger.Info("Signal Indicator Generation Started with candle set " + currentCandleSetDate);

            if (currentCandleSetDate == new DateTime(2021, 3, 2, 8, 0, 0))
            {

            }

            DB TradeDB = new DB();

            List<SignalIndicator> signalIndicators = new List<SignalIndicator>();

            List<MyTradeFavouredCoins> myTradeFavouredCoins = await TradeDB.MyTradeFavouredCoins.OrderBy(x => x.PreferenceOrder).AsNoTracking().ToListAsync();


            List<Candle> latestCandles = await TradeDB.Candle.AsNoTracking().Where(x => x.OpenTime == currentCandleSetDate).ToListAsync();

            DateTime referencecandleminimumtime = currentCandleSetDate.AddHours(-23);


            //logger.Info("Total Latest Candles= " + latestCandles.Count());

            //logger.Info("Reference set is from " + referencecandleminimumtime + " to " + currentCandleSetDate.AddHours(-1));

            //In Prod, get the latest candles from binance then go to remaining.

            List<Candle> ReferenceCandles = await TradeDB.Candle.AsNoTracking()
                .Where(x => x.OpenTime >= referencecandleminimumtime && x.OpenTime < currentCandleSetDate).ToListAsync();

            //logger.Info("Total Reference Candles= " + ReferenceCandles.Count());
            // List<SymbolPriceResponse> prices = await client.GetAllPrices();
#if !DEBUG
          //  List<Balance> currentBalance = await UpdateBalance(prices);
#endif


            //logger.Info("Starting to Loop through favorite coins to generate signals");

            foreach (var myfavcoin in myTradeFavouredCoins)
            {

                List<string> allusdcombinations = new List<string>() { myfavcoin.Pair + "USDT", myfavcoin.Pair + "BUSD", myfavcoin.Pair + "USDC" };

                if (myfavcoin.Pair == "BADGER")
                {

                }

                var usdCandleList = latestCandles.Where(x => allusdcombinations.Contains(x.Symbol));

                if (usdCandleList == null)
                {
                    //logger.Info("No USD Entries found in candles for " + myfavcoin.Pair + "Continuing to next coin");
                    continue;
                }

                //logger.Info(usdCandleList.Count() + " USD Entries found for " + myfavcoin.Pair + ". Creating signals ");

                #region signalIndicators

                SignalIndicator sInd = new SignalIndicator();

                #region Prefer BUSD if not available go for USDT

                var busdcandle = usdCandleList.Where(x => x.Symbol == myfavcoin.Pair + "BUSD").FirstOrDefault();
                if (busdcandle != null)
                {

                    ////logger.Info("BUSD Found for " + myfavcoin.Pair + " Using BUSD");
                    sInd.Symbol = busdcandle.Symbol;
                }
                else
                {
                    ////logger.Info("BUSD not found for " + myfavcoin.Pair + " Using USDT");
                    sInd.Symbol = myfavcoin.Pair + "USDT";
                }

                #endregion


                var selectedfavcoincandle = usdCandleList.Where(x => x.Symbol == sInd.Symbol).FirstOrDefault();


                if (selectedfavcoincandle == null)
                {

                    //logger.Info("No BUSD,USDT or USDC pairs found for  " + myfavcoin.Pair + " Continuing to next fav icon to generate signals");
                    continue;
                }

                // get the data from selectedfav coin [PROD]
                //sInd.CurrentPrice = selectedfavcoincandle.CurrentPrice;
                //sInd.DayHighPrice = selectedfavcoincandle.DayHighPrice;
                //sInd.DayLowPrice = selectedfavcoincandle.DayLowPrice;


                // get the data from selectedfav coin [For testing


                sInd.CurrentPrice = selectedfavcoincandle.CurrentPrice;
                sInd.DayHighPrice = selectedfavcoincandle.DayHighPrice;
                sInd.DayLowPrice = selectedfavcoincandle.DayLowPrice;
                sInd.CandleOpenTime= selectedfavcoincandle.OpenTime;

                //myfavcoinCandleList the list of records for the same coin in USDT,BUSD and USDC. Get some of these for indicator.
                sInd.DayVolume = usdCandleList.Sum(x => x.DayVolume);
                sInd.DayTradeCount = usdCandleList.Sum(x => x.DayTradeCount);


                //  var refcans = ReferenceCandles.Where(x => allusdcombinations.Contains(x.Symbol));

                var allrefswithUsdComboinations = ReferenceCandles.Where(x => allusdcombinations.Contains(x.Symbol));
                if (allrefswithUsdComboinations == null || allrefswithUsdComboinations.Count() == 0)
                {

                    //logger.Info("No BUSD,USDT or USDC pairs found for  " + myfavcoin.Pair + " in allrefswithUsdComboinations . Continuing to next fav icon to generate signals ");
                    continue;
                }

                sInd.ReferenceSetHighPrice = allrefswithUsdComboinations.Max(x => x.CurrentPrice);
                sInd.ReferenceSetLowPrice = allrefswithUsdComboinations.Min(x => x.CurrentPrice);
                sInd.ReferenceSetAverageCurrentPrice = allrefswithUsdComboinations.Average(x => x.CurrentPrice);
                sInd.ReferenceSetDayVolume = allrefswithUsdComboinations.Average(x => x.DayVolume);
                sInd.ReferenceSetDayTradeCount = (int)allrefswithUsdComboinations.Average(x => x.DayTradeCount);

                sInd.DayPriceDifferencePercentage =
                     ((sInd.DayHighPrice - sInd.DayLowPrice) /
                     ((sInd.DayHighPrice + sInd.DayLowPrice) / 2)) * 100;

                sInd.PriceDifferenceCurrentAndHighPercentage = Math.Abs(
                        (sInd.DayHighPrice - sInd.CurrentPrice) / ((sInd.DayHighPrice + sInd.CurrentPrice) / 2) * 100);

                sInd.PriceDifferenceCurrentAndLowPercentage = Math.Abs(
                    ((sInd.DayLowPrice - sInd.CurrentPrice) / ((sInd.DayLowPrice + sInd.CurrentPrice) / 2)) * 100);

                var dayAveragePrice = (sInd.DayHighPrice + sInd.DayLowPrice) / 2;

                if (sInd.CurrentPrice < dayAveragePrice)
                {
                    sInd.IsCloseToDayLow = true;
                }
                else
                {
                    sInd.IsCloseToDayHigh = true;
                }

                //logger.Info(selectedfavcoincandle.Symbol + "  CurrentPrice: " + Math.Round(sInd.CurrentPrice, 6) +
                  //" DayHighPrice: " + Math.Round(sInd.DayHighPrice, 6) +
                  // " DayLowPrice: " + Math.Round(sInd.DayLowPrice, 6) +
                  //   " DayVolume: " + Math.Round(sInd.DayVolume, 6) +
                  //     " DayTradeCount: " + sInd.DayTradeCount

                  //);


                //logger.Info(
                //    "Reference Set : " + selectedfavcoincandle.Symbol +
                // " Max  Price: " + Math.Round(sInd.ReferenceSetHighPrice, 6) +
                // " Min  Price: " + Math.Round(sInd.ReferenceSetLowPrice, 6) +
                // " Avg  Price: " + Math.Round(sInd.ReferenceSetAverageCurrentPrice, 6)
                // +
                //" DayVolume: " + Math.Round(sInd.ReferenceSetDayVolume, 6)
                //  +" DayTradeCount: " + Math.Round(sInd.ReferenceSetDayVolume, 6)
                //  );

                //logger.Info(
               //     "Price Diff % : high & low " + Math.Round(sInd.DayPriceDifferencePercentage, 6) +
               //     " Price Close to Day Low? " +sInd.IsCloseToDayLow +
               //     " Price Close to Day High? " + sInd.IsCloseToDayHigh
               //);


                signalIndicators.Add(sInd);
            }

            var reorderedSignalIndicatorList = signalIndicators.OrderByDescending(x => x.ReferenceSetDayTradeCount);

            //logger.Info("Signal Indicator Generation Completed");

            return reorderedSignalIndicatorList;


            #endregion
        }

        private async Task PerformBuys(IOrderedEnumerable<SignalIndicator> SignalGeneratorList)
        {

            logger.Info("Perform  Buys Started");
            DB TradeDB = new DB();

            var tradebots = await TradeDB.TradeBot.OrderBy(x => x.Id).ToListAsync();

            #region buying scan

            List<string> alreadyboughtCoins = tradebots.Where(x => x.Pair != null).Select(x => x.Pair).ToList();

            for (int i = 0; i < tradebots.Count(); i++)
            {
                if (tradebots[i].IsActivelyTrading) //trading, go to the next one
                {
                    logger.Info(tradebots[i].Name + " " + tradebots[i].Avatar + " is actively trading with " + tradebots[i].Pair + " so going to the next bot");
                    continue;
                }
                if (tradebots[i].Order == 1)
                {
                    logger.Info(tradebots[i].Name + " " + tradebots[i].Avatar + " is firt bot in the list and not actively trading so no refence amounts to trade with,this bot will scan the market condition for a favorable buy, lowest amount for the day");

                    logger.Info(tradebots[i].Name + " " + tradebots[i].Avatar + " Starting to go through signals");

                    foreach (var indicator in SignalGeneratorList)
                    {
                        logger.Info("Assessing signals for " + indicator.Symbol + " to see if its buyable ");

                        if (indicator.IsPicked)
                        {
                            logger.Info(indicator.Symbol + " is picked up by another bot, so not buying the same coin.Continuing to next one");
                            continue;
                        }

                        if (indicator.IsIgnored)
                        {
                            logger.Info(indicator.Symbol + " is already ignored, so not attempting the same coin.Continuing to next one");
                            continue;
                        }
                        if (alreadyboughtCoins.Contains(indicator.Symbol))
                        {
                            logger.Info(indicator.Symbol + " is already in trading in another bot, so not buying it. Continuing to next one");
                            continue;
                        }
                        var indicatorcurrentprice = indicator.CurrentPrice;
                        var indicatorSymbol = indicator.Symbol;
                        var indicatoroldprice = indicator.ReferenceSetAverageCurrentPrice;

                        var pricedifferencepercentage = (indicatorcurrentprice - indicatoroldprice) /
                        ((indicatorcurrentprice + indicatoroldprice / 2)) * 100;

                        logger.Info(indicator.Symbol +
                            " Current Price " + Math.Round(indicator.CurrentPrice, 6) +
                            " Ref Set Price " + Math.Round(indicatoroldprice, 6) +
                            " price diff % betn curr & ref " + Math.Round(pricedifferencepercentage, 6) +
                            " Bot config:  " + Math.Round(Convert.ToDecimal(tradebots[i].BuyWhenValuePercentageIsBelow), 2));
                        if (
                            pricedifferencepercentage < 0 &&
                            Math.Abs(pricedifferencepercentage) > tradebots[i].BuyWhenValuePercentageIsBelow
                            )
                        {
                            logger.Info(indicator.Symbol + 
                                " price diff % betn curr & ref " + Math.Round(pricedifferencepercentage, 6) + ". Its Good. Buying ");

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
                            tradebots[i].CandleOpenTime = indicator.CandleOpenTime;
                            TradeDB.TradeBot.Update(tradebots[i]);

                            logger.Info(indicator.Symbol +
                               " Bought for a total cost of : " + Math.Round(Convert.ToDecimal(tradebots[i].TotalBuyCost), 6) +
                               " Bought Quantity " + tradebots[i].QuantityBought +
                               " Coin Price" + Math.Round(Convert.ToDecimal(tradebots[i].BuyPricePerCoin), 6));
                                    await TradeDB.SaveChangesAsync();
                                    indicator.IsPicked = true;
                            // Update buy record, set it active, in live system, you will be issuing a buy order

                            while ((i + 3) < tradebots.Count() && tradebots[i + 2].Order == 1)
                            {
                                i++;
                            }

                            alreadyboughtCoins.Add(indicator.Symbol);

                            break;
                        }
                        else
                        {
                            logger.Info(indicator.Symbol +
                                  " price diff % betn curr & ref set " + Math.Round(pricedifferencepercentage, 6) +
                                  ". Not Good. Not Buying ");
                            indicator.IsIgnored=true;
                            continue;
                        }
                    }
                }
                else
                {

                    logger.Info(tradebots[i].Name + " " + tradebots[i].Avatar + " not the first bot, so the previous bot will be actively trading. The lower ones are support bots, buy only when the current prices are so much lower than the first bot ");

                    var previousCoinPrice = tradebots[i - 1].BuyPricePerCoin;
                    var previousCoinPair = tradebots[i - 1].Pair;

                    logger.Info("previous bot's Coin Price " + previousCoinPrice +
                        " previous bot's Coin Pair " + previousCoinPair);

                    DateTime PreviousCoinBuyTime = Convert.ToDateTime(tradebots[i - 1].CandleOpenTime);

                    foreach (var indicator in SignalGeneratorList)
                    {
                        if (indicator.IsPicked)
                        {
                            logger.Info(indicator.Symbol + " is picked up by another bot, so not buying the same coin.Continuing to next one");
                            continue;
                        }

                        if (indicator.IsIgnored)
                        {
                            logger.Info(indicator.Symbol + " is already ignored, so not attempting the same coin.Continuing to next one");
                            continue;
                        }
                        
                        if (alreadyboughtCoins.Contains(indicator.Symbol))
                        {
                            logger.Info(indicator.Symbol + " is already in trading in another bot, so not buying it. Continuing to next one");
                            continue;
                        }

                        var indicatorcurrentprice = indicator.CurrentPrice;
                        var indicatorSymbol = indicator.Symbol;

                        logger.Info(indicator.Symbol + " current Price is " + indicatorcurrentprice);

                        logger.Info(indicator.Symbol + " Looking at the current sginal indicator coin's price when the previous bot's coin was bought ");

                        var selectedCandle = await TradeDB.Candle.Where(
                            x => x.Symbol == indicator.Symbol &&
                            x.RecordedTime == PreviousCoinBuyTime
                            ).FirstOrDefaultAsync();

                        if(selectedCandle==null)
                        {
                            continue;
                        }

                        var indicatoroldprice = selectedCandle.CurrentPrice;

                        logger.Info(indicator.Symbol + " found the closest Candle at " + selectedCandle.OpenTime +
                            " The old price " + indicatoroldprice);

                        var pricedifferencepercentage = (indicatorcurrentprice - indicatoroldprice) / ((indicatorcurrentprice + indicatoroldprice / 2)) * 100;

                        logger.Info(indicator.Symbol + " Price diff at time " + selectedCandle.OpenTime +
                              " Current Price " + indicatorcurrentprice +
                              " The old price " + indicatoroldprice);

                        logger.Info(indicator.Symbol +
                          " price diff % betn curr & old " + pricedifferencepercentage +
                          " Bot config:  " + tradebots[i].BuyWhenValuePercentageIsBelow);

                        if (pricedifferencepercentage < 0 && Math.Abs(pricedifferencepercentage) > tradebots[i].BuyWhenValuePercentageIsBelow)
                        {
                            logger.Info(indicator.Symbol +
                         " price diff % betn curr & ref " + Math.Round(pricedifferencepercentage, 6) +
                         " .Good. Buying ");

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
                            tradebots[i].CandleOpenTime = indicator.CandleOpenTime;
                            TradeDB.TradeBot.Update(tradebots[i]);

                            logger.Info(indicator.Symbol +
                       " Bought for a total cost of : " + Math.Round(Convert.ToDecimal(tradebots[i].TotalBuyCost), 6) +
                       " Bought Quantity " + tradebots[i].QuantityBought +
                       " Coin Price" + Math.Round(Convert.ToDecimal(tradebots[i].BuyPricePerCoin), 6));
                            await TradeDB.SaveChangesAsync();
                            indicator.IsPicked = true;

                            alreadyboughtCoins.Add(indicator.Symbol);

                            while ((i + 3) < tradebots.Count() && tradebots[i + 2].Order == 1)
                            {
                                i++;
                            }


                            break;

                            // Update buy record, set it active, in live system, you will be issuing a buy order
                        }
                        else
                        {

                            logger.Info(indicator.Symbol +
                                  " price diff % betn curr & ref " + Math.Round(pricedifferencepercentage, 6) +
                                  " .Not Good. Not Buying ");
                            indicator.IsIgnored = true;
                            continue;
                        }
                    }
                }

            }

            #endregion buying scan

            logger.Info("Perform  Buys Completed");
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

        #region Algorithm

        // take all bots one by one.
        //if he is active -ingore ( for now)
        // take the successor who is not active
        //take the coin with biggest trades happening, who has dropped to the expectation of that bot
        // suppose  the predecessor was ADA and was bought two days back with value 1.5, now you can buy ADA if its value is 5% down to the previous one
        // now look at the price of the new coin you are going to buy at the time ADA was bought and compare that to the current price.
        //if that price is also 5% less and the coin is a better buy than ADA, buy that coin.

        // for the next coin, select the next coin in the list and repeat the same.


        // For the second set, prefer different set of coins.

        // Create Trade mini bots that each group will handle 1/5th of the investment.
        // Each mini bot will have 5 avatars that will wait for the right time to buy or sell.
        // 1st bot will start the buying and waiting to make a profit. If Market is going down, based on the rules set second will jump to buy at a lower price

        //  Sell all of batch when your profit goes 5% overall and then start again.

        #endregion

        private async Task Trade()
        {
            //[TODO]
            // if candles are being updated, wait till they complete.
            //Or if the candles are going to be updated soon, wait for that

            await ClearData();
            DB TradeDB = new DB();

            List<SignalIndicator> signalIndicators = new List<SignalIndicator>();
            Counter counter = await TradeDB.Counter.AsNoTracking().FirstOrDefaultAsync();

            //while(counter.IsCandleCurrentlyBeingUpdated)
            //{
            //    Thread.Sleep(10000);
            //    counter = await TradeDB.Counter.AsNoTracking().FirstOrDefaultAsync();
            //}

            //var minutedifference = (DateTime.Now - counter.CandleLastUpdatedTime).TotalMinutes;

            //if (minutedifference >12)
            //{
            //    Thread.Sleep(10000);
            //    counter = await TradeDB.Counter.AsNoTracking().FirstOrDefaultAsync();
            //    minutedifference = (DateTime.Now - counter.CandleLastUpdatedTime).TotalMinutes;
            //    logger.Info(" Candle retrieved only " + minutedifference + " minutes back. Dont need to get again");

            //}

            // int currentCandleSet = (counter.CandleCurrentSet - 1);  //PROD

            DateTime latestcandledate = TradeDB.Candle.AsNoTracking().Max(x => x.OpenTime); // for prod
            DateTime startingcandledate = new DateTime(2021, 3, 1, 23, 0, 0);

            // 2021-06-08 23:00:00.0000000

            while (startingcandledate < latestcandledate)
            {
                var SignalGeneratorList = await GetSignalIndicators(startingcandledate);
                await PerformBuys(SignalGeneratorList);
                // await PerformSells(SignalGeneratorList);
                startingcandledate = startingcandledate.AddHours(1);
            }

        }

        #region Unused Methods
        private static async Task<string> Execute()
        {
            //Provide your configuration and keys here, this allows the client to function as expected.
            string apiKey = "YOUR_API_KEY";
            string secretKey = "YOUR_SECRET_KEY";

            //Building a test logger
            var logger = LogManager.GetLogger(typeof(MainWindow));
            logger.Debug("Logging Test");

            logger.Debug("--------------------------");
            logger.Debug("BinanceExchange API - Tester");
            logger.Debug("--------------------------");

            //Initialise the general client client with config
            var client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = apiKey,
                SecretKey = secretKey,
                Logger = logger,
            });

            logger.Debug("Interacting with Binance...");

            bool DEBUG_ALL = false;

            /*
             *  Code Examples - Make sure you adjust value of DEBUG_ALL
             */
            if (DEBUG_ALL)
            {
                // Test the Client
                await client.TestConnectivity();

                // Get All Orders
                var allOrdersRequest = new AllOrdersRequest()
                {
                    Symbol = "ETHBTC",
                    Limit = 5,
                };

                allOrdersRequest = new AllOrdersRequest()
                {
                    Symbol = TradingPairSymbols.BTCPairs.ETH_BTC,
                    Limit = 5,
                };
                // Get All Orders
                var allOrders = await client.GetAllOrders(allOrdersRequest);

                // Get the order book, and use the cache
                var orderBook = await client.GetOrderBook("ETHBTC", true);

                // Cancel an order
                var cancelOrder = await client.CancelOrder(new CancelOrderRequest()
                {
                    NewClientOrderId = "123456",
                    OrderId = 523531,
                    OriginalClientOrderId = "789",
                    Symbol = "ETHBTC",
                });

                // Create an order with varying options
                var createOrder = await client.CreateOrder(new CreateOrderRequest()
                {
                    IcebergQuantity = 100,
                    Price = 230,
                    Quantity = 0.6m,
                    Side = OrderSide.Buy,
                    Symbol = "ETHBTC",
                    Type = OrderType.Market,
                });

                // Get account information
                var accountInformation = await client.GetAccountInformation(3500);

                // Get account trades
                var accountTrades = await client.GetAccountTrades(new AllTradesRequest()
                {
                    FromId = 352262,
                    Symbol = "ETHBTC",
                });

                // Get a list of Compressed aggregate trades with varying options
                var aggTrades = await client.GetCompressedAggregateTrades(new GetCompressedAggregateTradesRequest()
                {
                    StartTime = DateTime.UtcNow.AddDays(-1),
                    Symbol = "ETHBTC",
                });

                // Get current open orders for the specified symbol
                var currentOpenOrders = await client.GetCurrentOpenOrders(new CurrentOpenOrdersRequest()
                {
                    Symbol = "ETHBTC",
                });

                // Get daily ticker
                var dailyTicker = await client.GetDailyTicker("ETHBTC");

                // Get Symbol Order Book Ticket
                var symbolOrderBookTicker = await client.GetSymbolOrderBookTicker();

                // Get Symbol Order Price Ticker
                var symbolOrderPriceTicker = await client.GetSymbolsPriceTicker();

                // Query a specific order on Binance
                var orderQuery = await client.QueryOrder(new QueryOrderRequest()
                {
                    OrderId = 5425425,
                    Symbol = "ETHBTC",
                });

                // Firing off a request and catching all the different exception types.
                //try
                //{
                //    accountTrades = await client.GetAccountTrades(new AllTradesRequest()
                //    {
                //        FromId = 352262,
                //        Symbol = "ETHBTC",
                //    });
                //}
                //catch (BinanceBadRequestException badRequestException)
                //{

                //}
                //catch (BinanceServerException serverException)
                //{

                //}
                //catch (BinanceTimeoutException timeoutException)
                //{

                //}
                //catch (BinanceException unknownException)
                //{

                //}
            }

            // Start User Data Stream, ping and close
            var userData = await client.StartUserDataStream();
            await client.KeepAliveUserDataStream(userData.ListenKey);
            await client.CloseUserDataStream(userData.ListenKey);

            // Manual WebSocket usage
            var manualBinanceWebSocket = new InstanceBinanceWebSocketClient(client);
            var socketId = manualBinanceWebSocket.ConnectToDepthWebSocket("ETHBTC", b =>
            {
                System.Console.Clear();
                logger.Debug($"{JsonConvert.SerializeObject(b.BidDepthDeltas, Formatting.Indented)}");
                System.Console.SetWindowPosition(0, 0);
            });


            #region Advanced Examples           
            // This builds a local Kline cache, with an initial call to the API and then continues to fill
            // the cache with data from the WebSocket connection. It is quite an advanced example as it provides 
            // additional options such as an Exit Func<T> or timeout, and checks in place for cache instances. 
            // You could provide additional logic here such as populating a database, ping off more messages, or simply
            // timing out a fill for the cache.
            var dict = new Dictionary<string, KlineCacheObject>();
            //await BuildAndUpdateLocalKlineCache(client, "BNBBTC", KlineInterval.OneMinute,
            //    new GetKlinesCandlesticksRequest()
            //    {
            //        StartTime = DateTime.UtcNow.AddHours(-1),
            //        EndTime = DateTime.UtcNow,
            //        Interval = KlineInterval.OneMinute,
            //        Symbol = "BNBBTC"
            //    }, new WebSocketConnectionFunc(15000), dict);

            // This builds a local depth cache from an initial call to the API and then continues to fill 
            // the cache with data from the WebSocket
            var localDepthCache = await BuildLocalDepthCache(client);
            // Build the Buy Sell volume from the results
            var volume = ResultTransformations.CalculateTradeVolumeFromDepth("BNBBTC", localDepthCache);

            #endregion

            logger.Debug("Complete.");
            Thread.Sleep(6000);
            manualBinanceWebSocket.CloseWebSocketInstance(socketId);
            System.Console.ReadLine();

            return "";
        }

        /// <summary>
        /// Build local Depth cache from WebSocket and API Call example.
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private static async Task<Dictionary<string, DepthCacheObject>> BuildLocalDepthCache(IBinanceClient client)
        {
            // Code example of building out a Dictionary local cache for a symbol using deltas from the WebSocket
            var localDepthCache = new Dictionary<string, DepthCacheObject> {{ "BNBBTC", new DepthCacheObject()
            {
                Asks = new Dictionary<decimal, decimal>(),
                Bids = new Dictionary<decimal, decimal>(),
            }}};
            var bnbBtcDepthCache = localDepthCache["BNBBTC"];

            // Get Order Book, and use Cache
            var depthResults = await client.GetOrderBook("BNBBTC", true, 100);
            //Populate our depth cache
            depthResults.Asks.ForEach(a =>
            {
                if (a.Quantity != 0.00000000M)
                {
                    bnbBtcDepthCache.Asks.Add(a.Price, a.Quantity);
                }
            });
            depthResults.Bids.ForEach(a =>
            {
                if (a.Quantity != 0.00000000M)
                {
                    bnbBtcDepthCache.Bids.Add(a.Price, a.Quantity);
                }
            });

            // Store the last update from our result set;
            long lastUpdateId = depthResults.LastUpdateId;
            using (var binanceWebSocketClient = new DisposableBinanceWebSocketClient(client))
            {
                binanceWebSocketClient.ConnectToDepthWebSocket("BNBBTC", data =>
                {
                    if (lastUpdateId < data.UpdateId)
                    {
                        data.BidDepthDeltas.ForEach((bd) =>
                        {
                            CorrectlyUpdateDepthCache(bd, bnbBtcDepthCache.Bids);
                        });
                        data.AskDepthDeltas.ForEach((ad) =>
                        {
                            CorrectlyUpdateDepthCache(ad, bnbBtcDepthCache.Asks);
                        });
                    }
                    lastUpdateId = data.UpdateId;
                    System.Console.Clear();
                    System.Console.WriteLine($"{JsonConvert.SerializeObject(bnbBtcDepthCache, Formatting.Indented)}");
                    System.Console.SetWindowPosition(0, 0);
                });

                Thread.Sleep(8000);
            }
            return localDepthCache;
        }

        /// <summary>
        /// Advanced approach to building local Kline Cache from WebSocket and API Call example (refactored)
        /// </summary>
        /// <param name="binanceClient">The BinanceClient instance</param>
        /// <param name="symbol">The Symbol to request</param>
        /// <param name="interval">The interval for Klines</param>
        /// <param name="klinesCandlesticksRequest">The initial request for Klines</param>
        /// <param name="webSocketConnectionFunc">The function to determine exiting the websocket (can be timeout or Func based on external params)</param>
        /// <param name="cacheObject">The cache object. Must always be provided, and can exist with data.</param>
        /// <returns></returns>
        public static async Task BuildAndUpdateLocalKlineCache(IBinanceClient binanceClient,
            string symbol,
            KlineInterval interval,
            GetKlinesCandlesticksRequest klinesCandlesticksRequest,
            WebSocketConnectionFunc webSocketConnectionFunc,
            Dictionary<string, KlineCacheObject> cacheObject)
        {
            Guard.AgainstNullOrEmpty(symbol);
            Guard.AgainstNull(webSocketConnectionFunc);
            Guard.AgainstNull(klinesCandlesticksRequest);
            Guard.AgainstNull(cacheObject);

            long epochTicks = new DateTime(1970, 1, 1).Ticks;

            if (cacheObject.ContainsKey(symbol))
            {
                if (cacheObject[symbol].KlineInterDictionary.ContainsKey(interval))
                {
                    throw new Exception(
                        "Symbol and Interval pairing already provided, please use a different interval/symbol or pair.");
                }
                cacheObject[symbol].KlineInterDictionary.Add(interval, new KlineIntervalCacheObject());
            }
            else
            {
                var klineCacheObject = new KlineCacheObject
                {
                    KlineInterDictionary = new Dictionary<KlineInterval, KlineIntervalCacheObject>()
                };
                cacheObject.Add(symbol, klineCacheObject);
                cacheObject[symbol].KlineInterDictionary.Add(interval, new KlineIntervalCacheObject());
            }

            // Get Kline Results, and use Cache
            long ticks = klinesCandlesticksRequest.StartTime.Value.Ticks;
            var startTimeKeyTime = (ticks - epochTicks) / TimeSpan.TicksPerSecond;
            var klineResults = await binanceClient.GetKlinesCandlesticks(klinesCandlesticksRequest);

            var oneMinKlineCache = cacheObject[symbol].KlineInterDictionary[interval];
            oneMinKlineCache.TimeKlineDictionary = new Dictionary<long, KlineCandleStick>();
            var instanceKlineCache = oneMinKlineCache.TimeKlineDictionary;
            //Populate our kline cache with initial results
            klineResults.ForEach(k =>
            {
                instanceKlineCache.Add(((k.OpenTime.Ticks - epochTicks) / TimeSpan.TicksPerSecond), new KlineCandleStick()
                {
                    Close = k.Close,
                    High = k.High,
                    Low = k.Low,
                    Open = k.Open,
                    Volume = k.Volume,
                });
            });

            // Store the last update from our result set;
            using (var binanceWebSocketClient = new DisposableBinanceWebSocketClient(binanceClient))
            {
                binanceWebSocketClient.ConnectToKlineWebSocket(symbol, interval, data =>
                {
                    var keyTime = (data.Kline.StartTime.Ticks - epochTicks) / TimeSpan.TicksPerSecond;
                    var klineObj = new KlineCandleStick()
                    {
                        Close = data.Kline.Close,
                        High = data.Kline.High,
                        Low = data.Kline.Low,
                        Open = data.Kline.Open,
                        Volume = data.Kline.Volume,
                    };
                    if (!data.Kline.IsBarFinal)
                    {
                        if (keyTime < startTimeKeyTime)
                        {
                            return;
                        }

                        TryAddUpdateKlineCache(instanceKlineCache, keyTime, klineObj);
                    }
                    else
                    {
                        TryAddUpdateKlineCache(instanceKlineCache, keyTime, klineObj);
                    }
                    System.Console.Clear();
                    System.Console.WriteLine($"{JsonConvert.SerializeObject(instanceKlineCache, Formatting.Indented)}");
                    System.Console.SetWindowPosition(0, 0);
                });
                if (webSocketConnectionFunc.IsTimout)
                {
                    Thread.Sleep(webSocketConnectionFunc.Timeout);
                }
                else
                {
                    while (true)
                    {
                        if (!webSocketConnectionFunc.ExitFunction())
                        {
                            // Throttle Application
                            Thread.Sleep(100);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }


        private static void TryAddUpdateKlineCache(Dictionary<long, KlineCandleStick> primary, long keyTime, KlineCandleStick klineObj)
        {
            if (primary.ContainsKey(keyTime))
            {
                primary[keyTime] = klineObj;
            }
            else
            {
                primary.Add(keyTime, klineObj);
            }
        }

        private static void CorrectlyUpdateDepthCache(TradeResponse bd, Dictionary<decimal, decimal> depthCache)
        {
            const decimal defaultIgnoreValue = 0.00000000M;

            if (depthCache.ContainsKey(bd.Price))
            {
                if (bd.Quantity == defaultIgnoreValue)
                {
                    depthCache.Remove(bd.Price);
                }
                else
                {
                    depthCache[bd.Price] = bd.Quantity;
                }
            }
            else
            {
                if (bd.Quantity != defaultIgnoreValue)
                {
                    depthCache[bd.Price] = bd.Quantity;
                }
            }
        }

        #endregion Unused Methods

    }
}
