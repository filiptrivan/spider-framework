using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Soft.Generator.DesktopApp.Extensions
{
    public static class SqlExtensions
    {
        private static AsyncLocal<Stack<SqlTransaction>> _transactionStack = new AsyncLocal<Stack<SqlTransaction>>();

        public static T WithTransaction<T>(this SqlConnection connection, Func<T> action)
        {
            Stack<SqlTransaction> transactionStack = _transactionStack.Value ?? new Stack<SqlTransaction>();
            TransactionScope transactionScope = null;
            SqlTransaction transaction = null;
            bool isFirstTransaction = false;

            if (transactionStack.Count == 0)
            {
                transactionScope = new TransactionScope();
                connection.Open();
                transaction = connection.BeginTransaction(); // Start a new transaction if no existing transaction
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
                T result = action();

                if (isFirstTransaction)
                    transaction.Commit();

                return result;
            }
            catch
            {
                if (isFirstTransaction)
                    transaction.Rollback();

                throw;
            }
            finally
            {
                if (isFirstTransaction)
                {
                    transaction.Dispose();
                    transactionStack.Pop();
                    _transactionStack.Value = null;

                    connection.Close();

                    transactionScope.Dispose();
                }
            }
        }

        public static void WithTransaction(this SqlConnection connection, Action action)
        {
            Stack<SqlTransaction> transactionStack = _transactionStack.Value ?? new Stack<SqlTransaction>();
            TransactionScope transactionScope = null;
            SqlTransaction transaction = null;
            bool isFirstTransaction = false;

            if (transactionStack.Count == 0)
            {
                transactionScope = new TransactionScope();
                connection.Open();
                transaction = connection.BeginTransaction(); // Start a new transaction if no existing transaction
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
                action();

                if (isFirstTransaction)
                    transaction.Commit();
            }
            catch
            {
                if (isFirstTransaction)
                    transaction.Rollback();

                throw;
            }
            finally
            {
                if (isFirstTransaction)
                {
                    transaction.Dispose();
                    transactionStack.Pop();
                    _transactionStack.Value = null;

                    connection.Close();

                    transactionScope.Complete();
                    transactionScope.Dispose();
                }
            }
        }

    }
}
