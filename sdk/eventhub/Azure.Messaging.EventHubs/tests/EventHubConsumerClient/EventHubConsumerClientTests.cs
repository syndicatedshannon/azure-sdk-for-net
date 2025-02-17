﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Messaging.EventHubs.Authorization;
using Azure.Messaging.EventHubs.Core;
using Azure.Messaging.EventHubs.Errors;
using Azure.Messaging.EventHubs.Metadata;
using Moq;
using NUnit.Framework;

namespace Azure.Messaging.EventHubs.Tests
{
    /// <summary>
    ///   The suite of tests for the <see cref="EventHubConsumerClient" />
    ///   class.
    /// </summary>
    ///
    [TestFixture]
    public class EventHubConsumerClientTests
    {
        /// <summary>
        ///   Provides the test cases for non-fatal exceptions that are not retriable
        ///   when encountered in a subscription.
        /// </summary>
        ///
        public static IEnumerable<object[]> NonFatalNotRetriableExceptionTestCases()
        {
            yield return new object[] { new ConsumerDisconnectedException("Test", "Test") };
            yield return new object[] { new EventHubsResourceNotFoundException("Test", "Test") };
            yield return new object[] { new InvalidOperationException() };
            yield return new object[] { new NotSupportedException() };
            yield return new object[] { new NullReferenceException() };
            yield return new object[] { new ObjectDisposedException("dummy") };
        }

