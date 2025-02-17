﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;

namespace Azure.Messaging.EventHubs
{
    /// <summary>
    ///   The set of options that can be specified to influence the way in which events
    ///   are sent to the Event Hubs service.
    /// </summary>
    ///
    public class SendOptions
    {
        /// <summary>
        ///   Allows a hashing key to be provided for the batch of events, which instructs the Event Hubs
        ///   service map this key to a specific partition but allowing the service to choose an arbitrary,
        ///   partition for this batch of events and any other batches using the same partition hashing key.
        ///
        ///   The selection of a partition is stable for a given partition hashing key.  Should any other
        ///   batches of events be sent using the same exact partition hashing key, the Event Hubs service will
        ///   route them all to the same partition.
        ///
        ///   This should be specified only when there is a need to group events by partition, but there is
        ///   flexibility into which partition they are routed. If ensuring that a batch of events is sent
        ///   only to a specific partition, it is recommended that the identifier of the position be
        ///   specified directly when sending the batch.
        /// </summary>
        ///
        /// <value>
        ///   If the producer wishes to influence the automatic routing of events to partitions, the partition
        ///   hashing key to associate with the event or batch of events; otherwise, <c>null</c>.
        /// </value>
        ///
        /// <remarks>
        ///   If the <see cref="SendOptions.PartitionKey" /> is specified, then no <see cref="SendOptions.PartitionId" />
        ///   may be set when sending.
        /// </remarks>
        ///
        public string PartitionKey { get; set; }

        /// <summary>
        ///   If the identifier is not specified, the Event Hubs service will be responsible for routing events automatically to
        ///   to an available partition.  If specified, the sender is requesting that events be sent to this specific partition.
        /// </summary>
        ///
        /// <value>
        ///   If the producer wishes the events to be automatically to partitions, <c>null</c>; otherwise, the identifier
        ///   of the desired partition.
        /// </value>
        ///
        /// <remarks>
        ///   If the <see cref="SendOptions.PartitionId" /> is specified, then no <see cref="SendOptions.PartitionKey" />
        ///   may be set when sending.
        ///
        ///   <para>Allowing automatic routing of partitions is recommended when:</para>
        ///   <para>- The sending of events needs to be highly available.</para>
        ///   <para>- The event data should be evenly distributed among all available partitions.</para>
        ///
        ///   If no partition is specified, the following rules are used for automatically selecting one:
        ///   <para>1) Distribute the events equally amongst all available partitions using a round-robin approach.</para>
        ///   <para>2) If a partition becomes unavailable, the Event Hubs service will automatically detect it and forward the message to another available partition.</para>
        /// </remarks>
        ///
        public string PartitionId { get; set; }

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
    }
}
