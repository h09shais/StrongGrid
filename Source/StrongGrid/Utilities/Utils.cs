using Microsoft.IO;
using System;
using System.Linq;

namespace StrongGrid.Utilities
{
	/// <summary>
	/// Utils.
	/// </summary>
	internal static class Utils
	{
		private static readonly byte[] Secp256R1Prefix = Convert.FromBase64String("MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE");
		private static readonly byte[] CngBlobPrefix = { 0x45, 0x43, 0x53, 0x31, 0x20, 0, 0, 0 };

		public static DateTime Epoch { get; } = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static RecyclableMemoryStreamManager MemoryStreamManager { get; } = new RecyclableMemoryStreamManager();

		/// <summary>
		/// Converts a base64 encoded secp256r1/NIST P-256 public key 
		/// </summary>
		/// <param name="base64EncodedPublicKey">The base64 encoded public key.</param>
		/// <returns></returns>
		/// <remarks>
		/// From https://stackoverflow.com/questions/44502331/c-sharp-get-cngkey-object-from-public-key-in-text-file/44527439#44527439 .
		/// </remarks>
		public static byte[] ConvertSecp256R1PublicKeyToEccPublicBlob(string base64EncodedPublicKey)
		{
			var subjectPublicKeyInfo = Convert.FromBase64String(base64EncodedPublicKey);

			if (subjectPublicKeyInfo.Length != 91)
				throw new InvalidOperationException();

			var prefix = Secp256R1Prefix;

			if (!subjectPublicKeyInfo.Take(prefix.Length).SequenceEqual(prefix))
				throw new InvalidOperationException();

			var cngBlob = new byte[CngBlobPrefix.Length + 64];
			Buffer.BlockCopy(CngBlobPrefix, 0, cngBlob, 0, CngBlobPrefix.Length);

			Buffer.BlockCopy(
				subjectPublicKeyInfo,
				Secp256R1Prefix.Length,
				cngBlob,
				CngBlobPrefix.Length,
				64);

			return cngBlob;
		}
	}
}
