﻿using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using Shouldly;
using StrongGrid.Models;
using StrongGrid.Resources;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace StrongGrid.UnitTests.Resources
{
	public class BlocksTests
	{
		#region FIELDS

		private const string ENDPOINT = "suppression/blocks";

		private const string SINGLE_BLOCK_JSON = @"{
			'created': 1443651154,
			'email': 'user1@example.com',
			'reason': 'error dialing remote address: dial tcp 10.57.152.165:25: no route to host',
			'status': '4.0.0'
		}";
		private const string MULTIPLE_BLOCKS_JSON = @"[
			{
				'created': 1443651154,
				'email': 'user1@example.com',
				'reason': 'error dialing remote address: dial tcp 10.57.152.165:25: no route to host',
				'status': '4.0.0'
			}
		]";

		#endregion

		[Fact]
		public void Parse_json()
		{
			// Arrange

			// Act
			var result = JsonConvert.DeserializeObject<Block>(SINGLE_BLOCK_JSON);

			// Assert
			result.ShouldNotBeNull();
			result.CreatedOn.ShouldBe(new System.DateTime(2015, 9, 30, 22, 12, 34, 0, System.DateTimeKind.Utc));
			result.Email.ShouldBe("user1@example.com");
			result.Reason.ShouldBe("error dialing remote address: dial tcp 10.57.152.165:25: no route to host");
			result.Status.ShouldBe("4.0.0");
		}

		[Fact]
		public async Task GetAllAsync()
		{
			// Arrange
			var mockHttp = new MockHttpMessageHandler();
			mockHttp.Expect(HttpMethod.Get, Utils.GetSendGridApiUri(ENDPOINT) + $"?start_time=&end_time=&limit=25&offset=0").Respond("application/json", MULTIPLE_BLOCKS_JSON);

			var client = Utils.GetFluentClient(mockHttp);
			var blocks = new Blocks(client);

			// Act
			var result = await blocks.GetAllAsync().ConfigureAwait(false);

			// Assert
			mockHttp.VerifyNoOutstandingExpectation();
			mockHttp.VerifyNoOutstandingRequest();
			result.ShouldNotBeNull();
			result.Length.ShouldBe(1);
			result[0].Email.ShouldBe("user1@example.com");
		}

		[Fact]
		public async Task DeleteAllAsync()
		{
			// Arrange
			var mockHttp = new MockHttpMessageHandler();
			mockHttp.Expect(HttpMethod.Delete, Utils.GetSendGridApiUri(ENDPOINT)).Respond(HttpStatusCode.NoContent);

			var client = Utils.GetFluentClient(mockHttp);
			var blocks = new Blocks(client);

			// Act
			await blocks.DeleteAllAsync().ConfigureAwait(false);

			// Assert
			mockHttp.VerifyNoOutstandingExpectation();
			mockHttp.VerifyNoOutstandingRequest();
		}

		[Fact]
		public async Task DeleteMultipleAsync()
		{
			// Arrange
			var emailAddresses = new[] { "email1@test.com", "email2@test.com" };

			var mockHttp = new MockHttpMessageHandler();
			mockHttp.Expect(HttpMethod.Delete, Utils.GetSendGridApiUri(ENDPOINT)).Respond(HttpStatusCode.NoContent);

			var client = Utils.GetFluentClient(mockHttp);
			var blocks = new Blocks(client);

			// Act
			await blocks.DeleteMultipleAsync(emailAddresses).ConfigureAwait(false);

			// Assert
			mockHttp.VerifyNoOutstandingExpectation();
			mockHttp.VerifyNoOutstandingRequest();
		}

		[Fact]
		public async Task DeleteAsync()
		{
			// Arrange
			var emailAddress = "spam1@test.com";

			var mockHttp = new MockHttpMessageHandler();
			mockHttp.Expect(HttpMethod.Delete, Utils.GetSendGridApiUri(ENDPOINT, emailAddress)).Respond(HttpStatusCode.NoContent);

			var client = Utils.GetFluentClient(mockHttp);
			var blocks = new Blocks(client);

			// Act
			await blocks.DeleteAsync(emailAddress).ConfigureAwait(false);

			// Assert
			mockHttp.VerifyNoOutstandingExpectation();
			mockHttp.VerifyNoOutstandingRequest();
		}

		[Fact]
		public async Task GetAsync()
		{
			// Arrange
			var emailAddress = "user1@example.com";

			var mockHttp = new MockHttpMessageHandler();
			mockHttp.Expect(HttpMethod.Get, Utils.GetSendGridApiUri(ENDPOINT, emailAddress)).Respond("application/json", SINGLE_BLOCK_JSON);

			var client = Utils.GetFluentClient(mockHttp);
			var blocks = new Blocks(client);

			// Act
			var result = await blocks.GetAsync(emailAddress, null, CancellationToken.None).ConfigureAwait(false);

			// Assert
			mockHttp.VerifyNoOutstandingExpectation();
			mockHttp.VerifyNoOutstandingRequest();
			result.ShouldNotBeNull();
			result.Email.ShouldBe(emailAddress);
		}
	}
}
