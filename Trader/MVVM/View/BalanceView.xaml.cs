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
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Trader.Models;
namespace Trader.MVVM.View
{
    /// <summary>
    /// Interaction logic for BalanceView.xaml
    /// </summary>
    public partial class BalanceView : UserControl
    {


     
      

        public BalanceView()
        {
            InitializeComponent();

           
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


        //private async Task<List<Candle>> GetCandles()
        //{
        //    DB candledb = new DB();
        //    logger.Info("Getting Candle Started at " + DateTime.Now);
        //    var counter = await candledb.Counter.FirstOrDefaultAsync();
        //    List<Candle> candles = new List<Candle>();
        //    try
        //    {
        //        if (counter.IsCandleBeingUpdated)
        //        {
        //            return candles;
        //        }

        //        //var minutedifference = (DateTime.Now - counter.CandleLastUpdatedTime).TotalMinutes;

        //        //if (minutedifference < (intervalminutes - 5))
        //        //{
        //        //    logger.Info(" Candle retrieved only " + minutedifference + " minutes back. Dont need to get again");
        //        //    return candles;
        //        //}

        //        counter.IsCandleBeingUpdated = true;
        //        candledb.Update(counter);
        //        await candledb.SaveChangesAsync();

        //        var StartlastCandleHour = candledb.Candle.Max(x => x.OpenTime);

        //        MyTradeFavouredCoins = await candledb.MyCoins.ToListAsync();

        //        var prices = await client.GetAllPrices();
        //        // await UpdateBalance(prices);



        //        #region get all missing candles

        //        var totalhours = (DateTime.Now - StartlastCandleHour).TotalHours;

        //        if (totalhours > 1)
        //        {
        //            foreach (var coin in MyTradeFavouredCoins)
        //            {
        //                var pricesofcoin = prices.Where(x => x.Symbol.Contains(coin.Coin));
        //                if (pricesofcoin == null || pricesofcoin.Count() == 0)
        //                {
        //                    continue;
        //                }
        //                foreach (var price in pricesofcoin)
        //                {
        //                    if (price.Symbol != coin.Coin + "BUSD" && price.Symbol != coin.Coin + "USDC" && price.Symbol != coin.Coin + "USDT") // if the price symbol doesnt contain usdt and busd ignore those coins
        //                    {
        //                        continue;
        //                    }
        //                    //var pricechangeresponse = await client.GetDailyTicker(price.Symbol);
        //                    GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
        //                    cr.Limit = 40;
        //                    cr.Symbol = price.Symbol;
        //                    cr.Interval = KlineInterval.OneHour;
        //                    cr.StartTime = Convert.ToDateTime(StartlastCandleHour).AddHours(1);
        //                    cr.EndTime = DateTime.Now.AddHours(-1);
        //                    var candleresponse = await client.GetKlinesCandlesticks(cr);

        //                    foreach (var candleResp in candleresponse)
        //                    {
        //                        Candle addCandle = new Candle();

        //                        addCandle.Symbol = cr.Symbol;
        //                        addCandle.Open = candleResp.Open;
        //                        addCandle.RecordedTime = DateTime.Now;
        //                        addCandle.OpenTime = candleResp.OpenTime.AddHours(hourDifference);
        //                        addCandle.High = candleResp.High;
        //                        addCandle.Low = candleResp.Low;
        //                        addCandle.Close = candleResp.Close;
        //                        addCandle.Volume = candleResp.Volume;
        //                        addCandle.CloseTime = candleResp.CloseTime.AddHours(hourDifference);
        //                        addCandle.QuoteAssetVolume = candleResp.QuoteAssetVolume;
        //                        addCandle.NumberOfTrades = candleResp.NumberOfTrades;
        //                        addCandle.TakerBuyBaseAssetVolume = candleResp.TakerBuyBaseAssetVolume;
        //                        addCandle.TakerBuyQuoteAssetVolume = candleResp.TakerBuyQuoteAssetVolume;
        //                        addCandle.Change = 0;
        //                        addCandle.PriceChangePercent = 0;
        //                        addCandle.WeightedAveragePercent = 0;
        //                        addCandle.PreviousClosePrice = 0;
        //                        addCandle.CurrentPrice = candleResp.Close;
        //                        addCandle.OpenPrice = candleResp.Open;
        //                        addCandle.DayHighPrice = 0;
        //                        addCandle.DayLowPrice = 0;
        //                        addCandle.DayVolume = 0;
        //                        addCandle.DayTradeCount = 0;

        //                        var isCandleExisting = await candledb.Candle.Where(x => x.OpenTime == addCandle.OpenTime && x.Symbol == addCandle.Symbol).FirstOrDefaultAsync();

        //                        if (isCandleExisting == null)
        //                        {
        //                            candles.Add(addCandle);
        //                            await candledb.Candle.AddAsync(addCandle);
        //                            await candledb.SaveChangesAsync();
        //                        }
        //                    }
        //                }
        //            }

        //            var UpdatedlastCandleHour = candledb.Candle.Max(x => x.OpenTime);
        //            List<Candle> selectedCandles;
        //            while (StartlastCandleHour <= UpdatedlastCandleHour)
        //            {
        //                try
        //                {
        //                    foreach (var favtrade in MyTradeFavouredCoins)
        //                    {
        //                        selectedCandles = await candledb.Candle.Where(x => x.Symbol.Contains(favtrade.Coin) && x.OpenTime.Date == StartlastCandleHour.Date).ToListAsync();

        //                        if (selectedCandles == null || selectedCandles.Count == 0) continue;

        //                        foreach (var candle in selectedCandles)
        //                        {
        //                            candle.DayHighPrice = selectedCandles.Max(x => x.High);
        //                            candle.DayLowPrice = selectedCandles.Min(x => x.Low);
        //                            candle.DayVolume = selectedCandles.Sum(x => x.Volume);
        //                            candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
        //                            candledb.Candle.Update(candle);
        //                        }
        //                        await candledb.SaveChangesAsync();
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    logger.Error(" Updating candle error " + ex.Message);
        //                }
        //                StartlastCandleHour = StartlastCandleHour.AddDays(1);
        //            }

        //        }
        //        #endregion get all missing candles

        //        foreach (var coin in MyTradeFavouredCoins)
        //        {
        //            var pricesofcoin = prices.Where(x => x.Symbol.Contains(coin.Coin));
        //            if (pricesofcoin == null || pricesofcoin.Count() == 0)
        //            {
        //                continue;
        //            }
        //            foreach (var price in pricesofcoin)
        //            {
        //                if (price.Symbol != coin.Coin + "BUSD" && price.Symbol != coin.Coin + "USDC" && price.Symbol != coin.Coin + "USDT") // if the price symbol doesnt contain usdt and busd ignore those coins
        //                {
        //                    continue;
        //                }
        //                Candle candle = new Candle();
        //                var pricechangeresponse = await client.GetDailyTicker(price.Symbol);
        //                GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
        //                cr.Limit = 1;
        //                cr.Symbol = price.Symbol;
        //                cr.Interval = KlineInterval.OneHour;

        //                var candleresponse = await client.GetKlinesCandlesticks(cr);
        //                candle.RecordedTime = DateTime.Now;
        //                candle.Symbol = price.Symbol;
        //                candle.Open = candleresponse[0].Open;
        //                candle.OpenTime = candleresponse[0].OpenTime.AddHours(hourDifference);
        //                candle.High = candleresponse[0].High;
        //                candle.Low = candleresponse[0].Low;
        //                candle.Close = candleresponse[0].Close;
        //                candle.Volume = candleresponse[0].Volume;
        //                candle.CloseTime = candleresponse[0].CloseTime.AddHours(hourDifference);
        //                candle.QuoteAssetVolume = candleresponse[0].QuoteAssetVolume;
        //                candle.NumberOfTrades = candleresponse[0].NumberOfTrades;
        //                candle.TakerBuyBaseAssetVolume = candleresponse[0].TakerBuyBaseAssetVolume;
        //                candle.TakerBuyQuoteAssetVolume = candleresponse[0].TakerBuyQuoteAssetVolume;
        //                candle.Change = pricechangeresponse.PriceChange;
        //                candle.PriceChangePercent = pricechangeresponse.PriceChangePercent;
        //                candle.WeightedAveragePercent = pricechangeresponse.PriceChangePercent;
        //                candle.PreviousClosePrice = pricechangeresponse.PreviousClosePrice;
        //                candle.CurrentPrice = pricechangeresponse.LastPrice;
        //                candle.OpenPrice = pricechangeresponse.OpenPrice;
        //                candle.DayHighPrice = pricechangeresponse.HighPrice;
        //                candle.DayLowPrice = pricechangeresponse.LowPrice;
        //                candle.DayVolume = pricechangeresponse.Volume;
        //                candle.DayTradeCount = pricechangeresponse.TradeCount;
        //                // candle.DataSet = candlecurrentSet;

        //                var isCandleExisting = await candledb.Candle.Where(x => x.OpenTime == candle.OpenTime && x.Symbol == candle.Symbol).FirstOrDefaultAsync();

        //                if (isCandleExisting == null)
        //                {
        //                    candles.Add(candle);
        //                    await candledb.Candle.AddAsync(candle);
        //                    await candledb.SaveChangesAsync();
        //                }
        //                else
        //                {

        //                    candledb.Candle.Update(isCandleExisting);
        //                    await candledb.SaveChangesAsync();
        //                }
        //            }
        //        }

        //        counter.IsCandleBeingUpdated = false;

        //        candledb.Counter.Update(counter);
        //        await candledb.SaveChangesAsync();
        //        logger.Info("Getting Candle Completed at " + DateTime.Now);
        //    }
        //    catch (Exception ex)
        //    {

        //        logger.Info("Exception in Getting Candle  " + ex.Message);
        //        counter.IsCandleBeingUpdated = false;
        //        candledb.Counter.Update(counter);
        //        await candledb.SaveChangesAsync();

        //    }
        //    return candles;
        //}


        //private async void btnCollectData_Click(object sender, RoutedEventArgs e)
        //{
        //    logger.Info("Collect Data Started at " + DateTime.Now);

        //    var files = Directory.EnumerateFiles(@"C:\Shatlin\klines", "*.csv");
        //    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

        //    int i = 1;
        //    foreach (string file in files)
        //    {
        //        using (var candledb = new DB())
        //        {

        //            i++;
        //            string filename = file.Split('-')[0];
        //            filename = filename.Substring(filename.LastIndexOf("\\") + 1);
        //            using (var reader = new StreamReader(file))
        //            {
        //                while (!reader.EndOfStream)
        //                {
        //                    string line = reader.ReadLine();
        //                    string[] values = line.Split(",");
        //                    Candle candle = new Candle();
        //                    candle.Symbol = filename;
        //                    double d = Convert.ToDouble(values[0].ToString());
        //                    candle.RecordedTime = DateTime.Now;
        //                    candle.OpenTime = Convert.ToDateTime(epoch.AddMilliseconds(Convert.ToDouble(values[0])));
        //                    candle.Open = Convert.ToDecimal(values[1]);
        //                    candle.High = Convert.ToDecimal(values[2]);
        //                    candle.Low = Convert.ToDecimal(values[3]);
        //                    candle.Close = Convert.ToDecimal(values[4]);
        //                    candle.Volume = Convert.ToDecimal(values[5]);
        //                    candle.CloseTime = Convert.ToDateTime(epoch.AddMilliseconds(Convert.ToDouble(values[6])));
        //                    candle.QuoteAssetVolume = Convert.ToDecimal(values[7]);
        //                    candle.NumberOfTrades = Convert.ToInt32(values[8]);
        //                    candle.TakerBuyBaseAssetVolume = Convert.ToDecimal(values[9]);
        //                    candle.TakerBuyQuoteAssetVolume = Convert.ToDecimal(values[10]);
        //                    candle.Change = 0.0M;
        //                    candle.PriceChangePercent = 0.0M;
        //                    candle.WeightedAveragePercent = 0.0M;
        //                    candle.PreviousClosePrice = 0.0M;
        //                    candle.CurrentPrice = candle.Close;
        //                    candle.OpenPrice = candle.Open;
        //                    candle.DayHighPrice = 0.0M;
        //                    candle.DayLowPrice = 0.0M;
        //                    candle.DayVolume = 0.0M;
        //                    candle.DayTradeCount = 0;


        //                    await candledb.AddAsync(candle);

        //                }

        //                logger.Info(i + " : " + file + " Processing Completed ");


        //            }
        //            await candledb.SaveChangesAsync();
        //        }
        //    }

        //    logger.Info("----------All file Processing Completed-------------- ");
        //    await UpdateData();
        //}

        //private async Task UpdateData()
        //{
        //    DateTime currentdate = new DateTime(2021, 3, 1, 0, 0, 0);
        //    DateTime lastdate = new DateTime(2021, 6, 15, 23, 0, 0);


        //    while (currentdate <= lastdate)
        //    {
        //        using (var TradeDB = new DB())
        //        {

        //            List<Candle> selectedCandles;
        //            List<MyCoins> myTradeFavouredCoins = await TradeDB.MyCoins.AsNoTracking().ToListAsync();

        //            try
        //            {
        //                foreach (var favtrade in myTradeFavouredCoins)
        //                {

        //                    selectedCandles = await TradeDB.Candle.Where(x => x.Symbol.Contains(favtrade.Coin) && x.OpenTime.Date == currentdate.Date
        //                    ).ToListAsync();

        //                    if (selectedCandles == null || selectedCandles.Count == 0) continue;

        //                    foreach (var candle in selectedCandles)
        //                    {
        //                        candle.DayHighPrice = selectedCandles.Max(x => x.High);
        //                        candle.DayLowPrice = selectedCandles.Min(x => x.Low);
        //                        candle.DayVolume = selectedCandles.Sum(x => x.Volume);
        //                        candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
        //                        TradeDB.Candle.Update(candle);
        //                    }
        //                    await TradeDB.SaveChangesAsync();
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                logger.Error(" Updating candle error " + ex.Message);
        //            }

        //        }

        //        currentdate = currentdate.AddDays(1);
        //    }

        //    logger.Info(" Updating data Completed ");
        //}

        //private async void btnTrade_Click(object sender, RoutedEventArgs e)
        //{

        //    await Trade();
        //}


        //private async void CandleDataRetrieverTimer_Tick(object sender, EventArgs e)
        //{
        //    await GetCandles();
        //}

        //private async void CandleDailyDataRetrieverTimer_Tick(object sender, EventArgs e)
        //{
        //    await GetCandlesOnceaDay();
        //}







        //private async Task GetMyTrades()
        //{
        //    DB TradeDB = new DB();
        //    foreach (var coin in MyTradedCoinList)
        //    {
        //        try
        //        {
        //            List<AccountTradeReponse> accountTrades = await client.GetAccountTrades(new AllTradesRequest()
        //            {
        //                Limit = 50,
        //                Symbol = coin
        //            });

        //            foreach (var trade in accountTrades)
        //            {
        //                var mytrade = new MyTrade
        //                {
        //                    Price = trade.Price,
        //                    Pair = coin,
        //                    Quantity = trade.Quantity,
        //                    Commission = trade.Commission,
        //                    CommissionAsset = trade.CommissionAsset,
        //                    Time = trade.Time,
        //                    IsBuyer = trade.IsBuyer,
        //                    IsMaker = trade.IsMaker,
        //                    IsBestMatch = trade.IsBestMatch,
        //                    OrderId = trade.OrderId,
        //                    Amount = trade.Quantity * trade.Price + (trade.Quantity * trade.Price) * 0.075M / 100
        //                };
        //                await TradeDB.MyTrade.AddAsync(mytrade);
        //            }
        //        }
        //        catch (Exception ex)
        //        {

        //            logger.Error("Error while retrieving price ticker for " + coin + " " + ex.Message);
        //        }
        //    }
        //    await TradeDB.SaveChangesAsync();
        //}




















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

        //private async Task GetCandlesOnceaDay()
        //{
        //    logger.Info("Getting Daily Candle Started at " + DateTime.Now);
        //    DB candledb = new DB();
        //    var prices = await client.GetAllPrices();
        //    await UpdateBalance(prices);


        //    foreach (var price in prices)
        //    {

        //        if (
        //             price.Symbol.Contains("UPUSDT") || price.Symbol.Contains("DOWNUSDT") ||
        //             price.Symbol.Contains("UPBUSD") || price.Symbol.Contains("DOWNBUSD") ||
        //              price.Symbol.Contains("UPUSDC") || price.Symbol.Contains("DOWNUSDC") ||
        //             price.Symbol.Contains("BEARBUSD") || price.Symbol.Contains("BULLBUSD") ||
        //             price.Symbol.Contains("BEARUSDT") || price.Symbol.Contains("BULLUSDT") ||
        //             price.Symbol.Contains("BEARUSDC") || price.Symbol.Contains("BULLUSDC")
        //            )
        //        {
        //            continue;
        //        }

        //        if (!price.Symbol.Contains("BUSD") && !price.Symbol.Contains("USDT") && !price.Symbol.Contains("USDC")) // if the price symbol doesnt contain usdt and busd ignore those coins

        //        {
        //            continue;
        //        }

        //        DailyCandle dailycandle = new DailyCandle();
        //        var pricechangeresponse = await client.GetDailyTicker(price.Symbol);
        //        GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
        //        cr.Limit = 1;
        //        cr.Symbol = price.Symbol;
        //        cr.Interval = KlineInterval.OneDay;
        //        var candleresponse = await client.GetKlinesCandlesticks(cr);
        //        dailycandle.RecordedTime = DateTime.Now;
        //        dailycandle.Symbol = price.Symbol;
        //        dailycandle.Open = candleresponse[0].Open;
        //        dailycandle.OpenTime = candleresponse[0].OpenTime.AddHours(hourDifference);
        //        dailycandle.High = candleresponse[0].High;
        //        dailycandle.Low = candleresponse[0].Low;
        //        dailycandle.Close = candleresponse[0].Close;
        //        dailycandle.Volume = candleresponse[0].Volume;
        //        dailycandle.CloseTime = candleresponse[0].CloseTime.AddHours(hourDifference);
        //        dailycandle.QuoteAssetVolume = candleresponse[0].QuoteAssetVolume;
        //        dailycandle.NumberOfTrades = candleresponse[0].NumberOfTrades;
        //        dailycandle.TakerBuyBaseAssetVolume = candleresponse[0].TakerBuyBaseAssetVolume;
        //        dailycandle.TakerBuyQuoteAssetVolume = candleresponse[0].TakerBuyQuoteAssetVolume;
        //        dailycandle.Change = pricechangeresponse.PriceChange;
        //        dailycandle.PriceChangePercent = pricechangeresponse.PriceChangePercent;
        //        dailycandle.WeightedAveragePercent = pricechangeresponse.PriceChangePercent;
        //        dailycandle.PreviousClosePrice = pricechangeresponse.PreviousClosePrice;
        //        dailycandle.CurrentPrice = pricechangeresponse.LastPrice;
        //        dailycandle.OpenPrice = pricechangeresponse.OpenPrice;
        //        dailycandle.DayHighPrice = pricechangeresponse.HighPrice;
        //        dailycandle.DayLowPrice = pricechangeresponse.LowPrice;
        //        dailycandle.DayVolume = pricechangeresponse.Volume;
        //        dailycandle.DayTradeCount = pricechangeresponse.TradeCount;

        //        var isCandleExisting = await candledb.DailyCandle.Where(x => x.OpenTime == dailycandle.OpenTime && x.Symbol == dailycandle.Symbol).FirstOrDefaultAsync();

        //        if (isCandleExisting == null)
        //        {
        //            await candledb.DailyCandle.AddAsync(dailycandle);
        //            await candledb.SaveChangesAsync();
        //        }


        //    }


        //    var counters = await candledb.Counter.FirstOrDefaultAsync();

        //    candledb.Counter.Update(counters);
        //    await candledb.SaveChangesAsync();
        //    logger.Info("Getting Daily Candle Completed at " + DateTime.Now);
        //}

        //private async Task<List<Signal>> GetSignals(DateTime cndlHr)
        //{

        //    DB TradeDB = new DB();

        //    List<Signal> signals = new List<Signal>();

        //    List<Candle> latestCndls = await TradeDB.Candle.AsNoTracking().Where(x => x.OpenTime == cndlHr).ToListAsync();

        //    // DateTime refCandlMinTime = currentCandleSetDate.AddHours(-23);

        //    List<Candle> refCndls = await TradeDB.Candle.AsNoTracking()
        //        .Where(x => x.OpenTime >= cndlHr.AddHours(-23) && x.OpenTime < cndlHr).ToListAsync();

        //    foreach (var myfavcoin in myCoins)
        //    {

        //        try
        //        {
        //            #region Prefer BUSD if not available go for USDT

        //            List<string> usdStrings = new List<string>()
        //                { myfavcoin.Coin + "USDT", myfavcoin.Coin + "BUSD", myfavcoin.Coin + "USDC" };

        //            var usdCndlList = latestCndls.Where(x => usdStrings.Contains(x.Symbol));

        //            if (usdCndlList == null) continue;

        //            var busdcandle = usdCndlList.Where(x => x.Symbol == myfavcoin.Coin + "BUSD").FirstOrDefault();

        //            Signal sig = new Signal();

        //            if (busdcandle != null) sig.Symbol = busdcandle.Symbol;
        //            else sig.Symbol = myfavcoin.Coin + "USDT";

        //            var selCndl = usdCndlList.Where(x => x.Symbol == sig.Symbol).FirstOrDefault();

        //            if (selCndl == null) continue;

        //            var usdRefCndls = refCndls.Where(x => usdStrings.Contains(x.Symbol));

        //            if (usdRefCndls == null || usdRefCndls.Count() == 0) continue;

        //            #endregion

        //            sig.CurrPr = selCndl.CurrentPrice;
        //            sig.DayHighPr = selCndl.DayHighPrice;
        //            sig.DayLowPr = selCndl.DayLowPrice;
        //            sig.CandleOpenTime = selCndl.OpenTime;
        //            sig.CandleId = selCndl.Id;
        //            sig.DayVol = usdCndlList.Sum(x => x.DayVolume);
        //            sig.DayTradeCount = usdCndlList.Sum(x => x.DayTradeCount);
        //            sig.RefHighPr = usdRefCndls.Max(x => x.CurrentPrice);
        //            sig.RefLowPr = usdRefCndls.Min(x => x.CurrentPrice);
        //            sig.RefAvgCurrPr = usdRefCndls.Average(x => x.CurrentPrice);
        //            sig.RefDayVol = usdRefCndls.Average(x => x.DayVolume);
        //            sig.RefDayTradeCount = (int)usdRefCndls.Average(x => x.DayTradeCount);

        //            sig.DayPrDiffPercentage = sig.DayHighPr.GetDiffPerc(sig.DayLowPr);
        //            sig.PrDiffCurrAndHighPerc = Math.Abs(sig.DayHighPr.GetDiffPerc(sig.CurrPr));
        //            sig.PrDiffCurrAndLowPerc = Math.Abs(sig.DayLowPr.GetDiffPerc(sig.CurrPr));
        //            // this will always be positive. You need to first target those coins which are 
        //            //closest to low price. Dont worry about trade count for now

        //            sig.CurrPrDiffSigAndRef = sig.CurrPr.GetDiffPerc(sig.RefAvgCurrPr);
        //            //Difference between current price and the average current prices of last 24 hours

        //            var dayAveragePrice = (sig.DayHighPr + sig.DayLowPr) / 2;

        //            if (sig.CurrPr < dayAveragePrice) sig.IsCloseToDayLow = true;

        //            else sig.IsCloseToDayHigh = true;

        //            signals.Add(sig);
        //        }
        //        catch (Exception ex)
        //        {
        //            logger.Error("Error in signal Generator " + ex.Message);
        //        }
        //    }

        //    return signals.OrderBy(x => x.PrDiffCurrAndLowPerc).ToList();
        //}

        //#region QA

        //private async Task BuyTheCoinQA(PlayerQA player, Signal sig)
        //{
        //    DB db = new DB();

        //    var PriceResponse = await client.GetPrice(sig.Symbol);

        //    decimal mybuyPrice = PriceResponse.Price;

        //    player.Pair = sig.Symbol;

        //    var coin = myCoins.Where(x => x.Coin == sig.Symbol).FirstOrDefault();

        //    var coinprecison = coin.TradePrecision;

        //    var quantity = (player.AvailableAmountToBuy.Deci() / mybuyPrice).Rnd(coinprecison);

        //    //var buyOrder = await client.CreateOrder(new CreateOrderRequest()
        //    //{
        //    //    Price = mybuyPrice,
        //    //    Quantity = quantity,
        //    //    Side = OrderSide.Buy,
        //    //    Symbol = player.Pair,
        //    //    Type = OrderType.Limit,
        //    //    TimeInForce = TimeInForce.GTC
        //    //});


        //    player.IsTrading = true;
        //    player.DayHigh = sig.DayHighPr;
        //    player.DayLow = sig.DayLowPr;
        //    player.BuyCoinPrice = mybuyPrice;
        //    player.Quantity = quantity;
        //    player.BuyCommision = player.AvailableAmountToBuy * configr.CommisionAmount / 100;
        //    player.TotalBuyCost = player.AvailableAmountToBuy + player.BuyCommision;
        //    player.CurrentCoinPrice = mybuyPrice;
        //    player.TotalCurrentValue = player.AvailableAmountToBuy; //exclude commision in the current value.
        //    player.BuyTime = DateTime.Now;
        //    player.BuyOrderId = 10; //[TODO] hardcoded for QA
        //    player.SellOrderId = 0;
        //    player.UpdatedTime = DateTime.Now;
        //    player.BuyOrSell = "Buy";
        //    player.SellTime = null;
        //    player.SellCommision = player.BuyCommision;
        //    player.SellCoinPrice = mybuyPrice;
        //    player.ProfitLossAmt = (player.TotalCurrentValue - player.TotalBuyCost).Deci();
        //    player.TotalSellAmount = player.TotalBuyCost; // resetting available amount for trading
        //    player.AvailableAmountToBuy = 0; // bought, so no amount available to buy
        //    player.isBuyOrderCompleted = false;
        //    player.RepsTillCancelOrder = 0;
        //    player.SellAbovePerc = 0.6M;
        //    player.SellBelowPerc = player.SellAbovePerc;
        //    db.PlayerQA.Update(player);

        //    //Send Buy Order

        //    PlayerTradesQA playerHistory = iPlayerQAMapper.Map<PlayerQA, PlayerTradesQA>(player);
        //    playerHistory.Id = 0;
        //    await db.PlayerTradesQA.AddAsync(playerHistory);
        //    await db.SaveChangesAsync();
        //}

        //private async Task BuyQA()
        //{
        //    if (MySignals == null || MySignals.Count() == 0)
        //    {
        //        //   logger.Info("  " + StrTradeTime + " no signals found. So cannot proceed for buy process ");
        //        return;
        //    }

        //    DB db = new DB();

        //    var players = await db.PlayerQA.OrderBy(x => x.Id).ToListAsync();
        //    boughtCoins = await db.PlayerQA.Where(x => x.Pair != null).Select(x => x.Pair).ToListAsync();

        //    foreach (var player in players)
        //    {
        //        if (player.IsTrading)
        //        {
        //            await UpdateActivePlayerStatsQA(player);
        //            continue;
        //        }

        //        if (player.isBuyOrderCompleted) // before buying the buyordercompleted should be reset to false, so dont buy if its true
        //        {
        //            //  logger.Info("  " + StrTradeTime + " " + player.Name + " isBuyOrderCompleted is true. Cant use it to buy");
        //            continue;
        //        }

        //        if (player.AvailableAmountToBuy < configr.MinimumAmountToTradeWith)
        //        {
        //            //if (configr.ShowBuyingFlowLogs)
        //            //    logger.Info("  " + StrTradeTime + " " + player.Name + " Avl Amt " + player.AvailableAmountToBuy + " Not enough for buying ");
        //            continue;
        //        }
        //        //if (player.isBuyAllowed == false)
        //        //{
        //        //    //if (configr.ShowBuyingFlowLogs)
        //        //    //    logger.Info("  " + StrTradeTime + " " + player.Name + "  Not  Allowed for buying");
        //        //    continue;
        //        //}
        //        //if (configr.IsBuyingAllowed == false)
        //        //{
        //        //    //if (configr.ShowBuyingFlowLogs)
        //        //    //    logger.Info("  " + StrTradeTime + " " + player.Name + "  overall system not  Allowed for buying");
        //        //    continue;
        //        //}

        //        foreach (Signal sig in MySignals.OrderBy(x => x.PrDiffCurrAndHighPerc).ToList())
        //        {
        //            if (sig.IsIgnored)
        //                continue;

        //            if (sig.IsPicked)
        //                continue;

        //            if (boughtCoins.Contains(sig.Symbol))
        //            {
        //                sig.IsPicked = true;
        //                continue;
        //            }
        //            else
        //            {
        //                sig.IsPicked = false;
        //            }

        //            var bitcoinSignal = MySignals.Where(x => x.Symbol == "BTCUSDT").FirstOrDefault();

        //            if (bitcoinSignal != null)
        //            {
        //                // prices are going down. Dont buy till you see recovery
        //                if (
        //                    bitcoinSignal.TotalConsecutive15MinDowns >= 2 ||
        //                    bitcoinSignal.TotalConsecutive5MinDowns >= 2)
        //                {
        //                    sig.IsIgnored = true;
        //                    continue;
        //                }
        //            }

        //            // prices are going down. Dont buy till you see recovery
        //            if (sig.TotalConsecutive1MinDowns >= 3)
        //            {
        //                sig.IsIgnored = true;
        //                continue;
        //            }
        //            else
        //            {
        //                sig.IsIgnored = false;
        //            }

        //            // Day Trade Count too low
        //            if (sig.DayTradeCount < configr.MinAllowedTradeCount)
        //            {
        //                //logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
        //                //  " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
        //                //  "  Day Trade Count too low. Dont buy " +
        //                //  " Day Trade Count " + sig.DayTradeCount.Rnd(1)
        //                //  );

        //                sig.IsIgnored = true;
        //                continue;
        //            }
        //            else
        //            {
        //                sig.IsIgnored = false;
        //            }

        //            if (sig.IsBestTimeToScalpBuy)
        //            {
        //                try
        //                {
        //                    await BuyTheCoinQA(player, sig);
        //                    sig.IsPicked = true;
        //                    boughtCoins.Add(sig.Symbol);
        //                    break;
        //                }
        //                catch (Exception ex)
        //                {
        //                    logger.Error(" " + player.Name + " " + sig.Symbol + " Exception: " + ex.ToString());
        //                }
        //            }
        //            else
        //            {
        //                sig.IsPicked = false;
        //                sig.IsIgnored = false;
        //            }
        //        }
        //    }
        //}

        //private async Task SellQA(PlayerQA player)
        //{
        //    Signal sig = MySignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();
        //    if (sig == null) return;

        //    DB db = new DB();
        //    decimal mysellPrice = sig.CurrPr;

        //    if (player == null)
        //    {
        //        //logger.Info("Player returned as null. Some issue. Returning from Sell");
        //        return;
        //    }

        //    var pair = player.Pair;

        //    if (pair == null)
        //    {
        //        //logger.Info("Player's Pair to sell returned as null. Some issue. Returning from Sell");
        //        return;
        //    }

        //    var newPlayer = db.PlayerQA.AsNoTracking().Where(x => x.Name == player.Name).FirstOrDefault();
        //    if (newPlayer.IsTrading == false) return;

        //    //[TODO] Update if productionalized

        //    //if (newPlayer.SellOrderId > 0)
        //    //{
        //    //    //logger.Info("Sell order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
        //    //    return;
        //    //}
        //    //if (newPlayer.isBuyOrderCompleted == false)
        //    //{
        //    //    //logger.Info("Buy order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
        //    //    return;
        //    //}

        //    player.DayHigh = sig.DayHighPr;
        //    player.DayLow = sig.DayLowPr;
        //    player.UpdatedTime = DateTime.Now;
        //    player.SellCoinPrice = mysellPrice;

        //    decimal availableQty = GetAvailQtyQA(player, pair);

        //    if (availableQty <= 0)
        //    {
        //        //logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ') + " Available Quantity 0 for " + " Symbol " + player.Pair + " Sell not possible ");
        //        return;
        //    }

        //    if (player.Quantity == null || player.Quantity.Deci() == 0)
        //    {
        //        //logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ')
        //        //  + " player.Quantity  0 for " + " Symbol " + player.Pair + " Sell not possible ");
        //        return;
        //    }

        //    player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
        //    player.TotalSellAmount = mysellPrice * player.Quantity + player.SellCommision;
        //    player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
        //    player.CurrentCoinPrice = mysellPrice;
        //    player.TotalCurrentValue = player.TotalSellAmount;
        //    player.SellOrderId = 0;
        //    var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
        //    player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";

        //    if (player.ProfitLossChanges.Length > 200)
        //    {
        //        player.ProfitLossChanges = player.ProfitLossChanges.GetLast(200);
        //    }

        //    var NextSellbelow = prDiffPerc * configr.ReducePriceDiffPercBy / 100;

        //    // less than sellable percentage. Return
        //    if (prDiffPerc <= player.SellAbovePerc && ForceSell == false)
        //    {

        //        // Reducing Profit Perecetages every  hour if the coin is not able to make a sell due to high profit % set. Do it till you reach 1%

        //        if (DateTime.Now.Minute == configr.ReduceSellAboveAtMinute &&
        //            (DateTime.Now.Second >= configr.ReduceSellAboveFromSecond && DateTime.Now.Second <= configr.ReduceSellAboveToSecond))
        //        {

        //            if (player.SellAbovePerc >= configr.MinSellAbovePerc)
        //            {
        //                if (configr.IsReducingSellAbvAllowed)
        //                {
        //                    player.SellAbovePerc = player.SellAbovePerc - configr.ReduceSellAboveBy;

        //                    //logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
        //                    //    " " + player.Name +
        //                    //   " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
        //                    //   " Reduced player's SellAbovePerc to " + player.SellAbovePerc);
        //                }
        //            }
        //        }

        //        player.SellBelowPerc = player.SellAbovePerc;
        //        db.PlayerQA.Update(player);
        //        await db.SaveChangesAsync();
        //        return;
        //    }

        //    if (prDiffPerc >= player.SellBelowPerc && ForceSell == false)
        //    {
        //        if (prDiffPerc > player.LastRoundProfitPerc && NextSellbelow > player.SellBelowPerc)
        //        {
        //            player.SellBelowPerc = NextSellbelow;
        //        }
        //        player.LastRoundProfitPerc = prDiffPerc;
        //        player.AvailableAmountToBuy = 0;
        //        db.PlayerQA.Update(player);
        //        await db.SaveChangesAsync();

        //        if (sig != null)
        //        {
        //            //logger.Info("  " +
        //            //        sig.OpenTime.ToString("dd-MMM HH:mm") +
        //            //      " " + player.Name +
        //            //     " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
        //            //      " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
        //            //      " > NextSellbelow  " + NextSellbelow.Deci().Rnd().ToString().PadRight(11, ' ') +
        //            //      " Not selling ");
        //        }
        //        return;
        //    }

        //    if (sig != null)
        //    {
        //        //logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
        //        //         " " + player.Name +
        //        //         " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
        //        //         " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
        //        //         "  LastRoundProfitPerc  " + player.LastRoundProfitPerc.Deci().Rnd().ToString().PadRight(11, ' ')
        //        //        );
        //    }
        //    if ((newPlayer.isSellAllowed == false || configr.IsSellingAllowed == false) && ForceSell == false)
        //    {
        //        //logger.Info("  " + sig.OpenTime.ToString("dd-MMM HH:mm") +
        //        //          " " + player.Name +
        //        //          " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
        //        //          " Selling not allowed "
        //        //       );
        //        player.AvailableAmountToBuy = 0;
        //        player.LastRoundProfitPerc = prDiffPerc;
        //        db.PlayerQA.Update(player);
        //        await db.SaveChangesAsync();
        //        return;
        //    }
        //    //  if ((prDiffPerc < player.LastRoundProfitPerc && sig.IsBestTimeToSellAtDayHighest) || ForceSell == true)
        //    if (((prDiffPerc < player.LastRoundProfitPerc)) || ForceSell == true) // Scalp: (prDiffPerc < player.LastRoundProfitPerc ) || ForceSell == true)
        //    {
        //        if (sig != null)
        //        {
        //            //logger.Info("  " +
        //            //       sig.OpenTime.ToString("dd-MMM HH:mm") +
        //            //     " " + player.Name +
        //            //    " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
        //            //     " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
        //            //     " < LastRoundProfitPerc  " + player.LastRoundProfitPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
        //            //     " selling ");
        //        }

        //        ForceSell = false;

        //        var PriceChangeResponse = await client.GetDailyTicker(pair);

        //        //logger.Info("  " +
        //        //            sig.OpenTime.ToString("dd-MMM HH:mm") +
        //        //          " " + player.Name +
        //        //         " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
        //        //          " Contacting selling price ticker ");

        //        mysellPrice = PriceChangeResponse.LastPrice;

        //        player.DayHigh = PriceChangeResponse.HighPrice;
        //        player.DayLow = PriceChangeResponse.LowPrice;
        //        player.UpdatedTime = DateTime.Now;
        //        player.SellCoinPrice = mysellPrice;
        //        player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
        //        player.TotalSellAmount = mysellPrice * player.Quantity + player.SellCommision;
        //        player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
        //        player.CurrentCoinPrice = mysellPrice;
        //        player.TotalCurrentValue = player.TotalSellAmount;

        //        var coinprecison = myCoins.Where(x => x.Coin == pair).FirstOrDefault().TradePrecision;

        //        //var sellOrder = await client.CreateOrder(new CreateOrderRequest()
        //        //{
        //        //    Price = mysellPrice,
        //        //    Quantity = player.Quantity.Deci().Rnd(coinprecison),
        //        //    Side = OrderSide.Sell,
        //        //    Symbol = player.Pair,
        //        //    Type = OrderType.Limit,
        //        //    TimeInForce = TimeInForce.GTC
        //        //});

        //        player.SellOrderId = 10; //[TODO] sellOrder.OrderId;
        //        player.SellTime = DateTime.Now;
        //        player.AvailableAmountToBuy = player.TotalSellAmount;
        //        db.PlayerQA.Update(player);
        //        await db.SaveChangesAsync();

        //        await UpdatePlayerAfterSellConfirmedQA(player);
        //    }
        //    else
        //    {
        //        player.AvailableAmountToBuy = 0;
        //        player.LastRoundProfitPerc = prDiffPerc;
        //        db.PlayerQA.Update(player);
        //        await db.SaveChangesAsync();
        //    }

        //    ForceSell = false;
        //}

        //public async Task UpdateActivePlayerStatsQA(PlayerQA player)
        //{
        //    DB db = new DB();

        //    var playerSignal = MySignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();

        //    if (playerSignal != null)
        //    {
        //        player.DayHigh = playerSignal.DayHighPr;
        //        player.DayLow = playerSignal.DayLowPr;
        //        player.CurrentCoinPrice = playerSignal.CurrPr;
        //        player.TotalCurrentValue = player.CurrentCoinPrice * player.Quantity;
        //        player.TotalSellAmount = player.TotalCurrentValue;
        //        var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
        //        player.AvailableAmountToBuy = 0;
        //        player.UpdatedTime = DateTime.Now;
        //        db.PlayerQA.Update(player);
        //        await db.SaveChangesAsync();
        //    }
        //}

        //public decimal GetAvailQtyQA(PlayerQA player, string pair)
        //{
        //    decimal availableQty = player.Quantity.Deci();
        //    return availableQty;
        //}

        //private async Task UpdatePlayerAfterSellConfirmedQA(PlayerQA player)
        //{
        //    DB db = new DB();

        //    player.AvailableAmountToBuy = player.TotalSellAmount;

        //    var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);

        //    if (prDiffPerc <= 0)
        //    {
        //        player.BuyOrSell = "Loss";
        //    }
        //    else
        //    {
        //        player.BuyOrSell = "Profit";
        //    }

        //    PlayerTradesQA PlayerTrades = iPlayerQAMapper.Map<PlayerQA, PlayerTradesQA>(player);

        //    PlayerTrades.Id = 0;
        //    await db.PlayerTradesQA.AddAsync(PlayerTrades);
        //    player.LastRoundProfitPerc = 0;
        //    player.DayHigh = 0.0M;
        //    player.DayLow = 0.0M;
        //    player.Pair = null;
        //    player.BuyCoinPrice = 0.0M;
        //    player.CurrentCoinPrice = 0.0M;
        //    player.Quantity = 0.0M;
        //    player.TotalBuyCost = 0.0M;
        //    player.TotalCurrentValue = 0.0M;
        //    player.TotalSellAmount = 0.0M;
        //    player.BuyTime = null;
        //    player.SellTime = null;
        //    player.BuyCommision = 0.0M;
        //    player.SellCoinPrice = 0.0M;
        //    player.SellCommision = 0.0M;
        //    player.IsTrading = false;
        //    player.BuyOrSell = string.Empty;
        //    player.ProfitLossAmt = 0;
        //    player.ProfitLossChanges = string.Empty;
        //    player.BuyOrderId = 0;
        //    player.SellOrderId = 0;
        //    player.HardSellPerc = 0;
        //    player.isBuyOrderCompleted = false;
        //    player.RepsTillCancelOrder = 0;
        //    player.SellAbovePerc = 0.6M;
        //    player.SellBelowPerc = player.SellAbovePerc;
        //    db.PlayerQA.Update(player);
        //    await db.SaveChangesAsync();
        //    await RedistributeBalancesQA();

        //}

        //public async Task RedistributeBalancesQA()
        //{
        //    DB db = new DB();

        //    decimal TotalAmount = 0;

        //    var availplayers = await db.PlayerQA.Where(x => x.IsTrading == false).OrderBy(x => x.Id).ToListAsync();

        //    foreach (var player in availplayers)
        //    {
        //        TotalAmount = TotalAmount + player.AvailableAmountToBuy.Deci();
        //    }


        //    if (availplayers.Count() > 0)
        //    {
        //        var avgAvailAmountForTrading = TotalAmount / availplayers.Count();

        //        foreach (var player in availplayers)
        //        {
        //            player.AvailableAmountToBuy = avgAvailAmountForTrading;
        //            player.TotalCurrentValue = 0;
        //            db.PlayerQA.Update(player);
        //        }
        //        await db.SaveChangesAsync();
        //    }
        //}

        //#endregion QA

        #region QA
        //private async void btnClearPlayer_Click(object sender, RoutedEventArgs e)
        //{
        //    await ClearData();

        //}

        //private async void btnCollectData_Click(object sender, RoutedEventArgs e)
        //{

        //    logger.Info("Collect Data Started at " + DateTime.Now);

        //    var files = Directory.EnumerateFiles(@"C:\Shatlin\klines", "*.csv");
        //    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);

        //    int i = 1;
        //    foreach (string file in files)
        //    {
        //        using (var candledb = new DB())
        //        {

        //            i++;
        //            string filename = file.Split('-')[0];
        //            filename = filename.Substring(filename.LastIndexOf("\\") + 1);
        //            using (var reader = new StreamReader(file))
        //            {
        //                while (!reader.EndOfStream)
        //                {
        //                    string line = reader.ReadLine();
        //                    string[] values = line.Split(",");
        //                    Candle candle = new Candle();
        //                    candle.Symbol = filename;
        //                    double d = Convert.ToDouble(values[0].ToString());
        //                    candle.RecordedTime = DateTime.Now;
        //                    candle.OpenTime = Convert.ToDateTime(epoch.AddMilliseconds(Convert.ToDouble(values[0])));
        //                    candle.Open = Convert.ToDecimal(values[1]);
        //                    candle.High = Convert.ToDecimal(values[2]);
        //                    candle.Low = Convert.ToDecimal(values[3]);
        //                    candle.Close = Convert.ToDecimal(values[4]);
        //                    candle.Volume = Convert.ToDecimal(values[5]);
        //                    candle.CloseTime = Convert.ToDateTime(epoch.AddMilliseconds(Convert.ToDouble(values[6])));
        //                    candle.QuoteAssetVolume = Convert.ToDecimal(values[7]);
        //                    candle.NumberOfTrades = Convert.ToInt32(values[8]);
        //                    candle.TakerBuyBaseAssetVolume = Convert.ToDecimal(values[9]);
        //                    candle.TakerBuyQuoteAssetVolume = Convert.ToDecimal(values[10]);
        //                    candle.Change = 0.0M;
        //                    candle.PriceChangePercent = 0.0M;
        //                    candle.WeightedAveragePercent = 0.0M;
        //                    candle.PreviousClosePrice = 0.0M;
        //                    candle.CurrentPrice = candle.Close;
        //                    candle.OpenPrice = candle.Open;
        //                    candle.DayHighPrice = 0.0M;
        //                    candle.DayLowPrice = 0.0M;
        //                    candle.DayVolume = 0.0M;
        //                    candle.DayTradeCount = 0;


        //                    await candledb.AddAsync(candle);

        //                }

        //                logger.Info(i + " : " + file + " Processing Completed ");


        //            }
        //            await candledb.SaveChangesAsync();
        //        }
        //    }

        //    logger.Info("----------All file Processing Completed-------------- ");
        //    await UpdateData();
        //}

        //private async Task UpdateData()
        //{
        //    DateTime currentdate = new DateTime(2021, 3, 1, 23, 0, 0);
        //    DateTime lastdate = new DateTime(2021, 6, 18, 23, 0, 0);


        //    while (currentdate <= lastdate)
        //    {
        //        using (var TradeDB = new DB())
        //        {

        //            List<Candle> selectedCandles;
        //            List<MyCoins> myTradeFavouredCoins = await TradeDB.MyCoins.AsNoTracking().ToListAsync();

        //            try
        //            {
        //                foreach (var favtrade in myTradeFavouredCoins)
        //                {

        //                    selectedCandles = await TradeDB.Candle.Where(x => x.Symbol.Contains(favtrade.Coin) && x.OpenTime.Date == currentdate.Date
        //                    ).ToListAsync();

        //                    if (selectedCandles == null || selectedCandles.Count == 0) continue;

        //                    foreach (var candle in selectedCandles)
        //                    {
        //                        candle.DayHighPrice = selectedCandles.Max(x => x.High);
        //                        candle.DayLowPrice = selectedCandles.Min(x => x.Low);
        //                        candle.DayVolume = selectedCandles.Sum(x => x.Volume);
        //                        candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
        //                        TradeDB.Candle.Update(candle);
        //                    }
        //                    await TradeDB.SaveChangesAsync();
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                logger.Error(" Updating candle error " + ex.Message);
        //            }

        //        }

        //        currentdate = currentdate.AddDays(1);
        //    }

        //    logger.Info(" Updating data Completed ");
        //}

        //private async Task ClearData()
        //{
        //    DB TradeDB = new DB();

        //    await TradeDB.Database.ExecuteSqlRawAsync("TRUNCATE TABLE PlayerTrades");

        //    var players = await TradeDB.Player.ToListAsync();

        //    foreach (var player in players)
        //    {
        //        player.Pair = null;
        //        player.DayHigh = 0.0M;
        //        player.DayLow = 0.0M;
        //        player.BuyPricePerCoin = 0.0M;
        //        player.CurrentPricePerCoin = 0.0M;
        //        player.QuantityBought = 0.0M;
        //        player.TotalBuyCost = 0.0M;
        //        player.TotalCurrentValue = 0.0M;
        //        player.TotalSoldAmount = 0.0M;
        //        player.BuyTime = null;
        //        player.SellTime = null;
        //        player.CreatedDate = new DateTime(2021, 6, 16, 0, 0, 0);
        //        player.UpdatedTime = null;
        //        player.IsTrading = false;
        //        player.AvailableAmountForTrading = 100;
        //        player.OriginalAllocatedValue = 100;
        //        player.BuyingCommision = 0.0M;
        //        player.QuantitySold = 0.0M;
        //        player.SoldCommision = 0.0M;
        //        player.SoldPricePricePerCoin = 0.0M;
        //        player.TotalCurrentProfit = 0.0M;
        //        player.CandleOpenTimeAtBuy = null;
        //        player.BuyOrSell = string.Empty;
        //        player.CandleOpenTimeAtSell = null;
        //        player.TotalExpectedProfit = 0.0M;
        //        player.BuyCandleId = 0;
        //        player.SellCandleId = 0;
        //        player.SaleProfitOrLoss = 0;
        //        player.LossOrProfit = string.Empty;
        //        TradeDB.Update(player);
        //    }
        //    await TradeDB.SaveChangesAsync();


        //}

        //private async Task<List<Candle>> GetCandles_QA()
        //{
        //    logger.Info("Getting Candle Started at " + DateTime.Now.ToString("dd-MMM HH:mm"));
        //    //#TODO Update bots with current price when getting candles.

        //    DB candledb = new DB();


        //    List<Candle> candles = new List<Candle>();

        //    try
        //    {

        //        var StartlastCandleMinute = candledb.Candle.Max(x => x.OpenTime);

        //        MyCoins = await candledb.MyCoins.ToListAsync();

        //        var prices = await client.GetAllPrices();
        //        // await UpdateBalance(prices);



        //        #region get all missing candles

        //        var totalmins = (DateTime.Now - StartlastCandleMinute).TotalMinutes;

        //        if (totalmins > 30) //means you missed to get the last candle, so get those first.
        //        {
        //            logger.Info("    Candles missed. Collecting them ");

        //            foreach (var coin in MyCoins)
        //            {
        //                var pricesofcoin = prices.Where(x => x.Symbol.Contains(coin.Coin + "USDT"));
        //                if (pricesofcoin == null || pricesofcoin.Count() == 0)
        //                {
        //                    continue;
        //                }
        //                foreach (var price in pricesofcoin)
        //                {

        //                    GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
        //                    cr.Limit = 200;
        //                    cr.Symbol = price.Symbol;
        //                    cr.Interval = KlineInterval.FifteenMinutes;
        //                    cr.StartTime = Convert.ToDateTime(StartlastCandleMinute).AddMinutes(15);
        //                    cr.EndTime = DateTime.Now.AddMinutes(-15);
        //                    var candleresponse = await client.GetKlinesCandlesticks(cr);

        //                    foreach (var candleResp in candleresponse)
        //                    {
        //                        Candle addCandle = new Candle();

        //                        addCandle.Symbol = cr.Symbol;
        //                        addCandle.Open = candleResp.Open;
        //                        addCandle.RecordedTime = DateTime.Now;
        //                        addCandle.OpenTime = candleResp.OpenTime.AddHours(hourDifference);
        //                        addCandle.High = candleResp.High;
        //                        addCandle.Low = candleResp.Low;
        //                        addCandle.Close = candleResp.Close;
        //                        addCandle.Volume = candleResp.Volume;
        //                        addCandle.CloseTime = candleResp.CloseTime.AddHours(hourDifference);
        //                        addCandle.QuoteAssetVolume = candleResp.QuoteAssetVolume;
        //                        addCandle.NumberOfTrades = candleResp.NumberOfTrades;
        //                        addCandle.TakerBuyBaseAssetVolume = candleResp.TakerBuyBaseAssetVolume;
        //                        addCandle.TakerBuyQuoteAssetVolume = candleResp.TakerBuyQuoteAssetVolume;
        //                        addCandle.Change = 0;
        //                        addCandle.PriceChangePercent = 0;
        //                        addCandle.WeightedAveragePercent = 0;
        //                        addCandle.PreviousClosePrice = 0;
        //                        addCandle.CurrentPrice = candleResp.Close;
        //                        addCandle.OpenPrice = candleResp.Open;
        //                        addCandle.DayHighPrice = 0;
        //                        addCandle.DayLowPrice = 0;
        //                        addCandle.DayVolume = 0;
        //                        addCandle.DayTradeCount = 0;

        //                        var isCandleExisting = await candledb.Candle.Where(x => x.OpenTime == addCandle.OpenTime && x.Symbol == addCandle.Symbol).FirstOrDefaultAsync();

        //                        if (isCandleExisting == null)
        //                        {
        //                            candles.Add(addCandle);
        //                            await candledb.Candle.AddAsync(addCandle);
        //                            await candledb.SaveChangesAsync();
        //                        }
        //                    }
        //                }
        //                Thread.Sleep(1000);
        //            }

        //            var UpdatedlastCandleMinutes = candledb.Candle.Max(x => x.OpenTime);
        //            List<Candle> selectedCandles;
        //            while (StartlastCandleMinute <= UpdatedlastCandleMinutes)
        //            {
        //                try
        //                {
        //                    foreach (var favtrade in MyCoins)
        //                    {
        //                        selectedCandles = await candledb.Candle.Where(x => x.Symbol.Contains(favtrade.Coin) && x.OpenTime.Date == StartlastCandleMinute.Date).ToListAsync();

        //                        if (selectedCandles == null || selectedCandles.Count == 0) continue;

        //                        foreach (var candle in selectedCandles)
        //                        {
        //                            candle.DayHighPrice = selectedCandles.Max(x => x.High);
        //                            candle.DayLowPrice = selectedCandles.Min(x => x.Low);
        //                            candle.DayVolume = selectedCandles.Sum(x => x.Volume);
        //                            candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
        //                            candledb.Candle.Update(candle);
        //                        }
        //                        await candledb.SaveChangesAsync();
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    logger.Error(" Updating candle error " + ex.Message);
        //                }
        //                StartlastCandleMinute = StartlastCandleMinute.AddDays(1);
        //            }
        //            logger.Info("    Collecting missed candles completed ");
        //        }
        //        #endregion get all missing candles

        //        foreach (var coin in MyCoins)
        //        {
        //            var pricesofcoin = prices.Where(x => x.Symbol.Contains(coin.Coin + "USDT"));
        //            if (pricesofcoin == null || pricesofcoin.Count() == 0)
        //            {
        //                continue;
        //            }
        //            foreach (var price in pricesofcoin)
        //            {

        //                Candle candle = new Candle();
        //                var pricechangeresponse = await client.GetDailyTicker(price.Symbol);
        //                GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
        //                cr.Limit = 1;
        //                cr.Symbol = price.Symbol;
        //                cr.Interval = KlineInterval.FifteenMinutes;

        //                var candleresponse = await client.GetKlinesCandlesticks(cr);
        //                candle.RecordedTime = DateTime.Now;
        //                candle.Symbol = price.Symbol;
        //                candle.Open = candleresponse[0].Open;
        //                candle.OpenTime = candleresponse[0].OpenTime.AddHours(hourDifference);
        //                candle.High = candleresponse[0].High;
        //                candle.Low = candleresponse[0].Low;
        //                candle.Close = candleresponse[0].Close;
        //                candle.Volume = candleresponse[0].Volume;
        //                candle.CloseTime = candleresponse[0].CloseTime.AddHours(hourDifference);
        //                candle.QuoteAssetVolume = candleresponse[0].QuoteAssetVolume;
        //                candle.NumberOfTrades = candleresponse[0].NumberOfTrades;
        //                candle.TakerBuyBaseAssetVolume = candleresponse[0].TakerBuyBaseAssetVolume;
        //                candle.TakerBuyQuoteAssetVolume = candleresponse[0].TakerBuyQuoteAssetVolume;
        //                candle.Change = pricechangeresponse.PriceChange;
        //                candle.PriceChangePercent = pricechangeresponse.PriceChangePercent;
        //                candle.WeightedAveragePercent = pricechangeresponse.PriceChangePercent;
        //                candle.PreviousClosePrice = pricechangeresponse.PreviousClosePrice;
        //                candle.CurrentPrice = pricechangeresponse.LastPrice;
        //                candle.OpenPrice = pricechangeresponse.OpenPrice;
        //                candle.DayHighPrice = pricechangeresponse.HighPrice;
        //                candle.DayLowPrice = pricechangeresponse.LowPrice;
        //                candle.DayVolume = pricechangeresponse.Volume;
        //                candle.DayTradeCount = pricechangeresponse.TradeCount;
        //                // candle.DataSet = candlecurrentSet;

        //                var isCandleExisting = await candledb.Candle.Where(x => x.OpenTime == candle.OpenTime && x.Symbol == candle.Symbol).FirstOrDefaultAsync();

        //                if (isCandleExisting == null)
        //                {
        //                    candles.Add(candle);
        //                    await candledb.Candle.AddAsync(candle);
        //                    await candledb.SaveChangesAsync();
        //                }
        //                else
        //                {
        //                    candledb.Candle.Update(isCandleExisting);
        //                    await candledb.SaveChangesAsync();
        //                }
        //            }
        //        }

        //        logger.Info("Getting Candle Completed at " + DateTime.Now.ToString("dd-MMM HH:mm"));
        //        logger.Info("");
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Info("Exception in Getting Candle  " + ex.Message);
        //        throw;
        //    }
        //    return candles;
        //}

        //private async Task<List<Signal>> GetSignals_QA(DateTime cndlHr)
        //{

        //    DB TradeDB = new DB();

        //    List<Signal> signals = new List<Signal>();

        //    List<Candle> latestCndls = await TradeDB.Candle.AsNoTracking().Where(x => x.OpenTime == cndlHr).ToListAsync();

        //    // DateTime refCandlMinTime = currentCandleSetDate.AddHours(-23);

        //    List<Candle> refCndls = await TradeDB.Candle.AsNoTracking()
        //        .Where(x => x.OpenTime >= cndlHr.AddHours(-23) && x.OpenTime < cndlHr).ToListAsync();

        //    foreach (var myfavcoin in MyCoins)
        //    {
        //        #region dealwithUSDT only

        //        Signal sig = new Signal();
        //        sig.Symbol = myfavcoin.Coin + "USDT";
        //        var selCndl = latestCndls.Where(x => x.Symbol == sig.Symbol).FirstOrDefault();
        //        if (selCndl == null) continue;
        //        var selRefCndls = refCndls.Where(x => x.Symbol == sig.Symbol);

        //        if (selRefCndls == null || selRefCndls.Count() == 0) continue;

        //        #endregion

        //        #region Prefer BUSD if not available go for USDT

        //        //List<string> usdStrings = new List<string>()
        //        //    { myfavcoin.Coin + "USDT"}; //, myfavcoin.Coin + "BUSD", myfavcoin.Coin + "USDC"

        //        //var usdCndlList = latestCndls.Where(x => usdStrings.Contains(x.Symbol));

        //        //if (usdCndlList == null) continue;

        //        //var busdcandle = usdCndlList.Where(x => x.Symbol == myfavcoin.Coin + "BUSD").FirstOrDefault();

        //        //Signal sig = new Signal();

        //        //if (busdcandle != null) sig.Symbol = busdcandle.Symbol;
        //        //else 

        //        //    sig.Symbol = myfavcoin.Coin + "USDT";

        //        //var selCndl = usdCndlList.Where(x => x.Symbol == sig.Symbol).FirstOrDefault();

        //        //if (selCndl == null) continue;

        //        //var usdRefCndls = refCndls.Where(x => usdStrings.Contains(x.Symbol));

        //        //if (usdRefCndls == null || usdRefCndls.Count() == 0) continue;

        //        #endregion

        //        sig.CurrPr = selCndl.CurrentPrice;
        //        sig.DayHighPr = selCndl.DayHighPrice;
        //        sig.DayLowPr = selCndl.DayLowPrice;
        //        sig.CandleOpenTime = selCndl.OpenTime;
        //        sig.CandleCloseTime = selCndl.CloseTime;
        //        sig.CandleId = selCndl.Id;
        //        sig.DayVol = selRefCndls.Sum(x => x.DayVolume);
        //        sig.DayTradeCount = selRefCndls.Sum(x => x.DayTradeCount);
        //        sig.RefHighPr = selRefCndls.Max(x => x.CurrentPrice);
        //        sig.RefLowPr = selRefCndls.Min(x => x.CurrentPrice);
        //        sig.RefAvgCurrPr = selRefCndls.Average(x => x.CurrentPrice);
        //        sig.RefDayVol = selRefCndls.Average(x => x.DayVolume);
        //        sig.RefDayTradeCount = (int)selRefCndls.Average(x => x.DayTradeCount);

        //        sig.DayPrDiffPercentage = sig.DayHighPr.GetDiffPerc(sig.DayLowPr);
        //        sig.PrDiffCurrAndHighPerc = Math.Abs(sig.DayHighPr.GetDiffPerc(sig.CurrPr));
        //        sig.PrDiffCurrAndLowPerc = Math.Abs(sig.DayLowPr.GetDiffPerc(sig.CurrPr));
        //        // this will always be positive. You need to first target those coins which are 
        //        //closest to low price. Dont worry about trade count for now

        //        sig.CurrPrDiffSigAndRef = sig.CurrPr.GetDiffPerc(sig.RefAvgCurrPr);
        //        //Difference between current price and the average current prices of last 24 hours

        //        var dayAveragePrice = (sig.DayHighPr + sig.DayLowPr) / 2;

        //        if (sig.CurrPr < dayAveragePrice) sig.IsCloseToDayLow = true;

        //        else sig.IsCloseToDayHigh = true;

        //        signals.Add(sig);

        //    }

        //    return signals.OrderBy(x => x.PrDiffCurrAndLowPerc).ToList();
        //}

        //private async Task Buy_QA(List<Signal> Signals)
        //{

        //    /*
        //     *Step 1: Get All balances and apart from USDT, ensure they correspond to your player table.
        //     *Step 2: coins other than USDT are your "Actively Trading Coins"
        //     *Step 3: Once the matching is done,ignore anything in binance ( Could be from staking)
        //     *Step 4: Divide USDT to available bots, but leave 5% in account to cater for inconsistencies, this is what is available for them to buy. Update all Player fields.
        //     *
        //     *Remember: Get all signals, but just before issuing a buy order, get current price and do "the" checks.
        //     *Remember:Always issue limit order, so that you can record the exact price for which you bought for 
        //        In Prod, I would just need the latest candle list and no references.

        //     */

        //    if (Signals == null || Signals.Count() == 0)
        //    {
        //        logger.Info("No signals found. returning from buying");
        //        return;
        //    }

        //    var candleCloseTime = Signals.FirstOrDefault().CandleCloseTime.ToString("dd-MMM HH:mm");
        //    var candleOpenTime = Signals.FirstOrDefault().CandleOpenTime.ToString("dd-MMM HH:mm");

        //    logger.Info("Buying scan Started for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);
        //    #region definitions

        //    DB db = new DB();
        //    var players = await db.Player.OrderBy(x => x.Id).ToListAsync();

        //    boughtCoins = await db.Player.Where(x => x.Pair != null).
        //                  Select(x => x.Pair).ToListAsync();

        //    bool isdbUpdateRequired = false;

        //    #endregion definitions

        //    //bool isanybuyingdone = false;

        //    foreach (var player in players)
        //    {
        //        #region if player is currently trading, just update stats and go to next player

        //        if (player.IsTrading)
        //        {

        //            logger.Info("  " + candleCloseTime + " " + player.Name + player.Avatar + " currently occupied");

        //            var playersSignal = Signals.Where(x => x.Symbol == player.Pair).FirstOrDefault();

        //            if (playersSignal != null)
        //            {
        //                player.DayHigh = playersSignal.DayHighPr;
        //                player.DayLow = playersSignal.DayLowPr;
        //                player.CurrentPricePerCoin = playersSignal.CurrPr;
        //                player.TotalCurrentValue = player.CurrentPricePerCoin * player.QuantityBought;
        //                player.TotalCurrentProfit = player.TotalCurrentValue - player.OriginalAllocatedValue;
        //                player.UpdatedTime = DateTime.Now;
        //                isdbUpdateRequired = true;
        //            }
        //            continue;
        //        }

        //        #endregion if player is currently trading, just update stats and go to next player

        //        foreach (var sig in Signals)
        //        {

        //            if (boughtCoins.Contains(sig.Symbol))
        //            {
        //                logger.Info("  " +
        //                     sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
        //                    " " + player.Name + player.Avatar +
        //                    " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
        //                    " coin already bought. Going to next signal ");
        //                continue;
        //            }

        //            if (sig.IsPicked)
        //            {
        //                logger.Info("  " +
        //                sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
        //                " " + player.Name + player.Avatar +
        //                " " + sig.Symbol.Replace("USDT", "") +
        //                " is picked by another bot. look for another coin ");

        //                continue;
        //            }


        //            //buying criteria
        //            //1. signals are ordered by the coins whose current price are at their lowest at the moment
        //            //2. See if this price is the lowest in the last 24 hours
        //            //3. See if the price difference is lower than what the player is expecting to buy at. If yes, buy.

        //            //Later see if you are on a downtrend and keep waiting till it reaches its low and then buy

        //            if (sig.IsCloseToDayLow && (sig.PrDiffCurrAndHighPerc > player.BuyBelowPerc))
        //            {

        //                logger.Info("  " +
        //                    sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
        //                  " " + player.Name + player.Avatar +
        //                 " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
        //                  " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
        //                  " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
        //                  " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
        //                  " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
        //                  " < -" + player.BuyBelowPerc.Deci().Rnd(0) + " Buy Now");


        //                player.IsTrading = true;
        //                player.Pair = sig.Symbol;
        //                player.DayHigh = sig.DayHighPr;
        //                player.DayLow = sig.DayLowPr;
        //                player.BuyPricePerCoin = sig.CurrPr;
        //                player.QuantityBought = player.AvailableAmountForTrading / sig.CurrPr;
        //                player.BuyingCommision = player.AvailableAmountForTrading * 0.075M / 100;
        //                player.TotalBuyCost = player.AvailableAmountForTrading + player.BuyingCommision;
        //                player.CurrentPricePerCoin = sig.CurrPr;
        //                player.TotalCurrentValue = player.AvailableAmountForTrading;
        //                player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
        //                player.BuyTime = DateTime.Now;
        //                player.AvailableAmountForTrading = 0;
        //                player.CandleOpenTimeAtBuy = sig.CandleOpenTime;
        //                player.CandleOpenTimeAtSell = null;
        //                player.BuyCandleId = sig.CandleId;
        //                player.SellCandleId = 0;
        //                player.UpdatedTime = DateTime.Now;
        //                player.BuyOrSell = "Buy";
        //                player.SellTime = null;
        //                player.QuantitySold = 0.0M;
        //                player.SoldCommision = 0.0M;
        //                player.SoldPricePricePerCoin = 0.0M;
        //                sig.IsPicked = true;
        //                db.Player.Update(player);
        //                PlayerTrades playerHistory = iMapr.Map<Player, PlayerTrades>(player);
        //                playerHistory.Id = 0;
        //                await db.PlayerTrades.AddAsync(playerHistory);
        //                isdbUpdateRequired = true; //flag that db needs to be updated, and update it at the end
        //                boughtCoins.Add(sig.Symbol);

        //                break;
        //            }

        //            else
        //            {
        //                logger.Info("  " +
        //                      sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
        //                    " " + player.Name + player.Avatar +
        //                   " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
        //                    " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
        //                    " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
        //                    " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
        //                    " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
        //                    " > -" + player.BuyBelowPerc.Deci().Rnd(0) +
        //                    " Or not close to day low, Not Buying");
        //            }
        //        }
        //    }

        //    if (isdbUpdateRequired) await db.SaveChangesAsync();


        //    logger.Info("Buying scan Completed for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);

        //    logger.Info("");
        //}

        //private async Task Sell_QA(List<Signal> Signals)
        //{

        //    bool isSaleHappen = false;

        //    if (Signals == null || Signals.Count() == 0)
        //    {
        //        logger.Info("No signals found. returning from selling");
        //        return;
        //    }

        //    var candleCloseTime = Signals.FirstOrDefault().CandleCloseTime.ToString("dd-MMM HH:mm");
        //    var candleOpenTime = Signals.FirstOrDefault().CandleOpenTime.ToString("dd-MMM HH:mm");


        //    logger.Info("Selling scan Started for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);

        //    DB TradeDB = new DB();
        //    var players = await TradeDB.Player.OrderBy(x => x.Id).ToListAsync();

        //    foreach (var player in players)
        //    {

        //        if (!player.IsTrading)
        //        {
        //            logger.Info("  " + candleCloseTime +
        //             " " + player.Name + player.Avatar +
        //             "  waiting to buy. Nothing to sell");
        //            continue;
        //        }

        //        var CoinPair = player.Pair;
        //        var sig = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();


        //        if (sig == null)
        //        {
        //            logger.Info("  " + candleCloseTime +
        //             " " + player.Name + player.Avatar +
        //             " " + CoinPair.Replace("USDT", "").PadRight(7, ' ') +
        //             " No signals returned. Continuing ");
        //            continue;
        //        }

        //        //update to set all these values in PlayerHist
        //        player.TotalBuyCost = player.BuyPricePerCoin * player.QuantityBought + player.BuyingCommision;
        //        player.SoldCommision = player.CurrentPricePerCoin * player.QuantityBought * 0.075M / 100;
        //        player.TotalSoldAmount = player.TotalCurrentValue - player.SoldCommision;

        //        var prDiffPerc = player.TotalSoldAmount.GetDiffPerc(player.TotalBuyCost);


        //        if ((prDiffPerc > player.SellAbovePerc) || (prDiffPerc < player.SellBelowPerc))
        //        {

        //            if (prDiffPerc < player.DontSellBelowPerc)
        //            {
        //                logger.Info("  " +
        //                   candleCloseTime +
        //                   " " + player.Name + player.Avatar +
        //                   " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
        //                   " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(10, ' ') +
        //                   " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(10, ' ') +
        //                    " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
        //                   " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(10, ' ') +
        //                   " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
        //                   " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
        //                   " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(0) +
        //                   " > " + player.DontSellBelowPerc.Deci().Rnd(0) +
        //                   " Not selling");

        //                continue; //not selling for this player, but continuing for other players
        //            }
        //            player.SaleProfitOrLoss = (player.TotalSoldAmount - player.TotalBuyCost).Deci();
        //            player.DayHigh = sig.DayHighPr;
        //            player.DayLow = sig.DayLowPr;
        //            player.CurrentPricePerCoin = sig.CurrPr;
        //            player.TotalCurrentValue = player.CurrentPricePerCoin * player.QuantityBought;
        //            player.QuantitySold = Convert.ToDecimal(player.QuantityBought);
        //            player.AvailableAmountForTrading = player.TotalSoldAmount;
        //            player.SellTime = DateTime.Now;
        //            player.UpdatedTime = DateTime.Now;
        //            player.SoldPricePricePerCoin = sig.CurrPr;
        //            player.CandleOpenTimeAtSell = sig.CandleOpenTime;
        //            player.BuyOrSell = "SELL";
        //            player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
        //            player.SellCandleId = sig.CandleId;

        //            if (prDiffPerc > player.SellAbovePerc)
        //            {
        //                logger.Info("  " +
        //                   candleCloseTime +
        //                    " " + player.Name + player.Avatar +
        //                    " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
        //              " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
        //              " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
        //              " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
        //              " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
        //              " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
        //              " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
        //                    " > +" + player.SellAbovePerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
        //                    " Prof Sell");

        //                player.LossOrProfit = "Profit";
        //            }
        //            else if (prDiffPerc < player.SellBelowPerc)
        //            {

        //                logger.Info("  " +
        //              candleCloseTime +
        //                " " + player.Name + player.Avatar +
        //                " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
        //                " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
        //                " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
        //                " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
        //                " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
        //                " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
        //                " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
        //                " < " + player.SellBelowPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
        //                " loss sell");

        //                player.LossOrProfit = "Loss";
        //            }

        //            // create sell order (in live system)


        //            PlayerTrades PlayerHist = iMapr.Map<Player, PlayerTrades>(player);
        //            PlayerHist.Id = 0;
        //            await TradeDB.PlayerTrades.AddAsync(PlayerHist);

        //            // reset records to buy again
        //            player.DayHigh = 0.0M;
        //            player.DayLow = 0.0M;
        //            player.Pair = null;
        //            player.BuyPricePerCoin = 0.0M;
        //            player.CurrentPricePerCoin = 0.0M;
        //            player.QuantityBought = 0.0M;
        //            player.TotalBuyCost = 0.0M;
        //            player.TotalCurrentValue = 0.0M;
        //            player.TotalSoldAmount = 0.0M;
        //            player.BuyTime = null;
        //            player.SellTime = null;
        //            player.BuyingCommision = 0.0M;
        //            player.SoldPricePricePerCoin = 0.0M;
        //            player.QuantitySold = 0.0M;
        //            player.SoldCommision = 0.0M;
        //            player.IsTrading = false;
        //            player.CandleOpenTimeAtBuy = null;
        //            player.CandleOpenTimeAtSell = null;
        //            player.BuyOrSell = string.Empty;
        //            player.SaleProfitOrLoss = 0;
        //            player.LossOrProfit = string.Empty;

        //            TradeDB.Player.Update(player);
        //            isSaleHappen = true;
        //        }
        //        else
        //        {

        //            logger.Info("  " +
        //                candleCloseTime +
        //                " " + player.Name + player.Avatar +
        //                " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
        //                " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
        //                " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
        //                " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
        //                " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
        //                " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
        //                " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
        //                " < " + player.SellAbovePerc.Deci().Rnd(1).ToString().PadRight(3, ' ') +
        //                " and > " + player.SellBelowPerc.Deci().Rnd(1) + " Dont Sell");
        //        }
        //    }

        //    if (isSaleHappen) // redistribute balances
        //    {
        //        var availplayers = await TradeDB.Player.Where(x => x.IsTrading == false).ToListAsync();
        //        var avgAvailAmountForTrading = availplayers.Average(x => x.AvailableAmountForTrading);

        //        foreach (var player in availplayers)
        //        {
        //            player.AvailableAmountForTrading = avgAvailAmountForTrading;
        //            TradeDB.Player.Update(player);
        //        }

        //    }

        //    await TradeDB.SaveChangesAsync();

        //    logger.Info("Selling scan Completed for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);
        //    logger.Info("");
        //}

        #endregion QA

        #region oldcode

        //private async Task BuyAda()
        //{
        //    if (MySignals == null || MySignals.Count() == 0)
        //    {
        //        logger.Info("  " + StrTradeTime + " no signals found. So cannot proceed for buy process ");
        //        return;
        //    }

        //    DB db = new DB();

        //    var players = await db.Player.OrderBy(x => x.Id).ToListAsync();

        //    var player1 = players.Where(x => x.Name == "DIA01").FirstOrDefault();
        //    var player2 = players.Where(x => x.Name == "DIA02").FirstOrDefault();

        //    var isPlayer1Trading = player1.IsTrading;
        //    var isPlayer2Trading = player2.IsTrading;

        //    var player1prDiffPerc = player1.TotalSellAmount.GetDiffPercBetnNewAndOld(player1.TotalBuyCost);
        //    var player2prDiffPerc = player2.TotalSellAmount.GetDiffPercBetnNewAndOld(player2.TotalBuyCost);

        //    foreach (var player in players)
        //    {
        //        if (player.IsTrading)
        //        {
        //            await UpdateActivePlayerStats(player);
        //            continue;
        //        }

        //        if (player.isBuyOrderCompleted) // before buying the buyordercompleted should be reset to false, so dont buy if its true
        //        {
        //            logger.Info("  " + StrTradeTime + " " + player.Name + " isBuyOrderCompleted is true. Cant use it to buy");
        //            continue;
        //        }
        //        if (player.isBuyAllowed == false)
        //        {
        //            if (configr.ShowBuyingFlowLogs) logger.Info("  " + StrTradeTime + " " + player.Name + "  Not  Allowed for buying");
        //            continue;
        //        }
        //        if (configr.IsBuyingAllowed == false)
        //        {
        //            if (configr.ShowBuyingFlowLogs) logger.Info("  " + StrTradeTime + " " + player.Name + "  overall system not  Allowed for buying");
        //            continue;
        //        }

        //        var sig = MySignals.Where(x => x.Symbol == "ADAUSDT").FirstOrDefault();

        //        //    prices are going down. Dont buy till you see recovery
        //        if (sig.TotalConsecutive1MinDowns >= 3 || sig.TotalConsecutive5MinDowns >= 2)
        //        {
        //            logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
        //              " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
        //              "  Prices going down." +
        //              " 1d >=3  ? " + sig.TotalConsecutive1MinDowns.ToString().PadRight(7, ' ') + " " +
        //              " 5d >=2  ? " + sig.TotalConsecutive5MinDowns.ToString().PadRight(7, ' ')
        //              );

        //            return;
        //        }

        //        //  Day Trade Count too low
        //        if (sig.DayTradeCount < configr.MinAllowedTradeCount)
        //        {
        //            logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
        //              " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
        //              "  Day Trade Count too low. Dont buy " +
        //              " Day Trade Count " + sig.DayTradeCount.Rnd(1)
        //              );

        //            return;
        //        }


        //        if (isPlayer1Trading)
        //        {
        //            if (player.Name == "DIA02" && player1prDiffPerc < -2M && sig.IsBestTimeToScalpBuy)
        //            {
        //                try
        //                {
        //                    await BuyTheCoin(player, sig);
        //                    return;
        //                }
        //                catch (Exception ex)
        //                {
        //                    logger.Error(" " + player.Name + " " + sig.Symbol + " Exception: " + ex.ToString());
        //                }
        //            }
        //        }
        //        else if (isPlayer2Trading)
        //        {
        //            if (player.Name == "DIA01" && player2prDiffPerc < -2M && sig.IsBestTimeToScalpBuy)
        //            {
        //                try
        //                {
        //                    await BuyTheCoin(player, sig);
        //                    return;
        //                }
        //                catch (Exception ex)
        //                {
        //                    logger.Error(" " + player.Name + " " + sig.Symbol + " Exception: " + ex.ToString());
        //                }
        //            }
        //        }
        //        else if (sig.IsBestTimeToScalpBuy) //  sig.IsBestTimeToBuyAtDayLowest
        //        {
        //            try
        //            {
        //                await BuyTheCoin(player, sig);
        //                return;
        //            }
        //            catch (Exception ex)
        //            {
        //                logger.Error(" " + player.Name + " " + sig.Symbol + " Exception: " + ex.ToString());
        //            }
        //        }
        //        else
        //        {
        //            LogNoBuy(player, sig);
        //        }

        //    }
        //}

        //private async Task RemoveOldCandles()
        //{

        //    using (var db = new DB())
        //    {
        //        await db.Database.ExecuteSqlRawAsync("delete from Candle where CAST(RecordedTime AS DATE)  <= GETDATE()-1");
        //    }

        //}




        #endregion
    }

}

//private async Task SetGrid()
//{
//    DB db = new DB();
//    decimal? totProfitPerc = 0;
//    decimal? totProfit = 0;
//    decimal? totalbuys = 0;
//    decimal? totalcurrent = 0;
//    PlayerViewModels = new List<PlayerViewModel>();
//    var players = await db.Player.Where(x => x.IsTrading == true).ToListAsync();

//    foreach (var player in players)
//    {
//        PlayerViewModel playerViewModel = new PlayerViewModel();
//        var pair = player.Pair;
//        playerViewModel.Name = player.Name;
//        playerViewModel.Pair = pair;
//        playerViewModel.BuyPricePerCoin = player.BuyCoinPrice;
//        playerViewModel.CurrentPricePerCoin = player.CurrentCoinPrice;
//        playerViewModel.QuantityBought = player.Quantity;
//        playerViewModel.BuyTime = Convert.ToDateTime(player.BuyTime).ToString("dd-MMM HH:mm");
//        playerViewModel.SellBelowPerc = player.SellBelowPerc;
//        playerViewModel.SellAbovePerc = player.SellAbovePerc;
//        playerViewModel.TotalBuyCost = player.TotalBuyCost;
//        playerViewModel.TotalSoldAmount = player.TotalSellAmount;
//        playerViewModel.TotalCurrentValue = player.TotalCurrentValue;
//        totalcurrent += playerViewModel.TotalCurrentValue;
//        totalbuys += playerViewModel.TotalBuyCost;
//        var prDiffPerc = player.TotalCurrentValue.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
//        totProfitPerc += prDiffPerc;
//        totProfit += (player.TotalCurrentValue - player.TotalBuyCost);
//        playerViewModel.CurrentRoundProfitPerc = prDiffPerc;
//        playerViewModel.CurrentRoundProfitAmt = player.TotalCurrentValue - player.TotalBuyCost;
//        playerViewModel.LastRoundProfitPerc = player.LastRoundProfitPerc;
//        playerViewModel.ProfitLossChanges = player.ProfitLossChanges.GetLast(95);
//        PlayerViewModels.Add(playerViewModel);
//    }

//    PlayerGrid.ItemsSource = PlayerViewModels.OrderByDescending(x => x.CurrentRoundProfitAmt);

//    var inactiveplayers = await db.Player.Where(x => x.IsTrading == false).ToListAsync();

//    foreach (var inactiveplayer in inactiveplayers)
//    {
//        totalbuys += inactiveplayer.AvailableAmountToBuy;
//        totalcurrent += inactiveplayer.AvailableAmountToBuy;
//    }

//    lblAvgProfLoss.Text = "Profit: " + totProfit.Deci().Rnd(2) + " Invested: " + totalbuys.Deci().Rnd(0) + " Current: " + totalcurrent.Deci().Rnd(0);

//    lblLastRun.Text = "Last Run : " + StrTradeTime;
//    lblNextRun.Text = "Next Run: " + NextTradeTime;
//}



//public async Task UpdateAllowedPrecisionsForPairs()
//{

//    DB db = new DB();

//    exchangeInfo = await client.GetExchangeInfo();
//    await GetMyCoins();

//    foreach (var coin in myCoins)
//    {
//        var symbol = exchangeInfo.Symbols.Where(x => x.Symbol == coin.Pair).FirstOrDefault();
//        if (symbol != null)
//        {
//            ExchangeInfoSymbolFilterLotSize lotsize = symbol.Filters[2] as ExchangeInfoSymbolFilterLotSize;
//            var precision = lotsize.StepSize.GetAllowedPrecision();
//            coin.TradePrecision = precision;
//            db.MyCoins.Update(coin);
//        }
//        //    logger.Info("Precision for coin " + coin.Coin + " is set as " + precision + " Original step size from exchange info is " + lotsize.StepSize);
//    }

//    await db.SaveChangesAsync();


//}









//private void oldbuylowlogic()
//{
//    ////if current price is less than the lowest price of last two weeks
//    //// if current price is close to days low
//    ////if current price like -9% less than minimum price of last two weeks ( too much)

//    //if (sig.CurrPr < sig.MinRef1Day && sig.IsCloseToDayLow && sig.PrDiffCurrAndHighPerc < sig.PercBelowDayHighToBuy)
//    //{
//    //    sig.IsBestTimeToBuyAtDayLowest = true;
//    //    logger.Info(sig.Symbol + "  " + StrTradeTime + " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(5, ' ')
//    //        + " < Ref 1day candle mins " + sig.MinRef1Day.Rnd(5).ToString().PadRight(5, ' '));
//    //    continue;
//    //}
//    //else
//    //{
//    //    sig.IsBestTimeToBuyAtDayLowest = false;
//    //}

//    //if (sig.IsBestTimeToBuyAtDayLowest == false)
//    //{
//    //    if (sig.CurrPr < sig.MinRef4Hour && sig.IsCloseToDayLow && sig.CurrPr.GetDiffPercBetnNewAndOld(sig.MinRef4Hour) < sig.PercBelowDayHighToBuy)
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = true;
//    //        logger.Info(sig.Symbol + "  " + StrTradeTime + " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(5, ' ')
//    //            + " < Ref 4hr candle mins " + sig.MinRef4Hour.Rnd(5).ToString().PadRight(5, ' '));
//    //        continue;
//    //    }
//    //    else
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = false;
//    //    }
//    //}
//    //if (sig.IsBestTimeToBuyAtDayLowest == false)
//    //{
//    //    if (sig.CurrPr < sig.MinRef1Hour && sig.IsCloseToDayLow && sig.CurrPr.GetDiffPercBetnNewAndOld(sig.MinRef1Hour) < sig.PercBelowDayHighToBuy)
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = true;
//    //        logger.Info(sig.Symbol + "  " + StrTradeTime + " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(5, ' ')
//    //            + " < Ref 1hr candle mins " + sig.MinRef1Hour.Rnd(5).ToString().PadRight(5, ' '));
//    //        continue;
//    //    }
//    //    else
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = false;
//    //    }
//    //}
//    //if (sig.IsBestTimeToBuyAtDayLowest == false)
//    //{
//    //    if (sig.CurrPr < sig.MinRef30Min && sig.IsCloseToDayLow && sig.CurrPr.GetDiffPercBetnNewAndOld(sig.MinRef30Min) < sig.PercBelowDayHighToBuy)
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = true;
//    //        logger.Info(sig.Symbol + "  " + StrTradeTime + " CrPr " + sig.CurrPr.Rnd(5).ToString().PadRight(5, ' ')
//    //            + " < Ref 30  candle mins " + sig.MinRef30Min.Rnd(5).ToString().PadRight(5, ' '));

//    //        continue;

//    //    }
//    //    else
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = false;
//    //    }
//    //}

//    //if (sig.IsBestTimeToBuyAtDayLowest == false)
//    //{
//    //    if (sig.PrChPercCurrAndRef1Day < -20M && sig.IsCloseToDayLow)
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = true;
//    //        logger.Info(sig.Symbol + "  " + StrTradeTime + " PrChPercCurrAndRef1Day " + sig.PrChPercCurrAndRef1Day.Rnd(5).ToString().PadRight(5, ' ')
//    //            + " < 20 ");
//    //        continue;
//    //    }
//    //    else
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = false;
//    //    }
//    //}
//    //if (sig.IsBestTimeToBuyAtDayLowest == false)
//    //{

//    //    if (sig.PrChPercCurrAndRef4Hour < -18M && sig.IsCloseToDayLow)
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = true;
//    //        logger.Info(sig.Symbol + "  " + StrTradeTime + " PrChPercCurrAndRef4Hour " + sig.PrChPercCurrAndRef4Hour.Rnd(5).ToString().PadRight(5, ' ')
//    //            + " < 18 ");
//    //        continue;
//    //    }
//    //    else
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = false;
//    //    }
//    //}
//    //if (sig.IsBestTimeToBuyAtDayLowest == false)
//    //{

//    //    if (sig.PrChPercCurrAndRef1Hour < -16M && sig.IsCloseToDayLow)
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = true;
//    //        logger.Info(sig.Symbol + "  " + StrTradeTime + " PrChPercCurrAndRef1Hour " + sig.PrChPercCurrAndRef1Hour.Rnd(5).ToString().PadRight(5, ' ')
//    //            + " < 16 ");
//    //        continue;
//    //    }
//    //    else
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = false;
//    //    }
//    //}
//    //if (sig.IsBestTimeToBuyAtDayLowest == false)
//    //{

//    //    if (sig.PrChPercCurrAndRef30Min < -14M && sig.IsCloseToDayLow)
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = true;
//    //        logger.Info(sig.Symbol + "  " + StrTradeTime + " PrChPercCurrAndRef30Min " + sig.PrChPercCurrAndRef30Min.Rnd(5).ToString().PadRight(5, ' ')
//    //            + " < 14 ");
//    //        continue;
//    //    }
//    //    else
//    //    {
//    //        sig.IsBestTimeToBuyAtDayLowest = false;
//    //    }

//    //}

//    //if (sig.CurrPr > sig.DayAveragePr)
//    //{
//    //    sig.IsBestTimeToBuyAtDayLowest = false;
//    //}

//    //private int GetTotalConsecutiveUpOrDown(List<SignalCandle> candleList, string direction)
//    //{
//    //    // var mintime = candleList.Min(x => x.CloseTime);
//    //    // var avgPriceOfCandles = candleList.Average(x => x.ClosePrice);

//    //    if (candleList == null || candleList.Count == 0) return 0;

//    //    int TotalConsecutiveChanges = 0;

//    //    candleList = candleList.OrderByDescending(x => x.CloseTime).ToList();

//    //    bool directionCondition = false;

//    //    for (int i = 0; i < candleList.Count - 1; i++)
//    //    {
//    //        if (direction == "up")
//    //        {
//    //            if (candleList[i].ClosePrice > candleList[i + 1].ClosePrice)
//    //            {
//    //                directionCondition = true;
//    //            }
//    //            else if (i + 2 < candleList.Count)
//    //            {

//    //                if (candleList[i].ClosePrice > candleList[i + 2].ClosePrice)
//    //                {
//    //                    directionCondition = true;
//    //                }
//    //                else if (i + 3 < candleList.Count)
//    //                {

//    //                    if (candleList[i].ClosePrice > candleList[i + 3].ClosePrice)
//    //                    {
//    //                        directionCondition = true;
//    //                    }
//    //                    else
//    //                    {
//    //                        directionCondition = false;
//    //                    }
//    //                }
//    //                else
//    //                {
//    //                    directionCondition = false;
//    //                }
//    //            }
//    //            else
//    //            {
//    //                directionCondition = false;
//    //            }
//    //        }
//    //        else
//    //        {
//    //            if (candleList[i].ClosePrice <= candleList[i + 1].ClosePrice)
//    //            {
//    //                directionCondition = true;
//    //            }
//    //            else if (i + 2 < candleList.Count)
//    //            {

//    //                if (candleList[i].ClosePrice <= candleList[i + 2].ClosePrice)
//    //                {
//    //                    directionCondition = true;
//    //                }
//    //                else if (i + 3 < candleList.Count)
//    //                {

//    //                    if (candleList[i].ClosePrice <= candleList[i + 3].ClosePrice)
//    //                    {
//    //                        directionCondition = true;
//    //                    }
//    //                    else
//    //                    {
//    //                        directionCondition = false;
//    //                    }
//    //                }
//    //                else
//    //                {
//    //                    directionCondition = false;
//    //                }
//    //            }
//    //            else
//    //            {
//    //                directionCondition = false;
//    //            }
//    //        }

//    //        if (directionCondition)
//    //            TotalConsecutiveChanges++;
//    //        else
//    //            break;
//    //    }

//    //    return TotalConsecutiveChanges;
//    //}
//}

////private bool IsCoinPriceNotGoingUp(Signal sig)
////{

////    var LastFive_OneMinCandles = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime).Take(5);

////    var IsOneMinOnDownTrend = LastFive_OneMinCandles.First().ClosePrice < LastFive_OneMinCandles.Last().ClosePrice;

////    var LastThree_FiveMinCandles = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime).Take(3);

////    var IsFiveMinOnDownTrend = LastThree_FiveMinCandles.First().ClosePrice < LastThree_FiveMinCandles.Last().ClosePrice;

////    var LastTwo_FifteenMinCandles = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

////    var IsFifteenMinOnDownTrend = LastTwo_FifteenMinCandles.First().ClosePrice < LastTwo_FifteenMinCandles.Last().ClosePrice;

////    // prices are going down. Dont buy till you see recovery
////    if (IsOneMinOnDownTrend|| IsFiveMinOnDownTrend|| IsFifteenMinOnDownTrend)
////    {
////        logger.Info(sig.OpenTime.ToString("dd-MMM HH:mm") +
////          " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') + " CrPr " + sig.CurrPr.Rnd(3).ToString().PadRight(10, ' ') +
////          "  Prices not going up. Wait till you see uptrend" +
////          " 1up < 2  ? " + sig.TotalConsecutive1MinUps.ToString().PadRight(7, ' ') + " " 
////          );


////        return true;
////    }
////    return false;
////}

//private void CreateScalpBuySignals()
//{
//    //foreach (var sig in MySignals)
//    //{
//    //    try
//    //    {

//    //        //if (sig.CurrPr <= 0)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = false;
//    //        //    continue;
//    //        //}

//    //        //var OneMinCandles = sig.Ref1MinCandles.OrderByDescending(x => x.CloseTime).Take(5);

//    //        //var IsOneMinOnUpTrend = OneMinCandles.First().ClosePrice >= OneMinCandles.Max(x => x.ClosePrice);

//    //        //var FiveMinCandles = sig.Ref5MinCandles.OrderByDescending(x => x.CloseTime).Take(4);

//    //        //var IsFiveMinOnUpTrend = FiveMinCandles.First().ClosePrice >= FiveMinCandles.Max(x => x.ClosePrice);

//    //        //var FifteenMinCandles = sig.Ref15MinCandles.OrderByDescending(x => x.CloseTime).Take(2);

//    //        //var IsFifteenMinOnUpTrend = FifteenMinCandles.First().ClosePrice >= FifteenMinCandles.Max(x => x.ClosePrice);

//    //        //// prices are going down. Dont buy till you see recovery
//    //        //if (IsOneMinOnUpTrend && IsFiveMinOnUpTrend && IsFifteenMinOnUpTrend)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;


//    //        //}
//    //        //if (sig.IsBestTimeToBuyAtDayLowest)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    sig.PrChPercCurrAndRef30Min=-70M; // setting is so low so that this gets preference to buy
//    //        //    sig.PrChPercCurrAndRef4Hour = -70M;
//    //        //    sig.PrChPercCurrAndRef1Hour = -70M;
//    //        //    sig.PrChPercCurrAndRef15Min = -70M;
//    //        //    continue;
//    //        //}

//    //        //if (sig.Ref4HourCandles == null || sig.Ref4HourCandles.Count < 6)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = false;
//    //        //    continue;
//    //        //}
//    //        //if (sig.Ref1HourCandles == null || sig.Ref1HourCandles.Count < 23)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = false;
//    //        //    continue;
//    //        //}
//    //        //if (sig.Ref30MinCandles == null || sig.Ref30MinCandles.Count < 17)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = false;
//    //        //    continue;
//    //        //}
//    //        //if (sig.Ref15MinCandles == null || sig.Ref15MinCandles.Count < 15)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = false;
//    //        //    continue;
//    //        //}
//    //        //if (sig.Ref5MinCandles == null || sig.Ref5MinCandles.Count < 11)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = false;
//    //        //    continue;
//    //        //}



//    //        //if (sig.PrChPercCurrAndRef4Hour < configr.ScalpFourHourDiffLessThan) //-4M
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}
//    //        //if (sig.PrChPercCurrAndRef1Hour < configr.ScalpOneHourDiffLessThan)//-4M
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}
//    //        //if (sig.PrChPercCurrAndRef30Min < configr.ScalpThirtyMinDiffLessThan)//-4M
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}
//    //        //if (sig.PrChPercCurrAndRef15Min < configr.ScalpFifteenMinDiffLessThan)//-3M
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}
//    //        //if (sig.PrChPercCurrAndRef5Min < configr.ScalpFiveMinDiffLessThan) //-3M
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}

