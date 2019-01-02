﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ApiTest.WebStack
{
    /// <summary>
    /// 防止sql注入
    /// </summary>
    public class AntiSqlInjectAttribute : ActionFilterAttribute
    {

        /// <summary>
        /// Action执行前
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(HttpActionContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            var actionParameters = filterContext.ActionDescriptor.GetParameters();

            var actionArguments = filterContext.ActionArguments;

            foreach (var p in actionParameters)
            {
                var value = filterContext.ActionArguments[p.ParameterName];

                var pType = p.ParameterType;

                if (value == null)
                {
                    continue;
                }
                //如果不是值类型或接口，不需要过滤
                if (!pType.IsClass) continue;

                if (value is string)
                {
                    //对string类型过滤
                    filterContext.ActionArguments[p.ParameterName] = AntiSqlInject.Instance.GetSafetySql(value.ToString());
                }
                else
                {
                    //是一个class，对class的属性中，string类型的属性进行过滤
                    var properties = pType.GetProperties();
                    foreach (var pp in properties)
                    {
                        var temp = pp.GetValue(value);
                        if (temp == null)
                        {
                            continue;
                        }
                        pp.SetValue(value, temp is string ? AntiSqlInject.Instance.GetSafetySql(temp.ToString()) : temp);
                    }
                }
            }
        }

        /// <summary>
        /// 防止SQL注入
        /// </summary>
        public class AntiSqlInject
        {
            public static AntiSqlInject Instance = new AntiSqlInject();

            /// <summary>
            /// 初始化过滤方法
            /// </summary>
            static AntiSqlInject()
            {
                SqlKeywordsArray.AddRange(SqlSeparatKeywords.Split('|'));
                SqlKeywordsArray.AddRange(Array.ConvertAll(SqlCommandKeywords.Split('|'), h => h + " "));
                SqlKeywordsArray.AddRange(Array.ConvertAll(SqlCommandKeywords.Split('|'), h => " " + h));
            }

            private const string SqlCommandKeywords = "and|exec|execute|insert|select|delete|update|count|chr|mid|master|" +
                                                      "char|declare|sitename|net user|xp_cmdshell|or|create|drop|table|from|grant|use|group_concat|column_name|" +
                                                      "information_schema.columns|table_schema|union|where|select|delete|update|orderhaving|having|by|count|*|truncate|like";

            private const string SqlSeparatKeywords = "'|;|--|\'|\"|/*|%|#";

            private static readonly List<string> SqlKeywordsArray = new List<string>();

            /// <summary>
            /// 是否安全
            /// </summary>
            /// <param name="input">输入</param>
            /// <returns>返回</returns>
            public bool IsSafetySql(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                {
                    return true;
                }
                input = HttpUtility.UrlDecode(input).ToLower();

                foreach (var sqlKeyword in SqlKeywordsArray)
                {
                    if (input.IndexOf(sqlKeyword, StringComparison.Ordinal) >= 0)
                    {
                        return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// 返回安全字符串
            /// </summary>
            /// <param name="input">输入</param>
            /// <returns>返回</returns>
            public string GetSafetySql(string input)
            {
                if (string.IsNullOrEmpty(input))
                {
                    return string.Empty;
                }
                if (IsSafetySql(input)) { return input; }
                input = HttpUtility.UrlDecode(input).ToLower();

                foreach (var sqlKeyword in SqlKeywordsArray)
                {
                    if (input.IndexOf(sqlKeyword, StringComparison.Ordinal) >= 0)
                    {
                        input = input.Replace(sqlKeyword, string.Empty);
                    }
                }
                return input;
            }
        }


    }
}