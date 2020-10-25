using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Retail;
using UMC.Retail.Entities;
using UMC.Retail.Activities;
using UMC.Web.UI;
using UMC.Web;
using UMC.Data.Entities;
using System.Collections;

namespace UMC.Subs.Activities
{



    class SubjectCopyActivity : WebActivity
    {



        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var sId = this.AsyncDialog("Id", ag =>
            {


                return this.DialogValue("none");
            });
            var subEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>();

            subEntity.Where.And().Equal(new Subject { Id = Utility.Guid(sId, true) });

            var sub = subEntity.Single();
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {
                response.Redirect(POSModel.Account, POSCommand.Login);
            }
            var attr = new AttributeNames(request.Ticket);

            if (sub.user_id == user.Id)
            {
                this.Prompt("此图文是你自己的，不用复制");
            }
            var Score = ((sub.Score ?? 0) + 1);
            if (String.Equals("markdown", sub.ContentType, StringComparison.CurrentCultureIgnoreCase))
            {

                this.Prompt("此图文是Markdown格式，不支持复制编辑");
            }
            if ((sub.soure_id ?? Guid.Empty) != Guid.Empty)
            {
                this.AsyncDialog("Confirm", g => new Web.UIConfirmDialog("此图文来源于复制，你需要去原图文才能复制，点击确认将去原文"));

                this.Context.Send(new UISectionBuilder(request.Model, request.Command, new UMC.Web.WebMeta().Put("Id", sub.soure_id))

                        .Builder(), true);
            }

            this.AsyncDialog("Confirm", g => new Web.UIConfirmDialog(String.Format("复制此图文需要扣减 {0} 积分", Score)));//，你需要去原图文才能复制，点击确认将去原文"));



            var memberEntity = UMC.Retail.Entities.Utility.Database.ObjectEntity<UMC.Retail.Entities.VIPMember>();
            var member = memberEntity.Where.And().Equal(new VIPMember { Id = user.Id.Value }).Entities.Single();
            if (member == null || member.Points < Score)
            {
                this.Context.Send(new UISectionBuilder(request.Model, "UIData", new UMC.Web.WebMeta().Put("Id", "Subject.Points"))

                         .Builder(), true);
            }

            VIPUtility.MemberLog(attr.StoreId.Value, attr.Ticket.POSCode, sub.Id.Value, 0 - Score, String.Format("复制图文 {0}", sub.Title), MemberLogType.ExChange, user.Id.Value);

            if (sub.AppId.HasValue)
            {
                VIPUtility.MemberLog(attr.StoreId.Value, attr.Ticket.POSCode, sub.Id.Value, 1, String.Format("复制图文 {0}", sub.Title), MemberLogType.Royalty, sub.AppId.Value);

            }
            if (sub.user_id.HasValue && Score - 1 > 0)
            {
                VIPUtility.MemberLog(attr.StoreId.Value, attr.Ticket.POSCode, sub.Id.Value, Score - 1, String.Format("复制图文 {0}", sub.Title), MemberLogType.Sales, sub.AppId.Value);

            }
            sub.category_id = null;
            sub.Favs = 0;
            sub.Look = 0;
            sub.Reply = 0;
            sub.Status = -1;
            sub.SubmitTime = null;
            sub.soure_id = sub.Id;
            sub.AppId = null;
            sub.LastDate = DateTime.Now;
            sub.ReleaseDate = DateTime.Now;
            sub.Poster = user.Alias;
            var newId = Guid.NewGuid();
            sub.user_id = user.Id;

            var config = Data.JSON.Deserialize<Hashtable>(sub.ConfigXml) ?? new Hashtable();
            var imageKey = config["images"] as Hashtable ?? new Hashtable();
            sub.ConfigXml = Data.JSON.Serialize(new UMC.Web.WebMeta().Put("images", imageKey));

            var pictureEntity = UMC.Data.Database.Instance().ObjectEntity<UMC.Data.Entities.Picture>();

            pictureEntity.Where.And().GreaterEqual(new Data.Entities.Picture { Seq = 0 });
            var images = new List<Picture>();
            pictureEntity.Where
                .And().In(new Data.Entities.Picture
                {
                    group_id = sub.Id
                }).Entities.Query(dr =>
                {
                    images.Add(dr);
                    dr.group_id = newId;
                });
            var webr = UMC.Data.WebResource.Instance();
            foreach (var r in images)
            {

                var rpath1 = String.Format("{2}{1}/{0}/0.jpg", "1", newId, UMC.Data.WebResource.ImageResource);
                var rpath3 = String.Format("{2}{1}/{0}/0.jpg", "1", sub.Id, UMC.Data.WebResource.ImageResource);
                webr.CopyResolveUrl(rpath3, rpath1);
            }
            if (images.Count > 0)
                pictureEntity.Insert(images.ToArray());
            sub.Id = newId;
            subEntity.Insert(sub);
            response.Redirect(request.Model, "EditUI", sub.Id.ToString());


        }

    }
}