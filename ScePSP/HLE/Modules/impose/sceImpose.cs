using ScePSP.Hle.Attributes;
using ScePSP.Types;

namespace ScePSP.Hle.Modules.impose
{
    [HlePspModule(ModuleFlags = ModuleFlags.UserMode | ModuleFlags.Flags0x00010011)]
    public unsafe class sceImpose : HleModuleHost
    {
        uint umdPopupStatus;

        [HlePspFunction(NID = 0x72189C48, FirmwareVersion = 150)]
        [HleTrackCall]
        public uint sceImposeSetUMDPopupFunction(uint UmdPopupStatus)
        {
            this.umdPopupStatus = UmdPopupStatus;
            return 0;
        }

        [HlePspFunction(NID = 0xE0887BC8, FirmwareVersion = 150)]
        [HleTrackCall]
        public uint sceImposeGetUMDPopupFunction()
        {
            return this.umdPopupStatus;
        }

        /// <summary>
        /// Set the language and button assignment parameters
        /// </summary>
        /// <param name="Language">Language</param>
        /// <param name="ConfirmButton">Button assignment (Cross or circle)</param>
        /// <returns>Less than 0 on error</returns>
        [HlePspFunction(NID = 0x36AA6E91, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceImposeSetLanguageMode(PspLanguages Language, PSP_SYSTEMPARAM_BUTTON_PREFERENCE ConfirmButton)
        {
            HleConfig.Language = Language;
            HleConfig.ConfirmButton = ConfirmButton;
            return 0;
        }

        /// <summary>
        /// Get the language and button assignment parameters
        /// </summary>
        /// <param name="Language">Pointer to store the language</param>
        /// <param name="ConfirmButton">Pointer to store the button assignment (Cross or circle)</param>
        /// <returns>Less than 0 on error</returns>
        [HlePspFunction(NID = 0x24FD7BCF, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceImposeGetLanguageMode(out PspLanguages Language, out PSP_SYSTEMPARAM_BUTTON_PREFERENCE ConfirmButton)
        {
            Language = HleConfig.Language;
            ConfirmButton = HleConfig.ConfirmButton;
            return 0;
        }

        public enum ChargingEnum : uint
        {
            NotCharging = 0,
            Charging = 1,
        }

        public enum BatteryStatusEnum : uint
        {
            VeryLow = 0,
            Low = 1,
            PartiallyFilled = 2,
            FullyPilled = 3,
        }

        /// <summary>
        /// IsChargingPointer:      <para/>
        ///		0 - if not charging <para/>
        ///		1 - if charging     <para/>-<para/>
        ///	IconStatusPointer:                <para/>
        ///		0 - Battery is very low       <para/>       
        ///		1 - Battery is low            <para/>
        ///		2 - Battery is partial filled <para/>
        ///		3 - Battery is fully filled   <para/>
        /// </summary>
        /// <param name="IsChargingPointer"></param>
        /// <param name="IconStatusPointer"></param>
        /// <returns></returns>
        [HlePspFunction(NID = 0x8C943191, FirmwareVersion = 150)]
        //[HleTrackCall]
        public uint sceImposeGetBatteryIconStatus(ChargingEnum* IsChargingPointer, BatteryStatusEnum* IconStatusPointer)
        {
            *IsChargingPointer = ChargingEnum.NotCharging;
            *IconStatusPointer = BatteryStatusEnum.FullyPilled;
            return 0;
        }

        /// <summary>
        /// Set the value of the backlight timer.
        /// </summary>
        /// <param name="value">The backlight timer. (30 to a lot of seconds)</param>
        /// <returns>&lt; 0 on error.</returns>
        [HlePspFunction(NID = 0x967F6D4A, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceImposeSetBacklightOffTime(int value)
        {
            return 0;
        }

        /// <summary>
        /// Get the value of the backlight timer.
        /// </summary>
        /// <returns>Backlight timer in seconds, or &lt; 0 on error</returns>
        [HlePspFunction(NID = 0x8F6E3518, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceImposeGetBacklightOffTime()
        {
            return 0;
        }
    }
}
