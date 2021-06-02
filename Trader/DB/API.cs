using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trader.Models
{

    //1	2R85z2YZ1MA4P17VudVdBZyUJVGRZ4Pqqt3dDpNYMWHsWocmkgPCTFZIYE5TkYYZ	1a1TrhcElv7WmytTdJGPBFcIO12JDntpAdS7xLtOcacj1Lm0KWibchgAMKvr6lav
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
