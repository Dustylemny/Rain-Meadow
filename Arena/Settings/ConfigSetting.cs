using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Ionic.Zlib.DeflateManager;

namespace RainMeadow.Arena.Settings
{
    public class ConfigSetting<T> : GenericSetting<T, ConfigSetting<T>>
    {
        [JsonIgnore]
        private Configurable<T>? configurable;
        public ConfigSetting() : base()
        {
        }
        public ConfigSetting(Configurable<T> config) : this(config.key, config)
        {
        }
        public ConfigSetting(string id, Configurable<T> config) : base(id, config.Value)
        {
            configurable = config;
        }
        public override void StoreValueSomewhere()
        {
            configurable?.Value = CurrentValue;
        }
    }
}
