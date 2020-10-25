using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Data.Entities;
using UMC.Web;

namespace UMC.Subs.Activities
{
    [Mapping("System", "Scanning", Auth = WebAuthType.All, Desc = "移动扫描处理", Weight = 1)]
    public class SubjectScanningActivity : UMC.Web.Activity.SystemScanningActivity
    {
        protected override void Scanning(Uri url)
        {
            base.Scanning(url);
            var paths = new List<String>();

            paths.AddRange(url.LocalPath.Trim('/').Split('/'));
            if (paths.Count > 0)
            {
                var project = Utility.CMS.ObjectEntity<UMC.Data.Entities.Project>().Where.And().Equal(new Project { Code = paths[0] }).Entities.Single();
                if (project != null)
                {
                    if (paths.Count > 1)
                    {
                        var projectItem = Utility.CMS.ObjectEntity<UMC.Data.Entities.ProjectItem>()
                            .Where.And().Equal(new ProjectItem { Code = paths[1], project_id = project.Id }).Entities.Single();

                        if (paths.Count == 3 && projectItem != null)
                        {
                            var sub = Utility.CMS.ObjectEntity<Subject>().Where.And().Equal(new Subject
                            {
                                project_id = project.Id,
                                project_item_id = projectItem.Id,
                                Code = paths[2]
                            }).Entities.Single();
                            if (sub != null)
                            {
                                this.Context.Response.Redirect("Subject", "UIData", sub.Id.ToString());
                            }
                        }
                    }
                    this.Context.Response.Redirect("Subject", "ProjectUI", project.Id.ToString());

                }

            }

        }
        public override void ProcessActivity(WebRequest request, WebResponse response)
        {

            var svalue = this.AsyncDialog("Url", d =>
            {
                this.Context.Send("Scanning", true);
                return this.DialogValue("none");
            });


            if (svalue.StartsWith("http://") || svalue.StartsWith("https://"))
            {
                this.Scanning(new Uri(svalue));
                this.Context.Send("OpenUrl", new UMC.Web.WebMeta().Put("value", svalue), true);
            }
            else
            {
                this.Prompt("此扫码未处理");

            }
        }
    }

}
