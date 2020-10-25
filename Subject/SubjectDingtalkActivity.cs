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
    class SubjectDingtalkActivity : WebActivity
    {
        public static ProjectUserSetting JsAccessToken(Guid projectId)
        {
            var puser = AccessToken(projectId);
            if (puser != null)
            {
                if (puser.APIExpiresTime > Data.Utility.TimeSpan(DateTime.Now))
                {
                    return puser;
                }
                var url = String.Format("https://oapi.dingtalk.com/get_jsapi_ticket?access_token={0}", puser.AccessToken);
                System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
                var data = UMC.Data.JSON.Deserialize(httpClient.GetStringAsync(url)
                      .Result) as Hashtable;
                if (data["errcode"].ToString() == "0")
                {
                    var ticket = data["ticket"] as string;
                    var expires_in = String.Format("{0}", data["expires_in"]);
                    puser.APIExpiresTime = Utility.TimeSpan() + Utility.IntParse(expires_in, 0);
                    puser.APITicket = ticket;


                    Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
                      .Where.And().Equal(new ProjectUserSetting { Id = puser.Id }).Entities.Update(new ProjectUserSetting()
                      {
                          APIExpiresTime = puser.APIExpiresTime,
                          APITicket = ticket
                      });// == 0, e => e.Insert(pseting));
                }
                //puser.s
            }
            return puser;
        }
        public static ProjectUserSetting AccessToken(Guid projectId)
        {

            var projectSetting = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectSetting>()
                   .Where.And().Equal(new ProjectSetting { project_id = projectId, Type = 11 }).Entities.Single() ?? new ProjectSetting();
            if (projectSetting == null)
            {
                return null;
            }
            ProjectUserSetting setting = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
                   .Where.And().Equal(new ProjectUserSetting { Id = projectSetting.user_setting_id, Type = projectSetting.Type }).Entities.Single();
            if (setting == null)
            {
                return null;
            }
            if (setting.ExpiresTime > Data.Utility.TimeSpan(DateTime.Now))
            {
                return setting;
            }
            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            var text = httpClient.GetStringAsync(String.Format("https://oapi.dingtalk.com/gettoken?appkey={0}&appsecret={1}", setting.AppId, setting.AppSecret))
                  .Result;
            var acc = Data.JSON.Deserialize(text) as Hashtable;
            var access_token = acc["access_token"] as string;
            var expires_in = acc["expires_in"] as string;
            if (String.IsNullOrEmpty(access_token))
            {
                return null;
            }
            else
            {
                Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
                  .Where.And().Equal(new ProjectUserSetting { Id = projectSetting.user_setting_id, Type = 11 }).Entities.Update(new ProjectUserSetting()
                  {
                      AccessToken = access_token,
                      ExpiresTime = Utility.TimeSpan() + (Utility.IntParse(expires_in, 0) / 5)
                  });// == 0, e => e.Insert(pseting));
            }
            setting.AccessToken = access_token;
            return setting;
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
                   .Where.And().Equal(new ProjectSetting { project_id = projectId, Type = 11 }).Entities.Single() ?? new ProjectSetting();
            ProjectUserSetting projectSettings = new ProjectUserSetting();
            if (setting.user_setting_id.HasValue)
            {
                projectSettings = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
              .Where.And().Equal(new ProjectUserSetting { Id = setting.user_setting_id, Type = 11 }).Entities.Single() ?? new ProjectUserSetting();

                if (projectSettings.user_id == user.Id)
                {

                }
                else
                {
                    this.AsyncDialog("Confirm", g => new UIConfirmDialog("此项目钉钉配置来源于引用，你确认移除从新配置吗"));
                    projectSettings = new ProjectUserSetting();
                    //Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectSetting>()
                    //   .Where.And().Equal(new ProjectSetting { project_id = projectId, Type = 11 }).Entities
                    // .Delete();
                }
            }
            var type = this.AsyncDialog("Type", "No");
            switch (type)
            {
                case "Dingtalk":
                    var strt = UMC.Security.AccessToken.Current.Data["DingTalk-Setting"] as string;//, Utility.Guid(projectId)).Commit();
                    if (String.IsNullOrEmpty(strt))
                    {
                        this.Prompt("当前非钉钉环境");
                    }

                    var userSetting = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
                        .Where.And().Equal(new ProjectUserSetting { Id = Utility.Guid(strt, true) }).Entities.Single();

                    if (userSetting == null)
                    {
                        this.Prompt("钉钉环境配置 错误，请从新登录");
                    }
                    var setting2 = new ProjectSetting() { user_setting_id = userSetting.Id, project_id = project.Id, Type = 11 }; Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectSetting>()
                           .Where.And().Equal(new ProjectSetting { project_id = projectId, Type = 11 }).Entities
                           .IFF(e => e.Update(setting2) == 0, e => e.Insert(setting2));
                    this.Prompt("设置成功", false);
                    this.Context.Send("Subject.Settings", true);

                    break;
            }

            var settins = this.AsyncDialog("Setting", g =>
            {
                var form = new UIFormDialog();
                form.Title = "配置钉钉应用";
                form.AddText("企业Id", "CorpId", projectSettings.CorpId);
                form.AddText("应用Id", "AgentId", projectSettings.AgentId);
                form.AddText("AppKey", "AppId", projectSettings.AppId);
                form.AddText("AppSecret", "AppSecret", projectSettings.AppSecret);

                form.Submit("确认", request, "Subject.Settings");

                var strt = UMC.Security.AccessToken.Current.Data["DingTalk-Setting"] as string;//, Utility.Guid(projectId)).Commit();
                if (String.IsNullOrEmpty(strt) == false)
                {
                    form.AddUI("引用当前环境钉钉配置", "").Command(request.Model, "Dingtalk", new WebMeta().Put("Project", project.Id.ToString()).Put("Type", "Dingtalk"));

                }
                return form;
            });
            var AppId = settins["AppId"];
            var AppSecret = settins["AppSecret"];

            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            var text = httpClient.GetStringAsync(String.Format("https://oapi.dingtalk.com/gettoken?appkey={0}&appsecret={1}", AppId, AppSecret))
                  .Result;

            var acc = Data.JSON.Deserialize(text) as Hashtable;
            var access_token = acc["access_token"] as string;
            var expires_in = acc["expires_in"] as string;
            if (String.IsNullOrEmpty(access_token))
            {
                this.Prompt("钉钉应用配置不正确");
            }
            else
            {
                var pseting = new ProjectUserSetting()
                {
                    CorpId = settins["CorpId"],
                    AgentId = settins["AgentId"],
                    user_id = user.Id,
                    Id = projectSettings.Id ?? Guid.NewGuid(),
                    AppId = AppId,
                    AccessToken = access_token,
                    Type = 11,
                    AppSecret = AppSecret,
                    ExpiresTime = Utility.TimeSpan() + (Utility.IntParse(expires_in, 0) / 5)
                };
                Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectUserSetting>()
                  .Where.And().Equal(new ProjectUserSetting { Id = pseting.Id }).Entities.IFF(e => e.Update(pseting) == 0, e => e.Insert(pseting));

                var setting2 = new ProjectSetting() { user_setting_id = pseting.Id, project_id = project.Id, Type = 11 }; Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectSetting>()
                       .Where.And().Equal(new ProjectSetting { project_id = projectId, Type = 11 }).Entities
                       .IFF(e => e.Update(setting2) == 0, e => e.Insert(setting2));
            }

            this.Context.Send("Subject.Settings", true);

        }

    }
}