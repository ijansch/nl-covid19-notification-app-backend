// Copyright 2020 De Staat der Nederlanden, Ministerie van Volksgezondheid, Welzijn en Sport.
// Licensed under the EUROPEAN UNION PUBLIC LICENCE v. 1.2
// SPDX-License-Identifier: EUPL-1.2

using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.Reflection;

namespace Iks.Protobuf
{
    public sealed partial class DiagnosisKey : IMessage<DiagnosisKey>
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      , pb::IBufferMessage
#endif
    {
        private static readonly MessageParser<DiagnosisKey> parser = new MessageParser<DiagnosisKey>(() => new DiagnosisKey());
        private UnknownFieldSet _unknownFields;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static MessageParser<DiagnosisKey> Parser { get { return parser; } }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public static MessageDescriptor Descriptor
        {
            get { return global::Eu.Interop.EfgsReflection.Descriptor.MessageTypes[1]; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        MessageDescriptor IMessage.Descriptor
        {
            get { return Descriptor; }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public DiagnosisKey()
        {
            OnConstruction();
        }

        partial void OnConstruction();

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public DiagnosisKey(DiagnosisKey other) : this()
        {
            _keyData = other._keyData;
            _rollingStartIntervalNumber = other._rollingStartIntervalNumber;
            _rollingPeriod = other._rollingPeriod;
            _transmissionRiskLevel = other._transmissionRiskLevel;
            _visitedCountries = other._visitedCountries.Clone();
            _origin = other._origin;
            _reportType = other._reportType;
            _daysSinceOnsetOfSymptoms = other._daysSinceOnsetOfSymptoms;
            _unknownFields = UnknownFieldSet.Clone(other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public DiagnosisKey Clone()
        {
            return new DiagnosisKey(this);
        }

        /// <summary>Field number for the "keyData" field.</summary>
        public const int KeyDataFieldNumber = 1;
        private ByteString _keyData = ByteString.Empty;
        /// <summary>
        /// key
        /// </summary>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public ByteString KeyData
        {
            get { return _keyData; }
            set
            {
                _keyData = ProtoPreconditions.CheckNotNull(value, "value");
            }
        }

        /// <summary>Field number for the "rollingStartIntervalNumber" field.</summary>
        public const int RollingStartIntervalNumberFieldNumber = 2;
        private uint _rollingStartIntervalNumber;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public uint RollingStartIntervalNumber
        {
            get { return _rollingStartIntervalNumber; }
            set
            {
                _rollingStartIntervalNumber = value;
            }
        }

        /// <summary>Field number for the "rollingPeriod" field.</summary>
        public const int RollingPeriodFieldNumber = 3;
        private uint _rollingPeriod;
        /// <summary>
        /// number of 10-minute windows between key-rolling
        /// </summary>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public uint RollingPeriod
        {
            get { return _rollingPeriod; }
            set
            {
                _rollingPeriod = value;
            }
        }

        /// <summary>Field number for the "transmissionRiskLevel" field.</summary>
        public const int TransmissionRiskLevelFieldNumber = 4;
        private int _transmissionRiskLevel;
        /// <summary>
        /// risk of transmission
        /// </summary>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int TransmissionRiskLevel
        {
            get { return _transmissionRiskLevel; }
            set
            {
                _transmissionRiskLevel = value;
            }
        }

        /// <summary>Field number for the "visitedCountries" field.</summary>
        public const int VisitedCountriesFieldNumber = 5;
        private static readonly FieldCodec<string> repeatedVisitedCountriesCodec
            = FieldCodec.ForString(42);
        private readonly RepeatedField<string> _visitedCountries = new RepeatedField<string>();
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public RepeatedField<string> VisitedCountries
        {
            get { return _visitedCountries; }
        }

        /// <summary>Field number for the "origin" field.</summary>
        public const int OriginFieldNumber = 6;
        private string _origin = "";
        /// <summary>
        /// country of origin
        /// </summary>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public string Origin
        {
            get { return _origin; }
            set
            {
                _origin = ProtoPreconditions.CheckNotNull(value, "value");
            }
        }

        /// <summary>Field number for the "reportType" field.</summary>
        public const int ReportTypeFieldNumber = 7;
        private global::Iks.Protobuf.EfgsReportType _reportType = global::Iks.Protobuf.EfgsReportType.Unknown;
        /// <summary>
        /// set by backend
        /// </summary>
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public global::Iks.Protobuf.EfgsReportType ReportType
        {
            get { return _reportType; }
            set
            {
                _reportType = value;
            }
        }

        /// <summary>Field number for the "days_since_onset_of_symptoms" field.</summary>
        public const int DaysSinceOnsetOfSymptomsFieldNumber = 8;
        private int _daysSinceOnsetOfSymptoms;
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int DaysSinceOnsetOfSymptoms
        {
            get { return _daysSinceOnsetOfSymptoms; }
            set
            {
                _daysSinceOnsetOfSymptoms = value;
            }
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override bool Equals(object other)
        {
            return Equals(other as DiagnosisKey);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public bool Equals(DiagnosisKey other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(other, this))
            {
                return true;
            }
            if (KeyData != other.KeyData)
            {
                return false;
            }

            if (RollingStartIntervalNumber != other.RollingStartIntervalNumber)
            {
                return false;
            }

            if (RollingPeriod != other.RollingPeriod)
            {
                return false;
            }

            if (TransmissionRiskLevel != other.TransmissionRiskLevel)
            {
                return false;
            }

            if (!_visitedCountries.Equals(other._visitedCountries))
            {
                return false;
            }

            if (Origin != other.Origin)
            {
                return false;
            }

            if (ReportType != other.ReportType)
            {
                return false;
            }

            if (DaysSinceOnsetOfSymptoms != other.DaysSinceOnsetOfSymptoms)
            {
                return false;
            }

            return Equals(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override int GetHashCode()
        {
            var hash = 1;
            if (KeyData.Length != 0)
            {
                hash ^= KeyData.GetHashCode();
            }

            if (RollingStartIntervalNumber != 0)
            {
                hash ^= RollingStartIntervalNumber.GetHashCode();
            }

            if (RollingPeriod != 0)
            {
                hash ^= RollingPeriod.GetHashCode();
            }

            if (TransmissionRiskLevel != 0)
            {
                hash ^= TransmissionRiskLevel.GetHashCode();
            }

            hash ^= _visitedCountries.GetHashCode();
            if (Origin.Length != 0)
            {
                hash ^= Origin.GetHashCode();
            }

            if (ReportType != global::Iks.Protobuf.EfgsReportType.Unknown)
            {
                hash ^= ReportType.GetHashCode();
            }

            if (DaysSinceOnsetOfSymptoms != 0)
            {
                hash ^= DaysSinceOnsetOfSymptoms.GetHashCode();
            }

            if (_unknownFields != null)
            {
                hash ^= _unknownFields.GetHashCode();
            }
            return hash;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public override string ToString()
        {
            return JsonFormatter.ToDiagnosticString(this);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void WriteTo(CodedOutputStream output)
        {
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      output.WriteRawMessage(this);
#else
            if (KeyData.Length != 0)
            {
                output.WriteRawTag(10);
                output.WriteBytes(KeyData);
            }
            if (RollingStartIntervalNumber != 0)
            {
                output.WriteRawTag(16);
                output.WriteUInt32(RollingStartIntervalNumber);
            }
            if (RollingPeriod != 0)
            {
                output.WriteRawTag(24);
                output.WriteUInt32(RollingPeriod);
            }
            if (TransmissionRiskLevel != 0)
            {
                output.WriteRawTag(32);
                output.WriteInt32(TransmissionRiskLevel);
            }
            _visitedCountries.WriteTo(output, repeatedVisitedCountriesCodec);
            if (Origin.Length != 0)
            {
                output.WriteRawTag(50);
                output.WriteString(Origin);
            }
            if (ReportType != global::Iks.Protobuf.EfgsReportType.Unknown)
            {
                output.WriteRawTag(56);
                output.WriteEnum((int)ReportType);
            }
            if (DaysSinceOnsetOfSymptoms != 0)
            {
                output.WriteRawTag(64);
                output.WriteSInt32(DaysSinceOnsetOfSymptoms);
            }
            if (_unknownFields != null)
            {
                _unknownFields.WriteTo(output);
            }
#endif
        }

#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalWriteTo(ref pb::WriteContext output) {
      if (KeyData.Length != 0) {
        output.WriteRawTag(10);
        output.WriteBytes(KeyData);
      }
      if (RollingStartIntervalNumber != 0) {
        output.WriteRawTag(16);
        output.WriteUInt32(RollingStartIntervalNumber);
      }
      if (RollingPeriod != 0) {
        output.WriteRawTag(24);
        output.WriteUInt32(RollingPeriod);
      }
      if (TransmissionRiskLevel != 0) {
        output.WriteRawTag(32);
        output.WriteInt32(TransmissionRiskLevel);
      }
      visitedCountries_.WriteTo(ref output, _repeated_visitedCountries_codec);
      if (Origin.Length != 0) {
        output.WriteRawTag(50);
        output.WriteString(Origin);
      }
      if (ReportType != global::Eu.Interop.ReportType.Unknown) {
        output.WriteRawTag(56);
        output.WriteEnum((int) ReportType);
      }
      if (DaysSinceOnsetOfSymptoms != 0) {
        output.WriteRawTag(64);
        output.WriteSInt32(DaysSinceOnsetOfSymptoms);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(ref output);
      }
    }
#endif

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public int CalculateSize()
        {
            var size = 0;
            if (KeyData.Length != 0)
            {
                size += 1 + CodedOutputStream.ComputeBytesSize(KeyData);
            }
            if (RollingStartIntervalNumber != 0)
            {
                size += 1 + CodedOutputStream.ComputeUInt32Size(RollingStartIntervalNumber);
            }
            if (RollingPeriod != 0)
            {
                size += 1 + CodedOutputStream.ComputeUInt32Size(RollingPeriod);
            }
            if (TransmissionRiskLevel != 0)
            {
                size += 1 + CodedOutputStream.ComputeInt32Size(TransmissionRiskLevel);
            }
            size += _visitedCountries.CalculateSize(repeatedVisitedCountriesCodec);
            if (Origin.Length != 0)
            {
                size += 1 + CodedOutputStream.ComputeStringSize(Origin);
            }
            if (ReportType != global::Iks.Protobuf.EfgsReportType.Unknown)
            {
                size += 1 + CodedOutputStream.ComputeEnumSize((int)ReportType);
            }
            if (DaysSinceOnsetOfSymptoms != 0)
            {
                size += 1 + CodedOutputStream.ComputeSInt32Size(DaysSinceOnsetOfSymptoms);
            }
            if (_unknownFields != null)
            {
                size += _unknownFields.CalculateSize();
            }
            return size;
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(DiagnosisKey other)
        {
            if (other == null)
            {
                return;
            }
            if (other.KeyData.Length != 0)
            {
                KeyData = other.KeyData;
            }
            if (other.RollingStartIntervalNumber != 0)
            {
                RollingStartIntervalNumber = other.RollingStartIntervalNumber;
            }
            if (other.RollingPeriod != 0)
            {
                RollingPeriod = other.RollingPeriod;
            }
            if (other.TransmissionRiskLevel != 0)
            {
                TransmissionRiskLevel = other.TransmissionRiskLevel;
            }
            _visitedCountries.Add(other._visitedCountries);
            if (other.Origin.Length != 0)
            {
                Origin = other.Origin;
            }
            if (other.ReportType != global::Iks.Protobuf.EfgsReportType.Unknown)
            {
                ReportType = other.ReportType;
            }
            if (other.DaysSinceOnsetOfSymptoms != 0)
            {
                DaysSinceOnsetOfSymptoms = other.DaysSinceOnsetOfSymptoms;
            }
            _unknownFields = UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
        }

        [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
        public void MergeFrom(CodedInputStream input)
        {
#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
      input.ReadRawMessage(this);
#else
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
                            KeyData = input.ReadBytes();
                            break;
                        }
                    case 16:
                        {
                            RollingStartIntervalNumber = input.ReadUInt32();
                            break;
                        }
                    case 24:
                        {
                            RollingPeriod = input.ReadUInt32();
                            break;
                        }
                    case 32:
                        {
                            TransmissionRiskLevel = input.ReadInt32();
                            break;
                        }
                    case 42:
                        {
                            _visitedCountries.AddEntriesFrom(input, repeatedVisitedCountriesCodec);
                            break;
                        }
                    case 50:
                        {
                            Origin = input.ReadString();
                            break;
                        }
                    case 56:
                        {
                            ReportType = (global::Iks.Protobuf.EfgsReportType)input.ReadEnum();
                            break;
                        }
                    case 64:
                        {
                            DaysSinceOnsetOfSymptoms = input.ReadSInt32();
                            break;
                        }
                }
            }
#endif
        }

#if !GOOGLE_PROTOBUF_REFSTRUCT_COMPATIBILITY_MODE
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    void pb::IBufferMessage.InternalMergeFrom(ref pb::ParseContext input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, ref input);
            break;
          case 10: {
            KeyData = input.ReadBytes();
            break;
          }
          case 16: {
            RollingStartIntervalNumber = input.ReadUInt32();
            break;
          }
          case 24: {
            RollingPeriod = input.ReadUInt32();
            break;
          }
          case 32: {
            TransmissionRiskLevel = input.ReadInt32();
            break;
          }
          case 42: {
            visitedCountries_.AddEntriesFrom(ref input, _repeated_visitedCountries_codec);
            break;
          }
          case 50: {
            Origin = input.ReadString();
            break;
          }
          case 56: {
            ReportType = (global::Eu.Interop.ReportType) input.ReadEnum();
            break;
          }
          case 64: {
            DaysSinceOnsetOfSymptoms = input.ReadSInt32();
            break;
          }
        }
      }
    }
#endif

    }
}
