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
    class SubjectSearchActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
            var webr = UMC.Data.WebResource.Instance();
            var Id = this.AsyncDialog("Id", "auto");
            if (String.Equals(Id, "auto") == false)
            {
                var type = this.AsyncDialog("Type", "Info");
                var sid = UMC.Data.Utility.Guid(request.SendValue, true);

                subEntity.Where.And().Equal(new UMC.Data.Entities.Subject { Id = sid });

                var sub = subEntity.Single() ?? new UMC.Data.Entities.Subject { Id = sid };
                var hash = UMC.Data.Reflection.PropertyToDictionary(sub);

                hash["time"] = UMC.Data.Utility.GetDate(sub.ReleaseDate);
                if (type == "Info")
                {
                    var images = new List<UMC.Data.Entities.Picture>();
                    Utility.CMS.ObjectEntity<Data.Entities.Picture>().Where.And().Equal(new Data.Entities.Picture { group_id = sid.Value })
                        .Entities.Order.Asc(new Data.Entities.Picture { Seq = 0 }).Entities.Query(g => images.Add(g));

                    var imgs = new System.Data.DataTable();
                    imgs.Columns.Add("src");

                    if (images.Count > 0)
                    {
                        switch (images.Count)
                        {
                            case 2:
                            case 1:
                                imgs.Rows.Add(webr.ResolveUrl(sub.Id.Value, 1, "0") + "!cms1?_ts=" + UMC.Data.Utility.TimeSpan(images[0].UploadDate.Value));

                                break;
                            default:
                                for (var i = 0; i < 3; i++)
                                {
                                    imgs.Rows.Add(webr.ResolveUrl(sub.Id.Value, images[i].Seq ?? 0, "0") + "!cms3?_ts=" + UMC.Data.Utility.TimeSpan(images[i].UploadDate.Value));
                                }
                                break;
                        }
                    }
                    if (String.IsNullOrEmpty(sub.ContentType))
                    {
                        hash["ContentType"] = "text/html";
                    }
                    hash["Images"] = imgs;
                }
                else
                {
                    hash = new Hashtable();
                }
                if (String.IsNullOrEmpty(sub.Code) == false && sub.project_id.HasValue && sub.project_item_id.HasValue)
                {

                    var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
                    {
                        Id = sub.project_id
                    }).Entities.Single();
                    var projectItem = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new ProjectItem
                    {
                        Id = sub.project_item_id
                    }).Entities.Single();
                    if (project != null && projectItem != null)
                    {
                        if (type == "Path")
                        {
                            this.Context.Send("Subject.Path", new WebMeta().Put("Path", String.Format("{0}/{1}/{2}", project.Code, projectItem.Code, sub.Code)), true);
                        }
                        hash["Path"] = String.Format("{0}/{1}/{2}", project.Code, projectItem.Code, sub.Code);

                    }
                }


                response.Redirect(hash);


            }
            var form = request.SendValues ?? new UMC.Web.WebMeta();
            if (request.IsCashier == false)
            {
                subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1 });
            }
            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

            string sort = form[("sort")] as string;
            string dir = form[("dir")] as string;

            var category = form["Category"] as string;
            var pics = new List<UMC.Data.Entities.Picture>();

            Guid? CategoryId = UMC.Data.Utility.Guid(category);

            var Keyword = (form["Keyword"] as string ?? String.Empty);
            switch (Keyword)
            {
                case "发布":
                case "等候发布":
                case "发布中":
                    subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 0 });
                    Keyword = String.Empty;
                    break;
                case "已发布":
                    subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1 });
                    Keyword = String.Empty;
                    break;
                case "不发布":
                    subEntity.Where.And().Equal(new Data.Entities.Subject { Status = -1 });
                    Keyword = String.Empty;
                    break;
            }
            if (String.IsNullOrEmpty(Keyword) == false)
            {
                subEntity.Where.And().Like(new Subject { Title = Keyword });
            }
            subEntity.Where.And().Equal(new Subject
            {
                Visible = Utility.IntParse(form["Visible"], 1)
            });
            if (!String.IsNullOrEmpty(sort) && sort.StartsWith("_") == false)
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
                subEntity.Order.Desc(new Subject { LastDate = DateTime.Now });
            }
            var subs = new List<Subject>(); ;
            //var cateids = new List<Guid>();
            var ids = new List<Guid>();
            var search = UMC.Data.Reflection.CreateInstance<Subject>();
            search.DataJSON = null;
            //search.Description = null;
            search.Content = null;
            search.ConfigXml = null;
            subEntity.Query(search, start, limit, dr =>
             {
                 subs.Add(dr);
                 //cateids.Add(dr.category_id ?? Guid.Empty);
                 ids.Add(dr.Id.Value);
             });
            //var cates = new List<Category>();
            if (ids.Count > 0)
            {
                Utility.CMS.ObjectEntity<Data.Entities.Picture>().Where.And().In(new Data.Entities.Picture { group_id = ids[0] }, ids.ToArray())
                    .Entities.Order.Asc(new Data.Entities.Picture { Seq = 0 }).Entities.Query(g => pics.Add(g));
            }
            var data = new System.Data.DataTable();
            data.Columns.Add("id");
            data.Columns.Add("title");
            data.Columns.Add("desc");
            data.Columns.Add("time");
            data.Columns.Add("reply");
            data.Columns.Add("look");
            data.Columns.Add("favs");
            data.Columns.Add("tipoffs");
            data.Columns.Add("last");
            data.Columns.Add("status");
            data.Columns.Add("poster");
            data.Columns.Add("images", typeof(System.Data.DataTable));
            //data.Columns.Add("category", typeof(System.Collections.Hashtable));
            data.Columns.Add("_CellName");
            //var count = 108;
            foreach (var sub in subs)
            {
                if (sub.Visible == 0 && request.IsCashier == false)
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
                var imgs = new System.Data.DataTable();
                imgs.Columns.Add("src");

                if (ims.Count > 0)
                {
                    switch (ims.Count)
                    {
                        case 2:
                        case 1:

                            imgs.Rows.Add(webr.ResolveUrl(sub.Id.Value, 1, "0") + "!cms" + ((sub.IsPicture ?? false) ? "1" : "3") + "?_ts=" + UMC.Data.Utility.TimeSpan(ims[0].UploadDate.Value));

                            break;
                        default:
                            for (var i = 0; i < 3; i++)
                            {
                                imgs.Rows.Add(webr.ResolveUrl(sub.Id.Value, ims[i].Seq ?? 0, "0") + "!cms3?_ts=" + UMC.Data.Utility.TimeSpan(ims[i].UploadDate.Value));
                            }
                            break;
                    }
                }
                else
                {
                    imgs.Rows.Add(webr.ResolveUrl(sub.Id.Value, 1, "0") + "!cms1");
                }
                var s = "发布中";
                switch (sub.Status ?? 0)
                {
                    case -2:
                        s = "驳回";
                        break;
                    case -1:
                        s = "不发布";
                        break;
                    case 0:
                        s = "审阅中";
                        break;
                    case 1:
                        s = "已发布";
                        break;
                }
                data.Rows.Add(sub.Id, sub.Title, sub.Description, sub.ReleaseDate.HasValue ? Utility.GetDate(sub.ReleaseDate) : "未发布"
                    , sub.Reply ?? 0, sub.Look ?? 0, sub.Favs ?? 0, sub.TipOffs ?? 0, UMC.Data.Utility.GetDate(sub.LastDate), s, sub.Poster
                    , imgs, ims.Count > 2 ? "CMSThree" : ((sub.IsPicture ?? false) ? "CMSMax" : "CMSOne"));
            }
            var hashc = new System.Collections.Hashtable();
            hashc["data"] = data;
            var total = subEntity.Count(); ;
            hashc["total"] = total;// subEntity.Count();
            if (total == 0)
            {
                hashc["msg"] = "现在还未有发布的内容";
            }
            response.Redirect(hashc);
        }

    }
}