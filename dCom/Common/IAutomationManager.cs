﻿namespace Common
{
    /// <summary>
    /// Interface containing methods for starting and stopping the automation manager.
    /// </summary>
	public interface IAutomationManager
	{
        /// <summary>
        /// Starts the automation manager.
        /// </summary>
        /// <param name="delayBetweenCommands">The delay between commands (in seconds).</param>
		void Start(int delayBetweenCommands);

        /// <summary>
        /// Stops the autimation manager.
        /// </summary>
		void Stop();
	}
}
