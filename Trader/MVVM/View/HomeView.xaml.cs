
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BinanceExchange.API;
using BinanceExchange.API.Client;
using BinanceExchange.API.Client.Interfaces;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Market;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Models.Response.Error;
using BinanceExchange.API.Models.WebSocket;
using BinanceExchange.API.Utility;
using BinanceExchange.API.Websockets;
using log4net;
using Newtonsoft.Json;
using Trader.Models;
using WebSocketSharp;
using Microsoft.EntityFrameworkCore;
using System.Linq;

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


        DateTime referenceStartTime = DateTime.Today;

        public HomeView()
        {
            InitializeComponent();
            referenceStartTime = referenceStartTime.Add(new TimeSpan(6, 0, 0));
            Startup();
        }

        private async void Startup()
        {

            CandleDailyDataRetrieverTimer = new DispatcherTimer();
            CandleDailyDataRetrieverTimer.Tick += new EventHandler(CandleDailyDataRetrieverTimer_Tick);
            CandleDailyDataRetrieverTimer.Interval = new TimeSpan(24, 0, 0);
            CandleDailyDataRetrieverTimer.Start();

            CandleDataRetrieverTimer = new DispatcherTimer();
            CandleDataRetrieverTimer.Tick += new EventHandler(CandleDataRetrieverTimer_Tick);
            CandleDataRetrieverTimer.Interval = new TimeSpan(0, intervalminutes, 0);
            CandleDataRetrieverTimer.Start();

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
            //await GetCandles();
            //await GetCandlesOnceaDay();
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
                var accinfo = await client.GetAccountInformation();
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
                            bal.AverageBuyingCoinPrice = bal.BoughtPrice / bal.Free;
                            bal.CurrentCoinPrice = bal.CurrentPrice / bal.Free;
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

        private async Task GetCandles()
        {
            logger.Info("Getting Candle Started at " + DateTime.Now);
            DB candledb = new DB();

            var counter = await candledb.Counter.FirstOrDefaultAsync();

            if (counter.IsCandleCurrentlyBeingUpdated)
            {
                return;
            }

            var minutedifference = (DateTime.Now - counter.CandleLastUpdatedTime).TotalMinutes;

            if (minutedifference < (intervalminutes - 5))
            {

                logger.Info(" Candle retrieved only " + minutedifference + " minutes back. Dont need to get again");
                return;
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
            TraderTimer.Start();
            await Trade();
        }

        private async Task Trade()
        {
            DB TradeDB = new DB();

            List<SignalIndicator> signalIndicators = new List<SignalIndicator>();
            Counter counter = await TradeDB.Counter.AsNoTracking().FirstOrDefaultAsync();
            List<MyTradeFavouredCoins> myTradeFavouredCoins = await TradeDB.MyTradeFavouredCoins.OrderBy(x => x.PreferenceOrder).AsNoTracking().ToListAsync();
            await GetCandles();
            List<Candle> latestCandles = await TradeDB.Candle.AsNoTracking().Where(x => x.DataSet == (counter.CandleCurrentSet - 1)).ToListAsync();
            List<SymbolPriceResponse> prices = await client.GetAllPrices();
            List<Balance> currentBalance = await UpdateBalance(prices);

            foreach (var myfavcoin in myTradeFavouredCoins)
            {
                var myfavcoinCandle = latestCandles.Where(x => x.Symbol == myfavcoin.Pair).FirstOrDefault();
                if (myfavcoinCandle == null)
                {
                    continue;
                }

                #region signalIndicators
               
                SignalIndicator signalIndicator = new SignalIndicator();
                signalIndicator.Symbol = myfavcoinCandle.Symbol;
                signalIndicator.CurrentPrice = myfavcoinCandle.CurrentPrice;
                signalIndicator.DayHighPrice = myfavcoinCandle.DayHighPrice;
                signalIndicator.DayLowPrice = myfavcoinCandle.DayLowPrice;
                signalIndicator.DayVolume = myfavcoinCandle.DayVolume;
                signalIndicator.DayTradeCount = myfavcoinCandle.DayTradeCount;

                signalIndicator.DayPriceDifferencePercentage =
                    ((myfavcoinCandle.DayHighPrice - myfavcoinCandle.DayLowPrice) /
                    ((myfavcoinCandle.DayHighPrice + myfavcoinCandle.DayLowPrice) / 2)) * 100;

                signalIndicator.PriceDifferenceCurrentAndHighPercentage = Math.Abs(
                    (myfavcoinCandle.DayHighPrice - myfavcoinCandle.CurrentPrice) / ((myfavcoinCandle.DayHighPrice + myfavcoinCandle.CurrentPrice) / 2) * 100);

                signalIndicator.PriceDifferenceCurrentAndLowPercentage = Math.Abs(
                    ((myfavcoinCandle.DayLowPrice - myfavcoinCandle.CurrentPrice) / ((myfavcoinCandle.DayLowPrice + myfavcoinCandle.CurrentPrice) / 2)) * 100);

                var dayAveragePrice = (signalIndicator.DayHighPrice + signalIndicator.DayLowPrice) / 2;

                if (signalIndicator.CurrentPrice < dayAveragePrice)
                {
                    signalIndicator.IsCloseToDayLow = true;
                }
                else
                {
                    signalIndicator.IsCloseToDayHigh = true;
                }

                signalIndicators.Add(signalIndicator);

                #endregion

                // Create Trade mini bots that each group will handle 1/5th of the investment.
                // Each mini bot will have 5 avatars that will wait for the right time to buy or sell.
                // 1st bot will start the buying and waiting to make a profit. If Market is going down, based on the rules set second will jump to buy at a lower price
                //
            }

            string test = string.Empty;
        }


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

    }
}
