using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BinanceExchange.API.Models.Response;

namespace Trader.Models
{
    public class DB: DbContext
    {

        public DB()
        {
            Database.EnsureCreated();
        }

        public DbSet<Price> Price { get;set;}
        public DbSet<APIDetails> API { get; set; }
        public DbSet<Balance> Balance { get; set; }
        //public DbSet<PriceChange> PriceChange { get; set; }
        public DbSet<Candle> Candle { get; set; }
        public DbSet<DailyCandle> DailyCandle { get; set; }
        public DbSet<MyTrade> MyTrade { get; set; }
        public DbSet<MyTradeFavouredCoins> MyTradeFavouredCoins { get; set; }
        public DbSet<Counter> Counter { get; set; }
        public DbSet<TradeBot> TradeBot { get; set; }
        public DbSet<TradeBotHistory> TradeBotHistory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=binance;Integrated Security=True");
                base.OnConfiguring(optionsBuilder);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new PriceConfiguration());
            modelBuilder.ApplyConfiguration(new APIConfiguration());
            modelBuilder.ApplyConfiguration(new MyTradeConfiguration());
            modelBuilder.ApplyConfiguration(new MyBalanceConfiguration());
            modelBuilder.ApplyConfiguration(new CandleConfiguration());
            modelBuilder.ApplyConfiguration(new DailyCandleConfiguration());
            modelBuilder.ApplyConfiguration(new TradeBotConfiguration());
            modelBuilder.ApplyConfiguration(new TradeBotHistoryConfiguration());
        }
    }

   
}
