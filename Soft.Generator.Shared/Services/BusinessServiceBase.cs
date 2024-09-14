using Microsoft.EntityFrameworkCore;
using Soft.Generator.Shared.BaseEntities;
using Soft.Generator.Shared.Interfaces;
using Soft.Generator.Shared.SoftExceptions;
using Soft.Generator.Shared.Terms;
using Soft.Generator.Shared.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.Shared.Services
{
    public class BusinessServiceBase
    {
        private readonly IApplicationDbContext _context;

        public BusinessServiceBase(IApplicationDbContext context)
        {
            _context=context;
        }

        protected internal async Task<T> LoadInstanceAsync<T, ID>(ID id, int? version) where T : BusinessObject<ID>
        {
            return await _context.WithTransactionAsync(async () =>
            {
                T poco = await _context.DbSet<T>().FindAsync(id);

                if (poco == null)
                    throw new BusinessException(SharedTerms.EntityDoesNotExistInDatabase);

                if (version.HasValue && poco.Version != version)
                    throw new BusinessException(SharedTerms.ConcurrencyException);

                return poco;
            });
        }

        public async Task DeleteEntity<T, ID>(ID id) where T : BusinessObject<ID> where ID : struct // https://www.c-sharpcorner.com/article/equality-operator-with-inheritance-and-generics-in-c-sharp/
        {
            await _context.WithTransactionAsync(async () =>
            {
                int deletedRow = await _context.DbSet<T>().Where(x => x.Id.Equals(id)).ExecuteDeleteAsync();
                if (deletedRow == 0)
                    throw new BusinessException(SharedTerms.EntityDoesNotExistInDatabaseForDeleteRequest);
            });
        }
    }
}
