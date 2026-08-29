using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Persistence
{
    public class EfUnitOfWork : IUnitofWork
    {
        private readonly ApplicationDbContext _context;
        public EfUnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return new EfTransaction(transaction);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
