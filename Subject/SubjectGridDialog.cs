using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Data.Entities;
using System.Collections;

namespace UMC.Subs.Activities
{
    public class SubjectGridDialog : UMC.Web.UIGridDialog
    {
        public SubjectGridDialog()
        {
            this.IsSearch = true;
            this.Title = "资讯搜索";
        }
        protected override System.Collections.Hashtable GetHeader()
        {
            var header = new Header("Id", 25);
            header.AddField("Title", "标题");
            return header.GetHeader();
        }

        protected override System.Collections.Hashtable GetData(System.Collections.IDictionary paramsKey)
        {
            var start = UMC.Data.Utility.Parse((paramsKey["start"] ?? "0").ToString(), 0);
            var limit = UMC.Data.Utility.Parse((paramsKey["limit"] ?? "25").ToString(), 25);
            var productEntity = Utility.CMS.ObjectEntity<Subject>()
                .Where.And().Equal(new Subject { Status = 1 }).Entities;

            productEntity.Order.Asc(new Subject { ReleaseDate = DateTime.MinValue });


            var Keyword = (paramsKey["Keyword"] as string ?? String.Empty);//.Split(',');

            if (String.IsNullOrEmpty(Keyword) == false)
            {
                productEntity.Where.And().Like(new Subject { Title = Keyword });

            }
            var hash = new Hashtable();
            hash["data"] = productEntity.Query(start, limit);
            hash["total"] = productEntity.Count();
            return hash;
            //var gr =  new GridHeader<Subject>(25, new Subject
            //{
            //    Id = Guid.Empty,
            //    Title = String.Empty

            //}, productEntity);

            //return gr.GetData(paramsKey);
        }
    }
}