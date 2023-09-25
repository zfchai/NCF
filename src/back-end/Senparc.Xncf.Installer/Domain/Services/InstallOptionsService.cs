﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.Areas.Admin.Domain;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Log;
using Senparc.Xncf.Installer.Domain.Dto;
using Senparc.Xncf.Installer.OHS.Local.AppService;
using Senparc.Xncf.Instraller.Pages;
using Senparc.Xncf.XncfBuilder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Senparc.Xncf.Installer.Domain.Services
{
    public class InstallOptionsService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SenparcCoreSetting  _senparcCoreSetting;
        public InstallOptions Options { get; set; }
        public InstallOptionsService(IServiceProvider serviceProvider, AdminUserInfoService _accountInfoService, 
            IOptions<SenparcCoreSetting> senparcCoreSetting)
        {
            this._serviceProvider = serviceProvider;
            this._senparcCoreSetting = senparcCoreSetting.Value;

            //初始化Options的默认值
            Options = new InstallOptions();
            Options.DbConnectionString = GetDbConnectionString();
        }

        /// <summary>
        /// 读取配置中目标数据库连接字符串
        /// </summary>
        /// <returns></returns>
        public string GetDbConnectionString()
        {
            //string dbConfigName = SenparcDatabaseConnectionConfigs.GetFullDatabaseName(_senparcCoreSetting.DatabaseName);
            return SenparcDatabaseConnectionConfigs.ClientConnectionString;
        }

        /// <summary>
        /// 修改配置及缓存中目标数据库连接字符串
        /// </summary>
        public void ResetDbConnectionString()
        {
            if (Options.DbConnectionString == GetDbConnectionString())
                return;

            string dbConfigName = SenparcDatabaseConnectionConfigs.GetFullDatabaseName(_senparcCoreSetting.DatabaseName);

            //清空数据库配置缓存
            MethodCache.ClearMethodCache<ConcurrentDictionary<string, SenparcConfig>>(SenparcDatabaseConnectionConfigs.SENPARC_CONFIG_KEY);

            //修改数据库连接字符串
            Func<ConcurrentDictionary<string, SenparcConfig>> func = () =>
            {
                ConcurrentDictionary<string, SenparcConfig> configs = new ConcurrentDictionary<string, SenparcConfig>();
                try
                {
                    XmlDataContext xmlCtx = new XmlDataContext(SiteConfig.SenparcConfigDirctory);
                    var list = xmlCtx.GetXmlList<SenparcConfig>();
                    list.ForEach(z => configs[z.Name] = z);
                    foreach (var item in list)
                    {
                        if (item.Name == dbConfigName)
                        {
                            item.ConnectionStringFull = Options.DbConnectionString;
                            configs[dbConfigName].ConnectionStringFull = Options.DbConnectionString;
                            break;
                        }
                    }
                    xmlCtx.Save<SenparcConfig>(list);
                }
                catch (Exception e)
                {
                    Console.WriteLine("=== NCF === 修改数据库配置错误：" + e.ToString());
                    LogUtility.WebLogger.ErrorFormat("SenparcConfigs.Configs 修改错误：" + e.Message, e);
                }
                return configs;
            };

            //刷新数据库配置缓存
            _ = MethodCache.GetMethodCache(SenparcDatabaseConnectionConfigs.SENPARC_CONFIG_KEY, func, 60 * 999);
        }
    }
}
