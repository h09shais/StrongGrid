using Newtonsoft.Json.Linq;
using Pathoschild.Http.Client;
using StrongGrid.Models;
using StrongGrid.Utilities;
using System.Threading;
using System.Threading.Tasks;

namespace StrongGrid.Resources
{
	/// <summary>
	/// Allows you to set and check webhook settings.
	/// SendGrid’s Event Webhook will notify a URL of your choice via HTTP POST with information about events that occur as SendGrid processes your email.
	/// Common uses of this data are to remove unsubscribes, react to spam reports, determine unengaged recipients, identify bounced email addresses, or create advanced analytics of your email program.
	/// </summary>
	/// <seealso cref="StrongGrid.Resources.IWebhookSettings" />
	/// <remarks>
	/// See <a href="https://sendgrid.api-docs.io/v3.0/webhooks">SendGrid documentation</a> for more information.
	/// </remarks>
	public class WebhookSettings : IWebhookSettings
	{
		private const string _eventWebhookEndpoint = "user/webhooks/event";
		private const string _inboundParseWebhookEndpoint = "user/webhooks/parse";
		private readonly Pathoschild.Http.Client.IClient _client;

		/// <summary>
		/// Initializes a new instance of the <see cref="WebhookSettings" /> class.
		/// </summary>
		/// <param name="client">The HTTP client.</param>
		internal WebhookSettings(Pathoschild.Http.Client.IClient client)
		{
			_client = client;
		}

		/// <summary>
		/// Get the current event webhook settings.
		/// </summary>
		/// <param name="onBehalfOf">The user to impersonate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The <see cref="EventWebhookSettings" />.
		/// </returns>
		public Task<EventWebhookSettings> GetEventWebhookSettingsAsync(string onBehalfOf = null, CancellationToken cancellationToken = default)
		{
			return _client
				.GetAsync($"{_eventWebhookEndpoint}/settings")
				.OnBehalfOf(onBehalfOf)
				.WithCancellationToken(cancellationToken)
				.AsSendGridObject<EventWebhookSettings>();
		}

		/// <summary>
		/// Change the event webhook settings.
		/// </summary>
		/// <param name="enabled">if set to <c>true</c> [enabled].</param>
		/// <param name="url">the webhook endpoint url.</param>
		/// <param name="bounce">if set to <c>true</c> [bounce].</param>
		/// <param name="click">if set to <c>true</c> [click].</param>
		/// <param name="deferred">if set to <c>true</c> [deferred].</param>
		/// <param name="delivered">if set to <c>true</c> [delivered].</param>
		/// <param name="dropped">if set to <c>true</c> [dropped].</param>
		/// <param name="groupResubscribe">if set to <c>true</c> [groupResubscribe].</param>
		/// <param name="groupUnsubscribe">if set to <c>true</c> [groupUnsubscribe].</param>
		/// <param name="open">if set to <c>true</c> [open].</param>
		/// <param name="processed">if set to <c>true</c> [processed].</param>
		/// <param name="spamReport">if set to <c>true</c> [spamReport].</param>
		/// <param name="unsubscribe">if set to <c>true</c> [unsubscribe].</param>
		/// <param name="onBehalfOf">The user to impersonate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The <see cref="EventWebhookSettings" />.
		/// </returns>
		public Task<EventWebhookSettings> UpdateEventWebhookSettingsAsync(
			bool enabled,
			string url,
			bool bounce = default,
			bool click = default,
			bool deferred = default,
			bool delivered = default,
			bool dropped = default,
			bool groupResubscribe = default,
			bool groupUnsubscribe = default,
			bool open = default,
			bool processed = default,
			bool spamReport = default,
			bool unsubscribe = default,
			string onBehalfOf = null,
			CancellationToken cancellationToken = default)
		{
			var eventWebhookSettings = new EventWebhookSettings
			{
				Enabled = enabled,
				Url = url,
				Bounce = bounce,
				Click = click,
				Deferred = deferred,
				Delivered = delivered,
				Dropped = dropped,
				GroupResubscribe = groupResubscribe,
				GroupUnsubscribe = groupUnsubscribe,
				Open = open,
				Processed = processed,
				SpamReport = spamReport,
				Unsubscribe = unsubscribe
			};
			var data = JObject.FromObject(eventWebhookSettings);
			return _client
				.PatchAsync($"{_eventWebhookEndpoint}/settings")
				.OnBehalfOf(onBehalfOf)
				.WithJsonBody(data)
				.WithCancellationToken(cancellationToken)
				.AsSendGridObject<EventWebhookSettings>();
		}

		/// <summary>
		/// Sends a fake event notification post to the provided URL.
		/// </summary>
		/// <param name="url">the event notification endpoint url.</param>
		/// <param name="onBehalfOf">The user to impersonate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The async task.
		/// </returns>
		public Task SendEventTestAsync(string url, string onBehalfOf = null, CancellationToken cancellationToken = default)
		{
			var data = new JObject
			{
				{ "url", url }
			};

			return _client
				.PostAsync($"{_eventWebhookEndpoint}/test")
				.OnBehalfOf(onBehalfOf)
				.WithJsonBody(data)
				.WithCancellationToken(cancellationToken)
				.AsMessage();
		}

