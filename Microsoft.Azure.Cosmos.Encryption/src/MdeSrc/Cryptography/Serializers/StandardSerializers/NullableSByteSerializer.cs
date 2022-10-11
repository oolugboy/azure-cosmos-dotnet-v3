//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

// This file isn't generated, but this comment is necessary to exclude it from StyleCop analysis.
// <auto-generated/>

using System;

namespace Microsoft.Data.Encryption.Cryptography.Serializers
{
    /// <summary>
    /// Contains the methods for serializing and deserializing <see cref="sbyte"/>? type data objects.
    /// </summary>
    [CLSCompliant(false)]
    internal class NullableSByteSerializer : Serializer<sbyte?>
    {
        private static readonly SByteSerializer serializer = new SByteSerializer();

        /// <summary>
        /// The <see cref="Identifier"/> uniquely identifies a particular Serializer implementation.
        /// </summary>
        public override string Identifier => "SByte_Nullable";

        /// <summary>
        /// Deserializes the provided <paramref name="bytes"/>
        /// </summary>
        /// <param name="bytes">The data to be deserialized</param>
        /// <returns>The serialized data</returns>
        /// <exception cref="MicrosoftDataEncryptionException">
        /// The length of <paramref name="bytes"/> is less than 1.
        /// </exception>
        public override sbyte? Deserialize(byte[] bytes)
        {
            return bytes.IsNull() ? (sbyte?)null : serializer.Deserialize(bytes);
        }

        /// <summary>
        /// Serializes the provided <paramref name="value"/>
        /// </summary>
        /// <param name="value">The value to be serialized</param>
        /// <returns>
        /// An array of bytes with length 1.
        /// </returns>
        public override byte[] Serialize(sbyte? value)
        {
            return value.IsNull() ? null : serializer.Serialize(value.Value);
        }
    }
}
