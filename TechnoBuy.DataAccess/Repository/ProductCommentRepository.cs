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
    public class ProductCommentRepository : Repository<ProductComment>, IProductComment
    {
        private ApplicationDbContext _db;

        public ProductCommentRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ProductComment productComment)
        {
            _db.ProductComments.Update(productComment);
        }
    }
}
