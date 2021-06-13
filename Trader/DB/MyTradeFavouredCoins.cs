using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{
    public class MyTradeFavouredCoins
    {

        
        public int Id { get; set; }

        public string Pair { get; set; }

        public string BotGroup { get; set; }

        public List<string> GetMyCoins()
        {
            return new List<string>
            {
                "ETHBUSD",
                "BTCBUSD",
                "AAVEBUSD",
                "MATICBUSD",
                "MKRBUSD",
                "ADABUSD",
                "BNBBUSD",
                "SHIBBUSD",
                "BAKEBUSD",
                "NEOBUSD",
                "LINKBUSD",
                "DOGEBUSD",
                "MANABUSD",
                "HOTBUSD",
                "VETBUSD",
                "ZENUSDT",
                "ONEBUSD",
                "XLMBUSD",
                "XRPBUSD",
                "COMPBUSD",
            };
        }
    }

   


}
