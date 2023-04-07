using Common;

namespace dCom.ViewModel
{
    internal class AnalaogInput : AnalogBase
	{
        public AnalaogInput(IConfigItem c, IProcessingManager processingManager, IStateUpdater stateUpdater,
            IConfiguration configuration, int i) : base(c, processingManager, stateUpdater, configuration, i) { }

        // Write command is not applicable for input points.
        protected override void WriteCommand_Execute(object obj) { }
    }
}
