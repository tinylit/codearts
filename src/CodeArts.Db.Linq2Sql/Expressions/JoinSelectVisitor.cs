using System;
using System.Reflection;

namespace CodeArts.Db.Expressions
{
    /// <summary>
    /// Join (Select //...)
    /// </summary>
    public sealed class JoinSelectVisitor : SelectVisitor
    {
        private JoinSelectVisitor(BaseVisitor visitor) : base(visitor, true)
        {

        }

        /// <inheritdoc />
        public JoinSelectVisitor(JoinVisitor visitor) : base(visitor, true)
        {
        }

        /// <inheritdoc />
        public override SelectVisitor CreateInstance(BaseVisitor baseVisitor) => new JoinSelectVisitor(baseVisitor);

        /// <inheritdoc />
        protected override void DefMemberAs(string field, string alias)
        {
        }

        /// <inheritdoc />
        protected override void DefNewMemberAs(MemberInfo memberInfo, Type memberOfHostType)
        {
            if (memberInfo.ReflectedType != memberOfHostType && memberInfo.DeclaringType != memberOfHostType)
            {
                writer.As(memberInfo.Name);
            }
        }
    }
}
