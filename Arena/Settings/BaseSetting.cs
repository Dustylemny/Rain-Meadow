using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.Arena.Settings
{
    public class BaseSetting
    {
        [JsonIgnore]
        public string id;
        public BaseSetting() : this("", null)
        {

        }
        public BaseSetting(string id, object value)
        {
            this.id = id;
            if (IsValueValid(value))
                SetValue(value);
        }
        public void TrySetValue(object value)
        {
            if (IsValueValid(value))
                SetValue(value);
        }
        public void TryLoadValueToConnected()
        {
            if (CanLoadValueToConnected())
                LoadValueOntoConnected();
        }
        public virtual void LoadConnectedOntoValue()
        {

        }
        public virtual bool CanLoadValueToConnected() => true;
        protected virtual void LoadValueOntoConnected()
        {

        }
        public virtual void CombineWithLoadedSetting(BaseSetting savedSetting)
        {

        }
        public virtual void UpdateWithSavedSetting(BaseSetting savedSetting)
        {
        }
        protected virtual bool IsValueValid(object val) => true;
        public virtual void StoreValueSomewhere()
        {

        }
        protected virtual void SetValue(object value)
        {

        }
    }
}
