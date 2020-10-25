using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Web;
using UMC.Data.Entities;
using UMC.Web.UI;

namespace UMC.Subs.Activities
{
    /// <summary>
    /// 邮箱账户
    /// </summary>
    class SubjectMemberActivity : WebActivity
    {
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var key = this.AsyncDialog("Key", g => this.DialogValue("EDITER"));

            Guid? projectId = UMC.Data.Utility.Guid(this.AsyncDialog("Project", "EDITER"));//["Project"]);
            var type = this.AsyncDialog("Type", "All");


            var userId = Utility.Guid(Web.UIDialog.AsyncDialog("UserId", dKey =>
            {
                if (request.SendValues == null || request.SendValues.ContainsKey("start") == false)
                {
                    var buider = new UISectionBuilder(request.Model, request.Command, request.Arguments);
                    buider.CloseEvent("UI.Event");
                    this.Context.Send(buider.Builder(), true);
                }

                var webr = UMC.Data.WebResource.Instance();
                var form = request.SendValues ?? new UMC.Web.WebMeta();

                int limit = UMC.Data.Utility.IntParse(form["limit"] as string, 25);
                int start = UMC.Data.Utility.IntParse(form["start"] as string, 0);
                var sestion = start == 0 ? UISection.Create(new UIHeader().Search("搜索"), new UITitle("选择成员")) : UISection.Create();

                var objectEntity = Utility.CMS.ObjectEntity<ProjectMember>().Where.And().Equal(new Data.Entities.ProjectMember { project_id = projectId }).Entities;
                switch (type)
                {
                    default:
                        break;
                    case "User":
                        objectEntity.Where.And().Equal(new ProjectMember { AuthType = WebAuthType.User });
                        break;
                    case "Guest":
                        objectEntity.Where.And().Equal(new ProjectMember { AuthType = WebAuthType.Guest });
                        break;
                    case "Admin":
                        objectEntity.Where.And().Equal(new ProjectMember { AuthType = WebAuthType.Admin });
                        break;
                }
                var Keyword = (form["Keyword"] as string ?? String.Empty);


                if (String.IsNullOrEmpty(Keyword) == false)
                {
                    objectEntity.Where.And().Like(new ProjectMember { Alias = Keyword });
                }
                objectEntity.Query(start, limit, dr =>
                {

                    var cellUI = new UIIconNameDesc(new UIIconNameDesc.Item(webr.ResolveUrl(dr.user_id.Value, "1", "4"), dr.Alias,
                                              dr.CreationTime.ToString())
                                                      .Click(new UIClick(new WebMeta(request.Arguments).Put(dKey, dr.user_id)).Send(request.Model, request.Command)));
                    sestion.Add(cellUI);

                });
                if (sestion.Total == 0)
                {
                    sestion.Add("Desc", new UMC.Web.WebMeta().Put("desc", "未找到对应项目成员").Put("icon", "\uF016"), new UMC.Web.WebMeta().Put("desc", "{icon}\n{desc}"),
                        new UIStyle().Align(1).Color(0xaaa).Padding(20, 20).BgColor(0xfff).Size(12).Name("icon", new UIStyle().Font("wdk").Size(60)));
                }



                response.Redirect(sestion);
                return this.DialogValue("none");
            }));


            var member = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                  .Where.And().Equal(new Data.Entities.ProjectMember { user_id = userId, project_id = projectId }).Entities.Single();

            this.Context.Send(new WebMeta().UIEvent(key, new ListItem(member.Alias, member.user_id.ToString())), true);


        }
    }
}