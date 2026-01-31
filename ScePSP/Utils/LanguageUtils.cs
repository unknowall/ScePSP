using System;
using System.Reflection;

namespace ScePSPUtils
{
    // Token: 0x0200084B RID: 2123
    public class LanguageUtils
    {
        // Token: 0x06002E10 RID: 11792 RVA: 0x0010C9C0 File Offset: 0x0010ABC0
        public static void Swap<TType>(ref TType Left, ref TType Right)
        {
            TType ttype = Left;
            Left = Right;
            Right = ttype;
        }

        // Token: 0x06002E11 RID: 11793 RVA: 0x0005EC17 File Offset: 0x0005CE17
        public static void Transfer<TType>(ref TType Left, ref TType Right, bool CopyToLeft)
        {
            if (CopyToLeft)
            {
                Left = Right;
                return;
            }
            Right = Left;
        }

        // Token: 0x06002E12 RID: 11794 RVA: 0x0010C9E8 File Offset: 0x0010ABE8
        public static void LocalSet<TType>(ref TType Variable, TType LocalValue, Action LocalScope)
        {
            TType ttype = Variable;
            Variable = LocalValue;
            try
            {
                LocalScope();
            }
            finally
            {
                Variable = ttype;
            }
        }

        // Token: 0x06002E13 RID: 11795 RVA: 0x0010CA28 File Offset: 0x0010AC28
        public static void PropertyLocalSet(object Object, string PropertyName, object LocalValue, Action LocalScope)
        {
            PropertyInfo property = Object.GetType().GetProperty(PropertyName);
            object value = property.GetValue(Object);
            property.SetValue(Object, LocalValue);
            try
            {
                LocalScope();
            }
            finally
            {
                property.SetValue(Object, value);
            }
        }
    }
}