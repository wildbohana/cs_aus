using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
	{
		private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
		private IProcessingManager processingManager;
		private int delayBetweenCommands;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        // TODO dodaj TBC
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
		{
			this.storage = storage;
			this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;

            this.delayBetweenCommands = configuration.DelayBetweenCommands;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
		{
			InitializeAutomationWorkerThread();
			StartAutomationWorkerThread();
		}

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
		{
			automationWorker = new Thread(AutomationWorker_DoWork);
			automationWorker.Name = "Aumation Thread";
		}

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
		{
			automationWorker.Start();
		}

		// TODO implementiraj
		private void AutomationWorker_DoWork()
		{
            EGUConverter egu = new EGUConverter();

            PointIdentifier kolicinaSastojaka = new PointIdentifier(PointType.ANALOG_OUTPUT, 1000);
            PointIdentifier start = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3000);
            PointIdentifier mesalica = new PointIdentifier(PointType.DIGITAL_OUTPUT, 3001);
            PointIdentifier ventil1 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4000);      // Čokolada
            PointIdentifier ventil2 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4001);      // Mleko
            PointIdentifier ventil3 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4002);      // Voda
            PointIdentifier ventil4 = new PointIdentifier(PointType.DIGITAL_OUTPUT, 4003);      // Pražnjenje

            List<PointIdentifier> signali = new List<PointIdentifier> { kolicinaSastojaka, start, mesalica, ventil1, ventil2, ventil3, ventil4 };
            ushort tempSuma = 0;

            // Ukoliko je pokrenut proces pravljenja sladoleda, potrebno je:
            // prvo sipati 100 kg čokolade,
            // zatim 150 l mleka
            // pa 120 l vode.
            // Kad se završi sipanje sastojaka, potrebno je uključiti i držati uključeno mešalicu 10 sekundi.
            // Nakon toga sledi poslednja faza, gde se kroz ventil V4 prazni mešalica.

            while (!disposedValue)
            {
                List<IPoint> tacke = storage.GetPoints(signali);
                IConfigItem configElement = tacke[0].ConfigItem;
                
                // Količina sastojaka
                ushort kolSas = (ushort)egu.ConvertToEGU(configElement.ScaleFactor, configElement.Deviation, tacke[0].RawValue);

                #region AUTOMAT
                // Start - pokreće rad
                if (tacke[1].RawValue == 1 && tacke[2].RawValue == 0 && tacke[3].RawValue == 0 && tacke[4].RawValue == 0 && tacke[5].RawValue == 0 && tacke[6].RawValue == 0)
                {
                    processingManager.ExecuteWriteCommand(tacke[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil1.Address, 1);
                }

                // Ventil1 - Čokolada (+50 l/s)
                if (tacke[3].RawValue == 1 && tacke[1].RawValue == 1)
                {
                    if ((tempSuma += 50) <= 100)
                    {
                        kolSas += 50;

                        if (kolSas <= configElement.MaxValue)
                        {
                            processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, kolicinaSastojaka.Address, kolSas);
                        }
                        else
                        {
                            tempSuma = 0;
                            processingManager.ExecuteWriteCommand(tacke[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil1.Address, 0);
                            automationTrigger.WaitOne();
                            processingManager.ExecuteWriteCommand(tacke[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, mesalica.Address, 1);
                        }
                    }
                    else
                    {
                        tempSuma = 0;
                        processingManager.ExecuteWriteCommand(tacke[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil1.Address, 0);
                        automationTrigger.WaitOne();
                        processingManager.ExecuteWriteCommand(tacke[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil2.Address, 1);
                    }
                }

                // Ventil2 - Mleko (+50 l/s)
                if (tacke[4].RawValue == 1 && tacke[1].RawValue == 1)
                {
                    if ((tempSuma += 50) <= 150)
                    {
                        kolSas += 50;

                        if (kolSas <= configElement.MaxValue)
                        {
                            processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, kolicinaSastojaka.Address, kolSas);
                        }
                        else
                        {
                            tempSuma = 0;
                            processingManager.ExecuteWriteCommand(tacke[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil2.Address, 0);
                            automationTrigger.WaitOne();
                            processingManager.ExecuteWriteCommand(tacke[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, mesalica.Address, 1);
                        }
                    }
                    else
                    {
                        tempSuma = 0;
                        processingManager.ExecuteWriteCommand(tacke[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil2.Address, 0);
                        automationTrigger.WaitOne();
                        processingManager.ExecuteWriteCommand(tacke[5].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil3.Address, 1);
                    }                    
                }

                // Ventil3 - Voda (+30 l/s)
                if (tacke[5].RawValue == 1 && tacke[1].RawValue == 1)
                {
                    if ((tempSuma += 30) <= 120)
                    {
                        kolSas += 30;

                        if (kolSas <= configElement.MaxValue)
                        {
                            processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, kolicinaSastojaka.Address, kolSas);
                        }
                        else
                        {
                            tempSuma = 0;
                            processingManager.ExecuteWriteCommand(tacke[5].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil3.Address, 0);
                            automationTrigger.WaitOne();
                            processingManager.ExecuteWriteCommand(tacke[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, mesalica.Address, 1);
                        }
                    }
                    else
                    {
                        tempSuma = 0;
                        processingManager.ExecuteWriteCommand(tacke[5].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil3.Address, 0);
                        automationTrigger.WaitOne();
                        processingManager.ExecuteWriteCommand(tacke[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, mesalica.Address, 1);
                    }                    
                }

                // Mešalica - 10 sekundi "meša"
                if (tacke[2].RawValue == 1 && tacke[1].RawValue == 1)
                {
                    //Thread.Sleep(10000);
                    for (int i = 0; i < 10000; i += 1000)
                        automationTrigger.WaitOne();

                    processingManager.ExecuteWriteCommand(tacke[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, mesalica.Address, 0);
                    automationTrigger.WaitOne();
                    processingManager.ExecuteWriteCommand(tacke[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil4.Address, 1);
                }

                // Ventil4 - Pražnjenje (-100 kg/s)
                if (tacke[6].RawValue == 1 && tacke[1].RawValue == 1)
                {
                    int temp = kolSas - 100;

                    if (temp >= configElement.MinValue)
                    {
                        processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, kolicinaSastojaka.Address, temp);
                    }
                    else
                    {
                        processingManager.ExecuteWriteCommand(tacke[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil4.Address, 0);
                        automationTrigger.WaitOne();
                        processingManager.ExecuteWriteCommand(tacke[1].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, start.Address, 0);
                    }
                }
                #endregion

                #region RUČNO
                // Ako nije pokrenut proces punjenja, onda samo držati otvorene ventile dok ne dođe do limita
                if (tacke[3].RawValue == 1 && tacke[1].RawValue == 0)
                {
                    if ((kolSas += 50) <= configElement.MaxValue)
                        processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, kolicinaSastojaka.Address, kolSas);
                    else
                        processingManager.ExecuteWriteCommand(tacke[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil1.Address, 0);
                }

                if (tacke[4].RawValue == 1 && tacke[1].RawValue == 0)
                {
                    if ((kolSas += 50) <= configElement.MaxValue)
                        processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, kolicinaSastojaka.Address, kolSas);
                    else
                        processingManager.ExecuteWriteCommand(tacke[4].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil2.Address, 0);
                }

                if (tacke[5].RawValue == 1 && tacke[1].RawValue == 0)
                {
                    if ((kolSas += 30) <= configElement.MaxValue)
                        processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, kolicinaSastojaka.Address, kolSas);
                    else
                        processingManager.ExecuteWriteCommand(tacke[5].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil3.Address, 0);
                }

                if (tacke[6].RawValue == 1 && tacke[1].RawValue == 0)
                {
                    int temp = kolSas - 100;
                    if ((temp) >= configElement.MinValue)
                        processingManager.ExecuteWriteCommand(configElement, configuration.GetTransactionId(), configuration.UnitAddress, kolicinaSastojaka.Address, temp);
                    else
                        processingManager.ExecuteWriteCommand(tacke[6].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, ventil4.Address, 0);
                }
                #endregion

                // Iz nekog razloga mora ovako, a ne sa Thread.Sleep(delayBetweenCommands)
                for (int i = 0; i < delayBetweenCommands; i += 1000)
                    automationTrigger.WaitOne();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}
				disposedValue = true;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
		{
            InitializeAndStartThreads();
		}

        /// <inheritdoc />
        public void Stop()
		{
			Dispose();
		}
		#endregion
	}
}
