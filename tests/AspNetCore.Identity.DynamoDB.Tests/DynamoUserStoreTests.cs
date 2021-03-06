﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AspNetCore.Identity.DynamoDB.Tests.Common;
using Xunit;

namespace AspNetCore.Identity.DynamoDB.Tests
{
	public class DynamoUserStoreTests
	{
		[Fact]
		public async Task DynamoUserStore_ShouldPutThingsIntoUsersTableByDefault()
		{
			var user = new DynamoIdentityUser(TestUtils.RandomString(10));
			using (var dbProvider = DynamoDbServerTestUtils.CreateDatabase())
			{
                var roleStore = new DynamoRoleUsersStore<DynamoIdentityRole, DynamoIdentityUser>();
                await roleStore.EnsureInitializedAsync(dbProvider.Client, dbProvider.Context);

				var store = new DynamoUserStore<DynamoIdentityUser, DynamoIdentityRole>(roleStore);
				await store.EnsureInitializedAsync(dbProvider.Client, dbProvider.Context);

				// ACT
				var result = await store.CreateAsync(user, CancellationToken.None);

				// ASSERT
				Assert.True(result.Succeeded);
				var tableNames = (await dbProvider.Client.ListTablesAsync()).TableNames;
				var tableExists = tableNames.Any(x => x.Equals("users", StringComparison.Ordinal));
				Assert.True(tableExists);
			}
		}
	}
}