using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Web.UI;
using UMC.Web;
using UMC.Data.Entities;
using System.Collections;

namespace UMC.Subs.Activities
{



    [Mapping("Subject", "Select", Auth = WebAuthType.All, Desc = "选择我的图文", Category = 1)]
    class SubjectSelectActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var key = this.AsyncDialog("Key", g => this.DialogValue("Promotion"));
            if (String.IsNullOrEmpty(request.SendValue) == false)
            {
                var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                buider.CloseEvent("UI.Event");
                this.Context.Send(buider.Builder(), true);
            }

            var user = Security.Identity.Current;
            var ui = UISection.Create();

            var paramsKey = request.SendValues ?? new UMC.Web.WebMeta();

            var start = UMC.Data.Utility.Parse((paramsKey["start"] ?? "0").ToString(), 0);
            var limit = UMC.Data.Utility.Parse((paramsKey["limit"] ?? "25").ToString(), 25);



            var Type = this.AsyncDialog("Type", "Project");
            var itemId = Utility.Guid(this.AsyncDialog("Item", cId =>
            {
                return this.DialogValue("none");
            }));
            switch (Type)
            {
                case "Project":
                    {

                        ui.Title = new UITitle("我的专栏项目"); ;
                        var ids = new List<Guid>();
                        var pros = new List<Project>();
                        Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project { user_id = user.Id })
                            .Or().In("Id", Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember { user_id = user.Id })
                            .And().In(new ProjectMember { AuthType = WebAuthType.Admin }, WebAuthType.User).Entities.Script(new ProjectMember { project_id = Guid.Empty })).Entities.Order.Asc(new Project { Sequence = 0 }).Entities.Query(dr =>
                            {
                                pros.Add(dr); ids.Add(dr.Id.Value);
                            });

