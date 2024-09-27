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
    public class CartItemRepository : Repository<CartItem>, ICartItemRepository
    {
        private ApplicationDbContext _db;
        public CartItemRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(CartItem cartItem)
        {
            _db.Update(cartItem);
        }
    }
}
