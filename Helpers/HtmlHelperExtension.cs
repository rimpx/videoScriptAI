using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;

namespace videoscriptAI.Helpers
{
    public static class HtmlHelperExtensions
    {
        public static string GetRawString(this IHtmlContent content)
        {
            if (content == null)
                return null;

            using (var writer = new System.IO.StringWriter())
            {
                content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                return writer.ToString();
            }
        }
    }
}