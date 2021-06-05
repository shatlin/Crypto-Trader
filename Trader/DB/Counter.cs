using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{
    public class Counter
    {
        public int Id { get; set; }
        public int CandleCurrentSet { get; set; }
        public int DailyCandleCurrentSet { get; set; }
        public DateTime CandleLastUpdatedTime { get; set; }
        public DateTime DailyCandleLastUpdatedTime { get; set; }
        public bool IsCandleCurrentlyBeingUpdated { get; set; }
        public bool IsDailyCandleCurrentlyBeingUpdated { get; set; }
    }

   


}
