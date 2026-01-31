using ScePSPUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public sealed class PspContext : IDisposable
{
    static Logger Logger = Logger.GetLogger("PspContext");

    public PspContext()
    {
        SetInstance<PspContext>(this);
    }

    private readonly ConcurrentDictionary<Type, object> ObjectsByType = new ConcurrentDictionary<Type, object>();

    private readonly ConcurrentDictionary<Type, Type> TypesByType = new ConcurrentDictionary<Type, Type>();

    public object GetInstance(Type Type)
    {
        if (!ObjectsByType.ContainsKey(Type))
        {
            var Instance = default(object);

            Logger.Notice("GetInstance<{0}>: Miss!", Type);

            var ElapsedTime = Logger.Measure(() =>
            {
                var RealType = TypesByType.ContainsKey(Type) ? TypesByType[Type] : Type;

                if (RealType.IsAbstract) throw (new Exception(String.Format("Can't instantiate class '{0}', because it is abstract", RealType)));

                try
                {
                    //object Instance2 = null;
                    //var ElapsedTime2 = Logger.Measure(() =>
                    //{
                    //	Instance2 = Activator.CreateInstance(RealType, true);
                    //});
                    //Console.Error.WriteLine("{0} : {1} : {2}", ElapsedTime2, Type, RealType);
                    Instance = _SetInstance(Type, Activator.CreateInstance(RealType, true));
                }
                catch (MissingMethodException)
                {
                    throw (new Exception("No constructor for type '" + Type.Name + "'"));
                }

                InjectDependencesTo(Instance);
                //Instance._InitializeComponent(this);
                //Instance.InitializeComponent();
            });

            //Console.Out.WriteLineColored((ElapsedTime.TotalSeconds >= 0.05) ? ConsoleColor.Red : ConsoleColor.Gray, "GetInstance<{0}>: Miss! : LoadTime({1})", Type, ElapsedTime.TotalSeconds);
            Logger.Notice("GetInstance<{0}>: Miss! : LoadTime({1})", Type, ElapsedTime.TotalSeconds);

            return Instance;
        }

        return ObjectsByType[Type];
    }

    public TType GetInstance<TType>()// where TType : IInjectComponent
    {
        return (TType)GetInstance(typeof(TType));
    }

    public TType SetInstance<TType>(object Instance)// where TType : IInjectComponent
    {
        Logger.Info("PspEmulatorContext.SetInstance<{0}>", typeof(TType));
        return _SetInstance<TType>(Instance);
    }

    public object _SetInstance(Type Type, object Instance)
    {
        if (ObjectsByType.ContainsKey(Type))
        {
            throw (new InvalidOperationException());
        }
        ObjectsByType[Type] = Instance;
        return Instance;
    }

    public TType _SetInstance<TType>(object Instance)// where TType : IInjectComponent
    {
        return (TType)_SetInstance(typeof(TType), Instance);
    }

    public void SetInstanceType<TType1>(Type Type2)// where TType1 : IInjectComponent
    {
        SetInstanceType(typeof(TType1), Type2);
    }

    public void SetInstanceType<TType1, TType2>()// where TType1 : IInjectComponent
    {
        SetInstanceType<TType1>(typeof(TType2));
    }

    public void SetInstanceType(Type Type1, Type Type2)// where TType1 : IInjectComponent
    {
        TypesByType[Type1] = Type2;
    }

    public TType NewInstance<TType>()// where TType : IInjectComponent
    {
        RemoveInstance(typeof(TType));
        return GetInstance<TType>();
    }

    private void RemoveInstance(Type Type)
    {
        object Removed;
        while (ObjectsByType.TryRemove(Type, out Removed))
        {
            if (!ObjectsByType.ContainsKey(Type)) break;
        }
    }

    public void InjectDependencesTo(object Object)
    {
        var GetBindingFlags = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // Initialize all [Context]
        foreach (var Member in Object.GetType().GetMembers(GetBindingFlags))
        {
            //Console.WriteLine("{0}", Member);
            var Field = Member as FieldInfo;
            var Property = Member as PropertyInfo;
            Type MemberType = null;
            if (Member.MemberType == MemberTypes.Field) MemberType = Field.FieldType;
            if (Member.MemberType == MemberTypes.Property) MemberType = Property.PropertyType;

            var InjectAttributeList = Member.GetCustomAttributes(typeof(ContextAttribute), true);

            if (InjectAttributeList.Length > 0)
            {
                switch (Member.MemberType)
                {
                    case MemberTypes.Field: Field.SetValue(Object, this.GetInstance(MemberType)); break;
                    case MemberTypes.Property: Property.SetValue(Object, this.GetInstance(MemberType), null); break;
                }
                Logger.Notice("Inject {0} to {1}", MemberType, Object.GetType());
            }
        }

        // Call Initialization
        if (Object.GetType().GetInterfaces().Contains(typeof(IContextInitialize)))
        {
            ((IContextInitialize)Object).Initialize();
        }
    }

    bool _Dispose;

    public void Dispose()
    {
        if (_Dispose) return;

        _Dispose = true;

        foreach (var Item in ObjectsByType.Values)
        {
            if (Item == this) continue;

            if (Item.GetType().GetInterfaces().Contains(typeof(IDisposable)))
            {
                ((IDisposable)Item).Dispose();
            }
        }

        ObjectsByType.Clear();
    }

    public static PspContext Bootstrap(object Bootstrap, Dictionary<Type, Type> PairTypes = null)
    {
        var PspContext = new PspContext();
        PspContext.MapFromClassAttributes(Bootstrap);
        PspContext.MapFromPairTypes(PairTypes);
        PspContext.InjectDependencesTo(Bootstrap);
        return PspContext;
    }

    public void MapFromPairTypes(Dictionary<Type, Type> PairTypes)
    {
        if (PairTypes != null)
        {
            foreach (var Pair in PairTypes)
            {
                this.SetInstanceType(Pair.Key, Pair.Value);
            }
        }
    }

    public void MapFromClassAttributes(object Bootstrap)
    {
        foreach (var InjectMap in Bootstrap.GetType().GetCustomAttributes<ContextMapAttribute>(true))
        {
            this.SetInstanceType(InjectMap.From, InjectMap.To);
        }
    }
}
