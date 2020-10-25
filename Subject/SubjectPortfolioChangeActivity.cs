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
    class SubjectPortfolioChangeActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var subId = UMC.Data.Utility.Guid(this.AsyncDialog("Id", request.Model, "Select"));
            var sub = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                    .Where//.And().In(new Subject { portfolio_id = ids[0] }, ids.ToArray())
                    .And().Equal(new Subject { Id = subId }).Entities.Single(new Subject { project_id = Guid.Empty });

            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {

                this.Prompt("请登录", false);
                response.Redirect("Account", "Login");

            }
            var meta = new WebMeta();
            if (sub.project_id.HasValue)
            {

                var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
                {
                    Id = sub.project_id.Value
                }).Entities.Single();
                if (project != null && project.user_id == user.Id)
                {

                }
                else
                {
                    meta.Put("Project", sub.project_id);
                    var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                    {
                        project_id = sub.project_id,
                        user_id = user.Id
                    }).Entities.Single();
                    if (member != null)
                    {
                        switch (member.AuthType)
                        {
                            case WebAuthType.Admin:
                            case WebAuthType.User:
                                break;
                            default:
                                this.Prompt("您未有编辑此图文的权限");
                                break;
                        }

                    }
                    else
                    {
                        this.Prompt("您未有编辑此图文的权限");
                    }
                }
            }
            var sid = UMC.Data.Utility.Guid(this.AsyncDialog("Portfolio", request.Model, "Portfolio", meta));

            var Portfolios = new List<Portfolio>();
            var ids = new List<Guid>();

            var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>();

            var portfolio = scheduleEntity.Where.And().Equal(new Portfolio
            {
                Id = sid
            }).Entities.Single();

            Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                 .Where
                 .And().Equal(new Subject { Id = subId })
                 .Entities.Update(new Subject { portfolio_id = sid, project_item_id = portfolio.project_item_id, project_id = portfolio.project_id });



            this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Portfolio.Change").Put("Id", portfolio.Id).Put("Sub", subId), false);
        }

    }
}