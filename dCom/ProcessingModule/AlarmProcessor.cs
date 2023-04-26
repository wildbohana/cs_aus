﻿using Common;
using System.Configuration;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for alarm processing.
    /// </summary>
    public class AlarmProcessor
	{
        /// <summary>
        /// Processes the alarm for analog point.
        /// </summary>
        /// <param name="eguValue">The EGU value of the point.</param>
        /// <param name="configItem">The configuration item.</param>
        /// <returns>The alarm indication.</returns>
        // TODO implementiraj
		public AlarmType GetAlarmForAnalogPoint(double eguValue, IConfigItem configItem)
		{
            if (eguValue < configItem.LowLimit)
                return AlarmType.LOW_ALARM;
            else if (eguValue > configItem.HighLimit)
                return AlarmType.HIGH_ALARM;
            else
                return AlarmType.NO_ALARM;
        }

        /// <summary>
        /// Processes the alarm for digital point.
        /// </summary>
        /// <param name="state">The digital point state</param>
        /// <param name="configItem">The configuration item.</param>
        /// <returns>The alarm indication.</returns>
        // TODO implementiraj
        public AlarmType GetAlarmForDigitalPoint(ushort state, IConfigItem configItem)
		{
            if (state == configItem.AbnormalValue)
                return AlarmType.ABNORMAL_VALUE;
            else
                return AlarmType.NO_ALARM;
        }
    }
}
