﻿#region Copyright (C) 2016 Kevin (OS系列开源项目)

/***************************************************************************
*　　	文件功能描述：公号的功能接口基类，获取AccessToken ，获取微信服务器Ip列表
*
*　　	创建人： Kevin
*       创建人Email：1985088337@qq.com
*    	创建日期：2016   忘记哪一天
*       
*****************************************************************************/

#endregion

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OSS.Common.ComModels;
using OSS.Common.Modules;
using OSS.Common.Modules.CacheModule;
using OSS.Http.Mos;
using OSS.Social.WX.Offcial.Basic.Mos;
using OSS.Social.WX.SysUtils;

namespace OSS.Social.WX.Offcial
{
    /// <summary>
    /// 微信公号接口基类
    /// </summary>
    public class WxOffBaseApi:WxBaseApi
    {
        private readonly string m_OffcialAccessTokenKey;
   
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config"></param>
        public WxOffBaseApi(WxAppCoinfig config) : base(config)
        {
            m_OffcialAccessTokenKey = string.Format(WxCacheKeysUtil.OffcialAccessTokenKey, ApiConfig.AppId);
        }
       
        /// <summary>
        /// 获取微信服务器列表
        /// </summary>
        /// <returns></returns>
        public async Task<WxIpListResp> GetWxIpListAsync()
        {
            var req = new OsHttpRequest();

            req.HttpMothed = HttpMothed.GET;
            req.AddressUrl = string.Concat(m_ApiUrl, "/cgi-bin/getcallbackip");

            return await RestCommonOffcialAsync<WxIpListResp>(req);
        }

        #region  基础方法

        /// <summary>
        ///   获取公众号的AccessToken
        /// </summary>
        /// <returns></returns>
        public async Task<WxOffAccessTokenResp> GetAccessTokenAsync()
        {
            var tokenResp = CacheUtil.Get<WxOffAccessTokenResp>(m_OffcialAccessTokenKey, ModuleNames.SocialCenter);

            if (tokenResp == null || tokenResp.expires_date < DateTime.Now)
            {
                OsHttpRequest req = new OsHttpRequest();

                req.AddressUrl = $"{m_ApiUrl}/cgi-bin/token?grant_type=client_credential&appid={ApiConfig.AppId}&secret={ApiConfig.AppSecret}";
                req.HttpMothed = HttpMothed.GET;

                tokenResp =await RestCommon<WxOffAccessTokenResp>(req);

                if (!tokenResp.IsSuccess)
                    return tokenResp;

                tokenResp.expires_date = DateTime.Now.AddSeconds(tokenResp.expires_in - 600);

                CacheUtil.AddOrUpdate(m_OffcialAccessTokenKey, tokenResp, TimeSpan.FromSeconds(tokenResp.expires_in), null, ModuleNames.SocialCenter);
            }

            return tokenResp;
        }

        /// <summary>
        ///   公众号主要Rest请求接口封装
        ///      主要是预处理accesstoken赋值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="req"></param>
        /// <param name="funcFormat"></param>
        /// <returns></returns>
        protected async Task<T> RestCommonOffcialAsync<T>(OsHttpRequest req,
            Func<HttpResponseMessage, Task<T>> funcFormat = null)
            where T : WxBaseResp, new()
        {
            var tokenRes = await GetAccessTokenAsync();
            if (!tokenRes.IsSuccess)
                return tokenRes.ConvertToResult<T>();

            req.AddressUrl = string.Concat(req.AddressUrl, req.AddressUrl.IndexOf('?') > 0 ? "&" : "?", "access_token=",
                tokenRes.access_token);
            req.RequestSet = reqMsg => reqMsg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return await RestCommon<T>(req, funcFormat);
        }

        #endregion

        /// <summary>
        ///   下载文件方法
        /// </summary>
        protected static readonly Func<HttpResponseMessage, Task<WxFileResp>> DownLoadFileAsync = async resp =>
        {
            var contentStr = resp.Content.Headers.ContentType.MediaType;
            if (!contentStr.Contains("application/json"))
                return new WxFileResp() { content_type = contentStr, file = await resp.Content.ReadAsByteArrayAsync() };
            return JsonConvert.DeserializeObject<WxFileResp>(await resp.Content.ReadAsStringAsync());
        };

    }
}
