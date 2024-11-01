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
    public class AdminUserRepository : Repository<ApplicationUser>, IAdminUserRepository
    {
        private ApplicationDbContext _db;
        public AdminUserRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ApplicationUser appUser)
        {
            _db.Update(appUser);
        }
    }
}
