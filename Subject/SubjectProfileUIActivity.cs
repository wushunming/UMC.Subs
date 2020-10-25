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

    class SubjectProfileUIActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var ProjectId = Utility.Guid(this.AsyncDialog("Id", g =>
            {
                this.Prompt("请输入项目");

                return this.DialogValue("Project");
            })) ?? Guid.Empty;

            UISection ui = null;
            var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project { user_id = ProjectId }).Entities.Single();

            //var svs = request.SendValues ?? new UMC.Web.WebMeta();
            var form = (request.SendValues ?? new UMC.Web.WebMeta()).GetDictionary();
            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);
            var nextKey = this.AsyncDialog("NextKey", g => this.DialogValue("Header")); ;
            if (start == 0 && String.Equals(nextKey, "Header"))
            {

                var logoUrl = UMC.Data.WebResource.Instance().ResolveUrl(String.Format("{0}{1}/1/0.jpg!200", UMC.Data.WebResource.ImageResource, project.Id));

                //    var mcode = "您未登录";

                var members = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                      .Where.And().Equal(new Data.Entities.ProjectMember { project_id = project.Id })
                      .Entities.Count() + 1;
                var subs = Utility.CMS.ObjectEntity<Subject>().Where.And().Equal(new Data.Entities.Subject { project_id = project.Id }).Entities.Count();


                //Sections.Add(cmsText);

                String mcode = String.Format("成员 {0} 图文 {1}", members, subs);

                var Discount = new UIHeader.Profile(project.Caption, mcode, logoUrl);


                var color = 0x63b359;
                Discount.Gradient(color, color);
                var header = new UIHeader();
                var title = UITitle.Create();

                title.Title = "项目介绍";
                header.AddProfile(Discount, "{number}", "{amount}");


                ui = UISection.Create(header, title);
                if (String.IsNullOrEmpty(project.Description) == false)
                {
                    var cmsText = UICell.Create("CMSText", new UMC.Web.WebMeta().Put("text", project.Description));
                    cmsText.Style.Size(14).Color(0x999);
                }


            }
            else
            {
                ui = UISection.Create();

            }
            var items = new List<WebMeta>();
            items.Add(new UMC.Web.WebMeta().Put("text", "文章", "search", "Subs"));
            items.Add(new UMC.Web.WebMeta().Put("text", "成员", "search", "Member"));
            items.Add(new UMC.Web.WebMeta().Put("text", "动态", "search", "Dynamic"));
            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
            var selectIndex = UMC.Data.Utility.IntParse(this.AsyncDialog("selectIndex", g => this.DialogValue("0")), 0);
            var webr = UMC.Data.WebResource.Instance();
            switch (selectIndex)
            {
                case 0:
                    {

                        var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
                        subEntity.Order.Desc(new Subject { ReleaseDate = DateTime.Now });
                        subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1, IsDraught = false, project_id = project.Id });
                        SubjectUIActivity.Search(ui, subEntity, start, limit);
                        response.Redirect(ui);

                    }
                    break;
                case 1:
                    {
                        int mlimit = limit * 4;
                        int mstart = start * 4;
                        var ids = new List<Guid>();
                        var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>();
                        subEntity.Order.Desc(new ProjectMember { CreationTime = DateTime.Now });
                        subEntity.Query(mstart, mlimit, dr => ids.Add(dr.user_id.Value));
                        if (mstart == 0)
                        {
                            ids.Add(project.user_id.Value);
                        }
                        if (ids.Count > 0)
                        {
                            var users = new List<User>();
                            UMC.Data.Database.Instance().ObjectEntity<User>()
                                .Where.And().In(new User { Id = ids[0] }, ids.ToArray()).Entities.Query(dr => users.Add(dr));

                            var icons = new List<UIEventText>();
                            foreach (var v in users)
                            {

                                icons.Add(new UIEventText(v.Alias).Src(webr.ResolveUrl(v.Id.Value, "1", "4")));

                                if (icons.Count % 4 == 0)
                                {
                                    ui.AddIcon(icons.ToArray());
                                    icons.Clear();
                                }

                            }
                            if (icons.Count > 0)
                                ui.AddIcon(icons.ToArray());
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
                case 2:
                    {
                        var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectDynamic>();
                        subEntity.Where.And().Equal(new ProjectDynamic
                        {
                            project_id = project.Id
                        }).Entities.Order.Desc(new ProjectDynamic { Time = DateTime.MinValue });

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
                            UMC.Data.Database.Instance().ObjectEntity<User>().Where.And().In(new User { Id = uids[0] }, uids.ToArray())
                                    .Entities.Query(dr => cates.Add(dr));
                        }
                        foreach (var sub in subs)
                        {
                            var sType = "成员动态";
                            switch (sub.Type)
                            {
                                case DynamicType.Member:
                                    break;
                                case DynamicType.Portfolio:
                                    sType = "文集动态";
                                    break;
                                case DynamicType.Project:
                                    sType = "项目动态";
                                    break;
                                case DynamicType.Subject:
                                    sType = "文档动态";
                                    break;
                                case DynamicType.ProjectItem:
                                    sType = "栏位动态";
                                    break;
                            }
                            //



                            var user2 = cates.Find(d => d.Id == sub.user_id) ?? new User();

                            var data = new WebMeta().Put("alias", user2.Alias, "type", sType, "desc", sub.Explain).Put("time", sub.Time)
                                .Put("title", sub.Title);
                            var cell = UIImageTitleBottom.Create(webr.ResolveUrl(sub.user_id ?? Guid.Empty, "1", 5), data);

                            cell.Format.Put("left", "{alias} {time} {desc}").Put("right", "{type}");
                            cell.Style.Name("image-radius", 30);
                            ui.Add(cell);
                            //data.Rows.Add(sub.user_id, sub.Title, sub.Explain, sub.Time, sType, webr.ResolveUrl(sub.user_id ?? Guid.Empty, "1", 5),
                            //user2.Alias);
                        }
                        ui.Total = subEntity.Count();
                    }
                    break;


            }



            //var appKey = UMC.Security.Principal.Current.AppKey ?? Guid.Empty;


            //var li = ui.NewSection();// UIClick.Pager("Member", "Order", new WebADNuke.Web.WebMeta().Put("type", "App", "selectIndex", "0"))
            //li.AddCell("我的订单", "查看全部", UIClick.Pager("Member", "Order", new UMC.Web.WebMeta().Put("type", "App", "selectIndex", "0")));
            //li.AddCell("我的佣金", "", new Web.UIClick() { Command = "Commission", Model = "Member" });
            //li.AddCell("我的收藏", new Web.UIClick() { Command = "Favs", Model = request.Model });
            //li.AddCell("我的优惠券", new Web.UIClick() { Command = "Coupons", Model = "Member" });
            //if (appKey == Guid.Empty)
            //{

            //    var cate = Utility.CMS.ObjectEntity<UMC.Data.Entities.Category>().Where.And().Equal(new Data.Entities.Category { user_id = user.Id })
            //          .Entities.Count();
            //    if (cate > 0)
            //    {
            //        li.NewSection().AddCell("我的版务", new Web.UIClick() { Command = "Apply", Model = "Subject" });

            //    }
            //    li.AddCell("我的图文", new Web.UIClick() { Command = "Self", Model = "Subject" });
            //    li.NewSection().AddCell("积分政策", UIClick.Pager("Subject", "UIData", new UMC.Web.WebMeta().Put("Id", "Subject.Points")));
            //}
            //else
            //{
            //li.AddCell("我的图文", new Web.UIClick() { Command = "Self", Model = "Subject" });
            //li.NewSection().AddCell("卡券分享", "被领取即可获的收益", UIClick.Pager("Corp", "Coupons"));
            ////}


            //if (request.IsApp)
            //{
            //    ui.NewSection().AddCell('\uf083', "扫一扫", "", new Web.UIClick() { Key = "Scanning" });
            //    //.AddCell('\uf0c5', "软文转码", "将检测粘贴板", new Web.UIClick() { Key = "CaseCMS" });
            //    ui.NewSection()
            //        //.AddCell('\uf19c', "切换企业", "", Web.UIClick.Pager("Platform", "Corp", true))
            //        .AddCell('\uf013', "设置", "", Web.UIClick.Pager("UI", "Setting", true, "Close"));


            //}
            //else
            //{
            //    ui.NewSection()
            //        .AddCell('\uf013', "设置", "", Web.UIClick.Pager("UI", "Setting", true, "Close"));

            //}

            response.Redirect(ui);


            //}
        }
    }
}