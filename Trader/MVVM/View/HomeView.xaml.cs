
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
        public List<MyCoins> MyTradeFavouredCoins { get; set; }
        public List<string> MyTradedCoinList { get; set; }
        public DispatcherTimer CandleDataRetrieverTimer;
        public DispatcherTimer TraderTimer;
        public DispatcherTimer CandleDailyDataRetrieverTimer;
        BinanceClient client;
        ILog logger;
        DB db;
        int intervalMins = 15;
        double hourDifference = 2;
        IMapper iMapr;
        DateTime referenceStartTime = DateTime.Today;
        List<MyCoins> myCoins = new List<MyCoins>();
        List<string> boughtCoins = new List<string>();

        //  bool isfifteenminTrade = false;

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
            TraderTimer.Interval = new TimeSpan(0, intervalMins, 0);
            //  TraderTimer.Start();
            logger = LogManager.GetLogger(typeof(MainWindow));


            logger.Info(
            "Candle Time" +
            "|Player" +
            "|Symbol" +
            "|Day High" +
            "|Day Low" +
            "|Current Price Of Coin" +
            "|Total Buy Cost" +
            "|Total Sold Amount" +
            "|Price Difference" +
            "|Notes" +
            "|Buy Or Sell");

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
                cfg.CreateMap<Player, PlayerHist>();
            });

            iMapr = config.CreateMapper();

        }


        private void SetGrid()
        {
            DB GridDB = new DB();
            BalanceDG.ItemsSource = GridDB.Balance.AsNoTracking().OrderByDescending(x => x.DiffPerc).ToList();
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

        private async void btnTrade15_Click(object sender, RoutedEventArgs e)
        {
            //isfifteenminTrade = true;
            await Trade();
        }

        private async void btnClearPlayer_Click(object sender, RoutedEventArgs e)
        {
            await ClearData();

        }

        private async void btnCollect15Data_Click(object sender, RoutedEventArgs e)
        {

            logger.Info("Collect Data Started at " + DateTime.Now);

            var files = Directory.EnumerateFiles(@"C:\Shatlin\klines", "*.csv");
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

            int i = 1;
            foreach (string file in files)
            {
                using (var candledb = new DB())
                {

                    i++;
                    string filename = file.Split('-')[0];
                    filename = filename.Substring(filename.LastIndexOf("\\") + 1);
                    using (var reader = new StreamReader(file))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line = reader.ReadLine();
                            string[] values = line.Split(",");
                            Candle15 candle = new Candle15();
                            candle.Symbol = filename;
                            double d = Convert.ToDouble(values[0].ToString());
                            candle.RecordedTime = DateTime.Now;
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
                            candle.CurrentPrice = candle.Close;
                            candle.OpenPrice = candle.Open;
                            candle.DayHighPrice = 0.0M;
                            candle.DayLowPrice = 0.0M;
                            candle.DayVolume = 0.0M;
                            candle.DayTradeCount = 0;


                            await candledb.AddAsync(candle);

                        }

                        logger.Info(i + " : " + file + " Processing Completed ");


                    }
                    await candledb.SaveChangesAsync();
                }
            }

            logger.Info("----------All file Processing Completed-------------- ");
            await UpdateData15();
        }

        private async void TraderTimer_Tick(object sender, EventArgs e)
        {
            await Trade();
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
                                bal.TotCurrentPrice = asset.Free + asset.Locked;
                                bal.TotBoughtPrice = asset.Free + asset.Locked;
                                await BalanceDB.Balance.AddAsync(bal);
                                continue;
                            }

                            foreach (var price in prices)
                            {
                                try
                                {
                                    if (price.Symbol.ToUpper() == asset.Asset.ToUpper() + "USDT")
                                    {
                                        bal.TotCurrentPrice = bal.Free * price.Price;
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
                            bal.TotBoughtPrice = spentprice;
                            bal.AvgBuyCoinPrice = (bal.TotBoughtPrice / bal.Free) + (bal.TotBoughtPrice / bal.Free) * 1 / 100;

                            try
                            {
                                bal.CurrCoinPrice = prices.Where(x => x.Symbol == asset.Asset + "USDT").FirstOrDefault().Price;
                            }
                            catch
                            {
                                bal.CurrCoinPrice = (bal.TotCurrentPrice / bal.Free);
                            }
                            bal.Difference = bal.TotCurrentPrice - bal.TotBoughtPrice;
                            bal.DiffPerc = (bal.TotCurrentPrice - bal.TotBoughtPrice) / ((bal.TotCurrentPrice + bal.TotBoughtPrice) / 2) * 100;

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
                        totalinvested += balance.TotBoughtPrice;
                        totalcurrent += balance.TotCurrentPrice;
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

        private async Task UpdateData15()
        {
            DateTime currentdate = new DateTime(2021, 3, 1, 23, 0, 0);
            DateTime lastdate = new DateTime(2021, 6, 16, 23, 0, 0);


            while (currentdate <= lastdate)
            {
                using (var TradeDB = new DB())
                {

                    List<Candle15> selectedCandles;
                    List<MyCoins> myTradeFavouredCoins = await TradeDB.MyCoins.AsNoTracking().ToListAsync();

                    try
                    {
                        foreach (var favtrade in myTradeFavouredCoins)
                        {

                            selectedCandles = await TradeDB.Candle15.Where(x => x.Symbol.Contains(favtrade.Coin) && x.OpenTime.Date == currentdate.Date
                            ).ToListAsync();

                            if (selectedCandles == null || selectedCandles.Count == 0) continue;

                            foreach (var candle in selectedCandles)
                            {
                                candle.DayHighPrice = selectedCandles.Max(x => x.High);
                                candle.DayLowPrice = selectedCandles.Min(x => x.Low);
                                candle.DayVolume = selectedCandles.Sum(x => x.Volume);
                                candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
                                TradeDB.Candle15.Update(candle);
                            }
                            await TradeDB.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(" Updating candle error " + ex.Message);
                    }

                }

                currentdate = currentdate.AddDays(1);
            }

            logger.Info(" Updating data Completed ");
        }

        private async Task ClearData()
        {
            DB TradeDB = new DB();

            await TradeDB.Database.ExecuteSqlRawAsync("TRUNCATE TABLE PlayerHist");

            var players = await TradeDB.Player.ToListAsync();

            foreach (var player in players)
            {
                player.Pair = null;
                player.DayHigh = 0.0M;
                player.DayLow = 0.0M;
                player.BuyPricePerCoin = 0.0M;
                player.CurrentPricePerCoin = 0.0M;
                player.QuantityBought = 0.0M;
                player.TotalBuyCost = 0.0M;
                player.TotalCurrentValue = 0.0M;
                player.TotalSoldAmount = 0.0M;
                player.BuyTime = null;
                player.SellTime = null;
                player.CreatedDate = new DateTime(2021, 6, 16, 0, 0, 0);
                player.UpdatedTime = null;
                player.IsTrading = false;
                player.AvailableAmountForTrading = 100;
                player.OriginalAllocatedValue = 100;
                player.BuyingCommision = 0.0M;
                player.QuantitySold = 0.0M;
                player.SoldCommision = 0.0M;
                player.SoldPricePricePerCoin = 0.0M;
                player.TotalCurrentProfit = 0.0M;
                player.CandleOpenTimeAtBuy = null;
                player.BuyOrSell = string.Empty;
                player.CandleOpenTimeAtSell = null;
                player.TotalExpectedProfit = 0.0M;
                player.BuyCandleId = 0;
                player.SellCandleId = 0;
                TradeDB.Update(player);
            }
            await TradeDB.SaveChangesAsync();


        }

        private async Task<List<Candle15>> GetCandles15()
        {

            //#TODO Update bots with current price when getting candles.

            DB candledb = new DB();
            logger.Info("Getting Candle Started at " + DateTime.Now);
            var counter = await candledb.Counter.FirstOrDefaultAsync();
            List<Candle15> candles = new List<Candle15>();
            try
            {
                if (counter.IsCandleBeingUpdated)
                {
                    return candles;
                }

                //var minutedifference = (DateTime.Now - counter.CandleLastUpdatedTime).TotalMinutes;

                //if (minutedifference < (intervalminutes - 5))
                //{
                //    logger.Info(" Candle retrieved only " + minutedifference + " minutes back. Dont need to get again");
                //    return candles;
                //}

                counter.IsCandleBeingUpdated = true;
                candledb.Update(counter);
                await candledb.SaveChangesAsync();

                var StartlastCandleMinute = candledb.Candle15.Max(x => x.OpenTime);

                MyTradeFavouredCoins = await candledb.MyCoins.ToListAsync();

                var prices = await client.GetAllPrices();
                // await UpdateBalance(prices);



                #region get all missing candles

                var totalmins = (DateTime.Now - StartlastCandleMinute).TotalMinutes;

                if (totalmins > 15)
                {
                    foreach (var coin in MyTradeFavouredCoins)
                    {
                        var pricesofcoin = prices.Where(x => x.Symbol.Contains(coin.Coin));
                        if (pricesofcoin == null || pricesofcoin.Count() == 0)
                        {
                            continue;
                        }
                        foreach (var price in pricesofcoin)
                        {

                            if (price.Symbol != coin.Coin + "USDT") // if the price symbol doesnt contain usdt ignore those coins
                            {
                                continue;
                            }

                            //if (price.Symbol != coin.Coin + "BUSD" && price.Symbol != coin.Coin + "USDC" && price.Symbol != coin.Coin + "USDT") // if the price symbol doesnt contain usdt and busd ignore those coins
                            //{
                            //    continue;
                            //}
                            //var pricechangeresponse = await client.GetDailyTicker(price.Symbol);
                            GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
                            cr.Limit = 100;
                            cr.Symbol = price.Symbol;
                            cr.Interval = KlineInterval.FifteenMinutes;
                            cr.StartTime = Convert.ToDateTime(StartlastCandleMinute).AddMinutes(15);
                            cr.EndTime = DateTime.Now.AddMinutes(-15);
                            var candleresponse = await client.GetKlinesCandlesticks(cr);

                            foreach (var candleResp in candleresponse)
                            {
                                Candle15 addCandle = new Candle15();

                                addCandle.Symbol = cr.Symbol;
                                addCandle.Open = candleResp.Open;
                                addCandle.RecordedTime = DateTime.Now;
                                addCandle.OpenTime = candleResp.OpenTime.AddHours(hourDifference);
                                addCandle.High = candleResp.High;
                                addCandle.Low = candleResp.Low;
                                addCandle.Close = candleResp.Close;
                                addCandle.Volume = candleResp.Volume;
                                addCandle.CloseTime = candleResp.CloseTime.AddHours(hourDifference);
                                addCandle.QuoteAssetVolume = candleResp.QuoteAssetVolume;
                                addCandle.NumberOfTrades = candleResp.NumberOfTrades;
                                addCandle.TakerBuyBaseAssetVolume = candleResp.TakerBuyBaseAssetVolume;
                                addCandle.TakerBuyQuoteAssetVolume = candleResp.TakerBuyQuoteAssetVolume;
                                addCandle.Change = 0;
                                addCandle.PriceChangePercent = 0;
                                addCandle.WeightedAveragePercent = 0;
                                addCandle.PreviousClosePrice = 0;
                                addCandle.CurrentPrice = candleResp.Close;
                                addCandle.OpenPrice = candleResp.Open;
                                addCandle.DayHighPrice = 0;
                                addCandle.DayLowPrice = 0;
                                addCandle.DayVolume = 0;
                                addCandle.DayTradeCount = 0;

                                var isCandleExisting = await candledb.Candle15.Where(x => x.OpenTime == addCandle.OpenTime && x.Symbol == addCandle.Symbol).FirstOrDefaultAsync();

                                if (isCandleExisting == null)
                                {
                                    candles.Add(addCandle);
                                    await candledb.Candle15.AddAsync(addCandle);
                                    await candledb.SaveChangesAsync();
                                }
                            }
                        }
                    }

                    var UpdatedlastCandleMinutes = candledb.Candle15.Max(x => x.OpenTime);
                    List<Candle15> selectedCandles;
                    while (StartlastCandleMinute <= UpdatedlastCandleMinutes)
                    {
                        try
                        {
                            foreach (var favtrade in MyTradeFavouredCoins)
                            {
                                selectedCandles = await candledb.Candle15.Where(x => x.Symbol.Contains(favtrade.Coin) && x.OpenTime.Date == StartlastCandleMinute.Date).ToListAsync();

                                if (selectedCandles == null || selectedCandles.Count == 0) continue;

                                foreach (var candle in selectedCandles)
                                {
                                    candle.DayHighPrice = selectedCandles.Max(x => x.High);
                                    candle.DayLowPrice = selectedCandles.Min(x => x.Low);
                                    candle.DayVolume = selectedCandles.Sum(x => x.Volume);
                                    candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
                                    candledb.Candle15.Update(candle);
                                }
                                await candledb.SaveChangesAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(" Updating candle error " + ex.Message);
                        }
                        StartlastCandleMinute = StartlastCandleMinute.AddDays(1);
                    }

                }
                #endregion get all missing candles

                foreach (var coin in MyTradeFavouredCoins)
                {
                    var pricesofcoin = prices.Where(x => x.Symbol.Contains(coin.Coin));
                    if (pricesofcoin == null || pricesofcoin.Count() == 0)
                    {
                        continue;
                    }
                    foreach (var price in pricesofcoin)
                    {
                        if (price.Symbol != coin.Coin + "USDT") // if the price symbol doesnt contain usdt ignore those coins
                        {
                            continue;
                        }

                        //if (price.Symbol != coin.Coin + "BUSD" && price.Symbol != coin.Coin + "USDC" && price.Symbol != coin.Coin + "USDT") // if the price symbol doesnt contain usdt and busd ignore those coins
                        //{
                        //    continue;
                        //}
                        Candle15 candle = new Candle15();
                        var pricechangeresponse = await client.GetDailyTicker(price.Symbol);
                        GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
                        cr.Limit = 1;
                        cr.Symbol = price.Symbol;
                        cr.Interval = KlineInterval.FifteenMinutes;

                        var candleresponse = await client.GetKlinesCandlesticks(cr);
                        candle.RecordedTime = DateTime.Now;
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
                        // candle.DataSet = candlecurrentSet;

                        var isCandleExisting = await candledb.Candle15.Where(x => x.OpenTime == candle.OpenTime && x.Symbol == candle.Symbol).FirstOrDefaultAsync();

                        if (isCandleExisting == null)
                        {
                            candles.Add(candle);
                            await candledb.Candle15.AddAsync(candle);
                            await candledb.SaveChangesAsync();
                        }
                        else
                        {

                            candledb.Candle15.Update(isCandleExisting);
                            await candledb.SaveChangesAsync();
                        }
                    }
                }

                counter.IsCandleBeingUpdated = false;
                candledb.Counter.Update(counter);
                await candledb.SaveChangesAsync();
                logger.Info("Getting Candle Completed at " + DateTime.Now);
            }
            catch (Exception ex)
            {

                logger.Info("Exception in Getting Candle  " + ex.Message);
                counter.IsCandleBeingUpdated = false;
                candledb.Counter.Update(counter);
                await candledb.SaveChangesAsync();
                throw ex;
            }
            return candles;
        }

        private async Task<List<Signal>> GetSignals15(DateTime cndlHr)
        {

            DB TradeDB = new DB();

            List<Signal> signals = new List<Signal>();

            List<Candle15> latestCndls = await TradeDB.Candle15.AsNoTracking().Where(x => x.OpenTime == cndlHr).ToListAsync();

            // DateTime refCandlMinTime = currentCandleSetDate.AddHours(-23);

            List<Candle15> refCndls = await TradeDB.Candle15.AsNoTracking()
                .Where(x => x.OpenTime >= cndlHr.AddHours(-23) && x.OpenTime < cndlHr).ToListAsync();

            foreach (var myfavcoin in myCoins)
            {
                
                    #region dealwithUSDT only

                    Signal sig = new Signal();

                    sig.Symbol = myfavcoin.Coin + "USDT";

                    var selCndl = latestCndls.Where(x => x.Symbol == sig.Symbol).FirstOrDefault();

                    if (selCndl == null) continue;

                    var selRefCndls = refCndls.Where(x => x.Symbol == sig.Symbol);

                    if (selRefCndls == null || selRefCndls.Count() == 0) continue;

                    #endregion

                    #region Prefer BUSD if not available go for USDT

                    //List<string> usdStrings = new List<string>()
                    //    { myfavcoin.Coin + "USDT"}; //, myfavcoin.Coin + "BUSD", myfavcoin.Coin + "USDC"

                    //var usdCndlList = latestCndls.Where(x => usdStrings.Contains(x.Symbol));

                    //if (usdCndlList == null) continue;

                    //var busdcandle = usdCndlList.Where(x => x.Symbol == myfavcoin.Coin + "BUSD").FirstOrDefault();

                    //Signal sig = new Signal();

                    //if (busdcandle != null) sig.Symbol = busdcandle.Symbol;
                    //else 

                    //    sig.Symbol = myfavcoin.Coin + "USDT";

                    //var selCndl = usdCndlList.Where(x => x.Symbol == sig.Symbol).FirstOrDefault();

                    //if (selCndl == null) continue;

                    //var usdRefCndls = refCndls.Where(x => usdStrings.Contains(x.Symbol));

                    //if (usdRefCndls == null || usdRefCndls.Count() == 0) continue;

                    #endregion

                    sig.CurrPr = selCndl.CurrentPrice;
                    sig.DayHighPr = selCndl.DayHighPrice;
                    sig.DayLowPr = selCndl.DayLowPrice;
                    sig.CandleOpenTime = selCndl.OpenTime;
                    sig.CandleId = selCndl.Id;
                    sig.DayVol = selRefCndls.Sum(x => x.DayVolume);
                    sig.DayTradeCount = selRefCndls.Sum(x => x.DayTradeCount);
                    sig.RefHighPr = selRefCndls.Max(x => x.CurrentPrice);
                    sig.RefLowPr = selRefCndls.Min(x => x.CurrentPrice);
                    sig.RefAvgCurrPr = selRefCndls.Average(x => x.CurrentPrice);
                    sig.RefDayVol = selRefCndls.Average(x => x.DayVolume);
                    sig.RefDayTradeCount = (int)selRefCndls.Average(x => x.DayTradeCount);

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

            return signals.OrderBy(x => x.PrDiffCurrAndLowPerc).ToList();
        }

        private async Task Buy(List<Signal> Signals)
        {

            //problem to fix. You will keep on buying if its going down ( No: You should be able to sell if you need to buy)

            if (Signals == null || Signals.Count() == 0)
            {
                // logger.Info("No signals found. returning from buying");
                return;
            }

            #region definitions

            var candleopentime = Signals.FirstOrDefault().CandleOpenTime;

            DB db = new DB();
            var players = await db.Player.OrderBy(x => x.Id).ToListAsync();

            boughtCoins = await db.Player.Where(x => x.Pair != null).
                          Select(x => x.Pair).ToListAsync();

            bool isdbUpdateRequired = false;

            #endregion definitions

            //bool isanybuyingdone = false;

            foreach (var player in players)
            {
                #region if player is currently trading, just update stats and go to next player

                if (player.IsTrading)
                {
                    var playersSignal = Signals.Where(x => x.Symbol == player.Pair).FirstOrDefault();

                    if (playersSignal != null)
                    {
                        player.DayHigh = playersSignal.DayHighPr;
                        player.DayLow = playersSignal.DayLowPr;
                        player.CurrentPricePerCoin = playersSignal.CurrPr;
                        player.TotalCurrentValue = player.CurrentPricePerCoin * player.QuantityBought;
                        player.TotalCurrentProfit = player.TotalCurrentValue - player.OriginalAllocatedValue;
                        player.UpdatedTime = DateTime.Now;
                        isdbUpdateRequired = true;
                    }
                    continue;
                }

                #endregion if player is currently trading, just update stats and go to next player

                foreach (var sig in Signals)
                {
                    if (sig.IsPicked) { continue; }
                    if (boughtCoins.Contains(sig.Symbol)) continue;

                    //buying criteria
                    //1. signals are ordered by the coins whose current price are at their lowest at the moment
                    //2. See if this price is the lowest in the last 24 hours
                    //3. See if the price difference is lower than what the player is expecting to buy at. If yes, buy.

                    //Later see if you are on a downtrend and keep waiting till it reaches its low and then buy

                    if (sig.IsCloseToDayLow && (sig.PrDiffCurrAndHighPerc > player.BuyBelowPerc))
                    {

                        logger.Info(
                          sig.CandleOpenTime +
                          "|" + player.Name + player.Avatar +
                          "|" + sig.Symbol +
                          "|" + sig.DayHighPr.Rnd() +
                          "|" + sig.DayLowPr.Rnd() +
                          "|" + sig.CurrPr.Rnd() +
                          "|" + player.TotalBuyCost.Deci().Rnd() +
                          "|" + player.TotalSoldAmount.Deci().Rnd() +
                          "|-" + sig.PrDiffCurrAndHighPerc.Rnd() + " % " +
                          "| < -" + player.BuyBelowPerc.Deci().Rnd(0) + " % " +
                          "|" + "Buy");


                        player.IsTrading = true;
                        player.Pair = sig.Symbol;
                        player.DayHigh = sig.DayHighPr;
                        player.DayLow = sig.DayLowPr;
                        player.BuyPricePerCoin = sig.CurrPr;
                        player.QuantityBought = player.AvailableAmountForTrading / sig.CurrPr;
                        player.BuyingCommision = player.AvailableAmountForTrading * 0.075M / 100;
                        player.TotalBuyCost = player.AvailableAmountForTrading + player.BuyingCommision;
                        player.CurrentPricePerCoin = sig.CurrPr;
                        player.TotalCurrentValue = player.AvailableAmountForTrading;
                        player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
                        player.BuyTime = DateTime.Now;
                        player.AvailableAmountForTrading = 0;
                        player.CandleOpenTimeAtBuy = sig.CandleOpenTime;
                        player.CandleOpenTimeAtSell = null;
                        player.BuyCandleId = sig.CandleId;
                        player.SellCandleId = 0;
                        player.UpdatedTime = DateTime.Now;
                        player.BuyOrSell = "BUY";
                        player.SellTime = null;
                        player.QuantitySold = 0.0M;
                        player.SoldCommision = 0.0M;
                        player.SoldPricePricePerCoin = 0.0M;
                        sig.IsPicked = true;
                        db.Player.Update(player);
                        PlayerHist playerHistory = iMapr.Map<Player, PlayerHist>(player);
                        playerHistory.Id = 0;
                        await db.PlayerHist.AddAsync(playerHistory);
                        isdbUpdateRequired = true; //flag that db needs to be updated, and update it at the end
                        boughtCoins.Add(sig.Symbol);
                        //isanybuyingdone = true;
                        break;
                    }
                }
            }

            if (isdbUpdateRequired) await db.SaveChangesAsync();
        }

        private async Task Sell(List<Signal> Signals)
        {


            bool isSaleHappen = false;

            if (Signals == null || Signals.Count() == 0)
            {
                //  logger.Info("No signals found. returning from selling");
                return;
            }
            var candleopentime = Signals.FirstOrDefault().CandleOpenTime;
            DB TradeDB = new DB();
            var players = await TradeDB.Player.OrderBy(x => x.Id).ToListAsync();
            foreach (var player in players)
            {
                if (!player.IsTrading) continue; // empty player, nothing to update

                var CoinPair = player.Pair;
                var sig = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();

                //if (sig == null) //for some weird reason if symbol comes null.
                //{
                //    if (CoinPair.Contains("BUSD")) CoinPair = CoinPair.Replace("BUSD", "USDT");
                //    else if (CoinPair.Contains("USDT")) CoinPair = CoinPair.Replace("USDT", "BUSD");
                //    sig = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();
                //}

                if (sig == null) continue;

                //update to set all these values in PlayerHist
                player.TotalBuyCost = player.BuyPricePerCoin * player.QuantityBought + player.BuyingCommision;
                player.SoldCommision = player.CurrentPricePerCoin * player.QuantityBought * 0.075M / 100;
                player.TotalSoldAmount = player.TotalCurrentValue - player.SoldCommision;

                var prDiffPerc = player.TotalSoldAmount.GetDiffPerc(player.TotalBuyCost);


                //decimal SellAbovePerc = 5;
                //decimal SellBelowPerc = -3;
                //decimal DontSellBelowPerc = -12;


                // In real life, keep monitoring to create a stop loss below -3% or create monitoring tools to do the monitoring


                if ((prDiffPerc > player.SellAbovePerc) || (prDiffPerc < player.SellBelowPerc)) //(prDiffPerc > 5) || (prDiffPerc < -3 && prDiffPerc > -25)
                {

                    //this is unusual. I am not ready to sell at a loss of 12$.I would wait  for longer period. THis is to protect sudden super dippers who
                    //can completely wipe all my profit in an hour and then bring back the money back to its position
                    if (prDiffPerc < player.DontSellBelowPerc)
                    {
                        logger.Info(
                        sig.CandleOpenTime +
                        "|" + player.Name + player.Avatar +
                        "|" + sig.Symbol +
                        "|" + sig.DayHighPr.Rnd() +
                        "|" + sig.DayLowPr.Rnd() +
                        "|" + sig.CurrPr.Rnd() +
                        "|" + player.TotalBuyCost.Deci().Rnd() +
                        "|" + player.TotalSoldAmount.Deci().Rnd() +
                        "|" + prDiffPerc.Deci().Rnd() +
                        "|" + " > " + player.DontSellBelowPerc.Deci().Rnd(2) + " % " +
                        "|" + "Not selling");
                        continue; //not selling for this player, but continuing for other players
                    }



                    player.DayHigh = sig.DayHighPr;
                    player.DayLow = sig.DayLowPr;
                    player.CurrentPricePerCoin = sig.CurrPr;
                    player.TotalCurrentValue = player.CurrentPricePerCoin * player.QuantityBought;
                    player.QuantitySold = Convert.ToDecimal(player.QuantityBought);
                    player.AvailableAmountForTrading = player.TotalSoldAmount;
                    player.SellTime = DateTime.Now;
                    player.UpdatedTime = DateTime.Now;
                    player.SoldPricePricePerCoin = sig.CurrPr;
                    player.CandleOpenTimeAtSell = sig.CandleOpenTime;
                    player.BuyOrSell = "SELL";
                    player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
                    player.SellCandleId = sig.CandleId;

                    if (prDiffPerc > player.SellAbovePerc)
                    {
                        logger.Info(
                        sig.CandleOpenTime +
                        "|" + player.Name + player.Avatar +
                        "|" + sig.Symbol +
                        "|" + sig.DayHighPr.Rnd() +
                        "|" + sig.DayLowPr.Rnd() +
                        "|" + sig.CurrPr.Rnd() +
                        "|" + player.TotalBuyCost.Deci().Rnd() +
                        "|" + player.TotalSoldAmount.Deci().Rnd() +
                        "|" + prDiffPerc.Deci().Rnd(2) +
                        "|" + " > " + player.SellAbovePerc.Deci().Rnd(2) + " % " +
                        "|" + "Selling at profit");
                    }
                    else if (prDiffPerc < player.SellBelowPerc)
                    {
                        logger.Info(
                                        sig.CandleOpenTime +
                                        "|" + player.Name + player.Avatar +
                                        "|" + sig.Symbol +
                                        "|" + sig.DayHighPr.Rnd() +
                                        "|" + sig.DayLowPr.Rnd() +
                                        "|" + sig.CurrPr.Rnd() +
                                        "|" + player.TotalBuyCost.Deci().Rnd() +
                                        "|" + player.TotalSoldAmount.Deci().Rnd() +
                                        "|" + prDiffPerc.Deci().Rnd() +
                                        "|" + " < " + player.SellBelowPerc.Deci().Rnd(2) + " % " +
                                        "|" + "Selling at loss");
                    }



                    // create sell order (in live system)
                    PlayerHist PlayerHist = iMapr.Map<Player, PlayerHist>(player);
                    PlayerHist.Id = 0;
                    await TradeDB.PlayerHist.AddAsync(PlayerHist);

                    // reset records to buy again
                    player.DayHigh = 0.0M;
                    player.DayLow = 0.0M;
                    player.Pair = null;
                    player.BuyPricePerCoin = 0.0M;
                    player.CurrentPricePerCoin = 0.0M;
                    player.QuantityBought = 0.0M;
                    player.TotalBuyCost = 0.0M;
                    player.TotalCurrentValue = 0.0M;
                    player.TotalSoldAmount = 0.0M;
                    player.BuyTime = null;
                    player.SellTime = null;
                    player.BuyingCommision = 0.0M;
                    player.SoldPricePricePerCoin = 0.0M;
                    player.QuantitySold = 0.0M;
                    player.SoldCommision = 0.0M;
                    player.IsTrading = false;
                    player.CandleOpenTimeAtBuy = null;
                    player.CandleOpenTimeAtSell = null;
                    player.BuyOrSell = string.Empty;
                    TradeDB.Player.Update(player);
                    isSaleHappen = true;
                }

            }

            if (isSaleHappen)
            {
                var availplayers = await TradeDB.Player.Where(x => x.IsTrading == false).ToListAsync();
                var avgAvailAmountForTrading = availplayers.Average(x => x.AvailableAmountForTrading);

                foreach (var player in availplayers)
                {
                    player.AvailableAmountForTrading = avgAvailAmountForTrading;
                    TradeDB.Player.Update(player);
                }

                //await TradeDB.SaveChangesAsync();
            }

            await TradeDB.SaveChangesAsync();

        }

        private void ManageRisk()
        {

        }

        int updateexpectedProfit = 0;

        private async Task Trade()
        {

            #region testRun

            await ClearData();



            //  DateTime latestCandleDate = db.Candle.AsNoTracking().Max(x => x.OpenTime); // for prod
            DateTime startTime = new DateTime(2021, 3, 1, 23, 0, 0);
            DateTime endTime = new DateTime(2021, 6, 16, 23, 0, 0);

            //  List<string> bots = new List<string>() { "Diana", "Damien", "Shatlin", "Pepper", "Eevee" };

            int i = 0;

            using (var db = new DB())
            {

                while (startTime < endTime)
                {
                    var allbots = await db.Player.ToListAsync();
                    List<Signal> signals = new List<Signal>();
                    myCoins = await db.MyCoins.AsNoTracking().ToListAsync();

                    try
                    {
                        if (i % 24 == 0) // do it only once a day
                        {
                            foreach (var b in allbots)
                            {
                                var starteddate = Convert.ToDateTime(b.CreatedDate);
                                var totaldayssinceStarted = (startTime - starteddate).Days;
                                var expectedProfit = Convert.ToDecimal(b.OriginalAllocatedValue);

                                for (int j = 0; j < totaldayssinceStarted; j++)
                                {
                                    expectedProfit = (expectedProfit + (expectedProfit * 0.6M / 100)); //expecting 0.4% profit daily
                                }
                                b.TotalExpectedProfit = expectedProfit;
                            }
                            await db.SaveChangesAsync();
                        }

                        signals = await GetSignals15(startTime);

                        await Buy(signals);
                        await Sell(signals);

                        startTime = startTime.AddMinutes(15);

                        i++;

                    }
                    catch (Exception ex)
                    {

                        logger.Error("Exception in trade " + ex.Message);
                    }

                }

            }


            logger.Info("---------------Test Run for Trading Completed --------------");

            #endregion testRun

            //#region ProdRun


            //DB db = new DB();
            //var allplayers = await db.Player.ToListAsync();
            //List<Signal> signals = new List<Signal>();
            //myCoins = await db.MyCoins.AsNoTracking().ToListAsync();

            //#region GetCandles

            //try
            //{
            //    var candles = await GetCandles15();
            //}
            //catch (Exception ex)
            //{
            //    logger.Error("Exception in getting Candles.Returning.. " + ex.Message);
            //    return;
            //}

            //#endregion  GetCandles

            //#region GetSignals

            //var latestCandleTime = db.Candle.Max(x => x.OpenTime);
            //try
            //{
            //    signals = await GetSignals15(latestCandleTime);
            //}
            //catch (Exception ex)
            //{
            //    logger.Error("Exception in signal Generator.Returning.. " + ex.Message);
            //    return;
            //}

            //#endregion  GetSignals

            //#region Buys

            //try
            //{
            //    await Buy(signals);
            //}
            //catch (Exception ex)
            //{
            //    logger.Error("Exception in Buy  " + ex.Message);
            //}

            //#endregion  Buys

            //#region Sells

            //try
            //{
            //    await Sell(signals);
            //}
            //catch (Exception ex)
            //{
            //    logger.Error("Exception in sell  " + ex.Message);
            //}

            //#endregion  Sells

            //#region Updating Profit Expectation stats once every 24 runs

            //if (updateexpectedProfit % 24 == 0) // do it only once a day
            //{
            //    foreach (var b in allplayers)
            //    {
            //        var starteddate = Convert.ToDateTime(b.CreatedDate);
            //        var totaldayssinceStarted = (latestCandleTime - starteddate).Days;
            //        var expectedProfit = Convert.ToDecimal(b.OriginalAllocatedValue);

            //        for (int j = 0; j < totaldayssinceStarted; j++)
            //        {
            //            expectedProfit = (expectedProfit + (expectedProfit * 0.6M / 100)); //expecting 0.4% profit daily
            //        }
            //        b.TotalExpectedProfit = expectedProfit;
            //    }
            //    await db.SaveChangesAsync();
            //}

            //updateexpectedProfit++;

            //#endregion

            ////  logger.Info("---------------Trading Completed for candle Time--------------" + latestCandleTime);

            //#endregion ProdRun
        }

    }
}

/*
 * 
 * 
 * 
 * 
 * private Player UpdateplayerToBuy(Player player, Signal signal)
    {
        player.IsActivelyTrading = true;
        player.Pair = signal.Symbol;
        player.DayHigh = signal.DayHighPr;
        player.DayLow = signal.DayLowPr;
        player.BuyPricePerCoin = signal.CurrPr;
        player.QuantityBought = player.AvailableAmountForTrading / signal.CurrPr;
        player.BuyingCommision = player.AvailableAmountForTrading * 0.075M / 100;
        player.TotalBuyCost = player.AvailableAmountForTrading + player.BuyingCommision;
        player.CurrentPricePerCoin = signal.CurrPr;
        player.TotalCurrentValue = player.AvailableAmountForTrading;
        player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
        player.BuyTime = DateTime.Now;
        player.AvailableAmountForTrading = 0;
        player.CandleOpenTimeAtBuy = signal.CandleOpenTime;
        player.CandleOpenTimeAtSell = null;
        player.UpdatedTime = DateTime.Now;
        player.BuyOrSell = "BUY";
        player.SellTime = null;
        player.QuantitySold = 0.0M;
        player.SoldCommision = 0.0M;
        player.SoldPricePricePerCoin = 0.0M;

        return player;
    }
 //old code of buy

 if (players[i].Order == 1)
                {
                    foreach (var sig in Signals)
                    {
                        if (sig.IsPicked) continue;
                        if (boughtCoins.Contains(sig.Symbol)) continue;

                        //buying criteria
                        //1. signals are ordered by the coins whose current price are at their lowest at the moment
                        //2. See if this price is the lowest in the last 24 hours
                        //3. See if the price difference is lower than what the player is expecting to buy at. If yes, buy.

                        if (sig.CurrPrDiffSigAndRef < 0 &&
                            Math.Abs(sig.CurrPrDiffSigAndRef) > players[i].BuyWhenValuePercentageIsBelow)
                        {
                            logger.Info(players[i].Name + "-" + players[i].Avatar + "-" + sig.Symbol + "-" +
                            "Curr Pr: " + sig.CurrPr.Rnd() +
                            " Ref Pr: " + sig.RefAvgCurrPr.Rnd() +
                            " price diff % " + sig.CurrPrDiffSigAndRef.Rnd() +
                            " > " + players[i].BuyWhenValuePercentageIsBelow.Deci().Rnd() + " Buying ");

                            players[i] = UpdateplayerToBuy(players[i], sig);
                            sig.IsPicked = true;
                            db.Player.Update(players[i]);

                            PlayerHist playerHistory = iMapr.Map<Player, PlayerHist>(players[i]);
                            playerHistory.Id = 0;
                            await db.PlayerHist.AddAsync(playerHistory);
                            isdbUpdateRequired = true; //flag that db needs to be updated, and update it at the end

                            // while ((i + 2) < players.Count() && players[i + 1].Order != 1) i++; 
                            // once the first player bought, there is no point to loop through 2,3,4,5 trying to buy. They will not buy. ( What if there is a second coin whose price is 6% lower than the current buy? so dont skip. Let them try
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

                        //look for the last players candlebuytime and get that candle.
                        var prevplayerCandle = await db.Candle.Where
                            (x => x.Symbol == sig.Symbol && 
                            x.OpenTime == Convert.ToDateTime(players[i - 1].CandleOpenTimeAtBuy)).FirstOrDefaultAsync();

                        if (prevplayerCandle == null) continue;

                        var prDiffPerc = sig.CurrPr.GetDiffPerc(prevplayerCandle.CurrentPrice);

                        if (prDiffPerc < 0 && Math.Abs(prDiffPerc) > players[i].BuyWhenValuePercentageIsBelow)
                        {
                            logger.Info(players[i].Name + "-" + players[i].Avatar + "-" + sig.Symbol + "-" +
                            "Curr Pr: " + Math.Round(sig.CurrPr, 6) +
                            " Ref Pr: " + Math.Round(prevplayerCandle.CurrentPrice, 6) +
                            " price diff % " + Math.Abs(Math.Round(prDiffPerc, 6)) +
                            " > " + Math.Round(Convert.ToDecimal(players[i].BuyWhenValuePercentageIsBelow), 2) + " Buying ");

                            players[i] = UpdateplayerToBuy(players[i], sig);
                            sig.IsPicked = true;
                            db.Player.Update(players[i]);
                            PlayerHist PlayerHist = iMapr.Map<Player, PlayerHist>(players[i]);
                            PlayerHist.Id = 0;
                            await db.PlayerHist.AddAsync(PlayerHist);
                            await db.SaveChangesAsync();
                            //while ((i + 2) < players.Count() && players[i + 1].Order != 1) i++;
                            boughtCoins.Add(sig.Symbol);
                            break;
                        }
                    }
                }
 */