        /// <summary>
        ///   Provides the test cases for non-fatal exceptions that are retriable
        ///   when encountered in a subscription.
        /// </summary>
        ///
        public static IEnumerable<object[]> NonFatalRetriableExceptionTestCases()
        {
            yield return new object[] { new EventHubsException(true, "Test") };
            yield return new object[] { new EventHubsCommunicationException("Test", "Test") };
            yield return new object[] { new ServiceBusyException("Test", "Test") };
            yield return new object[] { new TimeoutException() };
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ConstructorValidatesTheConsumerGroup(string consumerGroup)
        {
            var credential = new Mock<EventHubTokenCredential>(Mock.Of<TokenCredential>(), "{namespace}.servicebus.windows.net");
            Assert.That(() => new EventHubConsumerClient(consumerGroup, "dummyConnection", new EventHubConsumerClientOptions()), Throws.InstanceOf<ArgumentException>(), "The connection string constructor should validate the consumer group.");
            Assert.That(() => new EventHubConsumerClient(consumerGroup, "dummyNamespace", "dummyEventHub", credential.Object, new EventHubConsumerClientOptions()), Throws.InstanceOf<ArgumentException>(), "The namespace constructor should validate the consumer group.");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ConstructorValidatesTheConnectionString(string connectionString)
        {
            Assert.That(() => new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString), Throws.InstanceOf<ArgumentException>(), "The constructor without options should ensure a connection string.");
            Assert.That(() => new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString, "dummy", new EventHubConsumerClientOptions()), Throws.InstanceOf<ArgumentException>(), "The constructor with options should ensure a connection string.");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ConstructorValidatesTheNamespace(string constructorArgument)
        {
            var credential = new Mock<EventHubTokenCredential>(Mock.Of<TokenCredential>(), "{namespace}.servicebus.windows.net");
            Assert.That(() => new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, constructorArgument, "dummy", credential.Object), Throws.InstanceOf<ArgumentException>());
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ConstructorValidatesTheEventHub(string constructorArgument)
        {
            var credential = new Mock<EventHubTokenCredential>(Mock.Of<TokenCredential>(), "{namespace}.servicebus.windows.net");
            Assert.That(() => new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, "namespace", constructorArgument, credential.Object), Throws.InstanceOf<ArgumentException>());
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ConstructorValidatesTheCredential()
        {
            Assert.That(() => new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, "namespace", "hubName", default(TokenCredential)), Throws.ArgumentNullException);
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ConstructorValidatesTheConnection()
        {
            Assert.That(() => new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, default(EventHubConnection)), Throws.ArgumentNullException);
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ConnectionStringConstructorSetsTheRetryPolicy()
        {
            var expected = Mock.Of<EventHubsRetryPolicy>();
            var options = new EventHubConsumerClientOptions { RetryOptions = new RetryOptions { CustomRetryPolicy = expected } };
            var connectionString = "Endpoint=sb://somehost.com;SharedAccessKeyName=ABC;SharedAccessKey=123;EntityPath=somehub";
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString, options);

            Assert.That(GetRetryPolicy(consumer), Is.SameAs(expected));
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ExpandedConstructorSetsTheRetryPolicy()
        {
            var expected = Mock.Of<EventHubsRetryPolicy>();
            var credential = new Mock<EventHubTokenCredential>(Mock.Of<TokenCredential>(), "{namespace}.servicebus.windows.net");
            var options = new EventHubConsumerClientOptions { RetryOptions = new RetryOptions { CustomRetryPolicy = expected } };
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, "namespace", "hub", credential.Object, options);

            Assert.That(GetRetryPolicy(consumer), Is.SameAs(expected));
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ConnectionConstructorSetsTheRetryPolicy()
        {
            var expected = Mock.Of<EventHubsRetryPolicy>();
            var options = new EventHubConsumerClientOptions { RetryOptions = new RetryOptions { CustomRetryPolicy = expected } };
            var mockConnection = new MockConnection();
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection, options);

            Assert.That(GetRetryPolicy(consumer), Is.SameAs(expected));
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ConnectionStringConstructorCreatesDefaultOptions()
        {
            var expected = new EventHubConsumerClientOptions().RetryOptions;
            var connectionString = "Endpoint=sb://somehost.com;SharedAccessKeyName=ABC;SharedAccessKey=123;EntityPath=somehub";
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString);

            var policy = GetRetryPolicy(consumer);
            Assert.That(policy, Is.Not.Null, "There should have been a retry policy set.");
            Assert.That(policy, Is.InstanceOf<BasicRetryPolicy>(), "The default retry policy should be a basic policy.");

            var actual = ((BasicRetryPolicy)policy).Options;
            Assert.That(actual.IsEquivalentTo(expected), Is.True, "The default retry policy should be based on the default retry options.");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ExpandedConstructorCreatesDefaultOptions()
        {
            var credential = new Mock<EventHubTokenCredential>(Mock.Of<TokenCredential>(), "{namespace}.servicebus.windows.net");
            var expected = new EventHubConsumerClientOptions().RetryOptions;
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, "some-namespace", "hubName", credential.Object);

            var policy = GetRetryPolicy(consumer);
            Assert.That(policy, Is.Not.Null, "There should have been a retry policy set.");
            Assert.That(policy, Is.InstanceOf<BasicRetryPolicy>(), "The default retry policy should be a basic policy.");

            var actual = ((BasicRetryPolicy)policy).Options;
            Assert.That(actual.IsEquivalentTo(expected), Is.True, "The default retry policy should be based on the default retry options.");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ConnectionConstructorCreatesDefaultOptions()
        {
            var expected = new EventHubConsumerClientOptions().RetryOptions;
            var mockConnection = new MockConnection();
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            var policy = GetRetryPolicy(consumer);
            Assert.That(policy, Is.Not.Null, "There should have been a retry policy set.");
            Assert.That(policy, Is.InstanceOf<BasicRetryPolicy>(), "The default retry policy should be a basic policy.");

            var actual = ((BasicRetryPolicy)policy).Options;
            Assert.That(actual.IsEquivalentTo(expected), Is.True, "The default retry policy should be based on the default retry options.");
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        [TestCase(null)]
        [TestCase(1)]
        [TestCase(32769)]
        public void ConnectionStringConstructorSetsTheOwnerLevel(long? priority)
        {
            var options = new EventHubConsumerClientOptions
            {
                OwnerLevel = priority
            };

            var connectionString = "Endpoint=sb://somehost.com;SharedAccessKeyName=ABC;SharedAccessKey=123;EntityPath=somehub";
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString, options);

            Assert.That(consumer.OwnerLevel, Is.EqualTo(priority));
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        [TestCase(null)]
        [TestCase(1)]
        [TestCase(32769)]
        public void ExpandedConstructorSetsTheOwnerLevel(long? priority)
        {
            var options = new EventHubConsumerClientOptions
            {
                OwnerLevel = priority
            };

            var credential = new Mock<EventHubTokenCredential>(Mock.Of<TokenCredential>(), "{namespace}.servicebus.windows.net");
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, "namespace-name", "hub-name", credential.Object, options);
            Assert.That(consumer.OwnerLevel, Is.EqualTo(priority));
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        [TestCase(null)]
        [TestCase(1)]
        [TestCase(32769)]
        public void ConnectionConstructorSetsTheOwnerLevel(long? priority)
        {
            var options = new EventHubConsumerClientOptions
            {
                OwnerLevel = priority
            };

            var mockConnection = new MockConnection();
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection, options);

            Assert.That(consumer.OwnerLevel, Is.EqualTo(priority));
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ConnectionStringConstructorSetsTheConsumerGroup()
        {
            var consumerGroup = "SomeGroup";
            var connectionString = "Endpoint=sb://somehost.com;SharedAccessKeyName=ABC;SharedAccessKey=123;EntityPath=somehub";
            var consumer = new EventHubConsumerClient(consumerGroup, connectionString);

            Assert.That(consumer.ConsumerGroup, Is.EqualTo(consumerGroup));
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ExpandedConstructorSetsTheConsumerGroup()
        {
            var consumerGroup = "SomeGroup";
            var credential = new Mock<EventHubTokenCredential>(Mock.Of<TokenCredential>(), "{namespace}.servicebus.windows.net");
            var consumer = new EventHubConsumerClient(consumerGroup, "namespace", "eventHub", credential.Object);

            Assert.That(consumer.ConsumerGroup, Is.EqualTo(consumerGroup));
        }

        /// <summary>
        ///   Verifies functionality of the constructor.
        /// </summary>
        ///
        [Test]
        public void ConnectionConstructorSetsTheConsumerGroup()
        {
            var consumerGroup = "SomeGroup";
            var mockConnection = new MockConnection();
            var consumer = new EventHubConsumerClient(consumerGroup, mockConnection);

            Assert.That(consumer.ConsumerGroup, Is.EqualTo(consumerGroup));
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.FullyQualifiedNamespace" />
        ///   property.
        /// </summary>
        ///
        [Test]
        public void ConsumerDelegatesForTheFullyQualifiedNamespaceName()
        {
            var expected = "SomeNamespace";
            var mockConnection = new MockConnection(expected);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            Assert.That(consumer.FullyQualifiedNamespace, Is.EqualTo(expected));
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubProducerClient.EventHubName" />
        ///   property.
        /// </summary>
        ///
        [Test]
        public void ConsumerDelegatesForTheEventHubName()
        {
            var expected = "EventHubName";
            var mockConnection = new MockConnection(eventHubName: expected);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            Assert.That(consumer.EventHubName, Is.EqualTo(expected));
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.GetEventHubPropertiesAsync"/>
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task GetEventHubPropertiesAsyncUsesTheRetryPolicy()
        {
            var mockConnection = new MockConnection();
            var retryPolicy = Mock.Of<EventHubsRetryPolicy>();
            var options = new EventHubConsumerClientOptions { RetryOptions = new RetryOptions { CustomRetryPolicy = retryPolicy } };
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection, options);

            await consumer.GetEventHubPropertiesAsync();
            Assert.That(mockConnection.GetPropertiesInvokedWith, Is.SameAs(retryPolicy), "Either the call was not delegated or the retry policy was not passed.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.GetPartitionIdsAsync"/>
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task GetPartitionIdsUsesTheRetryPolicy()
        {
            var mockConnection = new MockConnection();
            var retryPolicy = Mock.Of<EventHubsRetryPolicy>();
            var options = new EventHubConsumerClientOptions { RetryOptions = new RetryOptions { CustomRetryPolicy = retryPolicy } };
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection, options);

            await consumer.GetPartitionIdsAsync();
            Assert.That(mockConnection.GetPartitionIdsInvokedWith, Is.SameAs(retryPolicy), "Either the call was not delegated or the retry policy was not passed.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.GetPartitionPropertiesAsync"/>
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task GetPartitionPropertiesUsesTheRetryPolicy()
        {
            var mockConnection = new MockConnection();
            var retryPolicy = Mock.Of<EventHubsRetryPolicy>();
            var options = new EventHubConsumerClientOptions { RetryOptions = new RetryOptions { CustomRetryPolicy = retryPolicy } };
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection, options);

            await consumer.GetPartitionPropertiesAsync("1");
            Assert.That(mockConnection.GetPartitionPropertiesInvokedWith, Is.SameAs(retryPolicy), "Either the call was not delegated or the retry policy was not passed.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.CloseAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task CloseAsyncClosesActiveTransportConsumers()
        {
            var transportConsumer = new ObservableTransportConsumerMock();
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            await using var firstReceiver = consumer.CreatePartitionReceiver("0", EventPosition.Earliest);
            await using var secondReceiver = consumer.CreatePartitionReceiver("0", EventPosition.FromOffset(23));

            await consumer.CloseAsync();
            Assert.That(transportConsumer.WasCloseCalled, Is.True);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.CloseAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task CloseAsyncSurfacesExceptionsForActiveTransportConsumers()
        {
            var mockTransportConsumer = new Mock<TransportConsumer>();
            var mockConnection = new MockConnection(() => mockTransportConsumer.Object);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            mockTransportConsumer
                .Setup(consumer => consumer.CloseAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new InvalidCastException()));

            try { await using var receiver = consumer.CreatePartitionReceiver("0", EventPosition.Earliest); } catch {}

            Assert.That(async () => await consumer.CloseAsync(), Throws.InstanceOf<InvalidCastException>());
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.CloseAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task CloseClosesActiveTransportConsumers()
        {
            var transportConsumer = new ObservableTransportConsumerMock();
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            await using var firstReceiver = consumer.CreatePartitionReceiver("0", EventPosition.Earliest);
            await using var secondReceiver = consumer.CreatePartitionReceiver("0", EventPosition.FromOffset(23));

            consumer.Close();
            Assert.That(transportConsumer.WasCloseCalled, Is.True);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.CloseAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task CloseSurfacesExceptionsForActiveTransportConsumers()
        {
            var mockTransportConsumer = new Mock<TransportConsumer>();
            var mockConnection = new MockConnection(() => mockTransportConsumer.Object);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            mockTransportConsumer
                .Setup(consumer => consumer.CloseAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.FromException(new InvalidCastException()));

            try { await using var receiver = consumer.CreatePartitionReceiver("0", EventPosition.Earliest); } catch {}

            Assert.That(() => consumer.Close(), Throws.InstanceOf<InvalidCastException>());
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncWithNoWaitTimeReturnsAnEnumerable()
        {
            var transportConsumer = new ObservableTransportConsumerMock();
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            IAsyncEnumerable<PartitionEvent> enumerable = consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12));

            Assert.That(enumerable, Is.Not.Null, "An enumerable should have been returned.");
            Assert.That(enumerable, Is.InstanceOf<IAsyncEnumerable<PartitionEvent>>(), "The enumerable should be of the correct type.");

            await using (IAsyncEnumerator<PartitionEvent> enumerator = enumerable.GetAsyncEnumerator())
            {
                Assert.That(enumerator, Is.Not.Null, "The enumerable should be able to produce an enumerator.");
                Assert.That(enumerator, Is.InstanceOf<IAsyncEnumerator<PartitionEvent>>(), "The enumerator should be of the correct type.");
            }
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncWithWaitTimeReturnsAnEnumerable()
        {
            var transportConsumer = new ObservableTransportConsumerMock();
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            IAsyncEnumerable<PartitionEvent> enumerable = consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), TimeSpan.FromSeconds(15));

            Assert.That(enumerable, Is.Not.Null, "An enumerable should have been returned.");
            Assert.That(enumerable, Is.InstanceOf<IAsyncEnumerable<PartitionEvent>>(), "The enumerable should be of the correct type.");

            await using (IAsyncEnumerator<PartitionEvent> enumerator = enumerable.GetAsyncEnumerator())
            {
                Assert.That(enumerator, Is.Not.Null, "The enumerable should be able to produce an enumerator.");
                Assert.That(enumerator, Is.InstanceOf<IAsyncEnumerator<PartitionEvent>>(), "The enumerator should be of the correct type.");
            }
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public void ReadEventsFromPartitionAsyncThrowsIfConsumerClosedBeforeRead()
        {
            var events = new List<EventData>
            {
               new EventData(Encoding.UTF8.GetBytes("One")),
               new EventData(Encoding.UTF8.GetBytes("Two")),
               new EventData(Encoding.UTF8.GetBytes("Three")),
               new EventData(Encoding.UTF8.GetBytes("Four")),
               new EventData(Encoding.UTF8.GetBytes("Five"))
            };

            var transportConsumer = new PublishingTransportConsumerMock(events);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var receivedEvents = 0;

            using var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(250);

            Assert.That(async () =>
            {
                await consumer.CloseAsync(cancellation.Token);

                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), cancellation.Token))
                {
                    if (partitionEvent.Data == null)
                    {
                        break;
                    }

                    ++receivedEvents;
                }
            }, Throws.InstanceOf<EventHubsClientClosedException>(), "The iterator should have indicated the consumer was closed.");

            Assert.That(cancellation.IsCancellationRequested, Is.False, "The cancellation should not have been requested.");
            Assert.That(receivedEvents, Is.EqualTo(0), "There should have been no events received.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public void ReadEventsFromPartitionAsyncThrowsIfCancelledBeforeRead()
        {
            var events = new List<EventData>
            {
               new EventData(Encoding.UTF8.GetBytes("One")),
               new EventData(Encoding.UTF8.GetBytes("Two")),
               new EventData(Encoding.UTF8.GetBytes("Three")),
               new EventData(Encoding.UTF8.GetBytes("Four")),
               new EventData(Encoding.UTF8.GetBytes("Five"))
            };

            var transportConsumer = new PublishingTransportConsumerMock(events);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var receivedEvents = 0;

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            Assert.That(async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), cancellation.Token))
                {
                    if (partitionEvent.Data == null)
                    {
                        break;
                    }

                    ++receivedEvents;
                }
            }, Throws.InstanceOf<TaskCanceledException>(), "The iterator should have indicated the token was not active.");

            Assert.That(cancellation.IsCancellationRequested, Is.True, "The cancellation should have been requested.");
            Assert.That(receivedEvents, Is.EqualTo(0), "There should have been no events received.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public void ReadEventsFromPartitionAsyncThrowsIfCancelledDuringRead()
        {
            var events = new List<EventData>
            {
               new EventData(Encoding.UTF8.GetBytes("One")),
               new EventData(Encoding.UTF8.GetBytes("Two")),
               new EventData(Encoding.UTF8.GetBytes("Three")),
               new EventData(Encoding.UTF8.GetBytes("Four")),
               new EventData(Encoding.UTF8.GetBytes("Five"))
            };

            var transportConsumer = new PublishingTransportConsumerMock(events);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var expectedEvents = events.Count - 2;
            var receivedEvents = 0;

            using var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(30));

            Assert.That(async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), cancellation.Token))
                {
                    if (++receivedEvents >= expectedEvents)
                    {
                        cancellation.Cancel();
                    }
                }
            }, Throws.InstanceOf<TaskCanceledException>(), "The iterator should have indicated the token was not active.");

            Assert.That(cancellation.IsCancellationRequested, Is.True, "The cancellation should have been requested.");
            Assert.That(receivedEvents, Is.EqualTo(expectedEvents), "There should have been the expected number events received.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public void ReadEventsFromPartitionAsyncDoesNotThrowIfNotCancelled()
        {
            var events = new List<EventData>
            {
               new EventData(Encoding.UTF8.GetBytes("One")),
               new EventData(Encoding.UTF8.GetBytes("Two")),
               new EventData(Encoding.UTF8.GetBytes("Three")),
               new EventData(Encoding.UTF8.GetBytes("Four")),
               new EventData(Encoding.UTF8.GetBytes("Five"))
            };

            var transportConsumer = new PublishingTransportConsumerMock(events);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var expectedEvents = events.Count - 2;
            var receivedEvents = 0;

            using var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(30));

            Assert.That(async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), cancellation.Token))
                {
                    if (++receivedEvents >= expectedEvents)
                    {
                       break;
                    }
                }
            }, Throws.Nothing, "The iterator should not have been canceled.");

