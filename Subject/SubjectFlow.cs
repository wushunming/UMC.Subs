using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UMC.Web;


namespace UMC.Subs.Activities
{
    [Mapping("Subject", Desc = "天天录模块")]
    public class SubjectFlow : WebFlow
    {
        public static WebActivity Activity(String cmd)
        {

            switch (cmd)
            {
                case "Export":
                    return new SubjectExportActivity();
                case "Login":
                    return new SubjectLoginActivity();
                case "Dingtalk":
                    return new SubjectDingtalkActivity();
                case "DDRobot":
                    return new SubjectDingtalkRobotActivity();
                case "SelfUI":
                    return new SubjectSelfUIActivity();
                case "Upload":
                    return new SubjectUploadActivity();
                case "Member":
                    return new SubjectMemberActivity();
                case "Publish":
                    return new SubjectPublishActivity();
                case "View":
                    return new SubjectViewActivity();
                case "CaseCMS":
                    return new SubjectCaseCMSActivity();
                case "Account":
                    return new SubjectAccountActivity();
                case "Follow":
                    return new SubjectFollowActivity();
                case "Dynamic":
                    return new SubjectDynamicActivity();
                case "Code":
                    return new SubjectCodeActivity();
                case "Toc":
                    return new SubjectSubsActivity();
                case "TipOff":
                    return new SubjectTipOffActivity();
                case "Menu":
                    return new SubjectMenuActivity();
                case "ProjectItem":
                    return new SubjectProjectItemActivity();
                case "Project":
                    return new SubjectProjectActivity();
                case "Team":
                    return new SubjectTeamActivity();
                case "Sequence":
                    return new SubjectSequenceActivity();
                case "UISetting":
                    return new SubjectUISettingActivity();
                case "PortfolioChange":
                    return new SubjectPortfolioChangeActivity();
                case "PortfolioDel":
                    return new SubjectPortfolioDelActivity();
                case "PortfolioSeq":
                    return new SubjectPortfolioSeqActivity();
                case "News":
                    return new SubjectNewsActivity();
                case "PortfolioSub":
                    return new SubjectPortfolioSubActivity();
                case "Portfolio":
                    return new SubjectPortfolioActivity();
                case "Markdown":
                    return new SubjectMarkdownActivity();
                case "Image":
                    return new SubjectImageActivity();
                case "WebPage":
                    return new SubjectWebPageActivity();
                case "ProjectItemSeq":
                    return new SubjectProjectItemSeqActivity();
                case "Self":
                    return new SubjectSelfActivity();
                case "Content":
                    return new SubjectContentActivity();
                case "EditUI":
                    return new SubjectEditUIActivity();
                case "Save":
                    return new SubjectSaveActivity();
                case "Parse":
                    return new SubjectParseActivity();
                case "Best":
                    return new SubjectBestActivity();
                case "UIData":
                    return new SubjectUIDataActivity();
                case "UI":
                    return new SubjectUIActivity();
                case "UIMin":
                    return new SubjectUIMinActivity();
                case "Section":
                    return new SubjectSectionsActivity();
                case "ProjectAtten":
                    return new SubjectProjectAttenActivity();
                case "Attention":
                    return new SubjectAttentionActivity();
                case "Data":
                    return new SubjectDataActivity();
                case "Share":
                    return new SubjectShareActivity();
                case "Delete":
                    return new SubjectDeleteActivity();
                case "Search":
                    return new SubjectSearchActivity();
                case "Recycle":
                    return new SubjectRecycleActivity();
                case "Comments":
                    return new SubjectCommentsActivity();
                case "Comment":
                    return new SubjectCommentActivity();
                case "Effective":
                    return new SubjectEffectiveActivity();
                case "Submit":
                    return new SubjectSubmitActivity();
                case "Status":
                    return new SubjectStatusActivity();
                case "Spread":
                    return new SubjectSpreadActivity();
                case "Rows":
                    return new SubjectRowsActivity();
                case "WeiXin":
                    return new SubjectWeiXinActivity();
                case "ProjectUI":
                    return new SubjectProjectUIActivity();
                case "Keyword":
                    return new SubjectKeywordActivity();

            }
            return WebActivity.Empty;
        }
        public override WebActivity GetFirstActivity()
        {
            return Activity(this.Context.Request.Command);


        }
        public override WebActivity GetNextActivity(string ActivityHeader)
        {

            return WebActivity.Empty;
        }
    }
}