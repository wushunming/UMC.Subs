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
    class SubjectAttentionActivity : WebActivity
    {

        public static String Attention(Guid project_id, out bool IsAttention)
        {

            var user = UMC.Security.Identity.Current;
            var acEnttiy = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                  .Where.And().Equal(new ProjectMember { user_id = user.Id, project_id = project_id }).Entities;
            if (acEnttiy.Count() == 0)
            {
                IsAttention = false;
                return "+关注";
            }
            else
            {
                IsAttention = true;
                return "已关注";
            }
        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var refer_id = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g =>
           {
               return new Web.UITextDialog() { Title = "关注Id" };
           }));
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {
                if (request.IsApp)
                {
                    response.Redirect("Account", "Login");
                }
                else
                {
                    this.Prompt("请登录");
                }
            }

            var section = this.AsyncDialog("section", g => this.DialogValue("1"));
            var row = this.AsyncDialog("row", g => this.DialogValue("1"));
            var ui = this.AsyncDialog("UI", g => this.DialogValue("none"));

            var vale = new UMC.Web.WebMeta().Put("section", section).Put("row", row).Put("method", "VALUE").Put("reloadSinle", true);


            var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                   .Where.And().Equal(new Project { Id = refer_id }).Entities.Single();
            if (project == null)
            {
                this.Prompt("未有此项目");
            }
            if (user.Id == project.user_id)
            {
                this.Prompt("你是项目的创立者，不需要关注");
            }

            var memberEnttiy = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                   .Where.And().Equal(new ProjectMember { user_id = user.Id, project_id = refer_id }).Entities;
            var member = memberEnttiy.Single();
            if (member != null)
            {
                if (member.AuthType == WebAuthType.Guest)
                {
                    memberEnttiy.Delete();

                }
                else
                {

                    this.AsyncDialog("Confrm", g => new UIConfirmDialog("你有此项目的管理权限，你要取消关注吗"));

                }
                if (request.Url.Query.Contains("_v=Sub") == false)
                {
                    UMC.Configuration.ConfigurationManager.ClearCache(refer_id.Value, null);
                    this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Edit", ui, vale.Put("value", new UMC.Web.WebMeta().Put("button", "+关注").Put("button-color", "#e67979"))), true);
                }
                else
                {
                    response.Redirect(new WebMeta().Put("text", "关注项目"));
                }
            }
            else
            {
                memberEnttiy.Insert(new UMC.Data.Entities.ProjectMember
                {
                    project_id = refer_id,
                    user_id = user.Id,
                    CreationTime = DateTime.Now,
                    AuthType = WebAuthType.Guest
                });
                Utility.CMS.ObjectEntity<ProjectDynamic>()
                           .Insert(new ProjectDynamic
                           {
                               Time = Utility.TimeSpan(DateTime.Now),
                               user_id = user.Id,
                               Explain = String.Format("{0} 加入了项目", user.Alias),
                               project_id = refer_id,
                               refer_id = user.Id,
                               Title = "项目成员",
                               Type = DynamicType.Member
                           });

                if (request.Url.Query.Contains("_v=Sub") == false)
                {
                    UMC.Configuration.ConfigurationManager.ClearCache(refer_id.Value, null);
                    this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Edit", ui, vale.Put("value", new UMC.Web.WebMeta().Put("button", "已关注").Put("button-color", "#25b864"))), true);
                }
                else
                {
                    response.Redirect(new WebMeta().Put("text", "已加入项目"));

                }

                //this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Edit", ui, vale.Put("value", new UMC.Web.WebMeta().Put("button", "已关注")).Put("style", new UIStyle().BgColor(0x25b864))), true);
            }



        }

    }
}