using System;
namespace ScePSP.GE.Run
{
    public unsafe sealed partial class GERunner
    {
        /**
		 * Start filling a new display-context
		 *
		 * Contexts available are:
		 *   - GU_DIRECT - Rendering is performed as list is filled
		 *   - GU_CALL - List is setup to be called from the main list
		 *   - GU_SEND - List is buffered for a later call to sceGuSendList()
		 *
		 * The previous context-type is stored so that it can be restored at sceGuFinish().
		 *
		 * @param cid - Context Type
		 * @param list - Pointer to display-list (16 byte aligned)
		 **/
        // void sceGuStart(int cid, void* list);

        /**
		 * Finish current display list and go back to the parent context
		 *
		 * If the context is GU_DIRECT, the stall-address is updated so that the entire list will
		 * execute. Otherwise, only the terminating action is written to the list, depending on
		 * context-type.
		 *
		 * The finish-callback will get a zero as argument when using this function.
		 *
		 * This also restores control back to whatever context that was active prior to this call.
		 *
		 * @return Size of finished display list
		 **/
        // int sceGuFinish(void);

        /**
		 * Finish current display list and go back to the parent context, sending argument id for
		 * the finish callback.
		 *
		 * If the context is GU_DIRECT, the stall-address is updated so that the entire list will
		 * execute. Otherwise, only the terminating action is written to the list, depending on
		 * context-type.
		 *
		 * @param id - Finish callback id (16-bit)
		 * @return Size of finished display list
		 **/
        // int sceGuFinishId(unsigned int id);

        /**
		 * Call previously generated display-list
		 *
		 * @param list - Display list to call
		 **/
        // void sceGuCallList(const void* list);

        /**
		 * Set wether to use stack-based calls or signals to handle execution of called lists.
		 *
		 * @param mode - GU_TRUE(1) to enable signals, GU_FALSE(0) to disable signals and use
		 * normal calls instead.
		 **/
        // void sceGuCallMode(int mode);

        /**
		 * Check how large the current display-list is
		 *
		 * @return The size of the current display list
		 **/
        // int sceGuCheckList(void);

        /**
		 * Send a list to the GE directly
		 *
		 * Available modes are:
		 *   - GU_TAIL - Place list last in the queue, so it executes in-order
		 *   - GU_HEAD - Place list first in queue so that it executes as soon as possible
		 *
		 * @param mode - Whether to place the list first or last in queue
		 * @param list - List to send
		 * @param context - Temporary storage for the GE context
		 **/
        // void sceGuSendList(int mode, const void* list, PspGeContext* context);

        public void OP_JUMP()
        {
            GECore.JumpRelativeOffset((uint)(Params24 & ~3));
        }

        public void OP_END()
        {
            GECore.Done = true;
            GECore.GeList.BackEnd.End(GpuState);
        }

        public void OP_FINISH()
        {
            GECore.Finish = true;
            GECore.GeList.BackEnd.Finish(GECore.GEStateStruct);
            GECore.DoFinish(PC, Params24, ExecuteNow: true);
        }

        public void OP_CALL()
        {
            GECore.CallRelativeOffset((uint)(Params24 & ~3));
        }

        public void OP_RET()
        {
            GECore.Ret();
        }

