using System;
using System.Collections.Generic;
using UMC.Data.Entities;
using UMC.Web;
using UMC.Web.UI;

[assembly: UMC.Web.Mapping]
namespace UMC.Subs
{

    public class Utility : UMC.Data.Utility
    {

        public static UMC.Data.Database CMS
        {
            get
            {
                return UMC.Data.Database.Instance();//.For(System.Guid.Empty);
            }
        }

        public static UIComment Comment(Comment cm, string model)
        {

            var forcomdss = new List<Comment>();
            Utility.CMS.ObjectEntity<Comment>().Where.And().Equal(new Data.Entities.Comment
            {
                for_id = cm.Id
            }).Entities.Order.Asc(new Comment { CommentDate = DateTime.MaxValue }).
            Entities.Query(dr => forcomdss.Add(dr));
            var pics = new List<UMC.Data.Entities.Picture>();

            CMS.ObjectEntity<Data.Entities.Picture>().Where.And().Equal(new Data.Entities.Picture { group_id = cm.Id })
                .Entities.Order.Asc(new Data.Entities.Picture { Seq = 0 }).Entities.Query(g => pics.Add(g));
            return Comment(cm, forcomdss, pics, UMC.Data.WebResource.Instance(), model);

        }
        public static UIComment Comment(Comment cm, List<Comment> replys, List<UMC.Data.Entities.Picture> pics, UMC.Data.WebResource webr, string model)
        {

            var btnStyle = new UIStyle().Size(12).Name("icon", new UIStyle().Font("wdk").Size(18)).Color(0x666);
            var cell = new UIComment(webr.ResolveUrl(cm.user_id.Value, 1, "0") + "!50");
            cell.Id = cm.Id.ToString();
           // cell.Tag(new UIEventText("d"));
            var image = new List<UIComment.Image>();
            cell.ImageClick(UIClick.Pager(model, "Account", new WebMeta().Put("Id", cm.user_id), true));

            UMC.Data.Utility.Each(pics, g =>
            {
                if (g.group_id == cm.Id)
                {
                    image.Add(new UIComment.Image
                    {
                        src = webr.ResolveUrl(cm.Id.Value, g.Seq, "0") + "!m200?_ts=" + UMC.Data.Utility.TimeSpan(g.UploadDate.Value),
                        max = webr.ResolveUrl(cm.Id.Value, g.Seq, "0")
                    });

                }
            });
            var nick = cm.Poster;
            if (String.IsNullOrEmpty(nick))
            {
                nick = "游客";
            }
            if (Data.Utility.IsPhone(nick))
            {
                nick = "手机客户";
            }
            cell.Name("name", nick)//.Name("tag", "x").Name("desc", "dd")
                .Content(cm.Content).Name("time", String.Format("{0:yyyy.MM.dd HH:mm}", cm.CommentDate));
            cell.Button(new UIEventText('\uf087', cm.Effective > 0 ? String.Format("( {0} )", cm.Effective) : "赞").Format("{icon} {text}").Style(btnStyle)
                .Click(Web.UIClick.Click(new UIClick("Refer", cm.Id.ToString()) { Model = model, Command = "Effective" }))
                , new UIEventText('\uF0E5', "回复").Format("{icon} {text}").Style(btnStyle)
                .Click(Web.UIClick.Click(new UIClick("Refer", cm.Id.ToString()) { Model = model, Command = "Comment" })));

            var rs = new List<UIComment.Reply>();
            foreach (var re in replys)
            {
                var ts = new UIComment.Reply { content = "{desc}", title = "{nick} 在 {time} 回复说:" };
                ts.data = new UMC.Web.WebMeta().Put("desc", re.Content).Put("time", String.Format("{0:yyyy.MM.dd HH:mm}", re.CommentDate))
                    .Put("nick", re.Poster);

                //ts.style.
                rs.Add(ts);
            }
            cell.Replys(rs.ToArray());
            cell.Images(image.ToArray());
            return cell;

        }
        public static List<UIComment> Comments(Data.Sql.IObjectEntity<Comment> entity, int start, int limit, String model)
        {
            var proposals = new List<Proposal>();

            var comments = new List<Comment>();
            var forcomdss = new List<Comment>();
            var ids = new List<Guid>();
            var user = UMC.Security.Identity.Current;
            entity.Where.And().Greater(new Comment { Visible = -1 });

            entity.Query(start, limit, g =>
            {
                comments.Add(g);
                ids.Add(g.Id.Value);
            });
            if (ids.Count > 0)
            {
                entity.Where.Reset().And().In(new Comment { for_id = ids[0] }, ids.ToArray());
                entity.Order.Clear();
                entity.Order.Asc(new Comment { CommentDate = DateTime.MaxValue });
                entity.Query(dr => forcomdss.Add(dr));

                Utility.CMS.ObjectEntity<Proposal>().Where.And()
                    .Equal(new Proposal { user_id = user.Id })
                    .And().In(new Proposal { ref_id = ids[0] }, ids.ToArray())
                    .Entities.Query(dr => proposals.Add(dr));
            }
            var pics = new List<UMC.Data.Entities.Picture>();
            if (ids.Count > 0)
            {
                CMS.ObjectEntity<Data.Entities.Picture>().Where.And().In(new Data.Entities.Picture { group_id = ids[0] }, ids.ToArray())
                    .Entities.Order.Asc(new Data.Entities.Picture { Seq = 0 }).Entities.Query(g => pics.Add(g));
            }

            var webr = UMC.Data.WebResource.Instance();
            var cells = new List<UIComment>();

            foreach (var cm in comments)
            {

                var replys = forcomdss.FindAll(g => g.for_id == cm.Id);
                cells.Add(Comment(cm, replys, pics, webr, model));
            }
            return cells;
        }


    }
}
