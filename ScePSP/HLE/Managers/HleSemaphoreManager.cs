using ScePSP.Hle.Threading.Semaphores;

namespace ScePSP.Hle.Managers
{
    public class HleSemaphoreManager
    {
        public HleUidPool<HleSemaphore> Semaphores = new HleUidPool<HleSemaphore>();

        public HleSemaphore Create()
        {
            return new HleSemaphore();
        }
    }
}