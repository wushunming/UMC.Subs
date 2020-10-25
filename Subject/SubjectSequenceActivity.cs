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
    class SubjectPortfolioSeqActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var ids = this.AsyncDialog("Id", request.Model, "Portfolio").Split(',');

            var scheduleEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>();
            var sub = scheduleEntity
                 .Where.Reset().And().Equal(new Portfolio { Id = Utility.Guid(ids[0]).Value }).Entities.Single();
            var user = UMC.Security.Identity.Current;
            var projectId = Guid.Empty;

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

            for (var i = 0; i < ids.Length; i++)
            {
                scheduleEntity
                    .Where.Reset().And().Equal(new Portfolio { Id = Utility.Guid(ids[i]).Value, project_id = projectId })
                    .Entities.Update(new Portfolio { Sequence = i });
            }

            response.Redirect(new WebMeta().Put("msg", ids.Length));


        }

    }
}