            Assert.That(cancellation.IsCancellationRequested, Is.False, "Cancellation should not have been requested.");
            Assert.That(receivedEvents, Is.EqualTo(expectedEvents), "There should have been the expected number events received.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncStopsPublishingWhenCanceled()
        {
            var events = Enumerable
                .Range(0, 1500)
                .Select(index => new EventData(Encoding.UTF8.GetBytes($"Event: { index }")))
                .ToList();

            var transportConsumer = new PublishingTransportConsumerMock(events);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var expectedEvents = 100;
            var receivedEvents = 0;

            using var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), cancellation.Token))
                {
                    if (++receivedEvents >= expectedEvents)
                    {
                        cancellation.Cancel();
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Expected; no action needed.
            }

            // There is a fair amount of non-determinism during background processing.  Pause a moment to allow additional events to build if publishing
            // has not stopped.  Allow for a fairly wide buffer for range checking to combat the potential that an additional receive pump took place
            // while cancellation was communicated.

            await Task.Delay(250);

            Assert.That(receivedEvents, Is.EqualTo(expectedEvents), "There should have been the expected number events received.");
            Assert.That(transportConsumer.PublishIndex, Is.EqualTo(expectedEvents).Within(transportConsumer.LastMaximumMessageCount * 2), "Publishing should have stopped when the iterator was canceled.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncStopsPublishingWhenIterationStops()
        {
            var events = Enumerable
                .Range(0, 1500)
                .Select(index => new EventData(Encoding.UTF8.GetBytes($"Event: { index }")))
                .ToList();

            var transportConsumer = new PublishingTransportConsumerMock(events);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var expectedEvents = 100;
            var receivedEvents = 0;

            using var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), cancellation.Token))
                {
                    if (++receivedEvents >= expectedEvents)
                    {
                        break;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Expected; no action needed.
            }

            // There is a fair amount of non-determinism during background processing.  Pause a moment to allow additional events to build if publishing
            // has not stopped.  Allow for a fairly wide buffer for range checking to combat the potential that an additional receive pump took place
            // while cancellation was communicated.

            await Task.Delay(250);

            Assert.That(receivedEvents, Is.EqualTo(expectedEvents), "There should have been the expected number events received.");
            Assert.That(transportConsumer.PublishIndex, Is.EqualTo(expectedEvents).Within(transportConsumer.LastMaximumMessageCount * 2), "Publishing should have stopped when the iterator was canceled.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncStopsPublishingOnException()
        {
            var iteration = 0;
            var lastMaximumCount = 0;
            var receivedEvents = 0;
            var expectedEvents = 5;
            var expectedException = new InvalidCastException();

            var transportConsumer = new ReceiveCallbackTransportConsumerMock((maxCount, _) =>
            {
                lastMaximumCount = maxCount;

                if (iteration++ == expectedEvents)
                {
                    throw expectedException;
                }

                return new EventData[] { new EventData(Encoding.UTF8.GetBytes("Event")) };
            });

            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);

            using var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), cancellation.Token))
                {
                    ++receivedEvents;
                }
            }
            catch (Exception ex) when (ex == expectedException)
            {
                // Expected; no action needed.
            }

            // There is a fair amount of non-determinism during background processing.  Pause a moment to allow additional events to build if publishing
            // has not stopped.  Allow for a fairly wide buffer for range checking to combat the potential that an additional receive pump took place
            // while cancellation was communicated.

            await Task.Delay(250);

            Assert.That(receivedEvents, Is.EqualTo(expectedEvents), "There should have been the expected number events received.");
            Assert.That(iteration, Is.EqualTo(expectedEvents).Within(lastMaximumCount * 2), "Publishing should have stopped when the exception was thrown.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncPublishesEventsWithOneIteratorAndSingleBatch()
        {
            var events = new List<EventData>
            {
                new EventData(Encoding.UTF8.GetBytes("One")),
                new EventData(Encoding.UTF8.GetBytes("Two")),
                new EventData(Encoding.UTF8.GetBytes("Three")),
                new EventData(Encoding.UTF8.GetBytes("Four")),
                new EventData(Encoding.UTF8.GetBytes("Five"))
            };

            var transportConsumer = new PublishingTransportConsumerMock(events);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var receivedEvents = new List<EventData>();

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), cancellation.Token))
            {
                receivedEvents.Add(partitionEvent.Data);

                if (receivedEvents.Count >= events.Count)
                {
                    break;
                }
            }

            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(receivedEvents, Is.EquivalentTo(events), "The received events should match the published events.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncPublishesEventsWithMultipleIteratorsAndSingleBatch()
        {
            var events = new List<EventData>
            {
                new EventData(Encoding.UTF8.GetBytes("One")),
                new EventData(Encoding.UTF8.GetBytes("Two")),
                new EventData(Encoding.UTF8.GetBytes("Three")),
                new EventData(Encoding.UTF8.GetBytes("Four")),
                new EventData(Encoding.UTF8.GetBytes("Five"))
            };

            var waitTime = TimeSpan.FromMilliseconds(5);
            var partition = "0";
            var position = EventPosition.FromOffset(22);
            var mockConnection = new MockConnection(() => new PublishingTransportConsumerMock(events));
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var firstSubscriberEvents = new List<EventData>();
            var secondSubscriberEvents = new List<EventData>();
            var firstCompletionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondCompletionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(75));

            var firstSubscriberTask = Task.Run(async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync(partition, position, waitTime, cancellation.Token).ConfigureAwait(false))
                {
                    if (partitionEvent.Data != null)
                    {
                        firstSubscriberEvents.Add(partitionEvent.Data);
                    }

                    if (firstSubscriberEvents.Count >= events.Count)
                    {
                        break;
                    }
                }

                firstCompletionSource.TrySetResult(0);

            }, cancellation.Token);

            var secondSubscriberTask = Task.Run(async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync(partition, position, waitTime, cancellation.Token).ConfigureAwait(false))
                {
                    if (partitionEvent.Data != null)
                    {
                        secondSubscriberEvents.Add(partitionEvent.Data);
                    }

                    if (secondSubscriberEvents.Count >= events.Count)
                    {
                        break;
                    }
                }

                secondCompletionSource.TrySetResult(0);

            }, cancellation.Token);

            await Task.WhenAll(firstSubscriberTask, secondSubscriberTask, firstCompletionSource.Task, secondCompletionSource.Task).ConfigureAwait(false);
            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(firstSubscriberEvents, Is.EquivalentTo(events), "The received events for the first subscriber should match the published events.");
            Assert.That(secondSubscriberEvents, Is.EquivalentTo(events), "The received events for the second subscriber should match the published events.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncPublishesEventsWithOneIteratorAndMultipleBatches()
        {
            var events = new List<EventData>();
            var transportConsumer = new PublishingTransportConsumerMock(events);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var receivedEvents = new List<EventData>();

            events.AddRange(
                Enumerable.Range(0, (GetBackgroundPublishReceiveBatchSize(consumer) * 3))
                    .Select(index => new EventData(Encoding.UTF8.GetBytes($"Event Number { index }")))
            );

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromSequenceNumber(123), cancellation.Token))
            {
                receivedEvents.Add(partitionEvent.Data);

                if (receivedEvents.Count >= events.Count)
                {
                    break;
                }
            }

            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(receivedEvents, Is.EquivalentTo(events), "The received events should match the published events.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncPublishesEventsWithMultipleIteratorsAndMultipleBatches()
        {
            var waitTime = TimeSpan.FromMilliseconds(5);
            var events = new List<EventData>();
            var partition = "0";
            var position = EventPosition.FromSequenceNumber(453);
            var mockConnection = new MockConnection(() => new PublishingTransportConsumerMock(events));
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var firstSubscriberEvents = new List<EventData>();
            var secondSubscriberEvents = new List<EventData>();
            var firstCompletionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondCompletionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            events.AddRange(
                Enumerable.Range(0, (GetBackgroundPublishReceiveBatchSize(consumer) * 3))
                    .Select(index => new EventData(Encoding.UTF8.GetBytes($"Event Number { index }")))
            );

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(75));

            var firstSubscriberTask = Task.Run(async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync(partition, position, waitTime, cancellation.Token).ConfigureAwait(false))
                {
                    if (partitionEvent.Data != null)
                    {
                        firstSubscriberEvents.Add(partitionEvent.Data);
                    }

                    if (firstSubscriberEvents.Count >= events.Count)
                    {
                        break;
                    }
                }

                firstCompletionSource.TrySetResult(0);

            }, cancellation.Token);

            var secondSubscriberTask = Task.Run(async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync(partition, position, waitTime, cancellation.Token).ConfigureAwait(false))
                {
                    if (partitionEvent.Data != null)
                    {
                        secondSubscriberEvents.Add(partitionEvent.Data);
                    }

                    if (secondSubscriberEvents.Count >= events.Count)
                    {
                        break;
                    }
                }

                secondCompletionSource.TrySetResult(0);

            }, cancellation.Token);

            await Task.WhenAll(firstSubscriberTask, secondSubscriberTask, firstCompletionSource.Task, secondCompletionSource.Task).ConfigureAwait(false);
            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(firstSubscriberEvents, Is.EquivalentTo(events), "The received events for the first subscriber should match the published events.");
            Assert.That(secondSubscriberEvents, Is.EquivalentTo(events), "The received events for the second subscriber should match the published events.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task ReadEventsFromPartitionAsyncRespectsWaitTimeWhenPublishingEvents()
        {
            var events = new List<EventData>
            {
                new EventData(Encoding.UTF8.GetBytes("One")),
                new EventData(Encoding.UTF8.GetBytes("Two")),
                new EventData(Encoding.UTF8.GetBytes("Three")),
                new EventData(Encoding.UTF8.GetBytes("Four")),
                new EventData(Encoding.UTF8.GetBytes("Five"))
            };

            var maxWaitTime = TimeSpan.FromMilliseconds(50);
            var publishDelay = maxWaitTime.Add(TimeSpan.FromMilliseconds(15));
            var transportConsumer = new PublishingTransportConsumerMock(events, () => publishDelay);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var receivedEvents = new List<EventData>();
            var consecutiveEmptyCount = 0;

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(100));

            await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(12), maxWaitTime, cancellation.Token))
            {
                receivedEvents.Add(partitionEvent.Data);
                consecutiveEmptyCount = (partitionEvent.Data == null) ? consecutiveEmptyCount + 1 : 0;

                if (consecutiveEmptyCount > 1)
                {
                    break;
                }
            }

            // Because there is a random delay in the receive loop, the exact count of empty events cannot be predicted.  There
            // should be at least one total, but no more than one for each published event.

            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(receivedEvents.Count, Is.AtLeast(events.Count + 1).And.LessThanOrEqualTo(events.Count * 2), "There should be empty events present due to the wait time.");
            Assert.That(receivedEvents.Where(item => item != null), Is.EquivalentTo(events), "The received events should match the published events when empty events are removed.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        [TestCase(typeof(ChannelClosedException))]
        [TestCase(typeof(ArithmeticException))]
        [TestCase(typeof(IndexOutOfRangeException))]
        public void ReadEventsFromPartitionAsyncPropagatesException(Type exceptionType)
        {
            var transportConsumer = new ReceiveCallbackTransportConsumerMock((_max, _time) => throw (Exception)Activator.CreateInstance(exceptionType));
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var receivedEvents = 0;

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Assert.That(async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(2), cancellation.Token))
                {
                    ++receivedEvents;
                    break;
                }
            }, Throws.TypeOf(exceptionType), "An exception during receive should be propagated by the iterator.");

            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(receivedEvents, Is.EqualTo(0), "No events should have been received.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        [TestCase(typeof(TaskCanceledException))]
        [TestCase(typeof(OperationCanceledException))]
        public void ReadEventsFromPartitionAsyncSurfacesCancelation(Type exceptionType)
        {
            var transportConsumer = new ReceiveCallbackTransportConsumerMock((_max, _time) => throw (Exception)Activator.CreateInstance(exceptionType));
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var receivedEvents = 0;

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Assert.That(async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.Earliest, cancellation.Token))
                {
                    ++receivedEvents;
                    break;
                }
            }, Throws.InstanceOf<TaskCanceledException>(), "Cancellation should be surfaced as a TaskCanceledException");

            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(receivedEvents, Is.EqualTo(0), "No events should have been received.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        [TestCaseSource(nameof(NonFatalNotRetriableExceptionTestCases))]
        public void ReadEventsFromPartitionAsyncSurfacesNonRetriableExceptions(Exception exception)
        {
            var transportConsumer = new ReceiveCallbackTransportConsumerMock((_max, _time) => throw exception);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection);
            var receivedEvents = 0;

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Func<Task> invoke = async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(120), cancellation.Token))
                {
                    ++receivedEvents;
                    break;
                }
            };

            Assert.That(async () => await invoke(), Throws.TypeOf(exception.GetType()), "The enumerator should surface the exception.");
            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(receivedEvents, Is.EqualTo(0), "No events should have been received.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        [TestCaseSource(nameof(NonFatalRetriableExceptionTestCases))]
        public void ReadEventsFromPartitionAsyncRetriesAndSurfacesRetriableExceptions(Exception exception)
        {
            const int maximumRetries = 2;

            var expectedReceiveCalls = (maximumRetries + 1);
            var receiveCalls = 0;

            Func<int, TimeSpan?, IEnumerable<EventData>> receiveCallback = (_max, _time) =>
            {
                ++receiveCalls;
                throw exception;
            };

            var mockRetryPolicy = new Mock<EventHubsRetryPolicy>();
            var options = new EventHubConsumerClientOptions { RetryOptions = new RetryOptions { CustomRetryPolicy = mockRetryPolicy.Object } };
            var transportConsumer = new ReceiveCallbackTransportConsumerMock(receiveCallback);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection, options);
            var receivedEvents = 0;

            mockRetryPolicy
                .Setup(policy => policy.CalculateRetryDelay(It.Is<Exception>(value => value == exception), It.Is<int>(value => value <= maximumRetries)))
                .Returns(TimeSpan.FromMilliseconds(5));

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Func<Task> invoke = async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromSequenceNumber(123), cancellation.Token))
                {
                    ++receivedEvents;
                    break;
                }
            };

            Assert.That(async () => await invoke(), Throws.TypeOf(exception.GetType()), "The enumerator should surface the exception.");
            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(receiveCalls, Is.EqualTo(expectedReceiveCalls), "The retry policy should have been applied.");
            Assert.That(receivedEvents, Is.EqualTo(0), "No events should have been received.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.ReadEventsFromPartitionAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        [TestCaseSource(nameof(NonFatalRetriableExceptionTestCases))]
        public void ReadEventsFromPartitionAsyncHonorsRetryPolicyForRetriableExceptions(Exception exception)
        {
            var receiveCalls = 0;

            Func<int, TimeSpan?, IEnumerable<EventData>> receiveCallback = (_max, _time) =>
            {
                ++receiveCalls;
                throw exception;
            };

            var mockRetryPolicy = new Mock<EventHubsRetryPolicy>();
            var options = new EventHubConsumerClientOptions { RetryOptions = new RetryOptions { CustomRetryPolicy = mockRetryPolicy.Object } };
            var transportConsumer = new ReceiveCallbackTransportConsumerMock(receiveCallback);
            var mockConnection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, mockConnection, options);
            var receivedEvents = 0;

            mockRetryPolicy
                .Setup(policy => policy.CalculateRetryDelay(It.IsAny<Exception>(), It.IsAny<int>()))
                .Returns(default(TimeSpan?));

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Func<Task> invoke = async () =>
            {
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsFromPartitionAsync("0", EventPosition.FromOffset(888), cancellation.Token))
                {
                    ++receivedEvents;
                    break;
                }
            };

            Assert.That(async () => await invoke(), Throws.TypeOf(exception.GetType()), "The enumerator should surface the exception.");
            Assert.That(cancellation.IsCancellationRequested, Is.False, "The iteration should have completed normally.");
            Assert.That(receiveCalls, Is.EqualTo(1), "The retry policy should have been respected.");
            Assert.That(receivedEvents, Is.EqualTo(0), "No events should have been received.");
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.CloseAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task CloseAsyncClosesTheConnectionWhenOwned()
        {
            var connectionString = "Endpoint=sb://somehost.com;SharedAccessKeyName=ABC;SharedAccessKey=123;EntityPath=somehub";
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString);

            await consumer.CloseAsync();

            var connection = GetConnection(consumer);
            Assert.That(connection.Closed, Is.True);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.Close" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public void CloseClosesTheConnectionWhenOwned()
        {
            var connectionString = "Endpoint=sb://somehost.com;SharedAccessKeyName=ABC;SharedAccessKey=123;EntityPath=somehub";
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connectionString);

            consumer.Close();

            var connection = GetConnection(consumer);
            Assert.That(connection.Closed, Is.True);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubConsumerClient.CloseAsync" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public async Task CloseAsyncDoesNotCloseTheConnectionWhenNotOwned()
        {
            var transportConsumer = new ObservableTransportConsumerMock();
            var connection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connection);

            await consumer.CloseAsync();
            Assert.That(connection.Closed, Is.False);
        }

        /// <summary>
        ///   Verifies functionality of the <see cref="EventHubProducer.Close" />
        ///   method.
        /// </summary>
        ///
        [Test]
        public void CloseDoesNotCloseTheConnectionWhenNotOwned()
        {
            var transportConsumer = new ObservableTransportConsumerMock();
            var connection = new MockConnection(() => transportConsumer);
            var consumer = new EventHubConsumerClient(EventHubConsumerClient.DefaultConsumerGroupName, connection);

            consumer.Close();
            Assert.That(connection.Closed, Is.False);
        }

        /// <summary>
        ///   Retrieves the Connection for the consumer using its private accessor.
        /// </summary>
        ///
        private static EventHubConnection GetConnection(EventHubConsumerClient consumer) =>
            (EventHubConnection)
                typeof(EventHubConsumerClient)
                    .GetProperty("Connection", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(consumer);

        /// <summary>
        ///   Retrieves the RetryPolicy for the consumer using its private accessor.
        /// </summary>
        ///
        private static EventHubsRetryPolicy GetRetryPolicy(EventHubConsumerClient consumer) =>
            (EventHubsRetryPolicy)
                typeof(EventHubConsumerClient)
                    .GetProperty("RetryPolicy", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(consumer);

        /// <summary>
        ///   Retrieves the number of background publish event batch size for a consumer, using its private field.
        /// </summary>
        ///
        /// <param name="consumer">The consumer to retrieve the channels for.</param>
        ///
        /// <returns>The size of the batch that is received when publishing events in the background.</returns>
        ///
        private int GetBackgroundPublishReceiveBatchSize(EventHubConsumerClient consumer) =>
            (int)
                typeof(EventHubConsumerClient)
                    .GetField("BackgroundPublishReceiveBatchSize", BindingFlags.Static | BindingFlags.NonPublic)
                    .GetValue(consumer);

        /// <summary>
        ///   Allows for observation of operations performed by the consumer for testing purposes.
        /// </summary>
        ///
        private class ObservableTransportConsumerMock : TransportConsumer
        {
            public bool WasCloseCalled = false;
            public (int, TimeSpan?) ReceiveCalledWith;

            public new EventData LastReceivedEvent
            {
                get => base.LastReceivedEvent;
                set => base.LastReceivedEvent = value;
            }

            public override Task<IEnumerable<EventData>> ReceiveAsync(int maximumMessageCount,
                                                                      TimeSpan? maximumWaitTime,
                                                                      CancellationToken cancellationToken)
            {
                ReceiveCalledWith = (maximumMessageCount, maximumWaitTime);
                return Task.FromResult(Enumerable.Empty<EventData>());
            }

            public override Task CloseAsync(CancellationToken cancellationToken)
            {
                WasCloseCalled = true;
                return Task.CompletedTask;
            }
        }

        /// <summary>
        ///   Allows for publishing a known set of events in response to receive calls
        ///   by the consumer for testing purposes.
        /// </summary>
        ///
        private class PublishingTransportConsumerMock : TransportConsumer
        {
            public IList<EventData> EventsToPublish = new List<EventData>(0);
            public Func<TimeSpan?> PublishDelayCallback = () => null;
            public int PublishIndex = 0;
            public int LastMaximumMessageCount = 0;

            public PublishingTransportConsumerMock(IList<EventData> eventsToPublish = null,
                                                   Func<TimeSpan?> publishDelayCallback = null) : base()
            {
                if (eventsToPublish != null)
                {
                    EventsToPublish = eventsToPublish;
                }

                if (publishDelayCallback != null)
                {
                    PublishDelayCallback = publishDelayCallback;
                }
            }

            public override Task<IEnumerable<EventData>> ReceiveAsync(int maximumMessageCount, TimeSpan? maximumWaitTime, CancellationToken cancellationToken)
            {
                var stopWatch = Stopwatch.StartNew();
                PublishDelayCallback?.Invoke();
                stopWatch.Stop();

                if (((maximumWaitTime.HasValue) && (stopWatch.Elapsed >= maximumWaitTime)) || (PublishIndex >= EventsToPublish.Count))
                {
                    return Task.FromResult(Enumerable.Empty<EventData>());
                }

                var index = PublishIndex;

                if (index + maximumMessageCount > EventsToPublish.Count)
                {
                    maximumMessageCount = (EventsToPublish.Count - index);
                }

                LastMaximumMessageCount = maximumMessageCount;
                PublishIndex = (index + maximumMessageCount);
                var source = EventsToPublish.Skip(index).Take(maximumMessageCount).ToList();

                return Task.FromResult((IEnumerable<EventData>)source);
            }

            public override Task CloseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        /// <summary>
        ///   Allows for invoking a callback in response to receive calls
        ///   by the consumer for testing purposes.
        /// </summary>
        ///
        private class ReceiveCallbackTransportConsumerMock : TransportConsumer
        {
            public Func<int, TimeSpan?, IEnumerable<EventData>> ReceiveCallback = (_max, _wait) => Enumerable.Empty<EventData>();

            public ReceiveCallbackTransportConsumerMock(Func<int, TimeSpan?, IEnumerable<EventData>> receiveCallback = null) : base()
            {
                if (receiveCallback != null)
                {
                    ReceiveCallback = receiveCallback;
                }
            }

            public override Task<IEnumerable<EventData>> ReceiveAsync(int maximumMessageCount, TimeSpan? maximumWaitTime, CancellationToken cancellationToken)
            {
                IEnumerable<EventData> results = ReceiveCallback(maximumMessageCount, maximumWaitTime);
                return Task.FromResult(results);
            }

            public override Task CloseAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        /// <summary>
        ///   Serves as a non-functional connection for testing consumer functionality.
        /// </summary>
        ///
        private class MockConnection : EventHubConnection
        {
            public EventHubsRetryPolicy GetPropertiesInvokedWith = null;
            public EventHubsRetryPolicy GetPartitionIdsInvokedWith = null;
            public EventHubsRetryPolicy GetPartitionPropertiesInvokedWith = null;
            public Func<TransportConsumer> TransportConsumerFactory = () => Mock.Of<TransportConsumer>();
            public bool WasClosed = false;

            public MockConnection(string namespaceName = "fakeNamespace",
                                  string eventHubName = "fakeEventHub") : base(namespaceName, eventHubName, CreateMockCredentials())
            {
            }

            private static EventHubTokenCredential CreateMockCredentials()
            {
                return new Mock<EventHubTokenCredential>(Mock.Of<TokenCredential>(), "{namespace}.servicebus.windows.net").Object;
            }

            public MockConnection(Func<TransportConsumer> transportConsumerFactory,
                                  string namespaceName,
                                  string eventHubName) : this(namespaceName, eventHubName)
            {
                TransportConsumerFactory = transportConsumerFactory;
            }

            public MockConnection(Func<TransportConsumer> transportConsumerFactory) : this(transportConsumerFactory, "fakeNamespace", "fakeEventHub")
            {
            }

            internal override Task<EventHubProperties> GetPropertiesAsync(EventHubsRetryPolicy retryPolicy,
                                                                        CancellationToken cancellationToken = default)
            {
                GetPropertiesInvokedWith = retryPolicy;
                return Task.FromResult(new EventHubProperties(EventHubName, DateTimeOffset.Parse("2015-10-27T00:00:00Z"), new string[] { "0", "1" }));
            }

            internal async override Task<string[]> GetPartitionIdsAsync(EventHubsRetryPolicy retryPolicy,
                                                                        CancellationToken cancellationToken = default)
            {
                GetPartitionIdsInvokedWith = retryPolicy;
                return await base.GetPartitionIdsAsync(retryPolicy, cancellationToken);
            }

            internal override Task<PartitionProperties> GetPartitionPropertiesAsync(string partitionId,
                                                                                    EventHubsRetryPolicy retryPolicy,
                                                                                    CancellationToken cancellationToken = default)
            {
                GetPartitionPropertiesInvokedWith = retryPolicy;
                return Task.FromResult(default(PartitionProperties));
            }

            internal override TransportConsumer CreateTransportConsumer(string consumerGroup, string partitionId, EventPosition eventPosition, EventHubConsumerClientOptions consumerOptions) => TransportConsumerFactory();

            internal override TransportClient CreateTransportClient(string fullyQualifiedNamespace, string eventHubName, EventHubTokenCredential credential, EventHubConnectionOptions options)
            {
                var client = new Mock<TransportClient>();

                client
                    .Setup(client => client.ServiceEndpoint)
                    .Returns(new Uri($"amgp://{ fullyQualifiedNamespace}.com/{eventHubName}"));

                return client.Object;
            }
        }
    }
}
