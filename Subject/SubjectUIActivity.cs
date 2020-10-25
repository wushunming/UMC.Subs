using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using System.IO;
using UMC.Web;
using UMC.Web.UI;

namespace UMC.Subs.Activities
{



    class SubjectUIActivity : WebActivity
    {
        public static void Search(String model, UISection ui, Data.Sql.IObjectEntity<Subject> subEntity, int start, int limit)
        {
            Search(ui, subEntity, model, "UIData", start, limit, false);
        }

        public static void Search(UISection ui, Data.Sql.IObjectEntity<Subject> subEntity, String model, String cmd, int start, int limit, bool isblock)
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
                var click = new Web.UIClick(sub.Id.ToString()).Send(model, cmd);
                var data = new UMC.Web.WebMeta().Put("title", sub.Title).Put("reply", (sub.Reply ?? 0).ToString()).Put("look", (sub.Look ?? 0).ToString());


                var cate = cates.Find(g => g.Id == sub.project_id);
                var pitem = pitems.Find(g => g.Id == sub.project_item_id);
                data.Put("pname", cate == null ? "草稿" : cate.Caption);
                if (sub.project_id == sub.user_id)
                {
                    data.Put("iname", "");
                }
                else
                {
                    data.Put("iname", pitem == null ? "" : pitem.Caption);
                }
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
                if (isblock)
                {
                    cell.Style.Name("block").Size(12).Color(0xaaa).Font("wdk").Click(UIClick.Click(new UIClick("Id", sub.Id.ToString(), "Type", "Block").Send(model, "TipOff")));
                    data.Put("block", "\uea0d");

                    cell.Format.Put("right", "\uF06E{1:licon} {look}   \uF0E6{1:ricon} {reply}  {2:block}");
                }
                else
                {

                    cell.Format.Put("right", "\uF06E{1:licon} {look}   \uF0E6{1:ricon} {reply}");
                }
                items.Add(cell);
            }
            ui.Total = subEntity.Count(); ;

        }

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            subEntity.Where.And().Greater(new Subject { Visible = -1 });

            var webr = UMC.Data.WebResource.Instance();
            var form = request.SendValues ?? new UMC.Web.WebMeta();

            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

            string sort = form[("sort")] as string;
            string dir = form[("dir")] as string;

            var category = form["Project"] as string;

            var pics = new List<UMC.Data.Entities.Picture>();

            Guid? CategoryId = null;

            var Keyword = (form["Keyword"] as string ?? String.Empty);
            if (String.IsNullOrEmpty(category) == false)
            {
                CategoryId = UMC.Data.Utility.Guid(category);
                if (CategoryId.HasValue)
                {
                    subEntity.Where.And().Equal(new Data.Entities.Subject { project_id = CategoryId, Status = 1 });

                }
                else
                {
                    var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                        .Where.And().Equal(new Project { Code = category }).Entities.Single();
                    if (project != null)
                    {
                        CategoryId = project.Id;
                        subEntity.Where.And().Equal(new Data.Entities.Subject { project_id = project.Id, Status = 1 });

                    }
                    else
                    {
                        subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1, IsDraught = false });
                    }

                }

            }
            else
            {
                subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1, IsDraught = false });
            }
            if (String.IsNullOrEmpty(Keyword) == false)
            {
                subEntity.Where.And().Like(new Subject { Title = Keyword });
            }
            if (!String.IsNullOrEmpty(sort))
            {
                if (dir == "DESC")
                {
                    subEntity.Order.Desc(sort);
                }
                else
                {
                    subEntity.Order.Asc(sort);
                }
            }
            else
            {
                subEntity.Order.Desc(new Subject { ReleaseDate = DateTime.Now });
            }
            var ui = UISection.Create();
            var items = ui;
            if (start == 0)
            {
                if (String.IsNullOrEmpty(Keyword) == false)
                {
                    ui.Title = new UITitle(String.Format("搜索“{0}”资讯", Keyword));
                }
                else if (CategoryId.HasValue)
                {
                    if (ui.Length > 0)
                    {
                        items = ui.NewSection();
                    }
                }

            }
            var user = Security.Identity.Current;


            subEntity.Where.And().NotIn("user_id", Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectBlock>().Where.And().Equal(new ProjectBlock { user_id = user.Id, Type = 0 })
                .Entities.Script(new ProjectBlock { ref_id = Guid.Empty })).And().NotIn("project_id", Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectBlock>().Where.And().Equal(new ProjectBlock { user_id = user.Id, Type = 1 })
                .Entities.Script(new ProjectBlock { ref_id = Guid.Empty }));

            Search(ui, subEntity, request.Model, "UIData", start, limit, request.IsApp);

            if (ui.Total == 0)
            {
                ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "还未有发布的项目资讯").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),
                    new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
            }
            response.Redirect(ui);
        }

    }
}