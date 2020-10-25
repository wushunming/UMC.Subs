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



    class SubjectWeiXinActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var Id = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g =>
            {
                this.Prompt("请输入参数");
                return this.DialogValue("none");
            }), true);

            if (String.IsNullOrEmpty(request.SendValue) == false)
            {

                this.Context.Send(new UISectionBuilder(request.Model, request.Command, new UMC.Web.WebMeta().Put("Id", Id))

                    .RefreshEvent("Subject.Save", "Subject.WeiXin")
                        .Builder(), true);
            }
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            subEntity.Where.And().Equal(new Subject { Id = Id });

            var sub = subEntity.Single();
            var webr = UMC.Data.WebResource.Instance();

            //var appid = "wx141d18e076be64a1";
            var user = UMC.Security.Identity.Current;
            var appid = user.Id.ToString();
            var config = Data.JSON.Deserialize<Hashtable>(sub.ConfigXml) ?? new Hashtable();
            var imageKey = config["images"] as Hashtable ?? new Hashtable();
            config["images"] = imageKey;
            Array articleids = config["articles"] as Array;
            var subsid = new List<Guid>();
            if (articleids != null)
            {
                foreach (var o in articleids)
                {
                    subsid.Add(Utility.Guid(o.ToString()).Value);
                }
            }
            var Model = this.AsyncDialog("Model", gKey =>
            {
                UITitle uITItle = UITitle.Create();
                uITItle.Title = "公众号群发";
                var sestion = UISection.Create(uITItle);

                var src = webr.ResolveUrl(sub.Id.Value, 1, "0") + "!cms1";
                if (subsid.Count == 0)
                {

                    var image = UICell.Create("CMSImage", new UMC.Web.WebMeta().Put("src", src));
                    image.Style.Padding(0, 10);
                    var title = new UIDesc(sub.Title);
                    title.Style.Bold().Height(40).Name("border", "none");
                    title.Click(new UIClick("Id", Id.ToString(), gKey, "Title") { Model = request.Model, Command = request.Command });

                    var desc = new UIDesc(sub.Description ?? sub.Title);
                    desc.Style.Height(40).Color(0x999).Name("border", "none");
                    desc.Click(new UIClick("Id", Id.ToString(), gKey, "Desc") { Model = request.Model, Command = request.Command });


                    sestion.Add(title)
                    .Add(image)
                    .Add(desc);
                }
                else
                {
                    var image = UICell.Create("CMSImage", new UMC.Web.WebMeta().Put("src", src).Put("title", sub.Title));
                    image.Style.Padding(10, 10, 0, 10);
                    image.Format.Put("title", "{title}");

                    sestion.Add(image);
                    Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                    .Where.And().In(new Subject { Id = subsid[0] }, subsid.ToArray())
                    .Entities.Query(dr =>
                    {
                        var cell = new UIImageTextValue(webr.ResolveUrl(sub.Id.Value, 1, "0") + "!200", dr.Title, null);
                        cell.Style.Name("image-width", 60);
                        cell.Click(new UIClick("Id", dr.Id.ToString(), gKey, "Items")
                        {
                            Model = request.Model,
                            Command = request.Command
                        });

                        var item = new UMC.Web.WebMeta().Put("_CellName", cell.Type).Put("value", cell.Data).Put("format", cell.Format).Put("style", cell.Style)
                        .Put("del", new UMC.Web.WebMeta().Put("click", new UIClick("Id", Id.ToString(), gKey, "Del", "Item", dr.Id.ToString())
                        {
                            Model = request.Model,
                            Command = request.Command
                        }));
                        sestion.AddCells(item);

                    });

                }
                var ctip = "未同步公众号";
                var ModifiedTime = config["ModifiedTime"] as string;
                if (String.IsNullOrEmpty(ModifiedTime) == false)
                {
                    ctip = String.Format("已同步公众号", Utility.GetDate(Utility.TimeSpan(Utility.IntParse(ModifiedTime, 0))));
                }

                var ls = sestion.NewSection().AddCell("图文封面", config.ContainsKey("thumb_media_id") ? "已同步公众号" : "未同步公众号", new UIClick("Id", Id.ToString(), gKey, "Thumb")
                {
                    Model = request.Model,
                    Command = request.Command
                }).AddCell("图文正文", ctip, new UIClick("Id", Id.ToString(), gKey, "Content")
                {
                    Model = request.Model,
                    Command = request.Command
                });
                var mstatus = "";
                if (config.ContainsKey("ContentLoading"))
                {
                    mstatus = "正在同步请等候";
                }
                else
                {
                    mstatus = config.ContainsKey("media_id") ? "已在公众生成" : "未在公众号生成";
                }

                ls.AddCell("群发素材", mstatus, new UIClick("Id", Id.ToString(), gKey, "Material")
                {
                    Model = request.Model,
                    Command = request.Command
                })
                .NewSection().AddCell("使用帮助", "查看", UIClick.Pager("Message", "UIData", new UMC.Web.WebMeta().Put("Id", "Help.SendWeiXin")));



                sestion.UIFootBar = new UIFootBar().AddText(new UIEventText("追加图文")
                    .Click(new UIClick("Id", Id.ToString(), "Model", "Preview")
                    {
                        Model = request.Model,
                        Command = request.Command
                    }), new UIEventText("微信预览")
                    .Click(new UIClick("Id", Id.ToString(), "Model", "Articles")
                    {
                        Model = request.Model,
                        Command = request.Command
                    }).Style(new UIStyle().BgColor()), new UIEventText("确认群发").Click(new UIClick("Id", Id.ToString(), "Model", "SendAll")
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
                case "Articles":
                    {
                        var sudId = Utility.Guid(this.AsyncDialog("Articles", request.Model, "Select")).Value;

                        var config2 = Data.JSON.Deserialize<Hashtable>(sub.ConfigXml) ?? new Hashtable();
                        Array articles2 = config2["articles"] as Array;
                        var subsid2 = new List<Guid>();
                        if (articles2 != null)
                        {
                            foreach (var o in articles2)
                            {
                                subsid.Add(Utility.Guid(o.ToString()).Value);
                            }
                        }
                        subsid.Remove(sudId);
                        subsid.Add(sudId);
                        config2["articles"] = subsid;
                        subEntity.Update(new Subject
                        {
                            ConfigXml = Data.JSON.Serialize(config2)
                        });
                    }
                    break;
                case "Title":
                    var title = Web.UIDialog.AsyncDialog("Title", g =>
                    {
                        var dl = new Web.UIFormDialog() { Title = "图文标题" };
                        dl.AddTextarea("图文标题", "Title", sub.Title);
                        dl.Submit("确认更改", request, "Subject.Save");
                        return dl;
                    });
                    subEntity.Update(new Subject
                    {
                        Title = title
                    });
                    this.Context.Send("Subject.Save", true);
                    break;
                case "Desc":
                    var desc = Web.UIDialog.AsyncDialog("Description", g =>
                    {
                        var dl = new Web.UIFormDialog() { Title = "图文摘要" };
                        dl.AddTextarea("图文摘要", "Description", sub.Description);
                        dl.Submit("确认更改", request, "Subject.WeiXin");
                        return dl;
                    });
                    subEntity.Update(new Subject
                    {
                        Description = desc
                    });
                    break;
                case "Thumb":
                    if (webr.SubmitCheck(appid) == false)
                    {
                        response.Redirect("Message", "Auth");
                    }
                    if (config.ContainsKey("thumb_media_id"))
                    {
                        var thumb_media_id = config["thumb_media_id"] as string;
                        webr.Submit("cgi-bin/material/del_material", Data.JSON.Serialize(new UMC.Web.WebMeta().Put("media_id", thumb_media_id)), appid);

                    }

                    var src = webr.ResolveUrl(sub.Id.Value, 1, "0") + "!cms1";


                    var data = webr.Submit("cgi-bin/material/add_material&type=image", src, appid);
                    if (data.ContainsKey("media_id") == false)
                    {
                        this.Prompt("同步封面图片失败");
                    }
                    else
                    {
                        config["thumb_media_id"] = data["media_id"];

                        subEntity.Update(new Subject
                        {
                            ConfigXml = Data.JSON.Serialize(config)
                        });
                        this.Prompt("最新封面成功同步到公众号。", false);
                    }
                    break;
                case "Content":
                    if (webr.SubmitCheck(appid) == false)
                    {
                        response.Redirect("Message", "Auth");
                    }
                    if (config.ContainsKey("thumb_media_id") == false)
                    {
                        this.Prompt("请先同步封面，再来同步正文");
                    }

                    var ModifiedTime = Utility.IntParse(config["ModifiedTime"] as string ?? "", 0);
                    if (Utility.TimeSpan() - ModifiedTime < 10)
                    {
                        this.Prompt("提示", "正在同步，请过一会再更新");
                    }
                    var res = this.AsyncDialog("Content", g =>
                    {
                        if (request.SendValues != null)
                        {
                            var url = request.SendValues["Url"];
                            if (String.IsNullOrEmpty(url) == false)
                            {
                                return this.DialogValue(System.Text.UTF8Encoding.UTF8.GetString(new UMC.Net.HttpClient().DownloadData(url)));

                            }
                        }
                        var domUrl = new Uri(request.UrlReferrer ?? request.Url, String.Format("{0}Show/{1}/UIMin/Id/{2}", webr.WebDomain(), request.Model, sub.Id)).AbsoluteUri;
                        this.Context.Send(new UMC.Web.WebMeta().Put("type", "OpenUrl").Put("selector", "#body section", "value", domUrl).Put("title", "正文同步到公众号素材")
                                        .Put("submit", new Web.UIClick("Id", sub.Id.ToString(), "Model", "Content", "Url", "Value")
                                        {
                                            Command = request.Command,
                                            Model = request.Model
                                        }), true);
                        return this.DialogValue("none");
                    });
                    config["ContentLoading"] = "YES";
                    subEntity.Update(new Subject { ConfigXml = Data.JSON.Serialize(config) });
                    UMC.Data.Reflection.Start(() =>
                    {
                        var udata = new Subject();
                        try
                        {
                            var content = Content(res, appid, imageKey);
                            config["ModifiedTime"] = Utility.TimeSpan();
                            config.Remove("ContentLoading");
                            udata.Content = content;

                        }
                        catch (Exception ex)
                        {
                            config.Remove("ContentLoading");
                            config["result"] = ex.Message;
                        }
                        finally
                        {
                            udata.ConfigXml = Data.JSON.Serialize(config);
                            subEntity.Update(udata);
                        }

                    });

                    break;
                case "Material":
                    if (webr.SubmitCheck(appid) == false)
                    {
                        response.Redirect("Message", "Auth");
                    }
                    if (config.ContainsKey("ModifiedTime") == false)
                    {
                        this.Prompt("提示", String.Format("“{0}”正文未同步到公众号", sub.Title));
                    }
                    if (config.ContainsKey("thumb_media_id") == false)
                    {
                        this.Prompt("提示", String.Format("“{0}”封面未同步到公众号", sub.Title));
                    }
                    if (config.ContainsKey("media_id"))
                    {
                        webr.Submit("cgi-bin/material/del_material", Data.JSON.Serialize(new UMC.Web.WebMeta().Put("media_id", config["media_id"])), appid);

                    }

                    var content_source_url = new Uri(request.Url, String.Format("{0}Page/{1}/UIData/Id/{2}", webr.WebDomain(), request.Model, sub.Id)).AbsoluteUri;


                    var articles = new List<WebMeta>();

                    articles.Add(new UMC.Web.WebMeta().Put("title", sub.Title)
                                                            .Put("author", user.Alias)
                                                            .Put("digest", sub.Description ?? sub.Title)
                                                            .Put("thumb_media_id", config["thumb_media_id"])
                                                            .Put("content", sub.Content)
                                                            .Put("show_cover_pic", 0)
                                                            .Put("content_source_url", content_source_url)
                                                            .Put("need_open_comment", (sub.IsComment ?? true) ? 1 : 0));
                    var subs = new List<Subject>();
                    if (subsid.Count > 0)
                    {
                        Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>().Where.And().In(new Subject
                        {
                            Id = subsid[0]
                        }, subsid.ToArray())
                        .Entities.Query(dr => subs.Add(dr));


                        foreach (var key in subsid)
                        {

                            var csub = subs.Find(g => g.Id == key);

                            var sconfig = Data.JSON.Deserialize<Hashtable>(csub.ConfigXml) ?? new Hashtable();


                            if (sconfig.ContainsKey("ModifiedTime") == false)
                            {
                                this.Prompt("提示", String.Format("“{0}”正文未同步到公众号", csub.Title));
                            }
                            if (sconfig.ContainsKey("thumb_media_id") == false)
                            {
                                this.Prompt("提示", String.Format("“{0}”封面未同步到公众号", csub.Title));
                            }
                            content_source_url = new Uri(request.Url, String.Format("{0}Page/{1}/UIData/Id/{2}", webr.WebDomain(), request.Model, csub.Id)).AbsoluteUri;

                            articles.Add(new UMC.Web.WebMeta().Put("title", csub.Title)
                                             .Put("author", user.Alias)
                                             .Put("digest", csub.Description ?? csub.Title)
                                             .Put("thumb_media_id", sconfig["thumb_media_id"])
                                             .Put("content", csub.Content)
                                             .Put("show_cover_pic", 0)
                                             .Put("content_source_url", content_source_url)
                                             .Put("need_open_comment", (sub.IsComment ?? true) ? 1 : 0));

                        }
                    }
                    var sData = webr.Submit("cgi-bin/material/add_news", Data.JSON.Serialize(new UMC.Web.WebMeta().Put("articles", articles)), appid);
                    if (sData.ContainsKey("media_id") == false)
                    {
                        this.Prompt("提示", "上传图文素材失败");
                    }
                    else
                    {
                        config["media_id"] = sData["media_id"];
                        config["MediaTime"] = Utility.TimeSpan();
                        config["result"] = "ok";
                        subEntity.Update(new Subject
                        {
                            ConfigXml = Data.JSON.Serialize(config)
                        });
                        this.Prompt("提示", "最新图文素材成功更新到公众号。", false);
                    }
                    break;
                case "SendAll":
                    if (webr.SubmitCheck(appid) == false)
                    {
                        response.Redirect("Message", "Auth");
                    }
                    if (config.ContainsKey("media_id") == false)
                    {
                        this.Prompt("提示", "请先生成群发素材");
                    }
                    if (config.ContainsKey("msg_id"))
                    {
                        var msgData = webr.Submit("cgi-bin/message/mass/get", Data.JSON.Serialize(new UMC.Web.WebMeta().Put("msg_id", config["msg_id"])), appid);

                        switch (msgData["msg_status"] as string)
                        {
                            case "SEND_SUCCESS":
                                this.Prompt("提示", "群发发送成功");
                                break;
                            case "SENDING":
                                this.Prompt("提示", "群发正在发送中");
                                break;
                            case "SEND_FAIL":
                                this.Prompt("提示", "群发发送失败，请在公众号后台查看");
                                break;
                            case "DELETE":
                                this.Prompt("提示", "信息已经删除");
                                break;
                        }
                    }


                    var tag = this.AsyncDialog("tag", t =>
                    {
                        var appKey = UMC.Security.Principal.Current.AppKey ?? Guid.Empty;
                        if (appKey == Guid.Empty)
                        {
                            return this.DialogValue("-1");
                        }
                        var tags = webr.Submit("cgi-bin/tags/get", String.Empty, appid);
                        if (String.Equals(tags["errcode"], "48001"))
                        {
                            return this.DialogValue("-1");
                        }
                        var header = new Web.UIGridDialog.Header("id", 0);
                        header.AddField("name", "粉丝标签");
                        header.AddField("count", "粉丝数");
                        var ls = new ArrayList();
                        ls.Add(new UMC.Web.WebMeta().Put("id", "-1", "name", "所有粉丝", "count", "all"));
                        ls.AddRange(tags["tags"] as Array);
                        var di = Web.UIGridDialog.Create(header, ls.ToArray());
                        di.Title = "群发的粉丝";
                        return di;
                    });

                    var filter = new UMC.Web.WebMeta().Put("is_to_all", tag == "-1");
                    if (tag != "-1")
                    {
                        filter.Put("tag_id", tag);
                    }

                    var sendall = new UMC.Web.WebMeta().Put("msgtype", "mpnews").Put("send_ignore_reprint", 0).Put("clientmsgid", Utility.Guid(sub.Id.Value))
                        .Put("filter", filter).Put("mpnews", new UMC.Web.WebMeta().Put("media_id", config["media_id"]));


                    var sendData = webr.Submit("cgi-bin/message/mass/sendall", Data.JSON.Serialize(sendall), appid);
                    if (sendData.ContainsKey("msg_data_id"))
                    {
                        config["msg_id"] = sendData["msg_id"];
                        config["msg_data_id"] = sendData["msg_data_id"];
                        config["clientmsgid"] = sub.Id;

                        subEntity.Update(new Subject
                        {
                            ConfigXml = Data.JSON.Serialize(config)
                        });
                        this.Prompt("提示", "群发指令已经成功提交");

                    }
                    else
                    {
                        var errcode = (sendData["errcode"] ?? "").ToString();
                        switch (errcode)
                        {
                            case "0":
                                this.Prompt("发送成功");
                                break;
                            case "48001":
                                this.Prompt("群发失败", "此公众号未认证");
                                break;
                            default:
                                this.Prompt("群发失败", "请确认此微信号已经关注公众号");
                                break;
                        }
                    }
                    break;
                case "Preview":
                    if (webr.SubmitCheck(appid) == false)
                    {
                        this.Context.Send(new UISectionBuilder("Message", "UIData", new UMC.Web.WebMeta().Put("Id", "Help.Auth"))

                                .Builder(), true);
                    }
                    if (config.ContainsKey("media_id") == false)
                    {
                        this.Prompt("提示", "请先生成群发素材");
                    }
                    else
                    {
                        var touser = Web.UIDialog.AsyncDialog("Touser", g =>
                        {
                            var dl = new Web.UIFormDialog() { Title = "微信号" };
                            dl.AddText("微信号", "Touser", String.Empty);
                            dl.Submit("确认", request, "Subject.WeiXin");
                            return dl;
                        });
                        //touser = "obUzr5iNYo78ptD8luWMgdZvIbkg";
                        var preview = new UMC.Web.WebMeta().Put("msgtype", "mpnews")
                            .Put("touser", touser).Put("mpnews", new UMC.Web.WebMeta().Put("media_id", config["media_id"]));


                        var rdata = webr.Submit("cgi-bin/message/mass/preview", Data.JSON.Serialize(preview), appid);
                        var errcode = (rdata["errcode"] ?? "").ToString();
                        switch (errcode)
                        {
                            case "0":
                                this.Prompt("发送成功");
                                break;
                            case "48001":
                                this.Prompt("发送失败", "此公众号未认证");
                                break;
                            default:
                                this.Prompt("发送失败", "请确认此微信号已经关注公众号");
                                break;
                        }
                    }
                    break;
                case "Items":

                    this.AsyncDialog("Items", g =>
                    {
                        var dl = new Web.UISheetDialog() { Title = "公众号同步" };
                        dl.Options.Add(new UIClick("Id", Id.ToString(), "Model", "Thumb")
                        {
                            Text = "封面同步到公众号",
                            Model = request.Model,
                            Command = request.Command
                        });
                        dl.Options.Add(new UIClick("Id", Id.ToString(), "Model", "Content")
                        {
                            Text = "正文同步到公众号",
                            Model = request.Model,
                            Command = request.Command
                        });
                        return dl;
                    });
                    break;
                case "Del":
                    var itemid = Utility.Guid(this.AsyncDialog("Item", "aut"));
                    if (itemid.HasValue)
                    {
                        subsid.Remove(itemid.Value);
                        config["articles"] = subsid;
                        subEntity.Update(new Subject
                        {
                            ConfigXml = Data.JSON.Serialize(config)
                        });
                    }
                    break;
            }

            this.Context.Send("Subject.WeiXin", true);
        }
        string Content(String html, String appId, Hashtable image)
        {
            var webr = UMC.Data.WebResource.Instance();
            var regex = new System.Text.RegularExpressions.Regex("(?<key>\\ssrc)=\"(?<src>[^\"]+)\"");
            return regex.Replace(html, g =>
            {
                var src = g.Groups["src"].Value;

                if (src.StartsWith("http://") || src.StartsWith("https://"))
                {
                    if (image.ContainsKey(src))
                    {
                        src = image["src"] as string;
                    }
                    else
                    {

                        if (src.StartsWith("http://mmbiz.qpic.cn/") == false)
                        {
                            var data = webr.Submit("cgi-bin/media/uploadimg", src, appId);
                            if (data != null)
                            {
                                if (data.ContainsKey("url"))
                                {
                                    image[src] = data["url"];
                                    src = data["url"] as string;

                                }
                            }
                            else
                            {
                                throw new ArgumentException("图片上传到公众号有错误，请联系管理员");
                            }
                        }
                    }

                }
                return String.Format("{0}=\"{1}\"", g.Groups["key"], src);
            });
        }
    }
}