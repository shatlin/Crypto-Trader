
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
                bot.CreatedDate = DateTime.Now;
                bot.UpdatedTime = null;
                bot.IsActivelyTrading = false;
                bot.AvailableAmountForTrading = 200;
                bot.OriginalAllocatedValue = 200;
                bot.BuyingCommision = 0.0M;
                bot.QuantitySold = 0.0M;
                bot.SoldCommision = 0.0M;
                bot.SoldPricePricePerCoin = 0.0M;
                bot.TotalCurrentProfit = 0.0M;
                bot.CandleOpenTimeAtBuy = null;
                bot.BuyOrSell = string.Empty;
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

            // await UpdateData();
        }


        private async Task UpdateData()
        {
            DateTime currentdate = new DateTime(2021, 6, 1);
            DateTime lastdate = new DateTime(2021, 6, 11);
            List<Candle> selectedCandles;

            DB TradeDB = new DB();

            List<MyTradeFavouredCoins> myTradeFavouredCoins = await TradeDB.MyTradeFavouredCoins.AsNoTracking().ToListAsync();

            while (currentdate < lastdate)
            {

                foreach (var favtrade in myTradeFavouredCoins)
                {

                    try
                    {
                        selectedCandles = await TradeDB.Candle.Where(x => x.Symbol.Contains(favtrade.Pair) && x.OpenTime.Date == currentdate
                        && x.CurrentPrice <= 0).ToListAsync();

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

                        logger.Error(" Updating candle error " + ex.Message);
                    }
                }

                currentdate = currentdate.AddDays(1);
            }


        }


        List<MyTradeFavouredCoins> myfavCoins = new List<MyTradeFavouredCoins>();

        private async Task<List<Signal>> GetSignals(DateTime currentCandleSetDate, string botname)
        {

            DB TradeDB = new DB();

            List<Signal> signals = new List<Signal>();
            List<Candle> latestCandles = await TradeDB.Candle.AsNoTracking().Where(x => x.OpenTime == currentCandleSetDate).ToListAsync();
            DateTime refCandlMinTime = currentCandleSetDate.AddHours(-23);

            List<Candle> ReferenceCandles = await TradeDB.Candle.AsNoTracking()
                .Where(x => x.OpenTime >= refCandlMinTime && x.OpenTime < currentCandleSetDate).ToListAsync();

            myfavCoins = await db.MyTradeFavouredCoins.AsNoTracking().ToListAsync();

            foreach (var myfavcoin in myfavCoins)
            {
                List<string> allusdcombinations = new List<string>() { myfavcoin.Pair + "USDT", myfavcoin.Pair + "BUSD", myfavcoin.Pair + "USDC" };
                var usdCandleList = latestCandles.Where(x => allusdcombinations.Contains(x.Symbol));
                if (usdCandleList == null) continue;

                Signal sig = new Signal();

                #region Prefer BUSD if not available go for USDT

                var busdcandle = usdCandleList.Where(x => x.Symbol == myfavcoin.Pair + "BUSD").FirstOrDefault();
                if (busdcandle != null) sig.Symbol = busdcandle.Symbol;

                else sig.Symbol = myfavcoin.Pair + "USDT";

                var selFavCoinCandle = usdCandleList.Where(x => x.Symbol == sig.Symbol).FirstOrDefault();

                if (selFavCoinCandle == null) continue;

                sig.CurrentPrice = selFavCoinCandle.CurrentPrice;
                sig.DayHighPrice = selFavCoinCandle.DayHighPrice;
                sig.DayLowPrice = selFavCoinCandle.DayLowPrice;
                sig.CandleOpenTime = selFavCoinCandle.OpenTime;

                sig.DayVolume = usdCandleList.Sum(x => x.DayVolume);
                sig.DayTradeCount = usdCandleList.Sum(x => x.DayTradeCount);

                var allrefswithUsds = ReferenceCandles.Where(x => allusdcombinations.Contains(x.Symbol));
                if (allrefswithUsds == null || allrefswithUsds.Count() == 0) continue;

                sig.ReferenceSetHighPrice = allrefswithUsds.Max(x => x.CurrentPrice);
                sig.ReferenceSetLowPrice = allrefswithUsds.Min(x => x.CurrentPrice);
                sig.ReferenceSetAverageCurrentPrice = allrefswithUsds.Average(x => x.CurrentPrice);
                sig.ReferenceSetDayVolume = allrefswithUsds.Average(x => x.DayVolume);
                sig.ReferenceSetDayTradeCount = (int)allrefswithUsds.Average(x => x.DayTradeCount);

                sig.DayPriceDifferencePercentage = ((sig.DayHighPrice - sig.DayLowPrice) / ((sig.DayHighPrice + sig.DayLowPrice) / 2)) * 100;
                sig.PriceDifferenceCurrentAndHighPercentage = Math.Abs((sig.DayHighPrice - sig.CurrentPrice) / ((sig.DayHighPrice + sig.CurrentPrice) / 2) * 100);
                sig.PriceDifferenceCurrentAndLowPercentage = Math.Abs(((sig.DayLowPrice - sig.CurrentPrice) / ((sig.DayLowPrice + sig.CurrentPrice) / 2)) * 100);

                var dayAveragePrice = (sig.DayHighPrice + sig.DayLowPrice) / 2;
                if (sig.CurrentPrice < dayAveragePrice) sig.IsCloseToDayLow = true;
                else sig.IsCloseToDayHigh = true;

                signals.Add(sig);
            }

            return signals.OrderByDescending(x => x.ReferenceSetDayTradeCount).ToList();

            #endregion
        }

        List<string> boughtCoins = new List<string>();

        private TradeBot UpdateBotToBuy(TradeBot bot, Signal indicator)
        {
            bot.IsActivelyTrading = true;
            bot.Pair = indicator.Symbol;
            bot.DayHigh = indicator.DayHighPrice;
            bot.DayLow = indicator.DayLowPrice;
            bot.BuyPricePerCoin = indicator.CurrentPrice;
            bot.QuantityBought = bot.AvailableAmountForTrading / indicator.CurrentPrice;
            bot.BuyingCommision = bot.AvailableAmountForTrading * 0.075M / 100;
            bot.TotalBuyCost = bot.AvailableAmountForTrading + bot.BuyingCommision;
            bot.CurrentPricePerCoin = indicator.CurrentPrice;
            bot.TotalCurrentValue = bot.AvailableAmountForTrading;
            bot.TotalCurrentProfit = bot.TotalCurrentValue- bot.OriginalAllocatedValue; 
            bot.BuyTime = DateTime.Now;
            bot.AvailableAmountForTrading = 0;
            bot.CandleOpenTimeAtBuy = indicator.CandleOpenTime;
            bot.CandleOpenTimeAtSell = null;
            bot.UpdatedTime = DateTime.Now;
            bot.BuyOrSell = "BUY";
            bot.SellTime = null;
            bot.QuantitySold = 0.0M;
            bot.SoldCommision = 0.0M;
            bot.SoldPricePricePerCoin = 0.0M;

            var starteddate = Convert.ToDateTime(bot.CreatedDate);
            int totaldayssinceStarted = (DateTime.Now - starteddate).Days;

            decimal expectedProfit = Convert.ToDecimal(bot.OriginalAllocatedValue);
            for (int j = 0; j < totaldayssinceStarted; j++)
            {
                expectedProfit = (expectedProfit + (expectedProfit * 2 / 100));
            }
            bot.TotalExpectedProfit = expectedProfit;

            return bot;

        }

        private async Task Buy(List<Signal> Signals, string botname)
        {
            DB db = new DB();

            var bots = await db.TradeBot.Where(x => x.Name == botname).OrderBy(x => x.Id).ToListAsync();

            boughtCoins = await db.TradeBot.Where(x => x.Pair != null).Select(x => x.Pair).ToListAsync();

            for (int i = 0; i < bots.Count(); i++)
            {
                
                if (bots[i].IsActivelyTrading)
                {
                    var botsSignal = Signals.Where(x => x.Symbol == bots[i].Pair).FirstOrDefault();

                    if (botsSignal != null)
                    {
                        bots[i].DayHigh = botsSignal.DayHighPrice;
                        bots[i].DayLow = botsSignal.DayLowPrice;
                        bots[i].CurrentPricePerCoin = botsSignal.CurrentPrice;
                        bots[i].TotalCurrentValue = bots[i].CurrentPricePerCoin * bots[i].QuantityBought;
                        bots[i].TotalCurrentProfit = bots[i].TotalCurrentValue - bots[i].OriginalAllocatedValue;
                        bots[i].UpdatedTime = DateTime.Now;

                        var starteddate = Convert.ToDateTime(bots[i].CreatedDate);
                        int totaldayssinceStarted = (DateTime.Now - starteddate).Days;

                        decimal expectedProfit = Convert.ToDecimal(bots[i].OriginalAllocatedValue);

                        for (int j = 0; j < totaldayssinceStarted; j++)
                        {
                            expectedProfit = (expectedProfit + (expectedProfit * 2 / 100));
                        }
                        bots[i].TotalExpectedProfit = expectedProfit;
                    }

                    continue; // actively trading, but should I not update the records to the current price?
                }
                if (bots[i].Order == 1)
                {
                    foreach (var sig in Signals)
                    {
                        if (sig.IsPicked) continue;
                        if (boughtCoins.Contains(sig.Symbol)) continue;

                        var sigCurrPri = sig.CurrentPrice;
                        var sigSymbol = sig.Symbol;
                        var sigOldPrice = sig.ReferenceSetAverageCurrentPrice;
                        var priceDiffPerc = (sigCurrPri - sigOldPrice) / ((sigCurrPri + sigOldPrice) / 2) * 100;

                        if (priceDiffPerc < 0 && Math.Abs(priceDiffPerc) > bots[i].BuyWhenValuePercentageIsBelow)
                        {
                            logger.Info(bots[i].Name + "-" + bots[i].Avatar + "-" + sig.Symbol + "-" +
                            "Curr Pr: " + Math.Round(sigCurrPri, 6) +
                            " Ref Pr: " + Math.Round(sigOldPrice, 6) +
                            " price diff % " + Math.Abs(Math.Round(priceDiffPerc, 6)) +
                            " > " + Math.Round(Convert.ToDecimal(bots[i].BuyWhenValuePercentageIsBelow), 2) + " Buying ");

                            bots[i] = UpdateBotToBuy(bots[i], sig);
                            sig.IsPicked = true;
                            db.TradeBot.Update(bots[i]);
                            TradeBotHistory tradeBotHistory = iMapper.Map<TradeBot, TradeBotHistory>(bots[i]);
                            tradeBotHistory.Id = 0;
                            await db.TradeBotHistory.AddAsync(tradeBotHistory);
                            await db.SaveChangesAsync();
                            while ((i + 2) < bots.Count() && bots[i + 1].Order != 1) i++;
                            boughtCoins.Add(sig.Symbol);
                            break;
                        }
                        else // the buy criteria is not met, so you are not buying anything. Nothing to update
                        {

                            continue;
                        }
                    }
                }
                else
                {
                    var prevCoinPr = bots[i - 1].BuyPricePerCoin;
                    var prevCoinPair = bots[i - 1].Pair;
                    DateTime prevCoinBuyTime = Convert.ToDateTime(bots[i - 1].CandleOpenTimeAtBuy);

                    foreach (var sig in Signals)
                    {
                        if (sig.IsPicked) { continue; }
                        if (boughtCoins.Contains(sig.Symbol)) continue;

                        var sigCurrPr = sig.CurrentPrice;
                        var sigSymbol = sig.Symbol;

                        var oldpriceCandle = await db.Candle.Where(
                            x => x.Symbol == sig.Symbol &&
                            x.OpenTime == prevCoinBuyTime
                            ).FirstOrDefaultAsync();

                        if (oldpriceCandle == null) continue;

                        var indOldPr = oldpriceCandle.CurrentPrice;
                        var prDiffPerc = (sigCurrPr - indOldPr) / ((sigCurrPr + indOldPr) / 2) * 100;

                        if (prDiffPerc < 0 && Math.Abs(prDiffPerc) > bots[i].BuyWhenValuePercentageIsBelow)
                        {
                            logger.Info(bots[i].Name + "-" + bots[i].Avatar + "-" + sig.Symbol + "-" +
                            "Curr Pr: " + Math.Round(sigCurrPr, 6) +
                            " Ref Pr: " + Math.Round(indOldPr, 6) +
                            " price diff % " + Math.Abs(Math.Round(prDiffPerc, 6)) +
                            " > " + Math.Round(Convert.ToDecimal(bots[i].BuyWhenValuePercentageIsBelow), 2) + " Buying ");

                            bots[i] = UpdateBotToBuy(bots[i], sig);
                            sig.IsPicked = true;
                            db.TradeBot.Update(bots[i]);
                            TradeBotHistory tradeBotHistory = iMapper.Map<TradeBot, TradeBotHistory>(bots[i]);
                            tradeBotHistory.Id = 0;
                            await db.TradeBotHistory.AddAsync(tradeBotHistory);
                            await db.SaveChangesAsync();
                            while ((i + 2) < bots.Count() && bots[i + 1].Order != 1) i++;
                            boughtCoins.Add(sig.Symbol);
                            break;
                        }
                        else // the buy criteria is not met, so you are not buying anything. Nothing to update
                        {
                            continue;
                        }
                    }
                }

            }
            await db.SaveChangesAsync();
        }

        private async Task Sell(List<Signal> Signals, string botname)
        {

            DB TradeDB = new DB();
            var bots = await TradeDB.TradeBot.Where(x => x.Name == botname).OrderBy(x => x.Id).ToListAsync();
            var batgroupname = string.Empty;
            decimal? totBuyPr = 0;
            decimal? totCurrPr = 0;

            decimal? avgProfitPerc= bots.Average(x=>x.SellWhenProfitPercentageIsAbove);
            double totaldaysnotsold=0;
            DateTime? maxbuyDate=bots.Max(x=>x.CandleOpenTimeAtBuy);
            
            if(maxbuyDate!=null)
            {
                totaldaysnotsold=(DateTime.Now- Convert.ToDateTime(maxbuyDate)).TotalDays;
            }

            double sellAfternotsoldfordays=bots.Average(x=>x.SellWhenNotSoldForDays);

            foreach (var bot in bots)
            {
                if (!bot.IsActivelyTrading) continue; // empty bot, nothing to update

                decimal qtyBght = Convert.ToDecimal(bot.QuantityBought);
                
                totBuyPr += bot.TotalBuyCost;

                foreach (var signal in Signals)
                {
                    if (signal.Symbol == bot.Pair)
                    {
                        var currentPrice = signal.CurrentPrice;
                        totCurrPr += (signal.CurrentPrice * qtyBght) + ((signal.CurrentPrice * qtyBght) * (0.075M / 100));
                        break;
                    }
                }
            }

            if (totBuyPr > 0)
            {
                var prDiffPerc = (totCurrPr - totBuyPr) / ((totCurrPr + totBuyPr) / 2) * 100;

                bool isBotGrSold = false;

                if (prDiffPerc > avgProfitPerc) //Your total profit is more than avgProfitPerc. Sell it and get ready to buy again.
                {
                    decimal botGrAvlAmt = 0.0M;
                    foreach (var bot in bots)
                    {
                        batgroupname = bot.Name;

                        if (!bot.IsActivelyTrading) // bought is not actively trading, but take the avail amt to calc how to distribue after sold
                        {
                            botGrAvlAmt += Convert.ToDecimal(bot.AvailableAmountForTrading);
                            continue;
                        }

                        var CoinPair = bot.Pair;
                        var signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();

                        if (signal == null)
                        {
                            if (CoinPair.Contains("BUSD")) CoinPair = CoinPair.Replace("BUSD", "USDT");
                            else if (CoinPair.Contains("USDT")) CoinPair = CoinPair.Replace("USDT", "BUSD");
                            signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();
                        }

                        bot.DayHigh = signal.DayHighPrice;
                        bot.DayLow = signal.DayLowPrice;
                        bot.CurrentPricePerCoin = signal.CurrentPrice;
                        bot.TotalCurrentValue = bot.CurrentPricePerCoin * bot.QuantityBought;
                        bot.QuantitySold = Convert.ToDecimal(bot.QuantityBought);
                        bot.SoldCommision = bot.CurrentPricePerCoin * bot.QuantityBought * 0.075M / 100;
                        bot.TotalSoldAmount = bot.TotalCurrentValue - bot.SoldCommision;
                        bot.AvailableAmountForTrading = bot.TotalSoldAmount;
                        bot.TotalCurrentProfit = 0.0M; // field not required
                        bot.SellTime = DateTime.Now;
                        bot.UpdatedTime = DateTime.Now;
                        bot.SoldPricePricePerCoin = signal.CurrentPrice;
                        bot.CandleOpenTimeAtSell = signal.CandleOpenTime;
                        bot.BuyOrSell = "SELL";
                        botGrAvlAmt += Convert.ToDecimal(bot.TotalSoldAmount);
                        // create sell order (in live system)

                        TradeBotHistory tradeBotHistory = iMapper.Map<TradeBot, TradeBotHistory>(bot);
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
                        bot.TotalCurrentProfit = 0.0M;
                        bot.IsActivelyTrading = false;
                        bot.CandleOpenTimeAtBuy = null;
                        bot.CandleOpenTimeAtSell = null;
                        bot.BuyOrSell = string.Empty;
                        TradeDB.TradeBot.Update(bot);
                        await TradeDB.SaveChangesAsync();
                        isBotGrSold = true;
                        // In the future write code to wait and see if the prices keep going up before selling abruptly.
                        //Only when you have made sufficiently sure that prices will not go higher, then sell them.
                    }

                    if (isBotGrSold)
                    {
                        logger.Info(batgroupname + "-" + " group buy price " + Math.Round(Convert.ToDecimal(totBuyPr), 6) +
                       " sell price " + Math.Round(Convert.ToDecimal(totCurrPr), 6) +
                       " diff % " + Math.Round(Convert.ToDecimal(prDiffPerc), 6) + " > "+ avgProfitPerc+ " % . selling ");

                        foreach (var bot in bots)
                        {
                            bot.AvailableAmountForTrading = botGrAvlAmt / 5;
                            TradeDB.TradeBot.Update(bot);
                        }
                        await TradeDB.SaveChangesAsync();
                    }
                }

                else if(totaldaysnotsold > sellAfternotsoldfordays)
                { 

                    var OriginalAllocatedValue = Convert.ToDecimal(bots.Sum(x=>x.OriginalAllocatedValue));

                    var starteddate=Convert.ToDateTime(bots.Max(x=>x.CreatedDate));

                    int totaldayssinceStarted=(DateTime.Now- starteddate).Days;
                    
                    decimal expectedProfit = OriginalAllocatedValue;

                    for (int i=0;i< totaldayssinceStarted;i++)
                    {
                        expectedProfit = (expectedProfit + (expectedProfit * 2/100));
                    }

                    var totalavailablamount=bots.Sum(x=>x.AvailableAmountForTrading);

                    var totalcurrentvalueoftradingbots=bots.Sum(x=>x.TotalCurrentValue);
                    
                    var totalbuyCostoftradingbots= bots.Sum(x => x.TotalBuyCost);

                    

                    decimal totalcurrentamount=0;

                    if(totalavailablamount!=null)
                    {
                        totalcurrentamount+= Convert.ToDecimal(totalavailablamount);
                    }
                    if (totalcurrentvalueoftradingbots != null)
                    {
                        totalcurrentamount += Convert.ToDecimal(totalcurrentvalueoftradingbots);
                    }



                    // calculate overall profit achieved so far (Sum of totalcurrentvalue+totalavailable amount - (Sum of initial investment). Ensure total current value is calculated before doing this.

                    decimal overallprofit = totalcurrentamount - OriginalAllocatedValue;

                    // calculate expected profit so far. compound interest of 2% from created date. 

                    // calculate loss when you sell. Total Buy price - Total Current Value.

                    var totalcurrentloss = totalbuyCostoftradingbots - totalcurrentvalueoftradingbots;

                    // if your total profit after sold is still bigger than expected profit so far, sell.

                    if((overallprofit + totalcurrentloss)> expectedProfit)
                    {
                        decimal botGrAvlAmt = 0.0M;

                        foreach (var bot in bots)
                        {
                            batgroupname = bot.Name;

                            if (!bot.IsActivelyTrading)
                            {
                                botGrAvlAmt += Convert.ToDecimal(bot.AvailableAmountForTrading);
                                continue;
                            }

                            var CoinPair = bot.Pair;
                            var signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();

                            if (signal == null)
                            {
                                if (CoinPair.Contains("BUSD")) CoinPair = CoinPair.Replace("BUSD", "USDT");
                                else if (CoinPair.Contains("USDT")) CoinPair = CoinPair.Replace("USDT", "BUSD");
                                signal = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();
                            }

                            bot.DayHigh = signal.DayHighPrice;
                            bot.DayLow = signal.DayLowPrice;
                            bot.CurrentPricePerCoin = signal.CurrentPrice;
                            bot.TotalCurrentValue = bot.CurrentPricePerCoin * bot.QuantityBought;
                            bot.QuantitySold = Convert.ToDecimal(bot.QuantityBought);
                            bot.SoldCommision = bot.CurrentPricePerCoin * bot.QuantityBought * 0.075M / 100;
                            bot.TotalSoldAmount = bot.TotalCurrentValue - bot.SoldCommision;
                            bot.AvailableAmountForTrading = bot.TotalSoldAmount;
                            bot.TotalCurrentProfit = 0.0M; // field not required
                            bot.SellTime = DateTime.Now;
                            bot.UpdatedTime = DateTime.Now;
                            bot.SoldPricePricePerCoin = signal.CurrentPrice;
                            bot.CandleOpenTimeAtSell = signal.CandleOpenTime;
                            bot.BuyOrSell = "SELL";
                            botGrAvlAmt += Convert.ToDecimal(bot.TotalSoldAmount);
                            // create sell order (in live system)

                            TradeBotHistory tradeBotHistory = iMapper.Map<TradeBot, TradeBotHistory>(bot);
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
                            bot.TotalCurrentProfit = 0.0M;
                            bot.IsActivelyTrading = false;
                            bot.CandleOpenTimeAtBuy = null;
                            bot.CandleOpenTimeAtSell = null;
                            bot.BuyOrSell = string.Empty;
                            TradeDB.TradeBot.Update(bot);
                            await TradeDB.SaveChangesAsync();
                            isBotGrSold = true;
                            // In the future write code to wait and see if the prices keep going up before selling abruptly.
                            //Only when you have made sufficiently sure that prices will not go higher, then sell them.
                        }

                        if (isBotGrSold)
                        {
                            logger.Info(batgroupname + "-" + " group buy price " + Math.Round(Convert.ToDecimal(totBuyPr), 6) +
                           " sell price " + Math.Round(Convert.ToDecimal(totCurrPr), 6) +
                           " diff % " + Math.Round(Convert.ToDecimal(prDiffPerc), 6) + " . selling at a lose to resume trading");

                            foreach (var bot in bots)
                            {
                                bot.AvailableAmountForTrading = botGrAvlAmt / 5;
                                TradeDB.TradeBot.Update(bot);
                            }
                            await TradeDB.SaveChangesAsync();
                        }

                    }

                    // else HODL.
                }


            }

        }

        private async Task Trade()
        {
            await ClearData();
            DB db = new DB();


            List<Signal> signals = new List<Signal>();

            DateTime latestCandleDate = db.Candle.AsNoTracking().Max(x => x.OpenTime); // for prod
            DateTime currentCandleDate = new DateTime(2021, 3, 1, 23, 0, 0);

            List<string> bots = new List<string>() { "Diana", "Damien", "Shatlin", "Pepper", "Eevee" };

            while (currentCandleDate < latestCandleDate)
            {
                foreach (string bot in bots)
                {
                    logger.Info("Starting Trade Cycle with Candle Open time " + currentCandleDate);
                    signals = await GetSignals(currentCandleDate, bot);
                    await Buy(signals, bot);
                    await Sell(signals, bot);
                    currentCandleDate = currentCandleDate.AddHours(1);
                }
            }

        }

    }
}