//    //        //if (sig.PrDiffCurrAndHighPerc >= -2M)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = false;
//    //        //    continue;
//    //        //}

//    //        //if (sig.PrDiffHighAndLowPerc <= 3M)
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = false;
//    //        //    continue;
//    //        //}

//    //        ////if (sig.CurrPr >= ((sig.DayHighPr + sig.DayAveragePr) / configr.DivideHighAndAverageBy))
//    //        ////{
//    //        ////    sig.IsBestTimeToScalpBuy = false;
//    //        ////    continue;
//    //        ////}

//    //        //if (sig.TotalConsecutive4HourDowns >= configr.ScalpFourHourDownMoreThan) //3
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}

//    //        //if (sig.TotalConsecutive1HourDowns >= configr.ScalpOneHourDownMoreThan) //4
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}

//    //        //if (sig.TotalConsecutive30MinDowns >= configr.ScalpThirtyMinDownMoreThan) //4
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}

//    //        //if (sig.TotalConsecutive15MinDowns >= configr.ScalpFifteenMinDownMoreThan) //4
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}

//    //        //if (sig.TotalConsecutive5MinDowns >= configr.ScalpFiveMinDownMoreThan) //5
//    //        //{
//    //        //    sig.IsBestTimeToScalpBuy = true;
//    //        //    continue;
//    //        //}

