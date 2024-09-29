using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoBuy.DataAccess.Service.IService
{
    public interface ICartService
    {
        int GetCartQuantity(string userId);
    }
}
