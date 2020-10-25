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
    class SubjectRowsActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            var webr = UMC.Data.WebResource.Instance();
            var form = request.SendValues ?? new UMC.Web.WebMeta();
            if (form.Count == 0 && String.IsNullOrEmpty(request.SendValue) == false)
            {
                var sid = UMC.Data.Utility.Guid(request.SendValue);
                if (sid.HasValue)
                {
                    subEntity.Where.And().Equal(new UMC.Data.Entities.Subject { Id = sid });

                    var su2bs = subEntity.Single() ?? new UMC.Data.Entities.Subject { Id = sid };
                    var hash = UMC.Data.Reflection.PropertyToDictionary(su2bs);
                    //if (su2bs.category_id.HasValue)
                    //{
                    //    var cate = Utility.CMS.ObjectEntity<Category>().Where.And().Equal(new Category { Id = su2bs.category_id.Value })
                    //          .Entities.Single();
                    //    if (cate != null)
                    //    {
                    //        hash["Category"] = cate.Caption;
                    //    }
                    //    else
                    //    {
                    //        hash["Category"] = "未知分类";
                    //    }

                    //}
                    //else
                    //{
                    //    hash["Category"] = "未分类";
                    //}

                    hash["time"] = UMC.Data.Utility.GetDate(su2bs.ReleaseDate);
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
                                imgs.Rows.Add(webr.ResolveUrl(su2bs.Id.Value, 1, "0") + "!cms1?_ts=" + UMC.Data.Utility.TimeSpan(images[0].UploadDate.Value));

                                break;
                            default:
                                for (var i = 0; i < 3; i++)
                                {
                                    imgs.Rows.Add(webr.ResolveUrl(su2bs.Id.Value, images[i].Seq ?? 0, "0") + "!cms3?_ts=" + UMC.Data.Utility.TimeSpan(images[i].UploadDate.Value));
                                }
                                break;
                        }
                    }

                    hash["Images"] = imgs;

                    response.Redirect(hash);
                }
                else
                {
                    response.Redirect(new UMC.Data.Entities.Subject { Id = Guid.NewGuid() });
                }
            }
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
            //if (CategoryId.HasValue)
            //{
            //    subEntity.Where.And().Equal(new Data.Entities.Subject { category_id = CategoryId });
            //    if (String.IsNullOrEmpty(Keyword) == false)
            //    {
            //        subEntity.Where.And().Like(new Subject { Title = Keyword });
            //    }
            //}
            //else
            //{
            //    var cate = Utility.CMS.ObjectEntity<Category>().Where.And().Equal(new Category { Caption = Keyword })
            //          .Entities.Single();
            //    if (cate != null)
            //    {
            //        subEntity.Where.And().Equal(new Data.Entities.Subject { category_id = cate.Id });
            //    }
            //    else 
            if (String.IsNullOrEmpty(Keyword) == false)
            {
                subEntity.Where.And().Like(new Subject { Title = Keyword });
            }
            //}

            if (request.IsCashier == false || request.IsApp)
            {
                subEntity.Where.And().Equal(new Subject { Visible = 1 });
            }
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
                subEntity.Order.Desc(new Subject { ReleaseDate = DateTime.Now });
            }
            var subs = new List<Subject>();
            var cateids = new List<Guid>();
            var ids = new List<Guid>();
            subEntity.Query(start, limit, dr =>
            {
                subs.Add(dr);
                //cateids.Add(dr.category_id ?? Guid.Empty);
                ids.Add(dr.Id.Value);
            });
            //var cates = new List<Category>();
            //if (ids.Count > 0)
            //{
            //    Utility.CMS.ObjectEntity<Category>().Where.And().In(new Category { Id = Guid.Empty }, cateids.ToArray())
            //        .Entities.Query(dr => cates.Add(dr));
            //}
            var data = new System.Data.DataTable();
            data.Columns.Add("id");
            data.Columns.Add("src");
            data.Columns.Add("title");
            data.Columns.Add("desc");
            data.Columns.Add("time");
            data.Columns.Add("reply");
            data.Columns.Add("look");
            data.Columns.Add("last");
            data.Columns.Add("status");
            data.Columns.Add("poster");
            //data.Columns.Add("category");
            //var count = 108;
            foreach (var sub in subs)
            {
                if (sub.Visible == 0 && request.IsCashier == false)
                {
                    continue;
                }

                var src = webr.ResolveUrl(sub.Id.Value, 1, "0") + "!cms3" + "?_ts=" + UMC.Data.Utility.TimeSpan(sub.ReleaseDate.Value);


                //var cate = cates.Find(g => g.Id == sub.category_id);

                var s = "发布中";
                switch (sub.Status ?? 0)
                {
                    case -1:
                        s = "不发布";
                        break;
                    case 0:
                        s = "发布中";
                        break;
                    case 1:
                        s = "已发布";
                        break;
                }
                data.Rows.Add(sub.Id, src, sub.Title, sub.Description, UMC.Data.Utility.GetDate(sub.ReleaseDate)
                    , sub.Reply ?? 0, sub.Look ?? 0, UMC.Data.Utility.GetDate(sub.LastDate), s, sub.Poster
                   );
            }
            var hashc = new System.Collections.Hashtable();
            hashc["data"] = data;
            var total = subEntity.Count(); ;
            hashc["total"] = total;// subEntity.Count();
            if (total == 0)
            {
                hashc["msg"] = "现在还未有发布的新闻";
            }
            response.Redirect(hashc);
        }

    }
}