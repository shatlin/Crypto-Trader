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
            Database.SetCommandTimeout(new TimeSpan(0,0,120));
            // Database.EnsureCreated();
        }

        public DbSet<APIDetails> API { get; set; }
        public DbSet<Balance> Balance { get; set; }
        public DbSet<Candle> Candle { get; set; }
        public DbSet<CandleBackUp> CandleBackUp { get; set; }
        public DbSet<MyTrade> MyTrade { get; set; }
        public DbSet<MyCoins> MyCoins { get; set; }
        public DbSet<Player> Player { get; set; }
        public DbSet<PlayerTrades> PlayerTrades { get; set; }
        public DbSet<Config> Config { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Data Source=localhost;Initial Catalog=Shatlin;Integrated Security=True;Connect Timeout=60");
                base.OnConfiguring(optionsBuilder);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new APIConfiguration());
            modelBuilder.ApplyConfiguration(new MyTradeConfiguration());
            modelBuilder.ApplyConfiguration(new ConfigConfiguration()).SeedConfig();
            modelBuilder.ApplyConfiguration(new MyBalanceConfiguration());
            modelBuilder.ApplyConfiguration(new CandleConfiguration());
            modelBuilder.ApplyConfiguration(new CandleBackUpConfiguration());
            modelBuilder.ApplyConfiguration(new PlayerConfiguration());
            modelBuilder.ApplyConfiguration(new PlayerHistConfiguration());
        }
    }

   
}
