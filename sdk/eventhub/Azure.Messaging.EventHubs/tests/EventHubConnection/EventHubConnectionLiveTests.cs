﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Messaging.EventHubs.Authorization;
using Azure.Messaging.EventHubs.Core;
using Azure.Messaging.EventHubs.Errors;
using Azure.Messaging.EventHubs.Metadata;
using Azure.Messaging.EventHubs.Tests.Infrastructure;
using NUnit.Framework;

namespace Azure.Messaging.EventHubs.Tests
{
    /// <summary>
    ///   The suite of live tests for the <see cref="EventHubConnection" />
    ///   class.
    /// </summary>
    ///
    /// <remarks>
    ///   These tests have a dependency on live Azure services and may
    ///   incur costs for the associated Azure subscription.
    /// </remarks>
    ///
    [TestFixture]
    [Category(TestCategory.Live)]
    [Category(TestCategory.DisallowVisualStudioLiveUnitTesting)]
    public class EventHubConnectionLiveTests
    {
        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        public async Task ConnectionCanConnectToEventHubsUsingFullConnectionString()
        {
            await using (EventHubScope scope = await EventHubScope.CreateAsync(1))
            {
                var connectionString = TestEnvironment.BuildConnectionStringForEventHub(scope.EventHubName);

                await using (var connection = new TestConnectionWithTransport(connectionString))
                {
                    Assert.That(() => connection.GetPropertiesAsync(), Throws.Nothing);
                }
            }
        }

        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        public async Task ConnectionCanConnectToEventHubsUsingConnectionStringAndEventHub()
        {
            await using (EventHubScope scope = await EventHubScope.CreateAsync(1))
            {
                var connectionString = TestEnvironment.EventHubsConnectionString;

                await using (var connection = new TestConnectionWithTransport(connectionString, scope.EventHubName))
                {
                    Assert.That(() => connection.GetPropertiesAsync(), Throws.Nothing);
                }
            }
        }

        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        public async Task ConnectionCanConnectToEventHubsUsingSharedKeyCredential()
        {
            await using (EventHubScope scope = await EventHubScope.CreateAsync(1))
            {
                var connectionString = TestEnvironment.BuildConnectionStringForEventHub(scope.EventHubName);
                var properties = ConnectionStringParser.Parse(connectionString);
                var credential = new EventHubSharedKeyCredential(properties.SharedAccessKeyName, properties.SharedAccessKey);

                await using (var connection = new TestConnectionWithTransport(properties.Endpoint.Host, scope.EventHubName, credential))
                {
                    Assert.That(() => connection.GetPropertiesAsync(), Throws.Nothing);
                }
            }
        }

        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        public async Task ConnectionCanConnectToEventHubsUsingArguments()
        {
            await using (EventHubScope scope = await EventHubScope.CreateAsync(1))
            {
                var options = new EventHubConnectionOptions();
                var connectionString = TestEnvironment.BuildConnectionStringForEventHub(scope.EventHubName);
                var connectionProperties = ConnectionStringParser.Parse(connectionString);

                var credential = new SharedAccessSignatureCredential
                (
                    new SharedAccessSignature
                    (
                        $"{ options.TransportType.GetUriScheme() }://{ connectionProperties.Endpoint.Host }/{ connectionProperties.EventHubName }".ToLowerInvariant(),
                        connectionProperties.SharedAccessKeyName,
                        connectionProperties.SharedAccessKey,
                        TimeSpan.FromHours(4)
                    )
                );

                await using (var connection = new TestConnectionWithTransport(connectionProperties.Endpoint.Host, connectionProperties.EventHubName, credential))
                {
                    Assert.That(() => connection.GetPropertiesAsync(), Throws.Nothing);
                }
            }
        }

        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        [TestCase(TransportType.AmqpTcp)]
        [TestCase(TransportType.AmqpWebSockets)]
        public async Task ConnectionTransportCanRetrieveProperties(TransportType transportType)
        {
            var partitionCount = 4;

            await using (EventHubScope scope = await EventHubScope.CreateAsync(partitionCount))
            {
                var connectionString = TestEnvironment.EventHubsConnectionString;

                await using (var connection = new TestConnectionWithTransport(connectionString, scope.EventHubName, new EventHubConnectionOptions { TransportType = transportType }))
                {
                    EventHubProperties properties = await connection.GetPropertiesAsync();

                    Assert.That(properties, Is.Not.Null, "A set of properties should have been returned.");
                    Assert.That(properties.Name, Is.EqualTo(scope.EventHubName), "The property Event Hub name should match the scope.");
                    Assert.That(properties.PartitionIds.Length, Is.EqualTo(partitionCount), "The properties should have the requested number of partitions.");
                    Assert.That(properties.CreatedAt, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(60)), "The Event Hub should have been created just about now.");
                }
            }
        }

        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        [TestCase(TransportType.AmqpTcp)]
        [TestCase(TransportType.AmqpWebSockets)]
        public async Task ConnectionTransportCanRetrievePartitionProperties(TransportType transportType)
        {
            var partitionCount = 4;

            await using (EventHubScope scope = await EventHubScope.CreateAsync(partitionCount))
            {
                var options = new EventHubConnectionOptions();
                var connectionString = TestEnvironment.BuildConnectionStringForEventHub(scope.EventHubName);
                var connectionProperties = ConnectionStringParser.Parse(connectionString);

                var credential = new SharedAccessSignatureCredential
                (
                    new SharedAccessSignature
                    (
                        $"{ options.TransportType.GetUriScheme() }://{ connectionProperties.Endpoint.Host }/{ connectionProperties.EventHubName }".ToLowerInvariant(),
                        connectionProperties.SharedAccessKeyName,
                        connectionProperties.SharedAccessKey,
                        TimeSpan.FromHours(4)
                    )
                );

                await using (var connection = new TestConnectionWithTransport(connectionProperties.Endpoint.Host, connectionProperties.EventHubName, credential, new EventHubConnectionOptions { TransportType = transportType }))
                {
                    var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var properties = await connection.GetPropertiesAsync();
                    var partition = properties.PartitionIds.First();
                    var partitionProperties = await connection.GetPartitionPropertiesAsync(partition, cancellation.Token);

                    Assert.That(partitionProperties, Is.Not.Null, "A set of partition properties should have been returned.");
                    Assert.That(partitionProperties.Id, Is.EqualTo(partition), "The partition identifier should match.");
                    Assert.That(partitionProperties.EventHubName, Is.EqualTo(connectionProperties.EventHubName).Using((IEqualityComparer<string>)StringComparer.InvariantCultureIgnoreCase), "The Event Hub path should match.");
                    Assert.That(partitionProperties.BeginningSequenceNumber, Is.Not.EqualTo(default(long)), "The beginning sequence number should have been populated.");
                    Assert.That(partitionProperties.LastEnqueuedSequenceNumber, Is.Not.EqualTo(default(long)), "The last sequence number should have been populated.");
                    Assert.That(partitionProperties.LastEnqueuedOffset, Is.Not.EqualTo(default(long)), "The last offset should have been populated.");
                }
            }
        }

        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        public async Task ConnectionTransportPartitionIdsMatchPartitionProperties()
        {
            await using (EventHubScope scope = await EventHubScope.CreateAsync(4))
            {
                var connectionString = TestEnvironment.BuildConnectionStringForEventHub(scope.EventHubName);

                await using (var connection = new TestConnectionWithTransport(connectionString))
                {
                    EventHubProperties properties = await connection.GetPropertiesAsync();
                    var partitions = await connection.GetPartitionIdsAsync();

                    Assert.That(properties, Is.Not.Null, "A set of properties should have been returned.");
                    Assert.That(properties.PartitionIds, Is.Not.Null, "A set of partition identifiers for the properties should have been returned.");
                    Assert.That(partitions, Is.Not.Null, "A set of partition identifiers should have been returned.");
                    Assert.That(partitions, Is.EquivalentTo(properties.PartitionIds), "The partition identifiers returned directly should match those returned with properties.");
                }
            }
        }

        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ConnectionTransportCannotRetrieveMetadataWhenClosed(bool sync)
        {
            await using (EventHubScope scope = await EventHubScope.CreateAsync(1))
            {
                var connectionString = TestEnvironment.BuildConnectionStringForEventHub(scope.EventHubName);

                await using (var connection = new TestConnectionWithTransport(connectionString))
                {
                    var partition = (await connection.GetPartitionIdsAsync()).First();

                    Assert.That(async () => await connection.GetPropertiesAsync(), Throws.Nothing);
                    Assert.That(async () => await connection.GetPartitionPropertiesAsync(partition), Throws.Nothing);

                    if (sync)
                    {
                        connection.Close();
                    }
                    else
                    {
                        await connection.CloseAsync();
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5));

                    Assert.That(async () => await connection.GetPartitionIdsAsync(), Throws.TypeOf<EventHubsClientClosedException>());
                    Assert.That(async () => await connection.GetPropertiesAsync(), Throws.TypeOf<EventHubsClientClosedException>());
                    Assert.That(async () => await connection.GetPartitionPropertiesAsync(partition), Throws.TypeOf<EventHubsClientClosedException>());
                }
            }
        }

        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        [TestCase("XYZ")]
        [TestCase("-1")]
        [TestCase("1000")]
        [TestCase("-")]
        public async Task ConnectionTransportCannotRetrievePartitionPropertiesWhenPartitionIdIsInvalid(string invalidPartition)
        {
            await using (EventHubScope scope = await EventHubScope.CreateAsync(1))
            {
                var connectionString = TestEnvironment.BuildConnectionStringForEventHub(scope.EventHubName);

                await using (var connection = new TestConnectionWithTransport(connectionString))
                {
                    Assert.That(async () => await connection.GetPartitionPropertiesAsync(invalidPartition), Throws.TypeOf<ArgumentOutOfRangeException>());
                }
            }
        }

        /// <summary>
        ///   Verifies that the <see cref="EventHubConnection" /> is able to
        ///   connect to the Event Hubs service.
        /// </summary>
        ///
        [Test]
        public async Task ConnectionTransportCannotRetrieveMetadataWhenProxyIsInvalid()
        {
            await using (EventHubScope scope = await EventHubScope.CreateAsync(1))
            {
                var connectionString = TestEnvironment.BuildConnectionStringForEventHub(scope.EventHubName);
                var retryOptions = new RetryOptions { TryTimeout = TimeSpan.FromMinutes(2) };

                var clientOptions = new EventHubConnectionOptions
                {
                    Proxy = new WebProxy("http://1.2.3.4:9999"),
                    TransportType = TransportType.AmqpWebSockets
                };

                await using (var connection = new TestConnectionWithTransport(connectionString))
                await using (var invalidProxyConnection = new TestConnectionWithTransport(connectionString, clientOptions))
                {
                    connection.RetryPolicy = new BasicRetryPolicy(retryOptions);
                    invalidProxyConnection.RetryPolicy = new BasicRetryPolicy(retryOptions);

                    var partition = (await connection.GetPartitionIdsAsync()).First();

                    Assert.That(async () => await invalidProxyConnection.GetPartitionIdsAsync(), Throws.InstanceOf<WebSocketException>().Or.InstanceOf<TimeoutException>());
                    Assert.That(async () => await invalidProxyConnection.GetPropertiesAsync(), Throws.InstanceOf<WebSocketException>().Or.InstanceOf<TimeoutException>());
                    Assert.That(async () => await invalidProxyConnection.GetPartitionPropertiesAsync(partition), Throws.InstanceOf<WebSocketException>().Or.InstanceOf<TimeoutException>());
                }
            }
        }

        /// <summary>
        ///   Provides a local implementation of the <see cref="EventHubsConnection" /> with an exposed
        ///   transport client for testing purposes.
        /// </summary>
        ///
        private class TestConnectionWithTransport : EventHubConnection
        {
            public EventHubsRetryPolicy RetryPolicy { get; set; } = new BasicRetryPolicy(new RetryOptions());

            public TestConnectionWithTransport(string connectionString,
                                               EventHubConnectionOptions connectionOptions = default) : base(connectionString, connectionOptions)
            {
            }

            public TestConnectionWithTransport(string connectionString,
                                               string eventHubName,
                                               EventHubConnectionOptions connectionOptions = default) : base(connectionString, eventHubName, connectionOptions)
            {
            }

            public TestConnectionWithTransport(string fullyQualifiedNamespace,
                                               string eventHubName,
                                               TokenCredential credential,
                                               EventHubConnectionOptions connectionOptions = default) : base(fullyQualifiedNamespace, eventHubName, credential, connectionOptions)
            {
            }

            public Task<EventHubProperties> GetPropertiesAsync(CancellationToken cancellationToken = default) => base.GetPropertiesAsync(RetryPolicy, cancellationToken);
            public Task<string[]> GetPartitionIdsAsync(CancellationToken cancellationToken = default) => base.GetPartitionIdsAsync(RetryPolicy, cancellationToken);
            public Task<PartitionProperties> GetPartitionPropertiesAsync(string partitionId, CancellationToken cancellationToken = default) => base.GetPartitionPropertiesAsync(partitionId, RetryPolicy, cancellationToken);
        }
    }
}
