using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.Arena.Settings
{
    public abstract class GenericSetting<T, Setting> : BaseSetting where Setting : GenericSetting<T, Setting>
    {
        public event Action<T, Setting> LoadFromSetting;
        public event Func<Setting, T> LoadToSetting;
        [JsonIgnore]
        public bool isOwnerSetting = true;
        public T value;
        [JsonIgnore]
        public bool CanUpdateOrLoadValue => !isOwnerSetting || OnlineManager.lobby?.isOwner == true;
        [JsonIgnore]
        public T CurrentValue
        {
            get => value;
            set
            {
                if (CanUpdateOrLoadValue)
                    this.value = value;
            }
        }
        public GenericSetting(): this("")
        {

        }
        public GenericSetting(string id) : this(id, default)
        {

        }
        public GenericSetting(string id, T value) : base(id, value)
        {
            this.value = value;
        }
        protected override bool IsValueValid(object val) => val is T;
        protected override void SetValue(object value)
        {
            CurrentValue = (T)value;
        }
        public override bool CanLoadValueToConnected() => CanUpdateOrLoadValue;
        protected override void LoadValueOntoConnected()
        {
            LoadFromSetting.Invoke(CurrentValue, (Setting)this);
        }
        public override void LoadConnectedOntoValue()
        {
            CurrentValue = LoadToSetting.Invoke((Setting)this);
        }
        public override void UpdateWithSavedSetting(BaseSetting savedSetting)
        {
            if (savedSetting is not GenericSetting<T, Setting> setting) return;
            CurrentValue = setting.CurrentValue;
        }
        public override void CombineWithLoadedSetting(BaseSetting loadedSetting)
        {
            if (loadedSetting is not GenericSetting<T, Setting> setting) return;
            value = setting.CurrentValue;
        }
    }
}
