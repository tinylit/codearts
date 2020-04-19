using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Emit
{
    /// <summary>
    /// 构造函数。
    /// </summary>
    public class ConstructorEmitter : IMemberEmitter
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="typeEmitter">类型。</param>
        /// <param name="builder">构造器。</param>
        public ConstructorEmitter(AbstractTypeEmitter typeEmitter, ConstructorBuilder builder)
        {
        }

        public MemberInfo Member => throw new NotImplementedException();

        public Type ReturnType => throw new NotImplementedException();

        public void Emit()
        {
            throw new NotImplementedException();
        }
    }
}
