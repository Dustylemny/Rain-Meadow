using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.Arena.Settings
{
    public class Settings<T> : GenericSetting<T, Settings<T>>
    {
        public event Action<T, Settings<T>> OnStoreCurrentValue;
        public Settings() : this("")
        {

        }
        public Settings(string id) : this(id, default)
        {

        }
        public Settings(string id, T value) : base(id, value)
        {
        }
        public override void StoreValueSomewhere()
        {
            OnStoreCurrentValue?.Invoke(CurrentValue, this);
        }
    }
}
