using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace Heart_Module.Data.Scripts.HeartModule.ExceptionHandler
{
    
    public class CriticalHandle
    {
        const int WarnTimeSeconds = 20;
        private static CriticalHandle I;
        private long CriticalCloseTime = -1;

        public void LoadData()
        {
            I = this;
        }

        int i = 0;
        public void Update()
        {
            if (CriticalCloseTime == -1)
                return;
            double secondsRemaining = Math.Round((CriticalCloseTime - DateTime.Now.Ticks) / (double)TimeSpan.TicksPerSecond, 1);

            if (secondsRemaining <= 0)
                MyVisualScriptLogicProvider.SessionClose(1000, false, true);

            MyAPIGateway.Utilities.ShowNotification($"HeartMod CRITICAL ERROR - Shutting down in {secondsRemaining}s", 1000/60);
            i++;
            if (i >= 60)
            {
                i = 0;
                MyAPIGateway.Utilities.ShowMessage("HeartMod", $"CRITICAL ERROR - Shutting down in {secondsRemaining} seconds.");
            }
        }

        public void UnloadData()
        {
            I = null;
        }

        public static void ThrowCriticalException(Exception ex, Type callingType, ulong callerId = ulong.MaxValue)
        {
            I?.m_ThrowCriticalException(ex, callingType, callerId);
        }

        private void m_ThrowCriticalException(Exception ex, Type callingType, ulong callerId = ulong.MaxValue)
        {
            if (CriticalCloseTime == -1)
                return;
            HeartLog.LogException(ex, callingType, (callerId != ulong.MaxValue ? $"Shared exception from {callerId}: " : "") + "Critical ");
            CriticalCloseTime = DateTime.Now.Ticks + WarnTimeSeconds * TimeSpan.TicksPerSecond;
        }
    }
}
