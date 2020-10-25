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



    class SubjectContentActivity : WebActivity
    {



        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var Url = this.AsyncDialog("Id", g =>
            {
                if (request.IsApp == false)
                {
                    return this.DialogValue("News");
                }
                var optls = new Web.UISheetDialog();
                optls.Title = "新建图文"; ;
                optls.Options.Add(new UIClick("News") { Command = request.Command, Model = request.Model, Text = "新建富文本图文" });
                optls.Options.Add(new UIClick("Markdown") { Command = request.Command, Model = request.Model, Text = "新建Markdown文档" });
                optls.Options.Add(new UIClick() { Key = "CaseCMS", Text = "抓取粘贴板网址图文" });

                return optls;
            });
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {

                this.Prompt("请先登录", false);
                response.Redirect("Account", "Login");
            }

            var sId = UMC.Data.Utility.Guid(Url);
            if (sId.HasValue == false)
            {
                var sType = "text/html";
                switch (Url)
                {
                    case "Project":
                        response.Redirect(request.Model, "ProjectUI", "News", true);
                        break;
                    case "Markdown":
                        sType = "markdown";
                        if (request.IsApp)
                            this.Context.Send("Markdown", new WebMeta().Put("Id", "News"), true);
                        break;
                }
                if (request.IsApp == false)
                {
                    var sub2 = new Subject()
                    {
                        Visible = 1,
                        CreationTime = DateTime.Now,
                        Title = DateTime.Now.ToShortDateString(),
                        IsPicture = false,
                        IsDraught = true,
                        Id = Guid.NewGuid(),
                        ContentType = sType,
                        LastDate = DateTime.Now,
                        Poster = user.Alias,
                        Seq = Utility.TimeSpan(),
                        last_user_id = user.Id,
                        user_id = user.Id,
                        Status = -1
                    };
                    SubjectSaveActivity.Dashboard(user, sub2);
                    Utility.CMS.ObjectEntity<Subject>().Insert(sub2);
                    this.Context.Send("Markdown", new WebMeta().Put("Id", Utility.Guid(sub2.Id.Value)), true);

                }
            }


            var sub = sId.HasValue ? (Utility.CMS.ObjectEntity<Subject>()
                        .Where.And().Equal(new Subject
                        {
                            Id = sId
                        }).Entities.Single() ?? new Subject { Id = sId }) : new Subject { Id = sId }; ;
            if (String.IsNullOrEmpty(request.SendValue) == false)
            {
                if (sub.project_id.HasValue)
                {
                    var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
                    {
                        Id = sub.project_id
                    }).Entities.Single();
                    if (project != null)
                    {
                        if (project.user_id == user.Id)
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
                    }
                }
                if (String.Equals("markdown", sub.ContentType, StringComparison.CurrentCultureIgnoreCase))
                {
                    this.Context.Send("Markdown", new WebMeta().Put("Id", sId), true);

                }
                else
                {

                    if (request.IsApp)
                    {
                        this.Context.Send(new UISectionBuilder(request.Model, request.Command, new UMC.Web.WebMeta().Put("Id", sId.HasValue ? sId : Guid.NewGuid()))
                            .CloseEvent("Subject.Save")
                                .Builder().Put("IsEditer", true), true);
                    }
                    else
                    {

                        this.Context.Send("Markdown", new WebMeta().Put("Id", Utility.Guid(sId.Value)), true);
                    }


                }

            }

            var Next = this.AsyncDialog("Next", "none");
            var ui = UISection.Create();
            var title = new UITitle("图文编辑器");
            ui.Title = title;
            title.Right(new UIEventText().Icon('\uf0c7').Click(UIClick.Click(new UIClick("Id", sub.Id.ToString(), "Next", Next)
            {
                Command = "Save",
                Model = request.Model
            })));


            var celss = UMC.Data.JSON.Deserialize<WebMeta[]>((String.IsNullOrEmpty(sub.DataJSON) ? "[]" : sub.DataJSON)) ?? new UMC.Web.WebMeta[] { };
            foreach (var pom in celss)
            {
                switch (pom["_CellName"])
                {
                    case "CMSImage":
                        pom.Put("style", new UIStyle().Padding(0, 10));
                        break;
                }
            }

            if (celss.Length == 0)
            {
                var ed = ui.NewSection();
                ed.DisableSeparatorLine();
                ed.IsEditer = true;
                ed.AddCells(new UMC.Web.WebMeta().Put("_CellName", "CMSText").Put("value", new UMC.Web.WebMeta().Put("text", "新建文档")));
            }
            else
            {
                var ed = ui.NewSection();
                ed.IsEditer = true;
                ed.AddCells(celss);
                ed.DisableSeparatorLine();

            }
            var style = new UIStyle();
            var footer = new UIHeader();
            ui.UIFooter = footer;//
            footer.Desc(new UMC.Web.WebMeta("icon", "\uf004", "desc", "天天录，录入您知识财富"), "{icon}\n{desc}", style);

            style.Height(350).Color(0xf0f0f0).AlignCenter().BgColor(0xf8f8f8).Name("border", "none");//.BorderColor
            style.Name("icon").Font("wdk").Size(40);


            response.Redirect(ui);
        }

    }
}