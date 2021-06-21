﻿
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
        public DateTime ProcessingTime { get; set; }
        public string ProcessingTimeString { get; set; }
        public decimal? lastRoundsProfitPerc = 0;
        public int totalConsecutivelosses = 0;

        BinanceClient client;
        ILog logger;

        IMapper iMapr;
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
            TraderTimer.Start();
            lblBotName.Text = configr.Botname;
            logger = LogManager.GetLogger(typeof(MainWindow));


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

        private async Task<List<Candle>> GetCandle()
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

        private async Task<List<Signal>> GetSignals()
        {
            DB TradeDB = new DB();
            CurrentSignals = new List<Signal>();
            List<Candle> candles = await GetCandle();

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

            CurrentSignals = CurrentSignals.OrderByDescending(x => x.PrDiffCurrAndHighPerc).ToList();
            return CurrentSignals;
        }

        private async Task Buy()
        {
            //Optimzation: Use only signals whose pricediff> the obots buy conditions. Filtter and then use the filtered signals
            if (CurrentSignals == null || CurrentSignals.Count() == 0)
            {
                logger.Info("No signals found. returning from buying");
                return;
            }

            logger.Info("Buying scan Started at " + ProcessingTimeString);

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



            #endregion redistribute balances to bots waiting to buy


            foreach (var player in players)
            {
                #region if player is currently trading, just update stats and go to next player

                if (player.IsTrading)
                {
                    logger.Info("  " + ProcessingTimeString + " " + player.Name + player.Avatar + " currently occupied");

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
                    logger.Info("  " + ProcessingTimeString + " " + player.Name + player.Avatar + " Available amount " + player.AvailableAmountForTrading + " Not enough for trading");
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
                        //logger.Info("  " +
                        //sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
                        //" " + player.Name + player.Avatar +
                        //" " + sig.Symbol.Replace("USDT", "") +
                        //" is picked by another bot. look for another coin ");
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

                        var mybuyPrice = latestPrice - (latestPrice * configr.BufferPriceForBuyAndSell / 100); // setting the buy price to a tiny amount lesser than the current price.
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

            logger.Info("Buying scan Completed at " + ProcessingTime + ". Next scan at " + ProcessingTime.AddMinutes(configr.IntervalMinutes).ToString("dd-MMM HH:mm"));
            logger.Info("");
        }

        private async Task Sell()
        {
            decimal? currentRoundProfitPer=0;


            logger.Info("Selling scan Started at " + ProcessingTimeString);

            DB TradeDB = new DB();
            var players = await TradeDB.Player.OrderBy(x => x.Id).ToListAsync();

            foreach(var player in players)
            {
                var signal=CurrentSignals.Where(x=>x.Symbol==player.Pair).FirstOrDefault();

                var currentprice=signal.CurrPr;


                var sellPrice = currentprice + (currentprice * configr.BufferPriceForBuyAndSell / 100); // setting the sell price to a tiny amount more than the current price.

                var SoldCommision = sellPrice * player.QuantityBought * configr.CommisionAmount / 100;

                var TotalSoldAmount = sellPrice * player.QuantityBought - player.SoldCommision;

                var prDiffPerc = TotalSoldAmount.GetDiffPerc(player.TotalBuyCost);

                currentRoundProfitPer+= prDiffPerc;

            }

            if(currentRoundProfitPer >= lastRoundsProfitPerc) // You re continuing to make profit, so dont sell now
            {

                logger.Info("This round's profit percentage " + currentRoundProfitPer.Deci().Rnd(2) + " is getting bigger than "+ lastRoundsProfitPerc +" So dont sell now");
                lastRoundsProfitPerc=0;
                return;
            }
            else
            {
                logger.Info("This round's profit percentage " + currentRoundProfitPer.Deci().Rnd(2) + " is getting smaller than " + lastRoundsProfitPerc + " So aim to sell now");
                lastRoundsProfitPerc = 0;
            }

            foreach (var player in players)
            {
                #region conditions to go no further with sell

                if (!player.IsTrading)
                {
                    logger.Info("  " + ProcessingTimeString + " " + player.Name + player.Avatar + " is waiting to buy. Nothing to sell");
                    continue;
                }

                var pair = player.Pair;

                #endregion conditions to go no further with sell

                //update to set all these values in PlayerTrades

                var PriceResponse = await client.GetPrice(pair);
                var PriceChangeResponse = await client.GetDailyTicker(pair);

                var latestPrice = PriceResponse.Price;

                var mysellPrice = latestPrice + (latestPrice * configr.BufferPriceForBuyAndSell / 100); // setting the sell price to a tiny amount more than the current price.

                player.SoldCommision = mysellPrice * player.QuantityBought * configr.CommisionAmount / 100;
                player.TotalSoldAmount = mysellPrice * player.QuantityBought - player.SoldCommision;

                var prDiffPerc = player.TotalSoldAmount.GetDiffPerc(player.TotalBuyCost);

                lastRoundsProfitPerc+= prDiffPerc;

                if ((prDiffPerc > player.SellAbovePerc) || (prDiffPerc < player.SellBelowPerc))
                {
                    if (prDiffPerc < player.DontSellBelowPerc)
                    {
                        #region log not selling reason

                        logger.Info("  " +
                           ProcessingTimeString +
                           " " + player.Name + player.Avatar +
                           " " + pair.Replace("USDT", "").PadRight(7, ' ') +
                           " DHi   " + PriceChangeResponse.HighPrice.Rnd().ToString().PadRight(10, ' ') +
                           " DLo   " + PriceChangeResponse.LowPrice.Rnd().ToString().PadRight(10, ' ') +
                           " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
                           " CurPr " + latestPrice.Rnd().ToString().PadRight(10, ' ') +
                           " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(0) +
                           " > " + player.DontSellBelowPerc.Deci().Rnd(0) +
                           " Not selling");

                        #endregion log not selling reason
                        continue;
                    }


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
                            logger.Info("  " +
                            ProcessingTimeString +
                            " " + player.Name + player.Avatar +
                                 " " + pair.Replace("USDT", "").PadRight(7, ' ') +
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

                    if (prDiffPerc > player.SellAbovePerc)
                    {
                        #region log profit sell

                        logger.Info("  " +
                           ProcessingTimeString +
                            " " + player.Name + player.Avatar +
                              " " + pair.Replace("USDT", "").PadRight(7, ' ') +
                           " DHi   " + PriceChangeResponse.HighPrice.Rnd().ToString().PadRight(10, ' ') +
                           " DLo   " + PriceChangeResponse.LowPrice.Rnd().ToString().PadRight(10, ' ') +
                           " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
                           " CurPr " + latestPrice.Rnd().ToString().PadRight(10, ' ') +
                           " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
                            " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                            " > +" + player.SellAbovePerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                            " Profit Sell");
                        #endregion log profit sell
                        player.LossOrProfit = "Profit Sell";
                        configr.TotalConsecutiveLosses -= 1;
                    }
                    else if (prDiffPerc < player.SellBelowPerc)
                    {
                        #region log loss sell

                        logger.Info("  " +
                        ProcessingTimeString +
                        " " + player.Name + player.Avatar +
                         " " + pair.Replace("USDT", "").PadRight(7, ' ') +
                           " DHi   " + PriceChangeResponse.HighPrice.Rnd().ToString().PadRight(10, ' ') +
                           " DLo   " + PriceChangeResponse.LowPrice.Rnd().ToString().PadRight(10, ' ') +
                           " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
                           " CurPr " + latestPrice.Rnd().ToString().PadRight(10, ' ') +
                           " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
                        " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " < " + player.SellBelowPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " Loss Sell");

                        #endregion log loss sell
                        player.LossOrProfit = "Loss";
                        configr.TotalConsecutiveLosses += 1;

                        logger.Info("  " + ProcessingTimeString + " Total Consecutive Loss " + configr.TotalConsecutiveLosses);
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
                        ProcessingTimeString +
                        " " + player.Name + player.Avatar +
                          " " + pair.Replace("USDT", "").PadRight(7, ' ') +
                           " DHi   " + PriceChangeResponse.HighPrice.Rnd().ToString().PadRight(10, ' ') +
                           " DLo   " + PriceChangeResponse.LowPrice.Rnd().ToString().PadRight(10, ' ') +
                           " BuyCnPr " + player.BuyPricePerCoin.Deci().Rnd().ToString().PadRight(10, ' ') +
                           " CurPr " + latestPrice.Rnd().ToString().PadRight(10, ' ') +
                           " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
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

            logger.Info("Selling scan Completed at " + ProcessingTimeString);
            logger.Info("");
        }

        #endregion Prod

        private async Task Trade()
        {

            //if the last 5 trades were losses, it means the market is going downwards greatly. pause buying for 2 hours

            #region ProdRun

            DB db = new DB();
            var allplayers = await db.Player.ToListAsync();
            configr=await db.Config.FirstOrDefaultAsync();
            List<Signal> signals = new List<Signal>();
            MyCoins = await db.MyCoins.AsNoTracking().ToListAsync();

            var activePlayers = allplayers.Where(x => x.IsTrading == true);

            ProcessingTime = DateTime.Now;
            ProcessingTimeString= ProcessingTime.ToString("dd-MMM HH:mm:ss");

            #region GetSignals

          //  if (configr.TotalConsecutiveLosses < 3)
            {
                try
                {
                    await GetSignals();
                }
                catch (Exception ex)
                {
                    logger.Error("Exception in signal Generator.Returning.. " + ex.Message);
                    return;
                }
            }

            #endregion  GetSignals

            #region Sells

            if (activePlayers != null && activePlayers.Count() > 0)
            {
                try // sell first, so that you can use the available spots to buy next. Otherwise you will wait longer to buy.
                {
                    await Sell();
                }
                catch (Exception ex)
                {
                    logger.Error("Exception in sell  " + ex.Message);
                }
            }
            else
            {
                logger.Info("None of the players are active. Nothing bought so nothing to sell");
                logger.Info(" ");
            }

            #endregion  Sells

            #region Buys

            try
            {
                if (configr.TotalConsecutiveLosses < 3)
                {
                    await Buy();
                }
                else  
                {
                    logger.Info("Last 3 sells were at loss. Lets wait for " + 
                        (configr.MaxPauses - configr.TotalCurrentPauses) * 15 + " more minutes before attempting to buy again");

                    configr.TotalCurrentPauses += 1;

                    if (configr.TotalCurrentPauses >= configr.MaxPauses)
                    {
                        logger.Info("Wait times over. Resetting losses and starting to trade again");
                        configr.TotalCurrentPauses = 0;
                        configr.TotalConsecutiveLosses = 0;
                    }
                    db.Update(configr);
                    await db.SaveChangesAsync();
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
