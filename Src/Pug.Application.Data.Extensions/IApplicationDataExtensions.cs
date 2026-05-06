using System;
using System.Threading.Tasks;
using System.Transactions;

namespace Pug.Application.Data.Extensions
{
	public static class IApplicationDataExtensions
	{
		/// <summary>
		/// This is a wrapper function that allows developer to perform data tasks without having to worry about connection leak.
		/// </summary>
		/// <param name="action">Action to perform when a new instance of T data session has been successfully created.</param>
		/// <param name="transactionScopeOption">Specifies requirement and the scope of transaction.</param>
		/// <param name="onError">Action to perform when an error occured prior to completion of <paramref name="action"/>, this includes when error occured during creation of T data session instance.</param>
		/// <param name="errorHandler">Specifies if and how an error is to be handled. Corresponding error would be thrown by the function if this parameter is null.</param>
		/// <param name="onSuccess">Action to perform upon successful completion of <paramref name="action"/>. Created instance of T data session would have already been disposed at this stage.</param>
		/// <param name="onFinished">Action to perform upon completion of <paramref name="action"/> regardless of whether it was successful.</param>
		public static void Perform<T, C>(
			this IApplicationData<T> applicationData, 
			Action<T, C> action, 
			C context,
			TransactionScopeOption transactionScopeOption,
			TransactionOptions transactionOptions,
			Action<Exception, C> onError = null,
			Action<Exception, C> errorHandler = null, 
			Action<C> onSuccess = null, 
			Action<C> onFinished = null
		)
			where T : class, IApplicationDataSession
		{
			T dataSession = null;
			bool successful = true;

			try
			{
				if(transactionScopeOption != TransactionScopeOption.RequiresNew && Transaction.Current != null &&
					Transaction.Current.IsolationLevel == transactionOptions.IsolationLevel)
				{
					using( dataSession = applicationData.GetSession() )
					{
						action(dataSession, context);
					}
				}
				else
				{
					using(TransactionScope tx = new TransactionScope(transactionScopeOption, transactionOptions))
					{
						dataSession = applicationData.GetSession();

						action(dataSession, context);

						tx.Complete();
					}
				}
			}
			catch (Exception exception)
			{
				successful = false;

				if (onError != null)
					onError(exception, context);

				if (errorHandler == null)
					throw;

				errorHandler(exception, context);
			}
			finally
			{
				if (dataSession != null)
					try
					{
						dataSession.Dispose();
					}
					catch (Exception)
					{
						// todo: log error?
					}

				if (successful && onSuccess != null)
					onSuccess(context);

				if (onFinished != null)
					onFinished(context);
			}
		}

