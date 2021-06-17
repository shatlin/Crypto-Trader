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
        public bool IsCandleBeingUpdated { get; set; }
        public bool IsDailyCandleBeingUpdated { get; set; }
    }

   


}
