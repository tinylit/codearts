using System;

namespace CodeArts
{
    /// <summary>
    /// KeyGen构建器。
    /// </summary>
    public static class KeyGenFactory
    {
        private static IKeyGenFactory _keyGen;

        private static Action<IKeyGenFactory> KeyGenFactoryChanged;


        /// <summary>
        /// 静态构造函数
        /// </summary>
        static KeyGenFactory() => _keyGen = RuntimeServManager.Singleton<IKeyGenFactory, SnowflakeFactory>(keyGen => KeyGenFactoryChanged?.Invoke(_keyGen = keyGen));

        /// <summary>
        /// 生成主键工具箱（请用静态属性或字段接收）。
        /// </summary>
        /// <param name="factoryChanged">工厂改变事件。</param>
        /// <returns></returns>
        public static IKeyGen Create(Action<IKeyGenFactory> factoryChanged)
        {
            KeyGenFactoryChanged += factoryChanged ?? throw new ArgumentNullException(nameof(factoryChanged));

            return _keyGen.Create();
        }
    }
}
