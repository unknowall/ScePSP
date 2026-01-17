namespace ScePSP.Devices.Battery
{
    public class Battery
    {
        public bool IsPluggedIn = true;

        public bool IsPresent = true;

        public bool BatteryExists = true;

        public bool IsStandBy = false;

        public bool IsBatteryCharging = true;

        public int BatteryLifeTimeInMinutes = 5 * 60;

        public bool IsPowerOnline = true;

        public double BatteryLifePercent = 1.0;

        /// <summary>Some standard battery temperature 28 deg C</summary>
        public int BatteryTemperature = 28;

        /// <summary>Battery voltage 4,135 in slim</summary>
        public int BatteryVoltage = 4135;

        /// <summary>Led starts flashing at 12%</summary>
        public double LowPercent = 0.12;
    }
}