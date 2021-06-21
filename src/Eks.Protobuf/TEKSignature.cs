// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace NL.Rijksoverheid.ExposureNotification.BackEnd.Eks.Protobuf
{
    public sealed partial class TEKSignature : IMessage<TEKSignature>
    {
        private static readonly MessageParser<TEKSignature> parser = new MessageParser<TEKSignature>(() => new TEKSignature());
        private UnknownFieldSet _unknownFields;
        private int _hasBits0;
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static MessageParser<TEKSignature> Parser { get { return parser; } }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static MessageDescriptor Descriptor
        {
            get { return TemporaryExposureKeyExportReflection.Descriptor.MessageTypes[4]; }
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        MessageDescriptor IMessage.Descriptor
        {
            get { return Descriptor; }
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public TEKSignature()
        {
            OnConstruction();
        }

        partial void OnConstruction();

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public TEKSignature(TEKSignature other) : this()
        {
            _hasBits0 = other._hasBits0;
            _signatureInfo = other._signatureInfo != null ? other._signatureInfo.Clone() : null;
            _batchNum = other._batchNum;
            _batchSize = other._batchSize;
            _signature = other._signature;
            _unknownFields = UnknownFieldSet.Clone(other._unknownFields);
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public TEKSignature Clone()
        {
            return new TEKSignature(this);
        }

        /// <summary>Field number for the "signature_info" field.</summary>
        public const int SignatureInfoFieldNumber = 1;
        private SignatureInfo _signatureInfo;
        /// <summary>
        /// Info about the signing key, version, algorithm, and so on.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public SignatureInfo SignatureInfo
        {
            get { return _signatureInfo; }
            set
            {
                _signatureInfo = value;
            }
        }

        /// <summary>Field number for the "batch_num" field.</summary>
        public const int BatchNumFieldNumber = 2;
        private static readonly int batchNumDefaultValue = 0;

        private int _batchNum;
        /// <summary>
        /// For example, file 2 in batch size of 10. Ordinal, 1-based numbering.
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int BatchNum
        {
            get { if ((_hasBits0 & 1) != 0) { return _batchNum; } else { return batchNumDefaultValue; } }
            set
            {
                _hasBits0 |= 1;
                _batchNum = value;
            }
        }
        /// <summary>Gets whether the "batch_num" field is set</summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool HasBatchNum
        {
            get { return (_hasBits0 & 1) != 0; }
        }
        /// <summary>Clears the value of the "batch_num" field</summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void ClearBatchNum()
        {
            _hasBits0 &= ~1;
        }

        /// <summary>Field number for the "batch_size" field.</summary>
        public const int BatchSizeFieldNumber = 3;
        private static readonly int batchSizeDefaultValue = 0;

        private int _batchSize;
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int BatchSize
        {
            get { if ((_hasBits0 & 2) != 0) { return _batchSize; } else { return batchSizeDefaultValue; } }
            set
            {
                _hasBits0 |= 2;
                _batchSize = value;
            }
        }
        /// <summary>Gets whether the "batch_size" field is set</summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool HasBatchSize
        {
            get { return (_hasBits0 & 2) != 0; }
        }
        /// <summary>Clears the value of the "batch_size" field</summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void ClearBatchSize()
        {
            _hasBits0 &= ~2;
        }

        /// <summary>Field number for the "signature" field.</summary>
        public const int SignatureFieldNumber = 4;
        private static readonly ByteString signatureDefaultValue = ByteString.Empty;

        private ByteString _signature;
        /// <summary>
        /// Signature in X9.62 format (ASN.1 SEQUENCE of two INTEGER fields)
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public ByteString Signature
        {
            get { return _signature ?? signatureDefaultValue; }
            set
            {
                _signature = ProtoPreconditions.CheckNotNull(value, "value");
            }
        }
        /// <summary>Gets whether the "signature" field is set</summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool HasSignature
        {
            get { return _signature != null; }
        }
        /// <summary>Clears the value of the "signature" field</summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void ClearSignature()
        {
            _signature = null;
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other)
        {
            return Equals(other as TEKSignature);
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(TEKSignature other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (!Equals(SignatureInfo, other.SignatureInfo))
            {
                return false;
            }

            if (BatchNum != other.BatchNum)
            {
                return false;
            }

            if (BatchSize != other.BatchSize)
            {
                return false;
            }

            if (Signature != other.Signature)
            {
                return false;
            }

            return Equals(_unknownFields, other._unknownFields);
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            var hash = 1;
            if (_signatureInfo != null)
            {
                hash ^= SignatureInfo.GetHashCode();
            }

            if (HasBatchNum)
            {
                hash ^= BatchNum.GetHashCode();
            }

            if (HasBatchSize)
            {
                hash ^= BatchSize.GetHashCode();
            }

            if (HasSignature)
            {
                hash ^= Signature.GetHashCode();
            }

            if (_unknownFields != null)
            {
                hash ^= _unknownFields.GetHashCode();
            }
            return hash;
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override string ToString()
        {
            return JsonFormatter.ToDiagnosticString(this);
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(CodedOutputStream output)
        {
            if (_signatureInfo != null)
            {
                output.WriteRawTag(10);
                output.WriteMessage(SignatureInfo);
            }
            if (HasBatchNum)
            {
                output.WriteRawTag(16);
                output.WriteInt32(BatchNum);
            }
            if (HasBatchSize)
            {
                output.WriteRawTag(24);
                output.WriteInt32(BatchSize);
            }
            if (HasSignature)
            {
                output.WriteRawTag(34);
                output.WriteBytes(Signature);
            }
            if (_unknownFields != null)
            {
                _unknownFields.WriteTo(output);
            }
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            var size = 0;
            if (_signatureInfo != null)
            {
                size += 1 + CodedOutputStream.ComputeMessageSize(SignatureInfo);
            }
            if (HasBatchNum)
            {
                size += 1 + CodedOutputStream.ComputeInt32Size(BatchNum);
            }
            if (HasBatchSize)
            {
                size += 1 + CodedOutputStream.ComputeInt32Size(BatchSize);
            }
            if (HasSignature)
            {
                size += 1 + CodedOutputStream.ComputeBytesSize(Signature);
            }
            if (_unknownFields != null)
            {
                size += _unknownFields.CalculateSize();
            }
            return size;
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(TEKSignature other)
        {
            if (other == null)
            {
                return;
            }
            if (other._signatureInfo != null)
            {
                if (_signatureInfo == null)
                {
                    SignatureInfo = new SignatureInfo();
                }
                SignatureInfo.MergeFrom(other.SignatureInfo);
            }
            if (other.HasBatchNum)
            {
                BatchNum = other.BatchNum;
            }
            if (other.HasBatchSize)
            {
                BatchSize = other.BatchSize;
            }
            if (other.HasSignature)
            {
                Signature = other.Signature;
            }
            _unknownFields = UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }

        [System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(CodedInputStream input)
        {
            uint tag;
            while ((tag = input.ReadTag()) != 0)
            {
                switch (tag)
                {
                    default:
                        _unknownFields = UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
                        break;
                    case 10:
                        {
                            if (_signatureInfo == null)
                            {
                                SignatureInfo = new SignatureInfo();
                            }
                            input.ReadMessage(SignatureInfo);
                            break;
                        }
                    case 16:
                        {
                            BatchNum = input.ReadInt32();
                            break;
                        }
                    case 24:
                        {
                            BatchSize = input.ReadInt32();
                            break;
                        }
                    case 34:
                        {
                            Signature = input.ReadBytes();
                            break;
                        }
                }
            }
        }

    }
}
