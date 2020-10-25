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
    class SubjectDataActivity : WebActivity
    {

        List<WebMeta> Portfolio(Guid project_id, Guid subid)
        {
            var Portfolios = new List<Portfolio>();
            var ids = new List<Guid>();
            var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>();

            scheduleEntity.Where.And().Equal(new Portfolio
            {
                project_item_id = project_id
            });
            scheduleEntity.Order.Asc(new Portfolio { Sequence = 0 });
            scheduleEntity.Query(dr =>
            {
                Portfolios.Add(dr);
                ids.Add(dr.Id.Value);
            });
            var subs = new List<Subject>();
            if (ids.Count > 0)
            {
                var objectEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                    .Where.And().In(new Subject { portfolio_id = ids[0] }, ids.ToArray())
                    .And().Unequal(new Subject { Id = subid })
                    .Entities.Order.Asc(new Subject { Seq = 0 }).Entities;

                objectEntity.Where.And().Equal(new Subject { Status = 1 });



                objectEntity.Query(new Subject { Id = Guid.Empty, Title = String.Empty, portfolio_id = Guid.Empty, Code = String.Empty }, dr => subs.Add(dr));
            }
            var data = new List<WebMeta>();
            foreach (var p in Portfolios)
            {
                var meta = new WebMeta();
                meta.Put("id", p.Id).Put("text", p.Caption);
                var csubs = subs.FindAll(s => s.portfolio_id == p.Id);
                var dcsub = new List<WebMeta>();
                foreach (var cs in csubs)
                {
                    var mta = new WebMeta().Put("id", cs.Id).Put("text", cs.Title);

                    dcsub.Add(mta); ;
                }
                meta.Put("subs", dcsub);
                if (dcsub.Count == 0)
                {
                    continue;
                }
                data.Add(meta);
            }
            return data;

        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var strId = this.AsyncDialog("Id", g =>
            {
                return new Web.UITextDialog() { Title = "主题" };
            });
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            var sid = UMC.Data.Utility.Guid(strId);
            if (sid.HasValue)
            {
                subEntity.Where.And().Equal(new UMC.Data.Entities.Subject { Id = sid });
            }
            else
            {
                var codes = new List<String>(strId.Split('/'));
                switch (codes.Count)
                {
                    case 1:
                        codes.Insert(0, "Help");
                        codes.Insert(0, "UMC");
                        break;
                    case 2:
                        codes.Insert(0, "UMC");
                        break;
                    case 3:
                        break;
                    default:
                        response.Redirect(new WebMeta("Id", strId));
                        break;
                }

                var team = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Data.Entities.Project { Code = codes[0] }).Entities.Single();
                if (team == null)
                {
                    response.Redirect(new WebMeta("Id", strId));
                }
                subEntity.Where.And().Equal(new Subject { project_id = team.Id });
                var projectItem = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new Data.Entities.ProjectItem
                {
                    Code = codes[1],
                    project_id = team.Id
                }).Entities.Single();
                if (projectItem == null)
                {
                    response.Redirect(new WebMeta("Id", strId));
                }
                subEntity.Where.And().Equal(new Subject { project_item_id = projectItem.Id, Code = codes[2] });
            }

            var webr = UMC.Data.WebResource.Instance();
            var user = UMC.Security.Identity.Current;
            //var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
            //  subEntity.Where.And().Equal(new UMC.Data.Entities.Subject { Id = sid });

            var sub = subEntity.Single() ?? new UMC.Data.Entities.Subject { Id = sid };
            var hash = UMC.Data.Reflection.PropertyToDictionary(sub);

            hash["ReleaseDate"] = UMC.Data.Utility.GetDate(sub.ReleaseDate);
            hash["Content"] = UMC.Data.JSON.Expression(String.IsNullOrEmpty(sub.DataJSON) ? "[]" : sub.DataJSON);
            hash.Remove("DataJSON");
            if ((sub.PublishTime ?? 0) + 3600 < Utility.TimeSpan())// DateTime.Now)
            {
                hash["releaseId"] = sub.Id.ToString();
            }
            if (sub.project_item_id.HasValue)
            {
                hash["Portfolio"] = Portfolio(sub.project_item_id.Value, sub.Id.Value);
            }
            if (sub.Look.HasValue)
            {
                subEntity.Update("{0}+{1}", new Subject { Look = 1 });
            }
            else
            {
                subEntity.Update(new Subject { Look = 1 });

            }

            response.Redirect(hash);
        }

    }
}