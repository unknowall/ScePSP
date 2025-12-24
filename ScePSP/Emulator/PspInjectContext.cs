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
using System.Collections.Generic;

namespace ScePSP.Runner
{
    public class PspInjectContext
    {
        public static InjectContext CreateInjectContext(PspStoredConfig storedConfig, bool test, Action<InjectContext> configure = null)
        {
            var injectContext = new InjectContext();
            configure?.Invoke(injectContext);
            injectContext.SetInstance<PspStoredConfig>(storedConfig);
            injectContext.GetInstance<HleConfig>().HleModulesDll = typeof(HleModulesRoot).Assembly;
            injectContext.SetInstanceType<ICpuConnector, HleThreadManager>();
            injectContext.SetInstanceType<IGpuConnector, HleThreadManager>();
            injectContext.SetInstanceType<IInterruptManager, HleInterruptManager>();
            injectContext.SetInstanceType<PspMemory, FastPspMemory>();

            if (!test)
            {
                // RENDER
                PspPluginImpl.SelectWorkingPlugin<GpuImpl>(injectContext,
                    typeof(GpuImplSoft),
                    typeof(OpenglGpuImpl)
                    //typeof(GpuImplNull)
                );

                // AUDIO
                PspPluginImpl.SelectWorkingPlugin<PspAudioImpl>(injectContext, 
                    typeof(SDLAudioImpl),
                    typeof(AudioImplNull)
                    );
            }
            else
            {
                injectContext.SetInstanceType<GpuImpl, GpuImplNull>();
                injectContext.SetInstanceType<PspAudioImpl, AudioImplNull>();
            }

            return injectContext;
        }
    }
}