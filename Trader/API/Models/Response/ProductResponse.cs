using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using BinanceExchange.API.Converter;
using BinanceExchange.API.Models.Response.Interfaces;

namespace BinanceExchange.API.Models.Response
{
    [DataContract]
    public class ProductResponse : IResponse
    {
        [DataMember(Order = 1)]
        public string code { get; set; }

        [DataMember(Order = 2)]
        public string message { get; set; }

        [DataMember(Order = 3)]
        public string messageDetail { get; set; }

        [DataMember(Order = 4)]
        public List<Data> Data { get; set; }
    }

    public class Data
    {
        public string s { get; set; }
        public string st { get; set; }
        public string b { get; set; }
        public string q { get; set; }
        public string ba { get; set; }
        public string qa { get; set; }
        public decimal? i { get; set; }
        public decimal? ts { get; set; }
        public string an { get; set; }
        public string qn { get; set; }
        public decimal? o { get; set; }
        public decimal? h { get; set; }
        public decimal? l { get; set; }
        public decimal?  c { get; set; }
        public decimal? v { get; set; }
        public decimal? qv { get; set; }
        public decimal? y { get; set; }
        public decimal? cs { get; set; }
    }
}
