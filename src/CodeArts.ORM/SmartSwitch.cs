using System;

namespace CodeArts.ORM
{
    /// <summary>
    /// 智能开关
    /// </summary>
    internal class SmartSwitch
    {
        public bool IsFirst { get; private set; }

        public Action FirstAction { get; }

        public Action UnFirstAction { get; }

        public SmartSwitch(Action firstAction, Action unFirstAction)
        {
            IsFirst = true;
            FirstAction = firstAction;
            UnFirstAction = unFirstAction;
        }

        public void UnWrap(Action action)
        {
            bool isFirst = IsFirst;

            IsFirst = true;

            action?.Invoke();

            IsFirst = isFirst;
        }

        public void Execute()
        {
            if (IsFirst)
            {
                IsFirst = false;
                FirstAction?.Invoke();
                return;
            }
            UnFirstAction?.Invoke();
        }
    }
}
