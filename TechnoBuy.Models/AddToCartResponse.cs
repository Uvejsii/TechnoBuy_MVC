using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoBuy.Models
{
    public class AddToCartResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int CartQuantity { get; set; }
        public string RedirectUrl { get; set; }
    }
}
