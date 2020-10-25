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
    class SubjectCommentActivity : WebActivity
    {
        void UploadImage(Guid group_id, int seq, string UserHostAddress, Guid? userid)
        {
            var entity = UMC.Data.Database.Instance().ObjectEntity<UMC.Data.Entities.Picture>();

            entity.Where.And().Equal(new UMC.Data.Entities.Picture { Seq = seq, group_id = group_id });
            if (entity.Update(new UMC.Data.Entities.Picture
            {
                Location = UserHostAddress,
                UploadDate = DateTime.Now,
                user_id = userid
            }) == 0)
            {
                var photo = new UMC.Data.Entities.Picture
                {
                    Location = UserHostAddress,
                    group_id = group_id,
                    Seq = seq,
                    user_id = userid,
                    UploadDate = DateTime.Now
                };
                entity.Insert(photo);
            }

        }

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var refer_id = UMC.Data.Utility.Guid(this.AsyncDialog("Refer", g =>
            {
                return new Web.UITextDialog() { Title = "评论的主题" };
            }));

            var user = UMC.Security.Identity.Current;

            var Id = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g =>
            {
                return this.DialogValue(Guid.NewGuid().ToString());
            })).Value;

            var commentEntity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Comment>()
                    .Where.And().In(new Comment { Id = refer_id }, Id).Entities;
            var commant = commentEntity.Single();

            var ui = this.AsyncDialog("UI", g => this.DialogValue("none"));
            var section = this.AsyncDialog("section", g => this.DialogValue("1"));
            var row = this.AsyncDialog("row", g => this.DialogValue("1"));

            var Content = Web.UIDialog.AsyncDialog("Content", g =>
            {
                var from = new Web.UIFormDialog();
                if (commant != null)
                {
                    from.Title = "回复评论";
                }
                else
                {
                    from.Title = "评论";
                }
                if (request.IsApp)
                {
                    var sn = new WebMeta(request.Arguments);
                    sn.Put("Id", Id).Put("Refer", refer_id);
                    var ms = new WebMeta().Put("submit", new UIClick(sn) { Text = commant != null ? "请回复" : "请评论" }.Send(request.Model, request.Command));

                    this.Context.Send(commant == null ? "Comment" : "Reply", ms, true);

                }
                from.AddTextarea("内容", "Content", String.Empty).Put("MinSize", "6");

                if (commant == null)
                    from.AddFiles("图片上传", "Picture").Put("required", "none")
                  .Command("Design", "Picture", new UMC.Web.WebMeta().Put("id", Id));

                from.Submit(commant != null ? "确认回复" : "确认评论", request, "UI.Event");
                return from;
            });
            var image = this.AsyncDialog("image", "none");
            if (image != "none")
            {
                var webr = UMC.Data.WebResource.Instance();
                var imgs = image.Split(',');
                var ls = new List<Picture>();
                for (var i = 0; i < imgs.Length; i++)
                {
                    webr.Transfer(new Uri(imgs[i]), Id, i + 1);
                    ls.Add(new Picture { group_id = Id, Seq = i + 1, UploadDate = DateTime.Now, user_id = user.Id });
                }
                if (ls.Count > 0)
                {
                    var entity = UMC.Data.Database.Instance().ObjectEntity<UMC.Data.Entities.Picture>();

                    entity.Where.And().Equal(new UMC.Data.Entities.Picture { group_id = Id }).Entities.Delete();
                    entity.Insert(ls.ToArray());
                }
            }
            if (commant != null)
            {
                if (commant.Id == Id)
                {

                    return;
                }
                var forId = commant.for_id ?? Guid.Empty;
                if (forId == Guid.Empty)
                {
                    forId = commant.Id.Value;
                }
                var entity = Utility.CMS.ObjectEntity<Subject>()
                        .Where.And().Equal(new Subject { Id = commant.ref_id }).Entities;
                var sinle = entity.Single(new Subject { Reply = 0, project_id = Guid.Empty });
                if (sinle != null)
                {
                    if (sinle.Reply.HasValue)
                    {
                        entity.Update("{0}+{1}", new Subject { Reply = 1 });
                    }
                    else
                    {
                        entity.Update(new Subject { Reply = 1 });
                    }
                }

                commentEntity.Insert(new Comment
                {
                    Id = Id,
                    CommentDate = DateTime.Now,
                    for_id = forId,
                    ref_id = commant.ref_id,
                    Content = Content,
                    Effective = 0,
                    Farworks = 0,
                    Invalid = 0,
                    project_id = sinle != null ? sinle.project_id : null,
                    Reply = 0,
                    Score = 0,
                    Unhealthy = 0,
                    Visible = 0,
                    Poster = user.Alias,
                    user_id = user.Id
                });
                var cm = Utility.CMS.ObjectEntity<UMC.Data.Entities.Comment>()
                    .Where.And().Equal(new Data.Entities.Comment { Id = commant.Id })
                    .Entities.Single();
                var editer = new Web.UISection.Editer(section, row);
                editer.Put((Utility.Comment(cm, request.Model)), false);
                editer.Builder(this.Context, ui, true);

            }
            else
            {
                var entity = Utility.CMS.ObjectEntity<UMC.Data.Entities.Subject>()
                       .Where.And().Equal(new Subject { Id = refer_id }).Entities;
                var sinle = entity.Single(new Subject { Reply = 0, project_id = Guid.Empty });
                commentEntity.Insert(new Comment
                {
                    Id = Id,
                    CommentDate = DateTime.Now,
                    for_id = Guid.Empty,
                    ref_id = refer_id,
                    project_id = sinle != null ? sinle.project_id : null,
                    Content = Content,
                    Effective = 0,
                    Farworks = 0,
                    Invalid = 0,
                    Reply = 0,
                    Score = 0,
                    Unhealthy = 0,
                    Visible = 0,
                    Poster = user.Alias,
                    user_id = user.Id
                });
                if (sinle != null)
                {
                    if (sinle.Reply.HasValue)
                    {
                        entity.Update("{0}+{1}", new Subject { Reply = 1 });
                    }
                    else
                    {
                        entity.Update(new Subject { Reply = 1 });
                    }


                    var editer = new Web.UISection.Editer(1, 1);
                    var cm = Utility.CMS.ObjectEntity<UMC.Data.Entities.Comment>()
                    .Where.And().Equal(new Data.Entities.Comment { Id = Id, for_id = Guid.Empty })
                    .Entities.Single();

                    if (commentEntity.Where.Reset().And().Equal(new Comment { ref_id = refer_id, for_id = Guid.Empty }).And().Greater(new Comment
                    {
                        Visible = -1
                    }).Entities.Count() == 1)
                    {
                        editer.Put(Utility.Comment(cm, request.Model), false);
                    }
                    else
                    {

                        editer.Insert(Utility.Comment(cm, request.Model));
                    }

                    editer.Builder(this.Context, ui, true);

                }
            }



        }
    }
}