﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Azure.Messaging.EventHubs.Authorization
{
    /// <summary>
    ///   Provides a credential based on a shared access signature for a given
    ///   Event Hub instance.
    /// </summary>
    ///
    /// <seealso cref="SharedAccessSignature" />
    /// <seealso cref="Azure.Core.TokenCredential" />
    ///
    internal class SharedAccessSignatureCredential : TokenCredential
    {
        /// <summary>The buffer to apply when considering refreshing; signatures that expire less than this duration will be refreshed.</summary>
        private static readonly TimeSpan SignatureRefreshBuffer = TimeSpan.FromMinutes(5);

        /// <summary>The length of time extend signature validity, if a token was requested.</summary>
        private static readonly TimeSpan SignatureExtensionDuration = TimeSpan.FromMinutes(30);

        /// <summary>
        ///   The shared access signature that forms the basis of this security token.
        /// </summary>
        ///
        public SharedAccessSignature SharedAccessSignature { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SharedAccessSignatureCredential"/> class.
        /// </summary>
        ///
        /// <param name="signature">The shared access signature on which to base the token.</param>
        ///
        public SharedAccessSignatureCredential(SharedAccessSignature signature)
        {
            Argument.AssertNotNull(signature, nameof(signature));
            SharedAccessSignature = signature;
        }

        /// <summary>
        ///   Retrieves the token that represents the shared access signature credential, for
        ///   use in authorization against an Event Hub.
        /// </summary>
        ///
        /// <param name="requestContext">The details of the authentication request.</param>
        /// <param name="cancellationToken">The token used to request cancellation of the operation.</param>
        ///
        /// <returns>The token representing the shared access signature for this credential.</returns>
        ///
        public override AccessToken GetToken(TokenRequestContext requestContext,
                                             CancellationToken cancellationToken)
        {
            if (SharedAccessSignature.SignatureExpiration <= DateTimeOffset.UtcNow.Add(SignatureRefreshBuffer))
            {
                SharedAccessSignature.ExtendExpiration(SignatureExtensionDuration);
            }

            return new AccessToken(SharedAccessSignature.Value, SharedAccessSignature.SignatureExpiration);
        }

        /// <summary>
        ///   Retrieves the token that represents the shared access signature credential, for
        ///   use in authorization against an Event Hub.
        /// </summary>
        ///
        /// <param name="requestContext">The details of the authentication request.</param>
        /// <param name="cancellationToken">The token used to request cancellation of the operation.</param>
        ///
        /// <returns>The token representing the shared access signature for this credential.</returns>
        ///
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext,
                                                             CancellationToken cancellationToken) => new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
    }
}
