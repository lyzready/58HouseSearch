using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using RestSharp;
using System.Data;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using Dapper;
using AngleSharp.Dom;
using HouseMap.Dao;
using HouseMap.Dao.DBEntity;
using HouseMap.Crawler.Common;

using Newtonsoft.Json.Linq;
using HouseMap.Models;
using HouseMap.Common;

namespace HouseMap.Crawler
{

    public class DoubanCrawler : BaseCrawler
    {

        public DoubanCrawler(HouseDapper houseDapper, ConfigDapper configDapper) : base(houseDapper, configDapper)
        {
            this.Source = ConstConfigName.Douban;
        }

        public override string GetJsonOrHTML(JToken config, int page)
        {
            var groupID = config["groupid"]?.ToString();
            var apiURL = $"https://api.douban.com/v2/group/{groupID}/topics?start={page * 50}&count=50";
            return GetAPIResult(apiURL);

        }

        public override List<HouseInfo> ParseHouses(JToken config, string data)
        {
            var houses = new List<HouseInfo>();
            var resultJObject = JsonConvert.DeserializeObject<JObject>(data);
            var cityName = config["cityname"].ToString();
            foreach (var topic in resultJObject["topics"])
            {
                var housePrice = JiebaTools.GetHousePrice(topic["content"].ToString());
                var photos = topic["photos"]?.Select(photo => photo["alt"].ToString()).ToList();
                var house = new HouseInfo()
                {
                    HouseLocation = topic["title"].ToString(),
                    HouseTitle = topic["title"].ToString(),
                    HouseOnlineURL = topic["share_url"].ToString(),
                    HouseText = topic["content"].ToString(),
                    HousePrice = housePrice,
                    IsAnalyzed = true,
                    DisPlayPrice = housePrice > 0 ? $"{housePrice}元" : "",
                    Source = ConstConfigName.Douban,
                    LocationCityName = cityName,
                    Status = 1,
                    PicURLs = JsonConvert.SerializeObject(photos),
                    PubTime = topic["created"].ToObject<DateTime>()
                };
                houses.Add(house);
            }
            return houses;
        }

        private static string GetAPIResult(string apiURL)
        {
            try
            {
                var client = new RestClient(apiURL);
                var request = new RestRequest(Method.GET);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("connection", "keep-alive");
                request.AddHeader("x-requested-with", "XMLHttpRequest");
                request.AddHeader("referer", apiURL);
                request.AddHeader("accept", "*/*");
                request.AddHeader("user-agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 11_0 like Mac OS X) AppleWebKit/604.1.38 (KHTML, like Gecko) Version/11.0 Mobile/15A372 Safari/604.1");
                request.AddHeader("accept-language", "zh-CN,zh;q=0.9,en;q=0.8");
                request.AddHeader("accept-encoding", "gzip, deflate, br");
                request.AddHeader("cookie", "bid=qLvbOle-G58; ps=y; ue=^\\^codelover^@qq.com^^; push_noty_num=0; push_doumail_num=0; __utmz=30149280.1521636704.1.1.utmcsr=(direct)^|utmccn=(direct)^|utmcmd=(none); __utmv=30149280.15460; ll=^\\^108296^^; _vwo_uuid_v2=D87414308A33790472DBB4D2B1DC0DE7B^|6a9fc300e5ea8c7485f9a922d87e820b; __utma=30149280.2046746446.1521636704.1522567163.1522675073.3");
                IRestResponse response = client.Execute(request);
                if (response.IsSuccessful)
                {
                    return response.Content;
                }

            }
            catch (Exception ex)
            {
                LogHelper.Error("GetAPIResult", ex);
            }
            return "";

        }

    }
}