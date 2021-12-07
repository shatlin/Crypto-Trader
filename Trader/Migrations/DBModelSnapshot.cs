﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Trader.Models;

namespace Trader2.Migrations
{
    [DbContext(typeof(DB))]
    partial class DBModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "6.0.0-preview.4.21253.1")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("BinanceExchange.API.Models.Response.Balance", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Asset")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("AvgBuyCoinPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("CurrCoinPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("DiffPerc")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("Difference")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("Free")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("Locked")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("TotBoughtPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("TotCurrentPrice")
                        .HasColumnType("decimal(18,9)");

                    b.HasKey("Id");

                    b.ToTable("Balance");
                });

            modelBuilder.Entity("BinanceExchange.API.Models.Response.Candle", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("Change")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("Close")
                        .HasColumnType("decimal(18,9)");

                    b.Property<DateTime>("CloseTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("CurrentPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("DayHighPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("DayLowPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<int>("DayTradeCount")
                        .HasColumnType("int");

                    b.Property<decimal>("DayVolume")
                        .HasColumnType("decimal(23,4)");

                    b.Property<decimal>("High")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("Low")
                        .HasColumnType("decimal(18,9)");

                    b.Property<int>("NumberOfTrades")
                        .HasColumnType("int");

                    b.Property<decimal>("Open")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("OpenPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<DateTime>("OpenTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("PreviousClosePrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("PriceChangePercent")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("QuoteAssetVolume")
                        .HasColumnType("decimal(23,4)");

                    b.Property<DateTime?>("RecordedTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Symbol")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("TakerBuyBaseAssetVolume")
                        .HasColumnType("decimal(23,4)");

                    b.Property<decimal>("TakerBuyQuoteAssetVolume")
                        .HasColumnType("decimal(23,4)");

                    b.Property<decimal>("Volume")
                        .HasColumnType("decimal(23,4)");

                    b.Property<decimal>("WeightedAveragePercent")
                        .HasColumnType("decimal(18,9)");

                    b.HasKey("Id");

                    b.ToTable("Candle");
                });

            modelBuilder.Entity("BinanceExchange.API.Models.Response.MyTrade", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("Commission")
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("CommissionAsset")
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsBestMatch")
                        .HasColumnType("bit");

                    b.Property<bool>("IsBuyer")
                        .HasColumnType("bit");

                    b.Property<bool>("IsMaker")
                        .HasColumnType("bit");

                    b.Property<long>("OrderId")
                        .HasColumnType("bigint");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime>("Time")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("MyTrade");
                });

            modelBuilder.Entity("BinanceExchange.API.Models.Response.Player", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("AvailableAmountToBuy")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyAtPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyCoinPrice")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("BuyOrSell")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("BuyOrderId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("BuyTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("CurrentCoinPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayHigh")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayLow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DontSellBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("ForceSell")
                        .HasColumnType("bit");

                    b.Property<decimal?>("HardSellPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("IsTracked")
                        .HasColumnType("bit");

                    b.Property<bool>("IsTrading")
                        .HasColumnType("bit");

                    b.Property<decimal?>("LastRoundProfitPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("ProfitLossAmt")
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("ProfitLossChanges")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("Quantity")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<int>("RepsTillCancelOrder")
                        .HasColumnType("int");

                    b.Property<decimal?>("SellAbovePerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SellAtPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SellBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("SellCoinPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SellCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<long>("SellOrderId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("SellTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("TotalBuyCost")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalSellAmount")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("UpdatedTime")
                        .HasColumnType("datetime2");

                    b.Property<bool>("isBuyAllowed")
                        .HasColumnType("bit");

                    b.Property<bool>("isBuyOrderCompleted")
                        .HasColumnType("bit");

                    b.Property<bool>("isSellAllowed")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("Player");
                });

            modelBuilder.Entity("BinanceExchange.API.Models.Response.PlayerQA", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("AvailableAmountToBuy")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyCoinPrice")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("BuyOrSell")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("BuyOrderId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("BuyTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("CurrentCoinPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayHigh")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayLow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DontSellBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("HardSellPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("IsTrading")
                        .HasColumnType("bit");

                    b.Property<decimal?>("LastRoundProfitPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("ProfitLossAmt")
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("ProfitLossChanges")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("Quantity")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<int>("RepsTillCancelOrder")
                        .HasColumnType("int");

                    b.Property<decimal?>("SellAbovePerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SellBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("SellCoinPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SellCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<long>("SellOrderId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("SellTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("TotalBuyCost")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalSellAmount")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("UpdatedTime")
                        .HasColumnType("datetime2");

                    b.Property<bool>("isBuyAllowed")
                        .HasColumnType("bit");

                    b.Property<bool>("isBuyOrderCompleted")
                        .HasColumnType("bit");

                    b.Property<bool>("isSellAllowed")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("PlayerQA");
                });

            modelBuilder.Entity("BinanceExchange.API.Models.Response.PlayerTrades", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("AvailableAmountToBuy")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyCoinPrice")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("BuyOrSell")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("BuyOrderId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("BuyTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("CurrentCoinPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayHigh")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayLow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DontSellBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("ForceSell")
                        .HasColumnType("bit");

                    b.Property<decimal?>("HardSellPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("IsTracked")
                        .HasColumnType("bit");

                    b.Property<bool>("IsTrading")
                        .HasColumnType("bit");

                    b.Property<decimal?>("LastRoundProfitPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("ProfitLossAmt")
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("ProfitLossChanges")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("Quantity")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<int>("RepsTillCancelOrder")
                        .HasColumnType("int");

                    b.Property<decimal?>("SellAbovePerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SellBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("SellCoinPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SellCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<long>("SellOrderId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("SellTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("TotalBuyCost")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalSellAmount")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("UpdatedTime")
                        .HasColumnType("datetime2");

                    b.Property<bool>("isBuyAllowed")
                        .HasColumnType("bit");

                    b.Property<bool>("isBuyOrderCompleted")
                        .HasColumnType("bit");

                    b.Property<bool>("isSellAllowed")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("PlayerTrades");
                });

            modelBuilder.Entity("BinanceExchange.API.Models.Response.PlayerTradesQA", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("AvailableAmountToBuy")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyCoinPrice")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("BuyOrSell")
                        .HasColumnType("nvarchar(max)");

                    b.Property<long>("BuyOrderId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("BuyTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("CurrentCoinPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayHigh")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayLow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DontSellBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("HardSellPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("IsTrading")
                        .HasColumnType("bit");

                    b.Property<decimal?>("LastRoundProfitPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("ProfitLossAmt")
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("ProfitLossChanges")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("Quantity")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<int>("RepsTillCancelOrder")
                        .HasColumnType("int");

                    b.Property<decimal?>("SellAbovePerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SellBelowPerc")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("SellCoinPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SellCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<long>("SellOrderId")
                        .HasColumnType("bigint");

                    b.Property<DateTime?>("SellTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("TotalBuyCost")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalSellAmount")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("UpdatedTime")
                        .HasColumnType("datetime2");

                    b.Property<bool>("isBuyAllowed")
                        .HasColumnType("bit");

                    b.Property<bool>("isBuyOrderCompleted")
                        .HasColumnType("bit");

                    b.Property<bool>("isSellAllowed")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("PlayerTradesQA");
                });

            modelBuilder.Entity("Trader.Models.APIDetails", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("key")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("secret")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("id");

                    b.ToTable("API");
                });

            modelBuilder.Entity("Trader.Models.Config", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Botname")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("BufferPriceForBuyAndSell")
                        .HasColumnType("decimal(18,12)");

                    b.Property<decimal>("CommisionAmount")
                        .HasColumnType("decimal(18,12)");

                    b.Property<bool>("CrashSell")
                        .HasColumnType("bit");

                    b.Property<decimal>("DayHighGreaterthanToSell")
                        .HasColumnType("decimal(18,12)");

                    b.Property<decimal>("DayHighLessthanToSell")
                        .HasColumnType("decimal(18,12)");

                    b.Property<decimal>("DayLowGreaterthanTobuy")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("DayLowLessthanTobuy")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("DefaultSellAbovePerc")
                        .HasColumnType("decimal(6,4)");

                    b.Property<decimal>("DivideHighAndAverageBy")
                        .HasColumnType("decimal(18,12)");

                    b.Property<int>("IntervalMinutes")
                        .HasColumnType("int");

                    b.Property<bool>("IsBuyingAllowed")
                        .HasColumnType("bit");

                    b.Property<bool>("IsProd")
                        .HasColumnType("bit");

                    b.Property<bool>("IsReducingSellAbvAllowed")
                        .HasColumnType("bit");

                    b.Property<bool>("IsSellingAllowed")
                        .HasColumnType("bit");

                    b.Property<int>("MaxConsecutiveLossesBeforePause")
                        .HasColumnType("int");

                    b.Property<int>("MaxPauses")
                        .HasColumnType("int");

                    b.Property<int>("MaxRepsBeforeCancelOrder")
                        .HasColumnType("int");

                    b.Property<decimal>("MaximumAmountForaBot")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("MinAllowedTradeCount")
                        .HasColumnType("decimal(18,12)");

                    b.Property<decimal>("MinSellAbovePerc")
                        .HasColumnType("decimal(6,4)");

                    b.Property<decimal>("MinimumAmountToTradeWith")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal>("ReducePriceDiffPercBy")
                        .HasColumnType("decimal(6,4)");

                    b.Property<int>("ReduceSellAboveAtMinute")
                        .HasColumnType("int");

                    b.Property<decimal>("ReduceSellAboveBy")
                        .HasColumnType("decimal(6,4)");

                    b.Property<int>("ReduceSellAboveFromSecond")
                        .HasColumnType("int");

                    b.Property<int>("ReduceSellAboveToSecond")
                        .HasColumnType("int");

                    b.Property<decimal>("ScalpFifteenMinDiffLessThan")
                        .HasColumnType("decimal(6,4)");

                    b.Property<int>("ScalpFifteenMinDownMoreThan")
                        .HasColumnType("int");

                    b.Property<decimal>("ScalpFiveMinDiffLessThan")
                        .HasColumnType("decimal(6,4)");

                    b.Property<int>("ScalpFiveMinDownMoreThan")
                        .HasColumnType("int");

                    b.Property<decimal>("ScalpFourHourDiffLessThan")
                        .HasColumnType("decimal(6,4)");

                    b.Property<int>("ScalpFourHourDownMoreThan")
                        .HasColumnType("int");

                    b.Property<decimal>("ScalpOneHourDiffLessThan")
                        .HasColumnType("decimal(6,4)");

                    b.Property<int>("ScalpOneHourDownMoreThan")
                        .HasColumnType("int");

                    b.Property<decimal>("ScalpThirtyMinDiffLessThan")
                        .HasColumnType("decimal(6,4)");

                    b.Property<int>("ScalpThirtyMinDownMoreThan")
                        .HasColumnType("int");

                    b.Property<decimal>("SellWhenAllBotsAtLossBelow")
                        .HasColumnType("decimal(4,2)");

                    b.Property<bool>("ShouldSellWhenAllBotsAtLoss")
                        .HasColumnType("bit");

                    b.Property<bool>("ShowBuyLogs")
                        .HasColumnType("bit");

                    b.Property<bool>("ShowBuyingFlowLogs")
                        .HasColumnType("bit");

                    b.Property<bool>("ShowNoBuyLogs")
                        .HasColumnType("bit");

                    b.Property<bool>("ShowNoScalpBuyLogs")
                        .HasColumnType("bit");

                    b.Property<bool>("ShowNoSellLogs")
                        .HasColumnType("bit");

                    b.Property<bool>("ShowScalpBuyLogs")
                        .HasColumnType("bit");

                    b.Property<bool>("ShowSellLogs")
                        .HasColumnType("bit");

                    b.Property<bool>("ShowSellingFlowLogs")
                        .HasColumnType("bit");

                    b.Property<int>("TotalConsecutiveLosses")
                        .HasColumnType("int");

                    b.Property<int>("TotalCurrentPauses")
                        .HasColumnType("int");

                    b.Property<bool>("UpdateCoins")
                        .HasColumnType("bit");

                    b.HasKey("id");

                    b.ToTable("Config");
                });

            modelBuilder.Entity("Trader.Models.MyCoins", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<bool>("ClimbedHigh")
                        .HasColumnType("bit");

                    b.Property<bool>("ClimbingFast")
                        .HasColumnType("bit");

                    b.Property<string>("CoinName")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("CoinSymbol")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("CurrentPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("DayHighPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("DayLowPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("DayOpenPrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("DayPriceDiff")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("DayTradeCount")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("DayVolume")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("DayVolumeUSDT")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("FifteenMinChange")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("FiveMinChange")
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("ForceBuy")
                        .HasColumnType("bit");

                    b.Property<decimal>("FortyEightHourChange")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("FourHourChange")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("FourtyFiveMinChange")
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("IsIncludedForTrading")
                        .HasColumnType("bit");

                    b.Property<decimal>("MarketCap")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("OneHourChange")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("OneWeekChange")
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("PercAboveDayLowToSell")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("PercBelowDayHighToBuy")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("PrecisionDecimals")
                        .HasColumnType("decimal(30,12)");

                    b.Property<int>("Rank")
                        .HasColumnType("int");

                    b.Property<bool>("SuperHigh")
                        .HasColumnType("bit");

                    b.Property<decimal>("TenMinChange")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("ThirtyMinChange")
                        .HasColumnType("decimal(30,12)");

                    b.Property<int>("TradePrecision")
                        .HasColumnType("int");

                    b.Property<string>("TradeSuggestion")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("TwentyFourHourChange")
                        .HasColumnType("decimal(30,12)");

                    b.HasKey("Id");

                    b.ToTable("MyCoins");
                });

            modelBuilder.Entity("Trader.Models.SignalCandle", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTime>("AddedTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("CandleType")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal>("ClosePrice")
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime>("CloseTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("UpOrDown")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("SignalCandle");
                });
#pragma warning restore 612, 618
        }
    }
}
