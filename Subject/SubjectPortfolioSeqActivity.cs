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
    class SubjectSequenceActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var ids = this.AsyncDialog("Id", request.Model, "Select").Split(',');

            var PortfolioId = Utility.Guid(this.AsyncDialog("Portfolio", "Portfolio"));
            if (PortfolioId.HasValue == false)
            {
                return;

            }

            var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();
            var projectId = Guid.Empty;
            if (ids.Length > 0)
            {
                var sub = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>()
                     .Where.And().Equal(new Portfolio { Id = PortfolioId.Value }).Entities.Single();
                var user = UMC.Security.Identity.Current;

                if (sub.project_id.HasValue)
                {
                    projectId = sub.project_id.Value;
                    var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
                    {
                        Id = projectId
                    }).Entities.Single();
                    if (project != null && project.user_id == user.Id)
                    {

                    }
                    else
                    {
                        var member = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new ProjectMember
                        {
                            project_id = projectId,
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
            }

            for (var i = 0; i < ids.Length; i++)
            {
                scheduleEntity
                    .Where.Reset().And().Equal(new Subject { Id = Utility.Guid(ids[i]).Value, project_id = projectId })
                    .Entities.Update(new Subject { Seq = i, portfolio_id = PortfolioId });
            }

            response.Redirect(new WebMeta().Put("msg", ids.Length));


        }

    }
}