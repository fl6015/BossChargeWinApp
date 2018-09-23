using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Model;
using Business;
using Newtonsoft.Json;

namespace BossChargeWinApp
{
    public partial class MainForm : Form
    {
        private bool appStarted;
        private HttpHelper httpHelper;

        //private readonly Regex _payType = new Regex("<input type=\"hidden\" name=\"PayType\" value=\"(?<value>[\\S\\s]+?)\"/>");
        private readonly Regex _SIInfo = new Regex("packet.data.add(\"__SI__(?<name>[\\S\\s]+?)\", \"(?<value>[\\S\\s]+?)\");");
        private readonly Regex _contactId = new Regex("<input type=\"hidden\"  name=\"contactId\" id=\"contactId\" value=\"(?<value>[\\S\\s]+?)\"/>");
        private readonly Regex _encrystring = new Regex("<input type=\"hidden\"  name=\"encrystring\" value=\"(?<value>[\\S\\s]+?)\" />");

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            AddLog("程序启动...");
            httpHelper = new HttpHelper();
            LoadCaptchar();
        }

        private void LoadCaptchar()
        {
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                var httpResult = httpHelper.GetHtml(new HttpItem
                {
                    Url = "http://10.109.209.100:9081/uac/jsp/login/identifyingcode!getIdentifyingCode.action?data=" + DateTime.UtcNow,
                    Method = Method.Get,
                });
                if (httpResult.ResultByte != null)
                {
                    Image imgCaptchar = Image.FromStream(new MemoryStream(httpResult.ResultByte));
                    ShowCaptchar(imgCaptchar);
                }
                
            });
        }

        private void ShowCaptchar(Image imgCaptchar)
        {
            Invoke(new Action(() =>
            {
                picCaptchar.Image = imgCaptchar;
            }));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            AddLog("开始执行...");
            appStarted = true;
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            ThreadPool.QueueUserWorkItem(new WaitCallback(DoChargeWork), 10);
        }

        private void DoChargeWork(object count)
        {
            int rowCount = (int) count;
            while (appStarted)
            {
                var orders = OrderManage.GetOrders(rowCount);
                if (orders != null)
                {
                    foreach (var order in orders)
                    {
                        if (order == null || HaveDebt(order.MobileNum))
                        {
                            AddLog(string.Format("订单异常或者当前欠费,订单号:{0}", order?.OrderId.ToString() ?? string.Empty));
                        }
                        else
                        {
                            CommitChargeInfo(order);
                        }
                    }
                    break;
                }

            }
            AddLog("停止执行...");
        }

        private void DoPreWork()
        {
            AddLog("登录成功系统初始化操作进行中...");

            var httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.109.209.100:9081/uac/web3/jsp/login/login3!isLegalNotices.action",
                Method = Method.Post,
            });
            LogHelper._Info("login3!isLegalNotices.action:" + httpResult.Html);

            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.109.209.100:9081/uac/web3/jsp/login/login3!isFirstLogin.action",
                Method = Method.Get,
            });
            LogHelper._Info("login3!isFirstLogin.action:" + httpResult.Html);

            httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.109.209.100:9081/uac/web3/jsp/frame/main.jsp",
                Method = Method.Get,
            });
            //LogHelper._Info("login3!isFirstLogin.action:" + httpResult.Html);

            //http://10.109.209.100:9081/uac/web3/jsp/resource/app/appRes3!getSsoUrlList.action

            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.109.209.100:9081/uac/web3/jsp/goldbank/goldbank3!checkLoginBankMode.action?id=" + DateTime.UtcNow,
                Method = Method.Post,
                Postdata = "isInner=false&requestInfo.busyType=1&requestInfo.systemId=200360&requestInfo.resAcctId=2001413079",
            });
            LogHelper._Info("goldbank3!checkLoginBankMode.action:" + httpResult.Html);

            var loginBankMode = JsonConvert.DeserializeObject<LoginBankMode>(httpResult.Html);

            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.109.209.100:9081/uac/web3/jsp/resource/app/appRes3!accessAppRes.action",
                Method = Method.Post,
                Postdata = "appAcctId=rae3N0003&acctSeq=2001413079&appCode=SCNGCRM&appAcctName=%E5%91%A8%E7%86%99&appId=200360&url=http%25253A%25252F%25252F10.112.104.72%25253A11001%25252Fnpage%25252Flogin_common%25252Findex_4a.jsp&svnSn=&bankFlag=&bankToken=16%7C37%7C76%7C-14%7C-48%7C119%7C72%7C68%7C-38%7C4%7C-72%7C41%7C38%7C16%7C-61%7C15%7C126",
            });
            LogHelper._Info("appRes3!accessAppRes.action:" + httpResult.Html);

            var newUrl = httpResult.Html;

            //httpHelper = new HttpHelper();
            httpHelper.GetHtml(new HttpItem
            {
                Url = newUrl,
                Method = Method.Get,
            });
            LogHelper._Info(newUrl + ":" + httpResult.Html);

            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/login/main.jsp?op_track_mfrm_time=" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Method = Method.Get,
            });
            LogHelper._Info("main.jsp:" + httpResult.Html);

            AddLog("登录成功系统初始化操作完成...");

        }

        private bool HaveDebt(string mobileNum)
        {
            var httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/dispatch/send_boss.jsp?version=1&authen_code=&authen_name=&opCode=8145&opName=&crmActiveOpCode=8145&activeCustId=28011009761770&activePhone=0&activeIdNo=0&contactId=1118062300000002110589925&activeBrandId=0&activeMasterServId=0&activeProdId=0&activeProdName=0&activeBrandName=0&activeMasterServName=0&currentIdNo=0&currentSessionId=28011009761770",
                Method = Method.Get,
            });
            LogHelper._Info("send_boss.jsp:" + httpResult.Html);

            //Regex expression = new Regex(@"%download%#(?<Identifier>[0-9]*)");
            var postInfo = "encrystring=0f3d393b5cda1cd2aa9b75c17cfc4ddf8a8f916a289c71f2e90c7988f4ef1f6ed300e32db15addf40c7870c63598166be0732abc27259c1ac76587d091b5754bf336f83ab9a0fe1ff9502271c634e6ec6f15f6d2604ca19c8fb37fcd12707cec362553b3d2620b67ab80c67192fb9b0466668f61aca09e625a79943b42fc0f3ad79334738b01f6fac80010ecda529bf3cd741e0af6906d46&OUT_SYS_ID=25&login_no=rae3N0003&login_name=%D6%DC%CE%F5&ip_address=10.112.50.59&login_password=6d5fdfaa29ae5cb0&staff_id=10244251&dept_code=NULL&group_id=1458575&org_group=NULL&region_id=28&class_code=4800&group_name=%5BSA%5D%C4%CF%B3%E4%CB%B3%C7%EC%B7%D6%B9%AB%CB%BE%CB%C4%B4%A8%CC%EC%B4%B4%F0%A9%BF%C6%BF%C6%BC%BC%D3%D0%CF%DE%B9%AB%CB%BE&power_right=12&position_code=0%7C&param_value=OPEN&contact_phone=18281710971&region_class_value=N&activePhone=0&id_type=&id_iccid=&themePath=UI&hotkey=Y&rightkey=Y&chgcolor=N&opCoce=8145&opName=%CD%A3%BB%FA%B4%DF%BD%C9%D0%C5%CF%A2%B2%E9%D1%AF&login_type=0&mode_code_1104=&regionCode_1104=&beginDate_1104=&endDate_1104=&rptInfo=994&favInfo=%7BB18%3D14010%2C+B66%3D14010%2C+B61%3D14010%2C+B64%3D14010%2C+B62%3D14010%2C+B65%3D14010%2C+B63%3D14010%2C+99%3D14010%2C+102%3D14010%2C+101%3D14010%2C+B68%3D14010%2C+100%3D14010%7D&busiInfo=%7Ba326%3D16055%2C+1514d%3D424321%2C+1514c%3D403321%2C+a821%3D331288%2C+a498%3D16000%2C+a525%3D16025%2C+2000%3D16006%7D&boss_auth_code=null&boss_auth_name=null&module_list=%7B1436%3D-NNnull%2C+5738%3D-NNnull%2C+5756%3D-NYnull%2C+1270%3D-NNnull%2C+1147%3D-YYnull%2C+2354%3D-NYnull%2C+1106%3D-NYnull%2C+3845%3D-NYnull%2C+1095%3D-NNnull%2C+1104%3D-YYnull%2C+1861%3D-NYnull%2C+8123%3D-NNnull%2C+3896%3D-NYnull%2C+4615%3D-NYnull%2C+9755%3D-Nnull%2C+4961%3D-NYnull%2C+1627%3D-NYnull%2C+8148%3D-NNnull%2C+2228%3D-NNnull%2C+8145%3D-NNnull%2C+4580%3D-NNnull%2C+4983%3D-NNnull%2C+3880%3D-NYnull%2C+1052%3D-NYnull%2C+8143%3D-NNnull%2C+1646%3D-NNnull%2C+2018%3D-NNnull%2C+917E%3D-NNnull%2C+1879%3D-NNnull%2C+8507%3D-Nnull%2C+3851%3D-NYnull%2C+2802%3D-NYnull%2C+912A%3D-NNnull%2C+4487%3D-NNnull%2C+1514%3D-NNnull%2C+8000%3D-NNnull%2C+8107%3D-NNnull%2C+4975%3D-NYnull%2C+3915%3D-NNnull%2C+1120%3D-NYnull%2C+1531%3D-NNnull%2C+6429%3D-Nnull%2C+2984%3D-NNnull%2C+6428%3D-Nnull%2C+2389%3D-NYnull%2C+1163%3D-NYnull%2C+1000%3D-YNnull%2C+J616%3D-YYnull%7D&boss_op_code=8145";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.103.38:12002/npage/dispatch/chk.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("chk.jsp:" + httpResult.Html);

            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.103.38:12002/npage/s8145/f8145.jsp?contact_id=1118062300000002110589925&opCode=8145&opName=&crmActiveOpCode=8145",
                Method = Method.Get,
            });
            LogHelper._Info("f8145.jsp:" + httpResult.Html);

            //("__SI__[0-9]*", "[0-9]*")
            Regex expression = new Regex("(\"__SI__(?<num1>[0-9]*)\", \"(?<num2>[0-9]*)\")");
            string num1 = string.Empty, num2 = string.Empty;
            var results = expression.Matches(httpResult.Html);
            foreach (Match match in results)
            {
                num1 = match.Groups["num1"].Value;
                num2 = match.Groups["num2"].Value;
                break;
            }

            string curDate = DateTime.Today.ToString("yyyyMM");
            postInfo = "crmActiveCustId=undefined&crmActiveIdNo=undefined&crmActiveOpCode=8145" +
                "&crmActiveSessionId=undefined&beginDate=" + curDate + "&endDate=" + curDate + "&phoneNo=" +
                mobileNum + "&contractNo=&userFlag=0&__SI__" + num1 + "=" + num2 + "&checkOp=8142";
            LogHelper._Info("f8145_ajax_info.jsp-postInfo:" + postInfo);
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.103.38:12002/npage/s8145/f8145_ajax_info.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("f8145_ajax_info.jsp:" + httpResult.Html);

            //<td> \d*\.\d{2} </td>
            expression = new Regex("<td> (?<money>\\d*\\.\\d{2}) </td>");
            results = expression.Matches(httpResult.Html);
            LogHelper._Info("f8145_ajax_info.jsp-results.count:" + results.Count);
            int targetIndex = results.Count - 11; //要找的目标
            if (targetIndex <= 0)
                return false;

            int curIndex = 0;
            foreach (Match match in results)
            {
                curIndex++;
                if (curIndex == targetIndex)
                {
                    if (match.Groups["money"].Value != "0.00")
                        return true;

                }
            }

            return false;

        }

        private void CommitChargeInfo(OrderInfo order)
        {
            //先关闭上一个系统
            var postInfo = "custid=28211009815542";
            var httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/login/ajax_destroy.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("ajax_destroy.jsp:" + httpResult.Html);

            //
            string getUrl = "http://10.112.104.72:11001/npage/login/ajax_verify.jsp?interfaceCode=11&validateFlag=N&iconFlag=false&signUser=N&cust_id=&opcode=1646&" +
                            "g_activateTab=index&title=&targetUrl=s1646/f1646_main.jsp?version=0&grantNo=&op_track_time=" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = getUrl,
                Method = Method.Get,
            });
            LogHelper._Info("ajax_verify.jsp:" + httpResult.Html);

            //从cookie中获取DemixRandomNumParam
            string demixRandomNumParam = string.Empty;
            var cookies = httpHelper._container.GetCookies(new Uri("http://10.112.104.72:11001"));
            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name == "CSRF_TOKEN")
                {
                    demixRandomNumParam = cookie.Value;
                    break;
                }
            }

            //
            postInfo = "crmActiveCustId=undefined&crmActiveIdNo=undefined&crmActiveOpCode=null&crmActiveSessionId=undefined&" +
                       "DemixRandomNumParam=" + demixRandomNumParam + "&phoneNo=28411013289714&loginType=16&idNo=-1&qryType=cust_user";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/login/ajax_getcustid.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("ajax_getcustid.jsp:" + httpResult.Html);

            //
            postInfo = "crmActiveCustId=undefined&crmActiveIdNo=undefined&crmActiveOpCode=null&crmActiveSessionId=undefined&" +
                       "DemixRandomNumParam=" + demixRandomNumParam + "&custId%5B0%5D=28011009761770&custId=28011009761770&phoneNo=28411013289714&loginType=16&idNo=-1";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/login/ajax_qryUserProd.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("ajax_qryUserProd.jsp:" + httpResult.Html);

            //
            getUrl = "http://10.112.104.72:11001/npage/login/childTab.jsp?gCustId=28011009761770&loginType=16&phone_no=28411013289714&signUser=Y&idNo=-1&isMarket=true&iconFlag=false";
            string childTabUrl = getUrl;
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = getUrl,
                Method = Method.Get,
            });
            LogHelper._Info("childTab.jsp:" + httpResult.Html);

            //contactId=1118062700000002116292316&
            getUrl = "http://10.112.104.72:11001/npage/portal/cust/portal.jsp?gCustId=28011009761770&loginType=16&" +
                            "phone_no=28411013289714&contactId=-1&idNo=-1&opType=null&signUser=Y&unsetFrom=null&" +
                            "isMarket=true&function_code=&noShowUserFlag=N";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = getUrl,
                Method = Method.Get,
            });
            LogHelper._Info("portal.jsp:" + httpResult.Html);

            var expression = new Regex("contactId=(?<contactId>\\d{25})");
            var contactId = expression.Match(httpResult.Html).Groups["contactId"].Value;

            //
            postInfo = "crmActiveCustId=undefined&crmActiveIdNo=undefined&crmActiveOpCode=undefined&crmActiveSessionId=undefined&" +
                       "DemixRandomNumParam="+ demixRandomNumParam +"&contact_id="+ contactId +"&gCustId=28011009761770&loginType=16&opType=LOGIN&phone_no=28411013289714&signUser=Y";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/portal/cust/ajax_getCustInfo.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("ajax_getCustInfo.jsp:" + httpResult.Html);

            //
            postInfo = "crmActiveCustId=undefined&crmActiveIdNo=undefined&crmActiveOpCode=undefined&crmActiveSessionId=undefined&" +
                       "DemixRandomNumParam="+ demixRandomNumParam + "&contactId="+ contactId +"&cust_id=28011009761770";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/portal/cust/ajax_getCar.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("ajax_getCar.jsp:" + httpResult.Html);

            //
            postInfo = "crmActiveCustId=undefined&crmActiveIdNo=undefined&crmActiveOpCode=undefined&crmActiveSessionId=undefined&" +
                       "DemixRandomNumParam=" + demixRandomNumParam + "&gCustId=28011009761770&contact_id=" + contactId + "&" +
                       "phone_no=28411013289714&idNo=-1&loginType=16&signUser=Y";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/portal/cust/ajax_qryUserProd.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("ajax_qryUserProd.jsp:" + httpResult.Html);

            //
            postInfo = "crmActiveCustId=undefined&crmActiveIdNo=undefined&crmActiveOpCode=undefined&crmActiveSessionId=undefined&" +
                       "DemixRandomNumParam=" + demixRandomNumParam + "&svc_name=dgetstabyid&code_value=0&begin_value=1646";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/common/f_ajax_dynsrv_another.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("f_ajax_dynsrv_another.jsp:" + httpResult.Html);

            //
            getUrl = "http://10.112.104.72:11001/npage/s1646/f1646_main.jsp?version=0&authen_code=&authen_name=&opCode=1646&opName=集团付费计划变更&" +
                            "crmActiveOpCode=1646&activeCustId=28011009761770&activePhone=0&activeIdNo=0&contactId="+ contactId +
                            "&activeBrandId=0&activeMasterServId=0&activeProdId=0&activeProdName=0&activeBrandName=0&activeMasterServName=0&" +
                            "currentIdNo=0&currentSessionId=28011009761770";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = getUrl,
                Method = Method.Get,
                Referer = childTabUrl,
            });
            LogHelper._Info("f1646_main.jsp:" + httpResult.Html);

            //
            if (httpResult.Html.IndexOf("rtnCode:999999,") > -1) //
            {
                AddLog("权限验证失败:" + httpResult.Html);
                return;
            }

            //
            postInfo = "crmActiveCustId=undefined&crmActiveIdNo=undefined&crmActiveOpCode=null&crmActiveSessionId=undefined&" +
                       "DemixRandomNumParam="+ demixRandomNumParam + "&OP_CODE=1646&OPR_FLAG=PASSWORD&CONTRACT_NO=28411013289714&PASSWORD=112233&cust_id=28011009761770&id_no=0";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/s1517/f1517_ajax_checkPwd.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("f1517_ajax_checkPwd.jsp:" + httpResult.Html);

            //
            postInfo = "crmActiveCustId=undefined&crmActiveIdNo=undefined&crmActiveOpCode=null&crmActiveSessionId=undefined&" +
                       "DemixRandomNumParam="+ demixRandomNumParam + "&op_mode=0&op_code=1646&id_no=0&phone_no="+ order.MobileNum +"&cust_id=28011009761770";
            LogHelper._Info("f1646_ajax_memInfo.jsp-postInfo:" + postInfo);
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/s1646/f1646_ajax_memInfo.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("f1646_ajax_memInfo.jsp:" + httpResult.Html);

            //var expression = new Regex("<td> (?<money>\\d*\\.\\d{2}) </td>");
            expression = new Regex("v_id_no=\"(?<vid>\\d{14})\"");
            var vid = expression.Match(httpResult.Html).Groups["vid"].Value;

            //
            postInfo = "crmActiveCustId=28011009761770&crmActiveIdNo=0&crmActiveOpCode=1646&crmActiveSessionId=28011009761770&" +
                       "DemixRandomNumParam="+ demixRandomNumParam +"&phone_no=&functionName=goldBankCfm&valid_type=9&op_code=1646&op_code_4a=n1646&" +
                       "op_name=%E9%9B%86%E5%9B%A2%E4%BB%98%E8%B4%B9%E8%AE%A1%E5%88%92%E5%8F%98%E6%9B%B4";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/login/ajax_publicGoldBankChk.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("ajax_publicGoldBankChk.jsp:" + httpResult.Html);

            //
            postInfo = "operCode=8%7C-19%7C-16%7C115%7C-59%7C-96%7C65%7C-32%7C-69&mainLoginName=&" +
                       "subLoginName=16%7C94%7C77%7C-8%7C85%7C-11%7C1%7C-43%7C78%7C-84%7C109%7C73%7C-81%7C44%7C-83%7C-44%7C-123&appCode=SCNGCRM&" +
                       "operContent=%25E6%2593%258D%25E4%25BD%259C%25E5%2591%2598rae3N0003%25E8%25AF%25B7%25E6%25B1%2582%25E8%25AE%25BF%25E9%2597%2" +
                       "5AE%255B1646%255D%25E9%259B%2586%25E5%259B%25A2%25E4%25BB%2598%25E8%25B4%25B9%25E8%25AE%25A1%25E5%2588%2592%25E5%258F%2598%2" +
                       "5E6%259B%25B4%25E6%25A8%25A1%25E5%259D%2597&svcNum=";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.109.209.100:9081/uac/web3/jsp/goldbank/goldbank3!goldBankIframeAction.action",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("goldbank3!goldBankIframeAction.action:" + httpResult.Html);

            // 
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.109.209.100:9081/uac/web3/jsp/goldbank/goldbank3!checkApprover.action?apMainAcctId=10000650",
                Method = Method.Post,
                Postdata = string.Empty,
            });
            LogHelper._Info("goldbank3!checkApprover.action:" + httpResult.Html);

            //success=2389340
            postInfo = "operCode=n1646&appCode=SCNGCRM&userId=rae3N0003&apprType=2&apMainAcctId=10000650&queryType=iframe&" +
                       "applyReason=%E7%94%B3%E8%AF%B7%E5%AE%A1%E6%89%B9%2C%E8%AF%B7%E9%A2%86%E5%AF%BC%E5%AE%A1%E6%89%B9.&expireTime=&" +
                       "requestInfo.isBasedOnLogin=&requestInfo.busyType=&requestInfo.systemId=&requestInfo.policyId=58630&requestInfo.resAcctId=&" +
                       "requestInfo.operateKind=1&setApplyReasonDefault=true&requestInfo.isAppoint=&setApplyMainAcctIdDefault=false&approvalTimePeriodFlag=";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.109.209.100:9081/uac/web3/jsp/goldbank/goldbank3!queryAppr.action?id=" + DateTime.UtcNow.ToUniversalTime(),
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("goldbank3!queryAppr.action:" + httpResult.Html);

            string[] queryResults = httpResult.Html.Split('=');
            if (queryResults.Length != 2 || queryResults[0].Trim() != "success")
            {
                AddLog("发送验证失败:" + httpResult.Html);
                return;
            }
            string opSn = queryResults[1];
            string smsPwd = Microsoft.VisualBasic.Interaction.InputBox("请输入验证密钥", "Prompt", "", 50, 50);

            if (string.IsNullOrEmpty(smsPwd))
            {
                AddLog("验证密钥不能为空");
                return;
            }


            //success-2389340
            postInfo = "passwordStatus=&lockStatus=0&svnSn="+ opSn + "&operCode=n1646&queryType=iframe&apprType=2&appCode=SCNGCRM&userId=rae3N0003&caManufacturer=twcx&" +
                       "requestInfo.isBasedOnLogin=&requestInfo.busyType=&requestInfo.systemId=&requestInfo.resAcctId=&requestInfo.operateKind=1&requestInfo.policyId=58630&" +
                       "requestInfo.svcNum=&dialogMode=&applyType=1&expireTime=&apMainAcctId=10000650&applyReasonList=52733&" +
                       "applyReason=%E7%94%B3%E8%AF%B7%E5%AE%A1%E6%89%B9%2C%E8%AF%B7%E9%A2%86%E5%AF%BC%E5%AE%A1%E6%89%B9.&" +
                       "setApplyReasonDefault=&shortMessageSvnSn="+ opSn +"&shortMessageErrorTimes=0&shortKey=" + smsPwd;
            LogHelper._Info("goldbank3!checkValidate.action-postInfo:" + postInfo);
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.109.209.100:9081/uac/web3/jsp/goldbank/goldbank3!checkValidate.action",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("goldbank3!checkValidate.action:" + httpResult.Html);

            queryResults = httpResult.Html.Split('-');
            if (queryResults.Length != 2 || queryResults[0].Trim() != "success")
            {
                AddLog("验证密钥验证失败:" + httpResult.Html);
                return;
            }

            //1#2389340#24|70|-102|-62|-62|42|32|73|-92|127|-57|80|86|105|48|69|9|14|33|80|-52|42|90|-62|-127
            getUrl = "http://10.109.209.100:9081/uac/web3/jsp/goldbank/goldbank3!getToken4GoldBank.action?appCode=SCNGCRM&opResult=1&" +
                      "apMainAcctId=10000650&opSn=" + opSn + "&otherSn=&_=" + Common.GetTimeStamp();
            LogHelper._Info("goldbank3!getToken4GoldBank.action-getUrl:" + getUrl);
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = getUrl,
                Method = Method.Get,
            });
            LogHelper._Info("goldbank3!getToken4GoldBank.action:" + httpResult.Html);

            string[] strs = httpResult.Html.Split('#');
            if (strs.Length != 3 || strs[0].Trim() != "1")
            {
                AddLog("获取token失败:" + httpResult.Html);
                return;
            }
            string goldToken = httpResult.Html;

            //
            postInfo = "crmActiveCustId=28011009761770&crmActiveIdNo=0&crmActiveOpCode=1646&crmActiveSessionId=28011009761770&" +
                       "DemixRandomNumParam="+ demixRandomNumParam + "&op_code=1646&op_name=%E9%9B%86%E5%9B%A2%E4%BB%98%E8%B4%B9%E8%AE%A1%E5%88%92%E5%8F%98%E6%9B%B4&" +
                       "module_id_4a=n1646&accept_no=&appAccept=2391175&servicID=SCNGCRM&goldFlag=0&bankFlag=1";
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/login/ajax_goldBackChkLog.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("ajax_goldBackChkLog.jsp:" + httpResult.Html);

            //response.data.add("login_accept","7520879995591");
            postInfo = "crmActiveCustId=28011009761770&crmActiveIdNo=0&crmActiveOpCode=1646&crmActiveSessionId=28011009761770&DemixRandomNumParam=" + demixRandomNumParam + "&id_no=0&" +
                       "op_note=&cust_id=28011009761770&contract_no=28411013289714&contractatt_type=17&brand_id=0&op_mode=I&eff_date=2018-06-26&exp_date=2018-07-01&" +
                       "rate_flag=N&pay_type=1&cycle_type=0&chkout_pri=&bill_order=&date_cycle=1&serv_acct_id=&op_code=1646&pay_value=10.00&service_no=0&tbInfo=&" +
                       "phoneStr="+ vid +"%40&serviceOfferId=3900007&" +
                       "goldToken="+ HttpUtility.UrlEncode(goldToken) +"&" +
                       "pay_way=special";
            LogHelper._Info("f1646_ajax_sub.jsp-postInfo:" + postInfo);
            httpResult = httpHelper.GetHtml(new HttpItem
            {
                Url = "http://10.112.104.72:11001/npage/s1646/f1646_ajax_sub.jsp",
                Method = Method.Post,
                Postdata = postInfo,
            });
            LogHelper._Info("f1646_ajax_sub.jsp:" + httpResult.Html);

            expression = new Regex("add(\"login_accept\",\"(?<orderNum>\\d{13})\")");
            var orderNum = expression.Match(httpResult.Html).Groups["orderNum"].Value;
            if (string.IsNullOrEmpty(orderNum))
                AddLog("提交订单失败:" + httpResult.Html);
            else
                AddLog("提交成功:" + orderNum);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (LoginInfoIsValid())
            {
                //ThreadPool.QueueUserWorkItem((obj) =>
                {
                    btnLogin.Text = "登陆中...";
                    btnLogin.Enabled = false;

                    var httpResult = httpHelper.GetHtml(new HttpItem
                    {
                        Url = "http://10.109.209.100:9081/uac/web3/jsp/login/login3!checkSameMainAcctIsAlreadyLogin.action?loginName=" + txtUserName.Text.Trim(),
                        Method = Method.Get,
                    });
                    LogHelper._Info("checkSameMainAcctIsAlreadyLogin.action:" + httpResult.Html);

                    if (httpResult.Html.Trim() != "N") //已经登录，无需后续。
                    {
                        ShowLoginResult(true, "已经登录");
                        return;
                    }
                        

                    var postData =
                        "key=bBWWzK5fzBwe8AwuHAofyPuE!CnF!g%24UHAIf8A8g8A9E7DZPhD5oqSReqfae5gIt8AwW5AIVH1u1hfaehA9F8AwWHAoVH1Zu54afhfafwSwtmRwVqSVO7Cae5u8K5a8KhozKHgItHPIEhAqVHPzg7ETtmB5Vqo3O7DaVH15lDCThhfafwVRezB8!%40DTV8AwuhD5QbD3rqs5f%40B5JqcafwS5rqEQAzB8r%40DtVH1hERVWlweof5h%3D%3D&fingerFlag=1&loginName=" +
                        txtUserName.Text.Trim()
                        + "&loginPassword=" + Common.CreateMD5(txtUserPwd.Text.Trim()).ToLower() + "&identifyingCodeInput=" +
                        txtCaptchar.Text.Trim();

                    LogHelper._Info("login3!passwordCheck.action-parms:" + postData);

                    httpResult = httpHelper.GetHtml(new HttpItem
                    {
                        Url = "http://10.109.209.100:9081/uac/web3/jsp/login/login3!passwordCheck.action",
                        Method = Method.Post,
                        Postdata = postData,
                    });
                    LogHelper._Info("login3!passwordCheck.action:" + httpResult.Html);

                    if (httpResult.Html.Contains("登录名或者密码错误"))
                    {
                        ShowLoginResult(false, "登录名或者密码错误");
                        return;
                    }

                    if (httpResult.Html.Contains("验证码错误"))
                    {
                        ShowLoginResult(false, "验证码错误");
                        return;
                    }

                    httpResult = httpHelper.GetHtml(new HttpItem
                    {
                        Url = "http://10.109.209.100:9081/uac/web3/jsp/login/login3!getSMKey.action",
                        Method = Method.Post,
                        Postdata = "",
                    });
                    LogHelper._Info("login3!getSMKey.action:" + httpResult.Html);

                    //
                    string smsCode = Microsoft.VisualBasic.Interaction.InputBox("请输入您手机收到的验证码", "Prompt", "", 50, 50);

                    if (string.IsNullOrEmpty(smsCode))
                    {
                        ShowLoginResult(false, "验证码不能为空");
                        return;
                    }

                    postData =
                        "key=bBWWzK5fzBwe8AwuHAofyPuE!CnF!g%24UHAIf8A8g8A9E7DZPhD5oqSReqfae5gIt8AwW5AIVH1u1hfaehA9F8AwWHAoVH1Zu54afhfafwSwtmRwVqSVO7Cae5u8K5a8KhozKHgItHPIEhAqVHPzg7ETtmB5Vqo3O7DaVH15lDCThhfafwVRezB8!%40DTV8AwuhD5QbD3rqs5f%40B5JqcafwS5rqEQAzB8r%40DtVH1hERVWlweof5h%3D%3D&fingerFlag=1&smKey=" +
                        smsCode + "&validateSelect=1";
                    httpResult = httpHelper.GetHtml(new HttpItem
                    {
                        Url = "http://10.109.209.100:9081/uac/web3/jsp/login/login3!login.action",
                        Method = Method.Post,
                        Postdata = postData,
                    });
                    LogHelper._Info("login3!login.action:" + httpResult.Html);

                    if (httpResult.Html.Trim() == "Y")
                        ShowLoginResult(true, "登录成功");
                    else
                        ShowLoginResult(false, httpResult.Html);
                }
                //);
            }
        }

        private void ShowLoginResult(bool state, string msg)
        {
            Invoke(new Action(() =>
            {
                MessageBox.Show(state ? "登录完成..." : msg);
                btnLogin.Text = "登录";
                btnLogin.Enabled = true;
                btnStart.Enabled = true;
            }));

            if(state)
                DoPreWork();
        }

        private bool LoginInfoIsValid()
        {
            if (string.IsNullOrEmpty(txtUserName.Text.Trim()))
            {
                MessageBox.Show("请先输入登录名...");
                txtUserName.Focus();
                return false;
            }
            if (string.IsNullOrEmpty(txtUserPwd.Text.Trim()))
            {
                MessageBox.Show("请先输入密码...");
                txtUserPwd.Focus();
                return false;
            }
            if (string.IsNullOrEmpty(txtCaptchar.Text.Trim()))
            {
                MessageBox.Show("请先输入验证码...");
                txtCaptchar.Focus();
                return false;
            }

            return true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            appStarted = false;
            AddLog("停止中...");
        }

        private void AddLog(string msg)
        {
            Invoke(new Action(() =>
            {
                TextLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "]" + msg);
                TextLog.AppendText(Environment.NewLine);
                TextLog.ScrollToCaret();
            }));
        }

    }
}
