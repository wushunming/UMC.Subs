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



    class SubjectSelfActivity : WebActivity
    {

        void Me(WebRequest request, WebResponse response)
        {
            var user = UMC.Security.Identity.Current;


            var form = request.SendValues ?? new UMC.Web.WebMeta();




            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            subEntity.Where.And().Equal(new Subject { user_id = user.Id });
            subEntity.Where.And().GreaterEqual(new Subject { Visible = 0 });



            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

            string sort = form[("sort")] as string;
            string dir = form[("dir")] as string;

            var category = form["Project"] as string;
            var model = request.Model;


            Guid? CategoryId = UMC.Data.Utility.Guid(category);

            var Keyword = (form["Keyword"] as string ?? String.Empty);
            if (CategoryId.HasValue)
            {
                subEntity.Where.And().Equal(new Data.Entities.Subject { project_id = CategoryId });

            }

            if (String.IsNullOrEmpty(Keyword) == false)
            {
                subEntity.Where.And().Like(new Subject { Title = Keyword });
            }
            subEntity.Where.And().Greater(new Subject { Visible = 0 });

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
                subEntity.Order.Desc(new Subject { LastDate = DateTime.Now });
            }

            var subs = new List<Subject>();
            var cateids = new List<Guid>();
            var itemIds = new List<Guid>();

            var search = UMC.Data.Reflection.CreateInstance<Subject>();
            search.DataJSON = null;
            search.Content = null;
            search.ConfigXml = null;
            search.Description = null;
            subEntity.Query(search, start, limit, dr =>
            {
                subs.Add(dr);
                if (dr.project_id.HasValue)
                    cateids.Add(dr.project_id ?? Guid.Empty);
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




            var data = new System.Data.DataTable();
            data.Columns.Add("id");
            data.Columns.Add("title");
            data.Columns.Add("state");
            data.Columns.Add("last");
            data.Columns.Add("owner");
            data.Columns.Add("path");

            foreach (var sub in subs)
            {
                var state = "";
                switch (sub.Status)
                {
                    case 1:
                        break;
                    case 0:
                        state = "未更新";
                        break;
                    case -1:
                        state = "未发布";
                        break;
                }
                var owner = "草稿";
                var cate = cates.Find(g => g.Id == sub.project_id);
                var pitem = pitems.Find(g => g.Id == sub.project_item_id);
                if (cate != null && pitem != null)
                {
                    owner = String.Format("{0}/{1}", cate.Caption, pitem.Caption);
                    data.Rows.Add(sub.Id, sub.Title, state, Utility.GetDate(sub.LastDate), owner, String.Format("{0}/{1}/{2}", cate.Code, pitem.Code, sub.Code));
                }
                else
                {
                    data.Rows.Add(Utility.Guid(sub.Id.Value), sub.Title, state, Utility.GetDate(sub.LastDate), owner);
                }

            };
            var hashc = new System.Collections.Hashtable();
            hashc["data"] = data;
            var total = subEntity.Count(); ;
            hashc["total"] = total;// subEntity.Count();
            if (total == 0)
            {
                if (String.IsNullOrEmpty(Keyword))
                {
                    hashc["msg"] = "未有创造的知识";
                }
                else
                {
                    hashc["msg"] = String.Format("未有关联“{0}”", Keyword);
                }


            }
            response.Redirect(hashc);


        }


        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var user = UMC.Security.Identity.Current;
            switch (this.AsyncDialog("Type", "App"))
            {
                case "PC":
                    this.Me(request, response);
                    return;
            }

            var form = request.SendValues ?? new UMC.Web.WebMeta();

            if (form.ContainsKey("limit") == false)
            {

                this.Context.Send(new UISectionBuilder(request.Model, request.Command)

                    .RefreshEvent("Subject.Save", "image", "Subject.Content")
                        .Builder(), true);
            }


            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            subEntity.Where.And().Equal(new Subject { user_id = user.Id });
            subEntity.Where.And().GreaterEqual(new Subject { Visible = 0 });

            var webr = UMC.Data.WebResource.Instance();

            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

            string sort = form[("sort")] as string;
            string dir = form[("dir")] as string;

            var category = form["Project"] as string;
            var model = request.Model;

            var pics = new List<UMC.Data.Entities.Picture>();

            Guid? CategoryId = UMC.Data.Utility.Guid(category);

            var Keyword = (form["Keyword"] as string ?? String.Empty);
            if (CategoryId.HasValue)
            {
                subEntity.Where.And().Equal(new Data.Entities.Subject { project_id = CategoryId });

            }

            if (String.IsNullOrEmpty(Keyword) == false)
            {
                subEntity.Where.And().Like(new Subject { Title = Keyword });
            }
            subEntity.Where.And().Greater(new Subject { Visible = 0 });

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
                subEntity.Order.Desc(new Subject { LastDate = DateTime.Now });
            }
            //if (request.IsApp)
            //{

            var ui = UISection.Create();
            var u2 = ui;
            if (start == 0)
            {

                ui.Add(new UMC.Web.UI.UIIcon().Add(new UIEventText("新建Markdown").Icon('\uf198', 0x40c9c6).Click(new Web.UIClick("Markdown").Send("Subject", "Content")).Badge("MD格式")
                    , new UIEventText("新建富文本").Icon('\uf13b', 0x1890ff).Click(new Web.UIClick("UIClick").Send("Subject", "Content")).Badge("富文本")
                    , new UIEventText("抓取图文").Icon('\uf0c5', 0x36a3f7).Click(new Web.UIClick("UIClick").Send("Subject", "Content"))));
                u2 = ui.NewSection();
                //ui.NewSection()
                //   .AddCell('\uf198', "新建文档", "采用Markdown格式编写", new Web.UIClick("Markdown").Send("Subject", "Content"))
                //    .AddCell('\uf13b', "新建文档", "采用富文本格式编写", new Web.UIClick("News").Send("Subject", "Content"))
                //    .AddCell('\uf0c5', "抓取图文", "从粘贴板版网址中抓取图文", new Web.UIClick() { Key = "CaseCMS" });//.Header.Put("text", "宣传方案");
            }
            if (user.IsAuthenticated == false)
            {
                UIDesc desc = new UIDesc(new UMC.Web.WebMeta().Put("desc", "你尚未登录").Put("icon", "\uF016")).Desc("{icon}\n{desc}").Click(
                    new UIClick().Send("Account", "Login"));

                desc.Style.Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60));
                u2.Add(desc);
                response.Redirect(ui);
            }
            SubjectUIActivity.Search(u2, subEntity, request.Model, "EditUI", start, limit, false);
            if (u2.Total == 0)
            {
                u2.Add("Desc", new UMC.Web.WebMeta().Put("desc", "未有创造的知识").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),
                    new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
            }
            response.Redirect(ui);


        }

    }
}