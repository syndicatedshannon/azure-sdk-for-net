# Creating, getting, updating, and deleting keys

This sample demonstrates how to create, get, update, and delete a key in Azure Key Vault.
To get started, you'll need a URI to an Azure Key Vault. See the [README](../README.md) for links and instructions.

## Creating a KeyClient

To create a new `KeyClient` to create, get, update, or delete keys, you need the endpoint to a Key Vault and credentials.
You can use the [DefaultAzureCredential][DefaultAzureCredential] to try a number of common authentication methods optimized for both running as a service and development.

In the sample below, you can set `keyVaultUrl` based on an environment variable, configuration setting, or any way that works for your application.

```C# Snippet:KeysSample1KeyClient
var client = new KeyClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
```

## Creating a key

Let's create an RSA key valid for 1 year.
If the key already exists in the Key Vault, then a new version of the key is created.

```C# Snippet:KeysSample1CreateKey
string rsaKeyName = $"CloudRsaKey-{Guid.NewGuid()}";
var rsaKey = new CreateRsaKeyOptions(rsaKeyName, hardwareProtected: false)
{
    KeySize = 2048,
    ExpiresOn = DateTimeOffset.Now.AddYears(1)
};

client.CreateRsaKey(rsaKey);
```

## Getting a key

Let's get the cloud RSA key from the Key Vault.

```C# Snippet:KeysSample1GetKey
KeyVaultKey cloudRsaKey = client.GetKey(rsaKeyName);
Debug.WriteLine($"Key is returned with name {cloudRsaKey.Name} and type {cloudRsaKey.KeyType}");
```

## Updating key properties

After one year, the cloud RSA key is still required, so we need to update the expiry time of the key.
The update method can be used to update the expiry attribute of the key.

```C# Snippet:KeysSample1UpdateKeyProperties
cloudRsaKey.Properties.ExpiresOn.Value.AddYears(1);
KeyVaultKey updatedKey = client.UpdateKeyProperties(cloudRsaKey.Properties, cloudRsaKey.KeyOperations);
Debug.WriteLine($"Key's updated expiry time is {updatedKey.Properties.ExpiresOn}");
```

## Updating a key size

We need the cloud RSA key with bigger key size, so you want to update the key in Key Vault to ensure it has the required size.
Calling `CreateRsaKey` on an existing key creates a new version of the key in the Key Vault with the new specified size.

```C# Snippet:KeysSample1UpdateKey
var newRsaKey = new CreateRsaKeyOptions(rsaKeyName, hardwareProtected: false)
{
    KeySize = 4096,
    ExpiresOn = DateTimeOffset.Now.AddYears(1)
};

client.CreateRsaKey(newRsaKey);
```

## Deleting a key

The cloud RSA key is no longer needed, so we need to delete it from the Key Vault.

```C# Snippet:KeysSample1DeleteKey
DeleteKeyOperation operation = client.StartDeleteKey(rsaKeyName);
```

## Purging a deleted key

If the Key Vault is soft delete-enabled and you want to permanently delete the key before its `ScheduledPurgeDate`,
the deleted key needs to be purged. Before it can be purged, you need to wait until the key is fully deleted.

```C# Snippet:KeysSample1PurgeKey
while (!operation.HasCompleted)
{
    Thread.Sleep(2000);

    operation.UpdateStatus();
}

client.PurgeDeletedKey(rsaKeyName);
```

## Purging a deleted key asynchronously

When writing asynchronous code, you can instead await `WaitForCompletionAsync` to wait indefinitely.
You can optionally pass in a `CancellationToken` to cancel waiting after a certain period or time or any other trigger you require.

```C# Snippet:KeysSample1PurgeKeyAsync
await operation.WaitForCompletionAsync();

await client.PurgeDeletedKeyAsync(rsaKeyName);
```

## Source

* [Synchronous Sample1_HelloWorld.cs](../tests/samples/Sample1_HelloWorld.cs)
* [Asynchronous Sample1_HelloWorldAsync.cs](../tests/samples/Sample1_HelloWorldAsync.cs)

[DefaultAzureCredential]: ../../../identity/Azure.Identity/README.md
