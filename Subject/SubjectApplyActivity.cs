using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using UMC.Web.UI;
using UMC.Web;
using System.IO;

namespace UMC.Subs.Activities
{



    /// <summary>
    /// 版务处理
    /// </summary>
    class SubjectApplyActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {
                if (request.IsApp)
                {
                    response.Redirect("Account", "Login");
                }
                else
                {
                    this.Prompt("请登录");
                }
            }
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();


            var sId = this.AsyncDialog("Id", ag =>
             {
                 var form = request.SendValues ?? new UMC.Web.WebMeta();
                 if (form.ContainsKey("limit") == false)
                 {

                     this.Context.Send(new UISectionBuilder(request.Model, request.Command)

                         .RefreshEvent("Subject.Apply")
                             .Builder(), true);
                 }

                 subEntity.Where.And().Equal(new Subject { Status = 0, Visible = 1 });

                 var webr = UMC.Data.WebResource.Instance();

                 int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
                 int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

                 string sort = form[("sort")] as string;
                 string dir = form[("dir")] as string;

                 var category = form["Category"] as string;
                 var model = request.Model;

                 //subEntity.Where.And().In("category_id", Utility.CMS.ObjectEntity/*<Category>().Where.And().Equal(new Category { user_id = user.Id }).Entities.Script(new Category { Id = Guid.Empty }));*/
                 var pics = new List<UMC.Data.Entities.Picture>();

                 Guid? CategoryId = UMC.Data.Utility.Guid(category);

                 var Keyword = (form["Keyword"] as string ?? String.Empty);
                 if (CategoryId.HasValue)
                 {
                     subEntity.Where.And().Equal(new Data.Entities.Subject { category_id = CategoryId });

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
                 var subs = new List<Subject>();
                 var cateids = new List<Guid>();
                 var ids = new List<Guid>();
                 subEntity.Query(start, limit, dr =>
                {
                    subs.Add(dr);
                    cateids.Add(dr.category_id ?? Guid.Empty);
                    ids.Add(dr.Id.Value);
                });
                 //var cates = new List<Category>();
                 //if (ids.Count > 0)
                 //{
                 //    Utility.CMS.ObjectEntity<Category>().Where.And().In(new Category { Id = Guid.Empty }, cateids.ToArray())
                 //       .Entities.Query(dr => cates.Add(dr));
                 //    Data.Database.Instance().ObjectEntity<Data.Entities.Picture>().Where.And().In(new Data.Entities.Picture { group_id = ids[0] }, ids.ToArray())
                 //       .Entities.Order.Asc(new Data.Entities.Picture { Seq = 0 }).Entities.Query(g => pics.Add(g));
                 //}

                 var ui = UISection.Create();
                 if (start == 0)
                 {
                     ui.Title = new UITitle("我的版务");
                 }

                 foreach (var sub in subs)
                 {
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
                     var hash = new System.Collections.Hashtable();
                     var cate = cates.Find(g => g.Id == sub.category_id);
                     if (cate != null)
                     {
                         hash["id"] = cate.Id;
                         hash["text"] = cate.Caption;
                     }
                     else
                     {
                         hash["text"] = "未分类";
                     }

                     var click = Web.UIClick.Pager("Subject", "UIData", new UMC.Web.WebMeta().Put("Id", sub.Id));
                     var data = new UMC.Web.WebMeta().Put("title", sub.Title).Put("reply", sub.Reply ?? 0).Put("look", sub.Look ?? 0)
                        .Put("left", cate == null ? "未分类" : cate.Caption);
                     UICell cell;
                     switch (imgs.Count)
                     {
                         case 0:
                             cell = UICMS.CreateMax(click, data);
                             break;
                         default:
                             cell = (sub.IsPicture ?? false) ? UICMS.CreateMax(click, data, imgs[0]) : (ims.Count > 2 ? UICMS.CreateThree(click, data, imgs.ToArray()) : UICMS.CreateOne(click, data, imgs[0]));


                             break;
                     }

                     cell.Style.Name("licon", new UIStyle().Size(12).Font("wdk")).Name("ricon", new UIStyle().Size(12).Font("wdk"));
                     cell.Format.Put("right", "\uF06E{1:licon} {look}   \uF0E6{1:ricon} {reply}");
                     ui.Add(cell);
                 }
                 ui.Total = subEntity.Count();
                 if (ui.Total == 0)
                 {

                     ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "还未有需要审核的版务").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                       new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));

                 }
                 response.Redirect(ui);
                 return this.DialogValue("none");

             });

            var Type = this.AsyncDialog("Type", g =>
            {
                var shett = new Web.UISheetDialog() { Title = "图文版务" };
                shett.Options.Add(new UIClick(new UMC.Web.WebMeta(request.Arguments.GetDictionary()).Put("Type", "OK")) { Model = request.Model, Command = request.Command, Text = "审核通过" });
                shett.Options.Add(new UIClick(new UMC.Web.WebMeta(request.Arguments.GetDictionary()).Put("Type", "Reject")) { Model = request.Model, Command = request.Command, Text = "驳回重写" });
                return shett;
            });
            var usub = new Subject();
            usub.Status = Type == "OK" ? 1 : -2;

            var strs = new string[] { "内容低俗", "过度营销", "不符合社会价值观", "法律禁止" };
            var appdesc = Utility.IntParse(this.AsyncDialog("Desc", g =>
           {
               if (Type == "OK")
               {
                   return this.DialogValue("-1");
               }
               var shett = new Web.UISelectDialog() { Title = "驳回原因" };
               shett.Options.Put(strs[0], "0").Put(strs[1], "1").Put(strs[2], "2");
               return shett;
           }), -1);
            if (appdesc > -1)
            {
                usub.AppDesc = strs[appdesc];
            }
            if (usub.Status > 0)
            {
                usub.LastDate = DateTime.Now;
                usub.ReleaseDate = DateTime.Now;
            }
            else
            {
                usub.LastDate = DateTime.Now;
            }
            subEntity.Where.And().Equal(new Subject { Id = Utility.Guid(sId, true) })
            .Entities.Update(usub);
            this.Context.Send("Subject.Apply", true);

        }

    }
}