		/// <summary>
		/// This is a wrapper function that allows developer to perform data tasks without having to worry about connection leak. 
		/// </summary>
		/// <param name="action">Action to perform when a new instance of T data session has been successfully created.</param>
		/// <param name="onError">Action to perform when an error occured prior to completion of <paramref name="action"/>, this includes when error occured during creation of T data session instance.</param>
		/// <param name="errorHandler">Specifies if and how an error is to be handled. Corresponding error would be thrown by the function if this parameter is null.</param>
		/// <param name="onSuccess">Action to perform upon successful completion of <paramref name="action"/>. Created instance of T data session would have already been disposed at this stage.</param>
		/// <param name="onFinished">Action to perform upon completion of <paramref name="action"/> regardless of whether it was successful.</param>
		public static async Task PerformAsync<TDataSession, TContext>(
			this IApplicationData<TDataSession> applicationData, 
			Func<TDataSession, TContext, Task> action, 
			TContext context,
			TransactionScopeOption transactionScopeOption,
			TransactionOptions transactionOptions,
			Action<Exception, TContext> onError = null,
			Action<Exception, TContext> errorHandler = null, 
			Action<TContext> onSuccess = null, 
			Action<TContext> onFinished = null
		)		
			where TDataSession : class, IApplicationDataSession
		{
			TDataSession dataSession = null;
			bool successful = true;

			try
			{
				if(transactionScopeOption != TransactionScopeOption.RequiresNew && Transaction.Current != null &&
					Transaction.Current.IsolationLevel == transactionOptions.IsolationLevel)
				{
					using( dataSession = applicationData.GetSession() )
					{
						await action(dataSession, context).ConfigureAwait(false);
					}
				}
				else
				{
					using(TransactionScope tx = new TransactionScope(transactionScopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
					{
						dataSession = applicationData.GetSession();

						await action(dataSession, context).ConfigureAwait(false);

						tx.Complete();
					}
				}
			}
			catch (Exception exception)
			{
				successful = false;

				if (onError != null)
					onError(exception, context);

				if (errorHandler == null)
					throw;

				errorHandler(exception, context);
			}
			finally
			{
				if (dataSession != null)
					try
					{
						dataSession.Dispose();
					}
					catch (Exception)
					{
						// todo: log error?
					}

				if (successful && onSuccess != null)
					onSuccess(context);

				if (onFinished != null)
					onFinished(context);
			}
		}

		/// <summary>
		/// This is a wrapper function that allows developer to perform data tasks without having to worry about connection leak. 
		/// </summary>
		/// <param name="action">Action to perform when a new instance of T data session has been successfully created.</param>
		/// <param name="transactionScopeOption">Specifies requirement and the scope of transaction.</param>
		/// <param name="onError">Action to perform when an error occured prior to completion of <paramref name="action"/>, this includes when error occured during creation of T data session instance.</param>
		/// <param name="errorHandler">Specifies if and how an error is to be handled. Corresponding error would be thrown by the function if this parameter is null.</param>
		/// <param name="onSuccess">Action to perform upon successful completion of <paramref name="action"/>. Created instance of T data session would have already been disposed at this stage.</param>
		/// <param name="onFinished">Action to perform upon completion of <paramref name="action"/> regardless of whether it was successful.</param>
		public static void Perform<T, C>(
			this IApplicationData<T> applicationData, 
			Action<T, C> action, 
			C context,
			TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
			Action<Exception, C> onError = null,
			Action<Exception, C> errorHandler = null, 
			Action<C> onSuccess = null, 
			Action<C> onFinished = null
		)		
			where T : class, IApplicationDataSession
		{
			T dataSession = null;
			bool successful = true;

			try
			{
				using (TransactionScope tx = new TransactionScope(transactionScopeOption))
				{
					dataSession = applicationData.GetSession();

					action(dataSession, context);

					tx.Complete();
				}
			}
			catch (Exception exception)
			{
				successful = false;

				if (onError != null)
					onError(exception, context);

				if (errorHandler == null)
					throw;

				errorHandler(exception, context);
			}
			finally
			{
				if (dataSession != null)
					try
					{
						dataSession.Dispose();
					}
					catch (Exception)
					{
						// todo: log error?
					}

				if (successful && onSuccess != null)
					onSuccess(context);

				if (onFinished != null)
					onFinished(context);
			}
		}
		
		public static async Task PerformAsync<TDataSession, TContext>(
			this IApplicationData<TDataSession> applicationData, 
			Func<TDataSession, TContext, Task> action, 
			TContext context,
			TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
			Action<Exception, TContext> onError = null,
			Action<Exception, TContext> errorHandler = null, 
			Action<TContext> onSuccess = null, 
			Action<TContext> onFinished = null
		)		
			where TDataSession : class, IApplicationDataSession
		{
			TDataSession dataSession = null;
			bool successful = true;

			try
			{
				using (TransactionScope tx = new TransactionScope(transactionScopeOption, TransactionScopeAsyncFlowOption.Enabled))
				{
					dataSession = applicationData.GetSession();

					await action(dataSession, context).ConfigureAwait(false);

					tx.Complete();
				}
			}
			catch (Exception exception)
			{
				successful = false;

				if (onError != null)
					onError(exception, context);

				if (errorHandler == null)
					throw;

				errorHandler(exception, context);
			}
			finally
			{
				if (dataSession != null)
					try
					{
						dataSession.Dispose();
					}
					catch (Exception)
					{
						// todo: log error?
					}

				if (successful && onSuccess != null)
					onSuccess(context);

				if (onFinished != null)
					onFinished(context);
			}
		}

		public static R Execute<T, C, R>(
			this IApplicationData<T> applicationData, 
			Func<T, C, R> function, 
			C context,
			TransactionScopeOption transactionScopeOption,
			TransactionOptions transactionOptions,
			Action<Exception, C> onError = null, 
			Action<Exception, C> errorHandler = null, 
			Action<C> onSuccess = null, 
			Action<C> onFinished = null
		)
			where T : class, IApplicationDataSession
		{
			T dataSession = null;
			bool successful = true;
			R result = default(R);

			try
			{
				if(transactionScopeOption != TransactionScopeOption.RequiresNew && Transaction.Current != null &&
					Transaction.Current.IsolationLevel == transactionOptions.IsolationLevel)
				{
					using( dataSession = applicationData.GetSession() )
					{
						result = function(dataSession, context);
					}
				}
				else
				{
					using(TransactionScope tx = new TransactionScope(transactionScopeOption, transactionOptions))
					{
						using(dataSession = applicationData.GetSession())
						{
							result = function(dataSession, context);
							tx.Complete();
						}
					}
				}
			}
			catch (Exception exception)
			{
				successful = false;

				if (onError != null)
					onError(exception, context);

				if (errorHandler == null)
					throw;

				errorHandler(exception, context);
			}
			finally
			{
				if (successful && onSuccess != null)
					onSuccess(context);

				if (onFinished != null)
					onFinished(context);
			}

			return result;
		}

		public static async Task<TResult> ExecuteAsync<TDataSession, TContext, TResult>(
			this IApplicationData<TDataSession> applicationData, 
			Func<TDataSession, TContext, Task<TResult>> function, 
			TContext context,
			TransactionScopeOption transactionScopeOption,
			TransactionOptions transactionOptions,
			Action<Exception, TContext> onError = null, 
			Action<Exception, TContext> errorHandler = null, 
			Action<TContext> onSuccess = null, 
			Action<TContext> onFinished = null
		)
			where TDataSession : class, IApplicationDataSession
		{
			TDataSession dataSession = null;
			bool successful = true;
			TResult result = default(TResult);

			try
			{
				if(transactionScopeOption != TransactionScopeOption.RequiresNew && Transaction.Current != null &&
					Transaction.Current.IsolationLevel == transactionOptions.IsolationLevel)
				{
					using( dataSession = applicationData.GetSession() )
					{
						result = await function(dataSession, context).ConfigureAwait(false);
					}
				}
				else
				{
					using(TransactionScope tx = new TransactionScope(transactionScopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
					{
						using(dataSession = applicationData.GetSession())
						{
							result = await function(dataSession, context).ConfigureAwait(false);
							tx.Complete();
						}
					}
				}
			}
			catch (Exception exception)
			{
				successful = false;

				if (onError != null)
					onError(exception, context);

				if (errorHandler == null)
					throw;

				errorHandler(exception, context);
			}
			finally
			{
				if (successful && onSuccess != null)
					onSuccess(context);

				if (onFinished != null)
					onFinished(context);
			}

			return result;
		}

		public static R Execute<T, C, R>(
			this IApplicationData<T> applicationData,
			Func<T, C, R> function,
			C context,
			TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
			Action<Exception, C> onError = null,
			Action<Exception, C> errorHandler = null,
			Action<C> onSuccess = null,
			Action<C> onFinished = null
		)
			where T : class, IApplicationDataSession
		{
			T dataSession = null;
			bool successful = true;
			R result = default(R);

			try
			{
				using (TransactionScope tx = new TransactionScope(transactionScopeOption))
				{
					using( dataSession = applicationData.GetSession() )
					{
						result = function(dataSession, context);

						tx.Complete();
					}
				}
			}
			catch (Exception exception)
			{
				successful = false;

				if (onError != null)
					onError(exception, context);

				if (errorHandler == null)
					throw;

				errorHandler(exception, context);
			}
			finally
			{
				if (successful && onSuccess != null)
					onSuccess(context);

				if (onFinished != null)
					onFinished(context);
			}

			return result;
		}
		
		public static async Task<TResult> ExecuteAsync<TDataSession, TContext, TResult>(
			this IApplicationData<TDataSession> applicationData,
			Func<TDataSession, TContext, Task<TResult>> function,
			TContext context,
			TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
			Action<Exception, TContext> onError = null,
			Action<Exception, TContext> errorHandler = null,
			Action<TContext> onSuccess = null,
			Action<TContext> onFinished = null
		)
			where TDataSession : class, IApplicationDataSession
		{
			TDataSession dataSession = null;
			bool successful = true;
			TResult result = default(TResult);

			try
			{
				using (TransactionScope tx = new TransactionScope(transactionScopeOption, TransactionScopeAsyncFlowOption.Enabled))
				{
					using( dataSession = applicationData.GetSession() )
					{
						result = await function(dataSession, context).ConfigureAwait(false);

						tx.Complete();
					}
				}
			}
			catch (Exception exception)
			{
				successful = false;

				if (onError != null)
					onError(exception, context);

				if (errorHandler == null)
					throw;

				errorHandler(exception, context);
			}
			finally
			{
				if (successful && onSuccess != null)
					onSuccess(context);

				if (onFinished != null)
					onFinished(context);
			}

			return result;
		}

		[Obsolete]
		public static R Call<T, C, R>(
			this IApplicationData<T> applicationData,
			Func<T, C, R> function,
			C context,
			TransactionScopeOption transactionScopeOption = TransactionScopeOption.Required,
			Action<Exception, C> onError = null,
			Action<Exception, C> errorHandler = null,
			Action<C> onSuccess = null,
			Action<C> onFinished = null
		)
			where T : class, IApplicationDataSession
		{
			return Execute( applicationData, function, context, transactionScopeOption, onError, errorHandler, onSuccess, onFinished );
		}
	}

	}
