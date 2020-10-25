using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using UMC.Web;

namespace UMC.Subs.Activities
{
    class SubjectUISettingActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var subId = UMC.Data.Utility.Guid(this.AsyncDialog("Id", request.Model, "Select"));
            this.AsyncDialog("Sheet", s =>
             {
                 var d = new UMC.Web.UISheetDialog();
                 d.Title = "文档设置";
                 d.Options.Add(new UIClick(subId.ToString()) { Text = "移致" }.Send(request.Model, "PortfolioChange"));
                 d.Options.Add(new UIClick("Id", subId.ToString(), "Model", "Del") { Text = "删除" }.Send(request.Model, "EditUI"));
                 return d;
             });
        }

    }
}