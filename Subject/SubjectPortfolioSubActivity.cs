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
    class SubjectPortfolioSubActivity : WebActivity
    {
        public static List<WebMeta> Portfolio(Project team, ProjectItem project, int status)
        {
            var Portfolios = new List<Portfolio>();
            var ids = new List<Guid>();
            var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>();

            scheduleEntity.Where.And().Equal(new Portfolio
            {
                project_item_id = project.Id
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
                    .And().Equal(new Subject { Visible = 1 })
                    .Entities.Order.Asc(new Subject { Seq = 0 });
                if (status == -1)
                {
                    objectEntity.Entities.Where.And().Equal(new Subject { Status = 1 });
                }

                objectEntity.Entities.Query(new Subject { Id = Guid.Empty, Title = String.Empty, portfolio_id = Guid.Empty, Code = String.Empty }, dr => subs.Add(dr));
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
                    mta.Put("path", String.Format("{0}/{1}/{2}", team.Code, project.Code, String.IsNullOrEmpty(cs.Code) ? "none" : cs.Code));
                    dcsub.Add(mta); ;
                }
                meta.Put("subs", dcsub);
                if (status < 1 && dcsub.Count == 0)
                {
                    continue;
                }
                data.Add(meta);
            }
            return data;

        }
        void Portfolio(Guid projectid, WebResponse response)
        {
            var ids = new List<Guid>();
            var user = UMC.Security.Identity.Current;


            var projectEntity = Utility.CMS.ObjectEntity<ProjectItem>();
            var project = projectEntity.Where.And().In(new ProjectItem { Id = projectid }).Entities.Single();

            var team = Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project { Id = project.project_id }).Entities.Single();
            var editer = team.user_id == user.Id;
            int status = 1;
            if (editer == false)
            {
                status = -1;
                var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                {
                    project_id = team.Id,
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
                            status = -1;
                            break;
                        case WebAuthType.User:
                            status = 1;
                            break;
                    }
                }


            }

            var menu = new WebMeta();
            menu.Put("nav", String.Format("{0}/{1}", team.Code, project.Code));
            menu.Put("subs", Portfolio(team, project, status));
            response.Redirect(menu);//
            //return Portfolio(team, project, status);

        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var Key = this.AsyncDialog("Key", "none");
            var pkd = Utility.Guid(Key);
            if (pkd.HasValue)
            {
                Portfolio(pkd.Value, response);
            }
            else if (String.Equals("none", Key) == false)
            {
                var keys = Key.Split('/');
                if (keys.Length > 1)
                {
                    var user = UMC.Security.Identity.Current;
                    var team = Utility.CMS.ObjectEntity<Project>().Where.And().In(new Project { Code = keys[0] }).Entities.Single();

                    var projectEntity = Utility.CMS.ObjectEntity<ProjectItem>();
                    var project = projectEntity.Where.And().Equal(new ProjectItem { project_id = team.Id, Code = keys[1] }).Entities.Single();
                    var editer = team.user_id == user.Id;
                    int status = 1;
                    if (editer == false)
                    {
                        status = -1;
                        var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                        {
                            project_id = team.Id,
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
                                    status = -1;
                                    break;
                                case WebAuthType.User:
                                    status = 1;
                                    break;
                            }
                        }


                    }
                    var menu = new WebMeta();
                    if (keys.Length == 3)
                    {
                        var sub = Utility.CMS.ObjectEntity<Subject>().Where.And().Equal(new Subject
                        {
                            project_id = team.Id,
                            project_item_id = project.Id,
                            Code = keys[2]
                        }).Entities.Single();
                        if (sub != null)
                            menu.Put("spa", new WebMeta().Put("id", sub.Id).Put("path", String.Format("{0}/{1}/{2}", team.Code, project.Code, sub.Code)));

                    }
                    menu.Put("nav", String.Format("{0}/{1}", team.Code, project.Code));
                    menu.Put("subs", Portfolio(team, project, status));
                    response.Redirect(menu);//

                }
            }




        }

    }
}