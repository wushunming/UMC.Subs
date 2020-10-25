using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Data.Entities;
using UMC.Web.UI;
using UMC.Web;
using System.IO;

namespace UMC.Subs.Activities
{



    class SubjectEditUIActivity : WebActivity
    {

        private static string CreateToken(string message, string secret)
        {
            secret = secret ?? "";
            var encoding = System.Text.UTF8Encoding.UTF8; ;
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new System.Security.Cryptography.HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }
        void Send(WebRequest request, Subject sub, string cid, bool robot)
        {
            if (sub.project_id.HasValue == false)
            {
                return;
            }
            var user = Security.Identity.Current;
            var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
            {
                Id = sub.project_id.Value
            }).Entities.Single();
            var projectItem = Utility.CMS.ObjectEntity<ProjectItem>().Where.And().Equal(new ProjectItem
            {
                Id = sub.project_item_id.Value
            }).Entities.Single();
            if (robot)
            {
                var pseting = SubjectDingtalkRobotActivity.AccessToken(sub.project_id.Value);
                if (pseting != null)
                {
                    var url = pseting.AccessToken;
                    if (url.StartsWith("https://") == false)
                    {
                        url = "https://oapi.dingtalk.com/robot/send?access_token=" + url;

                    }
                    var sbUrl = new StringBuilder();
                    sbUrl.Append(url);
                    sbUrl.Append("&timestamp=");
                    var timestamp = Utility.TimeSpan() + "000";
                    sbUrl.Append(timestamp);
                    sbUrl.Append("&sign=");
                    sbUrl.Append(CreateToken(timestamp + "\n" + pseting.AppSecret, pseting.AppSecret));

                    var msg = new WebMeta().Put("msgtype", "actionCard").Put("actionCard", new WebMeta().Put("title", sub.Title, "text", string.Format("### {2}的内容发布\n### 标题:{0}\n#### 摘要：\n>{1}...", sub.Title, sub.Description, user.Alias), "singleTitle", "阅读全文", "singleURL", String.Format("https://www.365lu.cn/{0}/{1}/{2}", project.Code, projectItem.Code, sub.Code)));

                    var content = new System.Net.Http.StringContent(UMC.Data.JSON.Serialize(msg), System.Text.UTF8Encoding.UTF8, "application/json");

                    System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
                    var str = httpClient.PostAsync(sbUrl.ToString(), content).Result
                            .Content.ReadAsStringAsync().Result;
                    Utility.Log("Robot", sbUrl.ToString(), str, msg);
                }
            }

