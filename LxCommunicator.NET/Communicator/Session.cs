﻿using Newtonsoft.Json;
using Org.BouncyCastle.OpenSsl;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Loxone.Communicator {
	public class Session {
		/// <summary>
		/// Creates a new instance of the session object. <see cref="Session"/> is used to store information about the current connection to the miniserver.
		/// </summary>
		/// <param name="client">The client that should be used for communicating with the miniserver</param>
		/// <param name="tokenPermission">The permissions the user should have</param>
		/// <param name="deviceUuid">The uuid of the device</param>
		/// <param name="deviceInfo">Some info of the device</param>
		public Session(WebserviceClient client, int tokenPermission, string deviceUuid, string deviceInfo) {
			Client = client;
			TokenPermission = tokenPermission;
			DeviceUuid = deviceUuid;
			DeviceInfo = deviceInfo;
			GenerateAesKeyIv();
			GetRandomSalt(4);
		}

		public Session(WebserviceClient client, ConnectionSessionConfiguration connectionSessionConfiguration) {
			if (connectionSessionConfiguration is null) {
				throw new ArgumentNullException(nameof(connectionSessionConfiguration));
			}

			Client = client;
			ConnectionSessionConfiguration = connectionSessionConfiguration;
			TokenPermission = ConnectionSessionConfiguration.TokenPermission;
			DeviceUuid = ConnectionSessionConfiguration.DeviceUuid;
			DeviceInfo = ConnectionSessionConfiguration.DeviceInfo;
			GenerateAesKeyIv();
			GetRandomSalt(4);
		}

		/// <summary>
		/// The public key of the server
		/// </summary>
		public string PublicKey { get; private set; }

		/// <summary>
		/// The client used for communicating with the miniserver
		/// </summary>
		public WebserviceClient Client { get; set; }

		public ConnectionSessionConfiguration ConnectionSessionConfiguration { get; }

		/// <summary>
		/// The permission the current user has / should have
		/// </summary>
		public int TokenPermission { get; private set; }

		/// <summary>
		/// The uuid of the current device
		/// </summary>
		public string DeviceUuid { get; private set; }

		/// <summary>
		/// The info of the current device
		/// </summary>
		public string DeviceInfo { get; private set; }

		/// <summary>
		/// The current AES key
		/// </summary>
		public string AesKey { get; private set; }

		/// <summary>
		/// The current AES iv
		/// </summary>
		public string AesIv { get; private set; }

		/// <summary>
		/// The current random salt
		/// </summary>
		public string Salt { get; private set; }

		/// <summary>
		/// The session key of the current session
		/// </summary>
		private string SessionKey { get; set; }

		/// <summary>
		/// Get a userKey object from the Miniserver
		/// </summary>
		/// <param name="username">The username of the user trying to authenticate</param>
		/// <returns>A userKey object containing the key, the salt and the hashAlgorythm</returns>
		public async Task<UserKey> GetUserKey(string username) {
			var request = WebserviceRequest<UserKey>.Create(
				WebserviceRequestConfig.Auth(),
				nameof(GetUserKey),
				$"jdev/sys/getkey2/{username}"
			);

			var response = await Client.SendWebserviceAndWait(request);

			return response.Value;
		}

		/// <summary>
		/// Gets the public key from the miniserver
		/// </summary>
		/// <returns>A string containing the public key</returns>
		public async Task<string> GetMiniserverPublicKey() {
			if (PublicKey == null) {
				var request = WebserviceRequest<string>.Create(
					WebserviceRequestConfig.NoAuth(),
					nameof(GetMiniserverPublicKey),
					$"jdev/sys/getPublicKey"
				);

				var response = await Client.SendWebserviceAndWait(request);

				string publicKey = response.Value.Replace("-----BEGIN CERTIFICATE-----", "-----BEGIN PUBLIC KEY-----\n");
				publicKey = publicKey.Replace("-----END CERTIFICATE-----", "\n-----END PUBLIC KEY-----");

				PublicKey = PemToXml(publicKey);
			}

			return PublicKey;
		}

		/// <summary>
		/// Transforms the aesKey, the aesIv and the public key to the sessionKey
		/// </summary>
		/// <returns>A string containing the sessionKey</returns>
		public async Task<string> GetSessionKey() {
			if (SessionKey == null) {
				SessionKey = await Cryptography.EncryptRSA($"{AesKey}:{AesIv}", this);
			}

			return SessionKey;
		}

		/// <summary>
		/// Generates a random key and iv for aes encryption
		/// </summary>
		private void GenerateAesKeyIv() {
			Aes aes = Aes.Create();
			aes.Mode = CipherMode.CBC;
			aes.KeySize = 256;
			aes.GenerateIV();
			aes.GenerateKey();

			AesKey = BitConverter.ToString(aes.Key).Replace("-", "");
			AesIv = BitConverter.ToString(aes.IV).Replace("-", "");
		}

		/// <summary>
		/// Generates a random salt
		/// </summary>
		/// <param name="digits">The number of digits the salt should be long</param>
		private void GetRandomSalt(int digits) {
			Random random = new Random();
			Salt = string.Concat(Enumerable.Range(0, digits).Select(x => random.Next(16).ToString("X")));
		}

		/// <summary>
		/// Converts a string in PEM format to XML format
		/// </summary>
		/// <param name="pem">a string in PEM format</param>
		/// <returns>a string in XML format</returns>
		private string PemToXml(string pem) {
			RSA key = RSA.Create();
			key.ImportFromPem(pem.ToCharArray());
			return key.ToXmlString(false);
			//key.ImportSubjectPublicKeyInfo()

			//return GetXmlRsaKey(pem, obj => {
			//	var publicKey = (RsaKeyParameters)obj;
			//	
			//	return DotNetUtilities.ToRSA(
			//		publicKey
			//		,
			//		new CspParameters {
			//			Flags = CspProviderFlags.UseMachineKeyStore
			//		}
			//		);
			//}, rsa => rsa.ToXmlString(false));
		}

		/// <summary>
		/// Converts a string in PEM format to XML format
		/// </summary>
		/// <param name="pem">a string in PEM format</param>
		/// <param name="getRsa"></param>
		/// <param name="getKey"></param>
		/// <returns>a string in XML format</returns>
		private string GetXmlRsaKey(string pem, Func<object, RSA> getRsa, Func<RSA, string> getKey) {
			using (var ms = new MemoryStream())
			using (var sw = new StreamWriter(ms))
			using (var sr = new StreamReader(ms)) {
				sw.Write(pem);
				sw.Flush();
				ms.Position = 0;
				var pr = new PemReader(sr);
				object keyPair = pr.ReadObject();
				using (RSA rsa = getRsa(keyPair)) {
					var xml = getKey(rsa);
					return xml;
				}
			}
		}
	}

	/// <summary>
	/// Object, where information about the current user is stored
	/// </summary>
	public class UserKey {
		[JsonProperty("key")]
		public string Key { get; private set; }

		[JsonProperty("salt")]
		public string Salt { get; private set; }

		[JsonProperty("hashAlg")]
		public string Algorithm { get; private set; }

		public HashAlgorithm GetHashAlgorithm() {
			if (!string.IsNullOrEmpty(Algorithm) && Algorithm == "SHA256") {
				return new SHA256CryptoServiceProvider();
			}
			else {
				return new SHA1CryptoServiceProvider();
			}
		}

		public HMAC GetHMAC() {
			if (!string.IsNullOrEmpty(Algorithm) && Algorithm == "SHA256") {
				return new HMACSHA256();
			}
			else {
				return new HMACSHA1();
			}
		}
	}
}