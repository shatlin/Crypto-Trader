
using AutoMapper;
using BinanceExchange.API.Client;
using BinanceExchange.API.Enums;
using BinanceExchange.API.Models.Request;
using BinanceExchange.API.Models.Response;
using BinanceExchange.API.Models.Response.Abstract;
using BinanceExchange.API.Websockets;
using log4net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Trader.Models;

namespace Trader.MVVM.View
{

    public class CoinData
    {
        public string pair { get; set; } //s
        public string coinSymbol { get; set; } //b
        public decimal precision { get; set; } //i
        public string coinName { get; set; } //an
        public decimal openprice { get; set; } //o
        public decimal dayhigh { get; set; } //h
        public decimal daylow { get; set; } //l
        public decimal currentprice { get; set; } //c
        public decimal volume { get; set; } //v
        public decimal USDTVolume { get; set; } //qv
        public decimal totalCoinsInStorage { get; set; } //cs 
        public decimal MarketCap { get; set; }
    }

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

        public static decimal GetDiffPercBetnNewAndOld(this decimal newValue, decimal OldValue)
        {
            if (OldValue == 0) return 0M;
            return ((newValue - OldValue) / (OldValue)) * 100;
        }

        public static decimal? GetDiffPercBetnNewAndOld(this decimal? newValue, decimal? OldValue)
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
        public List<MyCoins> allCoins { get; set; }
        public DispatcherTimer TradeTimer;
        public DateTime TradeTime { get; set; }
        public string StrTradeTime { get; set; }
        public string NextTradeTime { get; set; }
        BinanceClient client;

        ILog logger;
        IMapper iPlayerMapper;

        List<string> boughtCoins = new List<string>();
        public Config configr = new Config();
        public ExchangeInfoResponse exchangeInfo = new ExchangeInfoResponse();
        public bool isControlCurrentlyInTradeMethod = false;
        public bool isRunning = false;

        public HomeView()
        {
            InitializeComponent();
            Startup();
        }

        private async void Startup()
        {

            if (isRunning == false)
            {
                isRunning = true;

                logger = LogManager.GetLogger(typeof(MainWindow));

                logger.Info("App Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));

                DB db = new DB();
                configr = await db.Config.FirstOrDefaultAsync();
                lblBotName.Text = configr.Botname;

                TradeTimer = new DispatcherTimer();
                TradeTimer.Tick += new EventHandler(TraderTimer_Tick);
                TradeTimer.Interval = new TimeSpan(0, 0, configr.IntervalMinutes);

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

                iPlayerMapper = playerMapConfig.CreateMapper();

                if (configr.IsProd)
                {
                    await RedistributeBalances();
                }
                else
                {
                    await RedistributeBalancesQA();
                }
                TradeTimer.Start();
                await Trade();
            }
        }

        private async Task<bool> ShouldSkipPlayerFromBuying(Player player)
        {
            DB db = new DB();



            if (player.IsTrading)
            {
                await UpdateActivePlayerStats(player);
                return true;
            }

            if (player.isBuyOrderCompleted) // before buying the buyordercompleted should be reset to false, so dont buy if its true
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " isBuyOrderCompleted is true. Cant use it to buy");
                return true;
            }

            if (!allCoins.Any(x => x.ForceBuy == true))
            {
                if (player.isBuyAllowed == false)
                {
                    if (configr.ShowBuyingFlowLogs)
                        logger.Info("  " + StrTradeTime + " " + player.Name + "  Not  Allowed for buying");
                    return true;
                }
                if (configr.IsBuyingAllowed == false)
                {
                    if (configr.ShowBuyingFlowLogs)
                        logger.Info("  " + StrTradeTime + " " + player.Name + "  overall system not  Allowed for buying");
                    return true;
                }
            }

