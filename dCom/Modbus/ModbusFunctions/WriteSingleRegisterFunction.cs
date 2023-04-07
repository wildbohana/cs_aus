﻿using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write single register functions/requests.
    /// </summary>
    public class WriteSingleRegisterFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleRegisterFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleRegisterFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            byte[] zahtev = new byte[12];

            // CommandParameters - TransactionId, ProtocolId, Length, UnitId, FunctionCode
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.TransactionId)), 0, zahtev, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.ProtocolId)), 0, zahtev, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.Length)), 0, zahtev, 4, 2);
            zahtev[6] = CommandParameters.UnitId;
            zahtev[7] = CommandParameters.FunctionCode;

            // ModbusReadCommandParameters - StartAddress, Quantity
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)((ModbusWriteCommandParameters)CommandParameters).OutputAddress)), 0, zahtev, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)((ModbusWriteCommandParameters)CommandParameters).Value)), 0, zahtev, 10, 2);

            return zahtev;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            var povratna = new Dictionary<Tuple<PointType, ushort>, ushort>();

            // response[7] - FunctionCode (0x06 + 0x80 == Error)
            // response[8] - OutputAddress (ExceptionCode ako je došlo do greške)
            if (response[7] == CommandParameters.FunctionCode + 0x80)
            {
                HandeException(response[8]);
            }
            else
            {
                var adresa = BitConverter.ToUInt16(response, 8);
                var vrednost = BitConverter.ToUInt16(response, 10);

                adresa = (ushort)IPAddress.NetworkToHostOrder((short)adresa);
                vrednost = (ushort)IPAddress.NetworkToHostOrder((short)vrednost);

                Tuple<PointType, ushort> kljuc = new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, adresa);
                povratna.Add(kljuc, vrednost);
            }

            return povratna;
        }
    }
}
