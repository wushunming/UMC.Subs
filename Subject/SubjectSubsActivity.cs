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
    class SubjectSubsActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var pkd = Utility.Guid(this.AsyncDialog("Id", g =>
             {
                 return this.DialogValue("none");

             }));

            var user = UMC.Security.Identity.Current;


            var projectEntity = Utility.CMS.ObjectEntity<ProjectItem>();
            var projectItem = projectEntity.Where.And().In(new ProjectItem { Id = pkd }).Entities.Single();

            var project = Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project { Id = projectItem.project_id }).Entities.Single();
            var editer = project.user_id == user.Id;
            int status = 1;
            if (editer == false)
            {
                status = -1;
                var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                {
                    project_id = project.Id,
                    user_id = user.Id
                }).Entities.Single();
                if (member != null)
                {
                    switch (member.AuthType)
                    {
                        case WebAuthType.Admin:
                            status = 1;
                            break;
                        case WebAuthType.All:
                            status = -1;
                            break;
                        case WebAuthType.Guest:
                            status = 0;
                            break;
                        case WebAuthType.User:
                            status = 1;
                            break;
                    }
                }


            }

            var Portfolios = new List<Portfolio>();
            var ids = new List<Guid>();
            var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>();

            scheduleEntity.Where.And().Equal(new Portfolio
            {
                project_item_id = pkd
            });
            scheduleEntity.Order.Asc(new Portfolio { Sequence = 0 });
            scheduleEntity.Query(dr =>
            {
                Portfolios.Add(dr);
                ids.Add(dr.Id.Value);
            });
            var userids = new List<Guid>();
            var subs = new List<Subject>();
            if (ids.Count > 0)
            {
                var objectEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                    .Where.And().In(new Subject { portfolio_id = ids[0] }, ids.ToArray())
                    .And().Equal(new Subject { Visible = 1 })
                    .Entities.Order.Asc(new Subject { Seq = 0 }).Entities;

                if (status < 1)
                {
                    objectEntity.Where.And().Equal(new Subject { Status = 1 });

                }


                objectEntity.Query(new Subject
                {
                    Status = 0,
                    Id = Guid.Empty,
                    Title = String.Empty,
                    portfolio_id = Guid.Empty,
                    Code = String.Empty,
                    last_user_id = Guid.Empty,
                    user_id = Guid.Empty
                }, dr =>
                {
                    subs.Add(dr);
                    if (dr.user_id.HasValue && userids.Exists(g => g == dr.user_id) == false)
                    {
                        userids.Add(dr.user_id.Value);
                    }
                    if (dr.last_user_id.HasValue && userids.Exists(g => g == dr.last_user_id) == false)
                    {
                        userids.Add(dr.last_user_id.Value);
                    }
                });
            }

            var webr = Data.WebResource.Instance();
            var users = new List<WebMeta>();
            if (userids.Count > 0)
            {
                Utility.CMS.ObjectEntity<User>()
                                    .Where.And().In(new User { Id = userids[0] }, userids.ToArray())
                                    .Entities.Query(dr =>
                                    {
                                        users.Add(new WebMeta().Put("src", webr.ResolveUrl(dr.Id.Value, "1", "4")).Put("text", dr.Alias).Put("id", dr.Id));
                                    });
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
                    if (String.IsNullOrEmpty(cs.Code))
                    {
                        cs.Code = "none";
                    }
                    mta.Put("code", cs.Code);
                    mta.Put("path", String.Format("{0}/{1}/{2}", project.Code, projectItem.Code, cs.Code));
                    if (cs.Status < 1)
                    {
                        mta.Put("state", "未发布");
                    }
                    //}
                    dcsub.Add(mta); ;
                }

                meta.Put("subs", dcsub);

                data.Add(meta);
            }
            var root = new WebMeta().Put("caption", projectItem.Caption).Put("path", String.Format("{0}/{1}", project.Code, projectItem.Code)).Put("data", data).Put("users", users);
            if ((projectItem.PublishTime ?? 0) + 3600 < Utility.TimeSpan())// DateTime.Now)
            {
                root.Put("releaseId", projectItem.Id.ToString());
            }
            response.Redirect(root);


        }

    }
}