﻿using System.Linq;
using EntityFramework.DbContextScope.Interfaces;
using Microsoft.EntityFrameworkCore;
using MoneyFox.DataAccess.Entities;

namespace MoneyFox.DataAccess.Repositories
{
    /// <summary>
    ///     Grants access to the data stored in the category table on the database.
    ///     To commit changes use the UnitOfWork.
    /// </summary>
    public class CategoryRepository : RepositoryBase<CategoryEntity>, ICategoryRepository
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public CategoryRepository(IAmbientDbContextLocator ambientDbContextLocator) : base(ambientDbContextLocator)
        {
        }

        protected override void AttachForeign(CategoryEntity entity)
        {
            if (entity.Payments != null && entity.Payments.Any())
            {
                DbContext.Attach(entity.Payments);
            }
            if (entity.RecurringPayments != null && entity.RecurringPayments.Any())
            {
                DbContext.Attach(entity.RecurringPayments);
            }
        }
    }
}