//    //        sig.IsBestTimeToScalpBuy = false;
//    //    }
//    //    catch (Exception ex)
//    //    {

//    //        logger.Info("Exception at scalp buy signal generators " + sig.Symbol + " " + ex.Message);
//    //        throw;
//    //    }
//    //}
//}

//        #endregion




//    }
//}

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




//  var watch = new Stopwatch();
//  watch.Start();
//   watch.Stop();
// logger.Info("Total EnsureAllSocketsRunning calculation time " + watch.ElapsedMilliseconds);
// watch.Restart();











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


//private async Task UpdateCoins()
//{

//    await GetMyCoins();
//    using (var db = new DB())
//    {
//        foreach (var coin in myCoins.Where(x => x.IsIncludedForTrading == true))
//        {
//            var sig = MySignals.Where(x => x.Symbol == coin.Pair).FirstOrDefault();
//            coin.DayTradeCount = sig.DayTradeCount;
//            db.Update(coin);
//        }
//        await db.SaveChangesAsync();
//    }
//    await GetMyCoins();
//}


//public async Task UpdateCoinsForTrading()
//{
//    List<Signal> signals = new List<Signal>();
//    exchangeInfo = await client.GetExchangeInfo();

//    foreach (var symbol in exchangeInfo.Symbols)
//    {
//        if (symbol.Symbol.EndsWith("USDT"))
//        {
//            if (symbol.Symbol.EndsWith("UPUSDT") || symbol.Symbol.EndsWith("DOWNUSDT") ||
//                symbol.Symbol.EndsWith("BULLUSDT") || symbol.Symbol.EndsWith("BEARUSDT") || symbol.Symbol == "BUSDUSDT" ||
//                symbol.Symbol == "USDCUSDT" || symbol.Symbol == "EURUSDT" || symbol.Symbol == "DAIUSDT"
//                )
//            {
//                continue;
//            }
//            var pricechangeresponse = await client.GetDailyTicker(symbol.Symbol);
//            Signal signal = new Signal();
//            signal.Symbol = symbol.Symbol;
//            signal.DayTradeCount = pricechangeresponse.TradeCount;
//            signals.Add(signal);
//        }
//    }

