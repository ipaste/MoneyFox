﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using MoneyManager.Foundation.Model;
using MoneyManager.Foundation.OperationContracts;
using PropertyChanged;

namespace MoneyManager.Core.Repositories
{
    [ImplementPropertyChanged]
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IDataAccess<FinancialTransaction> dataAccess;
        private readonly IDataAccess<RecurringTransaction> recurringDataAccess;
        private ObservableCollection<FinancialTransaction> data;

        /// <summary>
        ///     Creates a TransactionRepository Object
        /// </summary>
        /// <param name="dataAccess">Instanced financial transaction data Access</param>
        public TransactionRepository(IDataAccess<FinancialTransaction> dataAccess, IDataAccess<RecurringTransaction> recurringDataAccess)
        {
            this.dataAccess = dataAccess;
            this.recurringDataAccess = recurringDataAccess;
            data = new ObservableCollection<FinancialTransaction>(this.dataAccess.LoadList());
        }

        /// <summary>
        ///     cached transaction data
        /// </summary>
        public ObservableCollection<FinancialTransaction> Data
        {
            get { return data ?? (data = new ObservableCollection<FinancialTransaction>(dataAccess.LoadList())); }
            set
            {
                if (data == null)
                {
                    data = new ObservableCollection<FinancialTransaction>(dataAccess.LoadList());
                }
                if (Equals(data, value))
                {
                    return;
                }
                data = value;
            }
        }

        /// <summary>
        ///     The currently selected Transaction
        /// </summary>
        public FinancialTransaction Selected { get; set; }

        /// <summary>
        ///     Save a new item or update an existin one.
        /// </summary>
        /// <param name="item">item to save</param>
        public void Save(FinancialTransaction item)
        {
            if (item.ChargedAccount == null)
            {
                throw new InvalidDataException("charged accout is missing");
            }

            if (item.Id == 0)
            {
                data.Add(item);
            }

            //delete recurring transaction if isRecurring is no longer set.
            if (!item.IsRecurring && item.ReccuringTransactionId.HasValue)
            {
                recurringDataAccess.Delete(item.RecurringTransaction);
                item.ReccuringTransactionId = null;
            }

            dataAccess.Save(item);
        }

        /// <summary>
        ///     Deletes the passed item and removes the item from cache
        /// </summary>
        /// <param name="item">item to delete</param>
        public void Delete(FinancialTransaction item)
        {
            var reucurringList = recurringDataAccess.LoadList(x => x.Id == item.ReccuringTransactionId).ToList();

            foreach (var recTrans in reucurringList)
            {
                recurringDataAccess.Delete(recTrans);
            }

            data.Remove(item);
            dataAccess.Delete(item);
        }

        /// <summary>
        ///     Loads all transactions from the database to the data collection
        /// </summary>
        public void Load(Expression<Func<FinancialTransaction, bool>> filter = null)
        {
            Data = new ObservableCollection<FinancialTransaction>(dataAccess.LoadList(filter));
        }

        /// <summary>
        ///     Returns all transaction with date before today
        /// </summary>
        /// <returns>list of uncleared transactions</returns>
        public IEnumerable<FinancialTransaction> GetUnclearedTransactions()
        {
            return GetUnclearedTransactions(DateTime.Today);
        }

        /// <summary>
        ///     Returns all transaction with date in this month
        /// </summary>
        /// <returns>list of uncleared transactions</returns>
        public IEnumerable<FinancialTransaction> GetUnclearedTransactions(DateTime date)
        {
            return Data.Where(x => x.IsCleared == false
                                   && x.Date.Date <= date.Date).ToList();
        }

        /// <summary>
        ///     returns a list with transaction who is related to this account
        /// </summary>
        /// <param name="account">account to search the related</param>
        /// <returns>List of transactions</returns>
        public IEnumerable<FinancialTransaction> GetRelatedTransactions(Account account)
        {
            return Data
                .Where(x => x.ChargedAccount != null)
                .Where(
                    x =>
                        x.ChargedAccount.Id == account.Id ||
                        (x.TargetAccount != null && x.TargetAccount.Id == account.Id))
                .OrderByDescending(x => x.Date)
                .ToList();
        }

        /// <summary>
        ///     returns a list with transaction who recure in a given timeframe
        /// </summary>
        /// <returns>list of recurring transactions</returns>
        public IEnumerable<FinancialTransaction> LoadRecurringList(Func<FinancialTransaction, bool> filter = null)
        {
            var list = Data.Where(x => x.IsRecurring && x.RecurringTransaction != null
                                     && (x.RecurringTransaction.IsEndless || x.RecurringTransaction.EndDate >= DateTime.Now.Date)
                                     && (filter == null || filter.Invoke(x)))
                                     .ToList();

            var recurringIds = list.Select(x => x.ReccuringTransactionId).Distinct().ToList();

            return recurringIds
                .Select(id => list.Where(x => x.ReccuringTransactionId == id)
                    .OrderByDescending(x => x.Date)
                    .Last())
                .ToList();
        }
    }
}