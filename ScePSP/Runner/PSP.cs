using ScePSP.Audio;
using ScePSP.Audio.SDL;
using ScePSP.BackEnd.OpenGL;
using ScePSP.Components.Display;
using ScePSP.Core;
using ScePSP.Cpu;
using ScePSP.GE;
using ScePSP.Hle;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Modules;
using ScePSP.Memory;
using System;

namespace ScePSP.Runner
{
    public class PSP
    {
        static public PspContext Create(PspStoredConfig StoredConfig, IntPtr WindowHandle)
        {
            var _PspContext = new PspContext();
            _PspContext.SetInstance<PspStoredConfig>(StoredConfig);
            _PspContext.GetInstance<HleConfig>().HleModulesDll = typeof(HleModulesRoot).Assembly;
            _PspContext.GetInstance<GEConfig>().WindowHandle = WindowHandle;
            _PspContext.GetInstance<DisplayConfig>().WindowHandle = WindowHandle;
            _PspContext.SetInstanceType<ICpuConnector, HleThreadManager>();
            _PspContext.SetInstanceType<IGEConnector, HleThreadManager>();
            _PspContext.SetInstanceType<IInterruptManager, HleInterruptManager>();

            if (StoredConfig.UseFastMemory)
            {
                _PspContext.SetInstanceType<PspMemory, FastPspMemory>();
            }
            else
            {
                _PspContext.SetInstanceType<PspMemory, NormalPspMemory>();
            }

            _PspContext.SetInstanceType<GEBackEnd, GLBackEnd>();

            _PspContext.SetInstanceType<AudioBackEnd, SDLAudioBackEnd>();

            return _PspContext;
        }
    }
}
