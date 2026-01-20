using ScePSP.GE.State.SubStates;

namespace ScePSP.GE.Run
{
    public unsafe sealed partial class GERunner
    {
        /**
		 * Set transform matrices
		 *
		 * Available matrices are:
		 *   - GU_PROJECTION - View->Projection matrix
		 *   - GU_VIEW - World->View matrix
		 *   - GU_MODEL - Model->World matrix
		 *   - GU_TEXTURE - Texture matrix
		 *
		 * @param type - Which matrix-type to set
		 * @param matrix - Matrix to load
		 **/

        public void OP_VMS()
        {
            uint StartIndex = Params24;
            GECore.GEStateStruct->VertexState.ViewMatrix.Reset(StartIndex);
        }
        public void OP_VIEW()
        {
            GECore.GEStateStruct->VertexState.ViewMatrix.Write(Float1);
        }

        public void OP_WMS()
        {
            uint StartIndex = Params24;
            GECore.GEStateStruct->VertexState.WorldMatrix.Reset(StartIndex);
        }
        public void OP_WORLD()
        {
            GECore.GEStateStruct->VertexState.WorldMatrix.Write(Float1);
        }

        public void OP_PMS()
        {
            uint StartIndex = Params24;
            GECore.GEStateStruct->VertexState.ProjectionMatrix.Reset(StartIndex);
        }
        public void OP_PROJ()
        {
            GECore.GEStateStruct->VertexState.ProjectionMatrix.Write(Float1);
        }

        private SkinningStateStruct* SkinningState
        {
            get
            {
                return &GECore.GEStateStruct->SkinningState;
            }
        }

        /**
		  * Specify skinning matrix entry
		  *
		  * To enable vertex skinning, pass GU_WEIGHTS(n), where n is between
		  * 1-8, and pass available GU_WEIGHT_??? declaration. This will change
		  * the amount of weights passed in the vertex araay, and by setting the skinning,
		  * matrices, you will multiply each vertex every weight and vertex passed.
		  *
		  * Please see sceGuDrawArray() for vertex format information.
		  *
		  * @param index - Skinning matrix index (0-7)
		  * @param matrix - Matrix to set
		**/
        // it defines the position in the matrixes not the index of the matrix. So we will do a hack there until fixed.
        // http://svn.ps2dev.org/filedetails.php?repname=psp&path=%2Ftrunk%2Fpspsdk%2Fsrc%2Fgu%2FsceGuBoneMatrix.c
        public void OP_BOFS()
        {
            SkinningState->CurrentBoneIndex = (int)Params24;
        }

        public void OP_BONE()
        {
            var BoneMatrices = &SkinningState->BoneMatrix0;
            BoneMatrices[SkinningState->CurrentBoneIndex / 12].WriteAt(SkinningState->CurrentBoneIndex % 12, Float1);
            SkinningState->CurrentBoneIndex++;
        }
    }
}
