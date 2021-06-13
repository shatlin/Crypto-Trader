﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Trader.Models;

namespace Trader.Migrations
{
    [DbContext(typeof(DB))]
    [Migration("20210612161328_m25")]
    partial class m25
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

                    b.Property<decimal>("AverageBuyingCoinPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("BoughtPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("CurrentCoinPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("CurrentPrice")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("Difference")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("DifferencePercentage")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("Free")
                        .HasColumnType("decimal(18,9)");

                    b.Property<decimal>("Locked")
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

                    b.Property<int>("DataSet")
                        .HasColumnType("int");

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

            modelBuilder.Entity("BinanceExchange.API.Models.Response.DailyCandle", b =>
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

                    b.Property<int>("DataSet")
                        .HasColumnType("int");

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

                    b.ToTable("DailyCandle");
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

            modelBuilder.Entity("BinanceExchange.API.Models.Response.TradeBot", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("AvailableAmountForTrading")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Avatar")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("BuyOrSell")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("BuyPricePerCoin")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("BuyTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("BuyWhenValuePercentageIsBelow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyingCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("CandleOpenTimeAtBuy")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("CandleOpenTimeAtSell")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("CurrentPricePerCoin")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayHigh")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayLow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("IsActivelyTrading")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.Property<decimal?>("OriginalAllocatedValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("QuantityBought")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("QuantitySold")
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("SellTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("SellWhenProfitPercentageIsAbove")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SoldCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("SoldPricePricePerCoin")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalBuyCost")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentProfit")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalSoldAmount")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("UpdatedTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("TradeBot");
                });

            modelBuilder.Entity("BinanceExchange.API.Models.Response.TradeBotBackup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("AvailableAmountForTrading")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Avatar")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("BuyOrSell")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("BuyPricePerCoin")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("BuyTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("BuyWhenValuePercentageIsBelow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyingCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("CandleOpenTimeAtBuy")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("CandleOpenTimeAtSell")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("CurrentPricePerCoin")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayHigh")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayLow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("IsActivelyTrading")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.Property<decimal?>("OriginalAllocatedValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("QuantityBought")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("QuantitySold")
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("SellTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("SellWhenProfitPercentageIsAbove")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SoldCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("SoldPricePricePerCoin")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalBuyCost")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentProfit")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalSoldAmount")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("UpdatedTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("TradeBotBackup");
                });

            modelBuilder.Entity("BinanceExchange.API.Models.Response.TradeBotHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("AvailableAmountForTrading")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Avatar")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("BuyOrSell")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("BuyPricePerCoin")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("BuyTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("BuyWhenValuePercentageIsBelow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("BuyingCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("CandleOpenTimeAtBuy")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("CandleOpenTimeAtSell")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("CurrentPricePerCoin")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayHigh")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("DayLow")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<bool>("IsActivelyTrading")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Order")
                        .HasColumnType("int");

                    b.Property<decimal?>("OriginalAllocatedValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.Property<decimal?>("QuantityBought")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("QuantitySold")
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("SellTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal?>("SellWhenProfitPercentageIsAbove")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("SoldCommision")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal>("SoldPricePricePerCoin")
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalBuyCost")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentProfit")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalCurrentValue")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<decimal?>("TotalSoldAmount")
                        .IsRequired()
                        .HasColumnType("decimal(30,12)");

                    b.Property<DateTime?>("UpdatedTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("TradeBotHistory");
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

            modelBuilder.Entity("Trader.Models.Counter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CandleCurrentSet")
                        .HasColumnType("int");

                    b.Property<DateTime>("CandleLastUpdatedTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("DailyCandleCurrentSet")
                        .HasColumnType("int");

                    b.Property<DateTime>("DailyCandleLastUpdatedTime")
                        .HasColumnType("datetime2");

                    b.Property<bool>("IsCandleCurrentlyBeingUpdated")
                        .HasColumnType("bit");

                    b.Property<bool>("IsDailyCandleCurrentlyBeingUpdated")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("Counter");
                });

            modelBuilder.Entity("Trader.Models.MyTradeFavouredCoins", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Pair")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("MyTradeFavouredCoins");
                });

            modelBuilder.Entity("Trader.Models.Price", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("DataSet")
                        .HasColumnType("int");

                    b.Property<DateTime>("date")
                        .HasColumnType("datetime");

                    b.Property<string>("pair")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<decimal>("price")
                        .HasColumnType("decimal(18,9)");

                    b.HasKey("id");

                    b.ToTable("Price");
                });
#pragma warning restore 612, 618
        }
    }
}
