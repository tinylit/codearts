using CodeArts.AOP;
using CodeArts.Emit.Expressions;
using CodeArts.Proxies;
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
            var classType = new ClassEmitter(m, "test", TypeAttributes.Public);
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
            var classType = new ClassEmitter(m, "linq", TypeAttributes.Public);
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
        public interface IDependency
        {
            /// <inheritdoc />
            [DependencyIntercept]
            bool AopTest();
        }

        /// <inheritdoc />
        public class Dependency : IDependency
        {
            /// <inheritdoc />
            public bool AopTest() => true;
        }

        [TestMethod]
        public void AopTest()
        {
            var pattern = new ProxyByType(typeof(IDependency), typeof(Dependency), Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient);

            var descriptor = pattern.Resolve();

            IDependency dependency = (IDependency)Activator.CreateInstance(descriptor.ImplementationType);

            dependency.AopTest();
        }
    }
}