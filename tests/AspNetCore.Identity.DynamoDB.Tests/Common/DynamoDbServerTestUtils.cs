﻿using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace AspNetCore.Identity.DynamoDB.Tests.Common
{
	internal static class DynamoDbServerTestUtils
	{
		public static DisposableDatabase CreateDatabase() => new DisposableDatabase();

		public class DisposableDatabase : IDisposable
		{
			private bool _disposed;

			public DisposableDatabase()
			{
                var creds = new Amazon.Runtime.BasicAWSCredentials("test", "test");
				Client = new AmazonDynamoDBClient(creds, new AmazonDynamoDBConfig
				{
					ServiceURL = "http://localhost:8000"
				});
				Context = new DynamoDBContext(Client);
			}

			public IAmazonDynamoDB Client { get; }

			public IDynamoDBContext Context { get; }

			public void Dispose()
			{
				if (_disposed)
				{
					return;
				}
				Client.Dispose();
				_disposed = true;
			}
		}
	}
}
