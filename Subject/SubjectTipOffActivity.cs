using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Data.Entities;
using UMC.Web;

namespace UMC.Subs.Activities
{
    /// <summary>
    /// 举报
    /// </summary>
    class SubjectTipOffActivity : WebActivity
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
                 //return this.DialogValue("auto");
                 var sheet = new Web.UISheetDialog() { Title = "更多" };//屏蔽
                 sheet.Options.Add(new Web.UIClick("Id", proid.ToString(), "Type", "Share") { Command = request.Command, Model = model, Text = "分享到应用" });
                 //sheet.Options.Add(new Web.UIClick("Id", proid.ToString(), "Type", "0") { Command = request.Command, Model = model, Text = "屏蔽此作者" });
                 //sheet.Options.Add(new Web.UIClick("Id", proid.ToString(), "Type", "1") { Command = request.Command, Model = model, Text = "屏蔽此项目" });
                 sheet.Options.Add(new Web.UIClick("Id", proid.ToString(), "Type", "Off") { Command = request.Command, Model = model, Text = "举报内容" });
                 return sheet;
             });
            var user = Security.Identity.Current;
            switch (Type)
            {
                case "Off":
                    var frm = this.AsyncDialog("Settings", g =>
                     {
                         var fm = new UIFormDialog();
                         fm.Title = "举报";
                         fm.AddRadio("违规性质", "Type").Put("内容质量差").Put("旧闻重复").Put("内容不实").Put("标题夸张").Put("低俗色情")
                         .Put("其他问题").Put("侵犯名誉/商誉/隐私/肖像权");
                         fm.AddFile("证明材料", "Url", "");
                         fm.AddTextarea("举报描述", "Description", "");
                         return fm;
                     });
                    var url = new Uri(frm["Url"]);

                    var s = "UserResources/TipOff" + url.AbsolutePath.Substring(url.AbsolutePath.IndexOf('/', 2));//
                    Data.WebResource.Instance().Transfer(url, s);
                    var t = new SubjectTipOff();
                    Data.Reflection.SetProperty(t, frm.GetDictionary());
                    t.user_id = user.Id;
                    t.Url = Data.WebResource.Instance().ResolveUrl(s);
                    t.sub_id = sub.Id;
                    t.CreationTime = DateTime.Now;
                    Utility.CMS.ObjectEntity<SubjectTipOff>().Where.And().Equal(new SubjectTipOff { user_id = user.Id, sub_id = sub.Id })
                        .Entities.IFF(e => e.Update(t) == 0, e => e.Insert(t));
                    if (sub.TipOffs.HasValue)
                    {
                        subEntity.Update("{0}+{1}", new Subject { TipOffs = 1 });
                    }
                    else
                    {
                        subEntity.Update(new Subject { TipOffs = 1 });

                    }
                    this.Prompt("举报成功，我们将尽快处理");
                    break;
                case "Share":
                    response.Redirect(request.Model, "Share", proid.ToString());
                    break;
                default:
                case "Block":

                    var ui = this.AsyncDialog("UI", g => this.DialogValue("none"));
                    var section = this.AsyncDialog("section", g => this.DialogValue("1"));
                    var row = this.AsyncDialog("row", g => this.DialogValue("1"));
                    var frm2 = Utility.IntParse(this.AsyncDialog("Settings", g =>
                    {
                        if (Utility.IntParse(Type, -1) > -1)
                        {
                            return this.DialogValue(Type);
                        }
                        var fm = new UISelectDialog();
                        fm.Title = "屏蔽";
                        fm.Options.Put("屏蔽此作者", "0").Put("屏蔽此项目", "1");
                        return fm;
                    }), 0);
                    var block = new ProjectBlock { user_id = user.Id, ref_id = sub.user_id, Type = frm2 };
                    switch (frm2)
                    {
                        case 0:
                            block.ref_id = sub.user_id;
                            break;
                        case 1:
                            block.ref_id = sub.project_id;
                            break;
                    }
                    if (block.ref_id.HasValue)
                    {
                        Utility.CMS.ObjectEntity<ProjectBlock>().Where.And().Equal(block)
                       .Entities.IFF(e => e.Count() == 0, e => e.Insert(block));
                    }
                    if (ui == "none")
                    {
                        this.Prompt("屏蔽成功");
                    }
                    else
                    {
                        var editer = new Web.UISection.Editer(section, row, ui);
                        editer.Delete();
                        editer.ReloadSinle();
                        editer.Builder(this.Context, ui, true);
                    }
                    break;
            }
        }

    }
}