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



    class SubjectFollowActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var appKey = UMC.Security.Principal.Current.AppKey ?? Guid.Empty;
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
            var user = UMC.Security.Identity.Current;
            subEntity.Where.And().Greater(new Subject { Status = 0, Visible = -1 }).And().Equal(new Subject { IsDraught = false });



            subEntity.Where.And().In("project_id", Utility.CMS.ObjectEntity<ProjectMember>()
                .Where.And().Equal(new ProjectMember { user_id = user.Id })
                .Entities.Script(new ProjectMember { project_id = Guid.Empty }));


            var webr = UMC.Data.WebResource.Instance();
            var form = request.SendValues ?? new UMC.Web.WebMeta();
            subEntity.Where.And().Equal(new Data.Entities.Subject { Status = 1 });

            int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
            int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);

            string sort = form[("sort")] as string;
            string dir = form[("dir")] as string;


            var pics = new List<UMC.Data.Entities.Picture>();


            var Keyword = (form["Keyword"] as string ?? String.Empty);

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


            SubjectUIActivity.Search( request.Model, ui, subEntity, start, limit);
            if (ui.Total == 0)
            {
                ui.Add("Desc", new UMC.Web.WebMeta().Put("desc", "未有您的关注的项目").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),

                    new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));



            }
            response.Redirect(ui);
        }

    }
}