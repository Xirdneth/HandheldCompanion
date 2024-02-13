using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Media;

namespace HandheldCompanion.Managers.Interfaces
{
    public interface ISettingsManager
    {
        bool IsInitialized { get; }

        event SettingsManager.InitializedEventHandler Initialized;
        event SettingsManager.SettingValueChangedEventHandler SettingValueChanged;

        bool GetBoolean(string name, bool temporary = false);
        Color GetColor(string name, bool temporary = false);
        DateTime GetDateTime(string name, bool temporary = false);
        double GetDouble(string name, bool temporary = false);
        int GetInt(string name, bool temporary = false);
        SortedDictionary<string, object> GetProperties();
        string GetString(string name, bool temporary = false);
        StringCollection GetStringCollection(string name, bool temporary = false);
        uint GetUInt(string name, bool temporary = false);
        void SetProperty(string name, object value, bool force = false, bool temporary = false);
        void Start();
        void Stop();
    }
}