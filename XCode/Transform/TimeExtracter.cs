﻿using System;
using System.Diagnostics;
using System.Linq;
using NewLife.Log;
using XCode;
using XCode.Configuration;

/*
 *  时间点抽取流程：
 *      验证时间start，越界则退出
 *      抽取一批数据 list = FindAll(UpdateTime >= start, UpdateTime.Asc(), null, Row, 1000)
 *      有数据list.Count > 0
 *          last = list.Last().UpdateTime
 *          满一批，后续还有数据 list.Count == 1000
 *              最大时间行数 lastCount = list.Count(e=>UpdateTime==last)
 *              Row += lastCount
 *              滑动时间点 start = last
 *          不满一批，后续没有数据
 *              Row = 0;
 *              滑动时间点 start = last + 1
 *      返回这一批数据
 */

namespace XCode.Transform
{
    /// <summary>以时间为比较点的数据抽取器</summary>
    public class TimeExtracter : IExtracter
    {
        #region 属性
        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>设置</summary>
        public IExtractSetting Setting { get; set; }

        /// <summary>实体工厂</summary>
        public IEntityOperate Factory { get; set; }

        /// <summary>获取 或 设置 时间字段</summary>
        public String FieldName { get; set; }

        /// <summary>附加条件</summary>
        public String Where { get; set; }

        private FieldItem _Field;
        /// <summary>时间字段</summary>
        public FieldItem Field
        {
            get
            {
                if (_Field == null)
                {
                    _Field = Factory.Table.FindByName(FieldName);
                    if (_Field == null) throw new ArgumentNullException(nameof(Field));
                }
                return _Field;
            }
        }
        #endregion

        #region 构造
        public TimeExtracter()
        {
            Name = GetType().Name.TrimEnd("Extracter", "Worker");
        }
        #endregion

        #region 抽取数据
        /// <summary>抽取一批数据</summary>
        /// <returns></returns>
        public virtual IEntityList Fetch()
        {
            var set = Setting;
            if (set == null) set = Setting = new ExtractSetting();
            if (!set.Enable) return null;

            // 验证时间段
            var start = set.Start;
            var now = DateTime.Now;
            if (start >= now) return null;

            var end = now;
            if (set.End > DateTime.MinValue && end > set.End) end = set.End;

            // 区间无效
            if (start >= end) return null;

            var size = set.BatchSize;
            if (size <= 0) size = 1000;

            // 分批获取数据，如果没有取到，则结束
            var sw = Stopwatch.StartNew();
            var list = FetchData(start, end, set.Row, size);
            sw.Stop();

            // 取到数据，需要滑动窗口
            if (list.Count > 0)
            {
                var last = (DateTime)list.Last()[FieldName];
                // 满一批，后续还有数据
                if (list.Count >= 1000)
                {
                    // 最大时间行数
                    var maxCount = list.Count(e => (DateTime)e[FieldName] == last);
                    // 以最后时间为起点，跳过若干行。注意可能产生连续分页的情况
                    set.Start = last;
                    set.Row += maxCount;
                }
                else
                {
                    set.Start = last.AddSeconds(1);
                    set.Row = 0;
                }
            }

            return list;
        }

        /// <summary>分段分页抽取数据</summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="startRow"></param>
        /// <param name="maxRows"></param>
        /// <returns></returns>
        protected virtual IEntityList FetchData(DateTime start, DateTime end, Int32 startRow, Int32 maxRows)
        {
            var fi = Field;
            var exp = fi.Between(start, end);

            if (!Where.IsNullOrEmpty()) exp &= Where;

            return Factory.FindAll(exp, fi, null, startRow, maxRows);
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}