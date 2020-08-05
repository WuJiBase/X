﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using XCode.Configuration;
using NewLife;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace XCode.Cache
{
    /// <summary>统计字段缓存</summary>
    /// <typeparam name="TEntity"></typeparam>
    [DisplayName("统计字段")]
    public class FieldCache<TEntity> : EntityCache<TEntity> where TEntity : Entity<TEntity>, new()
    {
        private readonly String _fieldName;
        private FieldItem _field;
        private FieldItem _Unique;

        /// <summary>最大行数。默认50</summary>
        public Int32 MaxRows { get; set; } = 50;

        /// <summary>数据源条件</summary>
        public WhereExpression Where { get; set; }

        /// <summary>获取显示名的委托</summary>
        public Func<TEntity, String> GetDisplay { get; set; }

        /// <summary>显示名格式化字符串，两个参数是名称和个数</summary>
        public String DisplayFormat { get; set; } = "{0} ({1:n0})";

        /// <summary>对指定字段使用实体缓存</summary>
        /// <param name="field"></param>
        [Obsolete("=>FieldCache(String fieldName)")]
        public FieldCache(FieldItem field)
        {
            WaitFirst = false;
            Expire = 10 * 60;
            FillListMethod = () =>
            {
                return Entity<TEntity>.FindAll(Where.GroupBy(_field), _Unique.Desc(), _Unique.Count() & _field, 0, MaxRows);
            };

            _field = field;
            {
                var tb = field.Table;
                var id = tb.Identity;
                if (id == null && tb.PrimaryKeys.Length == 1) id = tb.PrimaryKeys[0];
                _Unique = id ?? throw new Exception("{0}缺少唯一主键，无法使用缓存".F(tb.TableName));
            }
        }

        /// <summary>对指定字段使用实体缓存</summary>
        /// <param name="fieldName"></param>
        public FieldCache(String fieldName)
        {
            WaitFirst = false;
            Expire = 10 * 60;
            FillListMethod = () =>
            {
                return Entity<TEntity>.FindAll(Where.GroupBy(_field), _Unique.Desc(), _Unique.Count() & _field, 0, MaxRows);
            };
            _fieldName = fieldName;
        }

        private void Init()
        {
            if (_field == null || !_fieldName.IsNullOrEmpty())
            {
                _field = Entity<TEntity>.Meta.Table.FindByName(_fieldName);

                var tb = _field.Table;
                var id = tb.Identity;
                if (id == null && tb.PrimaryKeys.Length == 1) id = tb.PrimaryKeys[0];
                _Unique = id ?? throw new Exception("{0}缺少唯一主键，无法使用缓存".F(tb.TableName));
            }
        }

        private Task<Dictionary<String, String>> _task;
        /// <summary>获取所有类别名称</summary>
        /// <returns></returns>
        public IDictionary<String, String> FindAllName()
        {
            Init();

            var key = "{0}_{1}".F(typeof(TEntity).Name, _field?.Name);
            var dc = DataCache.Current;

            if (_task == null || _task.Status == TaskStatus.RanToCompletion) _task = TaskEx.Run(() =>
            {
                //var id = _field.Table.Identity;
                var list = Entities.Take(MaxRows).ToList();

                var dic = new Dictionary<String, String>();
                foreach (var entity in list)
                {
                    var k = entity[_field.Name] + "";
                    var v = k;
                    if (GetDisplay != null)
                    {
                        v = GetDisplay(entity);
                        if (v.IsNullOrEmpty()) v = "[{0}]".F(k);
                    }

                    dic[k] = DisplayFormat.F(v, entity[_Unique.Name]);
                }

                // 更新缓存
                if (dic.Count > 0)
                {
                    dc.FieldCache[key] = dic;
                    dc.SaveAsync();
                }

                _task = null;

                return dic;
            });

            // 优先从缓存读取
            if (dc.FieldCache.TryGetValue(key, out var rs)) return rs;

            return _task.Result;
        }

        #region 辅助
        /// <summary>输出名称</summary>
        /// <returns></returns>
        public override String ToString()
        {
            var type = GetType();
            return "{0}<{1}>[{2}]".F(type.GetDisplayName() ?? type.Name, typeof(TEntity).FullName, _field.Name);
        }
        #endregion
    }
}