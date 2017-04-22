﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Amazon.DynamoDBv2.DataModel;
using AspNetCore.Identity.DynamoDB.Converters;
using AspNetCore.Identity.DynamoDB.Models;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace AspNetCore.Identity.DynamoDB
{
	public class ClaimsEntry
	{
		public int Index { get; set; } = -1;

		public string ClaimType { get; set; }

		public IList<string> ClaimValues { get; set; } = new List<string>();

		public IEnumerable<Claim> GetClaims() {
			return from v in ClaimValues select new Claim(ClaimType, v);
		}
	}

	[DynamoDBTable(Constants.DefaultTableName)]
	public class DynamoIdentityUser
	{
		public DynamoIdentityUser()
		{
			ClaimTypes = new List<string>();
			ClaimValues = new List<string>();
			LoginProviders = new List<string>();
			LoginProviderKeys = new List<string>();
			LoginProviderDisplayNames = new List<string>();
		}

		public DynamoIdentityUser(string userName, string email) : this(userName)
		{
			if (email != null)
			{
				Email = new DynamoUserEmail(email);
				NormalizedEmail = email.ToUpper();
			}
		}

		public DynamoIdentityUser(string userName, DynamoUserEmail email) : this(userName)
		{
			if (email != null)
			{
				Email = email;
				NormalizedEmail = email.Value.ToUpper();
			}
		}

		public DynamoIdentityUser(string userName) : this()
		{
			if (userName == null)
			{
				throw new ArgumentNullException(nameof(userName));
			}

			UserName = userName;
			NormalizedUserName = userName.ToUpper();
			Id = Guid.NewGuid().ToString();
			CreatedOn = DateTimeOffset.Now;
		}

		[DynamoDBHashKey]
		public string Id { get; set; }

		public string UserName { get; set; }

		[DynamoDBGlobalSecondaryIndexHashKey("NormalizedUserName-DeletedOn-index")]
		public string NormalizedUserName { get; set; }

		public DynamoUserEmail Email { get; set; }

		[DynamoDBGlobalSecondaryIndexHashKey("NormalizedEmail-DeletedOn-index")]
		public string NormalizedEmail { get; set; }

		public DynamoUserPhoneNumber PhoneNumber { get; set; }
		public string PasswordHash { get; set; }
		public string SecurityStamp { get; set; }
		public bool IsTwoFactorEnabled { get; set; }

		public List<string> ClaimTypes { get; set; }
		public List<string> ClaimValues { get; set; }

		public List<string> LoginProviders { get; set; }
		public List<string> LoginProviderKeys { get; set; }
		public List<string> LoginProviderDisplayNames { get; set; }

		public int AccessFailedCount { get; set; }
		public bool IsLockoutEnabled { get; set; }

		[DynamoDBProperty(typeof(DateTimeOffsetConverter))]
		public DateTimeOffset LockoutEndDate { get; set; }

		[DynamoDBProperty(typeof(DateTimeOffsetConverter))]
		public DateTimeOffset CreatedOn { get; set; }

		[DynamoDBGlobalSecondaryIndexRangeKey("NormalizedEmail-DeletedOn-index",
			"NormalizedUserName-DeletedOn-index", Converter = typeof(DateTimeOffsetConverter))]
		public DateTimeOffset DeletedOn { get; set; }

		[DynamoDBVersion]
		public int? VersionNumber { get; set; }

		public virtual void EnableTwoFactorAuthentication()
		{
			IsTwoFactorEnabled = true;
		}

		public virtual void DisableTwoFactorAuthentication()
		{
			IsTwoFactorEnabled = false;
		}

		public virtual void EnableLockout()
		{
			IsLockoutEnabled = true;
		}

		public virtual void DisableLockout()
		{
			IsLockoutEnabled = false;
		}

		public virtual void SetEmail(string email)
		{
			var dynamoUserEmail = new DynamoUserEmail(email);
			NormalizedEmail = email.ToUpper();
			SetEmail(dynamoUserEmail);
		}

		public virtual void SetEmail(DynamoUserEmail dynamoUserEmail)
		{
			Email = dynamoUserEmail;
		}

		public virtual void SetNormalizedUserName(string normalizedUserName)
		{
			if (normalizedUserName == null)
			{
				throw new ArgumentNullException(nameof(normalizedUserName));
			}

			NormalizedUserName = normalizedUserName;
		}

		public virtual void SetPhoneNumber(string phoneNumber)
		{
			var dynamoUserPhoneNumber = new DynamoUserPhoneNumber(phoneNumber);
			SetPhoneNumber(dynamoUserPhoneNumber);
		}

		public virtual void SetPhoneNumber(DynamoUserPhoneNumber dynamoUserPhoneNumber)
		{
			PhoneNumber = dynamoUserPhoneNumber;
		}

		public virtual void SetPasswordHash(string passwordHash)
		{
			PasswordHash = passwordHash;
		}

		public virtual void SetSecurityStamp(string securityStamp)
		{
			SecurityStamp = securityStamp;
		}

		public virtual void SetAccessFailedCount(int accessFailedCount)
		{
			AccessFailedCount = accessFailedCount;
		}

		public virtual void ResetAccessFailedCount()
		{
			AccessFailedCount = 0;
		}

		public virtual void LockUntil(DateTimeOffset lockoutEndDate)
		{
			LockoutEndDate = lockoutEndDate;
		}
		
		public virtual bool HasClaim(Claim claim)
		{
			var entry = GetClaimsEntryByType(claim.Type);
			return entry.ClaimValues.Contains(claim.Value);
		}

		public virtual void AddClaim(Claim claim)
		{
			if (claim == null)
			{
				throw new ArgumentNullException(nameof(claim));
			}

			var entry = GetClaimsEntryByType(claim.Type);
			entry.ClaimValues.Add(claim.Value);
			SaveClaimEntry(entry);
		}

		void SaveClaimEntry(ClaimsEntry entry)
		{
			if (entry.ClaimValues.Any())
			{
				var values = JsonConvert.SerializeObject(entry.ClaimValues);

				if (entry.Index == -1)
				{
					ClaimTypes.Add(entry.ClaimType);
					ClaimValues.Add(values);
				} else {
					ClaimValues[entry.Index] = values;
				}
			}
			else if (entry.Index != -1)
			{
				ClaimTypes.RemoveAt(entry.Index);
				ClaimValues.RemoveAt(entry.Index);
			}
		}

		IEnumerable<ClaimsEntry> GetClaimsEntries()
		{
			return ClaimTypes.Select((t, i) => new ClaimsEntry {
				Index = i,
				ClaimType = t,
				ClaimValues = JsonConvert.DeserializeObject<IList<string>>(ClaimValues[i])
			});
		}

	    ClaimsEntry GetClaimsEntryByType(string type)
		{
			return (
				from e in GetClaimsEntries() 
				where e.ClaimType == type 
				select e
			)
			.DefaultIfEmpty(new ClaimsEntry {
				ClaimType = type
			})
			.First();
		}

		public virtual IList<Claim> GetClaims()
		{
			return (
				from e in GetClaimsEntries() 
				from v in e.ClaimValues 
				select new Claim(e.ClaimType, v)
			).ToList();
		}

		public virtual void RemoveClaim(Claim claim)
		{
			if (claim == null)
			{
				throw new ArgumentNullException(nameof(claim));
			}

			var entry = GetClaimsEntryByType(claim.Type);
			entry.ClaimValues.Remove(claim.Value);
			SaveClaimEntry(entry);
		}

		public virtual void AddLogin(UserLoginInfo loginInfo)
		{
			if (loginInfo == null)
			{
				throw new ArgumentNullException(nameof(loginInfo));
			}

			LoginProviders.Add(loginInfo.LoginProvider);
			LoginProviderKeys.Add(loginInfo.ProviderKey);
			LoginProviderDisplayNames.Add(loginInfo.ProviderDisplayName ?? loginInfo.LoginProvider);
		}

		public virtual IList<UserLoginInfo> GetLogins()
		{
			return
				LoginProviders.Select((t, i) => new UserLoginInfo(t, LoginProviderKeys[i], LoginProviderDisplayNames[i])).ToList();
		}

		public virtual void RemoveLogin(UserLoginInfo loginInfo)
		{
			if (loginInfo == null)
			{
				throw new ArgumentNullException(nameof(loginInfo));
			}

			var providerIndex = LoginProviders.IndexOf(loginInfo.LoginProvider);
			LoginProviders.Remove(loginInfo.LoginProvider);
			LoginProviderKeys.RemoveAt(providerIndex);
			LoginProviderDisplayNames.RemoveAt(providerIndex);
		}

		public void Delete()
		{
			if (DeletedOn != default(DateTimeOffset))
			{
				throw new InvalidOperationException($"User '{Id}' has already been deleted.");
			}

			DeletedOn = DateTimeOffset.Now;
		}
	}
}
