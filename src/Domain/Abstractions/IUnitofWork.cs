using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Abstractions
{
    public interface IUnitofWork
    {
        Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

        Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    }
}
