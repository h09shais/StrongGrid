using HttpMultipartParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StrongGrid.Models.Webhooks;
using StrongGrid.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StrongGrid
{
	/// <summary>
	/// Allows parsing of information posted from SendGrid.
	/// This parser supports both 'Events' and 'Inbound emails'.
	/// </summary>
	public class WebhookParser
	{
		#region CTOR

#if NETSTANDARD
		static WebhookParser()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
#endif

		#endregion

		#region PUBLIC METHODS

		/// <summary>
		/// Parses the signed events webhook asynchronously.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>An array of <see cref="Event">events</see>.</returns>
		public async Task<Event[]> ParseSignedEventsWebhookAsync(Stream stream, IDictionary<string, string> headers, string publicKey)
		{
			string requestBody;
			using (var streamReader = new StreamReader(stream))
			{
				requestBody = await streamReader.ReadToEndAsync().ConfigureAwait(false);
			}

			var webHookEvents = ParseSignedEventsWebhook(requestBody, headers, publicKey);
			return webHookEvents;
		}

		/// <summary>
		/// Parses the events webhook asynchronously.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>An array of <see cref="Event">events</see>.</returns>
		public async Task<Event[]> ParseEventsWebhookAsync(Stream stream)
		{
			string requestBody;
			using (var streamReader = new StreamReader(stream))
			{
				requestBody = await streamReader.ReadToEndAsync().ConfigureAwait(false);
			}

			var webHookEvents = ParseEventsWebhook(requestBody);
			return webHookEvents;
		}

		/// <summary>
		/// Parses the signed events webhook.
		/// </summary>
		/// <param name="requestBody">The content submitted by SendGrid's WebHook.</param>
		/// <returns>An array of <see cref="Event">events</see>.</returns>
		public Event[] ParseSignedEventsWebhook(string requestBody, IDictionary<string, string> headers, string publicKey)
		{
			if (headers == null) throw new ArgumentNullException(nameof(headers));
			if (string.IsNullOrEmpty(publicKey)) throw new ArgumentNullException(nameof(publicKey));

			headers.TryGetValue("X-Twilio-Email-Event-Webhook-Signature", out string signature);
			headers.TryGetValue("X-Twilio-Email-Event-Webhook-Timestamp", out string timestamp);

			if (string.IsNullOrEmpty(signature)) throw new ArgumentException("The Twilio signature is missing from the request headers");
			if (string.IsNullOrEmpty(timestamp)) throw new ArgumentException("The Twilio timestamp is missing from the request headers");

			// Convert the signature and public key provided by SendGrid into formats usable by the .net crypto classes
			var sig = ConvertECDSASignature.LightweightConvertSignatureFromX9_62ToISO7816_8(256, Convert.FromBase64String(signature));
			var cngBlob = Utils.ConvertSecp256R1PublicKeyToEccPublicBlob(publicKey);

			// Must combine the timestamp and the payload
			var data = Encoding.UTF8.GetBytes(timestamp + requestBody);

			// Verify the signature
			var cngKey = CngKey.Import(cngBlob, CngKeyBlobFormat.EccPublicBlob);
			var eCDsaCng = new ECDsaCng(cngKey);
			var verified = eCDsaCng.VerifyData(data, sig);

			if (!verified)
			{
				throw new SecurityException("Webhook signature validation failed.");
			}

			var webHookEvents = ParseEventsWebhook(requestBody);
			return webHookEvents;
		}

		/// <summary>
		/// Parses the events webhook.
		/// </summary>
		/// <param name="requestBody">The content submitted by SendGrid's WebHook.</param>
		/// <returns>An array of <see cref="Event">events</see>.</returns>
		public Event[] ParseEventsWebhook(string requestBody)
		{
			var webHookEvents = JsonConvert.DeserializeObject<List<Event>>(requestBody, new WebHookEventConverter());
			return webHookEvents.ToArray();
		}

		/// <summary>
		/// Parses the inbound email webhook asynchronously.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>The <see cref="InboundEmail"/>.</returns>
		public async Task<InboundEmail> ParseInboundEmailWebhookAsync(Stream stream)
		{
			// We need to be able to rewind the stream.
			// Therefore, we must make a copy of the stream if it doesn't allow changing the position
			if (!stream.CanSeek)
			{
				using (var ms = Utils.MemoryStreamManager.GetStream())
				{
					await stream.CopyToAsync(ms).ConfigureAwait(false);
					return await ParseInboundEmailWebhookAsync(ms).ConfigureAwait(false);
				}
			}

			// It's important to rewind the stream
			stream.Position = 0;

			// Asynchronously parse the multipart content received from SendGrid
			var parser = await MultipartFormDataParser.ParseAsync(stream, Encoding.UTF8).ConfigureAwait(false);

			// Convert the 'charset' from a string into array of KeyValuePair
			var charsetsAsJObject = JObject.Parse(parser.GetParameterValue("charsets", "{}"));
			var charsets = charsetsAsJObject
				.Properties()
				.Select(prop =>
				{
					var key = prop.Name;
					var value = Encoding.GetEncoding(prop.Value.ToString());
					return new KeyValuePair<string, Encoding>(key, value);
				}).ToArray();

			// Create a dictionary of parsers, one parser for each desired encoding.
			// This is necessary because MultipartFormDataParser can only handle one
			// encoding and SendGrid can use different encodings for parameters such
			// as "from", "to", "text" and "html".
			var encodedParsers = charsets
				.Where(c => c.Value != Encoding.UTF8)
				.Select(c => c.Value)
				.Distinct()
				.Select(async encoding =>
				{
					stream.Position = 0; // It's important to rewind the stream
					return new
					{
						Encoding = encoding,
						Parser = await MultipartFormDataParser.ParseAsync(stream, encoding).ConfigureAwait(false)
					};
				})
				.Select(r => r.Result)
				.Union(new[]
				{
					new { Encoding = Encoding.UTF8, Parser = parser }
				})
				.ToDictionary(ep => ep.Encoding, ep => ep.Parser);

			return ParseInboundEmail(encodedParsers, charsets);
		}

		/// <summary>
		/// Parses the inbound email webhook.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <returns>The <see cref="InboundEmail"/>.</returns>
		public InboundEmail ParseInboundEmailWebhook(Stream stream)
		{
			// We need to be able to rewind the stream.
			// Therefore, we must make a copy of the stream if it doesn't allow changing the position
			if (!stream.CanSeek)
			{
				using (var ms = Utils.MemoryStreamManager.GetStream())
				{
					stream.CopyTo(ms);
					return ParseInboundEmailWebhook(ms);
				}
			}

			// It's important to rewind the stream
			stream.Position = 0;

			// Parse the multipart content received from SendGrid
			var parser = MultipartFormDataParser.Parse(stream, Encoding.UTF8);

			// Convert the 'charset' from a string into array of KeyValuePair
			var charsetsAsJObject = JObject.Parse(parser.GetParameterValue("charsets", "{}"));
			var charsets = charsetsAsJObject
				.Properties()
				.Select(prop =>
				{
					var key = prop.Name;
					var value = Encoding.GetEncoding(prop.Value.ToString());
					return new KeyValuePair<string, Encoding>(key, value);
				}).ToArray();

			// Create a dictionary of parsers, one parser for each desired encoding.
			// This is necessary because MultipartFormDataParser can only handle one
			// encoding and SendGrid can use different encodings for parameters such
			// as "from", "to", "text" and "html".
			var encodedParsers = charsets
				.Where(c => c.Value != Encoding.UTF8)
				.Select(c => c.Value)
				.Distinct()
				.Select(encoding =>
				{
					stream.Position = 0; // It's important to rewind the stream
					return new
					{
						Encoding = encoding,
						Parser = MultipartFormDataParser.Parse(stream, encoding)
					};
				})
				.Union(new[]
				{
					new { Encoding = Encoding.UTF8, Parser = parser }
				})
				.ToDictionary(ep => ep.Encoding, ep => ep.Parser);

			return ParseInboundEmail(encodedParsers, charsets);
		}

		#endregion

		#region PRIVATE METHODS

		private static Encoding GetEncoding(string parameterName, IEnumerable<KeyValuePair<string, Encoding>> charsets)
		{
			var encoding = charsets.Where(c => c.Key == parameterName);
			if (encoding.Any()) return encoding.First().Value;
			else return Encoding.UTF8;
		}

		private static MultipartFormDataParser GetEncodedParser(string parameterName, IEnumerable<KeyValuePair<string, Encoding>> charsets, IDictionary<Encoding, MultipartFormDataParser> encodedParsers)
		{
			var encoding = GetEncoding(parameterName, charsets);
			var parser = encodedParsers[encoding];
			return parser;
		}

		private static string GetEncodedValue(string parameterName, IEnumerable<KeyValuePair<string, Encoding>> charsets, IDictionary<Encoding, MultipartFormDataParser> encodedParsers, string defaultValue = null)
		{
			var parser = GetEncodedParser(parameterName, charsets, encodedParsers);
			var value = parser.GetParameterValue(parameterName, defaultValue);
			return value;
		}

		private static InboundEmail ParseInboundEmail(IDictionary<Encoding, MultipartFormDataParser> encodedParsers, KeyValuePair<string, Encoding>[] charsets)
		{
			// Get the default UTF8 parser
			var parser = encodedParsers.Single(p => p.Key == Encoding.UTF8).Value;

			// Convert the 'headers' from a string into array of KeyValuePair
			var headers = parser
					.GetParameterValue("headers", string.Empty)
					.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
					.Select(header =>
					{
						var splitHeader = header.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
						var key = splitHeader[0];
						var value = splitHeader.Length >= 2 ? splitHeader[1] : null;
						return new KeyValuePair<string, string>(key, value);
					}).ToArray();

			// Raw email
			var rawEmail = parser.GetParameterValue("email", string.Empty);

			// Combine the 'attachment-info' and Files into an array of Attachments
			var attachmentInfoAsJObject = JObject.Parse(parser.GetParameterValue("attachment-info", "{}"));
			var attachments = attachmentInfoAsJObject
				.Properties()
				.Select(prop =>
				{
					var attachment = prop.Value.ToObject<InboundEmailAttachment>();
					attachment.Id = prop.Name;

					var file = parser.Files.FirstOrDefault(f => f.Name == prop.Name);
					if (file != null)
					{
						attachment.Data = file.Data;
						if (string.IsNullOrEmpty(attachment.ContentType)) attachment.ContentType = file.ContentType;
						if (string.IsNullOrEmpty(attachment.FileName)) attachment.FileName = file.FileName;
					}

					return attachment;
				}).ToArray();

			// Convert the 'envelope' from a JSON string into a strongly typed object
			var envelope = JsonConvert.DeserializeObject<InboundEmailEnvelope>(parser.GetParameterValue("envelope", "{}"));

			// Convert the 'from' from a string into an email address
			var rawFrom = GetEncodedValue("from", charsets, encodedParsers, string.Empty);
			var from = MailAddressParser.ParseEmailAddress(rawFrom);

			// Convert the 'to' from a string into an array of email addresses
			var rawTo = GetEncodedValue("to", charsets, encodedParsers, string.Empty);
			var to = MailAddressParser.ParseEmailAddresses(rawTo);

			// Convert the 'cc' from a string into an array of email addresses
			var rawCc = GetEncodedValue("cc", charsets, encodedParsers, string.Empty);
			var cc = MailAddressParser.ParseEmailAddresses(rawCc);

			// Arrange the InboundEmail
			var inboundEmail = new InboundEmail
			{
				Attachments = attachments,
				Charsets = charsets,
				Dkim = GetEncodedValue("dkim", charsets, encodedParsers, null),
				Envelope = envelope,
				From = from,
				Headers = headers,
				Html = GetEncodedValue("html", charsets, encodedParsers, null),
				SenderIp = GetEncodedValue("sender_ip", charsets, encodedParsers, null),
				SpamReport = GetEncodedValue("spam_report", charsets, encodedParsers, null),
				SpamScore = GetEncodedValue("spam_score", charsets, encodedParsers, null),
				Spf = GetEncodedValue("SPF", charsets, encodedParsers, null),
				Subject = GetEncodedValue("subject", charsets, encodedParsers, null),
				Text = GetEncodedValue("text", charsets, encodedParsers, null),
				To = to,
				Cc = cc,
				RawEmail = rawEmail
			};

			return inboundEmail;
		}

		#endregion
	}
}
