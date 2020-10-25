using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Web;
using UMC.Data.Entities;
using UMC.Web.UI;

namespace UMC.Subs.Activities
{
    class SubjectAccountActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var identity = UMC.Security.Identity.Current;
            var userId = Utility.Guid(this.AsyncDialog("Id", g =>
            {
                if (identity.IsAuthenticated == false)
                {
                    response.Redirect("Account", "Login");
                }
                return this.DialogValue(identity.Id.ToString());
            }), true);

            var form = (request.SendValues ?? new UMC.Web.WebMeta()).GetDictionary();
            var webr = UMC.Data.WebResource.Instance();
            if (form.ContainsKey("limit") == false)
            {

                var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                this.Context.Send(buider.Builder(), true);
            }
            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);
            UISection ui, ui2;
            var selectIndex = UMC.Data.Utility.IntParse(this.AsyncDialog("selectIndex", g => this.DialogValue("0")), 0);

            var items = new List<WebMeta>();
            items.Add(new UMC.Web.WebMeta().Put("text", "知识录", "search", "Subs", "Key", "List"));
            items.Add(new UMC.Web.WebMeta().Put("text", "参与项目", "search", "Project", "Key", "List"));
            items.Add(new UMC.Web.WebMeta().Put("text", "个人动态", "search", "Dynamic", "Key", "List"));

            var Keyword = (form["Keyword"] as string ?? String.Empty);
            if (String.IsNullOrEmpty(Keyword) && selectIndex > -1)
            {
                Keyword = items[selectIndex]["search"];
            }
            var nextKey = this.AsyncDialog("NextKey", g => this.DialogValue("Header")); ;
            if (start == 0 && String.Equals(nextKey, "Header"))
            {

                var sign = Utility.CMS.ObjectEntity<Data.Entities.Account>()
                    .Where.And().Equal(new Data.Entities.Account { user_id = userId, Type = Security.Account.SIGNATURE_ACCOUNT_KEY }).Entities.Single();

                var logoUrl = webr.ResolveUrl(userId.Value, "1", 4);

                var members = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                      .Where.And().Equal(new Data.Entities.ProjectMember { user_id = userId })
                      .Entities.Count();

                var suject =
                Utility.CMS.ObjectEntity<Subject>()
                   .Where.And().Equal(new Subject { user_id = userId })
                   .Entities.GroupBy().Sum(new Subject { Reply = 0 })
                   .Sum(new Subject { Look = 0 }).Count(new Subject { Seq = 0 }).Single();



                var Discount = new UIHeader.Portrait(logoUrl);

                ;
                var user = identity.Id == userId ? new User { Id = identity.Id, Alias = identity.Alias } : Utility.CMS.ObjectEntity<Data.Entities.User>()
                    .Where.And().Equal(new Data.Entities.User { Id = userId }).Entities.Single();
                if (user != null)
                {
                    Discount.Value(user.Alias);
                }
                else
                {
                    var member = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                         .Where.And().Equal(new Data.Entities.ProjectMember { user_id = userId })
                         .Entities.Single();
                    if (member != null)
                    {
                        Discount.Value(member.Alias);
                    }
                }

                if (sign != null)
                    Discount.Time(sign.Name);// user.ActiveTime.ToString());

                var color = 0x63b359;
                Discount.Gradient(color, color);
                var header = new UIHeader();
                var title = UITitle.Create();

                title.Title = identity.Id == userId ? "我的知识录" : "成员详情";
                header.AddPortrait(Discount);

                title.Style.BgColor(color);
                title.Style.Color(0xfff);

                ui = UISection.Create(header, title);


                var uIIcon = new UIIconNameDesc(new UIIconNameDesc.Item('\uF02d', "知识创作", suject.Seq + "篇").Color(0x36a3f7), new UIIconNameDesc.Item('\uF19d', "关注项目", members + "项").Color(0x40c9c6));
                ui.Add(uIIcon);

                ui2 = ui.NewSection();

            }
            else
            {
                ui2 = ui = UISection.Create();

            }
            ui2.Key = "List";
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

            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
            switch (Keyword)
            {
                default:
                case "Subs":
                    {
                        var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
                        subEntity.Order.Desc(new Subject { ReleaseDate = DateTime.Now });
                        subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1, IsDraught = false, user_id = userId });
                        subEntity.Where.And().Greater(new Subject { Visible = -1 });
                        SubjectUIActivity.Search( request.Model, ui2, subEntity, start, limit);

                        if (ui2.Total == 0)
                        {
                            ui2.Add("Desc", new UMC.Web.WebMeta().Put("desc", "尚未有知识资讯").Put("icon", "\uF016")
                                , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                        }
                    }
                    break;
                case "Project":
                    {
                        var ids = new List<Guid>();
                        var subMebs = new List<ProjectMember>();
                        var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>();
                        subEntity.Where.And().Equal(new ProjectMember { user_id = userId });
                        subEntity.Order.Desc(new ProjectMember { AuthType = 0 }).Desc(new ProjectMember { CreationTime = DateTime.Now }); ;
                        subEntity.Query(start, limit, dr =>
                        {
                            ids.Add(dr.project_id.Value);
                            subMebs.Add(dr);
                        });


                        var proEntity = Utility.CMS.ObjectEntity<Project>();
                        if (start == 0)
                        {
                            proEntity.Where.And().Equal(new Project { user_id = userId });
                        }
                        if (ids.Count > 0)
                        {
                            proEntity.Where.Or().In(new Project { Id = ids[0] }, ids.ToArray());
                        }
                        var projects = new List<Project>();
                        var pids = new List<Guid>();
                        proEntity.Query(dr =>
                        {
                            projects.Add(dr);
                            pids.Add(dr.Id.Value);
                        });

                        if (projects.Count > 0)
                        {
                            var subs = new List<Subject>();
                            Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                                   .Where.And().In(new Subject { project_id = pids[0] }, pids.ToArray())
                                   .Entities.GroupBy(new Subject { project_id = Guid.Empty }).Count(new Subject { Seq = 0 }).Query(dr => subs.Add(dr));


                            if (start == 0)
                            {
                                var mp = projects.FindAll(p => p.user_id == userId);

                                foreach (var p in mp)
                                {
                                    var sub = subs.Find(s => s.project_id == p.Id);
                                    var cellUI = new UIIconNameDesc(new UIIconNameDesc.Item(webr.ResolveUrl(p.Id.Value, "1", "4"), p.Caption,
                            String.Format("知识{0}篇", sub == null ? 0 : sub.Seq))
                                    .Click(request.IsApp ? UIClick.Pager(request.Model, "ProjectUI", new WebMeta().Put("Id", p.Id), true) : new UIClick(p.Id.ToString()).Send(request.Model, "ProjectUI")));

                                    cellUI.Button("创立人", null, 0xccc);

                                    // 
                                    ui2.Add(cellUI);
                                }
                                subMebs.RemoveAll(d => mp.Exists(p => p.Id == d.project_id));
                                //if(mp!=nul)
                            }
                            foreach (var vd in subMebs)
                            {
                                var p = projects.Find(u => u.Id == vd.project_id);
                                if (p == null)
                                {
                                    continue;
                                }

                                var sub = subs.Find(s => s.project_id == p.Id);

                                var cellUI = new UIIconNameDesc(new UIIconNameDesc.Item(webr.ResolveUrl(p.Id.Value, "1", "4"), p.Caption,
                                    String.Format("知识{0}篇", sub == null ? 0 : sub.Seq))
                                    .Click(request.IsApp ? UIClick.Pager(request.Model, "ProjectUI", new WebMeta().Put("Id", p.Id), true) : new UIClick(p.Id.ToString()).Send(request.Model, "ProjectUI")));
                                switch (vd.AuthType)
                                {
                                    case WebAuthType.Admin:
                                        cellUI.Button("管理员", null, 0xccc);
                                        break;
                                    case WebAuthType.User:
                                        cellUI.Button("专栏作家", null, 0xccc);
                                        break;
                                }
                                // 
                                ui2.Add(cellUI);
                            }
                        }
                        var m = subEntity.Count();
                        int total = m;

                        ui.IsNext = total > start + limit;
                        if (start == 0 && ids.Count == 0 && pids.Count == 0)
                        {

                            ui2.Add("Desc", new UMC.Web.WebMeta().Put("desc", "尚未有参与项目").Put("icon", "\uF016")
                                , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                        }
                        response.Redirect(ui);
                    }
                    break;

                case "Dynamic":
                    {
                        var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectDynamic>();
                        subEntity.Where.And().Equal(new ProjectDynamic
                        {
                            user_id = userId
                        }).Entities.Order.Desc(new ProjectDynamic { Time = 0 });

                        var subs = new List<ProjectDynamic>();
                        var uids = new List<Guid>();
                        subEntity.Query(start, limit, dr =>
                        {
                            subs.Add(dr);
                            uids.Add(dr.project_id ?? Guid.Empty);

                        });
                        var cates = new List<Project>();
                        if (uids.Count > 0)
                        {
                            Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project { Id = uids[0] }, uids.ToArray())
                                    .Entities.Query(dr => cates.Add(dr));
                        }
                        foreach (var sub in subs)
                        {

                            var user2 = cates.Find(d => d.Id == sub.user_id) ?? new Project();

                            var data = new WebMeta().Put("alias", user2.Caption, "desc", sub.Explain).Put("time", Utility.TimeSpan(sub.Time ?? 0))
                                .Put("name", sub.Title)
                                .Put("src", webr.ResolveUrl(sub.project_id ?? Guid.Empty, "1", 5));
                            data.Put("click", new UIClick(new WebMeta().Put("Id", sub.user_id).Put("Time", sub.Time)).Send(request.Model, "Dynamic"));
                            var cell = UICell.Create("IconNameDesc", data);

                            cell.Format.Put("desc", "{alias} {time} {desc}");
                            cell.Style.Name("name").Size(14);
                            ui2.Add(cell);
                        }
                        ui.Total = subEntity.Count();
                        if (ui.Total == 0)
                        {

                            ui2.Add("Desc", new UMC.Web.WebMeta().Put("desc", "尚未有个人动态").Put("icon", "\uF016")
                                , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                        }
                    }
                    break;


            }



            response.Redirect(ui);

        }
    }
}