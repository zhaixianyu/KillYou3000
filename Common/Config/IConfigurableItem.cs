using System;
using System.Collections.Generic;

namespace KillYou3000.Common.Config
{
    /// <summary>
    /// 可配置物品接口，允许物品通过统一 UI 修改配置
    /// </summary>
    public interface IConfigurableItem
    {
        /// <summary>物品显示名称（用于 UI 标题）</summary>
        string ConfigTitle { get; }
        
        /// <summary>获取所有配置字段</summary>
        List<ConfigField> GetConfigFields();
        
        /// <summary>配置改变时的回调（用于多人游戏同步）</summary>
        void OnConfigChanged();
    }

    /// <summary>
    /// 配置字段定义
    /// </summary>
    public class ConfigField
    {
        /// <summary>字段标签（显示在 UI 中）</summary>
        public string Label;
        
        /// <summary>字段唯一标识（用于网络同步）</summary>
        public string Key;
        
        /// <summary>字段类型（double/int/bool）</summary>
        public ConfigFieldType FieldType;
        
        /// <summary>当前值</summary>
        public object Value;
        
        /// <summary>值改变时的回调</summary>
        public Action<object> OnChange;
    }

    /// <summary>
    /// 配置字段类型
    /// </summary>
    public enum ConfigFieldType
    {
        Double,
        Int,
        Bool
    }
}
