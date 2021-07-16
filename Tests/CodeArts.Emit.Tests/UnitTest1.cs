using CodeArts.AOP;
using CodeArts.Emit.Expressions;
using CodeArts.Proxies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

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

            var b = method.DeclareVariable(typeof(Expression));

            method.Append(new AssignAst(b, new ConstantAst(GetExpression(entry => entry.Id))));

            method.Append(new ReturnAst(new IfThenElseAst(new BinaryAst(pI, ExpressionType.GreaterThanOrEqual, pJ), pI, pJ)));

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

            var arg = method.DeclareVariable(typeof(ParameterExpression));
            var argProperty = method.DeclareVariable(typeof(MemberExpression));

            var callArg = AstExpression.Call(typeof(Expression).GetMethod(nameof(Expression.Parameter), new Type[] { typeof(Type) }), AstExpression.Constant(type));
            var callProperty = AstExpression.Call(typeof(Expression).GetMethod(nameof(Expression.Property), new Type[] { typeof(Expression), typeof(string) }), arg, AstExpression.Constant("Id"));
            var callBlock = AstExpression.Call(typeof(Expression).GetMethod(nameof(Expression.Block), new Type[] { typeof(IEnumerable<ParameterExpression>), typeof(IEnumerable<Expression>) }));

            method.Append(AstExpression.Assign(arg, callArg));
            method.Append(AstExpression.Assign(argProperty, callProperty));

            var variable_variables = method.DeclareVariable(typeof(ParameterExpression[]));

            var variables = AstExpression.NewArray(1, typeof(ParameterExpression));

            method.Append(AstExpression.Assign(variable_variables, variables));

            method.Append(AstExpression.Assign(AstExpression.ArrayIndex(variable_variables, 0), arg));

            var constantI = AstExpression.Call(typeof(Expression).GetMethod(nameof(Expression.Constant), new Type[] { typeof(object) }), AstExpression.Convert(pI, typeof(object)));

            var equalMethod = AstExpression.Call(typeof(Expression).GetMethod(nameof(Expression.Equal), new Type[] { typeof(Expression), typeof(Expression) }), argProperty, constantI);

            var lamdaMethod = typeof(Tests).GetMethod(nameof(Tests.Lambda))
                .MakeGenericMethod(typeof(Func<Entry, bool>));

            var whereLambda = method.DeclareVariable(typeof(Expression<Func<Entry, bool>>));

            method.Append(AstExpression.Assign(whereLambda, AstExpression.Call(lamdaMethod, equalMethod, variable_variables)));

            method.Append(AstExpression.Void());

            method.Append(AstExpression.Return());

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

        public class DependencyInterceptAttribute : InterceptAttribute
        {
            public override void Run(InterceptContext context, Intercept intercept)
            {
                intercept.Run(context);
            }

            public override Task RunAsync(InterceptAsyncContext context, InterceptAsync intercept)
            {
                return intercept.RunAsync(context);
            }

            public override Task<T> RunAsync<T>(InterceptAsyncContext context, InterceptAsync<T> intercept)
            {
                return intercept.RunAsync(context);
            }
        }

        /// <inheritdoc />
        [DependencyIntercept]
        public interface IDependency
        {
            /// <inheritdoc />
            bool AopTest();

            //[DependencyIntercept]
            bool AopTestByRef(int i, ref int j);

            [DependencyIntercept]
            bool AopTestByOut(int i, out int j);

            T Get<T>() where T : struct;

            Task<T> GetAsync<T>() where T : new();
        }

        /// <inheritdoc />
        public class Dependency : IDependency
        {
            /// <inheritdoc />
            public bool AopTestByRef(int i, ref int j)
            {
                j = i * 5;

                return (i & 1) == 0;
            }

            /// <inheritdoc />
            [DependencyIntercept]
            public bool AopTestByOut(int i, out int j)
            {
                j = i * 5;

                return (i & 1) == 0;
            }

            public bool AopTest() => true;

            public T Get<T>() where T : struct => default;

            public Task<T> GetAsync<T>() where T : new()
            {
                return Task.FromResult(new T());
            }
        }

        public interface IDependency<T> where T : class
        {
            [DependencyIntercept]
            T Clone(T obj);

            T Copy(T obj);
        }

        public class Dependency<T> : IDependency<T> where T : class
        {
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
        }

        [TestMethod]
        public void AopTest()
        {
            var services = new ServiceCollection();

            var serviceProvider = services.AddTransient<IDependency, Dependency>()
                 .AddSingleton<Dependency>()
                 .AddTransient(typeof(IDependency<>), typeof(Dependency<>))
                 .UseAOP()
                 .BuildServiceProvider();

            IDependency dependency = serviceProvider.GetService<IDependency>();
            IDependency<IDependency> dependency2 = serviceProvider.GetService<IDependency<IDependency>>();

            int j = 10;

            var k = dependency.AopTestByRef(3, ref j);

            var k2 = dependency.AopTestByOut(4, out j);

            var k3 = dependency.Get<long>();

            var k4 = dependency.GetAsync<Dependency>().GetAwaiter().GetResult();

            var dependency3 = dependency2.Clone(dependency);
            var dependency4 = dependency2.Copy(dependency);
        }
    }
}