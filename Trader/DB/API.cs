using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{

    public class APIDetails: BaseModel
    {
        public int id{get;set; }
        public string key { get; set; }
        public string secret { get; set; }
    }

    public partial class APIConfiguration : IEntityTypeConfiguration<APIDetails>
    {
        public void Configure(EntityTypeBuilder<APIDetails> builder)
        {
        }
    }

}
