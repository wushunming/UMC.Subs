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
    class SubjectDeleteActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            if (request.IsCashier)
            {

                var refer_id = UMC.Data.Utility.Guid(this.AsyncDialog("Id", g =>
                {
                    return new Web.UITextDialog() { Title = "评论的主题" };
                }));

                Utility.CMS.ObjectEntity<UMC.Data.Entities.Comment>().Where.Or().Equal(new Comment
                {
                    Id = refer_id,
                    for_id = refer_id
                }).Entities.Delete();
            }
        }

    }
}