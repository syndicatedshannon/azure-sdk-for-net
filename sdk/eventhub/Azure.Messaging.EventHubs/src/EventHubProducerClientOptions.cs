﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using Azure.Core;

namespace Azure.Messaging.EventHubs
{
    /// <summary>
    ///   The set of options that can be specified when creating an <see cref="EventHubProducerClient" />
    ///   to configure its behavior.
    /// </summary>
    ///
    public class EventHubProducerClientOptions
    {
        /// <summary>The set of options to use for configuring the connection to the Event Hubs service.</summary>
        private EventHubConnectionOptions _connectionOptions = new EventHubConnectionOptions();

        /// <summary>The set of options to govern retry behavior and try timeouts.</summary>
        private RetryOptions _retryOptions = new RetryOptions();

        /// <summary>
        ///   Gets or sets the options used for configuring the connection to the Event Hubs service.
        /// </summary>
        ///
        public EventHubConnectionOptions ConnectionOptions
        {
            get => _connectionOptions;
            set
            {
                Argument.AssertNotNull(value, nameof(ConnectionOptions));
                _connectionOptions = value;
            }
        }

        /// <summary>
        ///   The set of options to use for determining whether a failed operation should be retried and,
        ///   if so, the amount of time to wait between retry attempts.  These options also control the
        ///   amount of time allowed for publishing events and other interactions with the Event Hubs service.
        /// </summary>
        ///
        public RetryOptions RetryOptions
        {
            get => _retryOptions;
            set
            {
                Argument.AssertNotNull(value, nameof(RetryOptions));
                _retryOptions = value;
            }
        }

        /// <summary>
        ///   Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        ///
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        ///
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        ///
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => base.Equals(obj);

        /// <summary>
        ///   Returns a hash code for this instance.
        /// </summary>
        ///
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        ///
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        ///   Converts the instance to string representation.
        /// </summary>
        ///
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        ///
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString() => base.ToString();

        /// <summary>
        ///   Creates a new copy of the current <see cref="EventHubProducerClientOptions" />, cloning its attributes into a new instance.
        /// </summary>
        ///
        /// <returns>A new copy of <see cref="EventHubProducerClientOptions" />.</returns>
        ///
        internal EventHubProducerClientOptions Clone() =>
            new EventHubProducerClientOptions
            {
                _connectionOptions = ConnectionOptions.Clone(),
                _retryOptions = RetryOptions.Clone()
            };
    }
}
