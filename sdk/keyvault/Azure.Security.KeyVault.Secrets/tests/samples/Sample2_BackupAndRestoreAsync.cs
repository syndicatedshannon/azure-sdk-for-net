﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core.Testing;
using Azure.Identity;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Azure.Security.KeyVault.Secrets.Samples
{
    /// <summary>
    /// This sample demonstrates how to back up and restore a secret from Azure Key Vault using asynchronous methods of <see cref="SecretClient"/>.
    /// </summary>
    [LiveOnly]
    public partial class BackupAndRestore
    {
        [Test]
        [Ignore("https://github.com/Azure/azure-sdk-for-net/issues/6514")]
        public async Task BackupAndRestoreAsync()
        {
            // Environment variable with the Key Vault endpoint.
            string keyVaultUrl = Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URL");
            await BackupAndRestoreAsync(keyVaultUrl);
        }

        private async Task BackupAndRestoreAsync(string keyVaultUrl)
        {

            var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

            string secretName = $"StorageAccountPassword{Guid.NewGuid()}";

            var secret = new KeyVaultSecret(secretName, "f4G34fMh8v");
            secret.Properties.ExpiresOn = DateTimeOffset.Now.AddYears(1);

            KeyVaultSecret storedSecret = await client.SetSecretAsync(secret);

            string backupPath = Path.GetTempFileName();

            using (FileStream sourceStream = File.Open(backupPath, FileMode.OpenOrCreate))
            {
                byte[] byteSecret = await client.BackupSecretAsync(secretName);
                sourceStream.Seek(0, SeekOrigin.End);
                await sourceStream.WriteAsync(byteSecret, 0, byteSecret.Length);
            }

            // The storage account secret is no longer in use so you delete it.
            DeleteSecretOperation operation = await client.StartDeleteSecretAsync(secretName);

            // Before it can be purged, you need to wait until the secret is fully deleted.
            await operation.WaitForCompletionAsync();

            // If the Key Vault is soft delete-enabled and you want to permanently delete the secret before its `ScheduledPurgeDate`,
            // the deleted secret needs to be purged.
            await client.PurgeDeletedSecretAsync(secretName);

            SecretProperties restoreSecret = null;
            using (FileStream sourceStream = File.Open(backupPath, FileMode.Open))
            {
                byte[] result = new byte[sourceStream.Length];
                await sourceStream.ReadAsync(result, 0, (int)sourceStream.Length);
                restoreSecret = await client.RestoreSecretBackupAsync(result);
            }

            AssertSecretsEqual(storedSecret.Properties, restoreSecret);
        }
    }
}
