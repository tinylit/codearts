using CodeArts.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using static CodeArts.Emit.AstExpression;

namespace CodeArts.Emit.Tests
{
    public class Entry
    {
        public int Id { get; set; }
    }

    [TestClass]
    public class Tests
    {
        private static MethodInfo GetMethodInfo<T1, T2>(Func<T1, T2> f, T1 _) => f.Method;
        private static MethodInfo GetMethodInfo<T1, T2, T3>(Func<T1, T2, T3> f, T1 _, T2 _2) => f.Method;

        private static Expression GetExpression<TResult>(Expression<Func<Entry, TResult>> expression)
        {
            return expression;
        }

        [TestMethod]
        public void Test1()
        {
#if NET461
            var m = new ModuleEmitter(true);
#else
            var m = new ModuleEmitter();
#endif
            var classType = m.DefineType("test", TypeAttributes.Public);
            var method = classType.DefineMethod("Test", MethodAttributes.Public, typeof(int));

            var pI = method.DefineParameter(typeof(int), ParameterAttributes.None, "i");
            var pJ = method.DefineParameter(typeof(int), ParameterAttributes.None, "j");

            var b = Variable(typeof(Expression));

            method.Append(Assign(b, Constant(GetExpression(entry => entry.Id))));

            var switchAst = Switch(Constant(1));

            switchAst.Case(Constant(1))
                .Append(IncrementAssign(pI));

            switchAst.Case(Constant(2))
                .Append(AddAssign(pI, Constant(5)));

            var constantAst2 = Constant(1, typeof(object));

            var switchAst2 = Switch(constantAst2);

            var stringAst = Variable(typeof(string));
            var intAst = Variable(typeof(int));

            switchAst2.Case(stringAst);
            switchAst2.Case(intAst)
                .Append(Assign(pI, intAst));

            var switchAst3 = Switch(Constant("ABC"), DecrementAssign(pI), typeof(void));

            switchAst3.Case(Constant("A"))
                .Append(IncrementAssign(pI));

            switchAst3.Case(Constant("B"))
                .Append(AddAssign(pI, Constant(5)));

            method.Append(switchAst)
                .Append(switchAst2)
                .Append(switchAst3)
                .Append(Return(Condition(GreaterThanOrEqual(pI, pJ), pI, pJ)));

            var type = classType.CreateType();
#if NET461
            m.SaveAssembly();
#endif
        }

        public static Expression<TDelegate> Lambda<TDelegate>(Expression body, params ParameterExpression[] parameters)
        {
            return Expression.Lambda<TDelegate>(body, parameters);
        }

        public static IQueryable<TResult> Select<TSource, TResult>(IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector)
        {
            return Queryable.Select(source, selector);
        }

        [TestMethod]
        public void TestLinq()
        {
#if NET461
            var m = new ModuleEmitter(true);
#else
            var m = new ModuleEmitter();
#endif
            var classType = m.DefineType("linq", TypeAttributes.Public);
            var method = classType.DefineMethod("Query", MethodAttributes.Public, typeof(void));

            var pI = method.DefineParameter(typeof(int), ParameterAttributes.None, "i");
            var pJ = method.DefineParameter(typeof(int), ParameterAttributes.None, "j");

            var type = typeof(Entry);

            var arg = Variable(typeof(ParameterExpression));
            var argProperty = Variable(typeof(MemberExpression));

            var callArg = Call(typeof(Expression).GetMethod(nameof(Expression.Parameter), new Type[] { typeof(Type) }), Constant(type));
            var callProperty = Call(typeof(Expression).GetMethod(nameof(Expression.Property), new Type[] { typeof(Expression), typeof(string) }), arg, Constant("Id"));
            //var callBlock = Call(typeof(Expression).GetMethod(nameof(Expression.Block), new Type[] { typeof(IEnumerable<ParameterExpression>), typeof(IEnumerable<Expression>) }));

            method.Append(Assign(arg, callArg));
            method.Append(Assign(argProperty, callProperty));

            var variable_variables = Variable(typeof(ParameterExpression[]));

            var variables = NewArray(1, typeof(ParameterExpression));

            method.Append(Assign(variable_variables, variables));

            method.Append(Assign(ArrayIndex(variable_variables, 0), arg));

            var constantI = Call(typeof(Expression).GetMethod(nameof(Expression.Constant), new Type[] { typeof(object) }), Convert(pI, typeof(object)));

            var equalMethod = Call(typeof(Expression).GetMethod(nameof(Expression.Equal), new Type[] { typeof(Expression), typeof(Expression) }), argProperty, constantI);

            var lamdaMethod = typeof(Tests).GetMethod(nameof(Tests.Lambda))
                .MakeGenericMethod(typeof(Func<Entry, bool>));

            var whereLambda = Variable(typeof(Expression<Func<Entry, bool>>));

            method.Append(Assign(whereLambda, Call(lamdaMethod, equalMethod, variable_variables)));

            method.Append(ReturnVoid());

            classType.CreateType();
#if NET461
            m.SaveAssembly();
#endif

            // 生成代码如下：
            /**
            public void Query(int i, int j)
            {
	            ParameterExpression parameterExpression = Expression.Parameter(typeof(Entry));
	            MemberExpression left = Expression.Property(parameterExpression, "Id");
	            Expression<Func<Entry, bool>> expression = Tests.Lambda<Func<Entry, bool>>(parameters: new ParameterExpression[1]
	            {
		            parameterExpression
	            }, body: Expression.Equal(left, Expression.Constant(i)));
            }
             */
        }

        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
        public class DependencyInterceptAttribute : InterceptAttribute
        {
            static DependencyInterceptAttribute()
            {

            }
            public override void Run(InterceptContext context, Intercept intercept)
            {
                intercept.Run(context);
            }

