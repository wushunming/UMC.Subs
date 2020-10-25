using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data;
using UMC.Web;

namespace UMC.Subs.Activities
{
    class SubjectLoginActivity : WebActivity
    {

        void DD(String code, Guid projectId)
        {
            var accessToken = SubjectDingtalkActivity.AccessToken(projectId);
            var url = String.Format("https://oapi.dingtalk.com/user/getuserinfo?access_token={1}&code={0}", code, accessToken.AccessToken);

            System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
            var data = UMC.Data.JSON.Deserialize(httpClient.GetStringAsync(url)
                  .Result) as Hashtable;
            if (data.ContainsKey("userid"))
            {
                var userId = data["userid"] as string;

                var url2 = String.Format("https://oapi.dingtalk.com/user/get?access_token={1}&userid={0}", userId, accessToken.AccessToken);
                var data2 = UMC.Data.JSON.Deserialize(httpClient.GetStringAsync(url2).Result) as Hashtable;
                var mgurl = data2["avatar"] as string;
                var mobile = data2["mobile"] as string;
                if (String.IsNullOrEmpty(mobile))
                {
                    Utility.Debug("login", data, data2);
                    this.Prompt("未开通获取手机号码权限，请联系钉钉管理员");
                }

                var open = new UMC.Data.Entities.Account()
                {
                    Type = UMC.Security.Account.MOBILE_ACCOUNT_KEY,
                    Name = mobile
                };

                Security.Account.GetRelation(open);


                var nickname = data2["name"] as string;
                var user = UMC.Security.Membership.Instance().Identity(open.user_id.Value);
                if (user == null)
                {
                    user = UMC.Security.Membership.Instance().CreateUser(open.user_id.Value, "@" + mobile, Utility.Guid(open.user_id.Value), nickname);
                }
                UMC.Security.AccessToken.Login(user
                    , UMC.Security.AccessToken.Token.Value, 0, "DingTalkPC", false).Put("DingTalk-Setting", Utility.Guid(accessToken.Id.Value))
                    .Put("DingTalk-User-Id", userId).Commit();


                if (String.IsNullOrEmpty(mgurl) == false)
                    UMC.Data.WebResource.Instance().Transfer(new Uri(mgurl), open.user_id.Value, 1);

                this.Context.Send("User", true);


            }
            else
            {
                Utility.Debug("login", data);
                this.Prompt("未获得钉钉信息");
            }
        }

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var projectId = UMC.Data.Utility.Guid(this.AsyncDialog("Project", g =>
            {
                this.Prompt("请输入项目");
                return new Web.UITextDialog() { Title = "项目" };
            })).Value;
            var code = this.AsyncDialog("Code", g =>
            {

                this.Prompt("请输入项目");
                return new Web.UITextDialog() { Title = "项目" };
            });
            var Type = this.AsyncDialog("Type", "11");

            switch (Type)
            {
                case "11":
                    DD(code, projectId);
                    break;
            }



        }

    }
}