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
        
        
        //BinanceClient client;
        // ILog logger;
        // DB db;
        //public DispatcherTimer timer;
        //int intervalminutes = 5;

        public BalanceView()
        {
            InitializeComponent();

            //timer = new DispatcherTimer();
            //timer.Tick += new EventHandler(timer_Tick);
            //timer.Interval = new TimeSpan(0, intervalminutes, 0);
            //timer.Start();

            //db = new DB();

            //logger = LogManager.GetLogger(typeof(MainWindow));
            //var api = db.API.FirstOrDefault();
            //client = new BinanceClient(new ClientConfiguration()
            //{
            //    ApiKey = api.key,SecretKey = api.secret,Logger = logger
            //});
            
            //SetGrid();
            //CalculateBalanceSummary();
        }



        //private async void timer_Tick(object sender, EventArgs e)
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

        //private void SetGrid()
        //{
        //    db = new DB();
        //    BalanceDG.ItemsSource =  db.Balance.AsNoTracking().OrderByDescending(x => x.DifferencePercentage).ToList();
        //}

        //private async void btnUpdateBalance_Click(object sender, RoutedEventArgs e)
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

        //private async Task<bool> UpdateBalance()
        //{
        //    try
        //    {
        //        db=new DB();
        //        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Balance");
        //        var prices = await client.GetAllPrices();
        //        var accinfo = await client.GetAccountInformation();
        //        var trades = await db.MyTrade.ToListAsync();

        //        foreach (var asset in accinfo.Balances)
        //        {
        //            try
        //            {
        //                if (asset.Free > 0)
        //                {
        //                    var bal = new Balance
        //                    {
        //                        Asset = asset.Asset,
        //                        Free = asset.Free,
        //                        Locked = asset.Locked
        //                    };

        //                    if (asset.Asset.ToUpper() == "BUSD" || asset.Asset.ToUpper() == "USDT")
        //                    {
        //                        bal.CurrentPrice = asset.Free;
        //                        bal.BoughtPrice = asset.Free;
        //                        await db.Balance.AddAsync(bal);
        //                        continue;
        //                    }

        //                    foreach (var price in prices)
        //                    {
        //                        try
        //                        {
        //                            if (price.Symbol.ToUpper() == asset.Asset.ToUpper() + "USDT")
        //                            {
        //                                bal.CurrentPrice = bal.Free * price.Price;
        //                            }
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            logger.Error("Exception while retrieving Price for Asset " + asset.Asset + " " + ex.Message);
        //                        }
        //                    }

        //                    decimal spentprice = 0;

        //                    foreach (var trade in trades)
        //                    {
        //                        if (trade.Pair.ToUpper().Contains(asset.Asset.ToUpper()))
        //                        {
        //                            if (trade.IsBuyer)
        //                            {
        //                                spentprice += (trade.Price * trade.Quantity) + (trade.Price * trade.Quantity) * 0.075M / 100;
        //                            }
        //                            else
        //                            {
        //                                spentprice -= (trade.Price * trade.Quantity) + (trade.Price * trade.Quantity) * 0.075M / 100;
        //                            }
        //                        }
        //                    }
        //                    bal.BoughtPrice = spentprice;
        //                    bal.AverageBuyingCoinPrice = bal.BoughtPrice / bal.Free;
        //                    bal.CurrentCoinPrice = bal.CurrentPrice / bal.Free;
        //                    bal.Difference = bal.CurrentPrice - bal.BoughtPrice;
        //                    bal.DifferencePercentage = (bal.CurrentPrice - bal.BoughtPrice) / ((bal.CurrentPrice + bal.BoughtPrice) / 2) * 100;

        //                    await db.Balance.AddAsync(bal);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                logger.Error("Exception while retrieving balances " + ex.Message);
        //            }
        //        }

        //       await db.SaveChangesAsync();

        //        SetGrid();
        //       CalculateBalanceSummary();
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error($"Exception at Updating Balance {ex.Message}");
        //    }
        //    return true;
        //}

        ////public void DetachAllEntities()
        ////{
        ////    var changedEntriesCopy = db.ChangeTracker.Entries()
        ////        .Where(e => e.State == EntityState.Added ||
        ////                    e.State == EntityState.Modified ||
        ////                    e.State == EntityState.Deleted)
        ////        .ToList();

        ////    foreach (var entry in changedEntriesCopy)
        ////        entry.State = EntityState.Detached;
        ////}

        //private  void CalculateBalanceSummary()
        //{
        //    try
        //    {
        //        db=new DB();
        //        decimal totalinvested = 0;
        //        decimal totalcurrent = 0;
        //        decimal totaldifference = 0;
        //        decimal totaldifferenceinpercentage = 0;

        //        var balances =  db.Balance.AsNoTracking().ToList();
        //        if (balances != null && balances.Count > 0)
        //        {
        //            foreach (var balance in balances)
        //            {
        //                totalinvested += balance.BoughtPrice;
        //                totalcurrent += balance.CurrentPrice;
        //            }
        //            totaldifference = totalcurrent - totalinvested;
        //            totaldifferenceinpercentage = (totaldifference / ((totalinvested + totalcurrent) / 2)) * 100;
        //        }
        //        lblInvested.Text = "Invested:   " + String.Format("{0:0.00}", totalinvested);
        //        lblCurrentValue.Text = "Current Value:   " + String.Format("{0:0.00}", totalcurrent);
        //        lblDifference.Text = "Difference:   " + String.Format("{0:0.00}", totaldifference);
        //        lblDifferencePercentage.Text = "Difference %:   " + String.Format("{0:0.00}", totaldifferenceinpercentage);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error("Exception at setting Summary " + ex.Message);
        //    }
           
        //}

        //private static async Task<string> Execute()
        //{
        //    //Provide your configuration and keys here, this allows the client to function as expected.
        //    string apiKey = "YOUR_API_KEY";
        //    string secretKey = "YOUR_SECRET_KEY";

        //    //Building a test logger
        //    var logger = LogManager.GetLogger(typeof(MainWindow));
        //    logger.Debug("Logging Test");

        //    logger.Debug("--------------------------");
        //    logger.Debug("BinanceExchange API - Tester");
        //    logger.Debug("--------------------------");

        //    //Initialise the general client client with config
        //    var client = new BinanceClient(new ClientConfiguration()
        //    {
        //        ApiKey = apiKey,
        //        SecretKey = secretKey,
        //        Logger = logger,
        //    });

        //    logger.Debug("Interacting with Binance...");

        //    bool DEBUG_ALL = false;

        //    /*
        //     *  Code Examples - Make sure you adjust value of DEBUG_ALL
        //     */
        //    if (DEBUG_ALL)
        //    {
        //        // Test the Client
        //        await client.TestConnectivity();

        //        // Get All Orders
        //        var allOrdersRequest = new AllOrdersRequest()
        //        {
        //            Symbol = "ETHBTC",
        //            Limit = 5,
        //        };

        //        allOrdersRequest = new AllOrdersRequest()
        //        {
        //            Symbol = TradingPairSymbols.BTCPairs.ETH_BTC,
        //            Limit = 5,
        //        };
        //        // Get All Orders
        //        var allOrders = await client.GetAllOrders(allOrdersRequest);

        //        // Get the order book, and use the cache
        //        var orderBook = await client.GetOrderBook("ETHBTC", true);

        //        // Cancel an order
        //        var cancelOrder = await client.CancelOrder(new CancelOrderRequest()
        //        {
        //            NewClientOrderId = "123456",
        //            OrderId = 523531,
        //            OriginalClientOrderId = "789",
        //            Symbol = "ETHBTC",
        //        });

        //        // Create an order with varying options
        //        var createOrder = await client.CreateOrder(new CreateOrderRequest()
        //        {
        //            IcebergQuantity = 100,
        //            Price = 230,
        //            Quantity = 0.6m,
        //            Side = OrderSide.Buy,
        //            Symbol = "ETHBTC",
        //            Type = OrderType.Market,
        //        });

        //        // Get account information
        //        var accountInformation = await client.GetAccountInformation(3500);

        //        // Get account trades
        //        var accountTrades = await client.GetAccountTrades(new AllTradesRequest()
        //        {
        //            FromId = 352262,
        //            Symbol = "ETHBTC",
        //        });

        //        // Get a list of Compressed aggregate trades with varying options
        //        var aggTrades = await client.GetCompressedAggregateTrades(new GetCompressedAggregateTradesRequest()
        //        {
        //            StartTime = DateTime.UtcNow.AddDays(-1),
        //            Symbol = "ETHBTC",
        //        });

        //        // Get current open orders for the specified symbol
        //        var currentOpenOrders = await client.GetCurrentOpenOrders(new CurrentOpenOrdersRequest()
        //        {
        //            Symbol = "ETHBTC",
        //        });

        //        // Get daily ticker
        //        var dailyTicker = await client.GetDailyTicker("ETHBTC");

        //        // Get Symbol Order Book Ticket
        //        var symbolOrderBookTicker = await client.GetSymbolOrderBookTicker();

        //        // Get Symbol Order Price Ticker
        //        var symbolOrderPriceTicker = await client.GetSymbolsPriceTicker();

        //        // Query a specific order on Binance
        //        var orderQuery = await client.QueryOrder(new QueryOrderRequest()
        //        {
        //            OrderId = 5425425,
        //            Symbol = "ETHBTC",
        //        });

        //        // Firing off a request and catching all the different exception types.
        //        //try
        //        //{
        //        //    accountTrades = await client.GetAccountTrades(new AllTradesRequest()
        //        //    {
        //        //        FromId = 352262,
        //        //        Symbol = "ETHBTC",
        //        //    });
        //        //}
        //        //catch (BinanceBadRequestException badRequestException)
        //        //{

        //        //}
        //        //catch (BinanceServerException serverException)
        //        //{

        //        //}
        //        //catch (BinanceTimeoutException timeoutException)
        //        //{

        //        //}
        //        //catch (BinanceException unknownException)
        //        //{

        //        //}
        //    }

        //    // Start User Data Stream, ping and close
        //    var userData = await client.StartUserDataStream();
        //    await client.KeepAliveUserDataStream(userData.ListenKey);
        //    await client.CloseUserDataStream(userData.ListenKey);

        //    // Manual WebSocket usage
        //    var manualBinanceWebSocket = new InstanceBinanceWebSocketClient(client);
        //    var socketId = manualBinanceWebSocket.ConnectToDepthWebSocket("ETHBTC", b =>
        //    {
        //        System.Console.Clear();
        //        logger.Debug($"{JsonConvert.SerializeObject(b.BidDepthDeltas, Formatting.Indented)}");
        //        System.Console.SetWindowPosition(0, 0);
        //    });


        //    #region Advanced Examples           
        //    // This builds a local Kline cache, with an initial call to the API and then continues to fill
        //    // the cache with data from the WebSocket connection. It is quite an advanced example as it provides 
        //    // additional options such as an Exit Func<T> or timeout, and checks in place for cache instances. 
        //    // You could provide additional logic here such as populating a database, ping off more messages, or simply
        //    // timing out a fill for the cache.
        //    var dict = new Dictionary<string, KlineCacheObject>();
        //    //await BuildAndUpdateLocalKlineCache(client, "BNBBTC", KlineInterval.OneMinute,
        //    //    new GetKlinesCandlesticksRequest()
        //    //    {
        //    //        StartTime = DateTime.UtcNow.AddHours(-1),
        //    //        EndTime = DateTime.UtcNow,
        //    //        Interval = KlineInterval.OneMinute,
        //    //        Symbol = "BNBBTC"
        //    //    }, new WebSocketConnectionFunc(15000), dict);

        //    // This builds a local depth cache from an initial call to the API and then continues to fill 
        //    // the cache with data from the WebSocket
        //    var localDepthCache = await BuildLocalDepthCache(client);
        //    // Build the Buy Sell volume from the results
        //    var volume = ResultTransformations.CalculateTradeVolumeFromDepth("BNBBTC", localDepthCache);

        //    #endregion

        //    logger.Debug("Complete.");
        //    Thread.Sleep(6000);
        //    manualBinanceWebSocket.CloseWebSocketInstance(socketId);
        //    System.Console.ReadLine();

        //    return "";
        //}

        ///// <summary>
        ///// Build local Depth cache from WebSocket and API Call example.
        ///// </summary>
        ///// <param name="client"></param>
        ///// <returns></returns>
        //private static async Task<Dictionary<string, DepthCacheObject>> BuildLocalDepthCache(IBinanceClient client)
        //{
        //    // Code example of building out a Dictionary local cache for a symbol using deltas from the WebSocket
        //    var localDepthCache = new Dictionary<string, DepthCacheObject> {{ "BNBBTC", new DepthCacheObject()
        //    {
        //        Asks = new Dictionary<decimal, decimal>(),
        //        Bids = new Dictionary<decimal, decimal>(),
        //    }}};
        //    var bnbBtcDepthCache = localDepthCache["BNBBTC"];

        //    // Get Order Book, and use Cache
        //    var depthResults = await client.GetOrderBook("BNBBTC", true, 100);
        //    //Populate our depth cache
        //    depthResults.Asks.ForEach(a =>
        //    {
        //        if (a.Quantity != 0.00000000M)
        //        {
        //            bnbBtcDepthCache.Asks.Add(a.Price, a.Quantity);
        //        }
        //    });
        //    depthResults.Bids.ForEach(a =>
        //    {
        //        if (a.Quantity != 0.00000000M)
        //        {
        //            bnbBtcDepthCache.Bids.Add(a.Price, a.Quantity);
        //        }
        //    });

        //    // Store the last update from our result set;
        //    long lastUpdateId = depthResults.LastUpdateId;
        //    using (var binanceWebSocketClient = new DisposableBinanceWebSocketClient(client))
        //    {
        //        binanceWebSocketClient.ConnectToDepthWebSocket("BNBBTC", data =>
        //        {
        //            if (lastUpdateId < data.UpdateId)
        //            {
        //                data.BidDepthDeltas.ForEach((bd) =>
        //                {
        //                    CorrectlyUpdateDepthCache(bd, bnbBtcDepthCache.Bids);
        //                });
        //                data.AskDepthDeltas.ForEach((ad) =>
        //                {
        //                    CorrectlyUpdateDepthCache(ad, bnbBtcDepthCache.Asks);
        //                });
        //            }
        //            lastUpdateId = data.UpdateId;
        //            System.Console.Clear();
        //            System.Console.WriteLine($"{JsonConvert.SerializeObject(bnbBtcDepthCache, Formatting.Indented)}");
        //            System.Console.SetWindowPosition(0, 0);
        //        });

        //        Thread.Sleep(8000);
        //    }
        //    return localDepthCache;
        //}

        ///// <summary>
        ///// Advanced approach to building local Kline Cache from WebSocket and API Call example (refactored)
        ///// </summary>
        ///// <param name="binanceClient">The BinanceClient instance</param>
        ///// <param name="symbol">The Symbol to request</param>
        ///// <param name="interval">The interval for Klines</param>
        ///// <param name="klinesCandlesticksRequest">The initial request for Klines</param>
        ///// <param name="webSocketConnectionFunc">The function to determine exiting the websocket (can be timeout or Func based on external params)</param>
        ///// <param name="cacheObject">The cache object. Must always be provided, and can exist with data.</param>
        ///// <returns></returns>
        //public static async Task BuildAndUpdateLocalKlineCache(IBinanceClient binanceClient,
        //    string symbol,
        //    KlineInterval interval,
        //    GetKlinesCandlesticksRequest klinesCandlesticksRequest,
        //    WebSocketConnectionFunc webSocketConnectionFunc,
        //    Dictionary<string, KlineCacheObject> cacheObject)
        //{
        //    Guard.AgainstNullOrEmpty(symbol);
        //    Guard.AgainstNull(webSocketConnectionFunc);
        //    Guard.AgainstNull(klinesCandlesticksRequest);
        //    Guard.AgainstNull(cacheObject);

        //    long epochTicks = new DateTime(1970, 1, 1).Ticks;

        //    if (cacheObject.ContainsKey(symbol))
        //    {
        //        if (cacheObject[symbol].KlineInterDictionary.ContainsKey(interval))
        //        {
        //            throw new Exception(
        //                "Symbol and Interval pairing already provided, please use a different interval/symbol or pair.");
        //        }
        //        cacheObject[symbol].KlineInterDictionary.Add(interval, new KlineIntervalCacheObject());
        //    }
        //    else
        //    {
        //        var klineCacheObject = new KlineCacheObject
        //        {
        //            KlineInterDictionary = new Dictionary<KlineInterval, KlineIntervalCacheObject>()
        //        };
        //        cacheObject.Add(symbol, klineCacheObject);
        //        cacheObject[symbol].KlineInterDictionary.Add(interval, new KlineIntervalCacheObject());
        //    }

        //    // Get Kline Results, and use Cache
        //    long ticks = klinesCandlesticksRequest.StartTime.Value.Ticks;
        //    var startTimeKeyTime = (ticks - epochTicks) / TimeSpan.TicksPerSecond;
        //    var klineResults = await binanceClient.GetKlinesCandlesticks(klinesCandlesticksRequest);

        //    var oneMinKlineCache = cacheObject[symbol].KlineInterDictionary[interval];
        //    oneMinKlineCache.TimeKlineDictionary = new Dictionary<long, KlineCandleStick>();
        //    var instanceKlineCache = oneMinKlineCache.TimeKlineDictionary;
        //    //Populate our kline cache with initial results
        //    klineResults.ForEach(k =>
        //    {
        //        instanceKlineCache.Add(((k.OpenTime.Ticks - epochTicks) / TimeSpan.TicksPerSecond), new KlineCandleStick()
        //        {
        //            Close = k.Close,
        //            High = k.High,
        //            Low = k.Low,
        //            Open = k.Open,
        //            Volume = k.Volume,
        //        });
        //    });

        //    // Store the last update from our result set;
        //    using (var binanceWebSocketClient = new DisposableBinanceWebSocketClient(binanceClient))
        //    {
        //        binanceWebSocketClient.ConnectToKlineWebSocket(symbol, interval, data =>
        //        {
        //            var keyTime = (data.Kline.StartTime.Ticks - epochTicks) / TimeSpan.TicksPerSecond;
        //            var klineObj = new KlineCandleStick()
        //            {
        //                Close = data.Kline.Close,
        //                High = data.Kline.High,
        //                Low = data.Kline.Low,
        //                Open = data.Kline.Open,
        //                Volume = data.Kline.Volume,
        //            };
        //            if (!data.Kline.IsBarFinal)
        //            {
        //                if (keyTime < startTimeKeyTime)
        //                {
        //                    return;
        //                }

        //                TryAddUpdateKlineCache(instanceKlineCache, keyTime, klineObj);
        //            }
        //            else
        //            {
        //                TryAddUpdateKlineCache(instanceKlineCache, keyTime, klineObj);
        //            }
        //            System.Console.Clear();
        //            System.Console.WriteLine($"{JsonConvert.SerializeObject(instanceKlineCache, Formatting.Indented)}");
        //            System.Console.SetWindowPosition(0, 0);
        //        });
        //        if (webSocketConnectionFunc.IsTimout)
        //        {
        //            Thread.Sleep(webSocketConnectionFunc.Timeout);
        //        }
        //        else
        //        {
        //            while (true)
        //            {
        //                if (!webSocketConnectionFunc.ExitFunction())
        //                {
        //                    // Throttle Application
        //                    Thread.Sleep(100);
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}


        //private static void TryAddUpdateKlineCache(Dictionary<long, KlineCandleStick> primary, long keyTime, KlineCandleStick klineObj)
        //{
        //    if (primary.ContainsKey(keyTime))
        //    {
        //        primary[keyTime] = klineObj;
        //    }
        //    else
        //    {
        //        primary.Add(keyTime, klineObj);
        //    }
        //}

        //private static void CorrectlyUpdateDepthCache(TradeResponse bd, Dictionary<decimal, decimal> depthCache)
        //{
        //    const decimal defaultIgnoreValue = 0.00000000M;

        //    if (depthCache.ContainsKey(bd.Price))
        //    {
        //        if (bd.Quantity == defaultIgnoreValue)
        //        {
        //            depthCache.Remove(bd.Price);
        //        }
        //        else
        //        {
        //            depthCache[bd.Price] = bd.Quantity;
        //        }
        //    }
        //    else
        //    {
        //        if (bd.Quantity != defaultIgnoreValue)
        //        {
        //            depthCache[bd.Price] = bd.Quantity;
        //        }
        //    }
        //}
    }

}