            public override T Run<T>(InterceptContext context, Intercept<T> intercept)
            {
                if (context.Main.Name == nameof(IDependency.AopTestByRef))
                {
                    context.Inputs[1] = -10;

                    return default;
                }

                return intercept.Run(context);
            }

            public override Task RunAsync(InterceptContext context, InterceptAsync intercept)
            {
                return intercept.RunAsync(context);
            }

            public override Task<T> RunAsync<T>(InterceptContext context, InterceptAsync<T> intercept)
            {
                return intercept.RunAsync(context);
            }
        }

        /// <inheritdoc />
        [DependencyIntercept]
        public interface IDependency
        {
            bool Flags { get; set; }

            /// <inheritdoc />
            bool AopTest();

            [DependencyIntercept]
            bool AopTestByRef(int i, ref int j);

            //[DependencyIntercept]
            bool AopTestByOut(int i, out int j);

            T Get<T>() where T : struct;

            Task<T> GetAsync<T>() where T : new();
        }

        /// <inheritdoc />
        public class Dependency : IDependency
        {
            public bool Flags { get; set; }

            /// <inheritdoc />
            public bool AopTestByRef(int i, ref int j)
            {
                try
                {
                    return (i & 1) == 0;
                }
                finally
                {
                    j = i * 5;
                }

            }

            /// <inheritdoc />
            public virtual bool AopTestByOut(int i, out int j)
            {
                switch (i)
                {
                    case 1:
                        j = 5;
                        break;
                    case 2:
                        j = 15;
                        break;
                    default:
                        j = i * i * 5;
                        break;
                }


                string str = "A";

                switch (str)
                {
                    case "A":
                        str = "X";
                        break;
                    case "B":
                        str = "Y";
                        break;
                    default:
                        break;
                }

                object value = i;

                switch (value)
                {
                    case int i1:
                        i = i1;
                        break;
                    case string text:
                        i = 10;
                        break;
                    default:
                        break;
                }

                DateTimeKind timeKind = (DateTimeKind)i;

                switch (timeKind)
                {
                    case DateTimeKind.Local:
                        i++;
                        break;
                    case DateTimeKind.Unspecified:
                        i += 5;
                        break;
                    default:
                        i--;
                        break;
                }

                int? k = value is int ? (int?)value : default;

                //var b = (i == 1) && i > 0;

                //if (b)
                //{
                //    return true;
                //}

                j = 1;

                return (i & 1) == 0;
            }

            public bool AopTest() => true;

            public T Get<T>() where T : struct => default;

            public Task<T> GetAsync<T>() where T : new()
            {
                return Task.FromResult(new T());
            }
        }

        public class DependencyP : Dependency
        {
            private readonly Dependency dependencyz;

            public DependencyP(Dependency dependencyz)
            {
                this.dependencyz = dependencyz;
            }

            public override bool AopTestByOut(int i, out int j)
            {
                return dependencyz.AopTestByOut(i, out j);
            }
        }

        [DependencyIntercept]
        public interface IDependency<T> where T : class
        {
            T Clone(T obj);

            T Copy(T obj);

            T2 New<T2>() where T2 : T, new();
        }

        public class Dependency<T> : IDependency<T> where T : class
        {
            private static readonly Type __destinationProxyType__;

            public T Clone(T obj)
            {
                //... 克隆的逻辑。
                return obj;
            }

            public T Copy(T obj)
            {
                //... 克隆的逻辑。
                return obj;
            }

            public T2 New<T2>() where T2 : T, new()
            {
                return new T2();
            }

            static Dependency()
            {
                __destinationProxyType__ = typeof(Dependency<T>);
            }
        }

        [TestMethod]
        public void AopTest()
        {
            var services = new ServiceCollection();

            var serviceProvider = services.AddTransient<IDependency, Dependency>()
                 .AddSingleton<Dependency>()
                 .AddTransient(typeof(IDependency<>), typeof(Dependency<>))
                 .UseMiddleware()
                 .BuildServiceProvider();

            IDependency dependency = serviceProvider.GetService<IDependency>();
            IDependency<IDependency> dependency2 = serviceProvider.GetService<IDependency<IDependency>>();

            int j = 10;

            int i = -10;

            j = +i;

            dependency.Flags = true;

            var k = dependency.AopTestByRef(3, ref j);

            var k2 = dependency.AopTestByOut(4, out j);

            var k3 = dependency.Get<long>();

            var k4 = dependency.GetAsync<Dependency>().GetAwaiter().GetResult();

            var dependency3 = dependency2.Clone(dependency);
            var dependency4 = dependency2.Copy(dependency);

            var dependency5 = dependency2.New<Dependency>();

        }
    }
}