            return false;
        }

        private async Task BuyTheCoin(Player playertobuy, string pair, bool marketbuy)
        {

            if (configr.IsProd)
            {
                await RedistributeBalances();
            }
            else
            {
                await RedistributeBalancesQA();
            }


            DB db = new DB();

            var player = await db.Player.Where(x => x.Name == playertobuy.Name).FirstOrDefaultAsync();


            if (player.AvailableAmountToBuy < configr.MinimumAmountToTradeWith)
            {
                logger.Info(pair + "  " + StrTradeTime + " " + player.Name + " Avl Amt " + player.AvailableAmountToBuy + " Not enough for buying ");
                return;
            }

            var PriceResponse = await client.GetPrice(pair);


            decimal mybuyPrice = PriceResponse.Price;

            //var orderbook = await client.GetOrderBook(sig.Symbol, false, 8);
            // decimal mybuyPrice = orderbook.Asks.Min(x => x.Price);

            //foreach (var bid in orderbook.Bids)
            //{
            //    logger.Info(player.Pair + " Price " + bid.Price + " Qty " + bid.Quantity);
            //}
            //logger.Info(player.Pair + " Maximum bid is " + orderbook.Bids.Max(x => x.Price));

            // LogBuy(player, sig);

            player.Pair = pair;

            var coin = allCoins.Where(x => x.Pair == pair).FirstOrDefault();

            var coinprecison = coin.TradePrecision;

            var quantity = (player.AvailableAmountToBuy.Deci() / mybuyPrice).Rnd(coinprecison);

            BaseCreateOrderResponse buyOrder = null;

            if (marketbuy)
            {
                if (configr.IsProd)
                {
                    buyOrder = await client.CreateOrder(new CreateOrderRequest()
                    {
                        //Quantity = quantity,
                        //Side = OrderSide.Buy,
                        //Symbol = player.Pair,
                        //Type = OrderType.Market

                        Price = mybuyPrice,
                        Quantity = quantity,
                        Side = OrderSide.Buy,
                        Symbol = player.Pair,
                        Type = OrderType.Limit,
                        TimeInForce = TimeInForce.GTC
                    });
                }

                MyCoins buycoin = allCoins.Where(x => x.Pair == player.Pair).FirstOrDefault();

                if (buycoin.ForceBuy == true)
                {
                    buycoin.ForceBuy = false;
                    db.MyCoins.Update(buycoin);
                }

                player.IsTrading = true;
                player.DayHigh = coin.DayHighPrice;
                player.DayLow = coin.DayLowPrice;
                player.BuyCoinPrice = mybuyPrice;
                player.Quantity = quantity;
                player.BuyCommision = player.AvailableAmountToBuy * configr.CommisionAmount / 100;
                player.TotalBuyCost = player.AvailableAmountToBuy + player.BuyCommision;
                player.CurrentCoinPrice = mybuyPrice;
                player.TotalCurrentValue = player.AvailableAmountToBuy; //exclude commision in the current value.
                player.BuyTime = DateTime.Now;
                player.SellBelowPerc = player.SellAbovePerc;

                if (configr.IsProd)
                {
                    player.BuyOrderId = buyOrder.OrderId;
                }
                else
                {
                    player.BuyOrderId = 12345; //QA
                }
                player.SellOrderId = 0;
                player.UpdatedTime = DateTime.Now;
                player.BuyOrSell = "Buy";
                player.SellTime = null;
                player.isSellAllowed = true;
                player.SellAtPrice = null;
                player.BuyAtPrice = null;
                player.IsTracked = true;
                player.SellCommision = player.BuyCommision;
                player.SellCoinPrice = mybuyPrice;
                player.ProfitLossAmt = (player.TotalCurrentValue - player.TotalBuyCost).Deci();
                player.TotalSellAmount = player.TotalBuyCost; // resetting available amount for trading
                player.AvailableAmountToBuy = 0; // bought, so no amount available to buy

                if (configr.IsProd)
                {
                    player.isBuyOrderCompleted = false;
                }
                else
                {
                    player.isBuyOrderCompleted = true;
                }

                player.RepsTillCancelOrder = 0;
                player.SellAbovePerc = configr.DefaultSellAbovePerc;
                player.SellBelowPerc = configr.DefaultSellAbovePerc;
                db.Player.Update(player);
                PlayerTrades playerHistory = iPlayerMapper.Map<Player, PlayerTrades>(player);
                playerHistory.Id = 0;
                await db.PlayerTrades.AddAsync(playerHistory);

                await db.SaveChangesAsync();

                logger.Info(player.Name +
                         " " + coin.Pair.Replace("USDT", "").ToString().PadRight(7, ' ') +
                         " Price " + coin.CurrentPrice.Rnd().ToString().PadRight(11, ' ') +
                         " PrCh% " + coin.DayPriceDiff.Rnd().ToString().PadRight(11, ' ') +
                         " Quantity  " + quantity.Rnd().ToString().PadRight(11, ' ') +
                         " Buying ");
            }

            //Send Buy Order


        }

        private async Task Buy()
        {
            DB db = new DB();

            var players = await db.Player.OrderBy(x => x.Id).ToListAsync();

            boughtCoins = await db.Player.Where(x => x.Pair != null).Select(x => x.Pair).ToListAsync();

            foreach (var player in players)
            {
                if (await ShouldSkipPlayerFromBuying(player) == true)
                    continue;

                var forcebuyCoin = allCoins.Where(x => x.ForceBuy == true).FirstOrDefault();

                if (forcebuyCoin != null)
                {
                    await BuyTheCoin(player, forcebuyCoin.Pair, true);
                    boughtCoins.Add(forcebuyCoin.Pair);
                    return;
                }
                else if (player.IsTrading == false && player.Pair != null && player.Pair.Length > 0 && player.BuyAtPrice != null && player.BuyAtPrice > 0)
                {
                    var coin = allCoins.Where(x => x.Pair == player.Pair).FirstOrDefault();

                    if (coin != null)
                    {
                        if (coin.CurrentPrice < player.BuyAtPrice)
                        {
                            logger.Info(player.Name + " Marked buying price " + player.BuyAtPrice.Deci().Rnd(7) + " reached for " + player.Pair + " .Buying");
                            await BuyTheCoin(player, coin.Pair, true);
                            boughtCoins.Add(coin.Pair);
                            return;
                        }
                        else
                        {
                            logger.Info(player.Name + " Marked buying price " + player.BuyAtPrice.Deci().Rnd(7) + " not reached for " + player.Pair + " . Not Buying. Current Price is " + coin.CurrentPrice.Rnd(7));
                        }
                    }
                }

                continue;

                //if (MySignals.Any(x => x.ForceBuy == true))
                //{
                //    logger.Info("Coins marked for buying manually, so not going to buy automatically for " + player.Name);
                //    continue;
                //}
                //foreach (Signal sig in MySignals.OrderBy(x => x.PrDiffCurrAndHighPerc).ToList())
                //{

                //    if ((sig.IsIgnored || sig.IsPicked || !sig.IsIncludedForTrading) && sig.ForceBuy == false)
                //        continue;




                //    if (boughtCoins.Contains(sig.Symbol))
                //    {
                //        sig.IsPicked = true;
                //        continue;
                //    }
                //    else
                //    {
                //        sig.IsPicked = false;
                //    }

                //    if (sig.IsBestTimeToBuyAtDayLowest || sig.ForceBuy) // () sig.IsBestTimeToBuyAtDayLowest
                //    {
                //        //if (IsBitCoinGoingDown())
                //        //    continue;

                //        if (IsCoinPriceGoingDown(sig))
                //        {
                //            sig.IsIgnored = true;
                //            continue;
                //        }
                //        else
                //        {
                //            sig.IsIgnored = false;
                //        }


                //        if (IsCoinTradeCountTooLow(sig))
                //        {
                //            sig.IsIgnored = true;
                //            continue;
                //        }
                //        else
                //        {
                //            sig.IsIgnored = false;
                //        }
                //        try
                //        {
                //            await BuyTheCoin(player, sig, false);
                //            sig.IsPicked = true;
                //            boughtCoins.Add(sig.Symbol);
                //            break;
                //        }
                //        catch (Exception ex)
                //        {
                //            logger.Error(" " + player.Name + " " + sig.Symbol + " Exception: " + ex.ToString());
                //        }
                //    }
                //    else
                //    {
                //        LogNoBuy(player, sig);
                //        sig.IsPicked = false;
                //    }
                //}
            }
        }

        private async Task Sell(Player player)
        {
            #region initial Selling Set up

            string Quantityvalue = string.Empty;
            string predecimal = string.Empty;
            string decimals = string.Empty;
            string subdecimals = string.Empty;
            bool isSold = false;
            DB db = new DB();

            if (ShouldReturnFromSelling(player) == true) return;

            var coin = allCoins.Where(x => x.Pair == player.Pair).FirstOrDefault();
            var pair = player.Pair;
            var mysellPrice = coin.CurrentPrice;
            player.DayHigh = coin.DayHighPrice;
            player.DayLow = coin.DayLowPrice;
            player.UpdatedTime = DateTime.Now;
            player.SellCoinPrice = mysellPrice;
            player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
            player.TotalSellAmount = mysellPrice * player.Quantity - player.SellCommision;
            player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
            player.CurrentCoinPrice = mysellPrice;
            player.TotalCurrentValue = player.TotalSellAmount;
            player.SellOrderId = 0;


            var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
            var NextSellbelow = prDiffPerc * configr.ReducePriceDiffPercBy / 100;

            player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";

            if (player.ProfitLossChanges.Length > 200)
                player.ProfitLossChanges = player.ProfitLossChanges.GetLast(200);

            #endregion  initial Selling Set up

            #region Sell Price is Set. Sell Now if met

            if (player.SellAtPrice != null && player.IsTrading && player.CurrentCoinPrice > player.SellAtPrice && player.isSellAllowed)
            {
                logger.Info(player.Name + " Marked price to sell " + player.SellAtPrice.Deci().Rnd(7) + " reached for " + player.Pair + " .Selling");
                player.ForceSell = true;
            }

            if (player.SellAtPrice != null && player.IsTrading && player.CurrentCoinPrice < player.SellAtPrice && player.isSellAllowed)
            {
                logger.Info(player.Name + " Marked price to sell " + player.SellAtPrice.Deci().Rnd(7) + " not yet reached for " + player.Pair + " Cant sell now. Current Price is " + player.CurrentCoinPrice.Rnd(7));
                player.ForceSell = false;
            }


            #endregion

            #region Met with loss. Force Sell

            if (player.IsTrading && prDiffPerc < player.LossSellBelow && player.isSellAllowed)
            {
                logger.Info(player.Name + " Met with Loss " + prDiffPerc.Deci().Rnd(7) + " for " + player.Pair + " .Selling");
                player.ForceSell = true;
            }

            #endregion

            #region Price Difference Less Than Sell Above

            if (prDiffPerc <= player.SellAbovePerc && player.ForceSell == false)
            {
                if (DateTime.Now.Minute == configr.ReduceSellAboveAtMinute &&
                   (DateTime.Now.Second >= configr.ReduceSellAboveFromSecond &&
                    DateTime.Now.Second <= configr.ReduceSellAboveToSecond))
                {
                    if (player.SellAbovePerc >= configr.MinSellAbovePerc)
                    {
                        if (configr.IsReducingSellAbvAllowed)
                            player.SellAbovePerc = player.SellAbovePerc - configr.ReduceSellAboveBy;
                    }
                }

                player.SellBelowPerc = player.SellAbovePerc;
                player.LastRoundProfitPerc = prDiffPerc;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            #endregion Price Difference Less Than Sell Above

            #region Price Difference Greater Than Sell Above And Greater Than Sell below

            if (prDiffPerc >= player.SellBelowPerc && player.ForceSell == false)
            {
                if (prDiffPerc > player.LastRoundProfitPerc && NextSellbelow > player.SellBelowPerc)
                    player.SellBelowPerc = NextSellbelow;
                player.LastRoundProfitPerc = prDiffPerc;
                player.AvailableAmountToBuy = 0;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            #endregion Price Difference Greater Than Sell Above And Greater Than Sell below

            #region Sell Not allowed

            if ((player.isSellAllowed == false || configr.IsSellingAllowed == false) && player.ForceSell == false)
            {
                player.AvailableAmountToBuy = 0;
                player.LastRoundProfitPerc = prDiffPerc;
                db.Player.Update(player);
                await db.SaveChangesAsync();
                return;
            }

            #endregion Sell Not allowed 

            #region Price Went Below Last Profitable Round - Sell

            if (prDiffPerc < player.LastRoundProfitPerc || player.ForceSell == true)
            {
                if (coin != null)
                {
                    logger.Info(player.Name +
                           " " + coin.Pair.Replace("USDT", "").ToString().PadRight(7, ' ') +
                            " prDiffPerc " + prDiffPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                            " < LastRoundProfitPerc  " + player.LastRoundProfitPerc.Deci().Rnd().ToString().PadRight(11, ' ') +
                            " selling ");
                }

                var PriceChangeResponse = await client.GetDailyTicker(pair);

                var orderbook = await client.GetOrderBook(player.Pair, false, 8);

                ////foreach (var bid in orderbook.Bids)
                ////{
                ////    logger.Info(player.Pair + " Price " + bid.Price + " Qty " + bid.Quantity);
                ////}
                ////logger.Info(player.Pair + " Maximum bid is " + orderbook.Bids.Max(x => x.Price));
                mysellPrice = orderbook.Bids.Max(x => x.Price);

                //  mysellPrice = PriceChangeResponse.LastPrice;

                player.DayHigh = PriceChangeResponse.HighPrice;
                player.DayLow = PriceChangeResponse.LowPrice;
                player.UpdatedTime = DateTime.Now;
                player.SellCoinPrice = mysellPrice;
                player.SellCommision = mysellPrice * player.Quantity * configr.CommisionAmount / 100;
                player.TotalSellAmount = mysellPrice * player.Quantity - player.SellCommision;
                player.ProfitLossAmt = (player.TotalSellAmount - player.TotalBuyCost).Deci();
                player.CurrentCoinPrice = mysellPrice;
                player.TotalCurrentValue = player.TotalSellAmount;

                var coinprecison = allCoins.Where(x => x.Pair == pair).FirstOrDefault().TradePrecision;

                BaseCreateOrderResponse sellOrder = null;

                if (configr.CrashSell == true || player.ForceSell == true)
                {

                    Quantityvalue = player.Quantity.Deci().ToString();

                    if (Quantityvalue.Contains("."))
                    {
                        predecimal = Quantityvalue.Substring(0, Quantityvalue.IndexOf('.'));
                        decimals = Quantityvalue.Substring(Quantityvalue.IndexOf('.'));
                        subdecimals = decimals.Substring(0, coinprecison + 1);
                        Quantityvalue = predecimal + subdecimals;
                    }


                    // logger.Info("Force Selling " + player.Pair + " Qty " + player.Quantity.Deci().Rnd(coinprecison));

                    //sellOrder = await client.CreateOrder(new CreateOrderRequest()
                    //{
                    //    // Price = mysellPrice,
                    //    Quantity = Convert.ToDecimal(Quantityvalue),
                    //    //  Quantity = player.Quantity.Deci().Rnd(coinprecison),
                    //    Side = OrderSide.Sell,
                    //    Symbol = player.Pair,
                    //    Type = OrderType.Market
                    //});
                    isSold = true;

                    if (configr.IsProd)
                    {
                        sellOrder = await client.CreateOrder(new CreateOrderRequest()
                        {
                            Price = mysellPrice,
                            Quantity = player.Quantity.Deci().Rnd(coinprecison),
                            Side = OrderSide.Sell,
                            Symbol = player.Pair,
                            Type = OrderType.Limit,
                            TimeInForce = TimeInForce.GTC
                        });

                        player.SellOrderId = sellOrder.OrderId;
                    }
                    else
                    {
                        player.SellOrderId = 12345; //QA
                    }

                }
                else
                {
                    isSold = true;
                    if (configr.IsProd)
                    {
                        sellOrder = await client.CreateOrder(new CreateOrderRequest()
                        {
                            Price = mysellPrice,
                            Quantity = player.Quantity.Deci().Rnd(coinprecison),
                            Side = OrderSide.Sell,
                            Symbol = player.Pair,
                            Type = OrderType.Limit,
                            TimeInForce = TimeInForce.GTC
                        });

                        player.SellOrderId = sellOrder.OrderId;
                    }

                    else
                    {
                        player.SellOrderId = 12345; //QA
                    }
                }

                player.SellTime = DateTime.Now;
                player.AvailableAmountToBuy = player.TotalSellAmount;
            }

            #endregion Price Went Below Last Profitable Round - Sell

            #region Price going Above Last Profitable Round - Dont Sell

            else
            {
                player.AvailableAmountToBuy = 0;
            }

            #endregion  Price going Above Last Profitable Round - Dont Sell

            #region final selling Set up

            player.LastRoundProfitPerc = prDiffPerc;
            player.ForceSell = false;
            db.Player.Update(player);
            await db.SaveChangesAsync();

            if (isSold && !configr.IsProd)
            {
                await UpdatePlayerAfterSellConfirmed(player);
            }

            #endregion final selling Set up
        }

        private bool ShouldReturnFromSelling(Player player)
        {
            DB db = new DB();

            if (player == null)
            {
                logger.Info("Player returned as null. Some issue. Returning from Sell");
                return true;
            }

            var pair = player.Pair;

            if (pair == null)
            {
                logger.Info("Player's Pair to sell returned as null. Some issue. Returning from Sell");
                return true;
            }

            var newPlayer = db.Player.AsNoTracking().Where(x => x.Name == player.Name).FirstOrDefault();

            if (newPlayer.IsTrading == false) return true;

            if (newPlayer.SellOrderId > 0)
            {
                logger.Info("Sell order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
                return true;
            }
            if (newPlayer.isBuyOrderCompleted == false)
            {
                logger.Info("Buy order still pending for " + newPlayer.Pair + " " + newPlayer.Name);
                return true;
            }
            var availableQty = player.Quantity;

            if (availableQty <= 0)
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ') + " Available Quantity 0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return true;
            }

            if (player.Quantity == null || player.Quantity.Deci() == 0)
            {
                logger.Info("  " + StrTradeTime + " " + player.Name + " " + pair.Replace("USDT", "").PadRight(7, ' ')
                + " player.Quantity  0 for " + " Symbol " + player.Pair + " Sell not possible ");
                return true;
            }

            return false;
        }

        private async void SellThisBot(object sender, RoutedEventArgs e)
        {
            DB db = new DB();
            PlayerViewModel model = (sender as Button).DataContext as PlayerViewModel;
            Player player = await db.Player.Where(x => x.Name == model.Name).FirstOrDefaultAsync();
            player.ForceSell = true;
            await Sell(player);
            // await SetGrid();
        }

        public async Task RedistributeBalances()
        {
            DB db = new DB();
            var players = await db.Player.AsNoTracking().ToListAsync();

            //decimal TotalAmount=0;

            //foreach (var player in players)
            //{
            //    TotalAmount = TotalAmount + player.TotalCurrentValue.Deci() + player.AvailableAmountToBuy.Deci();
            //}

            //decimal OvearallAverageAmount = TotalAmount / players.Count();

            var availplayers = await db.Player.Where(x => x.IsTrading == false).OrderBy(x => x.Id).ToListAsync();

            AccountInformationResponse accinfo = await client.GetAccountInformation();

            decimal TotalAvalUSDT = 0;

            var USDT = accinfo.Balances.Where(x => x.Asset == "USDT").FirstOrDefault();

            if (USDT != null)
            {
                TotalAvalUSDT = USDT.Free - (USDT.Free * 0.5M / 100); //Take only 98 % to cater for small differences
            }
            if (availplayers.Count() > 0)
            {
                var avgAvailAmountForTrading = TotalAvalUSDT / availplayers.Count();

                if (avgAvailAmountForTrading > configr.MaximumAmountForaBot)
                {
                    avgAvailAmountForTrading = configr.MaximumAmountForaBot;
                }

                foreach (var player in availplayers)
                {
                    player.AvailableAmountToBuy = avgAvailAmountForTrading;
                    player.TotalCurrentValue = 0;
                    db.Player.Update(player);
                }
                await db.SaveChangesAsync();
            }
        }

        public async Task RedistributeBalancesQA()
        {
            DB db = new DB();

            var availplayers = await db.Player.Where(x => x.IsTrading == false).OrderBy(x => x.Id).ToListAsync();

            var avgBalance = availplayers.Average(x => x.AvailableAmountToBuy);

            foreach (var player in availplayers)
            {
                player.AvailableAmountToBuy = avgBalance;
                player.TotalCurrentValue = 0;
                db.Player.Update(player);
            }
            await db.SaveChangesAsync();
        }

        public async Task CalculateBalances()
        {
            DB db = new DB();
            var activeplayers = await db.Player.Where(x => x.IsTrading == true).AsNoTracking().ToListAsync();

            AccountInformationResponse accinfo = await client.GetAccountInformation();

            decimal TotalAvalUSDT = 0;
            decimal TotalAvalBNB = 0;

            var USDT = accinfo.Balances.Where(x => x.Asset == "USDT").FirstOrDefault();
            if (USDT != null) TotalAvalUSDT = USDT.Free;

            var bnbcoin = allCoins.Where(x => x.Pair == "BNBUSDT").FirstOrDefault();
            var bnb = accinfo.Balances.Where(x => x.Asset == "BNB").FirstOrDefault();
            if (bnb != null) TotalAvalBNB = bnb.Free * bnbcoin.CurrentPrice;

            decimal currentValue = 0;

            foreach (var player in activeplayers)
                currentValue += (player.Quantity * player.SellCoinPrice).Deci();

            currentValue += TotalAvalUSDT + TotalAvalBNB;

            var investedAmt = (activeplayers.Sum(x => x.TotalBuyCost) + TotalAvalUSDT + TotalAvalBNB).Deci();

            logger.Info(" Total Invested : " + investedAmt.Rnd(0) +
                        " Current Value: " + currentValue.Rnd(0) +
                        " P/L : " + (currentValue - investedAmt).Rnd(0) +
                        " Avl USDT : " + TotalAvalUSDT.Rnd(0)
                        );
            logger.Info(" ");
        }

        private async Task CheckCrashToSellAll()
        {
            decimal totalinvested = 0;
            decimal totalcurrent = 0;
            decimal totalprofit = 0;

            decimal totalnontraderamount = 0;
            if (configr.ShouldSellWhenAllBotsAtLoss == false) return;

            using (var db = new DB())
            {
                var AllPlayer = await db.Player.ToListAsync();

                foreach (var pl in AllPlayer)
                {
                    if (!string.IsNullOrEmpty(pl.Pair))
                    {
                        totalcurrent += Convert.ToDecimal(pl.TotalCurrentValue);
                        totalinvested += Convert.ToDecimal(pl.TotalBuyCost);
                    }
                    else
                    {
                        totalcurrent += Convert.ToDecimal(pl.AvailableAmountToBuy);
                        totalinvested += Convert.ToDecimal(pl.AvailableAmountToBuy);
                        totalnontraderamount += Convert.ToDecimal(pl.AvailableAmountToBuy);
                    }
                }

                totalprofit = totalcurrent - totalinvested;
                decimal prlsperc = ((totalcurrent - totalinvested) / (totalinvested) * 100);


                if (prlsperc <configr.SellWhenAllBotsAtLossBelow)
                {
                    foreach (var player in AllPlayer.Where(x=>x.IsTrading==true))
                    {
                        player.ForceSell = true;
                        db.Player.Update(player);
                    }
                    configr.CrashSell = true;
                    configr.IsBuyingAllowed = false;
                    db.Config.Update(configr);
                    logger.Info("Total loss went below "+configr.SellWhenAllBotsAtLossBelow.Rnd(2) + "Selling ");
                }

                await db.SaveChangesAsync();
            }
        }
    

    public async Task UpdateActivePlayerStats(Player player)
    {
        DB db = new DB();

        var coin = allCoins.Where(x => x.Pair == player.Pair).FirstOrDefault();

        if (coin != null)
        {
            player.DayHigh = coin.DayHighPrice;
            player.DayLow = coin.DayLowPrice;
            player.CurrentCoinPrice = coin.CurrentPrice;
            player.TotalCurrentValue = player.CurrentCoinPrice * player.Quantity;
            player.TotalSellAmount = player.TotalCurrentValue;
            player.ProfitLossAmt = (player.TotalCurrentValue - player.TotalBuyCost).Deci();
            var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
            player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";

            if (player.ProfitLossChanges.Length > 200)
                player.ProfitLossChanges = player.ProfitLossChanges.GetLast(200);

            //      player.LastRoundProfitPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);
            player.AvailableAmountToBuy = 0;
            player.UpdatedTime = DateTime.Now;
            db.Player.Update(player);
            await db.SaveChangesAsync();
        }
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

                    if (orderResponse.Status == OrderStatus.PartiallyFilled)
                    {
                        //dont cancel.Wait indefinitely till filled
                    }

                    else if (orderResponse.Status != OrderStatus.Filled)
                    {
                        if (player.RepsTillCancelOrder > configr.MaxRepsBeforeCancelOrder)
                        {
                            CancelOrderRequest cancelrequest = new CancelOrderRequest();
                            cancelrequest.OrderId = orderResponse.OrderId;
                            cancelrequest.Symbol = orderResponse.Symbol;
                            await client.CancelOrder(cancelrequest);
                            await ClearPlayer(player);
                        }
                        else
                        {
                            player.RepsTillCancelOrder = player.RepsTillCancelOrder + 1;
                            //player.Quantity=await GetAvailQty(player,player.Pair);
                            //player.TotalBuyCost=player.Quantity*player.BuyCoinPrice+player.BuyCommision;
                            db.Player.Update(player);
                            await db.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        player.isBuyOrderCompleted = true;
                        player.ForceSell = false;
                        db.Player.Update(player);
                        await db.SaveChangesAsync();
                    }
                }

            }
        }

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
                    if (orderResponse.Status == OrderStatus.PartiallyFilled)
                    {
                        //dont cancel.Wait indefinitely till filled
                    }
                    else if (orderResponse.Status != OrderStatus.Filled)
                    {
                        if (player.RepsTillCancelOrder > configr.MaxRepsBeforeCancelOrder)
                        {
                            CancelOrderRequest cancelrequest = new CancelOrderRequest();
                            cancelrequest.OrderId = orderResponse.OrderId;
                            cancelrequest.Symbol = orderResponse.Symbol;
                            await client.CancelOrder(cancelrequest);
                            player.SellOrderId = 0;
                            player.RepsTillCancelOrder = 0;
                            db.Player.Update(player);
                            await db.SaveChangesAsync();
                        }
                        else
                        {
                            player.RepsTillCancelOrder = player.RepsTillCancelOrder + 1;
                            db.Player.Update(player);
                            await db.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        await UpdatePlayerAfterSellConfirmed(player);
                    }
                }

            }
        }

    }

    private async Task UpdatePlayerAfterSellConfirmed(Player player)
    {
        DB db = new DB();

        player.AvailableAmountToBuy = player.TotalSellAmount;

        var prDiffPerc = player.TotalSellAmount.GetDiffPercBetnNewAndOld(player.TotalBuyCost);

        if (prDiffPerc <= 0)
        {
            player.BuyOrSell = "Loss";
        }
        else
        {
            player.BuyOrSell = "Profit";
        }
        PlayerTrades PlayerTrades = iPlayerMapper.Map<Player, PlayerTrades>(player);
        PlayerTrades.Id = 0;
        await db.PlayerTrades.AddAsync(PlayerTrades);
        player.ForceSell = false;
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
        player.HardSellPerc = 0;
        player.isBuyOrderCompleted = false;
        player.RepsTillCancelOrder = 0;
        player.SellAbovePerc = configr.DefaultSellAbovePerc;
        player.SellBelowPerc = configr.DefaultSellAbovePerc;
        player.isSellAllowed = true;
        player.SellAtPrice = null;
        player.BuyAtPrice = null;
        player.IsTracked = true;
        db.Player.Update(player);
        await db.SaveChangesAsync();
        if (configr.IsProd)
        {
            await RedistributeBalances();
        }
        else
        {
            await RedistributeBalancesQA();
        }

    }

    private async Task UpdateCoins()
    {

        var binanceCoinData = await client.GetProducts();

        List<CoinData> coinDataList = new List<CoinData>();

        foreach (var data in binanceCoinData.Data)
        {
            if (data.s.EndsWith("USDT"))
            {
                if (data.s.EndsWith("UPUSDT") || data.s.EndsWith("DOWNUSDT") ||
                    data.s.EndsWith("BULLUSDT") || data.s.EndsWith("BEARUSDT") ||
                    data.s == "BUSDUSDT" || data.s == "USDCUSDT"
                    || data.s == "EURUSDT" || data.s == "DAIUSDT"
                    || data.s == "TUSDUSDT" || data.s == "AUDUSDT"
                    || data.s == "USTUSDT" || data.s == "GBPUSDT"
                    || data.s == "USDPUSDT" || data.s == "SUSDUSDT"
                    )
                {
                    continue;
                }

                CoinData coinData = new CoinData();
                coinData.pair = data.s;
                coinData.coinSymbol = data.b;
                coinData.precision = data.i.Deci();
                coinData.coinName = data.an;
                coinData.openprice = data.o.Deci();
                coinData.dayhigh = data.h.Deci();
                coinData.daylow = data.l.Deci();
                coinData.currentprice = data.c.Deci();
                coinData.volume = data.v.Deci();
                coinData.USDTVolume = data.qv.Deci();
                coinData.totalCoinsInStorage = data.cs.Deci();
                coinData.MarketCap = coinData.totalCoinsInStorage * coinData.currentprice;
                coinDataList.Add(coinData);
            }
        }


        using (var db = new DB())
        {
            List<string> coins = db.MyCoins.Select(x => x.Pair).ToList();
            int i = 0;

            foreach (var coindata in coinDataList.OrderByDescending(x => x.USDTVolume))
            {
                try
                {
                    i++;
                    if (!coins.Contains(coindata.pair))
                    {
                        MyCoins coin = new MyCoins();
                        coin.Pair = coindata.pair;
                        coin.IsIncludedForTrading = true;
                        coin.TradePrecision = coindata.precision.GetAllowedPrecision();
                        coin.PercAboveDayLowToSell = 13;
                        coin.PercBelowDayHighToBuy = -13;
                        coin.CoinName = coindata.coinName;
                        coin.CoinSymbol = coindata.coinSymbol;
                        coin.Rank = i;
                        coin.DayTradeCount = coindata.volume;
                        coin.DayVolume = coindata.volume;
                        coin.DayVolumeUSDT = coindata.USDTVolume;
                        coin.DayOpenPrice = coindata.openprice;
                        coin.DayHighPrice = coindata.dayhigh;
                        coin.DayLowPrice = coindata.daylow;
                        coin.CurrentPrice = coindata.currentprice;
                        coin.DayPriceDiff = coindata.currentprice.GetDiffPercBetnNewAndOld(coindata.openprice);
                        coin.FiveMinChange = 0M;
                        coin.TenMinChange = 0M;
                        coin.FifteenMinChange = 0M;
                        coin.ThirtyMinChange = 0M;
                        coin.OneHourChange = 0M;
                        coin.FourHourChange = 0M;
                        coin.TwentyFourHourChange = 0M;
                        coin.FortyEightHourChange = 0M;
                        coin.OneWeekChange = 0M;
                        coin.PrecisionDecimals = coindata.precision;
                        coin.MarketCap = coindata.MarketCap;
                        coin.TradeSuggestion = String.Empty;
                        await db.MyCoins.AddAsync(coin);
                    }
                    else
                    {
                        var coin = db.MyCoins.Where(x => x.Pair == coindata.pair).FirstOrDefault();
                        coin.TradePrecision = coindata.precision.GetAllowedPrecision();
                        coin.Rank = i;
                        coin.DayTradeCount = coindata.volume;
                        coin.DayVolume = coindata.volume;
                        coin.DayVolumeUSDT = coindata.USDTVolume;
                        coin.DayOpenPrice = coindata.openprice;
                        coin.DayHighPrice = coindata.dayhigh;
                        coin.DayLowPrice = coindata.daylow;
                        coin.CurrentPrice = coindata.currentprice;
                        coin.DayPriceDiff = coindata.currentprice.GetDiffPercBetnNewAndOld(coindata.openprice);
                        coin.PrecisionDecimals = coindata.precision;
                        coin.MarketCap = coindata.MarketCap;

                        db.MyCoins.Update(coin);

                    }
                }
                catch (Exception ex)
                {
                    logger.Info("Exception while updating coins " + ex.Message);
                }
            }
            await db.SaveChangesAsync();
        }
    }

    private async Task GetMyCoins()
    {
        using (var db = new DB())
        {
            allCoins = await db.MyCoins.OrderByDescending(x => x.DayPriceDiff).ToListAsync();
        }
    }

    private async Task Trade()
    {
        TradeTime = DateTime.Now;
        StrTradeTime = TradeTime.ToString("dd-MMM HH:mm:ss");
        DB db = new DB();
        configr = await db.Config.FirstOrDefaultAsync();
        NextTradeTime = TradeTime.AddSeconds(configr.IntervalMinutes).ToString("dd-MMM HH:mm:ss");
        await UpdateCoins();
        Thread.Sleep(500);

        await GetMyCoins();

        if (configr.IsProd)
        {
            await CalculateBalances();
            await UpdateTradeBuyDetails();
            await UpdateTradeSellDetails();
        }
        //  await AssessStateAndAct();
        await Buy();
        var activePlayers = await db.Player.AsNoTracking().OrderBy(x => x.Id).Where(x => x.IsTrading == true).ToListAsync();
        foreach (var player in activePlayers)
        {
            await Sell(player);
        }

        await CheckCrashToSellAll();

    //    logger.Info("");
    }

    #region low priority methods

    private async void TraderTimer_Tick(object sender, EventArgs e)
    {
        try
        {
            await Trade();
        }
        catch (Exception ex)
        {
            isControlCurrentlyInTradeMethod = false;
            logger.Error("Exception at trade " + ex.Message);
        }
    }

    private async void btnTrade_Click(object sender, RoutedEventArgs e)
    {
        await Trade();
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
        player.HardSellPerc = 0;
        player.isBuyOrderCompleted = false;
        player.RepsTillCancelOrder = 0;
        db.Player.Update(player);
        await db.SaveChangesAsync();
    }

    #endregion

}
    }
