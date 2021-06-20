
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

        public static decimal? GetDiffPerc(this decimal? oldValue, decimal? NewValue)
        {

            return ((oldValue - NewValue) / ((NewValue + NewValue) / 2)) * 100;
        }


    }

    public partial class HomeView : UserControl
    {
        public List<MyCoins> MyCoins { get; set; }
        public DispatcherTimer CandleDataRetrieverTimer;
        public DispatcherTimer TraderTimer;
        public DispatcherTimer CandleDailyDataRetrieverTimer;
        public string ProcessingTime { get; set; }
        public bool isProd = false;

        public int totalConsecutivelosses = 0;
        int totalcurrentPauses = 0;
        int maxTotalPauses=3;

        BinanceClient client;
        ILog logger;
        int intervalMins = 7;
        
        IMapper iMapr;
        List<string> boughtCoins = new List<string>();
        List<Signal> CurrentSignals = new List<Signal>();

        //  bool isfifteenminTrade = false;

        public HomeView()
        {
            InitializeComponent();
            Startup();
        }

        private void Startup()
        {

            TraderTimer = new DispatcherTimer();
            TraderTimer.Tick += new EventHandler(TraderTimer_Tick);
            TraderTimer.Interval = new TimeSpan(0, intervalMins, 0);
            TraderTimer.Start();
            logger = LogManager.GetLogger(typeof(MainWindow));

            DB db = new DB();
            var api = db.API.FirstOrDefault();
            client = new BinanceClient(new ClientConfiguration()
            {
                ApiKey = api.key,
                SecretKey = api.secret,
                Logger = logger,
            });

            logger.Info("Application Started and Timer Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            try
            {
                SetGrid();
                CalculateBalanceSummary();
            }
            catch (Exception ex)
            {
                logger.Info("Exception during start calculating Balance at " + DateTime.Now.ToString("dd-MMM HH:mm:ss") + " " + ex.Message);
            }

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Player, PlayerTrades>();
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

        #region Prod

        private async Task<List<Candle>> GetCandle_Prod()
        {
            logger.Info("Getting Candle Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
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
                logger.Info("Exception in Getting Candle  " + ex.Message);
                throw;
            }
           
            await candledb.SaveChangesAsync();
            logger.Info("Getting Candle Completed at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            logger.Info("");
            return candles;
        }

        private async Task<List<Signal>> GetSignals_Prod()
        {
            DB TradeDB = new DB();
            CurrentSignals = new List<Signal>();
            List<Candle> candles = await GetCandle_Prod();

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

            CurrentSignals = CurrentSignals.OrderBy(x => x.PrDiffCurrAndLowPerc).ToList();
            return CurrentSignals;
        }

        private async Task Buy_Prod()
        {

            if (CurrentSignals == null || CurrentSignals.Count() == 0)
            {
                logger.Info("No signals found. returning from buying");
                return;
            }

            logger.Info("Buying scan Started at " + ProcessingTime);

            #region definitions

            DB db = new DB();
            var players = await db.Player.OrderBy(x => x.Id).ToListAsync();
            boughtCoins = await db.Player.Where(x => x.Pair != null).Select(x => x.Pair).ToListAsync();
            bool isdbUpdateRequired = false;

            #endregion definitions

            #region prod buying logic

            /*
             *Step 1: Get All balances and apart from USDT, ensure they correspond to your player table.
             *Step 2: coins other than USDT are your "Actively Trading Coins"
             *Step 3: Once the matching is done,ignore anything in binance ( Could be from staking)
             *Step 4: Divide USDT to available bots, but leave 5% in account to cater for inconsistencies, this is what is available for them to buy. Update all Player fields.
             *
             *Remember: Get all signals, but just before issuing a buy order, get current price and do "the" checks.
             *Remember:Always issue limit order, so that you can record the exact price for which you bought for 
                In Prod, I would just need the latest candle list and no references.

                       //buying criteria
                    //1. signals are ordered by the coins whose current price are at their lowest at the moment
                    //2. See if this price is the lowest in the last 24 hours
                    //3. See if the price difference is lower than what the player is expecting to buy at. If yes, buy.
                    //Later see if you are on a downtrend and keep waiting till it reaches its low and then buy

             */

            #endregion

            #region redistribute balances to bots waiting to buy

            if (isProd)
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



            #endregion redistribute balances to bots waiting to buy


            foreach (var player in players)
            {
                #region if player is currently trading, just update stats and go to next player

                if (player.IsTrading)
                {
                    logger.Info("  " + ProcessingTime + " " + player.Name + player.Avatar + " currently occupied");

                    var playersSignal = CurrentSignals.Where(x => x.Symbol == player.Pair).FirstOrDefault();

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

                if (player.AvailableAmountForTrading <= 70)
                {
                    logger.Info("  " + ProcessingTime + " " + player.Name + player.Avatar + " Available amount " + player.AvailableAmountForTrading + " Not enough for trading");
                    continue;
                }

                #endregion if player is currently trading, just update stats and go to next player

                foreach (var sig in CurrentSignals)
                {
                    #region conditions to no go further with buy

                    if (sig.IsIgnored) //signal is already checked in this buy cycle, so we are not going to buy. Dont need to check this for another bot
                    {
                        continue;
                    }

                    if (sig.IsPicked)
                    {
                        logger.Info("  " +
                        sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "") +
                        " is picked by another bot. look for another coin ");
                        continue;
                    }

                    if (boughtCoins.Contains(sig.Symbol))
                    {
                        logger.Info("  " +
                             sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
                            " " + player.Name + player.Avatar +
                            " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " coin already bought. Going to next signal ");
                        sig.IsIgnored = true;
                        continue;
                    }

                 

                  

                    #endregion  conditions to no go further with buy

                    if (sig.IsCloseToDayLow && (sig.PrDiffCurrAndHighPerc > player.BuyBelowPerc))
                    {
                        #region log the buy

                        logger.Info("  " +
                            sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
                          " " + player.Name + player.Avatar +
                         " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                          " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                          " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                          " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                          " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
                          " < -" + player.BuyBelowPerc.Deci().Rnd(0) + " Buy Now");

                        #endregion log the buy

                        var PriceResponse = await client.GetPrice(sig.Symbol);
                        var latestPrice = PriceResponse.Price;

                        var mybuyPrice = latestPrice - (latestPrice * 0.07M / 100); // setting the buy price to a tiny amount lesser than the current price.
                        player.Pair = sig.Symbol;

                        if (isProd)
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
                        player.BuyingCommision = player.AvailableAmountForTrading * 0.075M / 100;
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

                        sig.IsPicked = true;
                        db.Player.Update(player);

                        //Send Buy Order

                        PlayerTrades playerHistory = iMapr.Map<Player, PlayerTrades>(player);
                        playerHistory.Id = 0;
                        await db.PlayerTrades.AddAsync(playerHistory);
                        isdbUpdateRequired = true; //flag that db needs to be updated, and update it at the end
                        boughtCoins.Add(sig.Symbol);
                        break;
                    }
                    else
                    {
                        #region log the no buy

                        logger.Info("  " +
                              sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
                            " " + player.Name + player.Avatar +
                           " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                            " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                            " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                            " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
                            " > -" + player.BuyBelowPerc.Deci().Rnd(0) +
                            " Or not close to day low, Not Buying");

                        #endregion

                        sig.IsIgnored = true;
                    }
                }
            }
            if (isdbUpdateRequired) await db.SaveChangesAsync();

            logger.Info("Buying scan Completed at " + ProcessingTime + ". Next scan at " + DateTime.Now.AddMinutes(intervalMins).ToString("dd-MMM HH:mm"));
            logger.Info("");
        }

        private async Task Sell_Prod()
        {

            if (CurrentSignals == null || CurrentSignals.Count() == 0)
            {
                logger.Info("No signals found. returning from selling");
                return;
            }

            logger.Info("Selling scan Started at " + ProcessingTime);

            DB TradeDB = new DB();
            var players = await TradeDB.Player.OrderBy(x => x.Id).ToListAsync();

            foreach (var player in players)
            {
                #region conditions to go no further with sell

                if (!player.IsTrading)
                {
                    logger.Info("  " + ProcessingTime + " " + player.Name + player.Avatar + " is waiting to buy. Nothing to sell");
                    continue;
                }

                var pair = player.Pair;
                var sig = CurrentSignals.Where(x => x.Symbol == pair).FirstOrDefault();
                if (sig == null)
                {
                    logger.Info("  " + ProcessingTime + " " + player.Name + player.Avatar + " " + pair.Replace("USDT", "").PadRight(7, ' ') + " No signals returned. Continuing ");
                    continue;
                }

                #endregion conditions to go no further with sell

                //update to set all these values in PlayerTrades

                player.SoldCommision = player.CurrentPricePerCoin * player.QuantityBought * 0.075M / 100;
                player.TotalSoldAmount = player.TotalCurrentValue - player.SoldCommision;

                var prDiffPerc = player.TotalSoldAmount.GetDiffPerc(player.TotalBuyCost);

                if ((prDiffPerc > player.SellAbovePerc) || (prDiffPerc < player.SellBelowPerc))
                {
                    if (prDiffPerc < player.DontSellBelowPerc)
                    {
                        #region log not selling reason

                        logger.Info("  " +
                           ProcessingTime +
                           " " + player.Name + player.Avatar +
                           " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                           " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(10, ' ') +
                           " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(10, ' ') +
                           " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
                           " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(10, ' ') +
                           " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(0) +
                           " > " + player.DontSellBelowPerc.Deci().Rnd(0) +
                           " Not selling");

                        #endregion log not selling reason
                        continue;
                    }


                    decimal availableQty = 0;

                    if (isProd)
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
                            logger.Info("  " +
                            ProcessingTime +
                            " " + player.Name + player.Avatar +
                            " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                            " not available in Binance. its unusal, so wont execute sell order. Check it out");

                            continue;
                        }
                    }

                    else
                    {
                        availableQty = player.QuantityBought.Deci();
                    }

                    //if(player.QuantityBought!=availableQty)
                    //{
                    //    logger.Info("  " +
                    //       candleCloseTime +
                    //       " " + player.Name + player.Avatar +
                    //       " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                    //       " Player Quantity Bought " + player.QuantityBought +
                    //       " not equal to available qty in Binance " + availableQty +
                    //       " its unusal, so wont execute sell order. Check it out");
                    //    continue;
                    //}

                    var PriceResponse = await client.GetPrice(sig.Symbol);
                    var latestPrice = PriceResponse.Price;

                    var mysellPrice = latestPrice + (latestPrice * 0.07M / 100); // setting the sell price to a tiny amount more than the current price.

                    player.QuantityBought = availableQty;
                    player.SoldCommision = mysellPrice * availableQty * 0.075M / 100;
                    player.TotalSoldAmount = mysellPrice * availableQty - player.SoldCommision;

                    if (isProd)
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
                    player.DayHigh = sig.DayHighPr;
                    player.DayLow = sig.DayLowPr;
                    player.CurrentPricePerCoin = sig.CurrPr;
                    player.TotalCurrentValue = player.CurrentPricePerCoin * player.QuantityBought;
                    player.QuantitySold = Convert.ToDecimal(player.QuantityBought);
                    player.AvailableAmountForTrading = player.TotalSoldAmount;
                    player.SellTime = DateTime.Now;
                    player.UpdatedTime = DateTime.Now;
                    player.SoldPricePricePerCoin = mysellPrice;
                    player.CandleOpenTimeAtSell = sig.CandleOpenTime;
                    player.BuyOrSell = "Sell";
                    player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
                    player.SellCandleId = sig.CandleId;

                    if (prDiffPerc > player.SellAbovePerc)
                    {
                        #region log profit sell

                        logger.Info("  " +
                           ProcessingTime +
                            " " + player.Name + player.Avatar +
                            " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                            " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                            " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                             " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
                            " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                            " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
                            " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
                            " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                            " > +" + player.SellAbovePerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                            " Profit Sell");
                        #endregion log profit sell
                        player.LossOrProfit = "Profit Sell";
                        totalConsecutivelosses -= 1;
                    }
                    else if (prDiffPerc < player.SellBelowPerc)
                    {
                        #region log loss sell

                        logger.Info("  " +
                        ProcessingTime +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                        " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                        " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                         " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
                        " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                        " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " < " + player.SellBelowPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " Loss Sell");

                        #endregion log loss sell
                        player.LossOrProfit = "Loss";
                        totalConsecutivelosses += 1;

                        logger.Info("  " + ProcessingTime + " TOtal Consecutive Loss" + totalConsecutivelosses);
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

                    TradeDB.Player.Update(player);
                }
                else
                {
                    #region log dont sell reason

                    logger.Info("  " +
                        ProcessingTime +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                        " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                        " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                         " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
                        " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                        " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " < " + player.SellAbovePerc.Deci().Rnd(1).ToString().PadRight(3, ' ') +
                        " and > " + player.SellBelowPerc.Deci().Rnd(1) + " Dont Sell");

                    #endregion  log dont sell reason
                }
            }

            #region redistribute balances to bots waiting to buy

            //Not required any more. This logic is covered in buy process now

            //if (isSaleHappen) // redistribute balances, in prod you would get the USDT balance and redistribute
            //{
            //    var availplayers = await TradeDB.Player.Where(x => x.IsTrading == false).ToListAsync();
            //    var avgAvailAmountForTrading = availplayers.Average(x => x.AvailableAmountForTrading);

            //    foreach (var player in availplayers)
            //    {
            //        player.AvailableAmountForTrading = avgAvailAmountForTrading;
            //        TradeDB.Player.Update(player);
            //    }

            //}

            #endregion redistribute balances to bots waiting to buy

            await TradeDB.SaveChangesAsync();

            logger.Info("Selling scan Completed at " + ProcessingTime );
            logger.Info("");
        }

        #endregion Prod


        private async Task Trade()
        {

            //if the last 5 trades were losses, it means the market is going downwards greatly. pause buying for 2 hours

            #region ProdRun

            DB db = new DB();
            var allplayers = await db.Player.ToListAsync();
            List<Signal> signals = new List<Signal>();
            MyCoins = await db.MyCoins.AsNoTracking().ToListAsync();


            #region GetSignals

            try
            {
                await GetSignals_Prod();
                ProcessingTime = CurrentSignals.FirstOrDefault().CandleCloseTime.ToString("dd-MMM HH:mm:ss");
            }
            catch (Exception ex)
            {
                logger.Error("Exception in signal Generator.Returning.. " + ex.Message);
                return;
            }

            #endregion  GetSignals

            #region Sells

            try // sell firsth, so that you can use the available spots to buy next. Otherwise you will wait longer to buy.
            {
                await Sell_Prod();
            }
            catch (Exception ex)
            {
                logger.Error("Exception in sell  " + ex.Message);
            }

            #endregion  Sells

            #region Buys

            try
            {
                if (totalConsecutivelosses < 3)
                {
                    await Buy_Prod();
                }
                else  // 3 last trades lost money, so its a downward trend. Wait for maxpauses reps (15 mins) before buying. Keep tracking to sell.
                {
                    logger.Info("Last 3 sells were at loss. Lets wait for " + (maxTotalPauses - totalcurrentPauses) * 15 + " more minutes before attempting to buy again");

                    totalcurrentPauses += 1;

                    if (totalcurrentPauses >= maxTotalPauses) 
                    {
                        logger.Info("Wait times over. Resetting losses and starting to trade again");
                        totalcurrentPauses = 0;
                        totalConsecutivelosses = 0;
                        await Buy_Prod();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Exception in Buy  " + ex.Message);
            }

            #endregion  Buys

         


            //   logger.Info("---------------Trading Completed for candle Time--------------" + latestCandleTime);

            #endregion ProdRun

            #region testRun

            //await ClearData();


            ////  DateTime latestCandleDate = db.Candle.AsNoTracking().Max(x => x.OpenTime); // for prod
            //DateTime startTime = new DateTime(2021, 3, 1, 23, 0, 0);
            //DateTime endTime = new DateTime(2021, 6, 18, 23, 0, 0);

            ////  List<string> bots = new List<string>() { "Diana", "Damien", "Shatlin", "Pepper", "Eevee" };

            //int i = 1;

            //using (var db = new DB())
            //{
            //    myCoins = await db.MyCoins.AsNoTracking().ToListAsync();

            //    while (startTime < endTime)
            //    {
            //        var allbots = await db.Player.ToListAsync();


            //        try
            //        {
            //            if (i % 24 == 0) // do it only once a day
            //            {
            //                foreach (var b in allbots)
            //                {
            //                    var starteddate = Convert.ToDateTime(b.CreatedDate);
            //                    var totaldayssinceStarted = (startTime - starteddate).Days;
            //                    var expectedProfit = Convert.ToDecimal(b.OriginalAllocatedValue);

            //                    for (int j = 0; j < totaldayssinceStarted; j++)
            //                    {
            //                        expectedProfit = (expectedProfit + (expectedProfit * 0.6M / 100)); //expecting 0.4% profit daily
            //                    }
            //                    b.TotalExpectedProfit = expectedProfit;
            //                }
            //                await db.SaveChangesAsync();
            //            }

            //            CurrentSignals = await GetSignals_QA(startTime);

            //            await Buy_QA(CurrentSignals);
            //            await Sell_QA(CurrentSignals);

            //            startTime = startTime.AddMinutes(15);

            //            i++;

            //        }
            //        catch (Exception ex)
            //        {

            //            logger.Error("Exception in trade " + ex.Message);
            //        }

            //    }

            //}


            //logger.Info("---------------Test Run for Trading Completed --------------");

            #endregion testRun
        }

    }
}
