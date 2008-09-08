﻿// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;
using Security.Cryptography.Properties;

namespace Security.Cryptography
{
    /// <summary>
    ///     Generic implementation of a symmetric algorithm which is provided by the BCrypt layer of CNG.
    ///     Concrete SymmetricAlgorithm classes should contain an instance of this type and delegate all of
    ///     their work to that object.
    ///     
    ///     Most of the real encryption work occurs in the BCryptSymmetricCryptoTransform class. (see
    ///     code:code:Microsoft.Security.Cryptography.BCryptSymmetricCryptoTransform).
    /// </summary>
    internal sealed class BCryptSymmetricAlgorithm : SymmetricAlgorithm
    {
        private CngAlgorithm m_algorithm;
        private string m_implementation;

        internal BCryptSymmetricAlgorithm(CngAlgorithm algorithm,
                                          string implementation,
                                          KeySizes[] legalBlockSizes,
                                          KeySizes[] legalkeySizes)
        {
            Debug.Assert(algorithm != null, "algorithm != null");
            Debug.Assert(!String.IsNullOrEmpty(implementation), "!String.IsNullOrEmpty(implementation)");
            Debug.Assert(legalBlockSizes != null, "legalBlockSizes != null");
            Debug.Assert(legalkeySizes != null, "legalKeySizes != null");

            m_algorithm = algorithm;
            m_implementation = implementation;

            LegalBlockSizesValue = legalBlockSizes;
            LegalKeySizesValue = legalkeySizes;
        }

        /// <summary>
        ///     Setup a BCrypt algorithm with our current parameters
        /// </summary>
        [SecurityCritical]
        [SecurityTreatAsSafe]
        private SafeBCryptAlgorithmHandle SetupAlgorithm()
        {
            SafeBCryptAlgorithmHandle algorithmHandle = BCryptNative.OpenAlgorithm(m_algorithm.Algorithm, m_implementation);

            // If we've selected a different block size than the default, set that now
            if (BlockSize / 8 != BCryptNative.GetInt32Property(algorithmHandle, BCryptNative.ObjectPropertyName.BlockLength))
            {
                BCryptNative.SetInt32Property(algorithmHandle, BCryptNative.ObjectPropertyName.BlockLength, BlockSize / 8);
            }

            BCryptNative.SetStringProperty(algorithmHandle, BCryptNative.ObjectPropertyName.ChainingMode, BCryptNative.MapChainingMode(Mode));

            return algorithmHandle;
        }

        //
        // SymmetricAlgorithm abstract method implementations
        //

        [SecurityCritical]
        [SecurityTreatAsSafe]
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");

            return new BCryptSymmetricCryptoTransform(SetupAlgorithm(), rgbKey, rgbIV, Padding, false);
        }

        [SecurityCritical]
        [SecurityTreatAsSafe]
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");

            return new BCryptSymmetricCryptoTransform(SetupAlgorithm(), rgbKey, rgbIV, Padding, true);
        }

        public override void GenerateIV()
        {
            IVValue = new byte[BlockSizeValue / 8];
            RNGCng.StaticRng.GetBytes(IVValue);
        }

        public override void GenerateKey()
        {
            KeyValue = new byte[KeySizeValue / 8];
            RNGCng.StaticRng.GetBytes(KeyValue);
        }
    }
}