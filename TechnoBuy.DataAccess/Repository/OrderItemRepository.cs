using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnoBuy.DataAccess.Data;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.Models;

namespace TechnoBuy.DataAccess.Repository
{
    public class OrderItemRepository : Repository<OrderItem>, IOrderItemRespository
    {
        private ApplicationDbContext _db;
        public OrderItemRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderItem orderItem)
        {
            _db.OrderItems.Update(orderItem);
        }
    }
}
