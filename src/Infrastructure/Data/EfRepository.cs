using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using SharedKernel.Interfaces;

namespace Infrastructure.Data
{
    public class EfRepository : IRepository
    {
        private readonly AppDbContext dbContext;

        public EfRepository(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<T> GetByIdAsync<T>(Guid id) where T : BaseEntity
        {
            return dbContext.Set<T>().SingleOrDefaultAsync(e => e.Id == id);
        }

        public Task<List<T>> ListAsync<T>() where T : BaseEntity
        {
            return dbContext.Set<T>().ToListAsync();
        }

        public async Task<T> AddAsync<T>(T entity) where T : BaseEntity
        {
            await dbContext.Set<T>().AddAsync(entity);
            await dbContext.SaveChangesAsync();

            return entity;
        }

        public async Task UpdateAsync<T>(T entity) where T : BaseEntity
        {
            dbContext.Entry(entity).State = EntityState.Modified;
            await dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync<T>(T entity) where T : BaseEntity
        {
            dbContext.Set<T>().Remove(entity);
            await dbContext.SaveChangesAsync();
        }

        public T GetById<T>(Guid id) where T : BaseEntity
        {
            return dbContext.Set<T>().SingleOrDefault(e => e.Id == id);
        }
    }
}