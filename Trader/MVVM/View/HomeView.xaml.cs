
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
        public List<string> CoinList { get; set; }
        public List<string> MyTradedCoinList { get; set; }
        public DispatcherTimer timer;
        BinanceClient client;
        ILog logger;
        DB db;
        int intervalminutes = 5;

        public HomeView()
        {
            InitializeComponent();
            db = new DB();
            Startup();
        }

        private async void Startup()
        {
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, intervalminutes, 0);
            timer.Start();
            logger = LogManager.GetLogger(typeof(MainWindow));
            db = new DB();
            var api = db.API.FirstOrDefault();
            client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = api.key,
                SecretKey = api.secret,
                Logger = logger,
            });
            CoinList = new MyCoins().GetMyCoins();
            MyTradedCoinList = new MyTradedCoins().GetMyTradedCoins();
            await GetCandles();
        }


        private async Task GetPrices()
        {
            var prices = await client.GetAllPrices();
            foreach (var price in prices)
            {
                try
                {
                    var pr = new Price
                    {
                        date = DateTime.Now,
                        pair = price.Symbol,
                        price = price.Price
                    };
                    await db.Price.AddAsync(pr);
                }
                catch (Exception ex)
                {
                    logger.Error("Exception at Get Prices  " + ex.Message);
                }
            }
            await db.SaveChangesAsync();
        }

        private async Task GetMyTrades()
        {

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
                        await db.MyTrade.AddAsync(mytrade);
                    }
                }
                catch (Exception ex)
                {

                    logger.Error("Error while retrieving price ticker for " + coin + " " + ex.Message);
                }
            }
            await db.SaveChangesAsync();
        }

        private async void timer_Tick(object sender, EventArgs e)
        {
            await GetCandles();
        }

        private async Task GetCandles()
        {
            var prices = await client.GetAllPrices();

            foreach (var price in prices)
            {
                if (price.Symbol.Contains("BUSD") && !price.Symbol.Contains("USDT"))
                {
                    try
                    {
                        var pr = new Price
                        {
                            date = DateTime.Now,
                            pair = price.Symbol,
                            price = price.Price
                        };
                       
                        GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
                        Candle candle = new Candle();
                        cr.Limit = 1;
                        cr.Symbol = price.Symbol;
                        cr.Interval = KlineInterval.FiveMinutes;
                        var candleresponse = await client.GetKlinesCandlesticks(cr);
                        var pricechangeresponse = await client.GetDailyTicker(price.Symbol);
                        candle.RecordedTime = candleresponse[0].OpenTime;
                        candle.Symbol = price.Symbol;
                        candle.Open = candleresponse[0].Open;
                        candle.OpenTime = candleresponse[0].OpenTime;
                        candle.High = candleresponse[0].High;
                        candle.Low = candleresponse[0].Low;
                        candle.Close = candleresponse[0].Close;
                        candle.Volume = candleresponse[0].Volume;
                        candle.CloseTime = candleresponse[0].CloseTime;
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
                        await db.Price.AddAsync(pr);
                        await db.Candle.AddAsync(candle);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Error while retrieving prices and Candles ticker for " + price.Symbol + " " + ex.Message);
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        private async void btnTrade_Click(object sender, RoutedEventArgs e)
        {
           await GetCandles();
        }

    }
}
