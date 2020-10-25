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

    class SubjectUIDataActivity : WebActivity
    {
        void Subject(String model, UISection ui, UMC.Data.Entities.Subject sub, Project project, bool isEditer)
        {
            var webr = UMC.Data.WebResource.Instance();
            var user = UMC.Security.Identity.Current;
            if (project != null)
            {
                bool isIsAttention;

                var attent = new UMC.Web.WebMeta().Put("desc", project.Description ?? "未写描述", "name", project.Caption)
                    .Put("src", webr.ImageResolve(project.Id.Value, "1", 4));

                var desc2 = UICell.Create("IconNameDesc", attent);
                if (model == "Subject")
                {
                    attent.Put("button", SubjectAttentionActivity.Attention(sub.project_id.Value, out isIsAttention))
                        .Put("button-click", Web.UIClick.Click(new Web.UIClick("Id", project.Id.ToString()) { Model = model, Command = "Attention" }))
                        .Put("button-color", isIsAttention ? "#25b864" : "#e67979");

                    desc2.Format.Put("button", "{button}");
                    if (isIsAttention == false)
                    {
                        desc2.Style.Fixed();

                    }
                }
                attent.Put("click", new Web.UIClick("Id", project.Id.ToString()) { Model = model, Command = "ProjectUI" });

                desc2.Style.Name("desc", new UIStyle().Color(0x999)).Name("name", new UIStyle().Bold());

                desc2.Style.Name("border", "none");

                ui.Title.Name("text", project.Caption);

                ui.Title.Name("src", webr.ImageResolve(project.Id.Value, "1", 4));

                ui.Add(desc2);
                if (user.IsAuthenticated)
                {
                    UMC.Data.Database.Instance().ObjectEntity<UMC.Data.Entities.ProjectAccess>()
                   .Where.And().Equal(new UMC.Data.Entities.ProjectAccess
                   {
                       user_id = user.Id,
                       sub_id = sub.Id
                   })
                   .Entities.IFF(e => e.Update("{0}+{1}", new UMC.Data.Entities.ProjectAccess { Times = 1 }
                   , new UMC.Data.Entities.ProjectAccess { LastAccessTime = DateTime.Now }) == 0,
                   e => e.Insert(new UMC.Data.Entities.ProjectAccess
                   {
                       CreationTime = DateTime.Now,
                       Times = 1,
                       LastAccessTime = DateTime.Now,
                       sub_id = sub.Id,
                       user_id = user.Id
                   }));
                }


            }


            var celss = UMC.Data.JSON.Deserialize<WebMeta[]>((String.IsNullOrEmpty(sub.DataJSON) ? "[]" : sub.DataJSON)) ?? new UMC.Web.WebMeta[] { };

            foreach (var pom in celss)
            {
                switch (pom["_CellName"])
                {
                    case "CMSImage":
                        {
                            var value = pom.GetDictionary()["value"] as Hashtable;
                            if (value != null && value.ContainsKey("size"))
                            {
                                value.Remove("size");
                            }

                            pom.Put("style", new UIStyle().Padding(0, 10));
                        }
                        break;
                    case "CMSCode":
                        {
                            var value = pom.GetDictionary()["value"] as Hashtable;
                            if (value != null && value.ContainsKey("code"))
                            {
                                var code = value["code"] as string;
                                var type = value["type"] as string;
                                if (String.IsNullOrEmpty(code) == false)
                                {
                                    var cell = Data.Markdown.Highlight(code, type);
                                    pom.Put("value", cell.Data);
                                    pom.Put("format", cell.Format);
                                    pom.Put("style", cell.Style);
                                }
                            }

                        }

                        break;
                }
            }

            ui.AddCells(celss); ;


            var cateData = new UMC.Web.WebMeta().Put("icon", "\uf02c", "name", sub.Poster).Put("look", (sub.Look ?? 0) + "").Put("Reply", (sub.Reply ?? 0) + "");

            var footer2 = new UIButton(cateData);
            footer2.Title("{icon}  {name} | 阅读({look}) | 评论({Reply})");

            footer2.Style.Color(0x999).Size(14).Name("icon", new UIStyle().Font("wdk"));
            ui.Add(footer2);
            if (project != null && isEditer)
            {
                ui.Title.Name("Editer", "OK");
                if (this.Context.Request.IsApp)
                {
                    footer2.Button(new UIEventText("编辑图文").Style(new UIStyle().Color(0x3F51B5).Name("border", "none")).Click(new UIClick(sub.Id.ToString())
                    {
                        Model = model,
                        Command = "EditUI"
                    }));
                }


            }




        }
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
            var sid1 = UMC.Data.Utility.Guid(strId);
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
            if (sid1.HasValue)
            {
                subEntity.Where.And().Equal(new Data.Entities.Subject { Id = sid1 });
            }
            if (strId.IndexOf("/") > 0)
            {

                var paths = new List<String>();
                paths.AddRange(strId.Split('/'));
                if (paths.Count == 3)
                {

                    var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
                    {
                        Code = paths[0]
                    }).Entities.Single();
                    if (project != null)
                    {
                        var projectItem = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new ProjectItem
                        {
                            project_id = project.Id,
                            Code = paths[1]
                        }).Entities.Single();
                        if (projectItem != null)
                        {
                            sid1 = Guid.Empty;
                            subEntity.Where.And().Equal(new Subject
                            {
                                project_id = project.Id,
                                project_item_id = projectItem.Id,
                                Code = paths[2]
                            });
                        }
                    }
                }
            }


            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);


            var webr = UMC.Data.WebResource.Instance();
            var user = UMC.Security.Identity.Current;
            var nextKey = this.AsyncDialog("NextKey", g => this.DialogValue("Subject"));


            var selectIndex = UMC.Data.Utility.IntParse(this.AsyncDialog("selectIndex", g => this.DialogValue("0")), 0);

            UITabFixed tabFixed = new UITabFixed();
            tabFixed.Add("评论", "Comments", "Comments");
            tabFixed.Add("点赞", "Proposal", "Comments");
            tabFixed.Add("已读", "Access", "Comments");
            tabFixed.SelectIndex = selectIndex;



            Subject sub = sid1.HasValue ? subEntity.Single() : null;

            var ui = UISection.Create();
            var rui = ui;
            if (nextKey == "Subject")
            {
                ui.Key = nextKey;
                if (sub == null || sub.Visible == -1)
                {

                    var title = new UITitle("图文正文");
                    ui.Title = title;
                    if (strId.IndexOf("/") > 0)
                    {
                        ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "此图文已删除").Put("icon", "\uea0d")
                            , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                    }
                    else
                    {
                        ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", String.Format("未有{0}路径文档", strId)).Put("icon", "\uea0d")
                            , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                    }
                    ui.IsNext = false;

                }
                else
                {
                    //  
                    ui.IsNext = true;
                    var title = new UITitle("图文正文");
                    ui.Title = title;
                    title.Name("title", sub.Title);
                    title.Name("Id", sub.Id.ToString());
                    title.Float();
                    if (sub.Status > 0)
                    {
                        if ((sub.PublishTime ?? 0) + 3600 < Utility.TimeSpan())// DateTime.Now)
                        {
                            title.Name("releaseId", sub.Id.ToString());
                        }
                    }
                    var isEditer = false;
                    Project project = null;
                    ProjectItem projectItem = null;
                    if (sub.project_id.HasValue && sub.project_item_id.HasValue)
                    {

                        project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>().Where.And().Equal(new Data.Entities.Project { Id = sub.project_id })
                           .Entities.Single();




                        projectItem = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>().Where.And().Equal(new Data.Entities.ProjectItem { Id = sub.project_item_id })
                           .Entities.Single();
                    }

                    UIItem item = null;
                    if (project != null && projectItem != null)
                    {
                        if (String.IsNullOrEmpty(sub.Code) == false)
                        {
                            title.Name("Path", String.Format("{0}/{1}/{2}", project.Code, projectItem.Code, sub.Code));

                            if (request.IsApp)
                                title.Right('\uf141', UIClick.Click(new UIClick("Id", sub.Id.ToString()) { Command = "TipOff", Model = request.Model }));

                            var proider = UMC.Data.Reflection.GetDataProvider("cmsui", String.Format("{0}.{1}.{2}", project.Code, projectItem.Code, sub.Code));
                            if (proider == null)
                            {
                                proider = UMC.Data.Reflection.GetDataProvider("cmsui", String.Format("{0}.{1}", project.Code, projectItem.Code, sub.Code));
                            }
                            if (proider != null)
                            {
                                item = UMC.Data.Reflection.CreateObject(proider) as UIItem;

                            }
                        }

                        if (project.user_id == user.Id)
                        {
                            isEditer = true;
                        }
                        else
                        {
                            var member = Utility.CMS.ObjectEntity<ProjectMember>()
                                  .Where.And().Equal(new ProjectMember
                                  {
                                      project_id = project.Id,
                                      user_id = user.Id
                                  }).Entities.Single();
                            if (member != null)
                            {
                                switch (member.AuthType)
                                {
                                    case WebAuthType.Admin:
                                    case WebAuthType.User:
                                        isEditer = true;
                                        break;
                                }
                            }
                        }
                    }
                    if (item != null && item.Header(ui, sub) == false)
                    {
                        ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "此图文已删除").Put("icon", "\ue953"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                         new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                        ui.IsNext = false;

                    }
                    else
                    {
                        var desc = new UIDesc(new WebMeta().Put("desc", sub.Title).Put("state", "未发布"));
                        desc.Style.Bold().Size(18).Name("border", "none");
                        if (sub.Status < 0)
                        {
                            desc.Desc("{desc} [{1:state:1}]");
                            desc.Style.Name("state").Color(0x999).Size(13);
                        }
                        //if (request.IsApp)
                        //{
                        desc.Style.Padding(55, 10, 10, 10);
                        //}
                        //else
                        //{
                        //    desc.Style.Padding(10);

                        //}
                        ui.Add(desc);
                    }
                    if (isEditer == false && sub.Status < 1)
                    {
                        ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "此文档未发布，现还不能查看").Put("icon", "\uF0E6"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                         new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                        ui.IsNext = false;

                    }
                    else
                    {
                        ui.DisableSeparatorLine();
                        Subject(request.Model, ui, sub, project, isEditer);
                        if (sub.Look.HasValue)
                        {
                            subEntity.Update("{0}+{1}", new Data.Entities.Subject { Look = 1 });
                        }
                        else
                        {
                            subEntity.Update(new Data.Entities.Subject { Look = 1 });

                        }
                        if (item != null && ui.IsNext == true)
                        {
                            item.Footer(ui, sub);
                        }
                        if (ui.IsNext == true)
                            ui.IsNext = String.Equals(request.Model, "Subject");
                    }


                }

                if (ui.IsNext == false)
                {

                    response.Redirect(ui);
                }
                if (sub.IsComment == false)
                {
                    ui.IsNext = false;
                }

                ui.Add(SubjectBestActivity.BSSArea(sub, request.Model, request.IsApp));

                ui.StartIndex = 0;
                ui = ui.NewSection();
                start = 0;

            }
            var Keyword = (form["Keyword"] as string ?? String.Empty);
            if (String.IsNullOrEmpty(Keyword) && selectIndex > -1)
            {
                Keyword = tabFixed.SelectValue["search"];
            }
            tabFixed.Style.Name("border", "bottom");
            ui.Add(tabFixed);
            ui.Key = "Comments";
            switch (Keyword)
            {
                case "Comments":
                    {
                        var entity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Comment>();
                        entity.Where.And().Equal(new Data.Entities.Comment { ref_id = sub.Id.Value, for_id = Guid.Empty });

                        entity.Order.Desc(new Data.Entities.Comment { CommentDate = DateTime.Now });
                        entity.Where.And().Greater(new Comment { Visible = -1 });
                        var count = entity.Count();
                        var hash = Utility.Comments(entity, start, limit, request.Model);
                        if (count == 0)
                        {
                            rui.IsNext = false;
                            ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "暂无评论").Put("icon", "\uF0E6"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                                new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                        }
                        else
                        {
                            ui.AddCells(hash.ToArray());
                            rui.IsNext = ui.Total > limit + start;
                        }

                    }
                    break;
                case "Access":
                    {

                        var style = new UIStyle().AlignLeft().Name("border", "none");
                        int mlimit = limit * 4;
                        int mstart = start * 4;
                        var ids = new List<Guid>();
                        var accEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectAccess>();
                        accEntity.Where.And().Equal(new ProjectAccess { sub_id = sub.Id });
                        accEntity.Order.Desc(new ProjectAccess { LastAccessTime = DateTime.Now });
                        accEntity.Query(mstart, mlimit, dr => ids.Add(dr.user_id.Value));
                        if (ids.Count > 0)
                        {
                            var users = new List<User>();
                            Utility.CMS.ObjectEntity<User>()
                                                                                        .Where.And().In(new User { Id = ids[0] }, ids.ToArray()).Entities.Query(dr => users.Add(dr));

                            var icons = new List<UIEventText>();
                            foreach (var id in ids)
                            {

                                var v = users.Find(u => u.Id == id) ?? new User() { Id = id, Alias = "未知" };
                                icons.Add(new UIEventText(v.Alias).Src(webr.ResolveUrl(v.Id.Value, "1", "4")).Click(request.IsApp ? UIClick.Pager(request.Model, "Account", new WebMeta().Put("Id", v.Id), true) : new UIClick(v.Id.ToString()).Send(request.Model, "Account")));

                                if (icons.Count % 4 == 0)
                                {
                                    ui.Add(new Web.UI.UIIcon().Add(icons.ToArray()));
                                    icons.Clear();
                                }

                            }
                            if (icons.Count > 0)
                            {
                                var ls = new Web.UI.UIIcon().Add(icons.ToArray());
                                ls.Style.Copy(style);
                                ui.Add(ls);// new Web.UI.UIIcon().Add(icons.ToArray()));
                                           //ui2.AddIcon(style, icons.ToArray());
                            }
                        }
                        var m = accEntity.Count();
                        int total = m / 4;
                        if (m % 4 > 0)
                        {
                            total++;
                        }
                        ui.IsNext = (mstart + mlimit) < total;
                        if (m == 0)
                            ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "暂无访问").Put("icon", "\uF0E6"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                                new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                        response.Redirect(ui);

                    }
                    break;
                case "Proposal":
                    {

                        var style = new UIStyle().AlignLeft().Name("border", "none");
                        int mlimit = limit * 4;
                        int mstart = start * 4;
                        var ids = new List<Guid>();
                        var accEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Proposal>();
                        accEntity.Where.And().Equal(new Proposal { ref_id = sub.Id });
                        accEntity.Order.Desc(new Proposal { CreationDate = DateTime.Now });

                        accEntity.Query(mstart, mlimit, dr => ids.Add(dr.user_id.Value));
                        if (ids.Count > 0)
                        {
                            var users = new List<User>();
                            Utility.CMS.ObjectEntity<User>().Where.And().In(new User { Id = ids[0] }, ids.ToArray()).Entities.Query(dr => users.Add(dr));

                            var icons = new List<UIEventText>();
                            foreach (var id in ids)
                            {

                                var v = users.Find(u => u.Id == id) ?? new User() { Id = id, Alias = "未知" };
                                icons.Add(new UIEventText(v.Alias).Src(webr.ResolveUrl(v.Id.Value, "1", "4")).Click(request.IsApp ? UIClick.Pager(request.Model, "Account", new WebMeta().Put("Id", v.Id), true) : new UIClick(v.Id.ToString()).Send(request.Model, "Account")));

                                if (icons.Count % 4 == 0)
                                {
                                    ui.Add(new Web.UI.UIIcon().Add(icons.ToArray()));
                                    //ui.AddIcon(style, icons.ToArray());
                                    icons.Clear();
                                }

                            }
                            if (icons.Count > 0)
                            {
                                var ls = new Web.UI.UIIcon().Add(icons.ToArray());
                                ls.Style.Copy(style);
                                ui.Add(ls);// new Web.UI.UIIcon().Add(icons.ToArray()));
                                           //ui2.AddIcon(style, icons.ToArray());
                            }
                        }
                        var m = accEntity.Count();
                        int total = m / 4;
                        if (m % 4 > 0)
                        {
                            total++;
                        }
                        ui.IsNext = (mstart + mlimit) < total;
                        if (m == 0)
                            ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "暂无点赞").Put("icon", "\uf087"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                                new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                        response.Redirect(ui);

                    }
                    break;
            }
            response.Redirect(rui);
        }

    }
}