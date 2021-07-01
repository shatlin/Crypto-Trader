using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{
    public class MyCoins
    {
        public int Id { get; set; }
        public string Coin { get; set; }
        public int TradePrecision { get; set; }
    }


}