        /**
		 * Trigger signal to call code from the command stream
		 *
		 * Available behaviors are:
		 *   - GU_BEHAVIOR_SUSPEND - Stops display list execution until callback function finished
		 *   - GU_BEHAVIOR_CONTINUE - Do not stop display list execution during callback
		 *
		 * @param signal - Signal to trigger
		 * @param behavior - Behavior type
		 **/
        public void OP_SIGNAL()
        {
            var Signal = Extract(0, 16);
            var Behaviour = (SignalBehavior)Extract(16, 8);

            Console.Out.WriteLineColored(ConsoleColor.Green, "OP_SIGNAL: {0}, {1}", Signal, Behaviour);

            GpuInstruction next;

            switch (Behaviour)
            {
                case SignalBehavior.PSP_GE_SIGNAL_NONE:
                    break;

                case SignalBehavior.PSP_GE_SIGNAL_CALL:
                    next = GECore.ReadInstructionAndMoveNext();
                    if (next.OpCode == OpCodes.END)
                    {
                        uint hi16 = Signal & 0x0FFF;
                        // Read & skip END
                        uint lo16 = next.Instruction & 0xFFFF;
                        uint addr = (hi16 << 16) | lo16;
                        //uint oldPc = InstructionAddressCurrent;
                        GECore.CallAbsolute(addr);
                        //uint newPc = InstructionAddressCurrent;
                    }
                    break;

                case SignalBehavior.PSP_GE_SIGNAL_RET:
                    next = GECore.ReadInstructionAndMoveNext();
                    if (next.OpCode == OpCodes.END)
                    {
                        GECore.Ret();
                    }
                    break;

                case SignalBehavior.PSP_GE_SIGNAL_RTBP0:
                case SignalBehavior.PSP_GE_SIGNAL_RTBP1:
                case SignalBehavior.PSP_GE_SIGNAL_RTBP2:
                case SignalBehavior.PSP_GE_SIGNAL_RTBP3:
                case SignalBehavior.PSP_GE_SIGNAL_RTBP4:
                case SignalBehavior.PSP_GE_SIGNAL_RTBP5:
                case SignalBehavior.PSP_GE_SIGNAL_RTBP6:
                case SignalBehavior.PSP_GE_SIGNAL_RTBP7:
                    next = GECore.ReadInstructionAndMoveNext();
                    if (next.OpCode == OpCodes.END)
                    {
                        uint hi16 = Signal & 0xFFFF;
                        uint lo16 = next.Instruction & 0xFFFF;
                        uint width = (next.Instruction >> 16) & 0xFF;
                        uint addr = GECore.GetAddressRel((hi16 << 16) | lo16);

                        GECore.GEStateStruct->TextureMappingState.TextureState.Mipmap0.Address = addr;
                        GECore.GEStateStruct->TextureMappingState.TextureState.Mipmap0.BufferWidth = (ushort)width;
                    }
                    break;

                case SignalBehavior.PSP_GE_SIGNAL_OTBP0:
                case SignalBehavior.PSP_GE_SIGNAL_OTBP1:
                case SignalBehavior.PSP_GE_SIGNAL_OTBP2:
                case SignalBehavior.PSP_GE_SIGNAL_OTBP3:
                case SignalBehavior.PSP_GE_SIGNAL_OTBP4:
                case SignalBehavior.PSP_GE_SIGNAL_OTBP5:
                case SignalBehavior.PSP_GE_SIGNAL_OTBP6:
                case SignalBehavior.PSP_GE_SIGNAL_OTBP7:
                    next = GECore.ReadInstructionAndMoveNext();
                    if (next.OpCode == OpCodes.END)
                    {
                        uint hi16 = Signal & 0xFFFF;
                        uint lo16 = next.Instruction & 0xFFFF;
                        uint width = (next.Instruction >> 16) & 0xFF;
                        uint addr = GECore.GetAddressRelOffset((hi16 << 16) | lo16);

                        GECore.GEStateStruct->TextureMappingState.TextureState.Mipmap0.Address = addr;
                        GECore.GEStateStruct->TextureMappingState.TextureState.Mipmap0.BufferWidth = (ushort)width;
                    }
                    break;

                case SignalBehavior.PSP_GE_SIGNAL_HANDLER_CONTINUE:
                case SignalBehavior.PSP_GE_SIGNAL_HANDLER_PAUSE:
                case SignalBehavior.PSP_GE_SIGNAL_HANDLER_SUSPEND:
                    next = GECore.ReadInstructionAndMoveNext();
                    if (next.OpCode != OpCodes.END)
                    {
                        throw new NotImplementedException("Error! Next Signal not an END! : " + next.OpCode);
                    }
                    GECore.DoSignal(GECore.Pc, Signal, Behaviour, ExecuteNow: true);
                    break;
                default:
                    Console.WriteLine($"Not implemented {Behaviour}");
                    break;
            }

        }

    }
}
