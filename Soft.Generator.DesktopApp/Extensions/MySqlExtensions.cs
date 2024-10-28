using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soft.Generator.DesktopApp.Extensions
{
    public static class MySqlExtensions
    {
        private static AsyncLocal<Stack<MySqlTransaction>> _transactionStack = new AsyncLocal<Stack<MySqlTransaction>>();

        public static T WithTransaction<T>(this MySqlConnection connection, Func<T> action)
        {
            Stack<MySqlTransaction> transactionStack = _transactionStack.Value ?? new Stack<MySqlTransaction>();
            bool isFirstTransaction = false;
            MySqlTransaction transaction;

            if (transactionStack.Count == 0)
            {
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
                    connection.Dispose();
                    transaction.Dispose();
                    transactionStack.Pop();
                    _transactionStack.Value = null;
                }
            }
        }

        public static void WithTransaction(this MySqlConnection connection, Action action)
        {
            Stack<MySqlTransaction> transactionStack = _transactionStack.Value ?? new Stack<MySqlTransaction>();
            bool isFirstTransaction = false;
            MySqlTransaction transaction;

            if (transactionStack.Count == 0)
            {
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
                    connection.Close();
                    transaction.Dispose();
                    transactionStack.Pop();
                    _transactionStack.Value = null;
                }
            }
        }

    }
}
