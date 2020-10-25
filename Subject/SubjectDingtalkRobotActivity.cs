using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using UMC.Web;
using UMC.Web.UI;

namespace UMC.Subs.Activities
{
    class SubjectDingtalkRobotActivity : WebActivity
    {
        public static ProjectUserSetting AccessToken(Guid projectId)
        {

            var projectSetting = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectSetting>()
                   .Where.And().Equal(new ProjectSetting { project_id = projectId, Type = 12 }).Entities.Single();
            if (projectSetting == null)
            {
                return null;
            }
            return Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
                              .Where.And().Equal(new ProjectUserSetting { Id = projectSetting.user_setting_id, Type = projectSetting.Type }).Entities.Single();
        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var projectId = UMC.Data.Utility.Guid(this.AsyncDialog("Project", g =>
           {
               this.Prompt("请输入项目");
               return new Web.UITextDialog() { Title = "项目" };
           })).Value;

            var user = Security.Identity.Current;
            var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                      .Where.And().Equal(new Project { Id = projectId }).Entities.Single();
            if (project == null) { this.Prompt("没有此项目"); }
            if (project.user_id != user.Id)
            {
                this.Prompt("只有项目创立人才能配置此权限");
            }

            var setting = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectSetting>()
                   .Where.And().Equal(new ProjectSetting { project_id = projectId, Type = 12 }).Entities.Single() ?? new ProjectSetting();
            ProjectUserSetting projectSettings = new ProjectUserSetting();
            if (setting.user_setting_id.HasValue)
            {
                projectSettings = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
              .Where.And().Equal(new ProjectUserSetting { Id = setting.user_setting_id, Type = 12, user_id = user.Id }).Entities.Single() ?? new ProjectUserSetting();

                //if (projectSettings.user_id != user.Id)
                //{
                //    this.Prompt("只能配置自己的项目");
                //}
            }

            var settins = this.AsyncDialog("Setting", g =>
            {
                var form = new UIFormDialog();
                form.Title = "配置钉钉群机器人";
                form.AddText("Webhook", "AccessToken", projectSettings.AccessToken);
                form.AddText("加签Secret", "AppSecret", projectSettings.AppSecret);

                form.Submit("确认", request, "Subject.Settings");


                return form;
            });
            var access_token = settins["AccessToken"];
            var AppSecret = settins["AppSecret"];


            if (String.IsNullOrEmpty(access_token))
            {
                this.Prompt("钉钉应用配置不正确");
            }
            else
            {
                var pseting = new ProjectUserSetting()
                {
                    user_id = user.Id,
                    Id = projectSettings.Id ?? Guid.NewGuid(),
                    AccessToken = access_token,
                    Type = 12,
                    AppSecret = AppSecret,
                    ExpiresTime = 0// Utility.TimeSpan() + (Utility.IntParse(expires_in, 0) / 5)
                };
                Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
                  .Where.And().Equal(new ProjectUserSetting { Id = pseting.Id }).Entities.IFF(e => e.Update(pseting) == 0, e => e.Insert(pseting));

                var setting2 = new ProjectSetting() { user_setting_id = pseting.Id, project_id = project.Id, Type = 12 }; Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectSetting>()
                       .Where.And().Equal(new ProjectSetting { project_id = projectId, Type = 12 }).Entities
                       .IFF(e => e.Update(setting2) == 0, e => e.Insert(setting2));
            }

            this.Context.Send("Subject.Settings", true);

        }

    }
}