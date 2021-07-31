
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

        public static decimal GetDiffPerc(this decimal newValue, decimal OldValue)
        {
            if (OldValue == 0) return 0M;
            return ((newValue - OldValue) / (OldValue)) * 100;
        }

        public static decimal GetDiffPerc(this int newValue, int OldValue)
        {
            if (OldValue == 0) return 0M;
            return ((newValue - OldValue) / (OldValue)) * 100;
        }

        public static decimal? GetDiffPerc(this decimal? newValue, decimal? OldValue)
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
        IMapper iCandleMapper;
        List<string> boughtCoins = new List<string>();
        List<Signal> CurrentSignals = new List<Signal>();
        public Config configr = new Config();
        public bool ForceSell = false;

        public ExchangeInfoResponse exchangeInfo = new ExchangeInfoResponse();

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
            TradeTimer.Interval = new TimeSpan(0, configr.IntervalMinutes, 0);
           // TradeTimer.Interval = new TimeSpan(0, 0, 3);
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

            var candleMapConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Candle, CandleBackUp>();
            });

            iPlaymerMapper = playerMapConfig.CreateMapper();
            iCandleMapper = candleMapConfig.CreateMapper();

            MyCoins = await db.MyCoins.AsNoTracking().ToListAsync();
            await SetGrid();

            // await GetAllUSDTPairs();
            await UpdateAllowedPrecisionsForPairs();
            await RemoveOldCandles();

         //   GetSignalsNew();
            TradeTimer.Start();
            logger.Info("Application Started and Timer Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            logger.Info("");
        }

        private async void btnTrade_Click(object sender, RoutedEventArgs e)
        {
            await Trade();
        }

        private async void TraderTimer_Tick(object sender, EventArgs e)
        {
            await Trade();
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
                var prDiffPerc = player.TotalCurrentValue.GetDiffPerc(player.TotalBuyCost);
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

        private async Task<List<Candle>> GetCandle()
        {
            // logger.Info("  Getting Candle Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            DB candledb = new DB();
            List<Candle> candles = new List<Candle>();
            Candle candle = new Candle();
            try
            {
                foreach (var coin in MyCoins)
                {
                    var pair = coin.Coin;

                    candle = new Candle();
                    var pricechangeresponse = await client.GetDailyTicker(pair);

                    candle.RecordedTime = DateTime.Now;
                    candle.Symbol = pair;

                    //I dont use these, so setting to zero
                    candle.Open = 0;
                    candle.High = 0;
                    candle.Low = 0;
                    candle.Close = 0;
                    candle.Volume = 0;
                    candle.QuoteAssetVolume = 0;
                    candle.NumberOfTrades = 0;
                    candle.TakerBuyBaseAssetVolume = 0;
                    candle.TakerBuyQuoteAssetVolume = 0;


                    candle.OpenTime = pricechangeresponse.OpenTime;
                    candle.CloseTime = pricechangeresponse.CloseTime;
                    candle.Change = pricechangeresponse.PriceChange;
                    candle.PriceChangePercent = pricechangeresponse.PriceChangePercent;
                    candle.WeightedAveragePercent = pricechangeresponse.WeightedAveragePercent;
                    candle.PreviousClosePrice = pricechangeresponse.PreviousClosePrice;
                    candle.CurrentPrice = pricechangeresponse.LastPrice;
                    candle.OpenPrice = pricechangeresponse.OpenPrice;
                    candle.DayHighPrice = pricechangeresponse.HighPrice;
                    candle.DayLowPrice = pricechangeresponse.LowPrice;
                    candle.DayVolume = pricechangeresponse.Volume;
                    candle.DayTradeCount = pricechangeresponse.TradeCount;

                    candles.Add(candle);

                    var isCandleExisting = await candledb.Candle.Where(x => x.OpenTime == candle.OpenTime && x.Symbol == candle.Symbol).FirstOrDefaultAsync();

                    if (isCandleExisting == null) await candledb.Candle.AddAsync(candle);
                    else candledb.Candle.Update(isCandleExisting);
                }

            }
            catch (Exception ex)
            {
                logger.Info("  Exception in Getting Candle  " + candle.Symbol + " " + ex.Message);
                throw;
            }

            await candledb.SaveChangesAsync();
            //    logger.Info("  Getting Candle Completed at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            return candles;
        }

        private async Task<List<Signal>> GetSignals()
        {
            DB db = new DB();
            CurrentSignals = new List<Signal>();
            List<Candle> candles = new List<Candle>();

            candles = await GetCandle();

            foreach (var coin in MyCoins)
            {
                var pair = coin.Coin;
                var selCndl = candles.Where(x => x.Symbol == pair).FirstOrDefault();
                if (selCndl == null) continue;

                Signal sig = new Signal();
                sig.Symbol = pair;

                sig.CurrPr = selCndl.CurrentPrice;
                sig.DayHighPr = selCndl.DayHighPrice;
                sig.DayLowPr = selCndl.DayLowPrice;
                sig.CandleOpenTime = selCndl.OpenTime;
                sig.CandleCloseTime = selCndl.CloseTime;
                sig.CandleId = selCndl.Id;
                sig.DayVol = selCndl.DayVolume;
                sig.DayTradeCount = selCndl.DayTradeCount;

                sig.DayPrDiffPercentage = sig.DayHighPr.GetDiffPerc(sig.DayLowPr);
                sig.PrDiffCurrAndLowPerc = Math.Abs(sig.DayLowPr.GetDiffPerc(sig.CurrPr));

                sig.PrDiffCurrAndHighPerc = sig.CurrPr.GetDiffPerc(sig.DayHighPr);


                var dayAveragePrice = (sig.DayHighPr + sig.DayLowPr) / 2;

                dayAveragePrice = dayAveragePrice - (dayAveragePrice * 1.5M / 100);

                // dayAveragePrice = dayAveragePrice - (dayAveragePrice * 1M / 100);

                sig.IsCloseToDayLow = sig.CurrPr < dayAveragePrice;

                //new Selection Logic

                var preCandleTimes = selCndl.CloseTime.AddMinutes(-25);

                var previouscandles =
                    await db.Candle.AsNoTracking().Where(x => x.Symbol == sig.Symbol &&
                    x.CloseTime >= preCandleTimes && x.CloseTime < sig.CandleCloseTime).OrderBy(x => x.CloseTime).ToListAsync();


                for (int i = 0; i < previouscandles.Count - 1; i++)
                {
                    if (previouscandles[i + 1].CurrentPrice > previouscandles[i].CurrentPrice)
                    {
                        sig.TotalPreviousUps += 1;
                    }
                    else
                    {
                        sig.TotalPreviousDowns += 1;
                    }
                }
                sig.PriceChangeInLastHour = sig.CurrPr.GetDiffPerc(previouscandles[0].CurrentPrice);

                if ((sig.PriceChangeInLastHour < -1.2M && sig.CurrPr <= (sig.DayHighPr+dayAveragePrice)/2) || sig.IsCloseToDayLow) //sig.TotalPreviousDowns > sig.TotalPreviousUps &&
                    sig.IsBestTimeToBuy = true;

                CurrentSignals.Add(sig);
            }

            CurrentSignals = CurrentSignals.OrderBy(x => x.PrDiffCurrAndHighPerc).ToList();

            foreach (var sig in CurrentSignals)
            {
                LogSignal(sig);
            }
            return CurrentSignals;
        }

        private void GetSignalsNew()
        {

            CurrentSignals = new List<Signal>();
            var manualBinanceWebSocket = new InstanceBinanceWebSocketClient(client);

            foreach (var coin in MyCoins.Take(2))
            {
                var sig = CurrentSignals.Where(x => x.Symbol == coin.Coin).FirstOrDefault();
                logger.Info("Starting symboles for  " + coin.Coin);
                if (sig == null)
                {
                    sig = new Signal();
                    sig.Symbol = coin.Coin;
                    CurrentSignals.Add(sig);
                }

                try
                {
                    manualBinanceWebSocket.ConnectToIndividualSymbolTickerWebSocket(coin.Coin, b =>
                            {
                                sig = CurrentSignals.Where(x => x.Symbol == coin.Coin).FirstOrDefault();
                                sig.CurrPr = b.LastPrice;
                                sig.CandleOpenTime = DateTime.Now;
                                sig.CandleCloseTime = DateTime.Now;
                                sig.CandleId = 0;

                            });
                    logger.Info("Crossed ConnectToIndividualSymbolTickerWebSocket for " + coin.Coin);
                }
                catch (Exception ex)
                {

                    logger.Info("exception for ConnectToIndividualSymbolTickerWebSocket for " + coin.Coin + " " + ex.Message);
                }

                try
                {
                    manualBinanceWebSocket.ConnectToKlineWebSocket(coin.Coin, KlineInterval.OneHour, b =>
                           {
                               sig = CurrentSignals.Where(x => x.Symbol == coin.Coin).FirstOrDefault();
                               sig.Symbol = b.Symbol;
                               sig.RefHighPr = b.Kline.High;
                               sig.RefLowPr = b.Kline.Low;
                               sig.RefTradeCount = b.Kline.NumberOfTrades;
                               sig.RefVol = b.Kline.Volume;
                               sig.CandleOpenTime = DateTime.Now;
                               sig.CandleCloseTime = DateTime.Now;
                           });

                    logger.Info("Crossed ConnectToKlineWebSocket one hour for " + coin.Coin);
                }
                catch (Exception ex)
                {

                    logger.Info("exception for ConnectToKlineWebSocket one hour  for " + coin.Coin + " " + ex.Message);
                }
                try
                {
                    manualBinanceWebSocket.ConnectToKlineWebSocket(coin.Coin, KlineInterval.OneDay, b =>
                            {
                                sig = CurrentSignals.Where(x => x.Symbol == coin.Coin).FirstOrDefault();

                                sig.Symbol = b.Symbol;
                                sig.DayHighPr = b.Kline.High;
                                sig.DayLowPr = b.Kline.Low;
                                sig.DayVol = b.Kline.Volume;
                                sig.DayTradeCount = b.Kline.NumberOfTrades;

                                sig.DayPrDiffPercentage = sig.DayHighPr.GetDiffPerc(sig.DayLowPr);
                                sig.PrDiffCurrAndLowPerc = Math.Abs(sig.DayLowPr.GetDiffPerc(sig.CurrPr));

                                sig.PrDiffCurrAndHighPerc = sig.CurrPr.GetDiffPerc(sig.DayHighPr);

                                var dayAveragePrice = (sig.DayHighPr + sig.DayLowPr) / 2;
                                dayAveragePrice = dayAveragePrice - (dayAveragePrice * 1.5M / 100);

                                sig.IsCloseToDayLow = sig.CurrPr < dayAveragePrice;

                                sig.PriceChangeInLastHour = sig.CurrPr.GetDiffPerc(sig.RefLowPr);

                                if ((sig.PriceChangeInLastHour < -2M) || sig.IsCloseToDayLow) //sig.TotalPreviousDowns > sig.TotalPreviousUps &&
                                    sig.IsBestTimeToBuy = true;
                            });

                    logger.Info("Crossed ConnectToKlineWebSocket daily for " + coin.Coin);

                }
                catch (Exception ex)
                {

                    logger.Info("exception for ConnectToKlineWebSocket daily for " + coin.Coin + " " + ex.Message);
                }
                try
                {
                    manualBinanceWebSocket.ConnectToTradesWebSocket(coin.Coin, b =>
                          {
                              sig = CurrentSignals.Where(x => x.Symbol == coin.Coin).FirstOrDefault();
                              sig.LastTradePrice = b.Price;
                          });

                    logger.Info("Crossed ConnectToTradesWebSocket for " + coin.Coin);
                }
                catch (Exception ex)
                {

                    logger.Info("exception for ConnectToTradesWebSocket for " + coin.Coin + " " + ex.Message);
                }

            }
            logger.Info("Get signals new completed ");
        }

        public async Task RedistributeBalances()
        {
            DB db = new DB();
            var availplayers = await db.Player.Where(x => x.IsTrading == false).OrderBy(x => x.Id).ToListAsync();

            AccountInformationResponse accinfo = await client.GetAccountInformation();
            decimal TotalAvalUSDT = 0;

            var USDT = accinfo.Balances.Where(x => x.Asset == "USDT").FirstOrDefault();

            if (USDT != null)
            {
                TotalAvalUSDT = USDT.Free - (USDT.Free * 3 / 100); //Take only 98 % to cater for small differences
            }
            if (availplayers.Count() > 0)
            {
                var avgAvailAmountForTrading = TotalAvalUSDT / availplayers.Count();

                //if (avgAvailAmountForTrading > 99.9M)
                //{
                //    avgAvailAmountForTrading = 99.9M;
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
                var prDiffPerc = player.TotalSellAmount.GetDiffPerc(player.TotalBuyCost);
                player.AvailableAmountToBuy = 0;
                player.UpdatedTime = DateTime.Now;
                db.Player.Update(player);
                await db.SaveChangesAsync();
            }
        }

        private async Task BuyTheCoin(Player player, Signal sig)
        {
            DB db = new DB();
            decimal mybuyPrice = 0;

            LogBuy(player, sig);

            var PriceResponse = await client.GetPrice(sig.Symbol);
            mybuyPrice = PriceResponse.Price;

            //    mybuyPrice = mybuyPrice*99/100;
            // logger.Info("  Current price of coin " + sig.Symbol + " at  " + DateTime.Now.ToString("dd-MMM HH:mm:ss") + " is " + mybuyPrice);

            //  var mybuyPrice = latestPrice - (latestPrice * configr.BufferPriceForBuyAndSell / 100); // set  buy price to a tiny lesser than the current price.

            player.Pair = sig.Symbol;

            var coin = MyCoins.Where(x => x.Coin == sig.Symbol).FirstOrDefault();

            var coinprecison = coin.TradePrecision;

            var quantity = (player.AvailableAmountToBuy.Deci() / mybuyPrice).Rnd(coinprecison);

            //logger.Info(" Bought coin " + sig.Symbol + " at  " + DateTime.Now.ToString("dd-MMM HH:mm:ss") +
            //    " for price " + mybuyPrice + " Quantity = " + quantity +
            //    " and it costed " + player.AvailableAmountToBuy);

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
            player.isBuyCostAccurated = false;
            db.Player.Update(player);

            //Send Buy Order

            PlayerTrades playerHistory = iPlaymerMapper.Map<Player, PlayerTrades>(player);
            playerHistory.Id = 0;
            await db.PlayerTrades.AddAsync(playerHistory);
            await db.SaveChangesAsync();
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
                        if (orderResponse.Status != OrderStatus.Filled)
                        {
                            CancelOrderRequest cancelrequest = new CancelOrderRequest();
                            cancelrequest.OrderId = orderResponse.OrderId;
                            cancelrequest.Symbol = orderResponse.Symbol;
                            await client.CancelOrder(cancelrequest);
                            await ClearPlayer(player);
                        }
                        else
                        {
                            player.isBuyOrderCompleted = true;
                            player.isBuyCostAccurated = true;
                            db.Player.Update(player);
                            await db.SaveChangesAsync();
                        }
                    }

                }
            }

            //foreach (var player in players)
            //{
            //    decimal totalbuycost = 0;
            //    decimal commisionAmt = 0;
            //    decimal tradeprice = 0;
            //    decimal tradeQuantity = 0;
            //    if (!player.isBuyCostAccurated)
            //    {
            //        List<AccountTradeReponse> accountTrades = await client.GetAccountTrades(new AllTradesRequest()
            //        {
            //            Limit = 10,
            //            Symbol = player.Pair
            //        });

            //        foreach (var trade in accountTrades)
            //        {
            //            if (trade.OrderId != player.BuyOrderId)
            //            {
            //                continue;
            //            }
            //            if (trade.CommissionAsset == "BNB")
            //            {
            //                commisionAmt = trade.Commission * bnbprice.Price;
            //            }
            //            else
            //            {
            //                commisionAmt = trade.Commission;
            //            }

            //            tradeQuantity += trade.Quantity;
            //            totalbuycost += (trade.Price * trade.Quantity) + commisionAmt;
            //        }

            //        tradeprice = totalbuycost / tradeQuantity;

            //        logger.Info(
            //           " " + player.Name +
            //           " " + player.Pair.Replace("USDT", "") +
            //           " Buy Cost " + player.TotalBuyCost.Deci().Rnd(5) + " Update To " + totalbuycost.Rnd(5) +
            //           " Coin Price " + player.BuyCoinPrice.Deci().Rnd(5) + " Update To " + tradeprice.Rnd(5) +
            //           " Quanity " + player.Quantity.Deci().Rnd(5) + " Update To " + tradeQuantity.Rnd(5));

            //        player.BuyCoinPrice = tradeprice;
            //        player.Quantity = tradeQuantity;
            //        player.isBuyCostAccurated = true;
            //        player.TotalBuyCost = totalbuycost;
            //        db.Player.Update(player);
            //    }
            //}

            // await db.SaveChangesAsync();
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
                        if (orderResponse.Status != OrderStatus.Filled)
                        {
                            CancelOrderRequest cancelrequest = new CancelOrderRequest();
                            cancelrequest.OrderId = orderResponse.OrderId;
                            cancelrequest.Symbol = orderResponse.Symbol;
                            await client.CancelOrder(cancelrequest);
                            player.SellOrderId = 0;
                            db.Player.Update(player);
                            await db.SaveChangesAsync();
                        }
                        else
                        {
                            await UpdatePlayerAfterSellConfirmed(player);
                        }
                    }

                }
            }



            //DB db = new DB();

            //// it should be player trades

            //var players = await db.Player.Where(x => x.IsTrading == true).ToListAsync();
            //var bnbprice = await client.GetPrice("BNBUSDT");

            //foreach (var player in players)
            //{
            //    decimal totalbuycost = 0;
            //    decimal commisionAmt = 0;
            //    decimal tradeprice = 0;
            //    decimal tradeQuantity = 0;
            //    if (!player.isBuyCostAccurated)
            //    {
            //        List<AccountTradeReponse> accountTrades = await client.GetAccountTrades(new AllTradesRequest()
            //        {
            //            Limit = 10,
            //            Symbol = player.Pair
            //        });

            //        foreach (var trade in accountTrades)
            //        {
            //            if (trade.OrderId != player.BuyOrderId)
            //            {
            //                continue;
            //            }
            //            if (trade.CommissionAsset == "BNB")
            //            {
            //                commisionAmt = trade.Commission * bnbprice.Price;
            //            }
            //            else
            //            {
            //                commisionAmt = trade.Commission;
            //            }

            //            tradeQuantity += trade.Quantity;
            //            totalbuycost += (trade.Price * trade.Quantity) + commisionAmt;
            //        }

            //        tradeprice = totalbuycost / tradeQuantity;

            //        logger.Info(
            //           " " + player.Name +
            //           " " + player.Pair.Replace("USDT", "") +
            //           " Buy Cost " + player.TotalBuyCost.Deci().Rnd(5) + " Update To " + totalbuycost.Rnd(5) +
            //           " Coin Price " + player.BuyCoinPrice.Deci().Rnd(5) + " Update To " + tradeprice.Rnd(5) +
            //           " Quanity " + player.Quantity.Deci().Rnd(5) + " Update To " + tradeQuantity.Rnd(5));

            //        player.BuyCoinPrice = tradeprice;
            //        player.Quantity = tradeQuantity;
            //        player.isBuyCostAccurated = true;
            //        player.TotalBuyCost = totalbuycost;
            //        db.Player.Update(player);
            //    }
            //}

            //await db.SaveChangesAsync();
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
                    // logger.Info("  " + ProcessingTimeString + " " + player.Name + " is currently trading");
                    await UpdateActivePlayerStats(player);
                    continue;
                }

                if (player.isBuyOrderCompleted) // before buying the buyordercompleted should be false, so dont buy if its true
                {

                    logger.Info("  " + StrTradeTime + " " + player.Name + " isBuyOrderCompleted is true. Cant use it to buy");
                    continue;
                }

                if (player.AvailableAmountToBuy < configr.MinimumAmountToTradeWith)
                {
                    logger.Info("  " + StrTradeTime + " " + player.Name + " Avl Amt " + player.AvailableAmountToBuy + " Not enough for buying ");
                    continue;
                }

                foreach (Signal sig in CurrentSignals)
                {





                    if (sig.IsIgnored)
                    {
                        //   logger.Info("  " + ProcessingTimeString + " " + player.Name + "  " + sig.Symbol + " is already in ignore list ");
                        sig.IsIgnored = true;
                        continue;
                    }

                    if (sig.IsPicked)
                    {
                        //   logger.Info("  " + ProcessingTimeString + " " + player.Name + "  " + sig.Symbol + " is already in picked list ");
                        sig.IsPicked = true;
                        continue;
                    }

                    if (
                        sig.Symbol == "SNXUSDT" ||
                         sig.Symbol == "NANOUSDT" ||
                         sig.Symbol == "1INCHUSDT" ||
                           sig.Symbol == "GRTUSDT" ||
                        sig.Symbol == "ALPHAUSDT" ||
                        sig.Symbol == "ATOMUSDT"
                        )
                    {
                        sig.IsPicked = true;
                        continue;
                    }


                    // Use when some of your coins are really low and you are not able to buy new coins


                    if (boughtCoins.Contains(sig.Symbol))
                    {
                        sig.IsPicked = true;
                        continue;
                    }

                    //var count = boughtCoins.Where(x => x.Contains(sig.Symbol)).Count();

                    //if (count == 1)
                    //{
                    //    try
                    //    {
                    //        var selectedPlayer = await db.Player.AsNoTracking().Where(x => x.Pair == sig.Symbol).FirstOrDefaultAsync();

                    //        if (selectedPlayer == null)
                    //        {
                    //            sig.IsPicked = true;
                    //            continue;
                    //        }
                    //        var prlsPerc = selectedPlayer.TotalSellAmount.GetDiffPerc(selectedPlayer.TotalBuyCost);
                    //        // logger.Info(sig.Symbol + " Price Differce Buy and Sell amount " + prlsPerc.Deci().Rnd(5));

                    //        if (prlsPerc > -9.5M)
                    //        {
                    //            sig.IsPicked = true;
                    //            continue;
                    //        }
                    //    }
                    //    catch
                    //    {
                    //        sig.IsPicked = true;
                    //        continue;
                    //    }
                    //}
                    //if (count > 1)
                    //{
                    //    sig.IsPicked = true;
                    //    continue;
                    //}

                    if (PricesGoingDown(sig, player))
                    {
                        sig.IsIgnored = true;
                        continue;
                    }

                    // dont buy the same coin that you sold just now, at a higher price. Dont buy if the coin didnt drop by 10%

                    //try
                    //{
                    //    var coinsLastBuy = await db.PlayerTrades.Where(x => x.Pair == sig.Symbol && x.BuyOrSell != "Buy" && x.SellTime >= DateTime.Today).OrderBy(x => x.Id).LastOrDefaultAsync();

                    //    if (coinsLastBuy != null)
                    //    {
                    //        if (sig.CurrPr >= (coinsLastBuy.BuyCoinPrice + coinsLastBuy.SellCoinPrice) / 2)
                    //        {
                    //            logger.Info("  " +
                    //                sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
                    //                 " " + player.Name +
                    //                " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                    //            " This coin was bought earlier at a similar rate today " + coinsLastBuy.BuyCoinPrice.Deci().Rnd(5)
                    //            + " Current price is " + sig.CurrPr.Rnd(5) + " Ignore");

                    //            sig.IsIgnored = true;
                    //            continue;
                    //        }
                    //    }
                    //}
                    //catch (Exception ex)
                    //{

                    //    logger.Info("Exception while preventing to buy the same coin at a higher price" + ex.Message);
                    //}

                    if (sig.IsBestTimeToBuy && (sig.PrDiffCurrAndHighPerc < player.BuyBelowPerc))  //sig.IsBestTimeToBuy
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
            player.isBuyCostAccurated = false;
            player.HardSellPerc = 0;
            player.isBuyOrderCompleted = false;
            db.Player.Update(player);
            await db.SaveChangesAsync();
        }

        private async Task UpdatePlayerAfterSellConfirmed(Player player)
        {
            DB db = new DB();

            player.AvailableAmountToBuy = player.TotalSellAmount;

            var prDiffPerc = player.TotalSellAmount.GetDiffPerc(player.TotalBuyCost);

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
            player.isBuyCostAccurated = false;
            player.HardSellPerc = 0;
            player.isBuyOrderCompleted = false;
            player.isSellAmountAccurated = false;

            db.Player.Update(player);
            await db.SaveChangesAsync();
        }

        private async Task Sell(Player player)
        {
            DB db = new DB();
            decimal mysellPrice = 0;
            SymbolPriceChangeTickerResponse PriceChangeResponse = new SymbolPriceChangeTickerResponse();

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

            PriceChangeResponse = await client.GetDailyTicker(pair);

            mysellPrice = PriceChangeResponse.LastPrice;
            player.DayHigh = PriceChangeResponse.HighPrice;
            player.DayLow = PriceChangeResponse.LowPrice;
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
            var prDiffPerc = player.TotalSellAmount.GetDiffPerc(player.TotalBuyCost);
            player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";
            if (player.ProfitLossChanges.Length > 160)
            {
                player.ProfitLossChanges = player.ProfitLossChanges.GetLast(160);
            }
            Signal sig = CurrentSignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();
            var NextSellbelow = prDiffPerc * 93 / 100;

            // less than sellable percentage. Return
            if (prDiffPerc <= player.SellAbovePerc && ForceSell == false)
            {
                player.SellBelowPerc = player.SellAbovePerc;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }
            // Greater than sellabove percentage and prices are going up
            if (PricesGoingUp(sig, player) && ForceSell == false)
            {
                if (player.SellBelowPerc < NextSellbelow) player.SellBelowPerc = NextSellbelow;
                player.LastRoundProfitPerc = prDiffPerc;
                player.AvailableAmountToBuy = 0;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            //Greater than sellabove percentage, prices going down, but the profit percertage is still above sellbelow Percentage.

            if (NextSellbelow >= player.SellBelowPerc && ForceSell == false)
            {
                player.SellBelowPerc = NextSellbelow;
                player.LastRoundProfitPerc = prDiffPerc;
                player.AvailableAmountToBuy = 0;
                db.Player.Update(player);
                await db.SaveChangesAsync();

                logger.Info("  " +
                          sig.CandleOpenTime.ToString("dd-MMM HH:mm") +
                        " " + player.Name +
                       " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                        " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " > NextSellbelow  " + NextSellbelow.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " Not selling ");
                return;
            }

            if (sig != null)
            {
                logger.Info("  " +
                          sig.CandleOpenTime.ToString("dd-MMM HH:mm") +
                        " " + player.Name +
                       " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                        " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(12, ' ') +
                        "  LastRoundProfitPerc  " + player.LastRoundProfitPerc.Deci().Rnd().ToString().PadRight(12, ' ')
                        );
            }
            if ((prDiffPerc < player.LastRoundProfitPerc) || ForceSell == true)
            {
                if (sig != null)
                {
                    logger.Info("  " +
                              sig.CandleOpenTime.ToString("dd-MMM HH:mm") +
                            " " + player.Name +
                           " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(12, ' ') +
                            " < LastRoundProfitPerc  " + player.LastRoundProfitPerc.Deci().Rnd().ToString().PadRight(12, ' ') +
                            " selling ");
                }

                ForceSell = false;

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


                //if (prDiffPerc <= 0)
                //{
                //    player.BuyOrSell = "Loss";
                //    LogLossSell(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                //}
                //else
                //{
                //    player.BuyOrSell = "Profit";
                //    LogProfitSell(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                //}

                //PlayerTrades PlayerTrades = iPlaymerMapper.Map<Player, PlayerTrades>(player);
                //PlayerTrades.Id = 0;
                //await db.PlayerTrades.AddAsync(PlayerTrades);
                //player.LastRoundProfitPerc = 0;
                //player.DayHigh = 0.0M;
                //player.DayLow = 0.0M;
                //player.Pair = null;
                //player.BuyCoinPrice = 0.0M;
                //player.CurrentCoinPrice = 0.0M;
                //player.Quantity = 0.0M;
                //player.TotalBuyCost = 0.0M;
                //player.TotalCurrentValue = 0.0M;
                //player.TotalSellAmount = 0.0M;
                //player.BuyTime = null;
                //player.SellTime = null;
                //player.BuyCommision = 0.0M;
                //player.SellCoinPrice = 0.0M;
                //player.SellCommision = 0.0M;
                //player.SellBelowPerc = player.SellAbovePerc;
                //player.IsTrading = false;
                //player.BuyOrSell = string.Empty;
                //player.ProfitLossAmt = 0;
                //player.ProfitLossChanges = string.Empty;
                //player.BuyOrderId = 0;
                //player.SellOrderId = 0;
                //player.isBuyCostAccurated = false;
                //player.HardSellPerc = 0;
                //player.isBuyOrderCompleted = false;
                db.Player.Update(player);
            }
            else
            {
                LogNoSellReason(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                player.AvailableAmountToBuy = 0;
                player.LastRoundProfitPerc = prDiffPerc;
                db.Player.Update(player);
            }
            await db.SaveChangesAsync();
            ForceSell = false;
        }

        private async Task RemoveOldCandles()
        {

            using (var db = new DB())
            {
                await db.Database.ExecuteSqlRawAsync("delete from Candle where CAST(RecordedTime AS DATE)  <= GETDATE()-1");
            }

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
                    ExchangeInfoSymbolFilterLotSize lotsize = exchangeInfo.Symbols.Where(x => x.Symbol == coin.Coin).FirstOrDefault().Filters[2] as ExchangeInfoSymbolFilterLotSize;
                    var precision = lotsize.StepSize.GetAllowedPrecision();
                    coin.TradePrecision = precision;
                    db.MyCoins.Update(coin);
                    //    logger.Info("Precision for coin " + coin.Coin + " is set as " + precision + " Original step size from exchange info is " + lotsize.StepSize);
                }

                await db.SaveChangesAsync();
            }
            else
            {
                UpdatePrecisionCounter++;
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
                logger.Info(sig.Symbol.PadRight(7, ' ') + " Trade Count " + sig.DayTradeCount.ToString().PadRight(12, ' '));
            }
        }

        public bool PricesGoingDown(Signal sig, Player player)
        {
            if (sig == null) return false;

            DB db = new DB();

            decimal minoflastfewsignals = 0;
            var referencecandletimes = sig.CandleOpenTime.AddMinutes(-20);
            var lastfewsignals = db.Candle.AsNoTracking().Where(x => x.Symbol == sig.Symbol && x.OpenTime < sig.CandleOpenTime
            && x.OpenTime >= referencecandletimes).ToList();

            if (lastfewsignals != null && lastfewsignals.Count > 0)
            {
                minoflastfewsignals = lastfewsignals.Min(x => x.CurrentPrice);
            }
            else
            {
                return false;
            }

            if (sig.CurrPr <= minoflastfewsignals)
            {
                logger.Info("  " +
                   sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
                 " " + player.Name +
                " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                 " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                 " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                 " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                 " < Lst few rnds min " + minoflastfewsignals.Rnd(5).ToString().PadRight(12, ' ') +
                 " going down. No buy " + " PrDi Cr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' '));
                return true;
            }

            return false;
        }

        public bool PricesGoingUp(Signal sig, Player player)
        {
            if (sig == null) return false;

            DB db = new DB();

            decimal maxofLastfew = 0;
            var referencecandletimes = sig.CandleOpenTime.AddMinutes(-20);

            var lastfewsignals = db.Candle.AsNoTracking().Where(x => x.Symbol == sig.Symbol && x.OpenTime < sig.CandleOpenTime
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
                   sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
                 " " + player.Name +
                " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                 " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                 " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                 " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                 " > max of Lst few rnds " + maxofLastfew.Rnd(5).ToString().PadRight(12, ' ') +
                 " PrDiff " + player.TotalSellAmount.GetDiffPerc(player.TotalBuyCost).Deci().Rnd(5).ToString().PadRight(12, ' ') +
                 " going up. No Sell ");
                return true;
            }

            return false;
        }

        private async Task Trade()
        {
            //logger.Info("");

            //CurrentSignals = CurrentSignals.OrderBy(x => x.PrDiffCurrAndHighPerc).ToList();

            //foreach (var sig in CurrentSignals)
            //{
            //    LogSignal(sig);
            //}

            //logger.Info("");


            DB db = new DB();
            configr = await db.Config.FirstOrDefaultAsync();
            MyCoins = await db.MyCoins.AsNoTracking().ToListAsync();
            TradeTime = DateTime.Now;
            StrTradeTime = TradeTime.ToString("dd-MMM HH:mm:ss");
            NextTradeTime = TradeTime.AddMinutes(configr.IntervalMinutes).ToString("dd-MMM HH:mm:ss");

            #region GetSignals
            logger.Info("");
            logger.Info("Getting Candles started for " + StrTradeTime);
            try
            {
                await GetSignals();
            }
            catch (Exception ex)
            {
                logger.Error("  Exception in signal Generator.Returning.. " + ex.Message);
                return;
            }
            logger.Info("Getting Candles completed for " + StrTradeTime);
            logger.Info("");
            #endregion  GetSignals

            await UpdateTradeBuyDetails();
            await UpdateTradeSellDetails();
            Thread.Sleep(400);

            #region Buy

            try
            {

                logger.Info("Buying started for " + StrTradeTime);
                if (configr.IsBuyingAllowed) // && buyingCounter % 12 == 0
                {
                    await Buy();
                }
                else
                {
                    logger.Info("  Buying is not allowed at this moment " + StrTradeTime);
                }

                logger.Info("Buying Completed for " + StrTradeTime);

            }
            catch (Exception ex)
            {
                logger.Error("  Exception at buy " + ex.Message);
            }

            logger.Info("");
            #endregion Buys

            #region Sell

            logger.Info("Selling Started for " + StrTradeTime);

            try
            {
                Thread.Sleep(400);

                var activePlayers = await db.Player.AsNoTracking().OrderBy(x => x.Id).Where(x => x.IsTrading == true).ToListAsync();

                foreach (var player in activePlayers)
                {
                    if (player.isSellAllowed)
                    {
                        await Sell(player);
                    }
                    else
                    {
                        logger.Info("  " + StrTradeTime + " " + player.Name + " " + player.Pair + " is not allowed to  sell in this iteration ");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Exception in sell  " + ex.Message);
            }
            logger.Info("Selling Completed for " + StrTradeTime);

            #endregion  Sell



            await SetGrid();
            logger.Info("Next run at " + NextTradeTime);






            //var manualBinanceWebSocket = new InstanceBinanceWebSocketClient(client);

            //foreach (var coin in MyCoins)
            //{
            //    manualBinanceWebSocket.ConnectToIndividualSymbolTickerWebSocket(coin.Coin, b =>
            //    {
            //        logger.Debug("Symbol Ticker :" + b.Symbol + " current price " + b.LastPrice + " Low Price " + b.LowPrice + " Open Price " + b.OpenPrice +
            //            " Trades " + b.TotalNumberOfTrades + " Open Time " + b.StatisticsOpenTime + " High Price " + b.HighPrice
            //            );

            //        Thread.Sleep(5000);
            //    });
            //    Thread.Sleep(100);
            //}

            //// var socketId2 = manualBinanceWebSocket.ConnectToIndividualSymbolTickerWebSocket("ETHUSDT", b =>
            ////{
            ////    logger.Debug("Symbol Ticker :" + b.Symbol + " last price " + b.LastPrice + " Low Price " + b.LowPrice + " Open Price " + b.OpenPrice +
            ////        " Trades " + b.TotalNumberOfTrades + " Open Time " + b.StatisticsOpenTime + " High Price " + b.HighPrice
            ////        );
            ////    Thread.Sleep(2000);
            ////});



            ////var socketId = manualBinanceWebSocket.ConnectToIndividualSymbolTickerWebSocket(b =>
            ////{
            ////    logger.Debug("Symbol Ticker :" + b.Symbol + "  price " + b.Price + " WasBestPriceMatch " + b.WasBestPriceMatch + " WasBuyerMaker " + b.WasBuyerMaker 
            ////        );
            ////});




            ////var socketId2 = manualBinanceWebSocket.ConnectToDepthWebSocket("BTCUSDT", b =>
            ////{
            ////    logger.Debug(b.Symbol + " BidDepth Deltas" + b.BidDepthDeltas. +" Ask Depth Deltas" +b.AskDepthDeltas+ " Event Time "+b.EventTime +" Event Type " +b.EventType);
            ////});

            //var socketId3 = manualBinanceWebSocket.ConnectToKlineWebSocket("BTCUSDT", KlineInterval.FiveMinutes, b =>
            // {
            //     logger.Debug("Kline Socket :" + b.Symbol + " StartTime " + b.Kline.StartTime + " EndTime " + b.Kline.EndTime + " NumberOfTrades " + b.Kline.NumberOfTrades + " Open Price " + b.Kline.Open + " Close Price " + b.Kline.Close + " Low " + b.Kline.Low + " High " + b.Kline.High + " Volume " +
            //         b.Kline.Volume);
            // });

            ////var socketId4 = manualBinanceWebSocket.ConnectToTradesWebSocket("BTCUSDT", b =>
            ////{
            ////    logger.Debug("Trades Socket "+ b.Symbol+ " Price " + b.Price + " Quantity " + b.Quantity + " TradeTime " + b.TradeTime + " WasBestPriceMatch " + b.WasBestPriceMatch + " WasBuyerMaker " + b.WasBuyerMaker );
            ////});
        }

        #region low priority methods

        public void LogNoBuy(Player player, Signal sig)
        {
            //logger.Info("  " +
            //            sig.CandleOpenTime.ToString("dd-MMM HH:mm") +
            //          " " + player.Name +
            //         " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
            //           " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
            //           " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
            //          " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
            //          " Not Close to day low than high. Dont buy ");
        }

        public void LogBuy(Player player, Signal sig)
        {

            logger.Info("  " +
               sig.CandleOpenTime.ToString("dd-MMM HH:mm") +
             " " + player.Name +
            " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
              " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
              " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
             " CurCnPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
             " Close to day low than high. Buy Now ");

        }

        public void LogSignal(Signal sig)
        {
            string daylow = "";

            if (sig.IsCloseToDayLow)
                daylow = " DayLow : " + sig.IsCloseToDayLow + "".PadRight(5, ' ');
            else
                daylow = " DayLow : " + sig.IsCloseToDayLow + "".PadRight(4, ' ');

              if (sig.IsBestTimeToBuy)
            {
                logger.Info("  " + sig.CandleOpenTime.ToString("dd-MMM HH:mm") +
                   " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                   " CurCnPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                    daylow + " PrDi Cr&Hi " + sig.PrDiffCurrAndHighPerc.Rnd(6).ToString().PadRight(12, ' ') +
                    " Pr Ch in Lst Hr " + sig.PriceChangeInLastHour.Rnd(5).ToString().PadRight(12, ' ') +
                    " buy? : " + sig.IsBestTimeToBuy +
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