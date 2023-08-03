using Microsoft.Extensions.Logging;
using Portalum.Zvt.Helpers;
using Portalum.Zvt.Models;
using Portalum.Zvt.Responses;
using System;
using System.Collections.Generic;

namespace Portalum.Zvt.Parsers
{
    /// <summary>
    /// TlvParser
    /// </summary>
    public class TlvParser : ITlvParser
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, TlvInfo> _tlvInfos;

        /// <summary>
        /// TlvParser
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="tlvInfos"></param>
        public TlvParser(
            ILogger logger,
            TlvInfo[] tlvInfos = default)
        {
            this._logger = logger;

            this._tlvInfos = new Dictionary<string, TlvInfo>();
            if (tlvInfos != default)
            {
                foreach (var tlvInfo in tlvInfos)
                {
                    if (this._tlvInfos.ContainsKey(tlvInfo.Tag))
                    {
                        throw new NotSupportedException($"Cannot add tlvInfo {tlvInfo.Tag} (duplicate key)");
                    }

                    this._tlvInfos.Add(tlvInfo.Tag, tlvInfo);
                }
            }
        }

        /// <summary>
        /// Parse a TLV container
        /// </summary>
        /// <param name="data"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public bool Parse(byte[] data, IResponse response)
        {
            var lengthInfo = this.GetLength(data);
            if (!lengthInfo.Successful)
            {
                this._logger.LogError($"{nameof(Parse)} - Cannot detect length of TLV Container");
                return false;
            }

            var tlvData = data.AsSpan().Slice(lengthInfo.NumberOfBytesThatCanBeSkipped, lengthInfo.Length);

            return this.ParseInternal(tlvData, response);
        }

        private bool ParseInternal(Span<byte> data, IResponse response)
        {
            while (data.Length > 0)
            {
                var tagFieldInfo = this.GetTagFieldInfo(data);
                data = data.Slice(tagFieldInfo.NumberOfBytesThatCanBeSkipped);

                var tlvLengthInfo = this.GetLength(data);
                data = data.Slice(tlvLengthInfo.NumberOfBytesThatCanBeSkipped);
                if (!tlvLengthInfo.Successful)
                {
                    this._logger.LogError($"{nameof(Parse)} - Cannot detect the tlv length");
                    return false;
                }

                switch (tagFieldInfo.DataObjectType)
                {
                    case TlvTagFieldDataObjectType.Primitive:

                        var tlvPrimitiveDataPart = data.Slice(0, tlvLengthInfo.Length);
                        if (!this.ProcessTlvInfoAction(tagFieldInfo.Tag, tlvPrimitiveDataPart, response))
                        {
                            this._logger.LogInformation($"{nameof(ParseInternal)} - Cannot found a process action for tag:{tagFieldInfo.Tag} {tagFieldInfo.DataObjectType}");
                        }

                        data = data.Slice(tlvLengthInfo.Length);
                        break;

                    case TlvTagFieldDataObjectType.Constructed:
                        if (data.Length < tlvLengthInfo.Length)
                        {
                            this._logger.LogError($"{nameof(ParseInternal)} - Corrupt data package for tag:{tagFieldInfo.Tag} {tagFieldInfo.DataObjectType}");
                            return false;
                        }

                        var tlvConstructedDataPart = data.Slice(0, tlvLengthInfo.Length);
                        if (!this.ProcessTlvInfoAction(tagFieldInfo.Tag, tlvConstructedDataPart, response))
                        {
                            this._logger.LogInformation($"{nameof(ParseInternal)} - Cannot found a process action for tag:{tagFieldInfo.Tag} {tagFieldInfo.DataObjectType}");
                        }

                        data = data.Slice(tlvLengthInfo.Length);

                        if (!this.ParseInternal(tlvConstructedDataPart, response))
                        {
                            return false;
                        }

                        break;

                    default:
                        return false;
                }
            }

            return true;
        }

