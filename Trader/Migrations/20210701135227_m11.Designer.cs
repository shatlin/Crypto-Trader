﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Trader.Models;

namespace Trader2.Migrations
{
    [DbContext(typeof(DB))]
    [Migration("20210701135227_m11")]
    partial class m11
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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

            modelBuilder.Entity("BinanceExchange.API.Models.Response.CandleBackUp", b =>
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

                    b.ToTable("CandleBackUp");
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

                    b.Property<bool>("isSellAllowed")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("Player");
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

                    b.Property<bool>("isSellAllowed")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("PlayerTrades");
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

                    b.Property<int>("IntervalMinutes")
                        .HasColumnType("int");

                    b.Property<bool>("IsBuyingAllowed")
                        .HasColumnType("bit");

                    b.Property<bool>("IsProd")
                        .HasColumnType("bit");

                    b.Property<int>("MaxConsecutiveLossesBeforePause")
                        .HasColumnType("int");

                    b.Property<int>("MaxPauses")
                        .HasColumnType("int");

                    b.Property<decimal>("MinimumAmountToTradeWith")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("TotalConsecutiveLosses")
                        .HasColumnType("int");

                    b.Property<int>("TotalCurrentPauses")
                        .HasColumnType("int");

                    b.HasKey("id");

                    b.ToTable("Config");

                    b.HasData(
                        new
                        {
                            id = 1,
                            Botname = "DIANA",
                            BufferPriceForBuyAndSell = 0.075m,
                            CommisionAmount = 0.075m,
                            IntervalMinutes = 5,
                            IsBuyingAllowed = false,
                            IsProd = false,
                            MaxConsecutiveLossesBeforePause = 3,
                            MaxPauses = 1,
                            MinimumAmountToTradeWith = 70m,
                            TotalConsecutiveLosses = 0,
                            TotalCurrentPauses = 0
                        });
                });

            modelBuilder.Entity("Trader.Models.MyCoins", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Coin")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("TradePrecision")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("MyCoins");
                });
#pragma warning restore 612, 618
        }
    }
}
