﻿using System.Threading;
using System.Threading.Tasks;
using AspNetCore.Identity.DynamoDB.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace AspNetCore.Identity.DynamoDB.Tests
{
	public class UserStoreTests
	{
		[Fact]
		public async Task CreateAsync_ShouldCreateUser()
		{
			// ARRANGE
			using (var dbProvider = DynamoDbServerTestUtils.CreateDatabase())
			{
                var roleStore = new DynamoRoleUsersStore<DynamoIdentityRole, DynamoIdentityUser>();
                await roleStore.EnsureInitializedAsync(dbProvider.Client, dbProvider.Context);

                var userStore = new DynamoUserStore<DynamoIdentityUser, DynamoIdentityRole>(roleStore);
                await userStore.EnsureInitializedAsync(dbProvider.Client, dbProvider.Context);

				var generalStore = userStore as IUserStore<DynamoIdentityUser>;

				var user = new DynamoIdentityUser(TestUtils.RandomString(10));

				// ACT
				await generalStore.CreateAsync(user, CancellationToken.None);

				// ASSERT
				var retrievedUser = await dbProvider.Context.LoadAsync(user);

				Assert.NotNull(retrievedUser);
				Assert.Equal(user.UserName, retrievedUser.UserName);
				Assert.Equal(user.NormalizedUserName, retrievedUser.NormalizedUserName);
			}
		}
	}
}