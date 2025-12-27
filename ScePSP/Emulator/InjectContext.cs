using ScePSPUtils;
using ScePSPUtils.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public interface IInjectInitialize
{
    void Initialize();
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class InjectMapAttribute : Attribute
{
    public Type From { get; set; }
    public Type To { get; set; }

    public InjectMapAttribute(Type From, Type To)
    {
        this.From = From;
        this.To = To;
    }
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public sealed class InjectAttribute : Attribute
{
}

public class InjectMessageBus
{
    Dictionary<Type, List<Delegate>> Handlers = new Dictionary<Type, List<Delegate>>();

    public void Unregister<T>(Action<T> Handler)
    {
        //if (!Handlers.ContainsKey(typeof(T))) Handlers[typeof(T)] = new List<Delegate>();
        //Handlers[typeof(T)].Add(Handler);
    }

    public void Register<T>(Action<T> Handler)
    {
        if (!Handlers.ContainsKey(typeof(T))) Handlers[typeof(T)] = new List<Delegate>();
        Handlers[typeof(T)].Add(Handler);
    }

    public void Dispatch<T>(T Value)
    {
        if (Handlers.ContainsKey(typeof(T)))
        {
            foreach (var Handler in Handlers[typeof(T)])
            {
                Handler.DynamicInvoke(Value);
            }
        }
    }
}

public sealed class InjectContext : IDisposable
{
    public InjectContext()
    {
        SetInstance<InjectContext>(this);
    }

    static Logger Logger = Logger.GetLogger("InjectContext");
    private readonly ConcurrentDictionary<Type, object> ObjectsByType = new ConcurrentDictionary<Type, object>();
    private readonly ConcurrentDictionary<Type, Type> TypesByType = new ConcurrentDictionary<Type, Type>();

    public object GetInstance(Type Type)
    {
        if (ObjectsByType.ContainsKey(Type)) return ObjectsByType[Type];

        var Instance = default(object);

        Logger.Notice("GetInstance<{0}>: Miss!", Type);

        var ElapsedTime = Logger.Measure(() =>
        {
            var RealType = TypesByType.ContainsKey(Type) ? TypesByType[Type] : Type;

            if (RealType.IsAbstract)
                throw new Exception($"Can't instantiate class '{RealType}', because it is abstract");

            try
            {
                var constructors = RealType.GetConstructors();
                if (constructors.Length > 0)
                {
                    var constructor = constructors.First();
                    var paramTypes = constructor.GetParameters().Select(it => it.ParameterType).ToArray();
                    var paramValues = paramTypes.Select(GetInstance).ToArray();
                    Instance = _SetInstance(Type, constructor.Invoke(paramValues));
                }
                else
                {
                    Instance = _SetInstance(Type, Activator.CreateInstance(RealType, true));
                }
            }
            catch (MissingMethodException)
            {
                throw new Exception("No constructor for type '" + Type.Name + "'");
            }

            InjectDependencesTo(Instance);
            //Instance._InitializeComponent(this);
            //Instance.InitializeComponent();
        });

        //Console.Out.WriteLineColored((ElapsedTime.TotalSeconds >= 0.05) ? ConsoleColor.Red : ConsoleColor.Gray, "GetInstance<{0}>: Miss! : LoadTime({1})", Type, ElapsedTime.TotalSeconds);
        Logger.Notice("GetInstance<{0}>: Miss! : LoadTime({1})", Type, ElapsedTime.TotalSeconds);

        return Instance;
    }

    public TType GetInstance<TType>() => (TType)GetInstance(typeof(TType));

    public TType SetInstance<TType>(object Instance) // where TType : IInjectComponent
    {
        Logger.Info("PspEmulatorContext.SetInstance<{0}>", typeof(TType));
        return _SetInstance<TType>(Instance);
    }

    private object _SetInstance(Type Type, object Instance)
    {
        if (ObjectsByType.ContainsKey(Type)) throw new InvalidOperationException("Already set");
        ObjectsByType[Type] = Instance;
        return Instance;
    }

    private TType _SetInstance<TType>(object instance) => (TType)_SetInstance(typeof(TType), instance);
    public void SetInstanceType<TType1>(Type type2) => SetInstanceType(typeof(TType1), type2);
    public void SetInstanceType<TType1, TType2>() where TType2 : TType1 => SetInstanceType<TType1>(typeof(TType2));
    public void SetInstanceType(Type Type1, Type Type2) => TypesByType[Type1] = Type2;

    public TType NewInstance<TType>()
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
        var GetBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        foreach (var Member in Object.GetType().GetMembers(GetBindingFlags))
        {
            var Field = Member as FieldInfo;
            var Property = Member as PropertyInfo;
            var MemberType = Member.MemberType switch
            {
                MemberTypes.Field => Field.FieldType,
                MemberTypes.Property => Property.PropertyType,
                _ => null
            };

            var InjectAttributeList = Member.GetCustomAttributes(typeof(InjectAttribute), true);
            if (InjectAttributeList.Length <= 0) continue;

            switch (Member.MemberType)
            {
                case MemberTypes.Field:
                    Field.SetValue(Object, GetInstance(MemberType));
                    break;
                case MemberTypes.Property:
                    Property.SetValue(Object, GetInstance(MemberType), null);
                    break;
            }

            Logger.Notice("Inject {0} to {1}", MemberType, Object.GetType());
        }

        // Call Initialization
        if (Object.GetType().GetInterfaces().Contains(typeof(IInjectInitialize)))
        {
            ((IInjectInitialize)Object).Initialize();
        }
    }

    public void Dispose()
    {
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

    public static InjectContext Bootstrap(object Bootstrap, Dictionary<Type, Type> PairTypes = null)
    {
        var InjectContext = new InjectContext();
        InjectContext.MapFromClassAttributes(Bootstrap);
        InjectContext.MapFromPairTypes(PairTypes);
        InjectContext.InjectDependencesTo(Bootstrap);
        return InjectContext;
    }

    public void MapFromPairTypes(Dictionary<Type, Type> PairTypes)
    {
        if (PairTypes != null)
        {
            foreach (var Pair in PairTypes)
            {
                SetInstanceType(Pair.Key, Pair.Value);
            }
        }
    }

    public void MapFromClassAttributes(object Bootstrap)
    {
        foreach (var InjectMap in Bootstrap.GetType().GetCustomAttributes<InjectMapAttribute>(true))
        {
            SetInstanceType(InjectMap.From, InjectMap.To);
        }
    }
}
