using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Business
{
    public class HttpResult
    {
        /// <summary>
        /// 当前CookieContainer对象
        /// </summary>
        public CookieContainer CookieContainer { get; set; }
        /// <summary>
        /// 当前cookie的字符串
        /// </summary>
        public string Cookie {
            get
            {
                var cookiestr = string.Empty;
                var table = (Hashtable)CookieContainer.GetType().InvokeMember("m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, CookieContainer, new object[] { });
                foreach (var key in table.Keys)
                {
                    cookiestr += "[" + key + "]\r\n";
                    var pathlist = table[key];
                    var lstCookieCol = (SortedList)pathlist.GetType().InvokeMember("m_list", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance, null, pathlist, new object[] { });
                    cookiestr = lstCookieCol.Values.Cast<CookieCollection>().Aggregate(cookiestr, (current1, colCookies) => colCookies.Cast<Cookie>().Aggregate(current1, (current, c) => current + (c + "\r\n")));
                    cookiestr += "\r\n\r\n";
                }
                return cookiestr;
            } }
        /// <summary>
        /// header对象
        /// </summary>
        public WebHeaderCollection Header { get; set; }
        /// <summary>
        /// 返回的String类型数据 
        /// </summary>
        public string Html { get; set; }
        /// <summary>
        /// 返回的Byte数组
        /// </summary>
        public byte[] ResultByte { get; set; }
        /// <summary>
        /// 返回状态码,默认为OK
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }
        /// <summary>
        /// 返回状态说明
        /// </summary>
        public string StatusDescription { get; set; }
        /// <summary>
        /// 响应结果的URL
        /// </summary>
        public string ResponseUrl { get; set; }
        /// <summary>
        /// 获取重定向的URl
        /// </summary>
        public string RedirectUrl {
            get
            {
                try
                {
                    if (Header == null || Header.Count <= 0) return string.Empty;
                    if (!Header.AllKeys.Any(k => k.ToLower().Contains("location"))) return string.Empty;
                    if (string.IsNullOrEmpty(Header["location"])) return string.Empty;
                    return Header["location"];
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }}
    }

    public class HttpItem
    {
        public HttpItem()
        {
            Encoding = Encoding.UTF8;

            Method = Method.Get;
            Timeout = 15000;
            ReadWriteTimeout = 30000;
            KeepAlive = true;
            Allowautoredirect = false;
            Accept = "*/*";
            ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E)";
            //Expect100Continue = true;
            MaximumAutomaticRedirections = 20;
            PostDataType = PostDataType.String;
            ConnectionLimit = 1024;
            Header = new WebHeaderCollection();
            Header.Add("Pragma", "no-cache");
            Header.Add("Accept-Encoding", "gzip, deflate");
            Header.Add("Accept-Language", "zh-cn");
            Header.Add("x-requested-with", "XMLHttpRequest");
        }

        #region 基础信息
        /// <summary>
        /// 请求URL必须填写
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 获取或设置请求的方法 默认为GET方式
        /// </summary>
        public Method Method { get; set; }
        /// <summary>
        /// 请求超时时间默认15000
        /// </summary>
        public int Timeout { get; set; }
        /// <summary>
        /// 写入Post数据超时间默认30000
        /// </summary>
        public int ReadWriteTimeout { get; set; }
        /// <summary>
        /// 获取或设置一个值，该值指示是否与 Internet 资源建立持久性连接默认为false
        /// </summary>
        public bool KeepAlive { get; set; }
        /// <summary>
        /// 请求标头值 默认为text/html, application/xhtml+xml, */*
        /// </summary>
        public string Accept { get; set; }
        /// <summary>
        /// 请求返回类型 默认application/x-www-form-urlencoded
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// 客户端访问信息默认Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)
        /// </summary>
        public string UserAgent { get; set; }
        /// <summary>
        /// 来源地址，上次访问地址
        /// </summary>
        public string Referer { get; set; }
        /// <summary>
        /// 获取或设置一个值，该值指示请求是否应跟随重定向响应 默认false
        /// </summary>
        public bool Allowautoredirect { get; set; }
        /// <summary>
        /// 获取或设置一个值，该值确定是否使用 100-Continue 行为。如果 POST 请求需要 100-Continue 响应，则为 true；否则为 false。默认值为 true
        /// </summary>
        public bool Expect100Continue { get; set; }
        /// <summary>
        /// 设置请求将跟随的重定向的最大数目 默认20
        /// </summary>
        public int MaximumAutomaticRedirections { get; set; }
        /// <summary>
        /// 最大连接数 默认1024
        /// </summary>
        public int ConnectionLimit { get; set; }
        /// <summary>
        /// 获取或设置编码 返回数据无法识别是使用此编码 POST数据也使用此编码 默认Encoding.UTF8;
        /// </summary>
        public Encoding Encoding { get; set; }
        #endregion

        #region POST数据
        /// <summary>
        /// Post的数据类型 默认PostDataType.String
        /// </summary>
        public PostDataType PostDataType { get; set; }
        /// <summary>
        /// Post请求时要发送的字符串Post数据
        /// </summary>
        public string Postdata { get; set; }
        /// <summary>
        /// Post请求时要发送的Byte类型的Post数据
        /// </summary>
        public byte[] PostdataByte { get; set; }
        #endregion

        #region 证书
        /// <summary>
        /// 证书绝对路径
        /// </summary>
        public string CerPath { get; set; }
        /// <summary>
        /// 证书密码
        /// </summary>
        public string CerPwd { get; set; }
        /// <summary>
        /// 设置509证书集合
        /// </summary>
        public X509CertificateCollection ClentCertificates { get; set; }
        /// <summary>
        /// 获取或设置请求的身份验证信息。
        /// </summary>
        public ICredentials Credentials { get; set; }
        #endregion
        /// <summary>
        /// header对象
        /// </summary>
        public WebHeaderCollection Header { get; set; }
    }
    public class HttpHelper
    {
        /// <summary>
        ///获取网页编码
        /// </summary>
        private readonly string EncondingRegex = "<meta[^<]*charset=([^<]*)[\"']";

        public readonly CookieContainer _container = new CookieContainer();
        private HttpWebRequest _request;
        private HttpWebResponse _response;

        public HttpResult GetHtml(HttpItem item)
        {
            var result = new HttpResult();
            try
            {
                //准备参数
                SetRequest(item);
            }
            catch (Exception ex)
            {
                //配置参数时出错
                return new HttpResult() { CookieContainer = _container, Header = null, Html = ex.Message, StatusDescription = "配置参数时出错：" + ex.Message };
            }
            try
            {
                //请求数据
                using (_response = (HttpWebResponse) _request.GetResponse())
                {
                    GetData(item, result);
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    using (_response = (HttpWebResponse)ex.Response)
                    {
                        GetData(item, result);
                    }
                }
                else
                {
                    result.Html = ex.Message;
                }
            }
            catch (Exception ex)
            {
                result.Html = ex.Message;
            }

            return result;
        }
        private void GetData(HttpItem item, HttpResult result)
        {
            if (_response == null) return;
            //获取StatusCode
            result.StatusCode = _response.StatusCode;
            //获取最后访问的URl
            result.ResponseUrl = _response.ResponseUri.ToString();
            //获取StatusDescription
            result.StatusDescription = _response.StatusDescription;

            //获取Headers
            result.Header = _response.Headers;
            //cookie
            result.CookieContainer = _container;

            //处理网页Byte
            result.ResultByte = GetByte();

            if (result.ResultByte != null && result.ResultByte.Length > 0)
            {
                var encoding = GetEncoding(item, result, result.ResultByte);
                //得到返回的HTML
                result.Html = encoding.GetString(result.ResultByte);
            }
            else
            {
                //没有返回任何Html代码
                result.Html = string.Empty;
            }
        }

        /// <summary>
        /// 提取网页Byte
        /// </summary>
        /// <returns></returns>
        private byte[] GetByte()
        {
            byte[] responseByte;
            using (var stream = new MemoryStream())
            {
                var temp = _response.GetResponseStream();
                if (temp == null) return new List<byte>().ToArray();

                //GZIIP处理
                if (_response.ContentEncoding.Equals("gzip", StringComparison.InvariantCultureIgnoreCase))
                {
                    //开始读取流并设置编码方式
                    new GZipStream(temp, CompressionMode.Decompress).CopyTo(stream, 10240);
                }
                else
                {
                    //开始读取流并设置编码方式
                    temp.CopyTo(stream, 10240);
                }
                //获取Byte
                responseByte = stream.ToArray();
            }
            return responseByte;
        }

        private void SetRequest(HttpItem item)
        {
            //这一句一定要写在创建连接的前面。使用回调的方法进行证书验证。
            ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            //初始化对像，并设置请求的URL地址
            _request = (HttpWebRequest)WebRequest.Create(item.Url);

            //验证证书
            SetCer(item);
            SetCerList(item);
            //设置Header参数
            SetHead(item);
            _request.ServicePoint.Expect100Continue = item.Expect100Continue;
            _request.Timeout = item.Timeout;
            _request.ReadWriteTimeout = item.ReadWriteTimeout;
            //设置安全凭证
            _request.Credentials = item.Credentials;
            //最大重定向
            _request.MaximumAutomaticRedirections = item.MaximumAutomaticRedirections;
            //最大连接
            _request.ServicePoint.ConnectionLimit = item.ConnectionLimit;

            //Accept
            _request.Accept = item.Accept;
            //ContentType返回类型
            _request.ContentType = item.ContentType;
            //UserAgent客户端的访问类型，包括浏览器版本和操作系统信息
            _request.UserAgent = item.UserAgent;
            //来源地址
            _request.Referer = item.Referer;
            //是否保持连接
            _request.KeepAlive = item.KeepAlive;
            //是否执行跳转功能
            _request.AllowAutoRedirect = item.Allowautoredirect;


            //请求方式Get或者Post
            _request.Method = item.Method.ToString();
            //cookie
            _request.CookieContainer = _container;
            //post数据
            SetPostData(item);
        }

        #region post和head
        private void SetPostData(HttpItem item)
        {
            if (item.Method != Method.Post) return;

            byte[] buffer = null;
            //写入Byte类型
            if (item.PostDataType == PostDataType.Byte && item.PostdataByte != null && item.PostdataByte.Length > 0)
            {
                //验证在得到结果时是否有传入数据
                buffer = item.PostdataByte;
            }
            //写入文件
            else if (item.PostDataType == PostDataType.FilePath && !string.IsNullOrWhiteSpace(item.Postdata))
            {
                var r = new StreamReader(item.Postdata, item.Encoding);
                buffer = item.Encoding.GetBytes(r.ReadToEnd());
                r.Close();
            }
            //写入字符串
            else if (!string.IsNullOrWhiteSpace(item.Postdata))
            {
                buffer = item.Encoding.GetBytes(item.Postdata);
            }

            if (buffer == null) return;

            _request.ContentLength = buffer.Length;
            _request.GetRequestStream().Write(buffer, 0, buffer.Length);
        }

        private void SetHead(HttpItem item)
        {
            if (item.Header == null || item.Header.Count <= 0) return;

            foreach (var key in item.Header.AllKeys)
            {
                _request.Headers.Add(key, item.Header[key]);
            }
        }
        #endregion

        #region 证书
        /// <summary>
        /// 设置证书
        /// </summary>
        /// <param name="item"></param>
        private void SetCer(HttpItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.CerPath))
            {
                //将证书添加到请求里
                _request.ClientCertificates.Add(!string.IsNullOrWhiteSpace(item.CerPwd) ? new X509Certificate(item.CerPath, item.CerPwd) : new X509Certificate(item.CerPath));
            }
        }
        /// <summary>
        /// 设置多个证书
        /// </summary>
        /// <param name="item"></param>
        private void SetCerList(HttpItem item)
        {
            if (item.ClentCertificates == null || item.ClentCertificates.Count <= 0) return;

            foreach (var c in item.ClentCertificates)
            {
                _request.ClientCertificates.Add(c);
            }
        }

        /// <summary>
        /// 回调验证证书问题
        /// </summary>
        /// <param name="sender">流对象</param>
        /// <param name="certificate">证书</param>
        /// <param name="chain">X509Chain</param>
        /// <param name="errors">SslPolicyErrors</param>
        /// <returns>bool</returns>
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) { return true; }
        #endregion

        #region Encoding
        private Encoding GetEncoding(HttpItem item, HttpResult result, byte[] responseByte)
        {
            var meta = Regex.Match(Encoding.Default.GetString(responseByte), EncondingRegex, RegexOptions.IgnoreCase);
            var c = string.Empty;
            if (meta.Groups.Count > 0)
            {
                c = meta.Groups[1].Value.ToLower().Trim();
            }
            if (c.Length > 2)
            {
                try
                {
                    return Encoding.GetEncoding(c.Replace("\"", string.Empty).Replace("'", "").Replace(";", "").Replace("iso-8859-1", "gbk").Trim());
                }
                catch
                {
                    return string.IsNullOrEmpty(_response.CharacterSet) ? Encoding.UTF8 : Encoding.GetEncoding(_response.CharacterSet);
                }
            }
            else
            {
                return string.IsNullOrEmpty(_response.CharacterSet) ? Encoding.UTF8 : Encoding.GetEncoding(_response.CharacterSet);
            }
        }
        #endregion

        #region url编码
        public static string UrlEncode(string s)
        {
            return HttpUtility.UrlEncode(s, Encoding.UTF8);
        }
        #endregion
    }

    public enum Method
    {
        Get,
        Post
    }
    public enum PostDataType
    {
        /// <summary>
        /// 字符串类型，这时编码Encoding可不设置
        /// </summary>
        String,
        /// <summary>
        /// Byte类型，需要设置PostdataByte参数的值编码Encoding可设置为空
        /// </summary>
        Byte,
        /// <summary>
        /// 传文件，Postdata必须设置为文件的绝对路径，必须设置Encoding的值
        /// </summary>
        FilePath
    }
}
