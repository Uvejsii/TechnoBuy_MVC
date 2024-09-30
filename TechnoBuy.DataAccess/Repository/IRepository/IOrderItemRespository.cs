using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnoBuy.Models;

namespace TechnoBuy.DataAccess.Repository.IRepository
{
    public interface IOrderItemRespository : IRepository<OrderItem>
    {
        void Update(OrderItem orderItem);
    }
}
