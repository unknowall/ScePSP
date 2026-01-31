using ScePSP.Hle.Threading.EventFlags;

namespace ScePSP.Hle.Managers
{
    public enum EventFlagId : int { }

    public class HleEventFlagManager
    {
        public HleUidPoolSpecial<HleEventFlag, EventFlagId> EventFlags = new HleUidPoolSpecial<HleEventFlag, EventFlagId>();
    }
}