//    signals = signals.OrderByDescending(x => x.DayTradeCount).ToList();

//    using (var db = new DB())
//    {
//        List<string> coins = db.MyCoins.Select(x => x.Pair).ToList();
//        List<string> playercoins = db.Player.Select(x => x.Pair).ToList();
//        //  List<string> playerQAcoins = db.PlayerQA.Select(x => x.Pair).ToList();

//        foreach (var sig in signals)
//        {
//            // heavily traded but not in coin list. Add

//            //if (sig.DayTradeCount > configr.MinAllowedTradeCount)
//            //{
//            if (!coins.Contains(sig.Symbol))
//            {
//                MyCoins coin = new MyCoins();
//                coin.Pair = sig.Symbol;
//                coin.IsIncludedForTrading = true;
//                coin.TradePrecision = 0;
//                coin.PercAboveDayLowToSell = 13;
//                coin.PercBelowDayHighToBuy = -13;
//                await db.MyCoins.AddAsync(coin);
//                logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin Added");
//            }
//            else
//            {
//                //logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin existing");
//            }
//            // }
//            //else
//            //{
//            //    if (!playercoins.Contains(sig.Symbol)) // && !playerQAcoins.Contains(sig.Symbol)
//            //    {
//            //        var coin = await db.MyCoins.Where(x => x.Coin == sig.Symbol).FirstOrDefaultAsync();

//            //        if (coin != null)
//            //        {
//            //            db.MyCoins.Remove(coin);

//            //            logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin Removed");
//            //        }
//            //    }
//            //    else
//            //    {
//            //        logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' ') + " Coin Being Traded. Cant remove");
//            //    }
//            //}

//            // low traded, but in playerlist. Dont delete for now
//            //low traded and not in playerlist. Delete

//            // logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(11, ' '));
//        }
//        await db.SaveChangesAsync();
//    }
//}

