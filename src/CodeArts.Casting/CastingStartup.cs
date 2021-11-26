using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeArts.Casting
{
    /// <summary>
    /// 启动。
    /// </summary>
    public class CastingStartup : IStartup
    {
        /// <summary>
        /// 功能码。
        /// </summary>
        public int Code => 100;

        /// <summary>
        /// 权重。
        /// </summary>
        public int Weight => 1;

        /// <summary>
        /// 启动。
        /// </summary>
        public void Startup() => RuntimeServPools.TryAddSingleton<IMapper, CastingMapper>();
    }
}
