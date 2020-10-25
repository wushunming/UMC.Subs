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
    class SubjectUploadActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var sId = this.AsyncDialog("Id", "auto");
            var Key = this.AsyncDialog("Key", g =>
            {
                this.Prompt("请上传文件");
                //var optls = new Web.UISheetDialog();
                //optls.Title = "新建图文";
                //optls.Options.Add(new UIClick("Id", sId, g, "text/html") { Command = request.Command, Model = request.Model, Text = "富文本格式" });
                //optls.Options.Add(new UIClick("Id", sId, g, "markdown") { Command = request.Command, Model = request.Model, Text = "Markdown格式" });

                return this.DialogValue("none");
            });
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {

                this.Prompt("请先登录", false);
                response.Redirect("Account", "Login");
            }
            if (Key.EndsWith(".md", StringComparison.CurrentCultureIgnoreCase) == false)
            {
                this.Prompt("目前只支持md格式文件");
            }
            var title = Key.Substring(Key.LastIndexOf('/') + 1);
            title = title.Substring(0, title.LastIndexOf('.'));

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
                var sourceKey = String.Format("TEMP/{0}/{1}", UMC.Data.Utility.GetRoot(request.Url), Key);
                //if (client.DoesObjectExist("wdk", sourceKey))
                //{

                //    oosr.Transfer(new Uri("http://oss.365lu.cn/" + sourceKey), Key);
                //    response.Redirect(new WebMeta().Put("src", String.Format("{0}/{1}", oosr.WebDomain(), Key)));
                //}
                Array celss;
                var content = System.Text.UTF8Encoding.UTF8.GetString(new UMC.Net.HttpClient().DownloadData("http://oss.365lu.cn/" + sourceKey));

                var cells = Data.Markdown.Transform(content);
                var dlist = new ArrayList();
                foreach (var d in cells)
                {
                    dlist.Add(new WebMeta().Put("_CellName", d.Type).Put("value", d.Data).Put("format", d.Format).Put("style", d.Style).GetDictionary());

                }
                celss = dlist.ToArray();

                var sub = new Subject()
                {
                    Visible = 1,
                    CreationTime = DateTime.Now,
                    Title = title,
                    IsPicture = false,
                    IsDraught = true,
                    Id = Guid.NewGuid(),
                    ContentType = "markdown",
                    LastDate = DateTime.Now,
                    Poster = user.Alias,
                    Seq = Utility.TimeSpan(),
                    last_user_id = user.Id,
                    user_id = user.Id,
                    Status = -1,
                    Content = content,
                    DataJSON = Data.JSON.Serialize(celss)
                };

                sub.portfolio_id = portfolioId;
                sub.project_id = project.Id;
                sub.project_item_id = projectItem.Id;

                sub.Code = Utility.Parse36Encode(sub.Id.Value.GetHashCode());

                Utility.CMS.ObjectEntity<Subject>().Insert(sub);
                Utility.CMS.ObjectEntity<ProjectDynamic>().Insert(new ProjectDynamic
                {
                    Time = Utility.TimeSpan(sub.LastDate.Value),
                    user_id = user.Id,
                    Explain = "导入了文档",
                    project_id = sub.project_id,
                    refer_id = sub.Id,
                    Title = sub.Title,
                    Type = DynamicType.Subject
                });
                var path = String.Format("{0}/{1}/{2}", project.Code, projectItem.Code, sub.Code);

                this.Context.Send(new UMC.Web.WebMeta().Put("type", "Subject.Portfolio.Import").Put("Id", Portfolio.Id).Put("Sub", sub.Id).Put("Title", sub.Title)
                    .Put("Path", path), true);
            }
        }

    }
}