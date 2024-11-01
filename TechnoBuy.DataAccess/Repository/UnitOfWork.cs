using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnoBuy.DataAccess.Data;
using TechnoBuy.DataAccess.Repository.IRepository;

namespace TechnoBuy.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;

        public IProductRepository Product { get; private set; }
        public ICategoryRepository Category { get; private set; }
        public ICartRepository Cart { get; private set; }
        public ICartItemRepository CartItem { get; private set; }
        public IOrderRepository Order { get; private set; }
        public IOrderItemRespository OrderItem {  get; private set; }
        public IProductComment ProductComment {  get; private set; }
        public IAdminUserRepository AdminUser { get; set; }
        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            Product = new ProductRepository(_db);
            Category = new CategoryRepository(_db);
            Cart = new CartRepository(_db);
            CartItem = new CartItemRepository(_db);
            Order = new OrderRepository(_db);
            OrderItem = new OrderItemRepository(_db);
            ProductComment = new ProductCommentRepository(_db);
            AdminUser = new AdminUserRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
