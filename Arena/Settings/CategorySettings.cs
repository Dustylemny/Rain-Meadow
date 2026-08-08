using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.Arena.Settings
{
    public class CategorySettings : BaseSetting
    {
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.All)]
        public Dictionary<string, BaseSetting> settings = [];

        public CategorySettings() : this("")
        {

        }
        public CategorySettings(string category) : this(category, [])
        {

        }
        public CategorySettings(string category, Dictionary<string, BaseSetting> settings) : base(category, settings)
        {
            this.settings = settings;
        }
        public override void UpdateWithSavedSetting(BaseSetting savedSetting)
        {
            if (savedSetting is not CategorySettings categorySettings) return;
            List<string> toRemove = [];
            foreach (var setting in settings)
            {
                if (!categorySettings.settings.ContainsKey(setting.Key))
                    toRemove.Add(setting.Key);
                else 
                    setting.Value.UpdateWithSavedSetting(categorySettings.settings[setting.Key]);
            }
            for (int i = 0; i <  toRemove.Count; i++)
                settings.Remove(toRemove[i]);
        }
        public override void CombineWithLoadedSetting(BaseSetting savedSetting)
        {
            if (savedSetting is not CategorySettings categorySettings) return;
            List<string> toAdd = [];
            foreach (var setting in categorySettings.settings)
            {
                if (settings.ContainsKey(setting.Key))
                    settings[setting.Key].CombineWithLoadedSetting(setting.Value);
                else
                    toAdd.Add(setting.Key);
            }
            for (int i = 0;i < toAdd.Count;i++)
                settings.Add(toAdd[i], categorySettings.settings[toAdd[i]]);
        }
        protected override void LoadValueOntoConnected()
        {
            foreach (var item in settings)
                item.Value.TryLoadValueToConnected();
        }
        public override void StoreValueSomewhere()
        {
            foreach (var item in settings)
                item.Value.StoreValueSomewhere();
        }
        protected override void SetValue(object value)
        {
            //No
        }
        protected override bool IsValueValid(object val) => false;
        public bool TryLoadOrAddSetting<T>(ref T setting) where T : BaseSetting
        {
            if (settings.TryGetValue(setting.id, out BaseSetting val) && val is T t)
            {
                if (setting == null)
                    setting = t;
                else setting.CombineWithLoadedSetting(t);
                    return true;
            }
            else TryAddSettings(setting);
            return false;
        }
        public bool TryGetSetting<T>(string id, out T settings) where T : BaseSetting
        {
            settings = null;
            if (this.settings.TryGetValue(id, out BaseSetting val) && val is T t)
            {
                settings = t;
            }
            return settings != null;
        }
        public void TryAddSettings(params BaseSetting[] settings)
        {
            for (int i = 0; i < settings.Length; i++)
            {
                if (!this.settings.ContainsKey(settings[i].id))
                    this.settings.Add(settings[i].id, settings[i]);
            }
        }
        public bool TrySetSettingToConnected(string id)
        {
            if (settings.TryGetValue(id, out var setting))
            {
                setting.LoadConnectedOntoValue();
                return true;
            }
            return false;
        }
        public bool TrySetSettingToConnected(ConfigurableBase config) => TrySetSettingToConnected(config.key);
        public bool TrySetSetting(string id, object newVal)
        {
            if (settings.TryGetValue(id, out var setting))
            {
                setting.TrySetValue(newVal);
                return true;
            }
            return false;
        }
        public bool TrySetSetting(ConfigurableBase config, object newVal) => TrySetSetting(config.key, newVal);
    }
}
