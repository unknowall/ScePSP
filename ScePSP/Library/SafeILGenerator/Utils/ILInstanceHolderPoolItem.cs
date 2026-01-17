using System.Reflection;
using System.Runtime;
using SafeILGenerator.Ast.Nodes;

namespace SafeILGenerator.Utils
{
    public class ILInstanceHolderPoolItem<TType>
    {
        private readonly ILInstanceHolderPoolItem _item;
        public int Index => _item.Index;
        public FieldInfo FieldInfo => _item.FieldInfo;

        public ILInstanceHolderPoolItem(ILInstanceHolderPoolItem item) => _item = item;

        public TType Value
        {
            
            
            set { _item.Value = value; }
            
            
            get { return (TType) _item.Value; }
        }

        public void Free() => _item.Free();

        public AstNodeExprStaticFieldAccess AstFieldAccess => _item.GetAstFieldAccess();

        //public AstNodeExprStaticFieldAccess GetAstFieldAccess()
        //{
        //	return Item.GetAstFieldAccess();
        //}
    }
}