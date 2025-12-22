using ScePSP.Hle.Threading.EventFlags;

namespace ScePSP.Hle.Managers
{
    public enum EventFlagId
    {
    }

    public class HleEventFlagManager
    {
        public HleUidPoolSpecial<HleEventFlag, EventFlagId> EventFlags =
            new HleUidPoolSpecial<HleEventFlag, EventFlagId>();
    }
}