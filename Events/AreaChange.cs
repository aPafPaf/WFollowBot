using Coroutine;
using GameHelper.CoroutineEvents;
using System;
using System.Collections.Generic;

namespace WFollowBot.Events
{
    public class AreaChange : IDisposable
    {
        private ActiveCoroutine? onAreaChange;
        private bool disposed;

        public AreaChange()
        {
            this.onAreaChange = CoroutineHandler.Start(OnAreaChange());
        }

        private IEnumerator<Wait> OnAreaChange()
        {
            while (true)
            {
                yield return new Wait(RemoteEvents.AreaChanged);
                TerrainInfo.Update();
            }
        }

        public void Disable()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            if (this.onAreaChange != null)
            {
                this.onAreaChange.Cancel();
                this.onAreaChange = null;
            }

            this.disposed = true;
        }
    }
}
