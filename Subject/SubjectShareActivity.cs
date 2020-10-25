using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Data.Entities;
using UMC.Web;

namespace UMC.Subs.Activities
{
    /// <summary>
    /// 关账
    /// </summary>
    class SubjectShareActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var model = request.Model;
            var proid = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g => this.DialogValue(request.Url.ToString())));
            if (proid.HasValue == false)
            {
                this.Prompt("参数不正确");

            }
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            var sub = subEntity.Where.And().Equal(new Subject { Id = proid }).Entities.Single();
            if (sub == null)
            {
                this.Prompt("参数不正确");

            }
            var Type = this.AsyncDialog("Type", g =>
             {
                 return this.DialogValue("auto");
                 var sheet = new Web.UISheetDialog() { Title = "分享" };
                 sheet.Options.Add(new Web.UIClick("Id", proid.ToString(), "Type", "0") { Command = request.Command, Model = model, Text = "分享微信好友" });
                 sheet.Options.Add(new Web.UIClick("Id", proid.ToString(), "Type", "1") { Command = request.Command, Model = model, Text = "分享到朋友圈" });
                 sheet.Options.Add(new Web.UIClick("Id", proid.ToString(), "Type", "auto") { Command = request.Command, Model = model, Text = "分享到更多应用上" });
                 return sheet;
             });

            var uri = request.Url;
            var hash = new UMC.Web.WebMeta();

            hash["src"] = UMC.Data.WebResource.Instance().ResolveUrl(sub.Id.Value, "1", "0") + "!100";
            hash["title"] = sub.Title;

            var domain = UMC.Data.WebResource.Instance().WebDomain();

            hash["url"] = new Uri(request.Url, String.Format("{0}Page/{1}/UIData/Id/{2}", domain, model, sub.Id)).AbsoluteUri;
            if (String.IsNullOrEmpty(sub.Description))
            {
                hash["title"] = sub.Title;
                hash["desc"] = sub.Description ?? sub.Title;
            }
            else
            {
                hash["desc"] = sub.Description;
            }
            hash["type"] = "Share";
            switch (Type)
            {
                case "0":
                    hash["wx"] = "0";
                    break;
                case "1":
                    hash["wx"] = "1";
                    break;
                case "2":
                    hash["wx"] = "2";
                    break;
            }
            this.Context.Send(hash, true);



            //data["ShareUrl"] = String.Format("http://www.365lu.cn/{0}/.pro?subject={1}", WebADNuke.Data.WebUtility.GetRoot(request.Url), proid);
        }

    }
}