                        if (ids.Count > 0)
                        {
                            var webr = UMC.Data.WebResource.Instance();
                            var subs = new List<Subject>();
                            Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                                       .Where.And().In(new Subject { project_id = ids[0] }, ids.ToArray())
                                       .Entities.GroupBy(new Subject { project_id = Guid.Empty }).Count(new Subject { Seq = 0 }).Query(dr => subs.Add(dr));

                            foreach (var p in pros)
                            {
                                var sub = subs.Find(s => s.project_id == p.Id);
                                var desc = new UIIconNameDesc(new UIIconNameDesc.Item(webr.ResolveUrl(p.Id.Value, "1", "4"), p.Caption,
                                    String.Format("知识{0}篇", sub == null ? 0 : sub.Seq))
                                    .Click(Web.UIClick.Query(new WebMeta().Put("Item", p.Id).Put("Type", "Items"))));

                                if (p.user_id == user.Id)
                                {
                                    desc.Button("我的", null, 0x25b864);
                                }
                                ui.Add(desc);

                            }
                        }
                        else
                        {
                            ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "你未有专栏的项目").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                                new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                        }
                        response.Redirect(ui);
                    }
                    break;
                case "Items":
                    {
                        ui.Title = new UITitle("选择专栏");
                        var subs = new List<Subject>();
                        Utility.CMS.ObjectEntity<Subject>().Where.And().In(new Subject { project_id = itemId }).Entities
                            .GroupBy(new Subject { project_item_id = Guid.Empty })
                            .Count(new Subject { Look = 0 })
                            .Query(dr => subs.Add(dr));

                        var project = Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project { Id = itemId }).Entities.Single();

                        ui.AddCell('\uf112', "返回上一级", project.Caption, new UIClick(new WebMeta().Put("Key", key).Put("Type", "Project")) { Key = "Query" });

                        var u3 = ui.NewSection();
                        var projects = new List<ProjectItem>();
                        var projectEntity = Utility.CMS.ObjectEntity<ProjectItem>();
                        projectEntity.Where.And().In(new ProjectItem { project_id = itemId });

                        projectEntity.Order.Asc(new ProjectItem { Sequence = 0 });

                        projectEntity.Query(dr =>
                        {
                            var su = subs.Find(s => s.project_item_id == dr.Id);
                            u3.AddCell('\uf022', dr.Caption, String.Format("{0}篇", su == null ? 0 : su.Look)
                             , new UIClick(new WebMeta().Put("Key", key).Put("Item", dr.Id).Put("Type", "Portfolio")) { Key = "Query" });
                        });
                    }
                    break;
                case "Portfolio":
                    {

                        ui.Title = new UITitle("选择文集");
                        var projectItem = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new ProjectItem { Id = itemId }).Entities.Single();
                        var subs = new List<Subject>();
                        Utility.CMS.ObjectEntity<Subject>().Where.And().In(new Subject { project_item_id = itemId }).Entities
                            .GroupBy(new Subject { portfolio_id = Guid.Empty })
                            .Count(new Subject { Look = 0 })
                            .Query(dr => subs.Add(dr));

                        ui.AddCell('\uf112', "返回上一级", projectItem.Caption, new UIClick(new WebMeta().Put("Key", key).Put("Type", "Items").Put("Item", projectItem.project_id)) { Key = "Query" });

                        var ui3 = ui.NewSection();

                        Utility.CMS.ObjectEntity<Portfolio>().Where.And().Equal(new Portfolio { project_item_id = projectItem.Id }).Entities
                            .Order.Asc(new Portfolio { Sequence = 0 }).Entities
                            .Query(dr =>
                            {
                                var su = subs.Find(s => s.portfolio_id == dr.Id);
                                ui3.AddCell('\uf22b', dr.Caption, String.Format("{0}篇", su == null ? 0 : su.Look)
                                                    , new UIClick(new WebMeta().Put("Key", key).Put("Item", dr.Id).Put("Type", "Subs")) { Key = "Query" });
                            });



                    }
                    break;
                case "Subs":
                    {
                        var ui3 = ui;
                        if (start == 0)
                        {
                            ui.Title = new UITitle("图文选择");
                            var portfolio = Utility.CMS.ObjectEntity<Portfolio>().Where.And().In(new Portfolio { Id = itemId }).Entities.Single();


                            ui.AddCell('\uf112', "返回上一级", portfolio.Caption, new UIClick(new WebMeta().Put("Key", key).Put("Type", "Portfolio").Put("Item", portfolio.project_item_id)) { Key = "Query" });


                            ui3 = ui.NewSection();

                        }
                        ui3.Key = "Subs";
                        var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
                        subEntity.Order.Desc(new Subject { ReleaseDate = DateTime.Now });
                        subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1, portfolio_id = itemId }).And().Greater(new Subject { Status = 0, Visible = -1 });
                        this.Search(request.Model, ui3, subEntity, key, start, limit);
                        if (ui3.Total == 0)
                        {
                            ui3.Add("Desc", new UMC.Web.WebMeta().Put("desc", "尚未有发布的项目资讯").Put("icon", "\uF016")
                                , new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"), new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                        }



                    }
                    break;
                case "Sub":
                    {
                        var su = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>().Where.And().Equal(new Subject { Id = itemId.Value }).Entities.Single();

                        this.Context.Send(new UMC.Web.WebMeta().UIEvent(key, new Web.ListItem(su.Title, su.Id.ToString())), true);

                    }
                    break;
            }
            response.Redirect(ui);

        }

        void Search(String model, UISection ui, Data.Sql.IObjectEntity<Subject> subEntity, String key, int start, int limit)
        {
            var subs = new List<Subject>();
            var cateids = new List<Guid>();
            var ids = new List<Guid>();
            var itemIds = new List<Guid>();

            var search = UMC.Data.Reflection.CreateInstance<Subject>();
            search.DataJSON = null;
            search.Content = null;
            search.ConfigXml = null;
            subEntity.Query(search, start, limit, dr =>
            {
                subs.Add(dr);
                if (dr.project_id.HasValue)
                    cateids.Add(dr.project_id ?? Guid.Empty);
                ids.Add(dr.Id.Value);
                if (dr.project_item_id.HasValue)
                    itemIds.Add(dr.project_item_id.Value);
            });
            var cates = new List<Project>();
            var pitems = new List<Data.Entities.ProjectItem>();


            if (itemIds.Count > 0)
            {
                Utility.CMS.ObjectEntity<ProjectItem>().Where.And().In(new ProjectItem
                {
                    Id = itemIds[0]
                }, itemIds.ToArray())
                         .Entities.Query(dr => pitems.Add(dr));
            }
            if (cateids.Count > 0)
            {
                Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project { Id = cateids[0] }, cateids.ToArray())
                    .Entities.Query(dr => cates.Add(dr));


            }
            var pics = new List<UMC.Data.Entities.Picture>();
            if (ids.Count > 0)
            {
                Utility.CMS.ObjectEntity<Data.Entities.Picture>().Where.And().In(new Data.Entities.Picture
                {
                    group_id = ids[0]
                }, ids.ToArray()).Entities.Order.Asc(new Data.Entities.Picture { Seq = 0 }).Entities.Query(g => pics.Add(g));
            };
            var items = ui;
            var webr = UMC.Data.WebResource.Instance();


            foreach (var sub in subs)
            {
                if (sub.Visible == 0)
                {
                    continue;
                }
                var ims = new List<UMC.Data.Entities.Picture>();
                pics.RemoveAll(g =>
                {
                    if (g.group_id == sub.Id)
                    {
                        ims.Add(g);
                        return true;
                    }
                    return false;
                });
                var imgs = new List<String>();

                if (ims.Count > 0)
                {
                    switch (ims.Count)
                    {
                        case 2:
                        case 1:

                            imgs.Add(webr.ResolveUrl(sub.Id.Value, 1, "0") + "!cms" + ((sub.IsPicture ?? false) ? "1" : "3") + "?_ts=" + UMC.Data.Utility.TimeSpan(ims[0].UploadDate.Value));

                            break;
                        default:
                            for (var i = 0; i < 3; i++)
                            {
                                imgs.Add(webr.ResolveUrl(sub.Id.Value, ims[i].Seq ?? 0, "0") + "!cms3?_ts=" + UMC.Data.Utility.TimeSpan(ims[i].UploadDate.Value));
                            }
                            break;
                    }
                }
                var click = new Web.UIClick("Key", key, "Item", sub.Id.ToString(), "Type", "Sub").Send(model, "Select");
                var data = new UMC.Web.WebMeta().Put("title", sub.Title).Put("reply", (sub.Reply ?? 0).ToString()).Put("look", (sub.Look ?? 0).ToString());


                var cate = cates.Find(g => g.Id == sub.project_id);
                var pitem = pitems.Find(g => g.Id == sub.project_item_id);
                data.Put("pname", cate == null ? "草稿" : cate.Caption);
                data.Put("iname", pitem == null ? "" : pitem.Caption);
                data.Put("time", Utility.GetDate(sub.ReleaseDate));
                if (cate != null && pitem != null)
                {
                    data.Put("spa", new WebMeta().Put("id", sub.Id).Put("path", String.Format("{0}/{1}/{2}", cate.Code, pitem.Code, sub.Code)));
                }
                else
                {
                    data.Put("sub-id", Utility.Guid(sub.Id.Value));//.Put("path", String.Format("{0}/{1}/{2}", cate.Code, pitem.Code, sub.Code)));

                }
                data.Put("desc", sub.Description);
                UICell cell;
                switch (imgs.Count)
                {
                    case 0:
                        cell = new UICMS(click, data);
                        break;
                    default:
                        cell = (sub.IsPicture ?? false) ? new UICMS(click, data, imgs[0], true) : (ims.Count > 2 ? new UICMS(click, data, imgs[0], imgs[1], imgs[2]) : new UICMS(click, data, imgs[0]));


                        break;
                }
                cell.Format.Put("left", "{pname} {iname} {time}");
                cell.Style.Name("licon", new UIStyle().Size(12).Font("wdk")).Name("ricon", new UIStyle().Size(12).Font("wdk"));
                cell.Style.Name("pname").Color(0x777);
                cell.Style.Name("iname").Color(0x777);
                cell.Format.Put("right", "\uF06E{1:licon} {look}   \uF0E6{1:ricon} {reply}");
                items.Add(cell);
            }
            ui.Total = subEntity.Count(); ;

        }
    }
}