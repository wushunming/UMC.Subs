using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Web.UI;
using UMC.Web;
using UMC.Data.Entities;

namespace UMC.Subs.Activities
{
    class SubjectUIMinActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var strId = this.AsyncDialog("Id", g =>
             {
                 return new Web.UITextDialog() { Title = "主题" };
             });
            var form = request.SendValues ?? new UMC.Web.WebMeta();

            if (form.ContainsKey("limit") == false)
            {
                this.Context.Send(new UISectionBuilder(request.Model, request.Command, new UMC.Web.WebMeta().Put("Id", strId))
                    .Builder(), true);
            }
            var ui = UISection.Create();
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            var sid = UMC.Data.Utility.Guid(strId);
            if (sid.HasValue)
            {
                subEntity.Where.And().Equal(new UMC.Data.Entities.Subject { Id = sid });
            }
            else
            {
                var codes = new List<String>(strId.Split('/'));
                switch (codes.Count)
                {
                    case 1:
                        codes.Insert(0, "Help");
                        codes.Insert(0, "UMC");
                        break;
                    case 2:
                        codes.Insert(0, "UMC");
                        break;
                    case 3:
                        break;
                    default:
                        ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "不正确的编码").Put("icon", "\uea0d")
         , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                        response.Redirect(ui);
                        break;
                }

                var team = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project { Code = codes[0] }).Entities.Single();
                if (team == null)
                {
                    ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", String.Format("不存在“{0}”此项目", codes[0])).Put("icon", "\uea0d")
     , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                    response.Redirect(ui);
                }
                subEntity.Where.And().Equal(new Subject { project_id = team.Id });
                var projectItem = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new Data.Entities.ProjectItem
                {
                    Code = codes[1],
                    project_id = team.Id
                }).Entities.Single();
                if (projectItem == null)
                {
                    ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", String.Format("不存在“{0}/{1}”此栏位", team.Code, codes[1])).Put("icon", "\uea0d")
     , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                    response.Redirect(ui);
                }
                subEntity.Where.And().Equal(new Subject { project_item_id = projectItem.Id, Code = codes[2] });
            }


            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);

            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

            //title.Float();

            var sub = subEntity.Single();
            if (sub == null || sub.Visible == -1)
            {

                if (strId.IndexOf("/") > 0)
                {
                    ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", String.Format("未有{0}路径文档", strId)).Put("icon", "\uea0d")
                        , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                }
                else
                {
                    ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "此图文已删除").Put("icon", "\uea0d")
                        , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                }
                response.Redirect(ui);

            }

            var desc = new UIDesc(sub.Title);
            desc.Style.Bold().Size(18).Name("border", "none");
            if (request.IsApp)
            {
                desc.Style.Padding(55, 10, 10, 10);
            }
            else
            {
                desc.Style.Padding(10);

            }
            ui.Add(desc);


            var celss = UMC.Data.JSON.Deserialize<WebMeta[]>((String.IsNullOrEmpty(sub.DataJSON) ? "[]" : sub.DataJSON)) ?? new UMC.Web.WebMeta[] { };
            if (String.Equals("markdown", sub.ContentType, StringComparison.CurrentCultureIgnoreCase))
            {
                foreach (var pom in celss)
                {
                    switch (pom["_CellName"])
                    {
                        case "CMSImage":

                            pom.Put("style", new UIStyle().Padding(10));

                            break;
                    }
                }
            }
            else
            {
                foreach (var pom in celss)
                {
                    switch (pom["_CellName"])
                    {
                        case "CMSImage":
                            var value = pom.GetDictionary()["value"] as Hashtable;
                            if (value != null && value.ContainsKey("size"))
                            {
                                value.Remove("size");
                            }

                            pom.Put("style", new UIStyle().Padding(0, 10));

                            break;
                    }
                }

            }
            ui.DisableSeparatorLine();
            ui.AddCells(celss); ;

            response.Redirect(ui);
        }

    }
}