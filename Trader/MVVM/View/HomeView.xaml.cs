
using AutoMapper;
using BinanceExchange.API.Client;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using log4net;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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

        public static decimal GetDiffPerc(this int oldValue, int NewValue)
        {
            decimal oldvalueDecimal = Convert.ToDecimal(oldValue);
            decimal newvalueDecimal = Convert.ToDecimal(NewValue);
            return ((oldvalueDecimal - newvalueDecimal) / ((oldvalueDecimal + oldvalueDecimal) / 2)) * 100;
        }

        public static decimal? GetDiffPerc(this decimal? oldValue, decimal? NewValue)
        {

            return ((oldValue - NewValue) / ((NewValue + NewValue) / 2)) * 100;
        }

        public static string GetURL(this string pair)
        {
            return "https://www.binance.com/en/trade/" + pair.Replace("USDT", "_USDT") + "?layout=pro&type=spot";
        }
    }

    public partial class HomeView : UserControl
    {
        public List<MyCoins> MyCoins { get; set; }
        public DispatcherTimer CandleDataRetrieverTimer;
        public DispatcherTimer TraderTimer;
        public DispatcherTimer CandleDailyDataRetrieverTimer;
        public DateTime ProcessingTime { get; set; }
        public string ProcessingTimeString { get; set; }
        public decimal? lastRoundsProfitPerc = 0;
        public int totalConsecutivelosses = 0;
        public List<PlayerViewModel> PlayerViewModels;
        BinanceClient client;
        ILog logger;

        IMapper iMapr;
        IMapper iMapr2;
        List<string> boughtCoins = new List<string>();
        List<Signal> CurrentSignals = new List<Signal>();
        public Config configr = new Config();
        //  bool isfifteenminTrade = false;

        public HomeView()
        {
            InitializeComponent();
            Startup();
        }

        private void Startup()
        {
            DB db = new DB();

            configr = db.Config.First();

            TraderTimer = new DispatcherTimer();
            TraderTimer.Tick += new EventHandler(TraderTimer_Tick);
            TraderTimer.Interval = new TimeSpan(0, configr.IntervalMinutes, 0);

            lblBotName.Text = configr.Botname;
            logger = LogManager.GetLogger(typeof(MainWindow));


            var api = db.API.FirstOrDefault();
            client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = api.key,
                SecretKey = api.secret,
                Logger = logger,
            });


            //try
            //{
            //    SetGrid();
            //    CalculateBalanceSummary();
            //}
            //catch (Exception ex)
            //{
            //    //logger.Info("Exception during start calculating Balance at " + DateTime.Now.ToString("dd-MMM HH:mm:ss") + " " + ex.Message);
            //}

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Player, PlayerTrades>();
            });

            var config2 = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Candle, CandleBackUp>();
            });

            iMapr = config.CreateMapper();
            iMapr2 = config2.CreateMapper();
            if (configr.IsProd)
            {

                if (configr.Botname == "SHATLIN")
                {
                    Thread.Sleep(120000);
                }
                else if (configr.Botname == "DAMIEN")
                {
                    Thread.Sleep(240000);
                }
                TraderTimer.Start();
            }

            //logger.Info("Application Started and Timer Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));

        }

        private void SetGrid()
        {
            //DB GridDB = new DB();
            //BalanceDG.ItemsSource = GridDB.Balance.AsNoTracking().OrderByDescending(x => x.DiffPerc).ToList();
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

                //logger.Error("Exception at Updating Balance at timed intervals " + ex.Message);
            }
        }

        private async void btnTrade_Click(object sender, RoutedEventArgs e)
        {
            await Trade();
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
                                    //logger.Error("Exception while retrieving Price for Asset " + asset.Asset + " " + ex.Message);
                                }
                            }

                            decimal spentprice = 0;

                            foreach (var trade in trades)
                            {
                                if (trade.Pair.ToUpper().Contains(asset.Asset.ToUpper()))
                                {
                                    if (trade.IsBuyer)
                                    {
                                        spentprice += (trade.Price * trade.Quantity) + (trade.Price * trade.Quantity) * configr.CommisionAmount / 100;
                                    }
                                    else
                                    {
                                        spentprice -= (trade.Price * trade.Quantity) + (trade.Price * trade.Quantity) * configr.CommisionAmount / 100;
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
                        //logger.Error("Exception while retrieving balances " + ex.Message);
                    }
                }

                await BalanceDB.SaveChangesAsync();

                SetGrid();
                CalculateBalanceSummary();


            }
            catch (Exception ex)
            {
                //logger.Error($"Exception at Updating Balance {ex.Message}");
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
                //lblInvested.Text = "Invested:   " + String.Format("{0:0.00}", totalinvested);
                //lblCurrentValue.Text = "Current Value:   " + String.Format("{0:0.00}", totalcurrent);
                //lblDifference.Text = "Difference:   " + String.Format("{0:0.00}", totaldifference);
                //lblDifferencePercentage.Text = "Difference %:   " + String.Format("{0:0.00}", totaldifferenceinpercentage);
            }
            catch (Exception ex)
            {
                //logger.Error("Exception at setting Summary " + ex.Message);
            }

        }

        private async void btnClearPlayer_Click(object sender, RoutedEventArgs e)
        {
            await ClearData();

        }

        private async void btnSellAll_Click(object sender, RoutedEventArgs e)
        {
            await SellAll();
        }

        private async Task ClearData()
        {
            DB TradeDB = new DB();

            await TradeDB.Database.ExecuteSqlRawAsync("TRUNCATE TABLE PlayerTrades");

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
                player.CreatedDate = DateTime.Now;
                player.UpdatedTime = null;
                player.IsTrading = false;
                player.AvailableAmountForTrading = 200;
                player.OriginalAllocatedValue = 200;
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
                player.SaleProfitOrLoss = 0;
                player.LossOrProfit = string.Empty;
                player.LastRoundProfitPerc = 0;
                player.ProfitLossChanges = string.Empty;

                TradeDB.Update(player);
            }
            await TradeDB.SaveChangesAsync();


        }

        #region Prod

        private async void SellThisBot(object sender, RoutedEventArgs e)
        {
            PlayerViewModel model = (sender as Button).DataContext as PlayerViewModel;
            await SellPair(model.Pair);
            await Trade();
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
            //logger.Info("  Getting Candle Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            DB candledb = new DB();
            List<Candle> candles = new List<Candle>();

            try
            {
                foreach (var coin in MyCoins)
                {
                    var pair = coin.Coin + "USDT";

                    Candle candle = new Candle();
                    var pricechangeresponse = await client.GetDailyTicker(pair);

                    #region old candlerequest Code. I dont need it for my logic
                    //var candleRequest = new GetKlinesCandlesticksRequest
                    //{
                    //    Limit = 1,
                    //    Symbol = pair,
                    //    Interval = KlineInterval.FifteenMinutes
                    //};

                    //var candleResponse = await client.GetKlinesCandlesticks(candleRequest);
                    //var firstCandle = candleResponse[0];

                    //candle.RecordedTime = DateTime.Now;
                    //candle.Symbol = pair;
                    //candle.Open = firstCandle.Open;
                    //candle.OpenTime = firstCandle.OpenTime.AddHours(hourDifference);
                    //candle.High = firstCandle.High;
                    //candle.Low = firstCandle.Low;
                    //candle.Close = firstCandle.Close;
                    //candle.Volume = firstCandle.Volume;
                    //candle.CloseTime = firstCandle.CloseTime.AddHours(hourDifference);
                    //candle.QuoteAssetVolume = firstCandle.QuoteAssetVolume;
                    //candle.NumberOfTrades = firstCandle.NumberOfTrades;
                    //candle.TakerBuyBaseAssetVolume = firstCandle.TakerBuyBaseAssetVolume;
                    //candle.TakerBuyQuoteAssetVolume = firstCandle.TakerBuyQuoteAssetVolume;
                    #endregion old candlerequest Code. I dont need it for my logic

                    candle.RecordedTime = DateTime.Now;
                    candle.Symbol = pair;
                    candle.Open = 0;
                    candle.OpenTime = DateTime.Now;
                    candle.High = 0;
                    candle.Low = 0;
                    candle.Close = 0;
                    candle.Volume = 0;
                    candle.CloseTime = DateTime.Now;
                    candle.QuoteAssetVolume = 0;
                    candle.NumberOfTrades = 0;
                    candle.TakerBuyBaseAssetVolume = 0;
                    candle.TakerBuyQuoteAssetVolume = 0;

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
                //logger.Info("Exception in Getting Candle  " + ex.Message);
                throw;
            }

            await candledb.SaveChangesAsync();
            //logger.Info("  Getting Candle Completed at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));

            return candles;
        }

        private async Task<List<Signal>> GetSignals()
        {
            logger.Info("Generate Signals Started for " + ProcessingTimeString);

            DB db = new DB();
            CurrentSignals = new List<Signal>();
            List<Candle> candles = new List<Candle>();

            if (configr.IsProd)
            {
                candles = await GetCandle();
            }
            else
            {
                candles = await db.Candle.AsNoTracking().Where(x => x.OpenTime == startTime).ToListAsync();
            }

            //List<string> lastfivelosstradePairs = new List<string>();

            //var fivelosssales = await db.PlayerTrades.OrderByDescending(x => x.Id)
            //                                .Where(x => x.LossOrProfit == "Loss")
            //                                .Take(5).ToListAsync();
            //if (fivelosssales != null && fivelosssales.Count() > 0)
            //{
            //    foreach (var losssale in fivelosssales)
            //    {
            //        var losssaledate = Convert.ToDateTime(losssale.SellTime).Date;
            //        if (losssaledate == DateTime.Now.Date)
            //        {
            //            if (!lastfivelosstradePairs.Contains(losssale.Pair))
            //                lastfivelosstradePairs.Add(losssale.Pair);
            //        }
            //    }
            //}


            foreach (var coin in MyCoins)
            {
                var pair = coin.Coin + "USDT";
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
                sig.PrDiffCurrAndHighPerc = Math.Abs(sig.DayHighPr.GetDiffPerc(sig.CurrPr));
                sig.PrDiffCurrAndLowPerc = Math.Abs(sig.DayLowPr.GetDiffPerc(sig.CurrPr));
                var dayAveragePrice = (sig.DayHighPr + sig.DayLowPr) / 2;

                if (sig.CurrPr < dayAveragePrice) sig.IsCloseToDayLow = true;
                else sig.IsCloseToDayHigh = true;

                CurrentSignals.Add(sig);
            }

            //if (lastfivelosstradePairs.Count() > 0)
            //{
            //    foreach (var lostpair in lastfivelosstradePairs)
            //    {
            //        foreach (var signal in CurrentSignals)
            //        {
            //            if (signal.Symbol == lostpair)
            //            {
            //                //logger.Info("  " + lostpair + " recently was one of the last 5 loss sales. Not buying again today ");
            //                signal.IsIgnored = true;
            //            }
            //        }
            //    }
            //}

            CurrentSignals = CurrentSignals.OrderByDescending(x => x.DayTradeCount).ToList();
            logger.Info("Generate Signals completed for " + ProcessingTimeString);
            logger.Info("");
            return CurrentSignals;
        }

        public async Task RedistributeBalances()
        {
            DB db = new DB();
            var players = await db.Player.OrderBy(x => x.Id).ToListAsync();


            if (configr.IsProd)
            {

                AccountInformationResponse accinfo = await client.GetAccountInformation();

                decimal TotalAvalUSDT = 0;

                var USDT = accinfo.Balances.Where(x => x.Asset == "USDT").FirstOrDefault();

                if (USDT != null)
                {
                    TotalAvalUSDT = USDT.Free - (USDT.Free * 5 / 100); //Take only 95 % to cater for small differences
                }

                var availplayers = players.Where(x => x.IsTrading == false);

                if (availplayers.Count() > 0)
                {
                    var avgAvailAmountForTrading = TotalAvalUSDT / availplayers.Count();

                    foreach (var player in availplayers)
                    {
                        player.AvailableAmountForTrading = avgAvailAmountForTrading;
                        db.Player.Update(player);
                    }

                    await db.SaveChangesAsync();
                }
            }
            else // QA- Test Env.
            {
                var availplayers = players.Where(x => x.IsTrading == false);
                if (availplayers.Count() > 0)
                {
                    var avgAvailAmountForTrading = availplayers.Average(x => x.AvailableAmountForTrading);

                    foreach (var bot in availplayers)
                    {
                        bot.AvailableAmountForTrading = avgAvailAmountForTrading;
                        db.Player.Update(bot);
                    }

                    await db.SaveChangesAsync();
                }
            }


        }

        public async Task UpdateActivePlayerStats(Player player)
        {
            DB db = new DB();

            var playersSignal = CurrentSignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();

            if (playersSignal != null)
            {
                player.DayHigh = playersSignal.DayHighPr;
                player.DayLow = playersSignal.DayLowPr;
                player.CurrentPricePerCoin = playersSignal.CurrPr;
                player.TotalCurrentValue = player.CurrentPricePerCoin * player.QuantityBought;
                player.TotalCurrentProfit = player.TotalCurrentValue - player.OriginalAllocatedValue;
                player.AvailableAmountForTrading = 0;
                player.UpdatedTime = DateTime.Now;
                db.Player.Update(player);
                await db.SaveChangesAsync();

            }
        }

        public bool PricesGoingDown(Signal sig, Player player)
        {
            if (configr.IsProd)
            {
                DB db = new DB();

                decimal minoflastfewsignals = 0;

                var referencecandletimes = sig.CandleOpenTime.AddMinutes(-20);

                var lastfewsignals = db.Candle.Where(x => x.Symbol == sig.Symbol
                 && x.OpenTime < sig.CandleOpenTime
                  && x.OpenTime >= referencecandletimes).ToList();

                if (lastfewsignals != null && lastfewsignals.Count > 0)
                {
                    minoflastfewsignals = lastfewsignals.Min(x => x.CurrentPrice);
                }

                if (sig.CurrPr < minoflastfewsignals)
                {
                    //logger.Info("  " +
                    //   sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
                    // " " + player.Name + player.Avatar +
                    //" " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                    // " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                    // " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                    // " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                    // " < Min of Last 3 rounds " + minoflastfewsignals.Rnd(5).ToString().PadRight(8, ' ') +
                    // " Prices going down. Dont buy" + " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' '));
                    return true;
                }
            }
            return false;
        }

        private async Task BuyTheCoin(Player player, Signal sig)
        {
            DB db = new DB();
            decimal mybuyPrice = 0;
            #region log the buy

            //logger.Info("  " +
            //   sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
            // " " + player.Name + player.Avatar +
            //" " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
            // " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
            // " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
            // " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
            // " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
            // " < -" + player.BuyBelowPerc.Deci().Rnd(0) + " Buy Now ");

            #endregion log the buy

            if (configr.IsProd)
            {
                var PriceResponse = await client.GetPrice(sig.Symbol);
                mybuyPrice = PriceResponse.Price;
            }
            else
            {
                mybuyPrice = sig.CurrPr;
            }
            //  var mybuyPrice = latestPrice - (latestPrice * configr.BufferPriceForBuyAndSell / 100); // set  buy price to a tiny lesser than the current price.

            player.Pair = sig.Symbol;

            if (configr.IsProd)
            {
                var createOrder = await client.CreateTestOrder(new CreateOrderRequest()
                {
                    Price = mybuyPrice,
                    Quantity = player.AvailableAmountForTrading.Deci() / mybuyPrice,
                    Side = OrderSide.Buy,
                    Symbol = player.Pair,
                    Type = OrderType.Limit,
                });
            }

            player.IsTrading = true;

            player.DayHigh = sig.DayHighPr;
            player.DayLow = sig.DayLowPr;
            player.BuyPricePerCoin = mybuyPrice;
            player.QuantityBought = player.AvailableAmountForTrading / mybuyPrice;
            player.BuyingCommision = player.AvailableAmountForTrading * configr.CommisionAmount / 100;
            player.TotalBuyCost = mybuyPrice * player.QuantityBought + player.BuyingCommision;
            player.CurrentPricePerCoin = mybuyPrice;
            player.TotalCurrentValue = player.AvailableAmountForTrading;
            player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
            player.BuyTime = DateTime.Now;
            player.AvailableAmountForTrading = 0; // resetting available amount for tradning
            player.CandleOpenTimeAtBuy = sig.CandleOpenTime;
            player.CandleOpenTimeAtSell = null;
            player.BuyCandleId = sig.CandleId;
            player.SellCandleId = 0;
            player.UpdatedTime = DateTime.Now;
            player.BuyOrSell = "Buy";
            player.SellTime = null;
            player.QuantitySold = 0.0M;
            player.SoldCommision = 0.0M;
            player.SoldPricePricePerCoin = 0.0M;
            player.LossOrProfit = "Buy";
            player.SaleProfitOrLoss = 0;
            player.ProfitLossChanges = string.Empty;

            db.Player.Update(player);

            //Send Buy Order

            PlayerTrades playerHistory = iMapr.Map<Player, PlayerTrades>(player);
            playerHistory.Id = 0;
            await db.PlayerTrades.AddAsync(playerHistory);
            await db.SaveChangesAsync();
        }

        public async Task<decimal> GetAvailQty(Player player, string pair)
        {
            decimal availableQty = 0;
            if (configr.IsProd)
            {
                var coin = pair.Replace("USDT", "");

                AccountInformationResponse accinfo = await client.GetAccountInformation();

                var coinAvailable = accinfo.Balances.Where(x => x.Asset == coin).FirstOrDefault();

                if (coinAvailable != null)
                {
                    availableQty = coinAvailable.Free;
                }
                else
                {
                    //logger.Info("  " +
                    //ProcessingTimeString +
                    //" " + player.Name + player.Avatar +
                    //     " " + pair.Replace("USDT", "").PadRight(7, ' ') +
                    //" not available in Binance. its unusal, so wont execute sell order. Check it out");
                    availableQty = 0;
                }
            }

            else
            {
                availableQty = player.QuantityBought.Deci();
            }

            //if(player.QuantityBought!=availableQty)
            //{
            //    //logger.Info("  " +
            //       candleCloseTime +
            //       " " + player.Name + player.Avatar +
            //       " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
            //       " Player Quantity Bought " + player.QuantityBought +
            //       " not equal to available qty in Binance " + availableQty +
            //       " its unusal, so wont execute sell order. Check it out");
            //    continue;
            //}

            return availableQty;
        }

        private async Task Buy()
        {
            if (CurrentSignals == null || CurrentSignals.Count() == 0) return;

            logger.Info("Buying scan Started for " + ProcessingTimeString);

            DB db = new DB();
            var players = await db.Player.OrderBy(x => x.Id).ToListAsync();
            boughtCoins = await db.Player.Where(x => x.Pair != null).Select(x => x.Pair).ToListAsync();
            await RedistributeBalances();

            foreach (var player in players)
            {
                if (player.IsTrading)
                {
                    await UpdateActivePlayerStats(player);
                    continue;
                }

                if (player.AvailableAmountForTrading <= configr.MinimumAmountToTradeWith)
                {
                    //logger.Info("  " + ProcessingTimeString + " " + player.Name + player.Avatar + " Available Amt " + player.AvailableAmountForTrading + " Not enough for trading");
                    continue;
                }

                foreach (Signal sig in CurrentSignals)
                {
                    if (sig.IsIgnored || sig.IsPicked || boughtCoins.Contains(sig.Symbol) || PricesGoingDown(sig, player))
                    {
                        sig.IsIgnored = true;
                        continue;
                    }
                    if (sig.PrDiffCurrAndHighPerc > player.BuyBelowPerc)  //if (sig.IsCloseToDayLow && (sig.PrDiffCurrAndHighPerc > player.BuyBelowPerc)) 
                    {
                        await BuyTheCoin(player, sig);
                        sig.IsPicked = true;
                        boughtCoins.Add(sig.Symbol);
                        break;
                    }
                    else
                    {
                        LogNoBuy(player, sig);
                        sig.IsIgnored = true;
                    }
                }
            }
            logger.Info("Buying scan Completed for " + ProcessingTime);
            logger.Info("");
        }

        private async Task Sell()
        {
            decimal? averageProfitPerc = 0;
            int totalplayers = 0;
            logger.Info("Selling scan Started for " + ProcessingTimeString);
            PlayerViewModels = new List<PlayerViewModel>();
            DB TradeDB = new DB();
            var players = await TradeDB.Player.OrderBy(x => x.Id).ToListAsync();
            decimal mysellPrice = 0;
            SymbolPriceResponse PriceResponse = new SymbolPriceResponse();
            SymbolPriceChangeTickerResponse PriceChangeResponse = new SymbolPriceChangeTickerResponse();

            foreach (var player in players)
            {

                if (!player.IsTrading)
                {
                    continue;
                }

                PlayerViewModel playerViewModel = new PlayerViewModel();

                var pair = player.Pair;
                playerViewModel.Name = player.Name + player.Avatar;
                playerViewModel.Pair = pair;
                playerViewModel.BuyPricePerCoin = player.BuyPricePerCoin;
                playerViewModel.QuantityBought = player.QuantityBought;
                playerViewModel.BuyTime = Convert.ToDateTime(player.BuyTime).ToString("dd-MMM HH:mm");
                playerViewModel.SellBelowPerc = player.SellBelowPerc;
                playerViewModel.SellAbovePerc = player.SellAbovePerc;
                playerViewModel.TotalBuyCost = player.TotalBuyCost;

                if (configr.IsProd)
                {
                    PriceResponse = await client.GetPrice(pair);
                    PriceChangeResponse = await client.GetDailyTicker(pair);
                    mysellPrice = PriceResponse.Price;
                }
                else
                {
                    var signal = CurrentSignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();
                    mysellPrice = signal.CurrPr;
                    PriceChangeResponse.HighPrice = signal.DayHighPr;
                    PriceChangeResponse.LowPrice = signal.DayLowPr;
                    PriceChangeResponse.OpenTime = signal.CandleOpenTime;
                }

                player.SoldCommision = mysellPrice * player.QuantityBought * configr.CommisionAmount / 100;
                player.TotalSoldAmount = mysellPrice * player.QuantityBought - player.SoldCommision;
                var prDiffPerc = player.TotalSoldAmount.GetDiffPerc(player.TotalBuyCost);
                player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";
                playerViewModel.TotalCurrentValue = player.TotalSoldAmount;
                playerViewModel.CurrentPricePerCoin = mysellPrice;
                playerViewModel.CurrentRoundProfitPerc = prDiffPerc;
                playerViewModel.LastRoundProfitPerc = player.LastRoundProfitPerc;
                playerViewModel.ProfitLossChanges = player.ProfitLossChanges;
                averageProfitPerc += prDiffPerc;
                player.DayHigh = PriceChangeResponse.HighPrice;
                player.DayLow = PriceChangeResponse.LowPrice;
                player.UpdatedTime = DateTime.Now;
                player.SoldPricePricePerCoin = mysellPrice;
                decimal availableQty = await GetAvailQty(player, pair);
                if (availableQty <= 0) continue;

                player.QuantityBought = availableQty;
                player.SoldCommision = mysellPrice * availableQty * configr.CommisionAmount / 100;
                player.TotalSoldAmount = mysellPrice * availableQty - player.SoldCommision;
                player.SaleProfitOrLoss = (player.TotalSoldAmount - player.TotalBuyCost).Deci();
                player.CurrentPricePerCoin = mysellPrice;
                player.TotalCurrentValue = player.CurrentPricePerCoin * player.QuantityBought;
                player.QuantitySold = availableQty;
                player.AvailableAmountForTrading = player.TotalSoldAmount;
                player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
                player.SellCandleId = 0;

                totalplayers += 1;

                if (player.LastRoundProfitPerc != null && prDiffPerc > player.LastRoundProfitPerc)
                {
                    LogPriceIncreasingNoSell(player, pair, prDiffPerc);
                    player.LastRoundProfitPerc = prDiffPerc;
                    player.AvailableAmountForTrading = 0;
                    TradeDB.Player.Update(player);
                    PlayerViewModels.Add(playerViewModel);
                    continue;
                }

                if ((prDiffPerc > player.SellAbovePerc) || (prDiffPerc < player.SellBelowPerc))
                {
                    if (prDiffPerc < player.DontSellBelowPerc)
                    {
                        LogDontSellBelowPercReason(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                        PlayerViewModels.Add(playerViewModel);
                        player.LastRoundProfitPerc = prDiffPerc;
                        player.AvailableAmountForTrading = 0;
                        TradeDB.Player.Update(player);
                        continue;
                    }

                    if (configr.IsProd)
                    {
                        var createOrder = await client.CreateTestOrder(new CreateOrderRequest()
                        {
                            Price = mysellPrice,
                            Quantity = availableQty,
                            Side = OrderSide.Sell,
                            Symbol = player.Pair,
                            Type = OrderType.Limit,
                        });
                    }

                    player.SellTime = PriceChangeResponse.OpenTime;
                    player.CandleOpenTimeAtSell = PriceChangeResponse.OpenTime;
                    player.BuyOrSell = "Sell";
                    player.LastRoundProfitPerc = 0;


                    if (prDiffPerc > player.SellAbovePerc)
                    {
                        player.LossOrProfit = "Profit";
                        //configr.TotalConsecutiveLosses -= 1;
                        //if (configr.TotalConsecutiveLosses < 0) configr.TotalConsecutiveLosses = 0;
                        LogProfitSell(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                    }
                    else if (prDiffPerc < player.SellBelowPerc)
                    {
                        player.LossOrProfit = "Loss";
                        //  configr.TotalConsecutiveLosses += 1;
                        LogLossSell(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                    }

                    PlayerTrades PlayerTrades = iMapr.Map<Player, PlayerTrades>(player);
                    PlayerTrades.Id = 0;
                    await TradeDB.PlayerTrades.AddAsync(PlayerTrades);
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
                    player.SaleProfitOrLoss = 0;
                    player.LossOrProfit = string.Empty;
                    player.ProfitLossChanges = string.Empty;
                    TradeDB.Player.Update(player);
                }
                else
                {
                    LogNoSellReason(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                    playerViewModel.LastRoundProfitPerc = player.LastRoundProfitPerc;
                    player.AvailableAmountForTrading = 0;
                    player.LastRoundProfitPerc = prDiffPerc;
                    TradeDB.Player.Update(player);
                    PlayerViewModels.Add(playerViewModel);
                }

            }

            // TradeDB.Config.Update(configr);
            await TradeDB.SaveChangesAsync();
            //if (totalplayers > 0)
            //{
            //    //logger.Info("  " + ProcessingTimeString + " Average Potential Profit/loss in this round " + (averageProfitPerc / totalplayers).Deci().Rnd(5));
            //}

            logger.Info("Selling scan Completed for " + ProcessingTimeString + ". Next scan at " + ProcessingTime.AddMinutes(configr.IntervalMinutes).ToString("dd-MMM HH:mm"));

            logger.Info("");

            //BalanceDG.ItemsSource = PlayerViewModels.OrderByDescending(x => x.CurrentRoundProfitPerc);
            //if (totalplayers > 0)
            //{
            //    lblInvested.Text = "Avg Profit loss % : " + (averageProfitPerc / totalplayers).Deci().Rnd(5);
            //}
            //lblDifference.Text = "Last Trade : " + ProcessingTime.ToString("dd-MMM HH:mm");
            //lblDifferencePercentage.Text = "Next : " + ProcessingTime.AddMinutes(configr.IntervalMinutes).ToString("dd-MMM HH:mm");
        }

        private async Task SellAll()
        {
            DB db = new DB();
            var players = await db.Player.OrderBy(x => x.Id).Where(x => x.IsTrading == true).ToListAsync();
            foreach (var player in players)
            {
                await SellPair(player.Pair);
            }
            await Trade();
        }

        //remove this code. Just update a flag in signals to say that forcesell and call the standard trade method.
        private async Task SellPair(string pair)
        {
            //logger.Info("Selling " + pair + " Started at " + ProcessingTimeString);

            DB db = new DB();
            var player = await db.Player.Where(x => x.Pair == pair).FirstOrDefaultAsync();

            var PriceResponse = await client.GetPrice(pair);
            var PriceChangeResponse = await client.GetDailyTicker(pair);
            var latestPrice = PriceResponse.Price;
            var mysellPrice = latestPrice + (latestPrice * configr.BufferPriceForBuyAndSell / 100); // setting the sell price to a tiny amount more than the current price.

            player.SoldCommision = mysellPrice * player.QuantityBought * configr.CommisionAmount / 100;
            player.TotalSoldAmount = mysellPrice * player.QuantityBought - player.SoldCommision;
            var prDiffPerc = player.TotalSoldAmount.GetDiffPerc(player.TotalBuyCost);
            decimal availableQty = await GetAvailQty(player, pair);
            if (availableQty <= 0) return;

            player.QuantityBought = availableQty;
            player.SoldCommision = mysellPrice * availableQty * configr.CommisionAmount / 100;
            player.TotalSoldAmount = mysellPrice * availableQty - player.SoldCommision;

            if (configr.IsProd)
            {
                var createOrder = await client.CreateTestOrder(new CreateOrderRequest()
                {
                    Price = mysellPrice,
                    Quantity = availableQty,
                    Side = OrderSide.Sell,
                    Symbol = player.Pair,
                    Type = OrderType.Limit,
                });
            }

            player.SaleProfitOrLoss = (player.TotalSoldAmount - player.TotalBuyCost).Deci();
            player.DayHigh = PriceChangeResponse.HighPrice;
            player.DayLow = PriceChangeResponse.LowPrice;
            player.CurrentPricePerCoin = latestPrice;
            player.TotalCurrentValue = player.CurrentPricePerCoin * player.QuantityBought;
            player.QuantitySold = availableQty;
            player.AvailableAmountForTrading = player.TotalSoldAmount;
            player.SellTime = DateTime.Now;
            player.UpdatedTime = DateTime.Now;
            player.SoldPricePricePerCoin = mysellPrice;
            player.CandleOpenTimeAtSell = DateTime.Now;
            player.BuyOrSell = "Sell";
            player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
            player.SellCandleId = 0;
            player.LastRoundProfitPerc = 0;

            if (prDiffPerc >= 0)
            {
                LogProfitSell(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                player.LossOrProfit = "Profit";
                //  configr.TotalConsecutiveLosses -= 1;
                if (configr.TotalConsecutiveLosses < 0) configr.TotalConsecutiveLosses = 0;

            }
            else
            {
                LogLossSell(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                player.LossOrProfit = "Loss";
                //    configr.TotalConsecutiveLosses += 1;

            }

            PlayerTrades PlayerTrades = iMapr.Map<Player, PlayerTrades>(player);
            PlayerTrades.Id = 0;
            await db.PlayerTrades.AddAsync(PlayerTrades);

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
            player.SaleProfitOrLoss = 0;
            player.LossOrProfit = string.Empty;
            db.Player.Update(player);

            //  db.Config.Update(configr);
            await db.SaveChangesAsync();
            //logger.Info("Sell single Completed at " + ProcessingTimeString);
            //logger.Info("");

        }

        #endregion Prod

        private async Task BackupOldCandles()
        {

            var threedaysback = DateTime.Now.AddDays(-3);
            using (var db = new DB())
            {
                var oldercandles = db.Candle.Where(x => x.RecordedTime < threedaysback);
                foreach (var oldercandle in oldercandles)
                {
                    CandleBackUp candleBackUp = iMapr2.Map<Candle, CandleBackUp>(oldercandle);
                    candleBackUp.Id = 0;
                    await db.CandleBackUp.AddAsync(candleBackUp);
                    db.Candle.Remove(oldercandle);

                }
                await db.SaveChangesAsync();
            }
        }

        DateTime startTime = new DateTime();
        DateTime endTime = new DateTime();

        private async Task Trade()
        {
            DB db = new DB();
            configr = await db.Config.FirstOrDefaultAsync();

            MyCoins = await db.MyCoins.AsNoTracking().ToListAsync();
            if (configr.IsProd)
            {
                #region ProdRun


                var allplayers = await db.Player.ToListAsync();

                ProcessingTime = DateTime.Now;
                ProcessingTimeString = ProcessingTime.ToString("dd-MMM HH:mm:ss");

                #region GetSignals
                {
                    try
                    {
                        await GetSignals();
                    }
                    catch (Exception ex)
                    {
                        //logger.Error("Exception in signal Generator.Returning.. " + ex.Message);
                        return;
                    }
                }

                #endregion  GetSignals

                #region Buys

                try
                {
                    //if (configr.TotalConsecutiveLosses < 3)
                    //{
                    await Buy();
                    //}
                    //else
                    //{
                    ////logger.Info("Last " + configr.TotalConsecutiveLosses + " sells were at loss. Lets wait for " +
                    ////   (configr.MaxPauses - configr.TotalCurrentPauses) * configr.IntervalMinutes + " more minutes before attempting to buy again");

                    //configr.TotalCurrentPauses += 1;

                    //if (configr.TotalCurrentPauses > configr.MaxPauses)
                    //{

                    //    #region check if the falling market recovered and is on an upward trend to resume trading. 
                    //    //if this hour's prices are not getting higher than the previous hour's, there is no point in buying. Better to wait it out.

                    //    decimal totalincreases = 0;

                    //    string pair = string.Empty;

                    //    foreach (var coin in MyCoins)
                    //    {
                    //        pair = coin.Coin + "USDT";
                    //        var AvgOflastfewprices = db.Candle.Where(x => x.Symbol == pair).OrderByDescending(x => x.Id).Take(4).Average(x => x.CurrentPrice);
                    //        var AvgOfPrevfewPrices = db.Candle.Where(x => x.Symbol == pair).OrderByDescending(x => x.Id).Skip(4).Take(4).Average(x => x.CurrentPrice);

                    //        if (AvgOflastfewprices > AvgOfPrevfewPrices)
                    //        {
                    //            totalincreases += 1;
                    //        }

                    //        //logger.Info("Avg of last 4 " + pair + " is " + AvgOflastfewprices.Rnd(5) + "  Avg of previous 4 " +
                    //        //    pair + " is " + AvgOfPrevfewPrices.Rnd(5));
                    //    }

                    //    //logger.Info("Total Increases " + totalincreases + " out of " + MyCoins.Count());


                    //    if (totalincreases >= (MyCoins.Count() / 2))
                    //    {
                    //        //logger.Info("Wait times over. Scenario is better. Resetting losses and starting to trade again");
                    //        configr.TotalCurrentPauses = 0;
                    //        configr.TotalConsecutiveLosses = 0;
                    //    }
                    //    else
                    //    {
                    //        //logger.Info("Still on downward trend. Go back to pause and observe mode");
                    //        configr.TotalCurrentPauses = 0;
                    //    }

                    //    #endregion
                    //}
                    //db.Update(configr);
                    //await db.SaveChangesAsync();
                    //}
                }
                catch (Exception ex)
                {
                    logger.Error("Exception in Buy  " + ex.Message);
                }

                #endregion  Buys

                #region Sells

                try
                {
                    await Sell();
                }
                catch (Exception ex)
                {
                    //logger.Error("Exception in sell  " + ex.Message);
                }

                #endregion  Sells

                #region moveOldCandlestoBackup

                await BackupOldCandles();


                #endregion  moveOldCandlestoBackup

                #endregion ProdRun
            }
            else
            {
                #region testRun

                await ClearData();

                startTime = new DateTime(2021, 5, 1, 0, 0, 0);
                endTime = new DateTime(2021, 6, 27, 23, 0, 0);


                while (startTime < endTime)
                {
                    using (var db2 = new DB())
                    {
                        //  configr = await db2.Config.FirstOrDefaultAsync();
                        var allbots = await db2.Player.ToListAsync();

                        ProcessingTime = startTime;
                        ProcessingTimeString = ProcessingTime.ToString("dd-MMM HH:mm:ss");
                        try
                        {
                            await GetSignals();

                            #region Buys

                            try
                            {
                                await Buy();
                            }
                            catch (Exception ex)
                            {
                                logger.Error("Exception in Buy  " + ex.Message);
                            }

                            #endregion  Buys

                            await Sell();
                        }
                        catch (Exception ex)
                        {
                            logger.Error("Exception in trade " + ex.Message);
                        }
                    }
                    startTime = startTime.AddMinutes(5);
                }


                logger.Info("---------------Test Run for Trading Completed --------------");

                #endregion testRun
            }
        }


        #region log methods

        public void LogNoBuy(Player player, Signal sig)
        {
            //logger.Info("  " +
            //   sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
            // " " + player.Name + player.Avatar +
            //" " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
            // " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
            // " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
            // " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
            // " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
            // " > -" + player.BuyBelowPerc.Deci().Rnd(0) +
            //  " Dont buy");
        }

        public void LogPriceIncreasingNoSell(Player player, string pair, decimal? prDiffPerc)
        {
            //logger.Info("  " +
            //ProcessingTimeString +
            //" " + player.Name + player.Avatar +
            //" " + pair.Replace("USDT", "").PadRight(7, ' ') +
            //" PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(6) +
            //" > last round's price difference " + player.LastRoundProfitPerc.Deci().Rnd(6) +
            //"  Price increasing. Dont sell ");
        }

        public void LogDontSellBelowPercReason(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {
            #region log not selling reason

            //logger.Info("  " +
            //ProcessingTimeString +
            //" " + player.Name + player.Avatar +
            //" " + pair.Replace("USDT", "").PadRight(7, ' ') +
            //" DHi   " + PriceChangeResponse.HighPrice.Rnd().ToString().PadRight(10, ' ') +
            //" DLo   " + PriceChangeResponse.LowPrice.Rnd().ToString().PadRight(10, ' ') +
            //" BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
            //" CurPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
            //" BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
            //" SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            //" PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(5) +
            //" > " + player.DontSellBelowPerc.Deci().Rnd(0) +
            //" Not selling ");


            #endregion log not selling reason
        }

        public void LogLossSell(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {
            #region log loss sell

            //logger.Info("  " +
            //ProcessingTimeString +
            //" " + player.Name + player.Avatar +
            // " " + pair.Replace("USDT", "").PadRight(7, ' ') +
            //   " DHi   " + PriceChangeResponse.HighPrice.Rnd().ToString().PadRight(10, ' ') +
            //   " DLo   " + PriceChangeResponse.LowPrice.Rnd().ToString().PadRight(10, ' ') +
            //   " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
            //   " CurPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
            //   " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
            //   " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            //" PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(4).ToString().PadRight(5, ' ') +
            //" < " + player.SellBelowPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
            //" Loss Sell ");

            //logger.Info("  " + ProcessingTimeString + " Total Consecutive Loss " + configr.TotalConsecutiveLosses);
            #endregion log loss sell
        }

        public void LogProfitSell(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {

            #region log profit sell

            //logger.Info("  " +
            //ProcessingTimeString +
            // " " + player.Name + player.Avatar +
            //   " " + pair.Replace("USDT", "").PadRight(7, ' ') +
            //" DHi   " + PriceChangeResponse.HighPrice.Rnd().ToString().PadRight(10, ' ') +
            //" DLo   " + PriceChangeResponse.LowPrice.Rnd().ToString().PadRight(10, ' ') +
            //" BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
            //" CurPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
            //" BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
            //" SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            // " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(4).ToString().PadRight(5, ' ') +
            // " > " + player.SellAbovePerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
            // " Profit Sell ");
            #endregion log profit sell

        }

        public void LogNoSellReason(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {
            #region log dont sell reason

            //logger.Info("  " +
            //ProcessingTimeString +
            //" " + player.Name + player.Avatar +
            //  " " + pair.Replace("USDT", "").PadRight(7, ' ') +
            //   " DHi   " + PriceChangeResponse.HighPrice.Rnd().ToString().PadRight(10, ' ') +
            //   " DLo   " + PriceChangeResponse.LowPrice.Rnd().ToString().PadRight(10, ' ') +
            //   " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
            //   " CurPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
            //   " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
            //   " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            //" PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
            //" < " + player.SellAbovePerc.Deci().Rnd(1).ToString().PadRight(3, ' ') +
            //" and > " + player.SellBelowPerc.Deci().Rnd(1) + " Dont Sell ");



            #endregion  log dont sell reason
        }


        private async void btnCollectData_Click(object sender, RoutedEventArgs e)
        {

            //logger.Info("Collect Data Started at " + DateTime.Now);

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
                            Candle candle = new Candle();
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

                        //logger.Info(i + " : " + file + " Processing Completed ");


                    }
                    await candledb.SaveChangesAsync();
                }
            }

            //logger.Info("----------All file Processing Completed-------------- ");
            await UpdateData();
        }

        private async Task UpdateData()
        {
            DateTime currentdate = new DateTime(2021, 3, 1, 0, 0, 0);
            DateTime lastdate = new DateTime(2021, 4, 30, 23, 0, 0);


            while (currentdate <= lastdate)
            {
                using (var TradeDB = new DB())
                {

                    List<Candle> selectedCandles;
                    List<MyCoins> myTradeFavouredCoins = await TradeDB.MyCoins.AsNoTracking().ToListAsync();

                    try
                    {
                        foreach (var favtrade in myTradeFavouredCoins)
                        {

                            selectedCandles = await TradeDB.Candle.Where(x => x.Symbol == favtrade.Coin + "USDT"
                            && x.OpenTime.Date == currentdate.Date
                            && x.DayTradeCount == 0
                            ).ToListAsync();

                            if (selectedCandles == null || selectedCandles.Count == 0)
                            {
                                //logger.Error("No Candles found for " + favtrade.Coin + " on " + currentdate.Date + " ");
                                continue;
                            }
                            foreach (var candle in selectedCandles)
                            {
                                candle.DayHighPrice = selectedCandles.Max(x => x.High);
                                candle.DayLowPrice = selectedCandles.Min(x => x.Low);
                                candle.DayVolume = selectedCandles.Sum(x => x.Volume);
                                candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
                                if (candle.DayTradeCount == 0)
                                {

                                }
                                TradeDB.Candle.Update(candle);

                            }
                            await TradeDB.SaveChangesAsync();
                        }

                        //logger.Info("Candles Updated for " + currentdate.Date + " ");
                    }
                    catch (Exception ex)
                    {
                        //logger.Error(" Updating candle error " + ex.Message);
                    }

                }

                currentdate = currentdate.AddDays(1);
            }

            //logger.Info(" Updating data Completed ");
        }

        #endregion

    }
}
