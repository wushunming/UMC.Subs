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
    class SubjectExportActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var user = UMC.Security.Identity.Current;


            var code = this.AsyncDialog("code", g =>
            {
                response.Redirect(new WebMeta("Msg", "请输入Key"));
                return this.DialogValue("none");
            });



            var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>();
            scheduleEntity.Order.Asc(new Project { CreationTime = DateTime.MinValue });
            scheduleEntity.Where.And().Equal(new Project { Code = code });

            var team = scheduleEntity.Single();
            if (team == null)
            {
                response.Redirect(new WebMeta("Msg", "无此项目"));
            }

            var items = new List<ProjectItem>();


            var ls = new List<Guid>();
            var projectEntity = Utility.CMS.ObjectEntity<ProjectItem>();
            projectEntity.Where.And().In(new ProjectItem { project_id = team.Id });

            projectEntity.Order.Asc(new ProjectItem { Sequence = 0 });

            projectEntity.Query(dr =>
            {
                items.Add(dr);
                ls.Add(dr.Id.Value);
            });
            var pids = new List<Guid>();

            var Portfolios = new List<Portfolio>();
            if (ls.Count > 0)
            {
                Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>()
                .Where.And().In(new Portfolio { project_item_id = ls[0] }, ls.ToArray())
                .Entities.Order.Asc(new Portfolio { Sequence = 0 }).Entities.Query(dr =>
                {
                    Portfolios.Add(dr);
                    pids.Add(dr.Id.Value);
                });
            }
            var subs = new List<Subject>();
            if (pids.Count > 0)
            {
                var objectEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                       .Where.And().In(new Subject { portfolio_id = pids[0] }, pids.ToArray())
                       .And().Equal(new Subject { Visible = 1 })
                       .Entities.Order.Asc(new Subject { Seq = 0 });
                objectEntity.Entities.Where.And().Equal(new Subject { Status = 1 });

                objectEntity.Entities.Query(new Subject
                {
                    Id = Guid.Empty,
                    Title = String.Empty,
                    portfolio_id = Guid.Empty,
                    Code = String.Empty,
                    DataJSON = String.Empty
                }, dr => subs.Add(dr));
            }
            var its = new List<WebMeta>();

            foreach (var v in items)
            {
                var data = new List<WebMeta>();
                var ps = Portfolios.FindAll(d => d.project_item_id == v.Id);

                foreach (var p in ps)
                {
                    var data2 = new List<WebMeta>();

                    var sbs = subs.FindAll(d => d.portfolio_id == p.Id);
                    foreach (var sb in sbs)
                    {
                        data2.Add(new WebMeta().Put("code", sb.Code, "title", sb.Title, "content", sb.DataJSON, "key", Utility.Guid(sb.Id.Value)));
                    }
                    if (data2.Count > 0)
                        data.Add(new WebMeta().Put("data", data2).Put("title", p.Caption));
                }
                if (data.Count > 0)
                    its.Add(new WebMeta().Put("data", data).Put("title", v.Caption).Put("code", v.Code));
            }

            response.Redirect(new WebMeta().Put("data", its).Put("title", team.Caption).Put("code", team.Code).Put("desc", team.Description));


        }

    }
}