		/// <summary>
		/// Create inbound parse settings for a hostname.
		/// </summary>
		/// <param name="hostname">A specific and unique domain or sub-domain that you have created to use exclusively to parse your incoming email. For example, parse.yourdomain.com.</param>
		/// <param name="url">The public URL where you would like SendGrid to POST the data parsed from your email. Any emails sent with the given hostname provided (whose MX records have been updated to point to SendGrid) will be parsed and POSTed to this URL.</param>
		/// <param name="spamCheck">Indicates if you would like SendGrid to check the content parsed from your emails for spam before POSTing them to your domain.</param>
		/// <param name="sendRaw">Indicates if you would like SendGrid to post the original MIME-type content of your parsed email. When this parameter is set to "false", SendGrid will send a JSON payload of the content of your email.</param>
		/// <param name="onBehalfOf">The user to impersonate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The <see cref="InboundParseWebhookSettings" />.
		/// </returns>
		public Task<InboundParseWebhookSettings> CreateInboundParseWebhookSettingsAsync(string hostname, string url, bool spamCheck = false, bool sendRaw = false, string onBehalfOf = null, CancellationToken cancellationToken = default)
		{
			var data = CreateJObject(hostname, url, spamCheck, sendRaw);
			return _client
				.PostAsync($"{_inboundParseWebhookEndpoint}/settings")
				.OnBehalfOf(onBehalfOf)
				.WithJsonBody(data)
				.WithCancellationToken(cancellationToken)
				.AsSendGridObject<InboundParseWebhookSettings>();
		}

		/// <summary>
		/// Get all the inbound parse webhook settings.
		/// </summary>
		/// <param name="onBehalfOf">The user to impersonate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The <see cref="InboundParseWebhookSettings" />.
		/// </returns>
		public Task<InboundParseWebhookSettings[]> GetAllInboundParseWebhookSettingsAsync(string onBehalfOf = null, CancellationToken cancellationToken = default)
		{
			return _client
				.GetAsync($"{_inboundParseWebhookEndpoint}/settings")
				.OnBehalfOf(onBehalfOf)
				.WithCancellationToken(cancellationToken)
				.AsSendGridObject<InboundParseWebhookSettings[]>("result");
		}

		/// <summary>
		/// Get the inbound parse webhook settings for a specific hostname.
		/// </summary>
		/// <param name="hostname">The hostname associated with the inbound parse setting that you would like to retrieve.</param>
		/// <param name="onBehalfOf">The user to impersonate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The <see cref="InboundParseWebhookSettings" />.
		/// </returns>
		public Task<InboundParseWebhookSettings> GetInboundParseWebhookSettingsAsync(string hostname, string onBehalfOf = null, CancellationToken cancellationToken = default)
		{
			return _client
				.GetAsync($"{_inboundParseWebhookEndpoint}/settings/{hostname}")
				.OnBehalfOf(onBehalfOf)
				.WithCancellationToken(cancellationToken)
				.AsSendGridObject<InboundParseWebhookSettings>();
		}

		/// <summary>
		/// Update the inbound parse settings for a specific hostname.
		/// </summary>
		/// <param name="hostname">A specific and unique domain or sub-domain that you have created to use exclusively to parse your incoming email. For example, parse.yourdomain.com.</param>
		/// <param name="url">The public URL where you would like SendGrid to POST the data parsed from your email. Any emails sent with the given hostname provided (whose MX records have been updated to point to SendGrid) will be parsed and POSTed to this URL.</param>
		/// <param name="spamCheck">Indicates if you would like SendGrid to check the content parsed from your emails for spam before POSTing them to your domain.</param>
		/// <param name="sendRaw">Indicates if you would like SendGrid to post the original MIME-type content of your parsed email. When this parameter is set to "false", SendGrid will send a JSON payload of the content of your email.</param>
		/// <param name="onBehalfOf">The user to impersonate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The <see cref="InboundParseWebhookSettings" />.
		/// </returns>
		public Task UpdateInboundParseWebhookSettingsAsync(string hostname, Parameter<string> url = default, Parameter<bool> spamCheck = default, Parameter<bool> sendRaw = default, string onBehalfOf = null, CancellationToken cancellationToken = default)
		{
			var data = CreateJObject(hostname, url, spamCheck, sendRaw);
			return _client
				.PatchAsync($"{_inboundParseWebhookEndpoint}/settings/{hostname}")
				.OnBehalfOf(onBehalfOf)
				.WithJsonBody(data)
				.WithCancellationToken(cancellationToken)
				.AsSendGridObject<InboundParseWebhookSettings>();
		}

		/// <summary>
		/// Delete the inbound parse webhook settings for a specific hostname.
		/// </summary>
		/// <param name="hostname">The hostname associated with the inbound parse setting that you want to delete.</param>
		/// <param name="onBehalfOf">The user to impersonate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The async task.
		/// </returns>
		public Task DeleteInboundParseWebhookSettingsAsync(string hostname, string onBehalfOf = null, CancellationToken cancellationToken = default)
		{
			return _client
				.DeleteAsync($"{_inboundParseWebhookEndpoint}/settings/{hostname}")
				.OnBehalfOf(onBehalfOf)
				.WithCancellationToken(cancellationToken)
				.AsMessage();
		}

		/// <summary>
		/// Get the signed events public key.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The public key.
		/// </returns>
		public Task<string> GetSignedEventsPublicKeyAsync(CancellationToken cancellationToken = default)
		{
			return _client
				.GetAsync($"{_eventWebhookEndpoint}/settings/signed")
				.WithCancellationToken(cancellationToken)
				.AsSendGridObject<string>("public_key");
		}

		private static JObject CreateJObject(string hostname, Parameter<string> url, Parameter<bool> spamCheck, Parameter<bool> sendRaw)
		{
			var result = new JObject
			{
				{ "hostname", hostname }
			};
			result.AddPropertyIfValue("url", url);
			result.AddPropertyIfValue("spam_check", spamCheck);
			result.AddPropertyIfValue("send_raw", sendRaw);
			return result;
		}
	}
}
