namespace CodeArts
{
    /// <summary>
    /// 主键生成器（默认：雪花算法）
    /// </summary>
    public static class KeyGen
    {
        private static IKeyGen _keyGen;

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static KeyGen() => _keyGen = KeyGenFactory.Create(keyGen => _keyGen = keyGen.Create());

        /// <summary>
        /// 生成主键。
        /// </summary>
        /// <returns></returns>
        public static long Id() => _keyGen.Id();

        /// <summary>
        /// 生成主键。
        /// </summary>
        /// <returns></returns>
        public static Key New() => _keyGen.Create(_keyGen.Id());

        /// <summary>
        /// 生成主键。
        /// </summary>
        /// <returns></returns>
        public static Key Create(long key) => _keyGen.Create(key);
    }
}
