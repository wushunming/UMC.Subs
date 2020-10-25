using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Web;

namespace UMC.Subs.Activities
{



    class SubjectCaseCMSActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var Key = this.AsyncDialog("Key", g =>
            {
                this.Prompt("请传入网址");
                return this.DialogValue("none");
            });

            if (Key.StartsWith("https://") == false && Key.StartsWith("http://") == false)
            {
                this.Prompt("非网址不能转化");
            }
            var user = UMC.Security.Identity.Current;

            if (user.IsAuthenticated == false)
            {
                this.Prompt("先登录，再转码", false);
                response.Redirect("Account", "Login");
            }
            var ns = Key.Substring(Key.IndexOf("://"));

            var locales = UMC.Configuration.ProviderConfiguration.GetProvider(UMC.Data.Utility.MapPath("~/App_Data/UMC/parser.xml"));
            for (var i = 0; i < locales.Count; i++)
            {
                var p = locales[i];
                var prefix = p["prefix"];
                prefix = prefix.Substring(prefix.IndexOf("://"));

                if (ns.StartsWith(prefix))
                {
                    this.Context.Send("OpenUrl"
                        , new UMC.Web.WebMeta().Put("value", Key).Put("caseCMS", true), true);

                }
            }
            this.Prompt("提示", "此站点转码未收集，不能成功抓取；如果您需要收集，请与管理员联系。");


        }

    }
}