        private bool ProcessTlvInfoAction(string tag, Span<byte> data, IResponse response)
        {
            if (this._tlvInfos.TryGetValue(tag, out var tlvInfo))
            {
                if (tlvInfo.TryProcess == null)
                {
                    this._logger.LogDebug($"{nameof(ProcessTlvInfoAction)} - No action defined for Tag:{tag}");
                    return true;
                }

                tlvInfo.TryProcess(data.ToArray(), response);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get Tag Field Info
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public TlvTagFieldInfo GetTagFieldInfo(Span<byte> data)
        {
            if (data == null || data.Length == 0)
            {
                return null;
            }

            var tagFieldInfo = new TlvTagFieldInfo();
            var isFirstByte = true;

            foreach (var b in data)
            {
                var bits = BitHelper.GetBits(b);

                if (isFirstByte)
                {
                    isFirstByte = false;

                    if (bits.Bit7 && bits.Bit6)
                    {
                        tagFieldInfo.ClassType = TlvTagFieldClassType.PrivateClass;
                    }
                    else if (bits.Bit7 && !bits.Bit6)
                    {
                        tagFieldInfo.ClassType = TlvTagFieldClassType.ContextSpecificClass;
                    }
                    else if (!bits.Bit7 && bits.Bit6)
                    {
                        tagFieldInfo.ClassType = TlvTagFieldClassType.ApplicationClass;
                    }
                    else if (!bits.Bit7 && !bits.Bit6)
                    {
                        tagFieldInfo.ClassType = TlvTagFieldClassType.UniversalClass;
                    }

                    tagFieldInfo.DataObjectType = bits.Bit5 ? TlvTagFieldDataObjectType.Constructed : TlvTagFieldDataObjectType.Primitive;

                    if (bits.Bit4 && bits.Bit3 && bits.Bit2 && bits.Bit1 && bits.Bit0)
                    {
                        tagFieldInfo.Tag += $"{b:X2}";
                        tagFieldInfo.NumberOfBytesThatCanBeSkipped++;
                        continue;
                    }

                    tagFieldInfo.TagNumber += NumberHelper.BoolArrayToInt(bits.Bit0, bits.Bit1, bits.Bit2, bits.Bit3);
                    tagFieldInfo.Tag += $"{b:X2}";
                    tagFieldInfo.NumberOfBytesThatCanBeSkipped++;
                    break;
                }

                if (bits.Bit7) //Additional byte must be analyzed
                {
                    tagFieldInfo.TagNumber += NumberHelper.BoolArrayToInt(bits.Bit0, bits.Bit1, bits.Bit2, bits.Bit3, bits.Bit4, bits.Bit5, bits.Bit6);
                    tagFieldInfo.Tag += $"{b:X2}";
                    tagFieldInfo.NumberOfBytesThatCanBeSkipped++;
                    continue;
                }
                else //Last byte
                {
                    tagFieldInfo.TagNumber += NumberHelper.BoolArrayToInt(bits.Bit0, bits.Bit1, bits.Bit2, bits.Bit3, bits.Bit4, bits.Bit5, bits.Bit6);
                    tagFieldInfo.Tag += $"{b:X2}";
                    tagFieldInfo.NumberOfBytesThatCanBeSkipped++;
                    break;
                }
            }

            return tagFieldInfo;
        }

        /// <summary>
        /// Get the data length of a TLV container
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public TlvLengthInfo GetLength(Span<byte> data)
        {
            if (data == null || data.Length == 0)
            {
                return new TlvLengthInfo { Successful = false };
            }

            var bits = BitHelper.GetBits(data[0]);

            if (!bits.Bit7)
            {
                var length = NumberHelper.BoolArrayToInt(bits.Bit0, bits.Bit1, bits.Bit2, bits.Bit3, bits.Bit4, bits.Bit5, bits.Bit6);
                return new TlvLengthInfo { Successful = true, Length = length, NumberOfBytesThatCanBeSkipped = 1 };
            }

            if (!bits.Bit0 && !bits.Bit1 && !bits.Bit2 && !bits.Bit3 && !bits.Bit4 && !bits.Bit5 && !bits.Bit6 && bits.Bit7)
            {
                this._logger.LogInformation($"{nameof(GetLength)} - Invalid value");
                return new TlvLengthInfo { Successful = false };
            }

            if (bits.Bit0 && !bits.Bit1 && !bits.Bit2 && !bits.Bit3 && !bits.Bit4 && !bits.Bit5 && !bits.Bit6 && bits.Bit7)
            {
                if (data.Length < 2)
                {
                    this._logger.LogWarning($"{nameof(GetLength)} - Not enough bytes available");
                    return new TlvLengthInfo { Successful = false };
                }

                return new TlvLengthInfo { Successful = true, Length = data[1], NumberOfBytesThatCanBeSkipped = 2 };
            }

            if (!bits.Bit0 && bits.Bit1 && !bits.Bit2 && !bits.Bit3 && !bits.Bit4 && !bits.Bit5 && !bits.Bit6 && bits.Bit7)
            {
                if (data.Length < 3)
                {
                    this._logger.LogWarning($"{nameof(GetLength)} - Not enough bytes available");
                    return new TlvLengthInfo { Successful = false };
                }

                var length = NumberHelper.ToInt16LittleEndian(data.Slice(1, 2));
                if (length < 0)
                {
                    this._logger.LogWarning($"{nameof(GetLength)} - Negative length detected {length}");
                    return new TlvLengthInfo { Successful = false };
                }

                return new TlvLengthInfo { Successful = true, Length = length, NumberOfBytesThatCanBeSkipped = 3 };
            }

            this._logger.LogWarning($"{nameof(GetLength)} - RFU - Reserved for future use");
            return new TlvLengthInfo { Successful = false };
        }
    }
}
