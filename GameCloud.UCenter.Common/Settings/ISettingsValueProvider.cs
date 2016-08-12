﻿using System.Collections.Generic;

namespace GameCloud.UCenter.Common.Settings
{
    public interface ISettingsValueProvider
    {
        ICollection<SettingsValuePair> SettingValues { get; }
    }
}