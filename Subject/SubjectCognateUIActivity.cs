//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.Collections;
//using System.Reflection;
//using UMC.Data.Entities;
//using System.IO;
//using UMC.Web;
//using UMC.Web.UI;

//namespace UMC.Subs.Activities
//{



//    class SubjectCognateUIActivity : WebActivity
//    {
//        public override void ProcessActivity(WebRequest request, WebResponse response)
//        {
//            var appKey = UMC.Security.Principal.Current.AppKey ?? Guid.Empty;
//            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

//            subEntity.Where.And().Greater(new Subject { Visible = -1 });

//            var form = request.SendValues ?? new UMC.Web.WebMeta();

//            var CategoryId = UMC.Data.Utility.Guid(form["Project"] as string, true);

//            var webr = UMC.Data.WebResource.Instance();
//            subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1, IsDraught = false });

//            var ids = new List<Guid>();
//            Utility.CMS.ObjectEntity<ProjectCognate>().Where.And().Equal(new ProjectCognate
//            {
//                project_id = CategoryId.Value
//            }).Entities.Query(dr => ids.Add(dr.refer_project_id.Value));

//            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
//            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

//            string sort = form[("sort")] as string;
//            string dir = form[("dir")] as string;

//            var pics = new List<UMC.Data.Entities.Picture>();


//            var Keyword = (form["Keyword"] as string ?? String.Empty);

//            subEntity.Where.And().In(new Data.Entities.Subject { project_id = CategoryId }, ids.ToArray());//.And().in



//            if (String.IsNullOrEmpty(Keyword) == false)
//            {
//                subEntity.Where.And().Like(new Subject { Title = Keyword });
//            }
//            if (!String.IsNullOrEmpty(sort))
//            {
//                if (dir == "DESC")
//                {
//                    subEntity.Order.Desc(sort);
//                }
//                else
//                {
//                    subEntity.Order.Asc(sort);
//                }
//            }
//            else
//            {
//                subEntity.Order.Desc(new Subject { ReleaseDate = DateTime.Now });
//            }
//            var ui = UISection.Create();
//            var items = ui;
//            if (start == 0)
//            {
//                if (String.IsNullOrEmpty(Keyword) == false)
//                {
//                    ui.Title = new UITitle(String.Format("搜索“{0}”资讯", Keyword));
//                }

//            }


//            SubjectUIActivity.Search(ui, subEntity, start, limit);

//            if (ui.Total == 0)
//            {
//                ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "还未有发布的项目资讯").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),
//                    new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
//            }
//            response.Redirect(ui);
//        }

//    }
//}