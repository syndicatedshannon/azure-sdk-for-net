﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core.Testing;
using Azure.Identity;
using Azure.Security.KeyVault.Keys.Cryptography;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault.Keys.Samples
{
    /// <summary>
    /// Samples that are used in the associated README.md file.
    /// </summary>
    [LiveOnly]
    public partial class Snippets
    {
#pragma warning disable IDE1006 // Naming Styles
        private KeyClient client;
        private CryptographyClient cryptoClient;
#pragma warning restore IDE1006 // Naming Styles

        [OneTimeSetUp]
        public void CreateClient()
        {
            // Environment variable with the Key Vault endpoint.
            string keyVaultUrl = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL");

            #region Snippet:CreateKeyClient
            // Create a new key client using the default credential from Azure.Identity using environment variables previously set,
            // including AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, and AZURE_TENANT_ID.
            var client = new KeyClient(vaultUri: new Uri(keyVaultUrl), credential: new DefaultAzureCredential());

            // Create a new key using the key client.
            KeyVaultKey key = client.CreateKey("key-name", KeyType.Rsa);

            // Retrieve a key using the key client.
            key = client.GetKey("key-name");
            #endregion

            #region Snippet:CreateCryptographyClient
            // Create a new certificate client using the default credential from Azure.Identity using environment variables previously set,
            // including AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, and AZURE_TENANT_ID.
            var cryptoClient = new CryptographyClient(keyId: key.Id, credential: new DefaultAzureCredential());
            #endregion

            this.client = client;
            this.cryptoClient = cryptoClient;
        }

        [Test]
        public void CreateKey()
        {
            #region Snippet:CreateKey
            // Create a key. Note that you can specify the type of key
            // i.e. Elliptic curve, Hardware Elliptic Curve, RSA
            KeyVaultKey key = client.CreateKey("key-name", KeyType.Rsa);

            Console.WriteLine(key.Name);
            Console.WriteLine(key.KeyType);

            // Create a software RSA key
            var rsaCreateKey = new CreateRsaKeyOptions("rsa-key-name", hardwareProtected: false);
            KeyVaultKey rsaKey = client.CreateRsaKey(rsaCreateKey);

            Console.WriteLine(rsaKey.Name);
            Console.WriteLine(rsaKey.KeyType);

            // Create a hardware Elliptic Curve key
            // Because only premium key vault supports HSM backed keys , please ensure your key vault
            // SKU is premium when you set "hardwareProtected" value to true
            var echsmkey = new CreateEcKeyOptions("ec-key-name", hardwareProtected: true);
            KeyVaultKey ecKey = client.CreateEcKey(echsmkey);

            Console.WriteLine(ecKey.Name);
            Console.WriteLine(ecKey.KeyType);
            #endregion
        }

        [Test]
        public async Task CreateKeyAsync()
        {
            #region Snippet:CreateKeyAsync
            // Create a key of any type
            KeyVaultKey key = await client.CreateKeyAsync("key-name", KeyType.Rsa);

            Console.WriteLine(key.Name);
            Console.WriteLine(key.KeyType);

            // Create a software RSA key
            var rsaCreateKey = new CreateRsaKeyOptions("rsa-key-name", hardwareProtected: false);
            KeyVaultKey rsaKey = await client.CreateRsaKeyAsync(rsaCreateKey);

            Console.WriteLine(rsaKey.Name);
            Console.WriteLine(rsaKey.KeyType);

            // Create a hardware Elliptic Curve key
            // Because only premium key vault supports HSM backed keys , please ensure your key vault
            // SKU is premium when you set "hardwareProtected" value to true
            var echsmkey = new CreateEcKeyOptions("ec-key-name", hardwareProtected: true);
            KeyVaultKey ecKey = await client.CreateEcKeyAsync(echsmkey);

            Console.WriteLine(ecKey.Name);
            Console.WriteLine(ecKey.KeyType);
            #endregion
        }

        [Test]
        public void RetrieveKey()
        {
            // Make sure a key exists.
             client.CreateKey("key-name", KeyType.Rsa);

            #region Snippet:RetrieveKey
            KeyVaultKey key = client.GetKey("key-name");

            Console.WriteLine(key.Name);
            Console.WriteLine(key.KeyType);
            #endregion
        }

        [Test]
        public void UpdateKey()
        {
            #region Snippet:UpdateKey
            KeyVaultKey key = client.CreateKey("key-name", KeyType.Rsa);

            // You can specify additional application-specific metadata in the form of tags.
            key.Properties.Tags["foo"] = "updated tag";

            KeyVaultKey updatedKey = client.UpdateKeyProperties(key.Properties);

            Console.WriteLine(updatedKey.Name);
            Console.WriteLine(updatedKey.Properties.Version);
            Console.WriteLine(updatedKey.Properties.UpdatedOn);
            #endregion
        }

        [Test]
        public void ListKeys()
        {
            #region Snippet:ListKeys
            Pageable<KeyProperties> allKeys = client.GetPropertiesOfKeys();

            foreach (KeyProperties keyProperties in allKeys)
            {
                Console.WriteLine(keyProperties.Name);
            }
            #endregion
        }

        [Test]
        public async Task ListKeysAsync()
        {
            #region Snippet:ListKeysAsync
            AsyncPageable<KeyProperties> allKeys = client.GetPropertiesOfKeysAsync();

            await foreach (KeyProperties keyProperties in allKeys)
            {
                Console.WriteLine(keyProperties.Name);
            }
            #endregion
        }

        [Test]
        public void EncryptDecrypt()
        {
            #region Snippet:EncryptDecrypt
            byte[] plaintext = Encoding.UTF8.GetBytes("A single block of plaintext");

            // encrypt the data using the algorithm RSAOAEP
            EncryptResult encryptResult = cryptoClient.Encrypt(EncryptionAlgorithm.RsaOaep, plaintext);

            // decrypt the encrypted data.
            DecryptResult decryptResult = cryptoClient.Decrypt(EncryptionAlgorithm.RsaOaep, encryptResult.Ciphertext);
            #endregion
        }

        [Test]
        public void NotFound()
        {
            #region Snippet:KeyNotFound
            try
            {
                KeyVaultKey key = client.GetKey("some_key");
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            #endregion
        }

        [Ignore("The key is deleted and purged on tear down of this text fixture.")]
        public void DeleteKey()
        {
            #region Snippet:DeleteKey
            DeleteKeyOperation operation = client.StartDeleteKey("key-name");

            DeletedKey key = operation.Value;
            Console.WriteLine(key.Name);
            Console.WriteLine(key.DeletedOn);
            #endregion
        }

        [OneTimeTearDown]
        public async Task DeleteAndPurgeKey()
        {
            #region Snippet:DeleteAndPurgeKeyAsync
            DeleteKeyOperation operation = client.StartDeleteKey("key-name");

            // You only need to wait for completion if you want to purge or recover the key.
            await operation.WaitForCompletionAsync();

            DeletedKey key = operation.Value;
            client.PurgeDeletedKey(key.Name);
            #endregion

            DeleteKeyOperation rsaKeyOperation =  client.StartDeleteKey("rsa-key-name");
            DeleteKeyOperation ecKeyOperation =  client.StartDeleteKey("ec-key-name");

            try
            {
                // Deleting a key when soft delete is enabled may not happen immediately.
                Task.WaitAll(
                    rsaKeyOperation.WaitForCompletionAsync().AsTask(),
                    ecKeyOperation.WaitForCompletionAsync().AsTask());

                Task.WaitAll(
                    client.PurgeDeletedKeyAsync(rsaKeyOperation.Value.Name),
                    client.PurgeDeletedKeyAsync(ecKeyOperation.Value.Name));
            }
            catch
            {
                // Merely attempt to purge a deleted key since the Key Vault may not have soft delete enabled.
            }
        }

        [Ignore("The key is deleted and purged on tear down of this text fixture.")]
        public void DeleteAndPurge()
        {
            #region Snippet:DeleteAndPurgeKey
            DeleteKeyOperation operation = client.StartDeleteKey("key-name");

            while (!operation.HasCompleted)
            {
                Thread.Sleep(2000);

                operation.UpdateStatus();
            }

            DeletedKey key = operation.Value;
            client.PurgeDeletedKey(key.Name);
            #endregion
        }
    }
}
