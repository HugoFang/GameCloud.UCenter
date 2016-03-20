﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
using UCenter.Common.Portable;
using System.ComponentModel.Composition;
using System.ServiceModel.Security;
using System.Threading;
using NLog;
using UCenter.Common;
using UCenter.CouchBase.Database;
using UCenter.CouchBase.Entities;

namespace UCenter.Web.ApiControllers
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [RoutePrefix("api/app")]
    [TraceExceptionFilter("AppApiController")]
    public class AppApiController : ApiControllerBase
    {
        //---------------------------------------------------------------------
        private Logger logger = LogManager.GetCurrentClassLogger();

        //---------------------------------------------------------------------
        [ImportingConstructor]
        public AppApiController(CouchBaseContext db)
            : base(db)
        {
        }

        //---------------------------------------------------------------------
        [HttpPost]
        [Route("create")]
        public async Task<IHttpActionResult> Create([FromBody]AppInfo info)
        {
            logger.Info("创建App\nAppId={0}", info.AppId);

            var appEntity = new AppEntity()
            {
                AppId = info.AppId,
                AppSecret = info.AppSecret
            };

            await this.db.Bucket.InsertSlimAsync<AppEntity>(appEntity);
            var response = new AppResponse()
            {
                AppId = info.AppId,
                AppSecret = info.AppSecret
            };
            return CreateSuccessResult(response);
        }

        //---------------------------------------------------------------------
        [HttpPost]
        [Route("login")]
        public async Task<IHttpActionResult> Login(AppLoginInfo info)
        {
            logger.Info("App请求登录\nAppId={0}", info.AppId);

            var app = await this.db.Bucket.FirstOrDefaultAsync<AppEntity>(a => a.AppId == info.AppId && a.AppSecret == info.AppSecret);

            if (app == null)
            {
                return CreateErrorResult(UCenterErrorCode.AppLoginFailedNotExit, "App does not exist.");
            }

            app.Token = EncryptHashManager.GenerateToken();
            await this.db.Bucket.UpsertSlimAsync(app);

            //todo: need login record?
            return CreateSuccessResult(app);
        }

        //---------------------------------------------------------------------
        [HttpPost]
        [Route("verifyaccount")]
        public async Task<IHttpActionResult> AppVerifyAccount(AppVerifyAccountInfo info)
        {
            var result = new AppVerifyAccountResponse();

            var appAuthResult = await AuthApp(info.AppId, info.AppSecret);
            if (appAuthResult == UCenterErrorCode.AppLoginFailedNotExit)
            {
                return CreateErrorResult(UCenterErrorCode.AppLoginFailedNotExit, "App does not exist");

            }
            else if (appAuthResult == UCenterErrorCode.AppLoginFailedSecretError)
            {
                return CreateErrorResult(UCenterErrorCode.AppLoginFailedSecretError, "App secret incorrect");
            }

            var account = await db.Bucket.FirstOrDefaultAsync<AccountEntity>(a => a.AccountName == info.AccountName);
            if (account == null)
            {
                return CreateErrorResult(UCenterErrorCode.AccountLoginFailedNotExist, "Account does not exist");
            }

            result.AccountId = account.Id;
            result.AccountName = account.AccountName;
            result.AccountToken = account.Token;
            result.LastLoginDateTime = account.LastLoginDateTime;
            result.LastVerifyDateTime = DateTime.UtcNow;

            return CreateSuccessResult(result);
        }

        //---------------------------------------------------------------------
        [HttpPost]
        [Route("readdata")]
        public async Task<IHttpActionResult> AppReadData(AppDataInfo info)
        {
            logger.Info("App请求读取AppData\nAppId={0}", info.AppId);

            var appAuthResult = await AuthApp(info.AppId, info.AppSecret);
            if (appAuthResult == UCenterErrorCode.AppLoginFailedNotExit)
            {
                return CreateErrorResult(UCenterErrorCode.AppLoginFailedNotExit, "App does not exist");
            }
            if (appAuthResult == UCenterErrorCode.AppLoginFailedSecretError)
            {
                return CreateErrorResult(UCenterErrorCode.AppLoginFailedSecretError, "App secret incorrect");
            }

            // todo: && d.AccountName == info.AccountName
            var result = await db.Bucket.FirstOrDefaultAsync<AppDataEntity>(d => d.AppId == info.AppId );

            return CreateSuccessResult(result);
        }

        //---------------------------------------------------------------------
        [HttpPost]
        [Route("writedata")]
        public async Task<IHttpActionResult> AppWriteData(AppDataInfo info)
        {
            logger.Info("App请求写入AppData\nAppId={0}", info.AppId);

            var appAuthResult = await AuthApp(info.AppId, info.AppSecret);
            if (appAuthResult == UCenterErrorCode.AppLoginFailedNotExit)
            {
                return CreateErrorResult(UCenterErrorCode.AppLoginFailedNotExit, "App does not exist");
            }
            if (appAuthResult == UCenterErrorCode.AppLoginFailedSecretError)
            {
                return CreateErrorResult(UCenterErrorCode.AppLoginFailedSecretError, "App secret incorrect");
            }

            // todo: && d.AccountName == info.AccountName
            var appData = await db.Bucket.FirstOrDefaultAsync<AppDataEntity>(d => d.AppId == info.AppId );
            if (appData == null)
            {
                appData = new AppDataEntity()
                {
                    AppId = info.AppId,
                    //AccountName = info.AccountName,
                    Data = info.Data
                };
            }

            await db.Bucket.UpsertSlimAsync<AppDataEntity>(appData);

            return CreateSuccessResult(appData);
        }

        //---------------------------------------------------------------------
        private async Task<UCenterErrorCode> AuthApp(string appId, string appSecret)
        {
            var app = await this.db.Bucket.FirstOrDefaultAsync<AppEntity>(a => a.AppId == appId);
            if (app == null)
            {
                return UCenterErrorCode.AppLoginFailedNotExit;
            }
            if (appSecret != app.AppSecret)
            {
                return UCenterErrorCode.AppLoginFailedSecretError;
            }

            return UCenterErrorCode.Success;
        }
    }
}
