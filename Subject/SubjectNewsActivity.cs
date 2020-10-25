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
    class SubjectNewsActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var sId = this.AsyncDialog("Id", "auto");
            var Type = this.AsyncDialog("Type", g =>
            {
                return this.DialogValue("text/html");
                //var optls = new Web.UISheetDialog();
                //optls.Title = "新建图文";
                //optls.Options.Add(new UIClick("Id", sId, g, "text/html") { Command = request.Command, Model = request.Model, Text = "富文本格式" });
                //optls.Options.Add(new UIClick("Id", sId, g, "markdown") { Command = request.Command, Model = request.Model, Text = "Markdown格式" });

                //return optls;
            });
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {

                this.Prompt("请先登录", false);
                response.Redirect("Account", "Login");
            }
            switch (Type)
            {
                case "Project":
                    response.Redirect(request.Model, "Project", "News", true);
                    break;
            }

            var sub = new Subject()
            {
                Visible = 1,
                CreationTime = DateTime.Now,
                Title = DateTime.Now.ToShortDateString(),
                IsPicture = false,
                IsDraught = true,
                Id = Guid.NewGuid(),
                ContentType = Type,
                LastDate = DateTime.Now,
                Poster = user.Alias,
                Seq = Utility.TimeSpan(),
                last_user_id = user.Id,
                user_id = user.Id,
                Status = -1
            };
            var portfolioId = UMC.Data.Utility.Guid(sId);// UMC.Data.Utility.Guid(this.AsyncDialog("Id", "Auto"));
            if (portfolioId.HasValue)
            {
                var Portfolio = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>()
                    .Where.And().Equal(new Portfolio { Id = portfolioId }).Entities.Single();
                var projectItem = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                     .Where.And().Equal(new ProjectItem { Id = Portfolio.project_item_id }).Entities.Single();
                var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                     .Where.And().Equal(new Project { Id = Portfolio.project_id }).Entities.Single();

                if (user.Id == project.user_id)
                {

                }
                else
                {
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
                            case WebAuthType.User:
                                break;
                            default:
                                this.Prompt("您未有新增文档的权限");
                                break;
                        }

                    }
                    else
                    {
                        this.Prompt("您未有新增文档的权限");
                    }
                }

                sub.portfolio_id = portfolioId;
                sub.project_id = project.Id;
                sub.project_item_id = projectItem.Id;


                Utility.CMS.ObjectEntity<ProjectDynamic>().Insert(new ProjectDynamic
                {
                    Time = Utility.TimeSpan(sub.LastDate.Value),
                    user_id = user.Id,
                    Explain = "创建了文档",
                    project_id = sub.project_id,
                    refer_id = sub.Id,
                    Title = sub.Title,
                    Type = DynamicType.Subject
                });
                sub.Code = Utility.Parse36Encode(sub.Id.Value.GetHashCode());
                var path = String.Format("{0}/{1}/{2}", project.Code, projectItem.Code, sub.Code);
                Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>().Insert(sub);
                this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Portfolio.New").Put("Id", Portfolio.Id).Put("Sub", sub.Id).Put("Title", sub.Title)
                    .Put("Path", path), true);
            }
            else { }

        }

    }
}