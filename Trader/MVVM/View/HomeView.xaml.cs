
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
        public DateTime ProcessingTime { get; set; }
        public string ProcessingTimeString { get; set; }
        public string NextProcessingTimeString { get; set; }
        public int totalConsecutivelosses = 0;
        BinanceClient client;
        public List<PlayerViewModel> PlayerViewModels;
        int backupOldCandlesCounter = 1;
        public int UpdatePrecisionCounter = 0;
        ILog logger;
        IMapper iPlaymerMapper;
        IMapper iCandleMapper;
        List<string> boughtCoins = new List<string>();
        List<Signal> CurrentSignals = new List<Signal>();
        public Config configr = new Config();
        public bool ForceSell=false;

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

            TradeTimer.Start();
            await SetGrid();

            logger.Info("Application Started and Timer Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));

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
            decimal? totProfit=0;
            PlayerViewModels = new List<PlayerViewModel>();

            var players = await db.Player.Where(x => x.IsTrading == true).ToListAsync();

            var totalplayers = players.Count();

            foreach (var player in players)
            {
                PlayerViewModel playerViewModel = new PlayerViewModel();

                var pair = player.Pair;
                playerViewModel.Name = player.Name;
                playerViewModel.Pair = pair;
                playerViewModel.BuyPricePerCoin = player.BuyCoinPrice;
                playerViewModel.CurrentPricePerCoin=player.CurrentCoinPrice;
                playerViewModel.QuantityBought = player.Quantity;
                playerViewModel.BuyTime = Convert.ToDateTime(player.BuyTime).ToString("dd-MMM HH:mm");
                playerViewModel.SellBelowPerc = player.SellBelowPerc;
                playerViewModel.SellAbovePerc = player.SellAbovePerc;
                playerViewModel.TotalBuyCost = player.TotalBuyCost;
                playerViewModel.TotalSoldAmount = player.TotalSellAmount;
                playerViewModel.TotalCurrentValue = player.TotalCurrentValue;
                var prDiffPerc = player.TotalCurrentValue.GetDiffPerc(player.TotalBuyCost);
                totProfitPerc += prDiffPerc;
                totProfit+= (player.TotalCurrentValue- player.TotalBuyCost);
                playerViewModel.CurrentRoundProfitPerc = prDiffPerc;
                playerViewModel.LastRoundProfitPerc = player.LastRoundProfitPerc;
                playerViewModel.ProfitLossChanges = player.ProfitLossChanges.GetLast(80);
                PlayerViewModels.Add(playerViewModel);
            }

            PlayerGrid.ItemsSource = PlayerViewModels.OrderByDescending(x => x.CurrentRoundProfitPerc);

            if (totalplayers > 0)
            {
                lblAvgProfLoss.Text =   "Profit: "+ totProfit.Deci().Rnd(2) + "  ("+ totProfitPerc.Deci().Rnd(2)+ "%) ";
            }

            lblLastRun.Text = "Last Trade : " + ProcessingTimeString;
            lblNextRun.Text = "Next : " + NextProcessingTimeString;
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
            }
            await TradeDB.SaveChangesAsync();
        }

        private async void SellThisBot(object sender, RoutedEventArgs e)
        {
            DB db = new DB();
            PlayerViewModel model = (sender as Button).DataContext as PlayerViewModel;
            Player player = await db.Player.Where(x => x.Pair == model.Pair).FirstOrDefaultAsync();
            ForceSell = true;
            await Sell(player);
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
            logger.Info("  Getting Candle Started at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
            DB candledb = new DB();
            List<Candle> candles = new List<Candle>();
            try
            {
                foreach (var coin in MyCoins)
                {
                    var pair = coin.Coin + "USDT";

                    Candle candle = new Candle();
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
                logger.Info("  Exception in Getting Candle  " + ex.Message);
                throw;
            }

            await candledb.SaveChangesAsync();
            logger.Info("  Getting Candle Completed at " + DateTime.Now.ToString("dd-MMM HH:mm:ss"));
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

            CurrentSignals = CurrentSignals.OrderByDescending(x => x.DayTradeCount).ToList();
            return CurrentSignals;
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
                TotalAvalUSDT = USDT.Free - (USDT.Free * 5 / 100); //Take only 95 % to cater for small differences
            }
            if (availplayers.Count() > 0)
            {
                var avgAvailAmountForTrading = TotalAvalUSDT / availplayers.Count();

                foreach (var player in availplayers)
                {
                    player.AvailableAmountToBuy = avgAvailAmountForTrading;
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
                player.TotalSellAmount= player.TotalCurrentValue;
                var prDiffPerc = player.TotalSellAmount.GetDiffPerc(player.TotalBuyCost);
                player.ProfitLossChanges += prDiffPerc.Deci().Rnd(2) + " , ";
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

            // logger.Info("  Current price of coin " + sig.Symbol + " at  " + DateTime.Now.ToString("dd-MMM HH:mm:ss") + " is " + mybuyPrice);

            //  var mybuyPrice = latestPrice - (latestPrice * configr.BufferPriceForBuyAndSell / 100); // set  buy price to a tiny lesser than the current price.

            player.Pair = sig.Symbol;

            var coin = MyCoins.Where(x => x.Coin + "USDT" == sig.Symbol).FirstOrDefault();

            var coinprecison = coin.TradePrecision;

            var quantity = (player.AvailableAmountToBuy.Deci() / mybuyPrice).Rnd(coinprecison);

            //logger.Info(" Bought coin " + sig.Symbol + " at  " + DateTime.Now.ToString("dd-MMM HH:mm:ss") +
            //    " for price " + mybuyPrice + " Quantity = " + quantity +
            //    " and it costed " + player.AvailableAmountToBuy);



            var buyOrder = await client.CreateOrder(new CreateOrderRequest()
            {
                //Price = mybuyPrice,
                Quantity = quantity,
                Side = OrderSide.Buy,
                Symbol = player.Pair,
                Type = OrderType.Market,
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
            player.BuyTime = sig.CandleOpenTime;

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
            db.Player.Update(player);

            //Send Buy Order

            PlayerTrades playerHistory = iPlaymerMapper.Map<Player, PlayerTrades>(player);
            playerHistory.Id = 0;
            await db.PlayerTrades.AddAsync(playerHistory);
            await db.SaveChangesAsync();
        }

        private async Task UpdateTradeBuyDetails()
        {
            Thread.Sleep(60000);
            DB db = new DB();
            
            var players = await db.Player.Where(x => x.IsTrading == true).ToListAsync();
            var bnbprice =  await client.GetPrice("BNBUSDT");
            foreach (var player in players)
            {
                decimal totalbuycost = 0;
                decimal commisionAmt=0;
                if (!player.isBuyCostAccurated)
                {
                    List<AccountTradeReponse> accountTrades = await client.GetAccountTrades(new AllTradesRequest()
                    {
                        Limit = 10,
                        Symbol = player.Pair 
                    });

                    foreach (var trade in accountTrades)
                    {
                        if (trade.OrderId != player.BuyOrderId)
                        {
                            continue;
                        }
                            if (trade.CommissionAsset=="BNB")
                        {
                            commisionAmt= trade.Commission * bnbprice.Price;
                        }
                        else
                        {
                            commisionAmt= trade.Commission;
                        }
                        
                            totalbuycost+= (trade.Price*trade.Quantity )+ commisionAmt;
                    }
                    logger.Info("Previous buy cost of the bot "+player.Name + " was "+player.TotalBuyCost + " Now Updated to "+totalbuycost);
                    player.isBuyCostAccurated = true;
                    player.TotalBuyCost = totalbuycost;

                   db.Player.Update(player);
                }
            }
            await db.SaveChangesAsync();    

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
                ProcessingTimeString +
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
                logger.Info("  " + ProcessingTimeString + " no signals found. So cannot procced for buy process ");
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
                    logger.Info("  " + ProcessingTimeString + " " + player.Name + " is currently trading");
                    await UpdateActivePlayerStats(player);
                    continue;
                }

                if (player.AvailableAmountToBuy < configr.MinimumAmountToTradeWith)
                {
                    logger.Info("  " + ProcessingTimeString + " " + player.Name + " Available Amt " + player.AvailableAmountToBuy + " Not enough for buying ");
                    continue;
                }

                foreach (Signal sig in CurrentSignals)
                {
                    if (sig.IsIgnored || sig.IsPicked || boughtCoins.Contains(sig.Symbol)) //|| PricesGoingDown(sig, player)
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
        }

        private async Task Sell(Player player)
        {

            DB db = new DB();
            decimal mysellPrice = 0;
            SymbolPriceChangeTickerResponse PriceChangeResponse = new SymbolPriceChangeTickerResponse();
            
            if(player==null)
            {
                logger.Info("Player is returned is null. Some issue. Returning");
            }

            var pair = player.Pair;

            if (pair == null)
            {
                logger.Info("Pair returned as null. Some issue. Returning");
            }
            PriceChangeResponse = await client.GetDailyTicker(pair);
            mysellPrice = PriceChangeResponse.LastPrice;

            player.DayHigh = PriceChangeResponse.HighPrice;
            player.DayLow = PriceChangeResponse.LowPrice;
            player.UpdatedTime = DateTime.Now;
            player.SellCoinPrice = mysellPrice;

            decimal availableQty = await GetAvailQty(player, pair);

            //logger.Info("  FYI..For coin " + player.Pair + "  This is the avail Qty in binance " + availableQty + " Our DB shows available Qty as " + player.Quantity.Deci().Rnd(7));

            if (availableQty <= 0)
            {
                logger.Info("  " +
                  ProcessingTimeString +
                  " " + player.Name +
                  " " + pair.Replace("USDT", "").PadRight(7, ' ') +
                  " Available Quantity 0 for " +
                  " Symbol " + player.Pair +
                  " Sell not possible ");
                return;
            }

          //  player.Quantity = availableQty;

            if(player.Quantity==null || player.Quantity.Deci() == 0)
            {
                logger.Info("  " +
               ProcessingTimeString +
               " " + player.Name +
               " " + pair.Replace("USDT", "").PadRight(7, ' ') +
               " player.Quantity  0 for " +
               " Symbol " + player.Pair +
               " Sell not possible ");
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

            //if (player.LastRoundProfitPerc != 0 && prDiffPerc > player.LastRoundProfitPerc)
            //{
            //    LogPriceIncreasingNoSell(player, pair, prDiffPerc);
            //    player.LastRoundProfitPerc = prDiffPerc;
            //    player.AvailableAmountToBuy = 0;
            //    db.Player.Update(player);
            //    continue;
            //}

            if ((prDiffPerc > player.SellAbovePerc) || (prDiffPerc < player.SellBelowPerc)||ForceSell==true)
            {
                if ((prDiffPerc < player.DontSellBelowPerc) && ForceSell == false)
                {
                    LogDontSellBelowPercReason(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                    player.LastRoundProfitPerc = prDiffPerc;
                    player.AvailableAmountToBuy = 0;
                    db.Player.Update(player);
                    return;
                }

                ForceSell = false;

                var coin = MyCoins.Where(x => x.Coin + "USDT" == pair).FirstOrDefault();
                var coinprecison = coin.TradePrecision;

                logger.Info("  " +
                  ProcessingTimeString +
                  " " + player.Name +
                  " " + pair.Replace("USDT", "").PadRight(7, ' ') +
                  " Quantity " + player.Quantity.Deci().Rnd(coinprecison) +
                  " Symbol " + player.Pair +
                  " Creating a sell Order now ");

                var sellOrder = await client.CreateOrder(new CreateOrderRequest()
                {
                    //Price = mysellPrice,
                    Quantity = player.Quantity.Deci().Rnd(coinprecison),
                    Side = OrderSide.Sell,
                    Symbol = player.Pair,
                    Type = OrderType.Market,
                });

                player.SellTime = PriceChangeResponse.OpenTime;
                player.LastRoundProfitPerc = 0;
                player.AvailableAmountToBuy = player.TotalSellAmount;
                player.SellOrderId = sellOrder.OrderId;

                if (prDiffPerc > player.SellAbovePerc)
                {
                    player.BuyOrSell = "Profit";
                    configr.TotalConsecutiveLosses -= 1;
                    if (configr.TotalConsecutiveLosses < 0) configr.TotalConsecutiveLosses = 0;
                    db.Config.Update(configr);
                    LogProfitSell(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                }
                else if (prDiffPerc < player.SellBelowPerc)
                {
                    player.BuyOrSell = "Loss";
                    configr.TotalConsecutiveLosses += 1;
                    db.Config.Update(configr);
                    LogLossSell(player, PriceChangeResponse, pair, mysellPrice, prDiffPerc);
                }
                else
                {
                    player.BuyOrSell = "Cant Decide";
                }

                PlayerTrades PlayerTrades = iPlaymerMapper.Map<Player, PlayerTrades>(player);
                PlayerTrades.Id = 0;
                await db.PlayerTrades.AddAsync(PlayerTrades);
                // reset records to buy again

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
                player.IsTrading = false;
                player.BuyOrSell = string.Empty;
                player.ProfitLossAmt = 0;
                player.ProfitLossChanges = string.Empty;
                player.BuyOrderId=0;
                player.SellOrderId = 0;
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
        }

        private async Task BackupOldCandles()
        {
            if (backupOldCandlesCounter % 300 == 0)
            {
                var threedaysback = DateTime.Now.AddDays(-3);
                using (var db = new DB())
                {
                    var oldercandles = db.Candle.Where(x => x.RecordedTime < threedaysback);
                    foreach (var oldercandle in oldercandles)
                    {
                        CandleBackUp candleBackUp = iCandleMapper.Map<Candle, CandleBackUp>(oldercandle);
                        candleBackUp.Id = 0;
                        await db.CandleBackUp.AddAsync(candleBackUp);
                        db.Candle.Remove(oldercandle);

                    }
                    await db.SaveChangesAsync();
                }
                backupOldCandlesCounter = 1;
            }
            else
            {
                backupOldCandlesCounter++;
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
                    ExchangeInfoSymbolFilterLotSize lotsize = exchangeInfo.Symbols.Where(x => x.Symbol == coin.Coin + "USDT").FirstOrDefault().Filters[2] as ExchangeInfoSymbolFilterLotSize;
                    var precision = lotsize.StepSize.GetAllowedPrecision();
                    coin.TradePrecision = precision;
                    db.MyCoins.Update(coin);
                    logger.Info("Precision for coin " + coin.Coin + " is set as " + precision + " Original step size from exchange info is " + lotsize.StepSize);
                }

                await db.SaveChangesAsync();
            }
            else
            {
                UpdatePrecisionCounter++;
            }

        }

        private async Task Trade()
        {
            DB db = new DB();
            configr = await db.Config.FirstOrDefaultAsync();
            MyCoins = await db.MyCoins.AsNoTracking().ToListAsync();
            ProcessingTime = DateTime.Now;
            ProcessingTimeString = ProcessingTime.ToString("dd-MMM HH:mm:ss");
            NextProcessingTimeString= ProcessingTime.AddMinutes(configr.IntervalMinutes).ToString("dd-MMM HH:mm:ss");
            var allplayers = await db.Player.ToListAsync();
            await UpdateAllowedPrecisionsForPairs();

            #region GetSignals
            logger.Info("Generate Signals Started for " + ProcessingTimeString);
            try
            {
                await GetSignals();
            }
            catch (Exception ex)
            {
                logger.Error("  Exception in signal Generator.Returning.. " + ex.Message);
                return;
            }

            logger.Info("Generate Signals completed for " + ProcessingTimeString);
            logger.Info("");
            #endregion  GetSignals

            #region Buy
            logger.Info("Buying scan Started for " + ProcessingTimeString);
            try
            {
                if (configr.TotalConsecutiveLosses < configr.MaxConsecutiveLossesBeforePause)
                {
                    if (configr.IsBuyingAllowed)
                    {
                        await Buy();
                       // await UpdateTradeBuyDetails();
                    }
                    else
                    {
                        logger.Info("  " + ProcessingTimeString + " Buying is not allowed at this moment");
                    }
                }
                else
                {
                    if (configr.TotalCurrentPauses < configr.MaxPauses)
                    {
                        logger.Info("Last " + configr.TotalConsecutiveLosses + " sells were at loss. Lets wait for " +
                            (configr.MaxPauses - configr.TotalCurrentPauses) * configr.IntervalMinutes + " more minutes before attempting to buy again");
                    }
                    configr.TotalCurrentPauses += 1;

                    if (configr.TotalCurrentPauses > configr.MaxPauses)
                    {
                        configr.TotalCurrentPauses = 0;
                        configr.TotalConsecutiveLosses = 0;

                        if (configr.IsBuyingAllowed)
                        {
                            await Buy();
                           // await UpdateTradeBuyDetails();
                        }
                        else
                        {
                            logger.Info("  " + ProcessingTimeString + " Buying is not allowed at this moment");
                        }
                    }
                    db.Config.Update(configr);
                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.Error("  Exception at buy " + ex.Message);
            }
            logger.Info("Buying scan Completed for " + ProcessingTime);
            logger.Info("");
            #endregion Buys

            #region Sell

            

            logger.Info("Selling scan Started for " + ProcessingTimeString);

            try
            {
                var activePlayers = await db.Player.OrderBy(x => x.Id).Where(x => x.IsTrading == true).ToListAsync();
            
                foreach (var player in activePlayers)
                {
                    if (player.isSellAllowed)
                    {
                        await Sell(player);
                    }
                    else
                    {
                        logger.Info("  " + ProcessingTimeString + " " + player.Name + " is blocked from selling in this iteration");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Exception in sell  " + ex.Message);
            }
            logger.Info("Selling scan Completed for " + ProcessingTimeString + ". Next scan at " + NextProcessingTimeString);
            logger.Info("");

            #endregion  Sell
           
            await BackupOldCandles();
            await SetGrid();
           
        }

        #region low priority methods

        public void LogNoBuy(Player player, Signal sig)
        {
            logger.Info("  " +
               sig.CandleCloseTime.ToString("dd-MMM HH:mm") +
             " " + player.Name +
            " " + sig.Symbol.Replace("USDT", "").ToString().PadRight(7, ' ') +
              " DHi   " + sig.DayHighPr.Rnd().ToString().PadRight(12, ' ') +
              " DLo   " + sig.DayLowPr.Rnd().ToString().PadRight(12, ' ') +
             " CurCnPr " + sig.CurrPr.Rnd().ToString().PadRight(12, ' ') +
             " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
             " > -" + player.BuyBelowPerc.Deci().Rnd(2) +
              " Dont buy");


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
             " PrDif Curr & High -" + sig.PrDiffCurrAndHighPerc.Rnd().ToString().PadRight(12, ' ') +
             " < -" + player.BuyBelowPerc.Deci().Rnd(2) + " Buy Now ");


        }

        public void LogPriceIncreasingNoSell(Player player, string pair, decimal? prDiffPerc)
        {
            logger.Info("  " +
            ProcessingTimeString +
            " " + player.Name +
            " " + pair.Replace("USDT", "").PadRight(7, ' ') +
            " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(6) +
            " > last round's price difference " + player.LastRoundProfitPerc.Deci().Rnd(6) +
            "  Price increasing. Dont sell ");
        }

        public void LogDontSellBelowPercReason(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {
            #region log not selling reason

            logger.Info("  " +
            ProcessingTimeString +
            " " + player.Name +
            " " + pair.Replace("USDT", "").PadRight(7, ' ') +
            " BuyCnPr " + player.BuyCoinPrice.Deci().Rnd().ToString().PadRight(10, ' ') +
            " CurCnPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
            " TotBuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
            " TotSlPr  " + player.TotalSellAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(5) +
            " > " + player.DontSellBelowPerc.Deci().Rnd(2) +
            " Not selling ");


            #endregion log not selling reason
        }

        public void LogLossSell(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {
            #region log loss sell

            logger.Info("  " +
            ProcessingTimeString +
            " " + player.Name +
             " " + pair.Replace("USDT", "").PadRight(7, ' ') +
               " BuyCnPr " + player.BuyCoinPrice.Deci().Rnd().ToString().PadRight(10, ' ') +
               " CurCnPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
               " TotBuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
               " TotSlPr  " + player.TotalSellAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(4).ToString().PadRight(5, ' ') +
            " < " + player.SellBelowPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
            " Loss Sell ");

            logger.Info("  " + ProcessingTimeString + " Total Consecutive Loss " + configr.TotalConsecutiveLosses);
            #endregion log loss sell
        }

        public void LogProfitSell(Player player, SymbolPriceChangeTickerResponse PriceChangeResponse, string pair, decimal mysellPrice, decimal? prDiffPerc)
        {

            #region log profit sell

            logger.Info("  " +
            ProcessingTimeString +
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
            #region log dont sell reason

            logger.Info("  " +
            ProcessingTimeString +
            " " + player.Name +
              " " + pair.Replace("USDT", "").PadRight(7, ' ') +
             
               " BuyCnPr " + player.BuyCoinPrice.Deci().Rnd().ToString().PadRight(10, ' ') +
               " CurCnPr " + mysellPrice.Rnd().ToString().PadRight(10, ' ') +
               " TotBuyPr " + player.TotalBuyCost.Deci().Rnd().ToString().PadRight(8, ' ') +
               " TotSlPr  " + player.TotalSellAmount.Deci().Rnd().ToString().PadRight(8, ' ') +
            " PrDif Bogt & Sold " + prDiffPerc.Deci().Rnd(2).ToString().PadRight(5, ' ') +
            " < " + player.SellAbovePerc.Deci().Rnd(2).ToString().PadRight(3, ' ') +
            " and > " + player.SellBelowPerc.Deci().Rnd(2) + " Dont Sell ");



            #endregion  log dont sell reason
        }

        #endregion

    }
}
