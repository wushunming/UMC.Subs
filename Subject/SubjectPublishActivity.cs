using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections;
using System.Reflection;
using UMC.Web;
using UMC.Data.Entities;
using UMC.Web.UI;

namespace UMC.Subs.Activities
{
    class SubjectPublishActivity : WebActivity
    {

        public override void ProcessActivity(WebRequest request, WebResponse response)
        {
            var Key = this.AsyncDialog("Key", g =>
            {
                this.Prompt("Key");
                return this.DialogValue("none");
            });
            var type = this.AsyncDialog("Type", "Sub");
            switch (type)
            {
                case "Check":
                    var sp = Utility.TimeSpan();
                    var cache = UMC.Configuration.ConfigurationManager.DataCache(Utility.Guid(Key, true).Value, "Data", 1000, (k, v, c) =>
                     {
                         var Hash = new Hashtable();
                         Hash["time"] = sp;
                         return Hash;

                     });
                    var hash = cache.CacheData;
                    hash["now"] = sp;
                    if (Utility.IntParse(String.Format("{0}", hash["time"]), 0) == sp)
                    {
                        hash["isPublish"] = true;
                    }
                    response.Redirect(hash);
                    break;
            }

            var id = Utility.Guid(this.AsyncDialog("Id", "auto"));

            var sourceKey = "TEMP/" + UMC.Data.Utility.GetRoot(request.Url) + '/' + Key;
            var oosr = Data.WebResource.Instance();


            var content = UMC.Data.JSON.Deserialize(new Net.HttpClient().GetStringAsync(new Uri(oosr.TempDomain() + sourceKey)).Result) as Hashtable;

            var title = String.Format("{0} ¡¤ ÌìÌìÂ¼", content["title"]);
            var key = content["key"] as string;
            var desc = content["desc"] as string;

            desc = (desc.Length > 150 ? desc.Substring(0, 150) : desc).Replace('"', '¡¯');
            key = (key.Length > 100 ? key.Substring(0, 100) : key).Replace('"', '¡¯');
            using (System.IO.Stream stream = typeof(SubjectPublishActivity).Assembly
                              .GetManifestResourceStream("UMC.Subs.Resources.sub.html"))
            {
                var html = content["html"] as string;
                var tpl = new System.IO.StreamReader(stream).ReadToEnd();
                var sb = HtmlPublish.Release(html, title, key, desc, tpl);
                if (sb.Length > 0)
                {
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {

                        var writer = new System.IO.StreamWriter(ms);
                        writer.Write(sb.ToString());
                        writer.Flush();
                        ms.Position = 0;
                        oosr.Transfer(ms, Key);
                    }
                    if (id.HasValue)
                    {
                        switch (type)
                        {
                            case "Sub":
                                Utility.CMS.ObjectEntity<Subject>()
                                    .Where.And().Equal(new Subject { Id = id.Value }).Entities.Update(new Subject { PublishTime = Utility.TimeSpan() });
                                break;
                            case "Item":
                                Utility.CMS.ObjectEntity<ProjectItem>()
                                    .Where.And().Equal(new ProjectItem { Id = id.Value }).Entities.Update(new ProjectItem { PublishTime = Utility.TimeSpan() });
                                break;
                            case "Project":
                                Utility.CMS.ObjectEntity<Project>()
                                    .Where.And().Equal(new Project { Id = id.Value }).Entities.Update(new Project { PublishTime = Utility.TimeSpan() });
                                break;
                        }
                    }
                }
            }
            response.Redirect(new WebMeta().Put("src", String.Format("{0}{1}", oosr.WebDomain(), Key)));

        }
    }
}