using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TechnoBuy.DataAccess.Data;
using TechnoBuy.DataAccess.Repository.IRepository;

namespace TechnoBuy.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;
        public Repository(ApplicationDbContext db)
        {
            _db = db;
            dbSet = _db.Set<T>();
        }

        public void Add(T entity)
        {
            dbSet.Add(entity);
        }

        public T Get(Expression<Func<T, bool>> filter, string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            query = query.Where(filter);

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            return query.FirstOrDefault();
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null, 
            string? searchQuery = null, int pageNumber = 0, int pageSize = 0, string? citySearchQuery = null)
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                var lowerSearchQuery = searchQuery.ToLower();
                query = query.Where(x => EF.Property<string>(x, "Name").ToLower().Contains(lowerSearchQuery));
            }

            if (!string.IsNullOrEmpty(citySearchQuery))
            {
                var lowerCitySearchQuery = citySearchQuery.ToLower();
                query = query.Where(x => EF.Property<string>(x, "City").ToLower().Contains(lowerCitySearchQuery));
            }
            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            if (pageNumber > 0 && pageSize > 0)
            {
                query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }

            return query.ToList();
        }

        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }
    }
}
