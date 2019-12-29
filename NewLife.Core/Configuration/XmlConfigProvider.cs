﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace NewLife.Configuration
{
    /// <summary>Xml文件配置提供者</summary>
    /// <remarks>
    /// 支持从不同配置文件加载到不同配置模型
    /// </remarks>
    public class XmlConfigProvider : FileConfigProvider
    {
        /// <summary>读取配置文件，得到字典</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected override IDictionary<String, String> OnRead(String fileName)
        {
            using var fs = File.OpenRead(fileName);
            using var reader = XmlReader.Create(fs);

            var dic = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);

            reader.ReadStartElement();

            return dic;
        }

        /// <summary>把字典写入配置文件</summary>
        /// <param name="fileName"></param>
        /// <param name="source"></param>
        protected override void OnWrite(String fileName, IDictionary<String, String> source)
        {
            throw new NotImplementedException();
        }
    }
}