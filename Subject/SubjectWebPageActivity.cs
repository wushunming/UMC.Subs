using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Web;

namespace UMC.Subs.Activities
{



    class SubjectWebPageActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {
                if (request.IsApp)
                {
                    response.Redirect("Account", "Login");
                }
                else
                {
                    this.Prompt("请登录");
                }
            }
            var Key = this.AsyncDialog("Key", g =>
            {
                this.Prompt("请传入网址");
                return this.DialogValue("none");
            });
            var ui = this.AsyncDialog("UI", "none");
            var type = this.AsyncDialog("Model", g =>
            {
                var args = request.Arguments.GetDictionary();
                var sel = new UMC.Web.UISheetDialog() { Title = "分享" };

                sel.Options.Add(new Web.UIClick(new UMC.Web.WebMeta(args).Put(g, "1"))
                {
                    Text = "截屏转发",
                    Command = request.Command,
                    Model = request.Model
                });
                sel.Options.Add(new Web.UIClick(new UMC.Web.WebMeta(args).Put(g, "2"))
                {
                    Text = "抓取此网页图文",
                    Command = request.Command,
                    Model = request.Model
                });

                return sel;
            });
            switch (type)
            {
                case "1":
                    var qrurl = UMC.Data.Utility.QRUrl(Key);
                    this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Share", ui, new UMC.Web.WebMeta().Put("qrurl", qrurl)), true);
                    break;
                case "2":
                    break;
            }


            var locales = UMC.Configuration.ProviderConfiguration.GetProvider(UMC.Data.Utility.MapPath("~/App_Data/UMC/parser.xml"));
            for (var i = 0; i < locales.Count; i++)
            {
                var p = locales[i];
                if (Key.StartsWith(p["prefix"]))
                {
                    var src = p["src"];
                    if (String.IsNullOrEmpty(src) == false)
                    {
                        this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Parse", ui
                            , new UMC.Web.WebMeta().Put("src", src, "title", p["title"], "content", p["content"]).Put("js", p["js"]).Put("nslt", p["nslt"])), true);

                    }
                    else
                    {

                        this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Parse", ui
                            , new UMC.Web.WebMeta().Put("title", p["title"], "content", p["content"]).Put("js", p["js"]).Put("nslt", p["nslt"])), true);

                    }

                }
            }
            this.Prompt("提示", "此站点转码未收集，不能成功抓取；如果您需要收集，请与管理员联系。");


        }

    }
}