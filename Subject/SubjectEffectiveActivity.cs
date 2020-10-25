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
    class SubjectEffectiveActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var refer_id = UMC.Data.Utility.Guid(this.AsyncDialog("Refer", g =>
           {
               return new Web.UITextDialog() { Title = "评论的主题" };
           })).Value;
            var user = UMC.Security.Identity.Current;
            if (user.IsAuthenticated == false)
            {
                this.Prompt("不支持匿名点赞，请登录", false);
                response.Redirect("Account", "Login");

            }

            var ui = this.AsyncDialog("UI", g => this.DialogValue("none"));
            var section = this.AsyncDialog("section", g => this.DialogValue("1"));
            var row = this.AsyncDialog("row", g => this.DialogValue("1"));


            Utility.CMS.ObjectEntity<UMC.Data.Entities.Proposal>()
                    .Where.And().Equal(new Proposal { user_id = user.Id, ref_id = refer_id }).Entities.IFF(e =>
                    {
                        if (e.Count() > 0)
                        {
                            e.Delete();

                            var cmdEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Comment>()
                                      .Where.And().Equal(new Comment { Id = refer_id })
                                     .Entities;
                            cmdEntity.Update("{0}+{1}", new Comment { Effective = -1 });
                            var cmt = cmdEntity.Single();
                            if (cmt != null)
                            {
                                var vale = new UMC.Web.WebMeta().Put("section", section).Put("row", row).Put("method", "PUT").Put("reloadSinle", true);

                                vale.Put("value", new UMC.Web.WebMeta().Cell(Utility.Comment(cmt, request.Model)));

                                this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Edit", ui, vale), true);

                            }

                            return false;
                        }
                        return true;
                    }
                , e =>
                    {
                        e.Insert(new UMC.Data.Entities.Proposal
                        {
                            ref_id = refer_id,
                            user_id = user.Id,
                            Poster = user.Alias,
                            CreationDate = DateTime.Now
                        });
                        var cmdEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Comment>()
                                  .Where.And().Equal(new Comment { Id = refer_id })
                                 .Entities;
                        cmdEntity.Update("{0}+{1}", new Comment { Effective = 1 });
                        var cmt = cmdEntity.Single();
                        if (cmt != null)
                        {
                            var vale = new UMC.Web.WebMeta().Put("section", section).Put("row", row).Put("method", "PUT").Put("reloadSinle", true);

                            vale.Put("value", new UMC.Web.WebMeta().Cell(Utility.Comment(cmt, request.Model)));

                            this.Context.Send(new UMC.Web.WebMeta().UIEvent("UI.Edit", ui, vale), true);
                        }

                    });

        }

    }
}