            if (request.UserAgent.IndexOf("DingTalk") > 0)
            {
                //var strt = UMC.Security.AccessToken.Current.Data["DingTalk-Sub-Id"] as string;

                var userid = UMC.Security.AccessToken.Current.Data["DingTalk-User-Id"] as string;
                if (String.IsNullOrEmpty(cid) == false && String.IsNullOrEmpty(userid) == false)
                {
                    //  
                    var Url = String.Format("https://oapi.dingtalk.com/message/send_to_conversation?access_token={0}", SubjectDingtalkActivity.AccessToken(sub.project_id.Value).AccessToken);

                    var hash = new Hashtable();
                    hash["cid"] = cid;
                    hash["sender"] = userid;

                    hash["msg"] = new WebMeta().Put("msgtype", "action_card").Put("action_card", new WebMeta().Put("title", sub.Title, "markdown", string.Format("### {2}的内容发布\n### 标题:{0}\n#### 摘要：\n>{1}...", sub.Title, sub.Description, user.Alias), "single_title", "阅读全文", "single_url", String.Format("https://www.365lu.cn/{0}/{1}/{2}", project.Code, projectItem.Code, sub.Code)));


                    System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
                    var t = httpClient.PostAsync(Url, new System.Net.Http.StringContent(UMC.Data.JSON.Serialize(hash))).Result
                          .Content.ReadAsStringAsync().Result;

                }


            }

        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var Id = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g =>
           {
               this.Prompt("请输入参数");
               return this.DialogValue("none");
           }), true);

            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            subEntity.Where.And().Equal(new Subject { Id = Id });

            var sub = subEntity.Single();

            var user = UMC.Security.Identity.Current;

            if (sub.project_id.HasValue)
            {
                var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
                {
                    Id = sub.project_id
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
                                if (sub.Status > 0)
                                {
                                    this.Context.Send(new UISectionBuilder(request.Model, "UIData", new UMC.Web.WebMeta().Put("Id", Id))

                                            .Builder(), true);

                                }
                                if (sub.user_id == user.Id)
                                {
                                    this.Prompt(String.Format("{0}项目收回了您的编辑权限", project.Caption));
                                }
                                else
                                {
                                    this.Prompt("您未有编辑此图文的权限");
                                }
                                break;
                        }

                    }
                    else
                    {
                        if (sub.Status > 0)
                        {
                            this.Context.Send(new UISectionBuilder(request.Model, "UIData", new UMC.Web.WebMeta().Put("Id", Id))

                                    .Builder(), true);
                        }
                        if (sub.user_id == user.Id)
                        {
                            this.Prompt(String.Format("{0}项目收回了你的编辑权限", project.Caption));
                        }
                        else
                        {
                            this.Prompt("您未有编辑此图文的权限");
                        }
                    }
                }
            }
            if (String.IsNullOrEmpty(request.SendValue) == false)
            {

                this.Context.Send(new UISectionBuilder(request.Model, request.Command, new UMC.Web.WebMeta().Put("Id", Id))

                    .RefreshEvent("Subject.Save", "image", "Subject.Content").CloseEvent("Subject.Del")
                        .Builder(), true);
            }
            // var appKey = UMC.Security.Principal.Current.AppKey ?? Guid.Empty;

            var Model = this.AsyncDialog("Model", gKey =>
            {

                var webr = UMC.Data.WebResource.Instance();



                UITitle uITItle = UITitle.Create();
                uITItle.Title = "图文发布";
                var sestion = UISection.Create(uITItle);



                var pictureEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Picture>();

                pictureEntity.Where.And().GreaterEqual(new Data.Entities.Picture { Seq = 0 });
                pictureEntity.Order.Asc(new Data.Entities.Picture { Seq = 0 });
                var images = new List<String>();
                var items = new List<WebMeta>();
                pictureEntity.Where
                    .And().In(new Data.Entities.Picture
                    {
                        group_id = sub.Id
                    })
                    .Entities.Query(dr =>
                    {
                        var src = webr.ResolveUrl(dr.group_id.Value, dr.Seq, "0");
                        var chachKey = "?_ts=" + UMC.Data.Utility.TimeSpan(dr.UploadDate.Value);
                        items.Add(new UMC.Web.WebMeta().Put("src", src + "!200" + chachKey).Put("click", new Web.UIClick(new UMC.Web.WebMeta().Put("Id", dr.group_id.Value).Put("Seq", dr.Seq).Put(gKey, "Picture"))
                        {
                            Model = request.Model,
                            Command = request.Command
                        }));
                        images.Add(src + "?_ts=" + chachKey);
                    });
                var uidesc = new UIDesc(sub.Title);
                uidesc.Style.Bold().Height(50);
                uidesc.Click(new UIClick("Id", Id.ToString(), gKey, "Title") { Model = request.Model, Command = request.Command });

                var nine = new UMC.Web.WebMeta().Put("images", items);

                sestion.Delete(uidesc, new UIEventText().Click(new UIClick("Id", Id.ToString(), gKey, "Del") { Model = request.Model, Command = request.Command }));
                var sT = sub.Description ?? sub.Title;
                if (sT.Length > 48)
                {
                    sT = sT.Substring(0, 48) + "...";
                }
                var desc = new UIDesc(sT);
                desc.Style.Height(40).Color(0x999).Size(13);//.Name("border", "none");
                desc.Click(new UIClick("Id", Id.ToString(), gKey, "Desc") { Model = request.Model, Command = request.Command });
                sestion
                .Add(UICell.Create("NineImage", nine)).Add(desc)
                .AddCell("封面方式", sub.IsPicture == true ? "显示大图" : (images.Count > 2 ? "三张图" : "单张图")
                , new UIClick("Id", Id.ToString(), gKey, "Show") { Model = request.Model, Command = request.Command });
                if (images.Count < 3)
                {
                    nine.Put("click", new UIClick("Id", Id.ToString(), gKey, "Image") { Model = request.Model, Command = request.Command });

                }
                if (request.IsApp)
                {
                    sestion.NewSection().AddCell('\uf044', "编辑正文", "", new UIClick(Id.ToString()) { Command = "Content", Model = request.Model });

                }
                var status = "审阅中";

                if (sub.Status > 0)
                {
                    status = "已发布";
                }
                else if (sub.Status < 0)
                {
                    if (sub.Status == -2)
                    {
                        status = "被驳回";
                    }
                    else
                    {
                        status = "未发布";
                    }
                }
                var cateName = "草稿";
                if (sub.project_item_id.HasValue)
                {


                    //var portfolio = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>().Where.And().Equal(new Data.Entities.Portfolio
                    //{
                    //    Id = sub.portfolio_id.Value
                    //}).Entities.Single();

                    var ProjectItem = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>().Where.And().Equal(new Data.Entities.ProjectItem
                    {
                        Id = sub.project_item_id.Value
                    }).Entities.Single();

                    var Project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>().Where.And().Equal(new Data.Entities.Project
                    {
                        Id = sub.project_id.Value
                    }).Entities.Single();
                    cateName = String.Format("{0}/{1}", Project.Caption, ProjectItem.Caption);




                }
                var ness = sestion
                  .NewSection().AddCell("发布状态", status);

                ness.AddCell("所属专栏", cateName
                       , new UIClick("Id", sub.Id.ToString(), gKey, "Project") { Model = request.Model, Command = request.Command });

                ness.AddCell("评论功能", (sub.IsComment ?? true) ? "开启" : "关闭", new UIClick("Id", Id.ToString(), gKey, "Comment") { Model = request.Model, Command = request.Command })
                  .NewSection()
                  .AddCell("浏览正文", "", new UIClick(sub.Id.ToString()).Send(request.Model, "View"))
                  .AddCell("管理评论", "", new UIClick(Id.ToString()) { Command = "Comments", Model = request.Model });


                if (request.UserAgent.IndexOf("DingTalk") > 0 && sub.project_id.HasValue)
                {

                    if (Utility.CMS.ObjectEntity<ProjectSetting>()
                            .Where.And().Equal(new ProjectSetting
                            {
                                project_id = sub.project_id,
                                Type = 11
                            }).Entities.Count() > 0)
                    {
                        var strt = UMC.Security.AccessToken.Current.Data["DingTalk-Sub-Id"] as string;//, Utility.Guid(projectId)).Commit();

                        var text = "";
                        if (Utility.Guid(sub.Id.Value) == strt)
                        {
                            text = UMC.Security.AccessToken.Current.Data["DingTalk-Session-Text"] as string;
                        }
                        ness.NewSection().AddCell("发送到群", text, new UIClick("Id", Id.ToString(), gKey, "DingTalk") { Model = request.Model, Command = request.Command });
                    }
                }
                else
                {

                    if (Utility.CMS.ObjectEntity<ProjectSetting>()
                            .Where.And().Equal(new ProjectSetting
                            {
                                project_id = sub.project_id,
                                Type = 12
                            }).Entities.Count() > 0)
                    {

                        ness.NewSection().AddCell("发送到群", "", new UIClick("Id", Id.ToString(), gKey, "DingTalk") { Model = request.Model, Command = request.Command });
                    }
                }
                //}

                sestion.UIFootBar = new UIFootBar().AddText(
                new UIEventText(sub.Status == 1 ? "下架" : "确认发布").Click(new UIClick("Id", Id.ToString(), "Model", "Status", "Status", sub.Status == 1 ? "-1" : "1")
                {
                    Model = request.Model,
                    Command = request.Command
                }).Style(new UIStyle().BgColor()), new UIEventText("摘正文摘要").Click(new UIClick("Id", Id.ToString(), "Model", "AutoDesc")
                {
                    Model = request.Model,
                    Command = request.Command
                }));
                sestion.UIFootBar.IsFixed = true;
                response.Redirect(sestion);

                return this.DialogValue("none");
            });
            switch (Model)
            {
                case "Picture":
                    var seq = this.AsyncDialog("Seq", "1");
                    this.AsyncDialog("Picture", g =>
                    {
                        var sel = new Web.UISheetDialog();
                        sel.Options.Add(new UIClick(new UMC.Web.WebMeta().Put("id", sub.Id.Value).Put("seq", seq))
                        {
                            Command = "Picture",
                            Text = "重新上传",
                            Model = "Design"
                        }); sel.Options.Add(new UIClick(new UMC.Web.WebMeta().Put("id", sub.Id.Value).Put("seq", seq).Put("media_id", "none"))
                        {
                            Command = "Picture",
                            Text = "删除图片",
                            Model = "Design"
                        });
                        return sel;
                    });
                    break;
                case "DingTalk":
                    {
                        if (sub.Status == 1)
                        {
                            switch (this.AsyncDialog("Type", g =>
                             {
                                 var ds = new UISelectDialog();
                                 ds.Options.Put("用机器人发送", "Robot");
                                 ds.Options.Put("选择到人发送", "Session");
                                 return ds;
                             }))
                            {
                                case "Robot":
                                    if (Utility.CMS.ObjectEntity<ProjectSetting>()
                          .Where.And().Equal(new ProjectSetting
                          {
                              project_id = sub.project_id,
                              Type = 12
                          }).Entities.Count() > 0)
                                    {
                                        this.Send(request, sub, String.Empty, true);
                                        this.Prompt("机器人发送已经发起");
                                    }
                                    else
                                    {
                                        this.Prompt("此项目未配置钉钉机器人");
                                    }
                                    break;
                            }

                        }
                        var DingTalk = Web.UIDialog.AsyncDialog("TalkId", g =>
                        {
                            if (request.UserAgent.IndexOf("DingTalk") == -1)
                            {
                                this.Prompt("非钉钉环境，不能获取到钉钉会话参数");
                            }
                            var ticket = SubjectDingtalkActivity.JsAccessToken(sub.project_id.Value);
                            if (ticket == null)
                            {
                                this.Prompt("未有钉钉配置");
                            }
                            if (String.IsNullOrEmpty(ticket.AgentId))
                            {
                                this.Prompt("未有钉钉应用ID");
                            }
                            var nonceStr = Utility.TimeSpan();
                            var timeStamp = Utility.TimeSpan();
                            var url = (request.UrlReferrer ?? request.Url).AbsoluteUri;

                            String plain = "jsapi_ticket=" + ticket.APITicket + "&noncestr=" + nonceStr + "&timestamp=" + timeStamp
                                        + "&url=" + url;
                            var config = new WebMeta();
                            config.Put("agentId", ticket.AgentId);
                            config.Put("corpId", ticket.CorpId);
                            config.Put("timeStamp", timeStamp.ToString());
                            config.Put("nonceStr", nonceStr.ToString());
                            config.Put("url", url);
                            config.Put("signature", Utility.SHA1(plain).ToLower());
                            config.Put("jsApiList", new string[] { "biz.map.view", "biz.chat.pickConversation" });


                            this.Context.Send("Subject.DingTalk", new WebMeta().Put("method", "pickConversation").Put("Sign", config).Put("Params", new WebMeta(request.Arguments).Put("_model", request.Model, "_cmd", request.Command)).Put("Key", g), true);
                            return this.DialogValue("none");
                        });
                        if (sub.Status == 1)
                        {
                            this.Send(request, sub, DingTalk, false);
                            this.Prompt("会话发送已经发起");
                        }
                        else
                        {
                            UMC.Security.AccessToken.Current.Put("DingTalk-Sub-Id", Utility.Guid(sub.Id.Value)).Put("DingTalk-Session-Text", this.AsyncDialog("TalkId-Text", "Text")).Put("DingTalk-Session-Id", DingTalk).Commit();
                        }



                    }
                    break;
                case "Image":
                    Web.UIDialog.AsyncDialog("Image", g =>
                     {
                         var dl = new Web.UISheetDialog() { Title = "图片上传" };
                         dl.Options.Add(new Web.UIClick(sub.Id.ToString()) { Model = request.Model, Command = "Image", Text = "正文图片" });
                         dl.Options.Add(new Web.UIClick(sub.Id.ToString()) { Model = "Design", Command = "Picture", Text = "本地图片" });
                         return dl;
                     });
                    break;
                case "Title":
                    var title = Web.UIDialog.AsyncDialog("Title", g =>
                    {
                        var dl = new Web.UIFormDialog() { Title = "图文标题" };
                        dl.AddTextarea("快文标题", "Title", sub.Title);
                        dl.Submit("确认更改", request, "Subject.Save");
                        return dl;
                    });
                    subEntity.Update(new Subject
                    {
                        Title = title
                    });
                    break;
                case "Desc":
                    var desc = Web.UIDialog.AsyncDialog("Description", g =>
                    {
                        var dl = new Web.UIFormDialog() { Title = "图文摘要" };
                        dl.AddTextarea("图文摘要", "Description", sub.Description ?? sub.Title).Put("Rows", 10);
                        dl.Submit("确认更改", request, "Subject.Save");
                        return dl;
                    });
                    subEntity.Update(new Subject
                    {
                        Description = desc
                    });
                    break;
                case "Score":
                    var Score = Utility.IntParse(Web.UIDialog.AsyncDialog("Score", g =>
                   {
                       var dl = new Web.UIFormDialog() { Title = "图文积分" };
                       dl.AddNumber("图文积分", "Score", sub.Score);
                       dl.Submit("确认更改", request, "Subject.Save");
                       return dl;
                   }), 0);
                    if (Score < 0)
                    {
                        this.Prompt("积分必须大于或等于零");
                    }
                    subEntity.Update(new Subject
                    {
                        Score = Score
                    });
                    break;
                case "Project":
                    {
                        var meta = new WebMeta();
                        if (sub.project_id.HasValue)
                        {

                            var project = Utility.CMS.ObjectEntity<Project>().Where.And().Equal(new Project
                            {
                                Id = sub.project_id.Value
                            }).Entities.Single();
                            if (project != null)
                            {
                                if (project.Id == user.Id)
                                {

                                }
                                else
                                {
                                    meta.Put("Project", sub.project_id);
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
                        var sid = UMC.Data.Utility.Guid(this.AsyncDialog("PortfolioId", request.Model, "Portfolio", meta)).Value;

                        var portfolio = Utility.CMS.ObjectEntity<UMC.Data.Entities.Portfolio>()
                            .Where.And().Equal(new Portfolio { Id = sid }).Entities.Single();
                        var projectItem = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                             .Where.And().Equal(new ProjectItem { Id = portfolio.project_item_id }).Entities.Single();
                        if (request.IsCashier == false)
                        {
                            var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>()
                                 .Where.And().Equal(new Project { Id = portfolio.project_id }).Entities.Single();
                            if (project.user_id.Value != user.Id)
                            {

                                var member = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectMember>()
                                  .Where.And().Equal(new ProjectMember { project_id = project.Id }).Entities.Single();
                                if (member == null)
                                {
                                    this.Prompt("你尚未加入此项目的专栏");
                                }
                                else
                                {
                                    switch (member.AuthType)
                                    {
                                        case WebAuthType.Admin:
                                        case WebAuthType.User:
                                            break;
                                        default:
                                            this.Prompt("你尚未加入此项目的专栏");
                                            break;
                                    }
                                }
                            }
                        }
                        if (String.IsNullOrEmpty(sub.Code))
                        {

                            subEntity.Update(new Subject()
                            {
                                Code = Utility.Parse36Encode(sub.Id.Value.GetHashCode()),
                                portfolio_id = portfolio.Id,
                                project_id = projectItem.project_id,
                                project_item_id = projectItem.Id,
                                last_user_id = user.Id
                            });
                        }
                        else
                        {

                            subEntity.Update(new Subject()
                            {
                                portfolio_id = portfolio.Id,
                                project_id = projectItem.project_id,
                                project_item_id = projectItem.Id,
                                last_user_id = user.Id
                            });
                        }
                    }
                    break;
                case "Comment":
                    var s2 = UMC.Data.Utility.IntParse(this.AsyncDialog("Comment", g =>
                    {
                        var dl = new Web.UISelectDialog() { Title = "评论功能" };
                        dl.Options.Add("开启评论", "1");
                        dl.Options.Add("关闭评论", "-1");
                        return dl;
                    }), 0);
                    subEntity.Update(new Subject
                    {
                        IsComment = s2 == 1
                    });
                    break;
                case "Status":
                    var s = UMC.Data.Utility.IntParse(this.AsyncDialog("Status", g =>
                   {
                       var dl = new Web.UISelectDialog() { Title = "发布确认" };
                       dl.Options.Add("不发布", "-1");
                       dl.Options.Add("发布", "1");
                       return dl;
                   }), 0);
                    if (sub.project_id.HasValue == false || sub.project_item_id.HasValue == false || sub.portfolio_id.HasValue == false)
                    {
                        this.Prompt("请选择发布的栏位");
                    }
                    if ((sub.soure_id ?? Guid.Empty) != Guid.Empty)
                    {
                        this.Prompt("提示", "非原创，公共栏目不接收，只限于公众号群发。");
                    }
                    if (s == 0)
                    {
                        if (String.IsNullOrEmpty(sub.Url) == false && sub.Status == -1)
                        {
                            s = 1;
                        }
                    }
                    if (s == -1 && sub.Status == -2)
                    {
                        this.Prompt("被驳回状态，不需要此操作");
                    }

                    String Sdesc = null;
                    if (s > 0)
                    {
                        //if (String.IsNullOrEmpty(sub.Code))
                        if (String.IsNullOrEmpty(sub.Description))
                        {
                            //   sub.DataJSON
                            var celss = UMC.Data.JSON.Deserialize<WebMeta[]>((String.IsNullOrEmpty(sub.DataJSON) ? "[]" : sub.DataJSON)) ?? new UMC.Web.WebMeta[] { };
                            var sb = new StringBuilder();
                            foreach (var pom in celss)
                            {
                                switch (pom["_CellName"])
                                {
                                    case "CMSText":
                                        var value = pom.GetMeta("value");
                                        var format = pom.GetMeta("format")["text"];
                                        var fValue = Utility.Format(format, value.GetDictionary(), String.Empty);
                                        if (String.Equals(fValue, sub.Title) == false)
                                        {
                                            sb.Append(fValue);
                                        }
                                        if (sb.Length > 250)
                                        {
                                            break;
                                        }

                                        break;
                                }
                            }
                            Sdesc = sb.Length > 250 ? sb.ToString(0, 250) : sb.ToString();
                            sub.Description = Sdesc;
                        }

                        var IsDraught = Utility.CMS.ObjectEntity<UMC.Data.Entities.Picture>().Where
                            .And().In(new Data.Entities.Picture
                            {
                                group_id = sub.Id
                            }).Entities.Count() == 0;
                        subEntity.Update(new Subject
                        {
                            Status = s,
                            Description = Sdesc,
                            IsDraught = IsDraught,
                            Code = String.IsNullOrEmpty(sub.Code) ? Utility.Guid(sub.Id.Value) : null,
                            Poster = user.Alias,

                            ReleaseDate = DateTime.Now
                        });
                        var cid = String.Empty;//
                        var strt = UMC.Security.AccessToken.Current.Data["DingTalk-Sub-Id"] as string;//, Utility.Guid(projectId)).Commit();

                        if (Utility.Guid(sub.Id.Value) == strt)
                        {
                            cid = UMC.Security.AccessToken.Current.Data["DingTalk-Session-Id"] as string;
                        }
                        Send(request, sub, cid, true);
                        if (String.IsNullOrEmpty(strt) == false)
                            UMC.Security.AccessToken.Current.Put("DingTalk-Sub-Id", String.Empty).Commit();

                        this.Prompt("发布成功", false);
                    }
                    else
                    {
                        subEntity.Update(new Subject
                        {
                            Status = s,
                            Code = String.IsNullOrEmpty(sub.Code) ? Utility.Guid(sub.Id.Value) : null,
                        });

                    }
                    break;
                case "AutoDesc":
                    {

                        //   sub.DataJSON
                        var celss = UMC.Data.JSON.Deserialize<WebMeta[]>((String.IsNullOrEmpty(sub.DataJSON) ? "[]" : sub.DataJSON)) ?? new UMC.Web.WebMeta[] { };
                        var sb = new StringBuilder();
                        foreach (var pom in celss)
                        {
                            switch (pom["_CellName"])
                            {
                                case "CMSText":
                                    var value = pom.GetMeta("value");
                                    var format = pom.GetMeta("format")["text"];

                                    var fValue = Utility.Format(format, value.GetDictionary(), String.Empty);
                                    if (String.Equals(fValue, sub.Title) == false)
                                    {
                                        sb.Append(fValue);
                                    }
                                    if (sb.Length > 250)
                                    {
                                        break;
                                    }

                                    break;
                            }
                        }
                        var Sdesc2 = sb.Length > 250 ? sb.ToString(0, 250) : sb.ToString();

                        subEntity.Update(new Subject
                        {
                            Description = Sdesc2
                        });

                    }
                    break;
                case "Show":
                    var m = this.AsyncDialog("Show", g =>
                    {
                        var dl = new Web.UISelectDialog() { Title = "封面展示方式" };
                        dl.Options.Put("封面大图形式", "Max").Put("封面小图形式", "Min");
                        return dl;
                    });
                    subEntity.Update(new Subject { IsPicture = String.Equals(m, "Max") });
                    break;
                case "Del":
                    subEntity.Update(new Subject { Visible = -1, LastDate = DateTime.Now });
                    //subEntity.Delete();

                    this.Context.Send("Subject.Del", new WebMeta().Put("Id", sub.Id), false);
                    break;
            }

            this.Context.Send("Subject.Save", true);
        }
    }
}