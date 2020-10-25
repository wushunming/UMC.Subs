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
    class SubjectProjectAttenActivity : WebActivity
    {


        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var refer_id = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g =>
           {
               return new Web.UITextDialog() { Title = "关注Id" };
           }));
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {
                response.Redirect("Account", "Login");
            }

            var section = this.AsyncDialog("section", g => this.DialogValue("1"));
            var row = this.AsyncDialog("row", g => this.DialogValue("1"));
            var ui = this.AsyncDialog("UI", g => this.DialogValue("none"));

            var vale = new UMC.Web.WebMeta().Put("section", section).Put("row", row).Put("method", "VALUE").Put("reloadSinle", true);



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

                    this.AsyncDialog("Confrm", g => new UIConfirmDialog("你是项目的专栏作家，要取消此项目关注吗"));
                    memberEnttiy.Delete();

                }

                memberEnttiy.Where.Reset().And().Equal(new ProjectMember { project_id = refer_id });// Entities.Delete();

                UMC.Configuration.ConfigurationManager.ClearCache(refer_id.Value, "Data");

                this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Edit", ui, vale.Put("value", new UMC.Web.WebMeta().Put("button", "+关注").Put("button-color", "#e67979").Put("desc", String.Format("{0}人", memberEnttiy.Count() + 1)))), true);
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
                UMC.Configuration.ConfigurationManager.ClearCache(refer_id.Value, "Data");
                memberEnttiy.Where.Reset().And().Equal(new ProjectMember { project_id = refer_id });
                this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Edit", ui, vale.Put("value", new UMC.Web.WebMeta().Put("button", "已关注").Put("button-color", "#25b864").Put("desc", String.Format("{0}人", memberEnttiy.Count() + 1)))), true);

            }




        }

    }
}