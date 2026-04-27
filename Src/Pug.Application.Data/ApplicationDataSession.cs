using System;
using System.Data;
#if NETFX || NETSTANDARD_2_0
using System.Transactions;
#endif

namespace Pug.Application.Data
{
	public abstract class ApplicationDataSession : IApplicationDataSession
	{
		private readonly IDbConnection _connection;
		private Chain<IDbTransaction>.Link _currentTxLink;

		private readonly object _transactionSync = new object();
		
		public ApplicationDataSession(IDbConnection databaseSession)
		{
			_connection = databaseSession;
		}

		//private void onTransactionCompleted(Chain<IDbTransaction>.Link link)
		//{
		//    link.Content.Dispose();
		//}

		private void OnTransactionDisposed(Chain<IDbTransaction>.Link link)
		{
			_currentTxLink = link.Previous;
			TransactionDepth--;
		}

		//IDbTransaction Mix(Chain<IDbTransaction>.Link link)
		//{
		//    IDbTransaction transaction = link.Content;

		//    Type transactionType = transaction.GetType();

		//    ProxyGenerationOptions options = new ProxyGenerationOptions();
		//    options.AddMixinInstance(link);

		//    TransactionInterceptor interceptor = new TransactionInterceptor(onTransactionCompleted, onTransactionDisposed);

		//    IDbTransaction proxy = (IDbTransaction)dynamicProxyGenerator.CreateClassProxyWithTarget(transactionType, link.Content, options, interceptor);

		//    return proxy;
		//}

		#region IApplicationData Members

		protected IDbConnection Connection
		{
			get
			{
				return _connection;
			}
		}

		protected IDbTransaction Transaction
		{   get
			{
				return _currentTxLink.Content;
			}
		}

		public int TransactionDepth { get; private set; }

		public void BeginTransaction()
		{
			lock (_transactionSync)
			{
				_currentTxLink = new Chain<IDbTransaction>.Link(Connection.BeginTransaction(), _currentTxLink);
				TransactionDepth++;
			}
		}

		public void BeginTransaction(System.Data.IsolationLevel isolationLevel )
		{
			lock (_transactionSync)
			{ 
				_currentTxLink = new Chain<IDbTransaction>.Link(Connection.BeginTransaction(isolationLevel), _currentTxLink);
				TransactionDepth++;
			}
		}

		public void RollbackTransaction()
		{
			lock (_transactionSync)
				if ( _currentTxLink != null)
					try
					{
						_currentTxLink.Content.Rollback();
					}
					finally
					{
						_currentTxLink.Content.Dispose();
						OnTransactionDisposed(_currentTxLink);
					}
		}

		public void CommitTransaction()
		{
			lock (_transactionSync)
				if (_currentTxLink != null)
					try
					{
						_currentTxLink.Content.Commit();
					}
					finally
					{
						_currentTxLink.Content.Dispose();
						OnTransactionDisposed(_currentTxLink);
					}
		}

#if NETFX
		public void EnlistInTransaction(Transaction transaction)
		{
			Connection.EnlistTransaction(transaction);
		}
#endif
		#endregion

		protected T EvaluateIsNullToDefault<T>(object obj)
		{
			if (DBNull.Value == obj)
				return default(T);

			return (T)obj;
		}
		
		protected T EvaluateIsNull<T>(object obj, T replacement)
		{
			if( DBNull.Value == obj )
			{
				return replacement;
			}
			
			return (T)obj;
		}

#region IDisposable Members

		public virtual void Dispose()
		{ 
			while( _currentTxLink != null )
				RollbackTransaction();

			Connection.Close();
		}

#endregion
	}
}
