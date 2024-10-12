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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Soft.Generator.Shared.Services
{
    public class BusinessServiceBase
    {
        private readonly IApplicationDbContext _context;

        public BusinessServiceBase(IApplicationDbContext context)
        {
            _context=context;
        }

        protected internal async Task<T> LoadInstanceAsync<T, ID>(ID id, int? version) 
            where T : class, IBusinessObject<ID>
            where ID : struct
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

        protected internal async Task<T> LoadInstanceAsync<T, ID>(ID id) 
            where T : class, IReadonlyObject<ID>
            where ID : struct
        {
            return await _context.WithTransactionAsync(async () =>
            {
                T poco = await _context.DbSet<T>().FindAsync(id);

                if (poco == null)
                    throw new BusinessException(SharedTerms.EntityDoesNotExistInDatabase);

                return poco;
            });
        }

        protected internal async Task CheckVersionAsync<T, ID>(ID id, int version) 
            where T : class, IBusinessObject<ID> 
            where ID : struct
        {
            await _context.WithTransactionAsync(async () =>
            {
                int dbVersion = await _context.DbSet<T>().Where(x => x.Id.Equals(id)).Select(x => x.Version).SingleOrDefaultAsync();

                if (dbVersion != version)
                    throw new BusinessException(SharedTerms.ConcurrencyException);
            });
        }

        public async Task DeleteEntityAsync<T, ID>(ID id) where T : class, IBusinessObject<ID> where ID : struct // https://www.c-sharpcorner.com/article/equality-operator-with-inheritance-and-generics-in-c-sharp/
        {
            await _context.WithTransactionAsync(async () =>
            {
                int deletedRow = await _context.DbSet<T>().Where(x => x.Id.Equals(id)).ExecuteDeleteAsync();
                if (deletedRow == 0)
                    throw new BusinessException(SharedTerms.EntityDoesNotExistInDatabaseForDeleteRequest);
            });
        }

        //public async Task UpdateManyToManyAssociation<T1, T2>(IQueryable<TUser> query, Enum permissionCode)
        //{
        //    return await _context.WithTransactionAsync(async () =>
        //    {
        //        List<Role> roles = await LoadRoleListForUserExtended(userExtendedSaveBodyDTO.UserExtendedDTO.Id);
        //        foreach (Role role in roles)
        //        {
        //            if (userExtendedSaveBodyDTO.RoleIds.Contains(role.Id))
        //                userExtendedSaveBodyDTO.RoleIds.Remove(role.Id);
        //            else
        //                _context.DbSet<Role>().Remove(role); // TODO FT: Benchmark which is better for performance in this case, Remove, or ExecuteDelete
        //        }

        //        List<Role> rolesToInsert = await _context.DbSet<Role>()
        //                                    .Where(x => userExtendedSaveBodyDTO.RoleIds.Contains(x.Id))
        //                                    .ToListAsync();

        //        await _context.DbSet<Role>().AddRangeAsync(rolesToInsert);
        //    });

        //}

    }
}
