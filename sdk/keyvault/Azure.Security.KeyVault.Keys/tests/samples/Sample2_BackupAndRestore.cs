﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core.Testing;
using Azure.Identity;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;

namespace Azure.Security.KeyVault.Keys.Samples
{
    /// <summary>
    /// This sample demonstrates how to back up and restore a Key from Azure Key Vault using synchronous methods of <see cref="KeyClient">.
    /// </summary>
    [LiveOnly]
    public partial class BackupAndRestore
    {
        [Test]
        [Ignore("https://github.com/Azure/azure-sdk-for-net/issues/6514")]
        public void BackupAndRestoreSync()
        {
            // Environment variable with the Key Vault endpoint.
            string keyVaultUrl = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL");
            BackupAndRestoreSync(keyVaultUrl);
        }

        private void BackupAndRestoreSync(string keyVaultUrl)
        {
            #region Snippet:KeysSample2KeyClient
            var client = new KeyClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
            #endregion

            #region Snippet:KeysSample2CreateKey
            string rsaKeyName = $"CloudRsaKey-{Guid.NewGuid()}";
            var rsaKey = new CreateRsaKeyOptions(rsaKeyName, hardwareProtected: false)
            {
                KeySize = 2048,
                ExpiresOn = DateTimeOffset.Now.AddYears(1)
            };

            KeyVaultKey storedKey = client.CreateRsaKey(rsaKey);
            #endregion

            #region Snippet:KeysSample2BackupKey
            byte[] backupKey = client.BackupKey(rsaKeyName);
            #endregion

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Write(backupKey, 0, backupKey.Length);

                // The storage account key is no longer in use, so you delete it.
                DeleteKeyOperation operation = client.StartDeleteKey(rsaKeyName);

                // To ensure the key is deleted on server before we try to purge it.
                while (!operation.HasCompleted)
                {
                    Thread.Sleep(2000);

                    operation.UpdateStatus();
                }

                // If the keyvault is soft-delete enabled, then for permanent deletion, deleted key needs to be purged.
                client.PurgeDeletedKey(rsaKeyName);

                #region Snippet:KeysSample2RestoreKey
                KeyVaultKey restoredKey = client.RestoreKeyBackup(memoryStream.ToArray());
                #endregion

                AssertKeysEqual(storedKey.Properties, restoredKey.Properties);
            }
        }

        private void AssertKeysEqual(KeyProperties exp, KeyProperties act)
        {
            Assert.AreEqual(exp.Name, act.Name);
            Assert.AreEqual(exp.Version, act.Version);
            Assert.AreEqual(exp.Managed, act.Managed);
            Assert.AreEqual(exp.RecoveryLevel, act.RecoveryLevel);
            Assert.AreEqual(exp.ExpiresOn, act.ExpiresOn);
            Assert.AreEqual(exp.NotBefore, act.NotBefore);
        }
    }
}
