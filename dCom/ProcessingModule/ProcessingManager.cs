using Common;
using Modbus;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for processing points and executing commands.
    /// </summary>
    public class ProcessingManager : IProcessingManager
    {
        private IFunctionExecutor functionExecutor;
        private IStorage storage;
        private AlarmProcessor alarmProcessor;
        private EGUConverter eguConverter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingManager"/> class.
        /// </summary>
        /// <param name="storage">The point storage.</param>
        /// <param name="functionExecutor">The function executor.</param>
        public ProcessingManager(IStorage storage, IFunctionExecutor functionExecutor)
        {
            this.storage = storage;
            this.functionExecutor = functionExecutor;
            this.alarmProcessor = new AlarmProcessor();
            this.eguConverter = new EGUConverter();
            this.functionExecutor.UpdatePointEvent += CommandExecutor_UpdatePointEvent;
        }

        /// <inheritdoc />
        public void ExecuteReadCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort startAddress, ushort numberOfPoints)
        {
            ModbusReadCommandParameters p = new ModbusReadCommandParameters(6, (byte)GetReadFunctionCode(configItem.RegistryType), startAddress, numberOfPoints, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }
        
        /// <inheritdoc />
        // TODO zabrani menjanje stanja ventila ako je neki od njih uključen
        public void ExecuteWriteCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            // Verovatno trebam da izbacim ovo troje od gore
            PointIdentifier start = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3000);
            PointIdentifier mesalica = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001);     // 1
            PointIdentifier ventil1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4000);      // 2
            PointIdentifier ventil2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4001);      // 3
            PointIdentifier ventil3 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4002);      // 4
            PointIdentifier ventil4 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4003);      // 5

            List<PointIdentifier> lista = new List<PointIdentifier> { start, mesalica, ventil1, ventil2, ventil3, ventil4 };
            List<IPoint> tacke = storage.GetPoints(lista);

            // TODO popravi ovo, sve osim ovoga mi je dobro
            /*
            // Spreči istovremeno pražnjenje i punjenje mešalice
            if ((tacke[2].RawValue == 1 || tacke[3].RawValue == 1 || tacke[4].RawValue == 1) && value == 1 && pointAddress == 4003)
            {
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, tacke[5].ConfigItem.StartAddress, 0);       // izlaz - zatvori
                return;
            }
            if (tacke[5].RawValue == 1 && value == 1 && (pointAddress == 4000 || pointAddress == 4001 || pointAddress == 4002))
            {
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, pointAddress, 0);       // ulaz - zatvori
                return;
            }

            // U fazi punjenja mešalice, motor za mešanje ne sme da bude uključen
            if (tacke[0].RawValue == 1 && (tacke[2].RawValue == 1 || tacke[3].RawValue == 1 || tacke[4].RawValue == 1) && value == 1 && pointAddress == 3001)
            {
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, tacke[1].ConfigItem.StartAddress, 0);       // mešalica - ugasi
                return;
            }

            // Ukoliko se bilo koji od ventila otvori u fazi mešanja, potrebno je ugasiti mešalicu, zatvoriti sve ventile, i isprazniti mešalicu
            if (tacke[0].RawValue == 1 && tacke[1].RawValue == 1 && (pointAddress == 4000 || pointAddress == 4001 || pointAddress == 4002) && value == 1)
            {
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, tacke[1].ConfigItem.StartAddress, 0);       // mešalica - ugasi
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, tacke[2].ConfigItem.StartAddress, 0);       // ventil1 - zatvori
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, tacke[3].ConfigItem.StartAddress, 0);       // ventil2 - zatvori
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, tacke[4].ConfigItem.StartAddress, 0);       // ventil3 - zatvori
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, tacke[5].ConfigItem.StartAddress, 1);       // ventil4 - otvori
                return;
            }
            */

            // Već postojalo, ovo iznad je dodato
            if (configItem.RegistryType == PointType.ANALOG_OUTPUT)
            {
                ExecuteAnalogCommand(configItem, transactionId, remoteUnitAddress, pointAddress, value);
            }
            else
            {
                ExecuteDigitalCommand(configItem, transactionId, remoteUnitAddress, pointAddress, value);
            }
        }

        /// <summary>
        /// Executes a digital write command.
        /// </summary>
        /// <param name="configItem">The configuration item.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="remoteUnitAddress">The remote unit address.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="value">The value.</param>
        private void ExecuteDigitalCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(6, (byte)ModbusFunctionCode.WRITE_SINGLE_COIL, pointAddress, (ushort)value, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        /// <summary>
        /// Executes an analog write command.
        /// </summary>
        /// <param name="configItem">The configuration item.</param>
        /// <param name="transactionId">The transaction identifier.</param>
        /// <param name="remoteUnitAddress">The remote unit address.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="value">The value.</param>
        // TODO izmeni da vraća raw value
        private void ExecuteAnalogCommand(IConfigItem configItem, ushort transactionId, byte remoteUnitAddress, ushort pointAddress, int value)
        {
            value = (int)eguConverter.ConvertToRaw(configItem.ScaleFactor, configItem.Deviation, value);

            ModbusWriteCommandParameters p = new ModbusWriteCommandParameters(6, (byte)ModbusFunctionCode.WRITE_SINGLE_REGISTER, pointAddress, (ushort)value, transactionId, remoteUnitAddress);
            IModbusFunction fn = FunctionFactory.CreateModbusFunction(p);
            this.functionExecutor.EnqueueCommand(fn);
        }

        /// <summary>
        /// Gets the modbus function code for the point type.
        /// </summary>
        /// <param name="registryType">The register type.</param>
        /// <returns>The modbus function code.</returns>
        private ModbusFunctionCode? GetReadFunctionCode(PointType registryType)
        {
            switch (registryType)
            {
                case PointType.DIGITAL_OUTPUT: return ModbusFunctionCode.READ_COILS;
                case PointType.DIGITAL_INPUT: return ModbusFunctionCode.READ_DISCRETE_INPUTS;
                case PointType.ANALOG_INPUT: return ModbusFunctionCode.READ_INPUT_REGISTERS;
                case PointType.ANALOG_OUTPUT: return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                case PointType.HR_LONG: return ModbusFunctionCode.READ_HOLDING_REGISTERS;
                default: return null;
            }
        }

        /// <summary>
        /// Method for handling received points.
        /// </summary>
        /// <param name="type">The point type.</param>
        /// <param name="pointAddress">The point address.</param>
        /// <param name="newValue">The new value.</param>
        private void CommandExecutor_UpdatePointEvent(PointType type, ushort pointAddress, ushort newValue)
        {
            List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });
            
            if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT)
            {
                ProcessAnalogPoint(points.First() as IAnalogPoint, newValue);
            }
            else
            {
                ProcessDigitalPoint(points.First() as IDigitalPoint, newValue);
            }
        }

        /// <summary>
        /// Processes a digital point.
        /// </summary>
        /// <param name="point">The digital point</param>
        /// <param name="newValue">The new value.</param>
        // TODO dopuni
        private void ProcessDigitalPoint(IDigitalPoint point, ushort newValue)
        {
            point.RawValue = newValue;
            point.Timestamp = DateTime.Now;
            point.State = (DState)newValue;

            point.Alarm = alarmProcessor.GetAlarmForDigitalPoint(point.RawValue, point.ConfigItem);
        }

        /// <summary>
        /// Processes an analog point
        /// </summary>
        /// <param name="point">The analog point.</param>
        /// <param name="newValue">The new value.</param>
        // TODO dopuni
        private void ProcessAnalogPoint(IAnalogPoint point, ushort newValue)
        {
            point.RawValue = newValue;
            point.Timestamp = DateTime.Now;

            point.EguValue = eguConverter.ConvertToEGU(point.ConfigItem.ScaleFactor, point.ConfigItem.Deviation, newValue);
            point.Alarm = alarmProcessor.GetAlarmForAnalogPoint(point.EguValue, point.ConfigItem);
        }

        /// <inheritdoc />
        public void InitializePoint(PointType type, ushort pointAddress, ushort defaultValue)
        {
            List<IPoint> points = storage.GetPoints(new List<PointIdentifier>(1) { new PointIdentifier(type, pointAddress) });

            if (type == PointType.ANALOG_INPUT || type == PointType.ANALOG_OUTPUT)
            {
                ProcessAnalogPoint(points.First() as IAnalogPoint, defaultValue);
            }
            else
            {
                ProcessDigitalPoint(points.First() as IDigitalPoint, defaultValue);
            }
        }
    }
}
