using Microsoft.EntityFrameworkCore.Storage;
using Soft.Generator.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Soft.Generator.Shared.Extensions
{
    public static class ApplicationDbContextExtensions
    {
        /// <summary>
        /// It could only be 0 or 1 transaction in the stack
        /// 0 - begin the new transaction
        /// 1 - work with the first transaction
        /// </summary>
        private static AsyncLocal<Stack<IDbContextTransaction>> _transactionStack = new AsyncLocal<Stack<IDbContextTransaction>>();

        /// <summary>
        /// Explanation why you should use transaction for multiple read operations also: https://stackoverflow.com/questions/308905/should-there-be-a-transaction-for-read-queries
        /// Base isolation level for sql server (and EF) is READ COMMITED
        /// </summary>
        public static async Task<T> WithTransactionAsync<T>(this IApplicationDbContext context, Func<Task<T>> action)
        {
            Stack<IDbContextTransaction> transactionStack = _transactionStack.Value ?? new Stack<IDbContextTransaction>();
            bool isFirstTransaction = false;
            IDbContextTransaction transaction;

            if (transactionStack.Count == 0)
            {
                transaction = await context.Database.BeginTransactionAsync(); // Start a new transaction if no existing transaction
                transactionStack.Push(transaction);
                _transactionStack.Value = transactionStack;
                isFirstTransaction = true;
            }
            else
            {
                transaction = transactionStack.Peek(); // Use the existing transaction (Peek only reads the last one on the stack)
            }

            try
            {
                T result = await action();

                if (isFirstTransaction)
                    await transaction.CommitAsync();

                return result;
            }
            catch
            {
                if (isFirstTransaction)
                    await transaction.RollbackAsync();

                throw;
            }
            finally
            {
                if (isFirstTransaction)
                {
                    await transaction.DisposeAsync();
                    transactionStack.Pop();
                    _transactionStack.Value = null;
                }
            }
        }

        /// <summary>
        /// Overload method for void transactions
        /// </summary>
        public static async Task WithTransactionAsync(this IApplicationDbContext context, Func<Task> action)
        {
            Stack<IDbContextTransaction> transactionStack = _transactionStack.Value ?? new Stack<IDbContextTransaction>();
            bool isFirstTransaction = false;
            IDbContextTransaction transaction;

            if (transactionStack.Count == 0)
            {
                transaction = await context.Database.BeginTransactionAsync(); // Start a new transaction if no existing transaction
                transactionStack.Push(transaction);
                _transactionStack.Value = transactionStack;
                isFirstTransaction = true;
            }
            else
            {
                transaction = transactionStack.Peek(); // Use the existing transaction (Peek only reads the last one on the stack)
            }

            try
            {
                await action();

                if (isFirstTransaction)
                    await transaction.CommitAsync();
            }
            catch
            {
                if (isFirstTransaction)
                    await transaction.RollbackAsync();

                throw;
            }
            finally
            {
                if (isFirstTransaction)
                {
                    await transaction.DisposeAsync();
                    transactionStack.Pop();
                    _transactionStack.Value = null;
                }
            }
        }
    }
}
