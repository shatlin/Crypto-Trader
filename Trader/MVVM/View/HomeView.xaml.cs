
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

        public string candleOpenTime { get; set; }
        public string candleCloseTime { get; set; }

        BinanceClient client;
        ILog logger;
        int intervalMins = 15;
        double hourDifference = 2;
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

            SetGrid();
            CalculateBalanceSummary();

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
            logger.Info("Getting Candle Started at " + DateTime.Now.ToString("dd-MMM HH.mm"));
            DB candledb = new DB();
            List<Candle> candles = new List<Candle>();

            try
            {
                foreach (var coin in MyCoins)
                {
                    var pair = coin.Coin + "USDT";

                    Candle candle = new Candle();
                    var pricechangeresponse = await client.GetDailyTicker(pair);

                    var candleRequest = new GetKlinesCandlesticksRequest
                    {
                        Limit = 1,
                        Symbol = pair,
                        Interval = KlineInterval.FifteenMinutes
                    };

                    var candleResponse = await client.GetKlinesCandlesticks(candleRequest);
                    var firstCandle = candleResponse[0];

                    candle.RecordedTime = DateTime.Now;
                    candle.Symbol = pair;
                    candle.Open = firstCandle.Open;
                    candle.OpenTime = firstCandle.OpenTime.AddHours(hourDifference);
                    candle.High = firstCandle.High;
                    candle.Low = firstCandle.Low;
                    candle.Close = firstCandle.Close;
                    candle.Volume = firstCandle.Volume;
                    candle.CloseTime = firstCandle.CloseTime.AddHours(hourDifference);
                    candle.QuoteAssetVolume = firstCandle.QuoteAssetVolume;
                    candle.NumberOfTrades = firstCandle.NumberOfTrades;
                    candle.TakerBuyBaseAssetVolume = firstCandle.TakerBuyBaseAssetVolume;
                    candle.TakerBuyQuoteAssetVolume = firstCandle.TakerBuyQuoteAssetVolume;
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

                logger.Info("Getting Candle Completed at " + DateTime.Now.ToString("dd-MMM HH.mm"));
                logger.Info("");
            }
            catch (Exception ex)
            {
                logger.Info("Exception in Getting Candle  " + ex.Message);
                throw;
            }

            await candledb.SaveChangesAsync();
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

            logger.Info("Buying scan Started for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);

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


            #endregion redistribute balances to bots waiting to buy


            foreach (var player in players)
            {
                #region if player is currently trading, just update stats and go to next player

                if (player.IsTrading)
                {
                    logger.Info("  " + candleCloseTime + " " + player.Name + player.Avatar + " currently occupied");

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

                if(player.AvailableAmountForTrading<=70) 
                {
                    logger.Info("  " + candleCloseTime + " " + player.Name + player.Avatar + " Available amount "+ player.AvailableAmountForTrading + " Not enough for trading");
                    continue;
                }

                #endregion if player is currently trading, just update stats and go to next player

                foreach (var sig in CurrentSignals)
                {
                    #region conditions to no go further with buy

                    if (boughtCoins.Contains(sig.Symbol))
                    {
                        logger.Info("  " +
                             sig.CandleCloseTime.ToString("dd-MMM HH.mm") +
                            " " + player.Name + player.Avatar +
                            " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " coin already bought. Going to next signal ");
                        continue;
                    }

                    if (sig.IsPicked)
                    {
                        logger.Info("  " +
                        sig.CandleCloseTime.ToString("dd-MMM HH.mm") +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "") +
                        " is picked by another bot. look for another coin ");

                        continue;
                    }

                    #endregion  conditions to no go further with buy

                    if (sig.IsCloseToDayLow && (sig.PrDiffCurrAndHighPerc > player.BuyBelowPerc))
                    {
                        #region log the buy

                        logger.Info("  " +
                            sig.CandleCloseTime.ToString("dd-MMM HH.mm") +
                          " " + player.Name + player.Avatar +
                         " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                          " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                          " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                          " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                          " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
                          " < -" + player.BuyBelowPerc.Deci().Rnd(0) + " Buy Now");

                        #endregion log the buy

                        var mybuyPrice= sig.CurrPr - (sig.CurrPr*0.07M/100); // setting the buy price to a tiny amount lesser than the current price.
                        player.Pair = sig.Symbol;

                        // Create an order with varying options
                        var createOrder = await client.CreateOrder(new CreateOrderRequest()
                        {
                            Price = mybuyPrice,
                            Quantity = player.AvailableAmountForTrading.Deci() / mybuyPrice,
                            Side = OrderSide.Buy,
                            Symbol = player.Pair,
                            Type = OrderType.Limit,
                        });

                        player.IsTrading = true;
                     
                        player.DayHigh = sig.DayHighPr;
                        player.DayLow = sig.DayLowPr;
                        player.BuyPricePerCoin = mybuyPrice;
                        player.QuantityBought = player.AvailableAmountForTrading / mybuyPrice;
                        player.BuyingCommision = player.AvailableAmountForTrading * 0.075M / 100;
                        player.TotalBuyCost = player.BuyPricePerCoin * player.QuantityBought + player.BuyingCommision;
                        player.CurrentPricePerCoin = sig.CurrPr;
                        player.TotalCurrentValue = player.AvailableAmountForTrading;
                        player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
                        player.BuyTime = DateTime.Now;
                        player.AvailableAmountForTrading = 0; // resetting available amount for tradning
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
                        player.LossOrProfit = "BUY";
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

                        //logger.Info("  " +
                        //      sig.CandleCloseTime.ToString("dd-MMM HH.mm") +
                        //    " " + player.Name + player.Avatar +
                        //   " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                        //    " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                        //    " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                        //    " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                        //    " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
                        //    " > -" + player.BuyBelowPerc.Deci().Rnd(0) +
                        //    " Or not close to day low, Not Buying");

                        #endregion
                    }
                }
            }
            if (isdbUpdateRequired) await db.SaveChangesAsync();

            logger.Info("Buying scan Completed for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);
            logger.Info("");
        }

        private async Task Sell_Prod()
        {

            bool isSaleHappen = false;

            if (CurrentSignals == null || CurrentSignals.Count() == 0)
            {
                logger.Info("No signals found. returning from selling");
                return;
            }


            logger.Info("Selling scan Started for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);

            DB TradeDB = new DB();
            var players = await TradeDB.Player.OrderBy(x => x.Id).ToListAsync();

            foreach (var player in players)
            {
                #region conditions to go no further with sell

                if (!player.IsTrading)
                {
                    logger.Info("  " + candleCloseTime + " " + player.Name + player.Avatar + " is waiting to buy. Nothing to sell");
                    continue;
                }

                var pair = player.Pair;
                var sig = CurrentSignals.Where(x => x.Symbol == pair).FirstOrDefault();
                if (sig == null)
                {
                    logger.Info("  " + candleCloseTime + " " + player.Name + player.Avatar + " " + pair.Replace("USDT", "").PadRight(7, ' ') + " No signals returned. Continuing ");
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
                           candleCloseTime +
                           " " + player.Name + player.Avatar +
                           " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                           " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(10, ' ') +
                           " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(10, ' ') +
                           " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(10, ' ') +
                           " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(0) +
                           " > " + player.DontSellBelowPerc.Deci().Rnd(0) +
                           " Not selling");

                        #endregion log not selling reason
                        continue;
                    }

                    AccountInformationResponse accinfo = await client.GetAccountInformation();
                    decimal availableQty = 0;
                    
                    var coin=pair.Replace("USDT","");

                    var coinAvailable = accinfo.Balances.Where(x => x.Asset == coin).FirstOrDefault();

                    if(coinAvailable!=null)
                    {
                        availableQty = coinAvailable.Free;
                    }
                    else
                    {
                        logger.Info("  " +
                        candleCloseTime +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                        " not available in Binance. its unusal, so wont execute sell order. Check it out");

                        continue;
                    }

                    if(player.QuantityBought!=availableQty)
                    {
                        logger.Info("  " +
                           candleCloseTime +
                           " " + player.Name + player.Avatar +
                           " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                           " Player Quantity Bought " + player.QuantityBought +
                           " not equal to available qty in Binance " + availableQty +
                           " its unusal, so wont execute sell order. Check it out");
                        continue;
                    }

                    var mysellPrice = sig.CurrPr + (sig.CurrPr * 0.07M / 100); // setting the sell price to a tiny amount more than the current price.

                    player.SoldCommision = mysellPrice * player.QuantityBought * 0.075M / 100;
                    player.TotalSoldAmount = mysellPrice * player.QuantityBought - player.SoldCommision;


                   
                    var createOrder = await client.CreateOrder(new CreateOrderRequest()
                    {
                        Price = mysellPrice,
                        Quantity = player.QuantityBought.Deci(),
                        Side = OrderSide.Sell,
                        Symbol = player.Pair,
                        Type = OrderType.Limit,
                    });

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
                    player.BuyOrSell = "SELL";
                    player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
                    player.SellCandleId = sig.CandleId;

                    if (prDiffPerc > player.SellAbovePerc)
                    {
                        #region log profit sell

                        logger.Info("  " +
                           candleCloseTime +
                            " " + player.Name + player.Avatar +
                            " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                            " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                            " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                            " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                            " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
                            " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
                            " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                            " > +" + player.SellAbovePerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                            " Profit Sell");
                        #endregion log profit sell
                        player.LossOrProfit = "Profit";
                    }
                    else if (prDiffPerc < player.SellBelowPerc)
                    {
                        #region log loss sell

                        logger.Info("  " +
                        candleCloseTime +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                        " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                        " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                        " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                        " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " < " + player.SellBelowPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " loss sell");

                        #endregion log loss sell
                        player.LossOrProfit = "Loss";
                    }

                    // execute sell order (in live system)


                    PlayerTrades PlayerHist = iMapr.Map<Player, PlayerTrades>(player);
                    PlayerHist.Id = 0;
                    await TradeDB.PlayerTrades.AddAsync(PlayerHist);

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
                    isSaleHappen = true;
                }
                else
                {
                    #region log dont sell reason

                    logger.Info("  " +
                        candleCloseTime +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                        " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                        " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
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

            if (isSaleHappen) // redistribute balances, in prod you would get the USDT balance and redistribute
            {
                var availplayers = await TradeDB.Player.Where(x => x.IsTrading == false).ToListAsync();
                var avgAvailAmountForTrading = availplayers.Average(x => x.AvailableAmountForTrading);

                foreach (var player in availplayers)
                {
                    player.AvailableAmountForTrading = avgAvailAmountForTrading;
                    TradeDB.Player.Update(player);
                }

            }

            #endregion redistribute balances to bots waiting to buy

            await TradeDB.SaveChangesAsync();

            logger.Info("Selling scan Completed for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);
            logger.Info("");
        }

        #endregion Prod

        #region QA
        private async void btnClearPlayer_Click(object sender, RoutedEventArgs e)
        {
            await ClearData();

        }

        private async void btnCollectData_Click(object sender, RoutedEventArgs e)
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

                        logger.Info(i + " : " + file + " Processing Completed ");


                    }
                    await candledb.SaveChangesAsync();
                }
            }

            logger.Info("----------All file Processing Completed-------------- ");
            await UpdateData();
        }

        private async Task UpdateData()
        {
            DateTime currentdate = new DateTime(2021, 3, 1, 23, 0, 0);
            DateTime lastdate = new DateTime(2021, 6, 18, 23, 0, 0);


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

                            selectedCandles = await TradeDB.Candle.Where(x => x.Symbol.Contains(favtrade.Coin) && x.OpenTime.Date == currentdate.Date
                            ).ToListAsync();

                            if (selectedCandles == null || selectedCandles.Count == 0) continue;

                            foreach (var candle in selectedCandles)
                            {
                                candle.DayHighPrice = selectedCandles.Max(x => x.High);
                                candle.DayLowPrice = selectedCandles.Min(x => x.Low);
                                candle.DayVolume = selectedCandles.Sum(x => x.Volume);
                                candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
                                TradeDB.Candle.Update(candle);
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
                player.SaleProfitOrLoss = 0;
                player.LossOrProfit = string.Empty;
                TradeDB.Update(player);
            }
            await TradeDB.SaveChangesAsync();


        }

        private async Task<List<Candle>> GetCandles_QA()
        {
            logger.Info("Getting Candle Started at " + DateTime.Now.ToString("dd-MMM HH.mm"));
            //#TODO Update bots with current price when getting candles.

            DB candledb = new DB();


            List<Candle> candles = new List<Candle>();

            try
            {

                var StartlastCandleMinute = candledb.Candle.Max(x => x.OpenTime);

                MyCoins = await candledb.MyCoins.ToListAsync();

                var prices = await client.GetAllPrices();
                // await UpdateBalance(prices);



                #region get all missing candles

                var totalmins = (DateTime.Now - StartlastCandleMinute).TotalMinutes;

                if (totalmins > 30) //means you missed to get the last candle, so get those first.
                {
                    logger.Info("    Candles missed. Collecting them ");

                    foreach (var coin in MyCoins)
                    {
                        var pricesofcoin = prices.Where(x => x.Symbol.Contains(coin.Coin + "USDT"));
                        if (pricesofcoin == null || pricesofcoin.Count() == 0)
                        {
                            continue;
                        }
                        foreach (var price in pricesofcoin)
                        {

                            GetKlinesCandlesticksRequest cr = new GetKlinesCandlesticksRequest();
                            cr.Limit = 200;
                            cr.Symbol = price.Symbol;
                            cr.Interval = KlineInterval.FifteenMinutes;
                            cr.StartTime = Convert.ToDateTime(StartlastCandleMinute).AddMinutes(15);
                            cr.EndTime = DateTime.Now.AddMinutes(-15);
                            var candleresponse = await client.GetKlinesCandlesticks(cr);

                            foreach (var candleResp in candleresponse)
                            {
                                Candle addCandle = new Candle();

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

                                var isCandleExisting = await candledb.Candle.Where(x => x.OpenTime == addCandle.OpenTime && x.Symbol == addCandle.Symbol).FirstOrDefaultAsync();

                                if (isCandleExisting == null)
                                {
                                    candles.Add(addCandle);
                                    await candledb.Candle.AddAsync(addCandle);
                                    await candledb.SaveChangesAsync();
                                }
                            }
                        }
                        Thread.Sleep(1000);
                    }

                    var UpdatedlastCandleMinutes = candledb.Candle.Max(x => x.OpenTime);
                    List<Candle> selectedCandles;
                    while (StartlastCandleMinute <= UpdatedlastCandleMinutes)
                    {
                        try
                        {
                            foreach (var favtrade in MyCoins)
                            {
                                selectedCandles = await candledb.Candle.Where(x => x.Symbol.Contains(favtrade.Coin) && x.OpenTime.Date == StartlastCandleMinute.Date).ToListAsync();

                                if (selectedCandles == null || selectedCandles.Count == 0) continue;

                                foreach (var candle in selectedCandles)
                                {
                                    candle.DayHighPrice = selectedCandles.Max(x => x.High);
                                    candle.DayLowPrice = selectedCandles.Min(x => x.Low);
                                    candle.DayVolume = selectedCandles.Sum(x => x.Volume);
                                    candle.DayTradeCount = selectedCandles.Sum(x => x.NumberOfTrades);
                                    candledb.Candle.Update(candle);
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
                    logger.Info("    Collecting missed candles completed ");
                }
                #endregion get all missing candles

                foreach (var coin in MyCoins)
                {
                    var pricesofcoin = prices.Where(x => x.Symbol.Contains(coin.Coin + "USDT"));
                    if (pricesofcoin == null || pricesofcoin.Count() == 0)
                    {
                        continue;
                    }
                    foreach (var price in pricesofcoin)
                    {

                        Candle candle = new Candle();
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

                        var isCandleExisting = await candledb.Candle.Where(x => x.OpenTime == candle.OpenTime && x.Symbol == candle.Symbol).FirstOrDefaultAsync();

                        if (isCandleExisting == null)
                        {
                            candles.Add(candle);
                            await candledb.Candle.AddAsync(candle);
                            await candledb.SaveChangesAsync();
                        }
                        else
                        {
                            candledb.Candle.Update(isCandleExisting);
                            await candledb.SaveChangesAsync();
                        }
                    }
                }

                logger.Info("Getting Candle Completed at " + DateTime.Now.ToString("dd-MMM HH.mm"));
                logger.Info("");
            }
            catch (Exception ex)
            {
                logger.Info("Exception in Getting Candle  " + ex.Message);
                throw;
            }
            return candles;
        }

        private async Task<List<Signal>> GetSignals_QA(DateTime cndlHr)
        {

            DB TradeDB = new DB();

            List<Signal> signals = new List<Signal>();

            List<Candle> latestCndls = await TradeDB.Candle.AsNoTracking().Where(x => x.OpenTime == cndlHr).ToListAsync();

            // DateTime refCandlMinTime = currentCandleSetDate.AddHours(-23);

            List<Candle> refCndls = await TradeDB.Candle.AsNoTracking()
                .Where(x => x.OpenTime >= cndlHr.AddHours(-23) && x.OpenTime < cndlHr).ToListAsync();

            foreach (var myfavcoin in MyCoins)
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
                sig.CandleCloseTime = selCndl.CloseTime;
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

        private async Task Buy_QA(List<Signal> Signals)
        {

            /*
             *Step 1: Get All balances and apart from USDT, ensure they correspond to your player table.
             *Step 2: coins other than USDT are your "Actively Trading Coins"
             *Step 3: Once the matching is done,ignore anything in binance ( Could be from staking)
             *Step 4: Divide USDT to available bots, but leave 5% in account to cater for inconsistencies, this is what is available for them to buy. Update all Player fields.
             *
             *Remember: Get all signals, but just before issuing a buy order, get current price and do "the" checks.
             *Remember:Always issue limit order, so that you can record the exact price for which you bought for 
                In Prod, I would just need the latest candle list and no references.

             */

            if (Signals == null || Signals.Count() == 0)
            {
                logger.Info("No signals found. returning from buying");
                return;
            }

            var candleCloseTime = Signals.FirstOrDefault().CandleCloseTime.ToString("dd-MMM HH.mm");
            var candleOpenTime = Signals.FirstOrDefault().CandleOpenTime.ToString("dd-MMM HH.mm");

            logger.Info("Buying scan Started for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);
            #region definitions

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

                    logger.Info("  " + candleCloseTime + " " + player.Name + player.Avatar + " currently occupied");

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

                    if (boughtCoins.Contains(sig.Symbol))
                    {
                        logger.Info("  " +
                             sig.CandleCloseTime.ToString("dd-MMM HH.mm") +
                            " " + player.Name + player.Avatar +
                            " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " coin already bought. Going to next signal ");
                        continue;
                    }

                    if (sig.IsPicked)
                    {
                        logger.Info("  " +
                        sig.CandleCloseTime.ToString("dd-MMM HH.mm") +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "") +
                        " is picked by another bot. look for another coin ");

                        continue;
                    }


                    //buying criteria
                    //1. signals are ordered by the coins whose current price are at their lowest at the moment
                    //2. See if this price is the lowest in the last 24 hours
                    //3. See if the price difference is lower than what the player is expecting to buy at. If yes, buy.

                    //Later see if you are on a downtrend and keep waiting till it reaches its low and then buy

                    if (sig.IsCloseToDayLow && (sig.PrDiffCurrAndHighPerc > player.BuyBelowPerc))
                    {

                        logger.Info("  " +
                            sig.CandleCloseTime.ToString("dd-MMM HH.mm") +
                          " " + player.Name + player.Avatar +
                         " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                          " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                          " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                          " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                          " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
                          " < -" + player.BuyBelowPerc.Deci().Rnd(0) + " Buy Now");


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
                        PlayerTrades playerHistory = iMapr.Map<Player, PlayerTrades>(player);
                        playerHistory.Id = 0;
                        await db.PlayerTrades.AddAsync(playerHistory);
                        isdbUpdateRequired = true; //flag that db needs to be updated, and update it at the end
                        boughtCoins.Add(sig.Symbol);

                        break;
                    }

                    else
                    {
                        logger.Info("  " +
                              sig.CandleCloseTime.ToString("dd-MMM HH.mm") +
                            " " + player.Name + player.Avatar +
                           " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                            " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                            " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                            " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
                            " > -" + player.BuyBelowPerc.Deci().Rnd(0) +
                            " Or not close to day low, Not Buying");
                    }
                }
            }

            if (isdbUpdateRequired) await db.SaveChangesAsync();


            logger.Info("Buying scan Completed for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);

            logger.Info("");
        }

        private async Task Sell_QA(List<Signal> Signals)
        {

            bool isSaleHappen = false;

            if (Signals == null || Signals.Count() == 0)
            {
                logger.Info("No signals found. returning from selling");
                return;
            }

            var candleCloseTime = Signals.FirstOrDefault().CandleCloseTime.ToString("dd-MMM HH.mm");
            var candleOpenTime = Signals.FirstOrDefault().CandleOpenTime.ToString("dd-MMM HH.mm");


            logger.Info("Selling scan Started for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);

            DB TradeDB = new DB();
            var players = await TradeDB.Player.OrderBy(x => x.Id).ToListAsync();

            foreach (var player in players)
            {

                if (!player.IsTrading)
                {
                    logger.Info("  " + candleCloseTime +
                     " " + player.Name + player.Avatar +
                     "  waiting to buy. Nothing to sell");
                    continue;
                }

                var CoinPair = player.Pair;
                var sig = Signals.Where(x => x.Symbol == CoinPair).FirstOrDefault();


                if (sig == null)
                {
                    logger.Info("  " + candleCloseTime +
                     " " + player.Name + player.Avatar +
                     " " + CoinPair.Replace("USDT", "").PadRight(7, ' ') +
                     " No signals returned. Continuing ");
                    continue;
                }

                //update to set all these values in PlayerHist
                player.TotalBuyCost = player.BuyPricePerCoin * player.QuantityBought + player.BuyingCommision;
                player.SoldCommision = player.CurrentPricePerCoin * player.QuantityBought * 0.075M / 100;
                player.TotalSoldAmount = player.TotalCurrentValue - player.SoldCommision;

                var prDiffPerc = player.TotalSoldAmount.GetDiffPerc(player.TotalBuyCost);


                if ((prDiffPerc > player.SellAbovePerc) || (prDiffPerc < player.SellBelowPerc))
                {

                    if (prDiffPerc < player.DontSellBelowPerc)
                    {
                        logger.Info("  " +
                           candleCloseTime +
                           " " + player.Name + player.Avatar +
                           " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                           " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(10, ' ') +
                           " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(10, ' ') +
                           " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(10, ' ') +
                           " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
                           " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(0) +
                           " > " + player.DontSellBelowPerc.Deci().Rnd(0) +
                           " Not selling");

                        continue; //not selling for this player, but continuing for other players
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
                    player.SoldPricePricePerCoin = sig.CurrPr;
                    player.CandleOpenTimeAtSell = sig.CandleOpenTime;
                    player.BuyOrSell = "SELL";
                    player.TotalCurrentProfit = player.AvailableAmountForTrading - player.OriginalAllocatedValue;
                    player.SellCandleId = sig.CandleId;

                    if (prDiffPerc > player.SellAbovePerc)
                    {
                        logger.Info("  " +
                           candleCloseTime +
                            " " + player.Name + player.Avatar +
                            " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                      " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                      " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                      " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                      " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
                      " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
                      " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                            " > +" + player.SellAbovePerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                            " Prof Sell");

                        player.LossOrProfit = "Profit";
                    }
                    else if (prDiffPerc < player.SellBelowPerc)
                    {

                        logger.Info("  " +
                      candleCloseTime +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                        " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                        " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                        " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                        " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " < " + player.SellBelowPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " loss sell");

                        player.LossOrProfit = "Loss";
                    }

                    // create sell order (in live system)


                    PlayerTrades PlayerHist = iMapr.Map<Player, PlayerTrades>(player);
                    PlayerHist.Id = 0;
                    await TradeDB.PlayerTrades.AddAsync(PlayerHist);

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
                    isSaleHappen = true;
                }
                else
                {

                    logger.Info("  " +
                        candleCloseTime +
                        " " + player.Name + player.Avatar +
                        " " + sig.Symbol.Replace("USDT", "").PadRight(7, ' ') +
                        " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
                        " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
                        " CurPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
                        " BuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " SlPr  " + player.TotalSoldAmount.Deci().Rnd().ToString().PadRight(12, ' ') +
                        " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
                        " < " + player.SellAbovePerc.Deci().Rnd(1).ToString().PadRight(3, ' ') +
                        " and > " + player.SellBelowPerc.Deci().Rnd(1) + " Dont Sell");
                }
            }

            if (isSaleHappen) // redistribute balances
            {
                var availplayers = await TradeDB.Player.Where(x => x.IsTrading == false).ToListAsync();
                var avgAvailAmountForTrading = availplayers.Average(x => x.AvailableAmountForTrading);

                foreach (var player in availplayers)
                {
                    player.AvailableAmountForTrading = avgAvailAmountForTrading;
                    TradeDB.Player.Update(player);
                }

            }

            await TradeDB.SaveChangesAsync();

            logger.Info("Selling scan Completed for candle: Open time " + candleOpenTime + " Close Time " + candleCloseTime);
            logger.Info("");
        }

        #endregion QA


        private async Task Trade()
        {

            #region ProdRun

            DB db = new DB();
            var allplayers = await db.Player.ToListAsync();
            List<Signal> signals = new List<Signal>();
            MyCoins = await db.MyCoins.AsNoTracking().ToListAsync();


            #region GetSignals

            try
            {
                await GetSignals_Prod();
                candleOpenTime = CurrentSignals.FirstOrDefault().CandleOpenTime.ToString("dd-MMM HH.mm");
                candleCloseTime = CurrentSignals.FirstOrDefault().CandleCloseTime.ToString("dd-MMM HH.mm");
            }
            catch (Exception ex)
            {
                logger.Error("Exception in signal Generator.Returning.. " + ex.Message);
                return;
            }

            #endregion  GetSignals

            #region Buys

            try
            {
                await Buy_Prod();
            }
            catch (Exception ex)
            {
                logger.Error("Exception in Buy  " + ex.Message);
            }

            #endregion  Buys

            #region Sells

            try
            {
                await Sell_Prod();
            }
            catch (Exception ex)
            {
                logger.Error("Exception in sell  " + ex.Message);
            }

            #endregion  Sells


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
