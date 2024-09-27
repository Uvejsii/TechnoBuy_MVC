using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoBuy.Models
{
    public enum OrderStatus
    {
        Pending,
        Boxing,
        Shipping,
        Delivered,
        Cancelled,
        Returned
    }
}
