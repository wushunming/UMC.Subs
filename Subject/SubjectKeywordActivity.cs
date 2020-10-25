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
    class SubjectKeywordActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            Guid Project = UMC.Data.Utility.Guid(this.AsyncDialog("Project", "auto")).Value;

            var form = request.SendValues ?? new UMC.Web.WebMeta();
            var Keyword = (form["Keyword"] as string ?? String.Empty);

            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
            subEntity.Where.And().Equal(new Subject
            {
                project_id = Project,
                Status = 1
            });
            if (String.IsNullOrEmpty(Keyword) == false)
            {
                subEntity.Where.And().Contains().Or().Like(new Subject { Title = Keyword, Description = Keyword });
            }
            var subs = new List<Subject>();
            var ids = new List<Guid>();
            var items = new List<ProjectItem>();
            subEntity.Order.Desc(new Subject { Look = 0, Reply = 0 }).Desc(new Subject { ReleaseDate = DateTime.MinValue });


            var pro = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>().Where.And().Equal(new Data.Entities.Project { Id = Project }).Entities.Single();

            subEntity.Query(new Subject { Id = Guid.Empty, Title = String.Empty, project_item_id = Guid.Empty, Code = String.Empty }, 0, 8, dr =>
              {
                  if (ids.Contains(dr.project_item_id.Value) == false)
                      ids.Add(dr.project_item_id.Value);
                  subs.Add(dr);
              });

            if (ids.Count > 0)
            {
                Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>().Where.And().In(new ProjectItem { Id = ids[0] }, ids.ToArray())
                    .Entities.Query(dr => items.Add(dr));
            }
            var data = new List<WebMeta>();
            foreach (var c in subs)
            {
                var p = items.Find(i => i.Id == c.project_item_id);
                data.Add(new WebMeta().Put("text", c.Title).Put("path", String.Format("{0}/{1}/{2}", pro.Code, p.Code, c.Code)).Put("id", c.Id));
            }
            response.Redirect(data);

        }

    }
}