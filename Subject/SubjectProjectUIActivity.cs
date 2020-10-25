using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using UMC.Web.UI;
using UMC.Web;
using UMC.Security;
using UMC.Data.Entities;

namespace UMC.Subs.Activities
{

    class SubjectProjectUIActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var ProjectId = Utility.Guid(this.AsyncDialog("Id", g =>
            {
                this.Prompt("请输入项目");

                return this.DialogValue("Project");
            })) ?? Guid.Empty;
            var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project { Id = ProjectId }).Entities.Single();

            var Model = this.AsyncDialog("Model", ml =>
             {
                 if (project == null)
                 {
                     return this.DialogValue("News");
                 }
                 var form = (request.SendValues ?? new UMC.Web.WebMeta()).GetDictionary();
                 if (form.ContainsKey("start") == false)
                 {
                     if (request.IsApp)
                     {

                         var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                         this.Context.Send(buider.Builder(), true);
                     }
                     else
                     {

                         if (request.Url.Query.Contains("_v=Sub"))
                         {
                             this.Context.Send("Subject.Path", new WebMeta().Put("Path", project.Code), true);
                         }
                         else
                         {
                             var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                             this.Context.Send(buider.Builder(), true);
                         }
                     }
                 }
                 var webr = UMC.Data.WebResource.Instance();
                 UISection ui = null;
                 UISection ui2 = null;

                 var selectIndex = UMC.Data.Utility.IntParse(this.AsyncDialog("selectIndex", g => this.DialogValue("0")), 0);

                 var items = new List<WebMeta>();//
                 items.Add(new UMC.Web.WebMeta().Put("text", "团队成员", "search", "Member", "Key", "List"));
                 items.Add(new UMC.Web.WebMeta().Put("text", "文档资讯", "search", "Subs", "Key", "List"));
                 items.Add(new UMC.Web.WebMeta().Put("text", "项目动态", "search", "Dynamic", "Key", "List"));

                 var Keyword = (form["Keyword"] as string ?? String.Empty);
                 if (String.IsNullOrEmpty(Keyword) && selectIndex > -1)
                 {
                     Keyword = items[selectIndex]["search"];
                 }

                 int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);
                 var nextKey = this.AsyncDialog("NextKey", g => this.DialogValue("Header")); ;
                 if (start == 0 && String.Equals(nextKey, "Header"))
                 {
                     ;

                     var logoUrl = webr.ResolveUrl(project.Id.Value, "1", 4);

                     var members = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                           .Where.And().Equal(new Data.Entities.ProjectMember { project_id = project.Id })
                           .Entities.Count() + 1;




                     var suject =
                     Utility.CMS.ObjectEntity<Subject>()
                        .Where.And().Equal(new Subject { project_id = project.Id })
                        .Entities.GroupBy().Sum(new Subject { Reply = 0 })
                        .Sum(new Subject { Look = 0 }).Count(new Subject { Seq = 0 }).Single();



                     var Discount = new UIHeader.Portrait(logoUrl);


                     Discount.Value(project.Caption);
                     Discount.Time(project.Description);

                     var color = 0x63b359;
                     Discount.Gradient(color, color);
                     var header = new UIHeader();
                     var title = UITitle.Create();

                     title.Title = "项目介绍";
                     title.Style.BgColor(color);
                     title.Style.Color(0xfff);
                     header.AddPortrait(Discount);

                     ui = UISection.Create(header, title);
                     bool isIsAttention;

                     UIIconNameDesc uIIcon = new UIIconNameDesc();//
                     uIIcon.Put("icon", '\uF0c0').Put("color", "#40c9c6").Put("name", "团队规模").Put("desc", members + "人");
                     if (request.Model == "Subject")
                         uIIcon.Button(SubjectAttentionActivity.Attention(project.Id.Value, out isIsAttention), Web.UIClick.Click(new Web.UIClick("Id", project.Id.ToString()) { Model = request.Model, Command = "ProjectAtten" }), isIsAttention ? 0x25b864 : 0xe67979);
                     ui.Add(uIIcon);

                     uIIcon = new UIIconNameDesc(new UIIconNameDesc.Item('\uF02d', "文章数量", suject.Seq + "篇").Color(0x36a3f7), new UIIconNameDesc.Item('\uf06e', "浏览总数", suject.Look + "次").Color(0x34bfa3));
                     ui.Add(uIIcon);
                     ui2 = ui.NewSection();


                 }
                 else
                 {
                     ui2 = ui = UISection.Create();

                 }
                 if (start == 0 && String.Equals(nextKey, "Self") == false)
                 {
                     if (selectIndex > 0)
                     {

                         ui2.Add(UICell.Create("TabFixed", new UMC.Web.WebMeta().Put("items", items).Put("selectIndex", selectIndex)));
                     }
                     else
                     {
                         ui2.Add(UICell.Create("TabFixed", new UMC.Web.WebMeta().Put("items", items))); ;

                     }
                 }
                 ui2.Key = "List";
                 int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
                 switch (Keyword)
                 {
                     default:
                     case "Subs":
                         {

                             var Type = this.AsyncDialog("Type", "Items");

                             var itemId = Utility.Guid(this.AsyncDialog("CId", project.Id.ToString()));


                             switch (Type)
                             {
                                 case "Items":
                                     {
                                         var subs = new List<Subject>();
                                         Utility.CMS.ObjectEntity<Subject>().Where.And().In(new Subject { project_id = project.Id }).Entities
                                             .GroupBy(new Subject { project_item_id = Guid.Empty })
                                             .Count(new Subject { Look = 0 })
                                             .Query(dr => subs.Add(dr));

                                         var projects = new List<ProjectItem>();
                                         var projectEntity = Utility.CMS.ObjectEntity<ProjectItem>();
                                         projectEntity.Where.And().In(new ProjectItem { project_id = project.Id });

                                         projectEntity.Order.Asc(new ProjectItem { Sequence = 0 });

                                         projectEntity.Query(dr =>
                                         {
                                             var su = subs.Find(s => s.project_item_id == dr.Id);
                                             ui2.AddCell('\uf022', dr.Caption, String.Format("{0}篇", su == null ? 0 : su.Look)
                                              , new UIClick(new WebMeta().Put("key", "List").Put("send", new WebMeta().Put("CId", dr.Id).Put("Type", "Portfolio"))) { Key = "Query" });
                                         });
                                     }
                                     break;
                                 case "Portfolio":
                                     {
                                         var subs = new List<Subject>();
                                         Utility.CMS.ObjectEntity<Subject>().Where.And().In(new Subject { project_item_id = itemId }).Entities
                                             .GroupBy(new Subject { portfolio_id = Guid.Empty })
                                             .Count(new Subject { Look = 0 })
                                             .Query(dr => subs.Add(dr));


                                         var item = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().In(new ProjectItem { Id = itemId }).Entities.Single();
                                         var navData = new WebMeta().Put("item", item.Caption).Put("Icon", '\uf022');

                                         navData.Put("nav", "目录");
                                         navData.Put("split", '\uf105');
                                         var cell = UICell.Create("UI", navData);
                                         cell.Style.Name("nav").Click(new UIClick(new WebMeta().Put("key", "List").Put("send", new WebMeta().Put("CId", item.project_id).Put("Type", "Items"))) { Key = "Query" }).Color(0x36a3f7);
                                         cell.Style.Name("item").Color(0x999);
                                         cell.Format.Put("text", "{nav} {split} {item}");
                                         cell.Style.Name("split").Font("wdk");

                                         ui2.Add(cell);
                                         Utility.CMS.ObjectEntity<Portfolio>().Where.And().Equal(new Portfolio { project_item_id = item.Id }).Entities
                                             .Order.Asc(new Portfolio { Sequence = 0 }).Entities
                                             .Query(dr =>
                                         {
                                             var su = subs.Find(s => s.portfolio_id == dr.Id);
                                             ui2.AddCell('\uf22b', dr.Caption, String.Format("{0}篇", su == null ? 0 : su.Look)
                                                , new UIClick(new WebMeta().Put("key", "List").Put("send", new WebMeta().Put("CId", dr.Id).Put("Type", "Subs"))) { Key = "Query" });
                                         });



                                     }
                                     break;
                                 case "Subs":
                                     {
                                         var ui3 = ui2;
                                         if (start == 0)
                                         {
                                             var portfolio = Utility.CMS.ObjectEntity<Portfolio>().Where.And().In(new Portfolio { Id = itemId }).Entities.Single();
                                             var item = Utility.CMS.ObjectEntity<ProjectItem>()
                                                 .Where.And().In(new ProjectItem { Id = portfolio.project_item_id }).Entities.Single();

                                             var navData = new WebMeta().Put("item", item.Caption).Put("Icon", '\uf022');

                                             navData.Put("nav", "目录");
                                             navData.Put("split", '\uf105');
                                             navData.Put("port", portfolio.Caption);
                                             var cell = UICell.Create("UI", navData);
                                             cell.Style.Name("nav").Click(new UIClick(new WebMeta().Put("key", "List").Put("send", new WebMeta().Put("CId", item.project_id).Put("Type", "Items"))) { Key = "Query" }).Color(0x36a3f7);
                                             cell.Style.Name("item").Color(0x36a3f7).Click(new UIClick(new WebMeta().Put("key", "List").Put("send", new WebMeta().Put("CId", item.Id).Put("Type", "Portfolio"))) { Key = "Query" });
                                             cell.Format.Put("text", "{nav} {split} {item} {split} {port}");

                                             cell.Style.Name("port").Color(0x999);
                                             cell.Style.Name("split").Font("wdk");
                                             ui2.Add(cell);

                                             ui3 = ui2.NewSection();

                                         }
                                         ui3.Key = "Subs";
                                         var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
                                         subEntity.Order.Desc(new Subject { ReleaseDate = DateTime.Now });
                                         subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1, portfolio_id = itemId }).And().Greater(new Subject { Status = 0, Visible = -1 });
                                         SubjectUIActivity.Search(request.Model, ui3, subEntity, start, limit);
                                         if (ui3.Total == 0)
                                         {
                                             ui3.Add("Desc", new UMC.Web.WebMeta().Put("desc", "尚未有发布的项目资讯").Put("icon", "\uF016")
                                                 , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                                         }



                                     }
                                     break;
                             }

                         }
                         break;
                     case "Member":
                         {
                             var style = new UIStyle().AlignLeft();
                             int mlimit = limit * 4;
                             int mstart = start * 4;
                             var ids = new List<Guid>();
                             var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>();
                             subEntity.Where.And().Equal(new ProjectMember { project_id = project.Id });
                             subEntity.Order.Desc(new ProjectMember { CreationTime = DateTime.Now });
                             var wids = new List<Guid>();
                             var pms = new List<ProjectMember>();
                             if (mstart == 0)
                             {
                                 ids.Add(project.user_id.Value);
                                 subEntity.Where.And().In(new ProjectMember { AuthType = WebAuthType.Admin }, WebAuthType.User)
                                     .And().Unequal(new ProjectMember { user_id = project.user_id }).Entities.Order.Desc(new ProjectMember { AuthType = 0 });
                                 subEntity.Query(dr => { pms.Add(dr); ids.Add(dr.user_id.Value); });
                                 wids.AddRange(ids);

                             }
                             subEntity.Where.Reset().And().Equal(new ProjectMember { project_id = project.Id, AuthType = WebAuthType.Guest });
                             subEntity.Query(mstart, mlimit, dr => ids.Add(dr.user_id.Value));
                             if (ids.Count > 0)
                             {
                                 var users = new List<User>();
                                 Utility.CMS.ObjectEntity<User>()
                                     .Where.And().In(new User { Id = ids[0] }, ids.ToArray()).Entities.Query(dr => users.Add(dr));

                                 if (wids.Count > 0)
                                 {
                                     var puser = users.Find(u => u.Id == project.user_id) ?? new User { Alias = "未知", Id = project.user_id };


                                     ui2.Add(new UIIconNameDesc(new UIIconNameDesc.Item(webr.ResolveUrl(puser.Id.Value, "1", "4"), puser.Alias, "创立于" + Utility.GetDate(project.CreationTime))
                                     .Click(request.IsApp ? UIClick.Pager(request.Model, "Account", new WebMeta().Put("Id", puser.Id), true) : new UIClick(puser.Id.ToString()).Send(request.Model, "Account"))).Button("立项人", null, 0xb7babb));


                                     var ites = new List<UIIconNameDesc.Item>();
                                     foreach (var pm in pms)
                                     {
                                         var v = users.Find(u => u.Id == pm.user_id) ?? new User { Alias = pm.Alias };
                                         var text = "专栏作家";
                                         switch (pm.AuthType)
                                         {
                                             case WebAuthType.Admin:
                                                 text = "管理员";
                                                 break;
                                             case WebAuthType.User:
                                                 break;
                                         }
                                         ites.Add(new UIIconNameDesc.Item(webr.ResolveUrl(pm.user_id.Value, "1", "4"), v.Alias, text)
                                             .Click(request.IsApp ? UIClick.Pager(request.Model, "Account", new WebMeta().Put("Id", pm.user_id), true) : new UIClick(pm.user_id.ToString()).Send(request.Model, "Account")));
                                         if (ites.Count % 2 == 0)
                                         {
                                             ui2.Add(new UIIconNameDesc(ites.ToArray()));
                                             ites.Clear();
                                         }
                                     }
                                     if (ites.Count > 0)
                                         ui2.Add(new UIIconNameDesc(ites.ToArray()));
                                     ids.RemoveAll(g => wids.Exists(w => w == g));
                                 }

                                 var icons = new List<UIEventText>();
                                 foreach (var id in ids)
                                 {

                                     var v = users.Find(u => u.Id == id);
                                     icons.Add(new UIEventText(v.Alias).Src(webr.ResolveUrl(v.Id.Value, "1", "4")).Click(request.IsApp ? UIClick.Pager(request.Model, "Account", new WebMeta().Put("Id", v.Id), true) : new UIClick(v.Id.ToString()).Send(request.Model, "Account")));

                                     if (icons.Count % 4 == 0)
                                     {
                                         ui2.Add(new Web.UI.UIIcon().Add(icons.ToArray()));
                                         icons.Clear();
                                     }

                                 }
                                 if (icons.Count > 0)
                                 {
                                     var ls = new Web.UI.UIIcon().Add(icons.ToArray());
                                     ls.Style.Copy(style);
                                     ui2.Add(ls);// new Web.UI.UIIcon().Add(icons.ToArray()));
                                     //ui2.AddIcon(style, icons.ToArray());
                                 }
                                 //if (icons.Count > 0)
                                 //    ui2.AddIcon(style, icons.ToArray());
                             }
                             var m = subEntity.Count();
                             int total = m / 4;
                             if (m % 4 > 0)
                             {
                                 total++;
                             }
                             ui.Total = total;
                             response.Redirect(ui);
                         }
                         break;
                     case "Writer":
                         {
                             int mlimit = limit * 4;
                             int mstart = start * 4;
                             var ids = new List<ProjectMember>();
                             var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>();
                             subEntity.Where.And().Equal(new ProjectMember { project_id = project.Id });
                             subEntity.Where.And().In(new ProjectMember { AuthType = WebAuthType.Admin }, WebAuthType.User);
                             subEntity.Order.Desc(new ProjectMember { CreationTime = DateTime.Now });
                             subEntity.Query(mstart, mlimit, dr => ids.Add(dr));


                             var style = new UIStyle().AlignRight();
                             if (ids.Count > 0)
                             {

                                 var icons = new List<UIEventText>();
                                 foreach (var v in ids)
                                 {

                                     icons.Add(new UIEventText(v.Alias).Src(webr.ResolveUrl(v.user_id.Value, "1", "4")).Click(UIClick.Pager(request.Model, "Account", new WebMeta().Put("Id", v.user_id), true)));

                                     if (icons.Count % 4 == 0)
                                     {
                                         ui2.Add(new Web.UI.UIIcon().Add(icons.ToArray()));
                                         icons.Clear();
                                     }

                                 }
                                 if (icons.Count > 0)
                                 {
                                     var ls = new Web.UI.UIIcon().Add(icons.ToArray());
                                     ls.Style.Copy(style);
                                     ui2.Add(ls);// new Web.UI.UIIcon().Add(icons.ToArray()));
                                     //ui2.AddIcon(style, icons.ToArray());
                                 }
                             }
                             var m = subEntity.Count();
                             int total = m / 4;
                             if (m % 4 > 0)
                             {
                                 total++;
                             }
                             ui.Total = total;
                             response.Redirect(ui);
                         }
                         break;
                     case "Dynamic":
                         {
                             var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectDynamic>();
                             subEntity.Where.And().Equal(new ProjectDynamic
                             {
                                 project_id = project.Id
                             }).Entities.Order.Desc(new ProjectDynamic { Time = 0 });

                             var subs = new List<ProjectDynamic>();
                             var uids = new List<Guid>();
                             subEntity.Query(start, limit, dr =>
                                                          {
                                                              subs.Add(dr);
                                                              uids.Add(dr.user_id ?? Guid.Empty);

                                                          });
                             var cates = new List<User>();
                             if (uids.Count > 0)
                             {
                                 Utility.CMS.ObjectEntity<User>().Where.And().In(new User { Id = uids[0] }, uids.ToArray())
                                         .Entities.Query(dr => cates.Add(dr));
                             }
                             foreach (var sub in subs)
                             {

                                 var user2 = cates.Find(d => d.Id == sub.user_id) ?? new User();

                                 var data = new WebMeta().Put("alias", user2.Alias, "desc", sub.Explain).Put("time", sub.Time)
                                     .Put("name", sub.Title)
                                     .Put("src", webr.ResolveUrl(sub.user_id ?? Guid.Empty, "1", 5));
                                 var cell = UICell.Create("IconNameDesc", data);

                                 cell.Format.Put("desc", "{alias} {time} {desc}");
                                 cell.Style.Name("name").Size(14);
                                 ui2.Add(cell);
                             }
                             ui.Total = subEntity.Count();
                             if (ui.Total == 0)
                             {
                                 //   webr.

                                 ui2.Add("Desc", new UMC.Web.WebMeta().Put("desc", "尚未有此项目动态").Put("icon", "\uF016")
                                     , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                             }
                         }
                         break;


                 }



                 response.Redirect(ui);
                 return this.DialogValue("none");
             });
            var user = Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {
                this.Prompt("请登录", false);
                response.Redirect("Account", "Login");
            }
            if (project == null || project.user_id == user.Id)
            {
                switch (Model)
                {
                    case "Icon":

                        response.Redirect("Design", "Picture", new WebMeta().Put("id", project.Id).Put("seq", 1), true);

                        break;
                    case "Transfer":
                        var userId = Utility.Guid(this.AsyncDialog("Transfer", request.Model, "Member", new WebMeta().Put("Type", "Admin").Put("Project", project.Id))).Value;
                        this.AsyncDialog("Confirm", g => new UIConfirmDialog("你确认转移项目拥有者身份吗"));
                        Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project { Id = project.Id }).Entities
                            .Update(new Project { user_id = userId });
                        Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember { user_id = userId, project_id = project.Id })
                            .Entities.Update(new ProjectMember { user_id = project.user_id, CreationTime = DateTime.Now, Alias = user.Alias });

                        this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Project").Put("Id", project.Id).Put("Text", project.Caption)
                            .Put("Code", project.Code), true);

                        break;
                }


                var Caption = this.AsyncDialog("Settings", d =>
                {

                    var fmdg = new Web.UIFormDialog();
                    fmdg.Title = "编辑项目";
                    switch (Model)
                    {
                        case "News":
                            fmdg.Title = "新建项目";
                            fmdg.AddText("项目名称", "Caption", "");
                            fmdg.Submit("确认", request, "Subject.Project");
                            return fmdg;
                        //break;
                        default:
                        case "Caption":
                            fmdg.Title = "项目名称";
                            fmdg.AddText("项目名称", "Caption", project.Caption);
                            break;
                        case "Description":
                            fmdg.Title = "项目介绍";
                            fmdg.AddText("项目介绍", "Description", project.Description);
                            break;
                        case "Code":
                            fmdg.Title = "项目简码";
                            fmdg.AddText("项目简码", "Code", project.Code).PlaceHolder("短小易记有助于访问和传播").Put("tip", "");
                            break;
                    }
                    fmdg.Submit("确认", request, "Subject.Project");
                    fmdg.AddUI("对接", "配置钉钉应用").Command(request.Model, "Dingtalk", project.Id.ToString());
                    fmdg.AddUI("对接", "配置钉钉机器人").Command(request.Model, "DDRobot", project.Id.ToString());
                    //fmdg.AddUI("对接", "配置钉钉机器人").Command(request.Model, request.Command, new WebMeta().Put("Id", project.Id.ToString(), "Model", "Transfer"));
                    return fmdg;

                });
                var team = new Project();
                if (Model == "News")
                {

                    UMC.Data.Reflection.SetProperty(team, Caption.GetDictionary());
                    team.ModifiedTime = DateTime.Now;


                    team.Id = Guid.NewGuid();
                    team.user_id = user.Id;
                    team.Code = Utility.Parse36Encode(team.Id.Value.GetHashCode());
                    team.CreationTime = DateTime.Now;
                    team.Sequence = 0;


                    Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>().Insert(team);

                    var strt = UMC.Security.AccessToken.Current.Data["DingTalk-Setting"] as string;//, Utility.Guid(projectId)).Commit();
                    if (String.IsNullOrEmpty(strt) == false)
                    {
                        var userSetting = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
                            .Where.And().Equal(new ProjectUserSetting { Id = Utility.Guid(strt, true) }).Entities.Single();

                        if (userSetting != null)
                        {
                            var setting2 = new ProjectSetting() { user_setting_id = userSetting.Id, project_id = team.Id, Type = 11 }; Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectSetting>().Insert(setting2);
                        }
                    }
                    Data.WebResource.Instance().Transfer(new Uri("https://oss.365lu.cn/UserResources/app/zhishi-icon.jpg"), team.Id.Value, 1);
                    var p = new ProjectItem()
                    {
                        Id = Guid.NewGuid(),
                        Caption = "Home",
                        Code = Utility.Parse36Encode(Guid.NewGuid().GetHashCode()),
                        CreationTime = DateTime.Now,
                        project_id = team.Id,
                        Sequence = 0,
                        user_id = user.Id,
                    };
                    Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                                            .Insert(p);

                    var portfolio = new Portfolio()
                    {
                        Id = Guid.NewGuid(),
                        Caption = "随笔",
                        Count = 0,
                        CreationTime = DateTime.Now,
                        Sequence = 0,
                        user_id = user.Id,
                        project_id = team.Id,
                        project_item_id = p.Id,
                    };
                    Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>()
                                            .Insert(portfolio);

                    Utility.CMS.ObjectEntity<ProjectDynamic>()
                                                   .Insert(new ProjectDynamic
                                                   {
                                                       Time = Utility.TimeSpan(),//DateTime.Now,
                                                       user_id = user.Id,
                                                       Explain = "创建了项目",
                                                       project_id = team.Id,
                                                       refer_id = team.Id,
                                                       Title = team.Caption,
                                                       Type = DynamicType.Project
                                                   });

                    this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Project").Put("id", team.Id).Put("text", team.Caption)
                        .Put("code", team.Code), true);
                }
                UMC.Data.Reflection.SetProperty(team, Caption.GetDictionary());
                if (String.IsNullOrEmpty(team.Code) == false)
                {
                    if (team.Code.Length < 3)
                    {
                        this.Prompt("项目简码必须大于3个字符");
                    }
                    if (System.Text.RegularExpressions.Regex.IsMatch(team.Code, "^\\d+$") == true)
                    {
                        this.Prompt("项目简码不能全是数字");

                    }
                    if (System.Text.RegularExpressions.Regex.IsMatch(team.Code, "^\\w+$") == false)
                    {
                        this.Prompt("项目简码只能是字符和数字");
                    }
                    if (String.Equals(team.Code, project.Code, StringComparison.CurrentCulture) == false)
                    {
                        if (Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                               .Where.And().Equal(new Project
                               {
                                   Code = team.Code
                               }).Entities.Count() > 0)
                        {
                            this.Prompt("存在相同的简码");
                        }

                    }
                }

                team.ModifiedTime = DateTime.Now;
                var objectEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>();
                objectEntity.Where.And().Equal(new Project
                {
                    Id = project.Id
                });
                objectEntity.Update(team);
                this.Prompt("修改成功", false);
                this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Project").Put("id", project.Id).Put("text", team.Caption ?? project.Caption).Put("code", team.Code ?? project.Code), true);


            }

        }
    }
}