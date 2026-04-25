using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Pug.Application.Data
{
	public abstract class ApplicationData<T>
		: IApplicationData<T>
		where T : class, IApplicationDataSession
	{
		private readonly string _location;
		private readonly DbProviderFactory _dataProvider;
		private readonly IEnumerable<SchemaVersion> _schemaVersions;

		protected ApplicationData(string location, DbProviderFactory dataProvider)
		{
			this._location = location;
			this._dataProvider = dataProvider;
			
			_schemaVersions = InitializeUpgradeScripts();
		}

		protected string Location
		{
			get
			{
				return _location;
			}
		}

		protected DbProviderFactory DataAccessProvider
		{
			get
			{
				return _dataProvider;
			}
		}

		public T GetSession()
		{
			IDbConnection connection = _dataProvider.CreateConnection();
			connection.ConnectionString = _location;

			try
			{
				connection.Open();
			}
			catch ( Exception exception )
			{
				throw new UnableToConnect( "Unable to connect to database given the connection string and DbProviderFactory", exception );
			}

			return CreateApplicationDataSession( connection, DataAccessProvider);
		}

		protected abstract T CreateApplicationDataSession(IDbConnection databaseSession, DbProviderFactory dataAccessProvider );

		protected abstract IEnumerable<SchemaVersion> InitializeUpgradeScripts();
		
		public IEnumerable<SchemaUpgradeScript> GetSchemaUpgradeScripts(int currentVersion)
		{
			IEnumerable<SchemaVersion> pendingVersions =
				from schemaVersion in _schemaVersions where schemaVersion.Number > currentVersion select schemaVersion;

			List<SchemaUpgradeScript> upgradeScripts = new ( pendingVersions.Sum( x => x.UpgradeScripts.Count() ) );
			
			foreach( SchemaVersion schemaVersion in pendingVersions )
			{
				int sequence = 0;

				upgradeScripts.AddRange( from upgradeScript in schemaVersion.UpgradeScripts select new SchemaUpgradeScript( schemaVersion.Number, sequence, upgradeScript ) );
			}

			return upgradeScripts;
		}

		public IEnumerable<SchemaUpgradeScript> GetSchemaUpgradeScripts()
		{
			return GetSchemaUpgradeScripts( _schemaVersions.Select( x => x.Number ).Min() - 1 );
		}
	}
}
