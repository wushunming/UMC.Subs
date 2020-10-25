using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Web.UI;
using UMC.Web;
using UMC.Data.Entities;
using System.Collections;

namespace UMC.Subs.Activities
{
    class SubjectCodeActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var sId = Utility.Guid(this.AsyncDialog("Id", g =>
            {
                return this.DialogValue("none");

            })).Value;
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {

                this.Prompt("请登录", false);
                response.Redirect("Account", "Login");

            }
            var Code = this.AsyncDialog("Code", "auto");

            var entity = Utility.CMS.ObjectEntity<Subject>()
                    .Where.And().Equal(new Subject
                    {
                        Id = sId
                    }).Entities;
            var sub = entity.Single() ?? new Subject { Id = sId };
            if (String.Equals(sub.Code, Code, StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }


            var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
            {
                Id = sub.project_id.Value
            }).Entities.Single();
            if (project != null && project.user_id == user.Id)
            {

            }
            else
            {
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
            var projectItem = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new ProjectItem
            {
                Id = sub.project_item_id.Value
            }).Entities.Single();
            if (Utility.CMS.ObjectEntity<Subject>().Where.And().Equal(new Subject
            {
                Code = Code,
                project_item_id = sub.project_item_id
            }).Entities.Count() > 0)
            {
                this.Prompt("此简码已存在");
            }

            entity.Update(new Subject { Code = Code });

            this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Change").Put("id", sub.Id).Put("Sub", sub.Id).Put("text", sub.Title).Put("code", Code)
                .Put("path", String.Format("{0}/{1}/{2}", project.Code, projectItem.Code, Code)), true);



        }

    }
}