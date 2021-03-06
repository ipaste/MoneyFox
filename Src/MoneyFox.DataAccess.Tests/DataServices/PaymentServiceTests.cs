﻿using System;
using System.IO;
using EntityFramework.DbContextScope;
using Microsoft.EntityFrameworkCore;
using MoneyFox.Business.ViewModels;
using MoneyFox.DataAccess.Entities;
using MoneyFox.DataAccess.Repositories;
using MoneyFox.Foundation;
using MoneyFox.Foundation.Constants;
using MoneyFox.Service;
using MoneyFox.Service.DataServices;
using MoneyFox.Service.Pocos;
using Xunit;

namespace MoneyFox.DataAccess.Tests.DataServices
{
    /// <summary>
    ///     This test is in the DataAccess project, because it tests the direct access to the database.
    /// </summary>
    public class PaymentServiceTests : IDisposable
    {
        /// <summary>
        ///     Setup Logic who is executed before every test
        /// </summary>
        public PaymentServiceTests()
        {
            ApplicationContext.DbPath = Path.Combine(AppContext.BaseDirectory, DatabaseConstants.DB_NAME);

            dbContextScopeFactory = new DbContextScopeFactory();
            ambientDbContextLocator = new AmbientDbContextLocator();

            using (dbContextScopeFactory.Create())
            {
                ambientDbContextLocator.Get<ApplicationContext>().Database.Migrate();
            }
            using (var db = new ApplicationContext())
            {
                db.Database.Migrate();
            }
        }

        /// <summary>
        ///     Cleanup logic who is executed after executign every test.
        /// </summary>
        public void Dispose()
        {
            if (File.Exists(ApplicationContext.DbPath))
            {
                File.Delete(ApplicationContext.DbPath);
            }
        }

        private readonly DbContextScopeFactory dbContextScopeFactory;
        private readonly AmbientDbContextLocator ambientDbContextLocator;

        [Fact]
        [Trait("Category", "Integration")]
        public async void Save_WithRecurringPayment_GetRecurringPaymentFromHelper()
        {
            // Arrange
            var paymentRepository = new PaymentRepository(ambientDbContextLocator);
            var recurringPaymentRepository = new RecurringPaymentRepository(ambientDbContextLocator);
            var accountRepository = new AccountRepository(ambientDbContextLocator);

            AccountEntity testAccount;

            using (var dbContextScope = dbContextScopeFactory.Create())
            {
                testAccount = new AccountEntity {Name = "testAccount"};
                accountRepository.Add(testAccount);
                await dbContextScope.SaveChangesAsync();
            }

            var testEntry = new PaymentViewModel(new Payment
                {
                    Data =
                    {
                        ChargedAccount = testAccount,
                        Date = DateTime.Now,
                        IsRecurring = true,
                        Note = "Testtext"
                    }
                });
                testEntry.RecurringPayment = new RecurringPaymentViewModel(
                    RecurringPaymentHelper.GetRecurringFromPayment(testEntry.Payment,
                                                                   true,
                                                                   PaymentRecurrence.Bimonthly,
                                                                   DateTime.Now));

                var paymentService = new PaymentService(dbContextScopeFactory, paymentRepository, recurringPaymentRepository, accountRepository);

                // Act
                await paymentService.SavePayment(testEntry.Payment);
                var payment = await paymentService.GetById(testEntry.Payment.Data.Id);

                // Assert
                Assert.NotNull(payment);
            }
        }
    }