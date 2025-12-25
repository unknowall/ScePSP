using ScePSP.Core;
using ScePSP.Core.Audio;
using ScePSP.Core.Audio.Impl.Null;
using ScePSP.Core.Audio.Impl.SDL;
using ScePSP.Core.Cpu;
using ScePSP.Core.Gpu;
using ScePSP.Core.Gpu.Impl.Null;
using ScePSP.Core.Gpu.Impl.Opengl;
using ScePSP.Core.Gpu.Impl.Soft;
using ScePSP.Core.Memory;
using ScePSP.Hle;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Modules;
using System;

namespace ScePSP.Runner
{
    public enum PspGpuType
    {
        Soft = 0,
        OpenGL = 1,
        Null = -1,
    }

    public enum PspAudioType
    {
        SDL = 0,
        Null = -1,
    }

    public class PspInjectContext
    {
        public static InjectContext CreateInjectContext(PspStoredConfig storedConfig, 
            PspGpuType gputype = PspGpuType.Soft, PspAudioType audiotype = PspAudioType.SDL, 
            Action<InjectContext> configure = null)
        {
            var injectContext = new InjectContext();
            configure?.Invoke(injectContext);
            injectContext.SetInstance<PspStoredConfig>(storedConfig);
            injectContext.GetInstance<HleConfig>().HleModulesDll = typeof(HleModulesRoot).Assembly;
            injectContext.SetInstanceType<ICpuConnector, HleThreadManager>();
            injectContext.SetInstanceType<IGpuConnector, HleThreadManager>();
            injectContext.SetInstanceType<IInterruptManager, HleInterruptManager>();
            injectContext.SetInstanceType<PspMemory, FastPspMemory>();

            switch (gputype)
            {
                case PspGpuType.Null:
                    PspPluginImpl.SelectWorkingPlugin<GpuImpl>(injectContext, typeof(GpuImplNull));
                    break;
                case PspGpuType.Soft:
                    PspPluginImpl.SelectWorkingPlugin<GpuImpl>(injectContext, typeof(GpuImplSoft));
                    break;
                case PspGpuType.OpenGL:
                    PspPluginImpl.SelectWorkingPlugin<GpuImpl>(injectContext, typeof(OpenglGpuImpl));
                    break;
            }

            switch (audiotype)
            {
                case PspAudioType.Null:
                    PspPluginImpl.SelectWorkingPlugin<AudioImpl>(injectContext, typeof(AudioImplNull));
                    break;
                case PspAudioType.SDL:
                    PspPluginImpl.SelectWorkingPlugin<AudioImpl>(injectContext, typeof(SDLAudioImpl));
                    break;
            }

            return injectContext;
        }
    }
}